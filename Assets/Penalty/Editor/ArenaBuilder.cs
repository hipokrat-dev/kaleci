using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace PenaltyArena.Editor
{
    public static class ArenaBuilder
    {
        static Material grass, alternate, white, dark, teal, gold, concrete;
        static Material Mat(string name, Color color, float smooth = .3f)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.name=name; m.color=color;
            m.SetFloat("_Smoothness",smooth); m.enableInstancing=true;
            string path="Assets/Penalty/Materials/"+name+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(existing!=null) { EditorUtility.CopySerialized(m,existing); Object.DestroyImmediate(m); return existing; }
            AssetDatabase.CreateAsset(m,path); return m;
        }
        static GameObject Shape(string name, PrimitiveType type, Vector3 pos, Vector3 size, Material mat, Transform parent=null, bool collision=true)
        {
            var o=GameObject.CreatePrimitive(type); o.name=name; o.transform.SetParent(parent,false); o.transform.localPosition=pos; o.transform.localScale=size;
            o.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collision) Object.DestroyImmediate(o.GetComponent<Collider>());
            return o;
        }
        static void Bar(string name,Vector3 a,Vector3 b,float radius,Material mat,Transform parent=null,bool collision=true)
        {
            var o=Shape(name,PrimitiveType.Cylinder,(a+b)/2,new Vector3(radius*2,Vector3.Distance(a,b)/2,radius*2),mat,parent,collision);
            o.transform.up=(b-a).normalized;
        }
        static Transform Player(string name,Vector3 position,Material shirt)
        {
            var root=new GameObject(name).transform; root.position=position;
            Shape("Torso_PROVISIONAL",PrimitiveType.Capsule,new Vector3(0,1.12f,0),new Vector3(.46f,.42f,.3f),shirt,root);
            Shape("Head_PROVISIONAL",PrimitiveType.Sphere,new Vector3(0,1.76f,0),Vector3.one*.26f,concrete,root);
            foreach(float s in new[]{-1f,1f}) {
                Bar("Leg_PROVISIONAL",new Vector3(s*.13f,.85f,0),new Vector3(s*.24f,.15f,.03f),.095f,dark,root);
                Bar("Arm_PROVISIONAL",new Vector3(s*.23f,1.4f,0),new Vector3(s*.5f,.9f,.05f),.075f,shirt,root);
                Shape("Glove_PROVISIONAL",PrimitiveType.Sphere,new Vector3(s*.51f,.88f,.05f),Vector3.one*.18f,white,root);
            }
            return root;
        }
        [MenuItem("Penalty Arena/01 — Sahneyi yeniden oluştur")]
        public static void Create()
        {
            Directory.CreateDirectory("Assets/Penalty/Materials"); Directory.CreateDirectory("Assets/Penalty/Scenes");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            grass=Mat("Grass",new Color(.065f,.19f,.07f),.08f); alternate=Mat("GrassStripe",new Color(.09f,.24f,.085f),.1f);
            white=Mat("Ivory",new Color(.88f,.9f,.85f)); dark=Mat("Anthracite",new Color(.035f,.045f,.055f));
            teal=Mat("KeeperTeal",new Color(.025f,.34f,.35f)); gold=Mat("Champagne",new Color(.68f,.57f,.34f)); concrete=Mat("Concrete",new Color(.3f,.32f,.34f));
            var arena=new GameObject("Arena_ReplaceWithKCISA").transform;
            Shape("PitchCollider",PrimitiveType.Cube,new Vector3(0,-.16f,12),new Vector3(48,.3f,42),grass,arena);
            for(int i=0;i<14;i++) Shape("MowingStripe",PrimitiveType.Cube,new Vector3(0,-.005f,-8.5f+i*3),new Vector3(48,.01f,3),i%2==0?grass:alternate,arena,false);
            // Regulation penalty geometry, reduced surrounding training arena.
            Shape("GoalLine",PrimitiveType.Cube,new Vector3(0,.005f,0),new Vector3(40,.015f,.1f),white,arena,false);
            Shape("PenaltySpot",PrimitiveType.Cylinder,new Vector3(0,.013f,11),new Vector3(.22f,.006f,.22f),white,arena,false);
            foreach(float x in new[]{-20.16f,20.16f}) Shape("PenaltyAreaSide",PrimitiveType.Cube,new Vector3(x,.008f,8.25f),new Vector3(.1f,.015f,16.5f),white,arena,false);
            Shape("PenaltyAreaFront",PrimitiveType.Cube,new Vector3(0,.008f,16.5f),new Vector3(40.32f,.015f,.1f),white,arena,false);
            foreach(float x in new[]{-9.16f,9.16f}) Shape("GoalAreaSide",PrimitiveType.Cube,new Vector3(x,.008f,2.75f),new Vector3(.1f,.015f,5.5f),white,arena,false);
            Shape("GoalAreaFront",PrimitiveType.Cube,new Vector3(0,.008f,5.5f),new Vector3(18.32f,.015f,.1f),white,arena,false);
            var goal=new GameObject("Goal_7.32x2.44").transform;
            foreach(float x in new[]{-3.72f,3.72f}) Bar("Post",new Vector3(x,0,0),new Vector3(x,2.5f,0),.06f,white,goal);
            Bar("Crossbar",new Vector3(-3.72f,2.5f,0),new Vector3(3.72f,2.5f,0),.06f,white,goal);
            for(float x=-3.66f;x<=3.67f;x+=.22f) Bar("NetBackVertical",new Vector3(x,0,-1.8f),new Vector3(x,2.44f,-1.8f),.007f,white,goal,false);
            for(float y=.05f;y<=2.44f;y+=.2f) Bar("NetBackHorizontal",new Vector3(-3.66f,y,-1.8f),new Vector3(3.66f,y,-1.8f),.007f,white,goal,false);
            for(float z=-1.8f;z<=0;z+=.22f) {
                Bar("NetRoof",new Vector3(-3.66f,2.44f,z),new Vector3(3.66f,2.44f,z),.007f,white,goal,false);
                foreach(float x in new[]{-3.66f,3.66f}) Bar("NetSide",new Vector3(x,0,z),new Vector3(x,2.44f,z),.007f,white,goal,false);
            }
            // Invisible back net stops the ball after an authoritative goal has been detected.
            var back=new GameObject("NetCollision"); back.transform.position=new Vector3(0,1.22f,-1.85f); back.AddComponent<BoxCollider>().size=new Vector3(7.5f,2.6f,.05f);
            for(int tier=0;tier<9;tier++) {
                Shape("RearTerrace",PrimitiveType.Cube,new Vector3(0,tier*.55f,-8-tier*1.1f),new Vector3(64,.5f,1.1f),tier%2==0?dark:concrete,arena);
                foreach(float s in new[]{-1f,1f}) Shape("SideTerrace",PrimitiveType.Cube,new Vector3(s*(26+tier*1.1f),tier*.55f,10),new Vector3(1.1f,.5f,36),tier%2==0?dark:concrete,arena);
            }
            foreach(float x in new[]{-22f,22f}) foreach(float z in new[]{-5f,25f}) {
                Bar("FloodlightMast",new Vector3(x,0,z),new Vector3(x,15,z),.17f,concrete,arena);
                Shape("FloodlightPanel",PrimitiveType.Cube,new Vector3(x,15,z),new Vector3(3,1.2f,.25f),white,arena,false);
                var l=new GameObject("Floodlight").AddComponent<Light>(); l.type=LightType.Spot; l.range=65; l.spotAngle=95; l.intensity=25;
                l.transform.position=new Vector3(x,14.5f,z); l.transform.LookAt(new Vector3(0,0,8)); l.shadows=LightShadows.None;
            }
            var sun=new GameObject("ArenaSoftKey").AddComponent<Light>(); sun.type=LightType.Directional; sun.color=new Color(.72f,.81f,1); sun.intensity=1.1f; sun.shadows=LightShadows.Soft; sun.transform.rotation=Quaternion.Euler(48,-25,0);
            RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.2f,.23f,.3f); RenderSettings.skybox=null;
            var camera=new GameObject("Main Camera").AddComponent<Camera>(); camera.tag="MainCamera"; camera.transform.position=new Vector3(-1.7f,2.4f,15.5f); camera.transform.LookAt(new Vector3(0,1.1f,0)); camera.fieldOfView=52; camera.backgroundColor=new Color(.015f,.022f,.04f); camera.clearFlags=CameraClearFlags.SolidColor; camera.gameObject.AddComponent<AudioListener>(); camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            var keeper=Player("Keeper_REPLACE_RIG",new Vector3(0,0,.45f),teal);
            var striker=Player("Striker_REPLACE_RIG",new Vector3(-.65f,0,12.1f),white);
            var ball=Shape("Ball",PrimitiveType.Sphere,new Vector3(0,.115f,11),Vector3.one*.22f,white);
            var rb=ball.AddComponent<Rigidbody>(); rb.mass=.43f; rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic; rb.interpolation=RigidbodyInterpolation.Interpolate;
            var physics=new PhysicsMaterial("Football") { bounciness=.55f,dynamicFriction=.45f,staticFriction=.45f,bounceCombine=PhysicsMaterialCombine.Maximum }; ball.GetComponent<Collider>().material=physics;
            ball.AddComponent<BallContact>();
            var root=new GameObject("PenaltyArena"); var room=root.AddComponent<RoomConnection>(); var game=root.AddComponent<PenaltyGame>();
            game.room=room; game.ball=rb; game.keeper=keeper; game.striker=striker; game.gameCamera=camera; root.AddComponent<ArenaUI>().game=game;
            PlayerSettings.companyName="Family Arena"; PlayerSettings.productName="Penaltı Arena";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.familyarena.penalty");
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft; PlayerSettings.allowedAutorotateToPortrait=false; PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP); PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            var mobile=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if(mobile!=null) { GraphicsSettings.defaultRenderPipeline=mobile; for(int i=0;i<QualitySettings.names.Length;i++) { QualitySettings.SetQualityLevel(i); QualitySettings.renderPipeline=mobile; } }
            QualitySettings.vSyncCount=0; Application.targetFrameRate=60;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Penalty/Scenes/Arena.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Penalty/Scenes/Arena.unity",true)};
            AssetDatabase.SaveAssets(); Debug.Log("PENALTY_SCENE_CREATED");
        }
        static string BuildScene()
        {
            if(File.Exists(StadiumIntegration.Output) && File.Exists("Assets/HandballStadium/PenaltyLayout.json"))return StadiumIntegration.Output;
            if(File.Exists("Assets/Penalty/Scenes/ArenaWithGoalie.unity") && File.Exists("Assets/SuperGoalie/Meshes/Characters/Players/Player001/Character.fbx"))return "Assets/Penalty/Scenes/ArenaWithGoalie.unity";
            return "Assets/Penalty/Scenes/Arena.unity";
        }
        [MenuItem("Penalty Arena/02 — Android APK oluştur")]
        public static void BuildAndroid()
        {
            Directory.CreateDirectory("Builds");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{BuildScene()},locationPathName="Builds/PenaltiArena-dev.apk",target=BuildTarget.Android,options=BuildOptions.Development });
            if(result.summary.result!=BuildResult.Succeeded) throw new System.Exception("Android build: "+result.summary.result);
        }
        public static void BuildMac()
        {
            Directory.CreateDirectory("Builds");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes=new[]{BuildScene()},locationPathName="Builds/PenaltiArena.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development });
            if(result.summary.result!=BuildResult.Succeeded) throw new System.Exception("Mac build: "+result.summary.result);
        }
    }
}
