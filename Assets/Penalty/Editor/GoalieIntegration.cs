using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PenaltyArena.Editor
{
    public static class GoalieIntegration
    {
        const string Request="Temp/goalie-integration.request";
        const string Output="Assets/Penalty/Scenes/ArenaWithGoalie.unity";
        const string Art="Assets/Penalty/Art/Goalie";
        [InitializeOnLoadMethod] static void Init() { EditorApplication.delayCall += Pending; }
        static void Pending()
        {
            if (!File.Exists(Request)) return;
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying) { EditorApplication.delayCall+=Pending; return; }
            File.Delete(Request);
            try { Integrate(); } catch(Exception e) { File.WriteAllText("Docs/goalie-integration-error.txt",e.ToString()); Debug.LogException(e); }
        }
        [MenuItem("Penalty Arena/03 — Kaleci modelini entegre et")]
        public static void Integrate()
        {
            Directory.CreateDirectory(Art);
            var log=new StringBuilder();
            string modelPath="Assets/SuperGoalie/Meshes/Characters/Players/Player001/Character.fbx";
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if(model==null) throw new Exception("Character.fbx henüz import edilmedi.");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Art+"/Keeper.controller");
            if(controller==null) controller=AnimatorController.CreateAnimatorControllerAtPath(Art+"/Keeper.controller");
            var sm=controller.layers[0].stateMachine;
            foreach(var state in sm.states) sm.RemoveState(state.state);
            string[] states={"Idle","CentreLow","CentreMiddle","CentreHigh","LeftLow","LeftMiddle","LeftHigh","RightLow","RightMiddle","RightHigh"};
            string[] files={"IdleAlert","PunchDownCenter","PunchMiddleCentre","PunchHighCentre","PunchLowLeft","PunchMiddleLeft","PunchHighLeft","PunchLowLeft","PunchMiddleLeft","PunchHighLeft"};
            string[] names={"metarig|metarigAction","Save_Down","Punch Fall Back Start","PunchHighMiddleStart","Low Dive Left Start","Mid High Dive Left Start","High Dive Left Start","Low Dive Left Start","Mid High Dive Left Start","High Dive Left Start"};
            for(int i=0;i<states.Length;i++) {
                var source=AssetDatabase.LoadAllAssetsAtPath("Assets/SuperGoalie/Animations/GoalKeeperAnimations/"+files[i]+".fbx").OfType<AnimationClip>().FirstOrDefault(c=>c.name==names[i]);
                if(source==null)throw new Exception("Animation missing: "+files[i]);
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Art+"/"+states[i]+".anim");
                if(clip==null) { clip=UnityEngine.Object.Instantiate(source); clip.name=states[i]; AssetDatabase.CreateAsset(clip,Art+"/"+states[i]+".anim"); }
                EditorUtility.CopySerialized(source,clip); clip.name=states[i];
                AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
                var state=sm.AddState(states[i]); state.motion=clip; state.mirror=i>=7;
                if(i==0) sm.defaultState=state;
                log.AppendLine(states[i]+": "+source.name+" duration="+source.length+" humanoid="+source.humanMotion);
            }
            var original=SceneManager.GetActiveScene();
            // Preserve the generated baseline and any scene the user is editing.
            if(!File.Exists(Output)) File.Copy("Assets/Penalty/Scenes/Arena.unity",Output);
            AssetDatabase.ImportAsset(Output);
            var scene=original.path==Output?original:EditorSceneManager.OpenScene(Output,OpenSceneMode.Additive);
            var game=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PenaltyGame>(true)).Single();
            var keeper=game.keeper;
            foreach(var r in keeper.GetComponentsInChildren<Renderer>()) r.enabled=false;
            var existing=keeper.Find("GoalieVisual"); if(existing!=null)UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(model,scene); visual.name="GoalieVisual"; visual.transform.SetParent(keeper,false);
            var animator=visual.GetComponent<Animator>(); if(animator==null)animator=visual.AddComponent<Animator>();
            if(animator.avatar==null || !animator.avatar.isValid || !animator.avatar.isHuman)throw new Exception("Geçerli humanoid avatar yok.");
            animator.runtimeAnimatorController=controller; animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var renderers=visual.GetComponentsInChildren<Renderer>(); var bounds=renderers[0].bounds; foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            float factor=1.85f/bounds.size.y; visual.transform.localScale*=factor;
            bounds=renderers[0].bounds; foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            visual.transform.position+=new Vector3(keeper.position.x-bounds.center.x,keeper.position.y-bounds.min.y,keeper.position.z-bounds.center.z);
            foreach(var r in renderers) {
                var materials=r.sharedMaterials;
                for(int i=0;i<materials.Length;i++) {
                    var src=materials[i]; if(src==null)continue;
                    string name=src.name.Replace("/","_"); string dest=Art+"/"+name+"_URP.mat";
                    var mat=AssetDatabase.LoadAssetAtPath<Material>(dest);
                    if(mat==null) {
                        mat=new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.name=name+"_URP";
                        var tex=src.HasProperty("_MainTex")?src.GetTexture("_MainTex"):null;
                        mat.SetTexture("_BaseMap",tex); mat.SetColor("_BaseColor",Color.white); mat.SetFloat("_Smoothness",.22f); mat.enableInstancing=true;
                        if(r.name.ToLower().Contains("jearsy") || name.ToLower().Contains("kit"))mat.SetColor("_BaseColor",new Color(.12f,.65f,.64f));
                        AssetDatabase.CreateAsset(mat,dest);
                    }
                    materials[i]=mat; log.AppendLine(r.name+" material="+name+" texture="+(mat.mainTexture!=null?mat.mainTexture.name:"none"));
                }
                r.sharedMaterials=materials;
            }
            var presentation=visual.AddComponent<KeeperPresentation>(); presentation.game=game; presentation.animator=animator;
            keeper.name="Keeper_SuperGoalie";
            // Prevent missing shared material warnings on the imported ball; retain authoritative sphere collider.
            var ballPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SuperGoalie/Meshes/Ball/Meshes/Ball/soccer_ball.fbx");
            if(ballPrefab!=null && game.ball.transform.Find("BallVisual")==null) {
                var ballVisual=(GameObject)PrefabUtility.InstantiatePrefab(ballPrefab,scene); ballVisual.name="BallVisual"; ballVisual.transform.SetParent(game.ball.transform,false);
                var rs=ballVisual.GetComponentsInChildren<Renderer>(); if(rs.Length>0) {
                    var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                    ballVisual.transform.localScale*=.22f/Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));
                    b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);ballVisual.transform.position+=game.ball.position-b.center;
                    foreach(var r in rs) {
                        var bm=new Material(Shader.Find("Universal Render Pipeline/Lit")); bm.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SuperGoalie/Meshes/Ball/Textures/Ball/soccer texture.bmp");
                        string bp=Art+"/Ball_URP.mat";var old=AssetDatabase.LoadAssetAtPath<Material>(bp);if(old==null) {AssetDatabase.CreateAsset(bm,bp);old=bm;}else UnityEngine.Object.DestroyImmediate(bm);r.sharedMaterial=old;
                    }
                    game.ball.GetComponent<Renderer>().enabled=false;
                }
            }
            var fillObject=scene.GetRootGameObjects().FirstOrDefault(o=>o.name=="PlayerFrontFill");
            if(fillObject==null) { fillObject=new GameObject("PlayerFrontFill");SceneManager.MoveGameObjectToScene(fillObject,scene);fillObject.AddComponent<Light>(); }
            var fill=fillObject.GetComponent<Light>();fill.type=LightType.Directional;fill.intensity=1.6f;fill.color=new Color(.83f,.9f,1);fill.shadows=LightShadows.None;fill.transform.rotation=Quaternion.Euler(35,180,0);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            log.AppendLine("Avatar valid humanoid; scale factor="+factor);
            log.AppendLine("Scene="+Output+"; baseline preserved. Collision proxies unchanged; pose/hitbox alignment requires gameplay tuning.");
            File.WriteAllText("Docs/goalie-integration.txt",log.ToString());
            if(scene != original) {
                EditorSceneManager.CloseScene(scene,true);
                if(original.IsValid() && !original.isDirty) EditorSceneManager.OpenScene(Output,OpenSceneMode.Single);
                else if(original.IsValid()) SceneManager.SetActiveScene(original);
            }
            EditorApplication.delayCall += Preview;
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Output,true)};
            File.WriteAllText("Docs/goalie-integration.done","Imported, controller built, scene saved, humanoid validated.");
            Debug.Log("GOALIE_INTEGRATION_OK");
        }
        [MenuItem("Penalty Arena/04 — Kaleci görsel kontrolü")]
        public static void Preview()
        {
            if(SceneManager.sceneCount!=1 || SceneManager.GetActiveScene().path!=Output) return;
            var game=UnityEngine.Object.FindFirstObjectByType<PenaltyGame>();
            var animator=game.keeper.GetComponentInChildren<Animator>();
            var camera=game.gameCamera; var pos=camera.transform.position;var rot=camera.transform.rotation;
            var report=new StringBuilder();
            var rt=new RenderTexture(1280,720,24); var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try {
                camera.transform.position=new Vector3(2.9f,1.9f,5); camera.transform.LookAt(game.keeper.position+Vector3.up);
                foreach(string state in new[]{"Idle","LeftMiddle","RightMiddle"}) {
                    animator.Rebind(); animator.Play(state,0,.8f); animator.Update(0);
                    var hand=animator.GetBoneTransform(state=="RightMiddle"?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
                    report.AppendLine(state+" hand="+hand.position+" humanoid="+animator.isHuman);
                    camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                    texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
                    File.WriteAllBytes("Docs/goalie-"+state+".png",texture.EncodeToPNG());
                }
                File.WriteAllText("Docs/goalie-pose-check.txt",report.ToString());
            } finally {
                camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);
                camera.transform.SetPositionAndRotation(pos,rot); animator.Rebind();animator.Play("Idle",0,0);animator.Update(0);
            }
            Debug.Log("GOALIE_POSE_CHECK_OK");
            if(File.Exists("Temp/goalie-build.request")) {
                File.Delete("Temp/goalie-build.request");
                try { RuleChecks.Run(); ArenaBuilder.BuildAndroid(); File.WriteAllText("Docs/goalie-build.done","Android build with rigged goalie succeeded."); }
                catch(Exception e) { File.WriteAllText("Docs/goalie-build-error.txt",e.ToString()); Debug.LogException(e); }
            }
        }
    }
}
