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
        public RoomConnection room;
        public MatchSnapshot View { get; private set; } = new MatchSnapshot();
        public MatchPhase Phase => (MatchPhase)View.phase;
        public bool Shooting => room.Connected && room.LocalSlot == View.shooter;
        public bool Easy { get; set; } = true;
        public string PlayerName = "Oyuncu";
        MatchRules rules = new MatchRules();
        readonly bool[] ready = new bool[2], rematch = new bool[2];
        readonly string[] names = { "Oyuncu 1", "Oyuncu 2" };
        MatchPhase phase = MatchPhase.Lobby;
        float started, resultAt, diveAt = -100, nextSend, nextHello, lastReceived;
        Vector3 diveTarget, lastBall;
        bool dived, touched, gesture;
        int diveCode;
        Vector2 gestureStart;
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
        }
        void OnDestroy() { room.Command -= Handle; room.Snapshot -= Receive; room.Lost -= Disconnected; }
        void Disconnected()
        {
            phase = MatchPhase.Disconnected; View.phase = (int)phase;
            ball.isKinematic = true; gesture = false; ready[0] = ready[1] = false;
            rematch[0] = rematch[1] = false; lastSequence = -1;
        }
        void Receive(MatchSnapshot state)
        {
            if (!room.Host && state.sequence <= lastSequence) return;
            lastSequence = state.sequence; lastReceived = Time.unscaledTime;
            if (state.shotId != View.shotId || state.phase != View.phase) gesture = false;
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
            if (c.shotId != rules.ShotId || !Finite(c.x) || !Finite(c.y) || !Finite(c.power)) return;
            if (c.action == "shoot" && MatchRules.CanAct(slot, rules.Shooter, phase, true)) {
                var target = new Vector3(Mathf.Clamp(c.x, -4.8f, 4.8f), Mathf.Clamp(c.y, .15f, 3.6f), .12f);
                float power = Mathf.Clamp01(c.power);
                // Excess power can lift the shot above the crossbar. No aim is sent until release.
                target.y += Mathf.Max(0, power - .85f) * 5;
                float speed = ballSpeed * (Easy ? .8f : 1) * Mathf.Lerp(.7f, 1.15f, power);
                float flight = 11 / speed;
                ball.isKinematic = false;
                ball.linearVelocity = (target - ball.position) / flight - .5f * Physics.gravity * flight;
                ball.angularVelocity = new Vector3(12, 2, 0);
                phase = MatchPhase.Flying; started = Time.time; lastBall = ball.position;
            }
            if (c.action == "dive" && !dived && MatchRules.CanAct(slot, rules.Shooter, phase, false)) {
                dived = true; diveAt = Time.time;
                float direction = Mathf.Clamp(c.x, -1, 1);
                int height = c.y < .25f ? 1 : c.y < .7f ? 2 : 3;
                diveCode = height + (direction < -.15f ? 3 : direction > .15f ? 6 : 0);
                diveTarget = new Vector3(direction * diveReach, Mathf.Clamp01(c.y) * 1.2f, .45f);
            }
        }
        static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        static string CleanName(string name) => string.IsNullOrWhiteSpace(name) ? "Oyuncu" : new string(name.Where(c => !char.IsControl(c) && c != '<' && c != '>').Take(16).ToArray());
        void Begin()
        {
            rules = new MatchRules(); rematch[0] = rematch[1] = false; ResetShot();
        }
        void ResetBall()
        {
            ball.isKinematic = true; ball.position = new Vector3(0, .115f, 11); ball.rotation = Quaternion.identity;
            keeper.position = new Vector3(0, 0, .45f);
        }
        void ResetShot() { ResetBall(); phase = MatchPhase.Aiming; dived = touched = false; diveCode = 0; diveAt = -100; started = Time.time; }
        void Resolve(ShotResult outcome)
        {
            if (phase != MatchPhase.Flying || !rules.Resolve(rules.ShotId, outcome)) return;
            phase = rules.Finished ? MatchPhase.Finished : MatchPhase.Result; resultAt = Time.time;
        }
        public void Contact(bool goalkeeper) { if (room.Host && phase == MatchPhase.Flying && goalkeeper) touched = true; }
        void FixedUpdate()
        {
            if (!room.Host || !room.Connected) return;
            if (phase == MatchPhase.Flying || phase == MatchPhase.Aiming) {
                float t = (Time.time - diveAt) / diveDuration;
                float amount = dived ? Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI * .5f) : 0;
                keeper.position = Vector3.Lerp(new Vector3(0, 0, .45f), diveTarget, amount);
                if (dived && t > 1.8f && phase == MatchPhase.Aiming) { keeper.position = new Vector3(0, 0, .45f); }
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
            if (phase == MatchPhase.Result && Time.time - resultAt > 2.5f && rules.NextShot()) ResetShot();
        }
        void Update()
        {
            if (room.Connected && Time.unscaledTime > nextHello) { nextHello = Time.unscaledTime + 2; room.Send(new PlayerCommand { action = "hello", name = PlayerName }); }
            if (room.Host && room.Connected && Time.unscaledTime >= nextSend) {
                nextSend = Time.unscaledTime + .05f;
                room.Broadcast(new MatchSnapshot { sequence = ++sequence, shotId = rules.ShotId, shooter = rules.Shooter, phase = (int)phase,
                    score0 = rules.Score[0], score1 = rules.Score[1], winner = rules.Winner,
                    name0 = names[0], name1 = names[1], ready0 = ready[0], ready1 = ready[1], count = room.Count,
                    history0 = string.Join("", rules.History[0].Select(x => x == ShotResult.Goal ? "G" : x == ShotResult.Save ? "K" : "D")),
                    history1 = string.Join("", rules.History[1].Select(x => x == ShotResult.Goal ? "G" : x == ShotResult.Save ? "K" : "D")),
                    ball = ball.position, ballRotation = ball.rotation, keeper = keeper.position, easy = Easy,
                    diveCode = diveCode, diveProgress = dived ? Mathf.Clamp01((Time.time-diveAt)/diveDuration) : 0,
                    message = phase == MatchPhase.Result || phase == MatchPhase.Finished ? (rules.History[rules.Shooter].LastOrDefault() == ShotResult.Goal ? "GOL" : touched ? "KURTARIŞ" : "DIŞARIDA") : "" });
            }
            if (!room.Host && room.Connected) {
                float blend = 1 - Mathf.Exp(-22 * Time.deltaTime);
                ball.transform.position = Vector3.Lerp(ball.transform.position, View.ball, blend);
                ball.transform.rotation = Quaternion.Slerp(ball.transform.rotation, View.ballRotation, blend);
                keeper.position = Vector3.Lerp(keeper.position, View.keeper, blend);
            }
            bool inMatch = room.Connected && Phase != MatchPhase.Lobby && Phase != MatchPhase.Disconnected;
            bool goalie = inMatch && !Shooting;
            Vector3 cameraPos = goalie ? new Vector3(1.1f, 2.8f, -3.6f) : new Vector3(-1.7f, 2.4f, 15.5f);
            Vector3 look = goalie ? new Vector3(0, .75f, 10) : new Vector3(0, 1.1f, 0);
            gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, cameraPos, 1 - Mathf.Exp(-5 * Time.deltaTime));
            gameCamera.transform.rotation = Quaternion.Slerp(gameCamera.transform.rotation, Quaternion.LookRotation(look - gameCamera.transform.position), 1 - Mathf.Exp(-5 * Time.deltaTime));
            ReadInput();
        }
        void ReadInput()
        {
            if (!room.Connected || Stale || !(Phase == MatchPhase.Aiming || Phase == MatchPhase.Flying)) return;
            Vector2 pos = Vector2.zero; bool down = false, held = false, up = false;
            if (Touchscreen.current != null && (Touchscreen.current.primaryTouch.press.isPressed || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)) {
                var touch = Touchscreen.current.primaryTouch; pos = touch.position.ReadValue(); down = touch.press.wasPressedThisFrame; held = touch.press.isPressed; up = touch.press.wasReleasedThisFrame;
            } else if (Mouse.current != null) { pos = Mouse.current.position.ReadValue(); down = Mouse.current.leftButton.wasPressedThisFrame; held = Mouse.current.leftButton.isPressed; up = Mouse.current.leftButton.wasReleasedThisFrame; }
            if (down) {
                bool field = pos.y > Screen.height * .16f && pos.y < Screen.height * .83f && pos.x < Screen.width * .92f;
                Vector2 ballScreen = gameCamera.WorldToScreenPoint(ball.position);
                var ui = GetComponent<ArenaUI>();
                field = field && (ui == null || !ui.BlocksInput(pos));
                gesture = field && (!Shooting || (Phase == MatchPhase.Aiming && Vector2.Distance(pos, ballScreen) < Screen.height * .16f)); gestureStart = pos;
            }
            if (gesture && (held || up)) {
                Vector2 delta = (pos - gestureStart) / Screen.height;
                Aim = new Vector2(Mathf.Clamp(delta.x * 14, -4.8f, 4.8f), Mathf.Clamp(delta.y * 6, .15f, 3.6f)); Power = Mathf.Clamp01(delta.magnitude / .5f);
                if (up) {
                    if (Shooting && delta.magnitude > .035f) room.Send(new PlayerCommand { action = "shoot", shotId = View.shotId, x = Aim.x, y = Aim.y, power = Power });
                    else if (!Shooting) room.Send(new PlayerCommand { action = "dive", shotId = View.shotId, x = delta.magnitude < .035f ? 0 : -Mathf.Clamp(delta.x * 8, -1, 1), y = Mathf.Clamp01(delta.y * 8) });
                    gesture = false;
                }
            }
            var key = Keyboard.current;
            if (!Shooting && key != null && (key.leftArrowKey.wasPressedThisFrame || key.rightArrowKey.wasPressedThisFrame || key.spaceKey.wasPressedThisFrame))
                room.Send(new PlayerCommand { action = "dive", shotId = View.shotId, x = key.leftArrowKey.wasPressedThisFrame ? 1 : key.rightArrowKey.wasPressedThisFrame ? -1 : 0, y = key.upArrowKey.isPressed ? 1 : 0 });
        }
    }
}
