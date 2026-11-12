using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PenaltyArena.Editor
{
    public static partial class MotionAuthoring
    {
        public const string Art="Assets/Penalty/Art/Motion";
        public const string Output="Assets/Penalty/Scenes/ArenaAnimated.unity";
        const string Request="Temp/motion.request";
        static double nextCheck;
        [InitializeOnLoadMethod] static void Init() { EditorApplication.update-=Pending;EditorApplication.update+=Pending; }
        static void Pending()
        {
            if(Application.isBatchMode || EditorApplication.timeSinceStartup<nextCheck)return;
            nextCheck=EditorApplication.timeSinceStartup+1;
            if(!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying)return;
            string command=File.ReadAllText(Request).Trim();File.Delete(Request);
            try {
                if(command=="refresh")AssetDatabase.Refresh();
                else if(command=="inspect")Inspect();
                else if(command=="bake")Bake();
                else if(command=="verify")MotionChecks.Run();
                else if(command=="preview")MotionChecks.Preview();
                else if(command=="build") {RuleChecks.Run();MotionChecks.Run();ArenaBuilder.BuildMac();ArenaBuilder.BuildAndroid();}
                else throw new Exception("Unknown motion command: "+command);
                File.WriteAllText("Docs/motion-"+command+".done",DateTime.UtcNow.ToString("O"));
            } catch(Exception e) {File.WriteAllText("Docs/motion-error.txt",e.ToString());Debug.LogException(e);}
        }
        public static void Inspect()
        {
            Directory.CreateDirectory(Art);
            var scene=EditorSceneManager.NewPreviewScene();
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SuperGoalie/Meshes/Characters/Players/Player001/Character.fbx");
            var actor=Object.Instantiate(model);SceneManager.MoveGameObjectToScene(actor,scene);actor.transform.localScale=Vector3.one*.8861985f;
            var animator=actor.GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            string path=Art+"/Sampler.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm=controller.layers[0].stateMachine;foreach(var state in sm.states)sm.RemoveState(state.state);
            var report=new StringBuilder();
            var files=Directory.GetFiles("Assets/SuperGoalie/Animations/GoalKeeperAnimations","*.fbx");
            foreach(var file in files)foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview"))) {
                var state=sm.AddState(clip.name);state.motion=clip;
            }
            animator.runtimeAnimatorController=controller;
            try {
                foreach(var item in sm.states) {
                    var clip=(AnimationClip)item.state.motion;
                    report.AppendLine("CLIP "+clip.name+" duration="+clip.length+" human="+clip.humanMotion);
                    foreach(float t in new[]{0f,.25f,.5f,.75f,.99f}) {
                        animator.Rebind();animator.Play(item.state.name,0,t);animator.Update(0);
                        report.Append(t+": ");
                        foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.Head,HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot})report.Append(bone+"="+animator.GetBoneTransform(bone).position.ToString("F2")+" ");
                        report.AppendLine();
                    }
                }
                report.AppendLine("Muscles: "+string.Join(", ",HumanTrait.MuscleName));
                File.WriteAllText("Docs/motion-inspection.txt",report.ToString());
            } finally {EditorSceneManager.ClosePreviewScene(scene);}
            Debug.Log("MOTION_INSPECT_OK");
        }
    }
}
