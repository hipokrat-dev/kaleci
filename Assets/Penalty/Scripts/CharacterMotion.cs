using UnityEngine;

namespace PenaltyArena
{
    // Generic bone curves baked on the actual rig; no Animator events or root motion decide gameplay.
    public sealed class CharacterMotion : MonoBehaviour
    {
        public PenaltyGame game;
        public bool goalkeeper;
        public Transform actorRoot, visual;
        public AnimationClip idle, strike;
        public AnimationClip[] saves = new AnimationClip[10];
        public float[] handAtReach = new float[10];
        public Transform[] bones;
        public Transform leftHand,rightHand,leftFoot,rightFoot,rightToes,hips,head;
        public KeeperHitboxes hitboxes;
        public Vector3 home, contactRoot = new Vector3(.22f,0,11.35f);
        int predictedCode;
        double predictedAt = -100, acknowledgedAt = -100;
        Vector3[] blendPositions;
        Quaternion[] blendRotations;
        void Awake() { Initialize(); }
        public void Initialize()
        {
            var animator=visual.GetComponent<Animator>();if(animator!=null)animator.enabled=false;
            if(blendPositions==null || blendPositions.Length!=bones.Length) {blendPositions=new Vector3[bones.Length];blendRotations=new Quaternion[bones.Length];}
        }
        public void PredictDive(int code,double now)
        {
            if(!goalkeeper || game.room.Host || predictedCode!=0)return;
            if(game.View.diveCode!=0 && now-game.View.diveStartedAt<MotionTiming.SaveLength(game.diveDuration))return;
            predictedCode=code;predictedAt=now;acknowledgedAt=game.View.diveStartedAt;
        }
        void LateUpdate()
        {
            // Host poses and colliders are sampled together immediately before its physics step.
            if(game==null || (game.room.Connected && game.room.Host))return;
            var v=game.View;double now=game.NetworkNow;
            int code=v.diveCode;float elapsed=code==0?-1:(float)(now-v.diveStartedAt);
            if(predictedCode!=0) {
                if(v.diveStartedAt!=acknowledgedAt || now-predictedAt>.5 || game.Phase==MatchPhase.Disconnected)predictedCode=0;
                else {code=predictedCode;elapsed=(float)(now-predictedAt);}
            }
            Evaluate(code,elapsed,v.shotStartedAt>0,(float)(now-v.shotStartedAt),now);
            if(hitboxes!=null)hitboxes.SetActive(false);
        }
        public void Evaluate(int code,float elapsed,bool shotActive,float shotElapsed,double clock)
        {
            Initialize();
            if(goalkeeper) {
                float t=MotionTiming.SaveTime(elapsed,game.diveDuration);
                actorRoot.position=home;
                if(code<=0 || code>=saves.Length || elapsed<0 || t>=MotionTiming.SaveEnd)Sample(idle,(float)(clock%idle.length));
                else {
                    Sample(saves[code],t);
                    float blend=1-MotionTiming.Smooth(0,.10f,t)+MotionTiming.Smooth(MotionTiming.SaveEnd-.15f,MotionTiming.SaveEnd,t);
                    if(blend>0)BlendIdle(Mathf.Clamp01(blend));
                    int direction=code>=7?1:code>=4?-1:0;
                    float distance=Mathf.Max(0,game.diveReach-Mathf.Abs(handAtReach[code]));
                    actorRoot.position=home+Vector3.right*(direction*distance*MotionTiming.Travel(t));
                }
            } else {
                float t=Mathf.Max(0,shotElapsed);
                if(!shotActive || shotElapsed<0 || t>=MotionTiming.StrikeEnd) {actorRoot.position=home;Sample(idle,(float)(clock%idle.length));}
                else {
                    float approach=MotionTiming.Smooth(0,.42f,t)*(1-MotionTiming.Smooth(1.35f,MotionTiming.StrikeEnd,t));
                    actorRoot.position=Vector3.Lerp(home,contactRoot,approach);Sample(strike,t);
                }
            }
        }
        void Sample(AnimationClip clip,float time) { if(clip!=null)clip.SampleAnimation(visual.gameObject,Mathf.Clamp(time,0,clip.length)); }
        void BlendIdle(float weight)
        {
            for(int i=0;i<bones.Length;i++){blendPositions[i]=bones[i].localPosition;blendRotations[i]=bones[i].localRotation;}
            Sample(idle,0);
            for(int i=0;i<bones.Length;i++) {
                bones[i].localPosition=Vector3.Lerp(blendPositions[i],bones[i].localPosition,weight);
                bones[i].localRotation=Quaternion.Slerp(blendRotations[i],bones[i].localRotation,weight);
            }
        }
    }
}
