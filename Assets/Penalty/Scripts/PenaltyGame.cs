using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
namespace PenaltyArena
{
    public sealed class PenaltyGame : MonoBehaviour
    {
        [Header("Oynanış — iki oyuncuya eşit uygulanır")]
        public float ballSpeed = 20, diveReach = 2.9f, diveDuration = .65f, shotTimeout = 6;
        public Rigidbody ball;
        public Transform keeper, striker;
        public Camera gameCamera;
        public Vector3 shooterCameraPosition=new Vector3(0,1.6f,15.5f),shooterCameraTarget=new Vector3(0,-1,0);
        public Vector3 keeperCameraPosition=new Vector3(0,1.4f,-3),keeperCameraTarget=new Vector3(0,.85f,10);
        public Renderer[] goalNet = Array.Empty<Renderer>();
        double netHitAt;
        Vector3 netHitPoint;
        float netHitSpeed;
        public RoomConnection room;
        public CharacterMotion keeperMotion, strikerMotion;
        public double NetworkNow => room.NetworkTime;
        public MatchSnapshot View { get; private set; } = new MatchSnapshot();
        public MatchPhase Phase => (MatchPhase)View.phase;
        public bool Shooting => room.Connected && room.LocalSlot == View.shooter;
        public bool Easy { get; set; } = true;
        public string PlayerName = "Oyuncu";
        MatchRules rules = new MatchRules();
        TurnChoices choices;
        float turnDiveDuration;
        int submittedShotId=-1;
        public bool ChoiceLocked => submittedShotId==View.shotId || (room.LocalSlot==0?View.locked0:View.locked1);
        public bool CanChoose => room.Connected && !Stale && Phase==MatchPhase.Aiming && !ChoiceLocked;
        public float CurrentDiveDuration => room.Connected ? (room.Host ? (turnDiveDuration>0?turnDiveDuration:diveDuration) : (View.diveDuration>0?View.diveDuration:diveDuration)) : diveDuration;
        readonly bool[] ready = new bool[2], rematch = new bool[2];
        readonly string[] names = { "Oyuncu 1", "Oyuncu 2" };
        MatchPhase phase = MatchPhase.Lobby;
        float started, resultAt, diveAt = -100, nextSend, nextHello, lastReceived;
        Vector3 diveTarget, lastBall, queuedVelocity;
        double shotAt, diveStartedAt=-100;
        bool dived, touched, gesture;
        int diveCode;
        Vector2 gestureStart;
        float authoredFieldOfView;
        public static float FitVerticalFieldOfView(float vertical,float aspect)=>aspect>=16f/9?vertical:2*Mathf.Atan(Mathf.Tan(vertical*Mathf.Deg2Rad*.5f)*(16f/9)/Mathf.Max(.5f,aspect))*Mathf.Rad2Deg;
        public void UpdateCameraFraming(){if(authoredFieldOfView<=0)authoredFieldOfView=gameCamera.fieldOfView;gameCamera.fieldOfView=FitVerticalFieldOfView(authoredFieldOfView,gameCamera.aspect);}
        public Vector2 Aim { get; private set; }
        public float Power { get; private set; }
        public bool Aiming => gesture && Shooting;
        int sequence, lastSequence = -1;
        public bool Stale => room.Connected && !room.Host && Time.unscaledTime - lastReceived > 2;
        void Start()
        {
            Application.targetFrameRate = 60; Application.runInBackground = true;
            room.Command += Handle; room.Snapshot += Receive; room.Lost += Disconnected;
            ball.GetComponent<BallContact>().game = this;
            ResetBall();
            if(GetComponent<StadiumAudio>()==null)gameObject.AddComponent<StadiumAudio>();
            gameObject.AddComponent<ShotPresentation>();
        }
        void OnDestroy() { room.Command -= Handle; room.Snapshot -= Receive; room.Lost -= Disconnected; }
        void Disconnected()
        {
            phase = MatchPhase.Disconnected; View.phase = (int)phase;
            ball.isKinematic = true; gesture = false; shotAt=0;diveCode=0;ready[0] = ready[1] = false;
            netHitAt = View.netHitAt = 0;
            rematch[0] = rematch[1] = false; lastSequence = -1; choices=null;submittedShotId=-1;View.locked0=View.locked1=false;View.diveCode=0;View.shotStartedAt=0;View.diveStartedAt=-100;
        }
        void Receive(MatchSnapshot state)
        {
            if (!room.Host && state.sequence <= lastSequence) return;
            lastSequence = state.sequence; lastReceived = Time.unscaledTime;
            if (state.shotId != View.shotId || state.phase != View.phase) gesture = false;
            if(state.shotId!=View.shotId || (state.phase==(int)MatchPhase.Aiming && View.phase!=state.phase))submittedShotId=-1;
            View = state;
            if (!room.Host) ball.isKinematic = true;
        }
        public void Ready() => room.Send(new PlayerCommand { action = "ready", name = PlayerName });
        public void Rematch() => room.Send(new PlayerCommand { action = "rematch" });
        void Handle(int slot, PlayerCommand c)
        {
            if (!room.Host || slot < 0 || slot > 1 || c == null) return;
            if (c.action == "hello") { names[slot] = CleanName(c.name); return; }
            if (phase == MatchPhase.Disconnected && room.Count == 2) phase = MatchPhase.Lobby;
            if (c.action == "ready" && phase == MatchPhase.Lobby) {
                names[slot] = CleanName(c.name); ready[slot] = true;
                if (room.Count == 2 && ready[0] && ready[1]) Begin(); return;
            }
            if (c.action == "rematch" && phase == MatchPhase.Finished) {
                rematch[slot] = true; if (rematch[0] && rematch[1] && room.Count == 2) Begin(); return;
            }
            if(choices==null || !choices.TryLock(slot,c,phase))return;
            nextSend=0;
            if(!choices.BothLocked)return;
            var shot=choices.Shot;var save=choices.Save;
            var target=new Vector3(shot.X,shot.Y+Mathf.Max(0,shot.Power-.85f)*5,.12f);
            float speed=ballSpeed*(Easy?.8f:1)*Mathf.Lerp(.7f,1.15f,shot.Power);
            float flight=11/speed;
            queuedVelocity=(target-ball.position)/flight-.5f*Physics.gravity*flight;
            // Both animations share one future server timestamp. Full reach meets the ball,
            // including the striker's authored run-up before foot contact.
            shotAt=diveStartedAt=NetworkNow+.35;
            turnDiveDuration=MotionTiming.StrikeContact+flight*.95f;
            diveAt=Time.time+.35f;dived=true;
            diveCode=MotionTiming.DiveCode(save.X,save.Y);
            diveTarget=new Vector3(save.X*diveReach,save.Y*1.2f,.45f);
            phase=MatchPhase.Windup;
        }
        static string CleanName(string name) => string.IsNullOrWhiteSpace(name) ? "Oyuncu" : new string(name.Where(c => !char.IsControl(c) && c != '<' && c != '>').Take(16).ToArray());
        void Begin()
        {
            rules = new MatchRules(); rematch[0] = rematch[1] = false; ResetShot();
        }
        void ResetBall()
        {
            netHitAt = 0;
            ball.isKinematic = true; ball.position = new Vector3(0, .115f, 11); ball.rotation = Quaternion.identity;
            keeper.position = new Vector3(0, 0, .45f);
            if(strikerMotion!=null)striker.position=strikerMotion.home;
        }
        void ResetShot() { ResetBall(); choices=new TurnChoices(rules.ShotId,rules.Shooter); turnDiveDuration=0; phase = MatchPhase.Aiming; dived = touched = false; diveCode = 0; diveAt = -100; diveStartedAt=-100; shotAt=0; started = Time.time; }
        void Resolve(ShotResult outcome)
        {
            if (phase != MatchPhase.Flying || !rules.Resolve(rules.ShotId, outcome)) return;
            phase = rules.Finished ? MatchPhase.Finished : MatchPhase.Result; resultAt = Time.time;
        }
        public void Contact(bool goalkeeper) { if (room.Host && phase == MatchPhase.Flying && goalkeeper) touched = true; }
        public void ContactNet(Vector3 point, float speed)
        {
            if (!room.Host || netHitAt > 0 || speed < .5f) return;
            if (phase != MatchPhase.Result && phase != MatchPhase.Finished) return;
            if (rules.History[rules.Shooter].LastOrDefault() != ShotResult.Goal) return;
            netHitAt = NetworkNow;
            netHitPoint = point;
            netHitSpeed = speed;
            // A loose net absorbs much more energy than the rigid backstop.
            ball.linearVelocity *= .28f;
            ball.angularVelocity *= .6f;
            nextSend = 0;
        }
        void FixedUpdate()
        {
            if (!room.Host || !room.Connected) return;
            double now=NetworkNow;
            float shotElapsed=shotAt>0?(float)(now-shotAt):-1;
            if(strikerMotion!=null)strikerMotion.Evaluate(diveCode,0,shotAt>0,phase==MatchPhase.Windup?Mathf.Min(shotElapsed,MotionTiming.StrikeContact):shotElapsed,now);
            if(keeperMotion!=null) {
                keeperMotion.Evaluate(diveCode,(float)(now-diveStartedAt),shotAt>0,shotElapsed,now);
                keeperMotion.hitboxes.SetActive(phase!=MatchPhase.Disconnected);keeperMotion.hitboxes.Sync();
                Physics.SyncTransforms();
            }
            else if (phase == MatchPhase.Flying || phase == MatchPhase.Aiming || phase==MatchPhase.Windup) {
                float t = (Time.time - diveAt) / CurrentDiveDuration;
                float amount = dived ? Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI * .5f) : 0;
                keeper.position = Vector3.Lerp(new Vector3(0, 0, .45f), diveTarget, amount);
                if (dived && t > 1.8f && phase == MatchPhase.Aiming) { keeper.position = new Vector3(0, 0, .45f); }
            }
            if(phase==MatchPhase.Windup && shotElapsed>=MotionTiming.StrikeContact) {
                ball.isKinematic=false;ball.linearVelocity=queuedVelocity;ball.angularVelocity=new Vector3(12,2,0);
                phase=MatchPhase.Flying;started=Time.time;lastBall=ball.position;nextSend=0;
            }
            if (phase == MatchPhase.Flying) {
                Vector3 p = ball.position;
                // Goal: the entire ball crosses the plane between the inner faces of the posts.
                if (lastBall.z >= -.11f && p.z < -.11f) {
                    var crossing = Vector3.Lerp(lastBall, p, (lastBall.z + .11f) / (lastBall.z - p.z));
                    if (Mathf.Abs(crossing.x) <= 3.66f - .11f && crossing.y >= .11f && crossing.y <= 2.44f - .11f) Resolve(ShotResult.Goal);
                }
                if (phase == MatchPhase.Flying && (Time.time - started > shotTimeout || p.z < -3 || Mathf.Abs(p.x) > 9 || p.z > 20)) Resolve(touched ? ShotResult.Save : ShotResult.Miss);
                lastBall = p;
            }
            if (phase == MatchPhase.Result && Time.time - resultAt > 2.5f &&
                (!dived || now-diveStartedAt>=MotionTiming.SaveLength(CurrentDiveDuration)+.1f) && rules.NextShot()) ResetShot();
        }
        void Update()
        {
            if (room.Connected && Time.unscaledTime > nextHello) { nextHello = Time.unscaledTime + 2; room.Send(new PlayerCommand { action = "hello", name = PlayerName }); }
            if (room.Host && room.Connected && Time.unscaledTime >= nextSend) {
                nextSend = Time.unscaledTime + .05f;
                room.Broadcast(new MatchSnapshot { sequence = ++sequence, shotId = rules.ShotId, shooter = rules.Shooter, phase = (int)phase,
                    score0 = rules.Score[0], score1 = rules.Score[1], winner = rules.Winner,
                    locked0=choices!=null&&choices.IsLocked(0),locked1=choices!=null&&choices.IsLocked(1),diveDuration=turnDiveDuration,
                    name0 = names[0], name1 = names[1], ready0 = ready[0], ready1 = ready[1], count = room.Count,
                    history0 = string.Join("", rules.History[0].Select(x => x == ShotResult.Goal ? "G" : x == ShotResult.Save ? "K" : "D")),
                    history1 = string.Join("", rules.History[1].Select(x => x == ShotResult.Goal ? "G" : x == ShotResult.Save ? "K" : "D")),
                    ball = ball.position, ballRotation = ball.rotation, keeper = keeper.position, easy = Easy,
                    diveCode = diveCode, diveProgress = dived ? Mathf.Clamp01((Time.time-diveAt)/CurrentDiveDuration) : 0,
                    shotStartedAt=shotAt,diveStartedAt=diveStartedAt,serverSentAt=NetworkNow,
                    netHitAt=netHitAt,netHitPoint=netHitPoint,netHitSpeed=netHitSpeed,
                    ballVelocity=phase==MatchPhase.Flying?ball.linearVelocity:Vector3.zero,
                    message = phase == MatchPhase.Result || phase == MatchPhase.Finished ? (rules.History[rules.Shooter].LastOrDefault() == ShotResult.Goal ? "GOL" : touched ? "KURTARIŞ" : "DIŞARIDA") : "" });
            }
            if (!room.Host && room.Connected) {
                float blend = 1 - Mathf.Exp(-22 * Time.deltaTime);
                float age=(float)System.Math.Clamp(NetworkNow-View.serverSentAt,0,.1);
                Vector3 displayedBall=View.ball;
                if(Phase==MatchPhase.Flying)displayedBall+=View.ballVelocity*age+.5f*Physics.gravity*age*age;
                ball.transform.position = Vector3.Lerp(ball.transform.position, displayedBall, blend);
                ball.transform.rotation = Quaternion.Slerp(ball.transform.rotation, View.ballRotation, blend);
                if(keeperMotion==null)keeper.position = Vector3.Lerp(keeper.position, View.keeper, blend);
            }
            UpdateCameraFraming();
            bool inMatch = room.Connected && Phase != MatchPhase.Lobby && Phase != MatchPhase.Disconnected;
            bool goalie = inMatch && !Shooting;
            SetGoalNetVisible(!goalie || View.netHitAt > 0);
            Vector3 cameraPos = goalie ? keeperCameraPosition : shooterCameraPosition;
            Vector3 look = goalie ? keeperCameraTarget : shooterCameraTarget;
            gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, cameraPos, 1 - Mathf.Exp(-5 * Time.deltaTime));
            gameCamera.transform.rotation = Quaternion.Slerp(gameCamera.transform.rotation, Quaternion.LookRotation(look - gameCamera.transform.position), 1 - Mathf.Exp(-5 * Time.deltaTime));
            ReadInput();
        }
        // The keeper camera sits behind the goal; hide only its visual net, preserving collisions.
        public void SetGoalNetVisible(bool visible)
        {
            foreach(var renderer in goalNet)if(renderer!=null)renderer.enabled=visible;
        }
        public float ScreenDirectionToWorld(float x) => x * (gameCamera.transform.right.x >= 0 ? 1 : -1);
        public void SubmitShot(float x,float y,float power)
        {
            if(!CanChoose || !Shooting)return;
            submittedShotId=View.shotId;Aim=new Vector2(x,y);Power=power;
            room.Send(new PlayerCommand {action="shoot",shotId=View.shotId,x=x,y=y,power=power});
        }
        public void SubmitDive(float x,float y)
        {
            if(!CanChoose || Shooting)return;
            submittedShotId=View.shotId;
            room.Send(new PlayerCommand {action="dive",shotId=View.shotId,x=x,y=y});
        }
        void ReadInput()
        {
            if (!CanChoose){gesture=false;return;}
            var ui = GetComponent<ArenaUI>();
            if(ui!=null && ui.SettingsVisible){gesture=false;return;}
            Vector2 pos = Vector2.zero; bool down = false, held = false, up = false;
            if (Touchscreen.current != null && (Touchscreen.current.primaryTouch.press.isPressed || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)) {
                var touch = Touchscreen.current.primaryTouch; pos = touch.position.ReadValue(); down = touch.press.wasPressedThisFrame; held = touch.press.isPressed; up = touch.press.wasReleasedThisFrame;
            } else if (Mouse.current != null) { pos = Mouse.current.position.ReadValue(); down = Mouse.current.leftButton.wasPressedThisFrame; held = Mouse.current.leftButton.isPressed; up = Mouse.current.leftButton.wasReleasedThisFrame; }
            if (down) {
                bool field = pos.y > Screen.height * .16f && pos.y < Screen.height * .83f && pos.x < Screen.width * .92f;
                Vector2 ballScreen = gameCamera.WorldToScreenPoint(ball.position);
                field = field && (ui == null || !ui.BlocksInput(pos));
                gesture = field && (!Shooting || (Phase == MatchPhase.Aiming && Vector2.Distance(pos, ballScreen) < Screen.height * .16f)); gestureStart = pos;
            }
            if (gesture && (held || up)) {
                Vector2 delta = (pos - gestureStart) / Screen.height;
                Aim = new Vector2(Mathf.Clamp(ScreenDirectionToWorld(delta.x) * 14, -4.8f, 4.8f), Mathf.Clamp(delta.y * 6, .15f, 3.6f)); Power = Mathf.Clamp01(delta.magnitude / .5f);
                if (up) {
                    if (Shooting && delta.magnitude > .035f) SubmitShot(Aim.x,Aim.y,Power);
                    else if (!Shooting) SubmitDive(delta.magnitude < .035f ? 0 : Mathf.Clamp(ScreenDirectionToWorld(delta.x) * 8, -1, 1),Mathf.Clamp01(delta.y*8));
                    gesture = false;
                }
            }
            var key = Keyboard.current;
            if (!Shooting && key != null && (key.leftArrowKey.wasPressedThisFrame || key.rightArrowKey.wasPressedThisFrame || key.spaceKey.wasPressedThisFrame))
                SubmitDive(ScreenDirectionToWorld(key.leftArrowKey.wasPressedThisFrame ? -1 : key.rightArrowKey.wasPressedThisFrame ? 1 : 0),key.upArrowKey.isPressed ? 1 : 0);
        }
    }
}
