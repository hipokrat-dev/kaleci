using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace PenaltyArena.Editor
{
    public static partial class MotionAuthoring
    {
        sealed class Rig
        {
            public GameObject actor; public Animator animator; public Transform[] bones;
            public Vector3[] neutralPositions; public Quaternion[] neutralRotations;
            public Transform Bone(HumanBodyBones bone)=>animator.GetBoneTransform(bone);
            public void Source(string state,float normalized) {animator.enabled=true;animator.Rebind();animator.Play(state,0,Mathf.Clamp(normalized,0,.99999f));animator.Update(0);}
            public void Neutral() {for(int i=0;i<bones.Length;i++){bones[i].localPosition=neutralPositions[i];bones[i].localRotation=neutralRotations[i];}}
            public void BlendNeutral(float weight) {for(int i=0;i<bones.Length;i++){bones[i].localPosition=Vector3.Lerp(bones[i].localPosition,neutralPositions[i],weight);bones[i].localRotation=Quaternion.Slerp(bones[i].localRotation,neutralRotations[i],weight);}}
            public void Ground()
            {
                float min=float.MaxValue;
                foreach(var bone in FloorBones)min=Mathf.Min(min,Bone(bone).position.y);
                if(min<.1f)Bone(HumanBodyBones.Hips).position+=Vector3.up*(.1f-min);
            }
        }
        static readonly HumanBodyBones[] FloorBones={HumanBodyBones.Head,HumanBodyBones.Hips,HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.LeftLowerLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.LeftLowerArm,HumanBodyBones.RightLowerArm};
        static readonly string[] Files={"IdleAlert","PunchDownCenter","PunchMiddleCentre","PunchHighCentre","PunchLowLeft","PunchMiddleLeft","PunchHighLeft","PunchLowLeft","PunchMiddleLeft","PunchHighLeft"};
        static readonly string[] Clips={"metarig|metarigAction","Take 001","Punch Fall Back","CINEMA_4D_Main","Low Dive Left","Mid High Dive Left","High Dive Left","Low Dive Left","Mid High Dive Left","High Dive Left"};
        static readonly float[] ReachSource={0,.297f,.533f,.4f,.833f,.567f,.533f,.833f,.567f,.533f};
        static AnimationClip SourceClip(int code)=>AssetDatabase.LoadAllAssetsAtPath("Assets/SuperGoalie/Animations/GoalKeeperAnimations/"+Files[code]+".fbx").OfType<AnimationClip>().Single(c=>c.name==Clips[code]);
        [MenuItem("Penalty Arena/07 — Şut ve kurtarışları oluştur")]
        public static void Bake()
        {
            Directory.CreateDirectory(Art);Directory.CreateDirectory("Docs");
            var preview=EditorSceneManager.NewPreviewScene();
            AnimationClip idle=null,strike=null,strikerIdle=null;var saves=new AnimationClip[10];var reach=new float[10];
            try {
                var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SuperGoalie/Meshes/Characters/Players/Player001/Character.fbx"));
                SceneManager.MoveGameObjectToScene(actor,preview);actor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);actor.transform.localScale=Vector3.one*.8861985f;
                var animator=actor.GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                string cp=Art+"/BakeSampler.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(cp);
                if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(cp);
                var sm=controller.layers[0].stateMachine;foreach(var state in sm.states)sm.RemoveState(state.state);
                for(int i=0;i<10;i++){var state=sm.AddState("S"+i);state.motion=SourceClip(i);state.mirror=i>=7;if(i==0)sm.defaultState=state;}
                animator.runtimeAnimatorController=controller;
                var rig=new Rig {actor=actor,animator=animator};rig.Source("S0",0);
                var selected=new HashSet<Transform>();
                for(int i=0;i<(int)HumanBodyBones.LastBone;i++) {
                    var bone=animator.GetBoneTransform((HumanBodyBones)i);
                    while(bone!=null && bone!=actor.transform){selected.Add(bone);bone=bone.parent;}
                }
                rig.bones=actor.GetComponentsInChildren<Transform>().Where(selected.Contains).ToArray();AuthorKeeperReady(rig,0);
                rig.neutralPositions=rig.bones.Select(t=>t.localPosition).ToArray();rig.neutralRotations=rig.bones.Select(t=>t.localRotation).ToArray();
                idle=BakeCurves(rig,"KeeperIdle",SourceClip(0).length,t=>AuthorKeeperReady(rig,t));
                strikerIdle=BakeCurves(rig,"StrikerReady",2.4f,t=>{AuthorStrike(rig,-1);var chest=rig.Bone(HumanBodyBones.Chest);chest.rotation=Quaternion.AngleAxis(Mathf.Sin(t*Mathf.PI*2/2.4f)*.7f,Vector3.right)*chest.rotation;});
                strike=BakeCurves(rig,"PenaltyStrike",MotionTiming.StrikeEnd,t=>AuthorStrike(rig,t));
                for(int i=1;i<10;i++) {
                    int code=i;float sourceLength=SourceClip(i).length;
                    saves[i]=BakeCurves(rig,"Save"+i,MotionTiming.SaveEnd,t=>AuthorSave(rig,code,t,sourceLength));
                    AuthorSave(rig,i,MotionTiming.SaveReach,sourceLength);
                    reach[i]=rig.Bone(i>=7?HumanBodyBones.RightHand:HumanBodyBones.LeftHand).position.x;
                }
                AssetDatabase.SaveAssets();
            } finally {EditorSceneManager.ClosePreviewScene(preview);}
            var original=SceneManager.GetActiveScene();
            // Generate a separate scene. User edits in the open scene remain untouched.
            string working=Art+"/Working.unity";
            File.Copy(StadiumIntegration.Output,working,true);AssetDatabase.ImportAsset(working);
            var scene=EditorSceneManager.OpenScene(working,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var game=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PenaltyGame>()).Single();
            var keeper=Configure(game,game.keeper,true,idle,strike,saves,reach);
            var shooter=Configure(game,game.striker,false,strikerIdle,strike,saves,reach);
            game.keeperMotion=keeper;game.strikerMotion=shooter;
            CreateHitboxes(game,keeper);
            keeper.Evaluate(0,-1,false,-1,0);shooter.Evaluate(0,-1,false,-1,0);keeper.hitboxes.Sync();keeper.hitboxes.SetActive(true);
            for(int i=SceneManager.sceneCount-1;i>=0;i--) {
                var loaded=SceneManager.GetSceneAt(i);
                if(loaded!=scene && loaded.path==Output) {
                    if(loaded.isDirty)EditorSceneManager.SaveScene(loaded,Art+"/BeforeRebuild.unity",true);
                    EditorSceneManager.CloseScene(loaded,true);
                }
            }
            if(!EditorSceneManager.SaveScene(scene,Output))throw new Exception("Could not save animated scene");
            EditorSceneManager.CloseScene(scene,true);
            if(original.IsValid() && !original.isDirty)EditorSceneManager.OpenScene(Output,OpenSceneMode.Single);
            else if(original.IsValid())SceneManager.SetActiveScene(original);
            else EditorSceneManager.OpenScene(Output,OpenSceneMode.Single);
            File.WriteAllText("Docs/motion-bake.txt","Full save clips + authored 2.4s shot baked at 30 Hz; contact at 0.58s. Source art and source scenes preserved.\nHand offsets at reach: "+string.Join(",",reach.Select(v=>v.ToString("F3")))+"\n");
            Debug.Log("MOTION_BAKE_OK");
        }
        static AnimationClip BakeCurves(Rig rig,string name,float length,Action<float> pose)
        {
            var times=new SortedSet<float>{0,length};
            for(int i=1;i<Mathf.CeilToInt(length*30);i++)times.Add(i/30f);
            foreach(float t in new[]{MotionTiming.StrikeContact,MotionTiming.SaveReach,MotionTiming.SaveLand,MotionTiming.SaveStand})if(t<length)times.Add(t);
            var ts=times.ToArray();var values=new float[rig.bones.Length*7,ts.Length];
            var previous=new Quaternion[rig.bones.Length];
            for(int frame=0;frame<ts.Length;frame++) {
                pose(ts[frame]);
                for(int b=0;b<rig.bones.Length;b++) {
                    var p=rig.bones[b].localPosition;var q=rig.bones[b].localRotation;
                    if(frame>0 && Quaternion.Dot(previous[b],q)<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);previous[b]=q;
                    values[b*7,frame]=p.x;values[b*7+1,frame]=p.y;values[b*7+2,frame]=p.z;
                    values[b*7+3,frame]=q.x;values[b*7+4,frame]=q.y;values[b*7+5,frame]=q.z;values[b*7+6,frame]=q.w;
                }
            }
            var clip=new AnimationClip {name=name,frameRate=30,legacy=true,wrapMode=WrapMode.ClampForever};
            string[] properties={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
            var bindings=new EditorCurveBinding[rig.bones.Length*7];var curves=new AnimationCurve[bindings.Length];
            for(int b=0;b<rig.bones.Length;b++)for(int channel=0;channel<7;channel++) {
                int k=b*7+channel;var keys=new Keyframe[ts.Length];
                for(int f=0;f<ts.Length;f++) {
                    float before=f==0?0:(values[k,f]-values[k,f-1])/(ts[f]-ts[f-1]);
                    float after=f==ts.Length-1?0:(values[k,f+1]-values[k,f])/(ts[f+1]-ts[f]);
                    keys[f]=new Keyframe(ts[f],values[k,f],before,after);
                }
                bindings[k]=EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(rig.bones[b],rig.actor.transform),typeof(Transform),properties[channel]);curves[k]=new AnimationCurve(keys);
            }
            AnimationUtility.SetEditorCurves(clip,bindings,curves);clip.EnsureQuaternionContinuity();
            string path=Art+"/"+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(existing==null){AssetDatabase.CreateAsset(clip,path);return clip;}
            EditorUtility.CopySerialized(clip,existing);EditorUtility.SetDirty(existing);Object.DestroyImmediate(clip);return existing;
        }
        static void AuthorKeeperReady(Rig r,float t)
        {
            r.Source("S0",t/SourceClip(0).length);
            SolveArm(r,false,new Vector3(-.36f,.96f,.38f));SolveArm(r,true,new Vector3(.36f,.96f,.38f));r.Ground();
        }
        static void AuthorStrike(Rig r,float t)
        {
            r.Neutral();
            float backswing=MotionTiming.Smooth(.20f,.43f,t)*(1-MotionTiming.Smooth(.43f,MotionTiming.StrikeContact,t));
            float follow=MotionTiming.Smooth(MotionTiming.StrikeContact,.78f,t)*(1-MotionTiming.Smooth(.85f,1.25f,t));
            var hips=r.Bone(HumanBodyBones.Hips);hips.position=new Vector3(0,.86f+follow*.04f,-.04f);
            hips.rotation=Quaternion.AngleAxis(7+follow*10,Vector3.right)*hips.rotation;
            var chest=r.Bone(HumanBodyBones.Chest);chest.rotation=Quaternion.AngleAxis(-backswing*18+follow*22,Vector3.up)*chest.rotation;
            Vector3 left=new Vector3(-.20f,.10f,.17f),right=new Vector3(.22f,.10f,-.16f);
            if(t>=0 && t<.22f){float step=Mathf.Sin(t/.22f*Mathf.PI);left+=new Vector3(0,step*.1f,-step*.17f);}
            if(t>=.15f && t<=MotionTiming.StrikeContact)right=Vector3.Lerp(Vector3.Lerp(right,new Vector3(.22f,.30f,-.5f),MotionTiming.Smooth(.15f,.43f,t)),new Vector3(.22f,.10f,.12f),MotionTiming.Smooth(.43f,MotionTiming.StrikeContact,t));
            if(t>MotionTiming.StrikeContact)right=Vector3.Lerp(new Vector3(.22f,.10f,.12f),new Vector3(.22f,.54f,.63f),follow);
            if(t>1.35f && t<MotionTiming.StrikeEnd) {
                float step=(t-1.35f)/1.05f*Mathf.PI*4;
                left+=new Vector3(0,Mathf.Max(0,Mathf.Sin(step))*.12f,Mathf.Cos(step)*.14f);
                right+=new Vector3(0,Mathf.Max(0,-Mathf.Sin(step))*.12f,-Mathf.Cos(step)*.14f);
            }
            SolveLeg(r,false,left);SolveLeg(r,true,right);
            // At contact the toe meets the rear surface of the 22 cm ball, not its center.
            if(t>=.43f && t<=.80f) {
                var foot=r.Bone(HumanBodyBones.RightFoot);var toe=r.Bone(HumanBodyBones.RightToes);
                float contact=1-Mathf.Clamp01(Mathf.Abs(t-MotionTiming.StrikeContact)/.15f);
                var desired=new Vector3(.22f,.115f,.23f);
                SolveLeg(r,true,Vector3.Lerp(foot.position,foot.position+desired-toe.position,contact));
            }
            float balance=t<0?0:MotionTiming.Smooth(0,.24f,t)*(1-MotionTiming.Smooth(1.25f,MotionTiming.StrikeEnd,t));
            SolveArm(r,false,Vector3.Lerp(new Vector3(-.27f,.83f,.04f),new Vector3(-.49f,1.12f+backswing*.12f,.10f-follow*.12f),balance));
            SolveArm(r,true,Vector3.Lerp(new Vector3(.28f,.82f,-.02f),new Vector3(.46f,1.04f+follow*.16f,-.07f+follow*.16f),balance));
            r.Ground();
        }
        static void AuthorSave(Rig r,int code,float t,float length)
        {
            if(t>=MotionTiming.SaveEnd){r.Neutral();return;}
            float sourceTime=t<=MotionTiming.SaveReach?t/MotionTiming.SaveReach*ReachSource[code]:Mathf.Lerp(ReachSource[code],length,MotionTiming.Smooth(MotionTiming.SaveReach,MotionTiming.SaveStand,t));
            r.Source("S"+code,sourceTime/length);
            int direction=code>=7?1:code>=4?-1:0;int height=(code-1)%3;
            float reach=MotionTiming.Smooth(.04f,MotionTiming.SaveReach,t)*(1-MotionTiming.Smooth(MotionTiming.SaveReach+.08f,MotionTiming.SaveLand,t));
            // Construct the reach from the ready pose so source torso tilt isn't applied twice.
            if(code!=1)r.BlendNeutral(reach);
            var hips=r.Bone(HumanBodyBones.Hips);
            Vector3 desiredHip=new Vector3(0,height==0?.36f:height==1?.8f:direction==0?1.25f:1.65f,-.02f);
            hips.position=Vector3.Lerp(hips.position,desiredHip,reach);
            if(direction!=0) {
                int index=Array.IndexOf(r.bones,hips);
                var neutralWorld=hips.parent.rotation*r.neutralRotations[index];
                hips.rotation=Quaternion.Slerp(hips.rotation,Quaternion.AngleAxis(-direction*(height==2?52:72),Vector3.forward)*neutralWorld,reach);
                float feetY=height==0?.12f:height==1?.35f:1.1f;
                SolveLeg(r,false,Vector3.Lerp(r.Bone(HumanBodyBones.LeftFoot).position,new Vector3(-direction*.66f,feetY,-.12f),reach));
                SolveLeg(r,true,Vector3.Lerp(r.Bone(HumanBodyBones.RightFoot).position,new Vector3(-direction*.60f,feetY+.20f,.14f),reach));
            }
            float handY=height==0?.22f:height==1?1.15f:2.18f;
            Vector3 lead=new Vector3(direction*1.12f,handY,.26f);
            if(direction==0) {
                SolveArm(r,false,Vector3.Lerp(r.Bone(HumanBodyBones.LeftHand).position,new Vector3(-.15f,handY,.42f),reach));
                SolveArm(r,true,Vector3.Lerp(r.Bone(HumanBodyBones.RightHand).position,new Vector3(.15f,handY,.42f),reach));
            } else {
                SolveArm(r,direction>0,Vector3.Lerp(r.Bone(direction>0?HumanBodyBones.RightHand:HumanBodyBones.LeftHand).position,lead,reach));
                SolveArm(r,direction<0,Vector3.Lerp(r.Bone(direction<0?HumanBodyBones.RightHand:HumanBodyBones.LeftHand).position,lead+new Vector3(-direction*.14f,.12f,.02f),reach));
            }
            r.BlendNeutral(1-MotionTiming.Smooth(0,.12f,t));
            // Centre-low ends on the ground; blend through its crouch into the ready stance.
            if(code==1)r.BlendNeutral(MotionTiming.Smooth(1.1f,MotionTiming.SaveStand,t));
            else r.BlendNeutral(MotionTiming.Smooth(MotionTiming.SaveStand-.22f,MotionTiming.SaveStand,t));
            if(t>MotionTiming.SaveStand) {
                r.Neutral();float u=(t-MotionTiming.SaveStand)/(MotionTiming.SaveEnd-MotionTiming.SaveStand);
                float step=Mathf.Sin(u*Mathf.PI*4)*Mathf.Sin(u*Mathf.PI);
                var left=r.Bone(HumanBodyBones.LeftFoot).position;var right=r.Bone(HumanBodyBones.RightFoot).position;
                SolveLeg(r,false,left+new Vector3(-direction*step*.10f,Mathf.Max(0,step)*.11f,0));
                SolveLeg(r,true,right+new Vector3(direction*step*.10f,Mathf.Max(0,-step)*.11f,0));
            }
            r.Ground();
        }
        static void SolveArm(Rig r,bool right,Vector3 target) => TwoBone(r.Bone(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm),r.Bone(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm),r.Bone(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand),target,new Vector3(right?1:-1,-1,-.3f));
        static void SolveLeg(Rig r,bool right,Vector3 target)
        {
            var foot=r.Bone(right?HumanBodyBones.RightFoot:HumanBodyBones.LeftFoot);
            TwoBone(r.Bone(right?HumanBodyBones.RightUpperLeg:HumanBodyBones.LeftUpperLeg),r.Bone(right?HumanBodyBones.RightLowerLeg:HumanBodyBones.LeftLowerLeg),foot,target,Vector3.forward);
            var toe=r.Bone(right?HumanBodyBones.RightToes:HumanBodyBones.LeftToes);
            foot.rotation=Quaternion.FromToRotation(toe.position-foot.position,new Vector3(0,-.02f,.18f))*foot.rotation;
        }
        static void TwoBone(Transform upper,Transform lower,Transform end,Vector3 target,Vector3 pole)
        {
            Vector3 start=upper.position;float a=Vector3.Distance(start,lower.position),b=Vector3.Distance(lower.position,end.position);
            Vector3 delta=target-start;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);var axis=delta.normalized;
            var bend=Vector3.ProjectOnPlane(pole,axis).normalized;if(bend.sqrMagnitude<.01f)bend=Vector3.Cross(axis,Vector3.right).normalized;
            float along=(a*a-b*b+distance*distance)/(2*distance);var elbow=start+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-start,elbow-start)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(end.position-lower.position,start+axis*distance-lower.position)*lower.rotation;
        }
        static CharacterMotion Configure(PenaltyGame game,Transform root,bool keeper,AnimationClip idle,AnimationClip strike,AnimationClip[] saves,float[] reach)
        {
            var animator=root.GetComponentInChildren<Animator>();var visual=animator.transform;
            var old=visual.GetComponent<KeeperPresentation>();if(old!=null)Object.DestroyImmediate(old);
            foreach(var collider in root.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);
            visual.localPosition=Vector3.zero;visual.localRotation=keeper?Quaternion.identity:Quaternion.Euler(0,180,0);
            var motion=visual.GetComponent<CharacterMotion>();if(motion==null)motion=visual.gameObject.AddComponent<CharacterMotion>();
            motion.game=game;motion.goalkeeper=keeper;motion.actorRoot=root;motion.visual=visual;
            motion.idle=idle;motion.strike=strike;motion.saves=saves;motion.handAtReach=reach;
            motion.home=keeper?new Vector3(0,0,.45f):new Vector3(1.2f,0,12.1f);
            motion.bones=AnimationUtility.GetCurveBindings(idle).Select(b=>visual.Find(b.path)).Where(t=>t!=null).Distinct().ToArray();
            motion.hips=animator.GetBoneTransform(HumanBodyBones.Hips);motion.head=animator.GetBoneTransform(HumanBodyBones.Head);
            motion.leftHand=animator.GetBoneTransform(HumanBodyBones.LeftHand);motion.rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            motion.leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);motion.rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);motion.rightToes=animator.GetBoneTransform(HumanBodyBones.RightToes);
            foreach(var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>()){r.updateWhenOffscreen=true;r.localBounds=new Bounds(Vector3.up,Vector3.one*7);}
            motion.Initialize();return motion;
        }
        static void CreateHitboxes(PenaltyGame game,CharacterMotion motion)
        {
            var root=new GameObject("AnimatedHitboxes");root.transform.SetParent(game.keeper,false);
            var rb=game.keeper.GetComponent<Rigidbody>();if(rb==null)rb=game.keeper.gameObject.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            var boxes=root.AddComponent<KeeperHitboxes>();motion.hitboxes=boxes;var animator=motion.visual.GetComponent<Animator>();
            var segments=new List<KeeperHitboxes.Segment>();
            void Segment(string name,HumanBodyBones a,HumanBodyBones b,float radius) {
                var o=new GameObject(name);o.transform.SetParent(root.transform,false);var c=o.AddComponent<CapsuleCollider>();c.direction=1;
                segments.Add(new KeeperHitboxes.Segment{a=animator.GetBoneTransform(a),b=animator.GetBoneTransform(b),capsule=c,radius=radius});
            }
            Segment("Torso",HumanBodyBones.Hips,HumanBodyBones.Chest,.18f);
            foreach(bool right in new[]{false,true}) {
                Segment(right?"RightUpperArm":"LeftUpperArm",right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm,right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm,.065f);
                Segment(right?"RightForearm":"LeftForearm",right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm,right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand,.065f);
                Segment(right?"RightThigh":"LeftThigh",right?HumanBodyBones.RightUpperLeg:HumanBodyBones.LeftUpperLeg,right?HumanBodyBones.RightLowerLeg:HumanBodyBones.LeftLowerLeg,.095f);
                Segment(right?"RightShin":"LeftShin",right?HumanBodyBones.RightLowerLeg:HumanBodyBones.LeftLowerLeg,right?HumanBodyBones.RightFoot:HumanBodyBones.LeftFoot,.075f);
            }
            boxes.segments=segments.ToArray();boxes.spheres=new SphereCollider[3];boxes.sphereBones=new[]{motion.leftHand,motion.rightHand,motion.head};
            for(int i=0;i<3;i++){var o=new GameObject(i==0?"LeftGlove":i==1?"RightGlove":"Head");o.transform.SetParent(root.transform,false);var sphere=o.AddComponent<SphereCollider>();sphere.radius=i==2?.13f:.105f;boxes.spheres[i]=sphere;}
        }
    }
}
