using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace PenaltyArena.Editor
{
    public static partial class PremiumArena
    {
        public const string Art="Assets/Penalty/Art/Presentation";
        public const string Output="Assets/Penalty/Scenes/ArenaNight.unity";
        [MenuItem("Penalty Arena/08 — Gece arenasını oluştur")]
        public static void Build()
        {
            Directory.CreateDirectory(Art);AssetDatabase.Refresh();
            string working=Art+"/Working.unity";File.Copy(MotionAuthoring.Output,working,true);AssetDatabase.ImportAsset(working);
            var scene=EditorSceneManager.OpenScene(working,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var game=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PenaltyGame>()).Single();
            var root=new GameObject("NightArena");
            var pitch=scene.GetRootGameObjects().Single(o=>o.name=="FootballPitch");foreach(var r in pitch.GetComponentsInChildren<Renderer>())r.enabled=false;
            var stadium=scene.GetRootGameObjects().Single(o=>o.name=="KCISA_Stadium");
            // Retain the licensed steel roof structure; replace the blank indoor walls and sparse seating.
            foreach(var r in stadium.GetComponentsInChildren<Renderer>()) {
                if(!r.sharedMaterial.name.Contains("Steel_Structure_03_1")) {Object.DestroyImmediate(r.GetComponent<MeshFilter>());Object.DestroyImmediate(r);continue;}
                var m=Object.Instantiate(r.sharedMaterial);m.name="Night_"+r.sharedMaterial.name;m.SetColor("_BaseColor",new Color(.13f,.17f,.22f));m.SetFloat("_Smoothness",.5f);r.sharedMaterial=Save(m,m.name+".mat");
            }
            var scale=stadium.transform.localScale;scale.x*=1.6f;scale.z*=1.2f;stadium.transform.localScale=scale;
            var location=stadium.transform.position;location.z=(location.z-28)*1.6f+52.5f;stadium.transform.position=location;
            Pitch(root.transform);Bowl(root.transform);Goal(game,root.transform);Football(game);Players(game);Lights(game,root.transform);
            game.shooterCameraPosition=new Vector3(0,1.25f,15.3f);game.shooterCameraTarget=new Vector3(0,-.4f,0);
            game.keeperCameraPosition=new Vector3(0,1.24f,-3.6f);game.keeperCameraTarget=new Vector3(0,.55f,11);
            game.gameCamera.fieldOfView=38;game.gameCamera.transform.position=game.shooterCameraPosition;game.gameCamera.transform.LookAt(game.shooterCameraTarget);
            game.gameCamera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            game.gameCamera.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.FastApproximateAntialiasing;
            GraphicsSettings.useScriptableRenderPipelineBatching=true;
            game.keeperMotion.Evaluate(0,0,false,0,0);game.strikerMotion.Evaluate(0,0,false,0,0);
            for(int i=SceneManager.sceneCount-1;i>=0;i--){var s=SceneManager.GetSceneAt(i);if(s!=scene&&s.path==Output){if(s.isDirty)EditorSceneManager.SaveScene(s,Art+"/BeforeRebuild.unity",true);EditorSceneManager.CloseScene(s,true);}}
            AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene,Output))throw new Exception("Night scene save failed");
            EditorSceneManager.CloseScene(scene,true);EditorSceneManager.OpenScene(Output,OpenSceneMode.Single);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Output,true)};
            var all=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<MeshFilter>()).Where(m=>m.sharedMesh!=null&&m.GetComponent<Renderer>()!=null&&m.GetComponent<Renderer>().enabled).ToArray();
            File.WriteAllText("Docs/presentation-build.txt","Static triangles="+all.Sum(m=>(long)m.sharedMesh.triangles.Length/3)+" renderers="+all.Length+"\nKCISA steel retained; 24 spatial crowd chunks; three seating tiers; layered turf; nine bone-driven saves retained.\n");
            Debug.Log("PRESENTATION_BUILD_OK");
        }
        static void Lights(PenaltyGame game,Transform root)
        {
            var scene=game.gameObject.scene;
            foreach(var l in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Light>()).ToArray())Object.DestroyImmediate(l.gameObject);
            var panels=scene.GetRootGameObjects().FirstOrDefault(o=>o.name=="ArenaLightPanels");if(panels!=null)Object.DestroyImmediate(panels);
            Light Directional(string name,Vector3 rotation,Color color,float power,bool shadow) {
                var o=new GameObject(name);o.transform.SetParent(root,false);o.transform.rotation=Quaternion.Euler(rotation);var l=o.AddComponent<Light>();l.type=LightType.Directional;l.color=color;l.intensity=power;l.shadows=shadow?LightShadows.Soft:LightShadows.None;l.shadowBias=.025f;l.shadowNormalBias=.12f;return l;
            }
            RenderSettings.sun=Directional("FloodlightKey",new Vector3(52,155,0),new Color(.94f,.97f,1),1.8f,true);
            Directional("PitchFill",new Vector3(34,-25,0),new Color(.8f,.88f,1),1.1f,false);
            Directional("PlayerRim",new Vector3(25,12,0),new Color(1,.91f,.75f),.35f,false);
            RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.18f,.2f,.25f);RenderSettings.ambientEquatorColor=new Color(.08f,.1f,.14f);RenderSettings.ambientGroundColor=new Color(.045f,.055f,.035f);
            var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(new Color(.13f,.15f,.18f));RenderSettings.ambientProbe=sh;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.012f,.019f,.031f);RenderSettings.fogDensity=.0085f;
            game.gameCamera.backgroundColor=new Color(.003f,.006f,.015f);game.gameCamera.farClipPlane=220;game.gameCamera.allowHDR=true;
            var housings=new Geometry();var emitters=new Geometry();var halos=new Geometry();
            foreach(float z in new[]{-26f,120f})foreach(float x in new[]{-29f,-14f,14f,29f}) {
                var pos=new Vector3(x,z<0?10.3f:12.5f,z);var q=Quaternion.LookRotation(new Vector3(0,0,z<0?10:90)-pos);
                housings.Box(pos,new Vector3(4.3f,1.4f,.35f),Color.white,q);
                for(int row=0;row<2;row++)for(int column=0;column<8;column++) {
                    var p=pos+q*new Vector3((column-3.5f)*.49f,(row-.5f)*.62f,.2f);
                    emitters.Box(p,new Vector3(.28f,.32f,.05f),Color.white,q);
                }
                var center=pos+q*new Vector3(0,0,.26f);var u=q*Vector3.right*3.3f;var v=q*Vector3.up*1.8f;
                halos.Quad(center-u-v,center-u+v,center+u+v,center+u-v,new Color(1,1,1,.8f));
            }
            var housing=Mat("ProjectorHousing",new Color(.03f,.04f,.055f),.5f);var white=Mat("ProjectorEmitter",Color.white);white.SetColor("_Emission",new Color(4,4.5f,5));
            var halo=Mat("ProjectorHalo",new Color(.65f,.8f,1,.3f),.1f,"Penalty/Stadium Glow");
            MeshObject("ProjectorBanks",housings,housing,root);MeshObject("ProjectorLEDs",emitters,white,root);MeshObject("ProjectorGlow",halos,halo,root);
            foreach(var v in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Volume>()))v.enabled=false;
            var volume=new GameObject("NightGrade").AddComponent<Volume>();volume.transform.SetParent(root,false);volume.isGlobal=true;volume.priority=10;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();var bloom=profile.Add<Bloom>(true);bloom.intensity.value=.12f;bloom.threshold.value=1.2f;bloom.scatter.value=.6f;
            var tone=profile.Add<Tonemapping>(true);tone.mode.value=TonemappingMode.ACES;
            var grade=profile.Add<ColorAdjustments>(true);grade.postExposure.value=.2f;grade.contrast.value=13;grade.saturation.value=-5;
            var vignette=profile.Add<Vignette>(true);vignette.intensity.value=.18f;vignette.smoothness.value=.5f;
            volume.sharedProfile=Save(profile,"NightGrade.asset");
            foreach(var component in volume.sharedProfile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,volume.sharedProfile);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            pipeline.msaaSampleCount=4;pipeline.shadowDistance=30;pipeline.shadowCascadeCount=2;pipeline.mainLightShadowmapResolution=2048;
            var serialized=new SerializedObject(pipeline);serialized.FindProperty("m_SoftShadowsSupported").boolValue=true;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
            int quality=QualitySettings.GetQualityLevel();for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.skinWeights=SkinWeights.FourBones;QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;}QualitySettings.SetQualityLevel(quality,false);
        }
        public static void Reimport()
        {
            AssetDatabase.SaveAssets();
            foreach(string guid in AssetDatabase.FindAssets("",new[]{Art})) {string path=AssetDatabase.GUIDToAssetPath(guid);if(!path.EndsWith(".unity"))AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);}
        }
        public static void Inspect()
        {
            Directory.CreateDirectory(Art);
            var scene=EditorSceneManager.OpenPreviewScene(MotionAuthoring.Output);
            var game=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PenaltyGame>()).Single();
            var report=new StringBuilder();
            try {
                var rig=game.strikerMotion;
                foreach(var r in rig.visual.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                    var m=r.sharedMesh;var ps=m.vertices.Select(v=>rig.visual.InverseTransformPoint(r.transform.TransformPoint(v))).ToArray();
                    var b=new Bounds(ps[0],Vector3.zero);foreach(var p in ps)b.Encapsulate(p);
                    report.AppendLine(r.name+" verts="+m.vertexCount+" bounds="+b+" bones="+string.Join(",",r.bones.Select(t=>t.name)));
                    if(r.name=="body"||r.name=="gloaves")foreach(float x in new[]{.54f,.57f,.59f,.61f,.63f,.65f,.67f,.69f,.72f,.75f}) {
                        var ring=ps.Where(p=>Mathf.Abs(p.x-x)<.011f&&p.y>.95f).ToArray();if(ring.Length==0)continue;
                        report.AppendLine($"WRIST {r.name} x={x:F3} y={ring.Min(p=>p.y):F4}..{ring.Max(p=>p.y):F4} z={ring.Min(p=>p.z):F4}..{ring.Max(p=>p.z):F4} count={ring.Length}");
                    }
                    if(r.name=="kit") {
                        var uv=new Texture2D(1024,1024);var pixels=Enumerable.Repeat(Color.black,1024*1024).ToArray();
                        for(int i=0;i<m.vertexCount;i++) {
                            var p=ps[i];var t=m.uv[i];int x=Mathf.Clamp((int)(t.x*1023),0,1023),y=Mathf.Clamp((int)(t.y*1023),0,1023);
                            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)if(x+dx>=0&&x+dx<1024&&y+dy>=0&&y+dy<1024)pixels[(y+dy)*1024+x+dx]=new Color(p.y/2,p.z>0?1:0,Mathf.Abs(p.x));
                        }
                        uv.SetPixels(pixels);uv.Apply();File.WriteAllBytes("Docs/presentation-kit-uv.png",uv.EncodeToPNG());Object.DestroyImmediate(uv);
                    }
                }
                rig.Evaluate(0,0,false,0,0);
                foreach(var bone in new[]{HumanBodyBones.Head,HumanBodyBones.Neck,HumanBodyBones.Chest,HumanBodyBones.LeftHand}) {
                    var t=rig.visual.GetComponent<Animator>().GetBoneTransform(bone);
                    report.AppendLine(bone+" visual="+rig.visual.InverseTransformPoint(t.position)+" rot="+t.rotation.eulerAngles+" scale="+t.lossyScale);
                }
                var stadium=scene.GetRootGameObjects().Single(o=>o.name=="KCISA_Stadium");
                foreach(var r in stadium.GetComponentsInChildren<Renderer>())report.AppendLine("STADIUM "+r.name+" "+r.sharedMaterial.name+" "+r.bounds);
                File.WriteAllText("Docs/presentation-inspect.txt",report.ToString());
            } finally {EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
