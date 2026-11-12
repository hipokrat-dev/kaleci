using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace PenaltyArena.Editor
{
    public static class MotionChecks
    {
        static int checks;
        static void Require(bool ok,string label) {if(!ok)throw new Exception("MOTION: "+label);checks++;}
        static PenaltyGame Open(out Scene scene)
        {
            string working=MotionAuthoring.Art+"/Checks.unity";
            File.Copy(MotionAuthoring.Output,working,true);AssetDatabase.ImportAsset(working);
            scene=EditorSceneManager.OpenScene(working,OpenSceneMode.Additive);
            return scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PenaltyGame>()).Single();
        }
        public static void Run()
        {
            checks=0;var original=SceneManager.GetActiveScene();var game=Open(out var scene);var report=new StringBuilder();
            try {
                var k=game.keeperMotion;var s=game.strikerMotion;
                Require(k!=null&&s!=null,"Both rigs present");
                Require(!k.visual.GetComponent<Animator>().enabled&&!s.visual.GetComponent<Animator>().enabled,"No competing Animator");
                s.Evaluate(0,0,true,MotionTiming.StrikeContact,0);
                var rear=game.ball.position+Vector3.forward*.11f;
                report.AppendLine("Shot toe="+s.rightToes.position.ToString("F4")+" rear="+rear.ToString("F4"));
                Require(Vector3.Distance(s.rightToes.position,rear)<.16f,"Foot contact misses ball: "+Vector3.Distance(s.rightToes.position,rear));
                Require(s.leftFoot.position.y>.03f&&s.leftFoot.position.y<.20f,"Support foot on pitch");
                var mesh=new Mesh();float minY=float.MaxValue;
                foreach(var skin in s.visual.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                    skin.BakeMesh(mesh,true);foreach(var vertex in mesh.vertices)minY=Mathf.Min(minY,skin.transform.TransformPoint(vertex).y);
                }
                Object.DestroyImmediate(mesh);report.AppendLine("Shoe sole minimum world height="+minY.ToString("F4"));
                s.Evaluate(0,0,true,MotionTiming.StrikeEnd+.01f,0);
                Require(Vector3.Distance(s.actorRoot.position,s.home)<.001f,"Striker returns to start");
                for(int code=1;code<=9;code++) {
                    k.Evaluate(code,game.diveDuration,false,0,0);k.hitboxes.SetActive(true);k.hitboxes.Sync();
                    var hand=code>=7?k.rightHand:k.leftHand;
                    report.AppendLine("Save "+code+" hand="+hand.position.ToString("F3")+" hips="+k.hips.position.ToString("F3"));
                    if(code>=4)Require(Mathf.Abs(Mathf.Abs(hand.position.x)-game.diveReach)<.03f,"Lateral reach "+code);
                    for(int i=0;i<k.hitboxes.spheres.Length;i++)Require(Vector3.Distance(k.hitboxes.spheres[i].transform.position,k.hitboxes.sphereBones[i].position)<.001f,"Glove/head follows bone "+code);
                    for(float t=0;t<MotionTiming.SaveLength(game.diveDuration);t+=.05f) {
                        k.Evaluate(code,t,false,0,0);
                        Require(k.bones.All(b=>!float.IsNaN(b.position.x)&&!float.IsNaN(b.position.y)),"Finite pose "+code);
                        Require(Mathf.Min(k.leftHand.position.y,k.rightHand.position.y,k.leftFoot.position.y,k.rightFoot.position.y)>.025f,"Ground penetration "+code+" at "+t);
                    }
                    k.Evaluate(code,MotionTiming.SaveLength(game.diveDuration)+.01f,false,0,0);
                    Require(Vector3.Distance(k.actorRoot.position,k.home)<.001f,"Keeper recovers "+code);
                }
                report.AppendLine(checks+" assertions passed; full poses, foot contact, recovery (physics checked in the development player).");
                File.WriteAllText("Docs/motion-checks.txt",report.ToString());Debug.Log("MOTION_CHECKS_OK "+checks);
            } catch {File.WriteAllText("Docs/motion-checks.txt",report.ToString());throw;}
            finally {EditorSceneManager.CloseScene(scene,true);if(original.IsValid())SceneManager.SetActiveScene(original);}
        }
        public static void Preview()
        {
            var original=SceneManager.GetActiveScene();var game=Open(out var scene);SceneManager.SetActiveScene(scene);
            var camera=game.gameCamera;var previous=RenderTexture.active;
            var rt=new RenderTexture(960,540,24);var tex=new Texture2D(960,540,TextureFormat.RGB24,false);
            var hidden=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene).SelectMany(s=>s.GetRootGameObjects()).Where(o=>o.activeSelf).ToArray();
            foreach(var o in hidden)o.SetActive(false);
            var skins=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<SkinnedMeshRenderer>()).Where(r=>r.enabled).ToArray();
            var baked=new Mesh[skins.Length];var replacements=new GameObject[skins.Length];
            for(int i=0;i<skins.Length;i++) {
                var o=new GameObject("PreviewSkin");o.transform.SetParent(skins[i].transform,false);
                baked[i]=new Mesh();o.AddComponent<MeshFilter>().sharedMesh=baked[i];o.AddComponent<MeshRenderer>().sharedMaterials=skins[i].sharedMaterials;
                replacements[i]=o;skins[i].enabled=false;
            }
            void Render() {for(int i=0;i<skins.Length;i++)skins[i].BakeMesh(baked[i],true);camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();}
            Directory.CreateDirectory("Docs/motion-frames");
            try {
                camera.targetTexture=rt;camera.aspect=16f/9;camera.fieldOfView=45;
                for(int shot=0;shot<2;shot++)for(int i=0;i<105;i++) {
                    float t=i/30f;
                    if(shot==0) {
                        camera.transform.position=new Vector3(-3.4f,1.75f,14.3f);camera.transform.LookAt(new Vector3(0,.8f,11));
                        game.strikerMotion.Evaluate(0,0,true,t,0);game.keeperMotion.Evaluate(0,0,false,0,0);
                        float fly=Mathf.Max(0,t-MotionTiming.StrikeContact);
                        game.ball.transform.position=new Vector3(0,.115f,11)+new Vector3(1.8f,3.6f,-17)*fly+Physics.gravity*fly*fly*.5f;
                    } else {
                        camera.transform.position=new Vector3(0,2.2f,7.5f);camera.transform.LookAt(new Vector3(-.5f,1,0));
                        game.keeperMotion.Evaluate(5,t,false,0,0);game.strikerMotion.Evaluate(0,0,false,0,0);
                        game.ball.transform.position=new Vector3(0,-5,0);
                    }
                    Render();
                    File.WriteAllBytes($"Docs/motion-frames/{shot}-{i:000}.png",tex.EncodeToPNG());
                }
                // All nine reach poses, from the gameplay side of the goal.
                for(int code=1;code<=9;code++) {
                    game.keeperMotion.Evaluate(code,game.diveDuration,false,0,0);
                    Render();
                    File.WriteAllBytes($"Docs/motion-save-{code}.png",tex.EncodeToPNG());
                }
            } finally {camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);foreach(var m in baked)Object.DestroyImmediate(m);EditorSceneManager.CloseScene(scene,true);foreach(var o in hidden)if(o!=null)o.SetActive(true);if(original.IsValid())SceneManager.SetActiveScene(original);}
            Debug.Log("MOTION_PREVIEW_OK");
        }
    }
}
