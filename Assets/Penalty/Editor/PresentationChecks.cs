using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PenaltyArena.Editor
{
    public static class PresentationChecks
    {
        [MenuItem("Penalty Arena/09 — Gece sahnesini doğrula")]
        public static void Run()
        {
            int checks=0;
            void Require(bool valid,string label) { if(!valid)throw new Exception("PRESENTATION: "+label);checks++; }
            var scene=EditorSceneManager.OpenPreviewScene(PremiumArena.Output);
            try {
                var roots=scene.GetRootGameObjects();
                var game=roots.SelectMany(o=>o.GetComponentsInChildren<PenaltyGame>()).Single();
                foreach(string name in new[]{"Penalty/Stadium Surface","Penalty/Stadium Glow","Penalty/Stadium Crowd"}) {
                    var shader=Shader.Find(name);Require(shader!=null&&shader.isSupported,"Supported shader "+name);
                    Require(!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Shader compilation "+name);
                }
                var renderers=roots.SelectMany(o=>o.GetComponentsInChildren<Renderer>()).Where(r=>r.enabled).ToArray();
                Require(renderers.All(r=>r.sharedMaterials.All(m=>m!=null&&m.shader!=null)),"No missing materials");
                var camera=game.gameCamera;
                Require(camera.GetUniversalAdditionalCameraData().renderPostProcessing,"Camera post processing");
                var profile=roots.SelectMany(o=>o.GetComponentsInChildren<Volume>()).Single(v=>v.enabled).sharedProfile;
                Require(profile!=null&&profile.components.Count==4&&profile.components.All(c=>c!=null),"Persisted grading profile");
                Require(profile.TryGet<Tonemapping>(out var tone)&&tone.mode.value==TonemappingMode.ACES,"ACES grading");
                Require(profile.TryGet<Bloom>(out var bloom)&&bloom.intensity.value>0,"Bloom enabled");
                int colliders=roots.SelectMany(o=>o.GetComponentsInChildren<Collider>()).Count(c=>c.enabled);
                game.SetGoalNetVisible(false);Require(game.goalNet.All(r=>!r.enabled),"Keeper net visibility");
                Require(colliders==roots.SelectMany(o=>o.GetComponentsInChildren<Collider>()).Count(c=>c.enabled),"Net visibility preserves collisions");
                foreach(bool keeper in new[]{false,true}) {
                    camera.transform.position=keeper?game.keeperCameraPosition:game.shooterCameraPosition;
                    camera.transform.LookAt(keeper?game.keeperCameraTarget:game.shooterCameraTarget);
                    float sign=game.ScreenDirectionToWorld(1);
                    var a=camera.WorldToViewportPoint(new Vector3(0,1,0));var b=camera.WorldToViewportPoint(new Vector3(sign,1,0));
                    Require(b.x>a.x,"Screen-right swipe matches role camera");
                    if(!keeper){var ball=camera.WorldToViewportPoint(game.ball.position);Require(ball.z>0&&ball.y>.16f&&ball.y<.83f,"Ball remains inside gesture area");}
                }
                var s=game.strikerMotion;var k=game.keeperMotion;
                s.Evaluate(0,0,true,MotionTiming.StrikeContact,0);
                Require(Vector3.Distance(s.rightToes.position,game.ball.position+Vector3.forward*.11f)<.16f,"Shot contact retained");
                Require(k.hitboxes.segments.Length==9&&k.hitboxes.spheres.Length==3,"Bone collider layout retained");
                foreach(var rig in new[]{s,k}) {
                    foreach(var skin in rig.visual.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                        Require(skin.sharedMesh.bindposes.Length==skin.bones.Length&&skin.bones.All(b=>b!=null),"Valid skin binding "+skin.name);
                    }
                    var hair=rig.visual.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name.EndsWith("TexturedHair"));
                    Require(hair.sharedMesh.vertexCount>0&&hair.sharedMesh.triangles.Length>0,"Hair mesh bound to actor");
                }
                for(int code=1;code<=9;code++) {
                    k.Evaluate(code,game.diveDuration,false,0,0);k.hitboxes.Sync();
                    var hand=code>=7?k.rightHand:k.leftHand;
                    if(code>=4)Require(Mathf.Abs(Mathf.Abs(hand.position.x)-game.diveReach)<.03f,"Dive reach "+code);
                    for(int i=0;i<3;i++)Require(Vector3.Distance(k.hitboxes.spheres[i].transform.position,k.hitboxes.sphereBones[i].position)<.001f,"Hand/head collision alignment "+code);
                }
                long triangles=renderers.OfType<MeshRenderer>().Sum(r=>(long)r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3);
                Require(triangles<500000,"Static triangle budget");
                File.WriteAllText("Docs/presentation-checks.txt",$"PRESENTATION_CHECKS_OK {checks}\nStatic triangles: {triangles}; renderers including skins: {renderers.Length}\nShader support, persisted grading, materials, camera controls, contact, all nine saves, skin binding and collision alignment passed.\n");
                Debug.Log("PRESENTATION_CHECKS_OK "+checks);
            } finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
