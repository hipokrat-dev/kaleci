using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PenaltyArena.Editor
{
    // Uses only the local licensed package. Generated scenes/art are intentionally Git-ignored.
    public static class StadiumIntegration
    {
        const string Art = "Assets/Penalty/Art/Stadium";
        public const string Output = "Assets/Penalty/Scenes/ArenaStadium.unity";
        const string Request = "Temp/stadium-integration.request";
        [Serializable] class Layout { public Entry[] entries; }
        [Serializable] class Entry {
            public string name, model; public int category; public string[] materials;
            public Vector3 position, scale; public Quaternion rotation;
        }
        [InitializeOnLoadMethod] static void Init() { EditorApplication.delayCall += Pending; }
        static void Pending()
        {
            if(Application.isBatchMode)return;
            if (!File.Exists(Request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying) { EditorApplication.delayCall += Pending; return; }
            File.Delete(Request);
            try { Integrate(); } catch(Exception e) { File.WriteAllText("Docs/stadium-error.txt",e.ToString()); Debug.LogException(e); }
        }
        public static void IntegrateAndPreview() { Integrate(); Preview(); }
        static Bounds BoundsOf(GameObject o) {
            var rs=o.GetComponentsInChildren<Renderer>();
            var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;
        }
        static Material Material(string name, Color color) {
            string path=Art+"/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null) {m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Smoothness",.18f);m.enableInstancing=true;return m;
        }
        static Material Convert(string path, Dictionary<string,Material> converted) {
            if(converted.TryGetValue(path,out var ready))return ready;
            var source=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(source==null)throw new Exception("Stadium material missing: "+path);
            var m=Material(Path.GetFileNameWithoutExtension(path)+"_URP",new Color(.38f,.43f,.5f));
            if(source.HasProperty("_MainTex"))m.mainTexture=source.GetTexture("_MainTex");
            m.SetFloat("_Cull",2);converted[path]=m;return m;
        }
        [MenuItem("Penalty Arena/05 — KCISA stadyumunu entegre et")]
        public static void Integrate()
        {
            if(!File.Exists("Assets/HandballStadium/PenaltyLayout.json"))throw new Exception("Önce Tools/import_kcisa.py çalıştırılmalı.");
            if(SceneManager.GetActiveScene().isDirty)throw new Exception("Açık sahnede kaydedilmemiş değişiklikler var. Önce sahneyi kaydet.");
            Directory.CreateDirectory(Art);
            var layout=JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/HandballStadium/PenaltyLayout.json"));
            var report=new StringBuilder();
            // Import at mobile limits before any textures are referenced in the scene.
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/HandballStadium"})) {
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if(importer.maxTextureSize!=1024 || !importer.mipmapEnabled) {
                    importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.isReadable=false;
                    importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
                }
            }
            string sourceScene=File.Exists("Assets/Penalty/Scenes/ArenaWithGoalie.unity")?"Assets/Penalty/Scenes/ArenaWithGoalie.unity":"Assets/Penalty/Scenes/Arena.unity";
            // Create the derived scene from a baseline every time; never rewrite source scenes.
            var scene=EditorSceneManager.OpenScene(sourceScene,OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene,Output);
            var game=Object.FindFirstObjectByType<PenaltyGame>();
            var placeholder=scene.GetRootGameObjects().First(o=>o.name=="Arena_ReplaceWithKCISA");
            foreach(var t in placeholder.GetComponentsInChildren<Transform>().ToArray())
                if(t!=placeholder.transform && (t.name.Contains("Terrace") || t.name.StartsWith("Floodlight")))Object.DestroyImmediate(t.gameObject);
            placeholder.name="FootballPitch";
            foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.name=="Floodlight")Object.DestroyImmediate(l.gameObject);
            var root=new GameObject("KCISA_Stadium");
            var converted=new Dictionary<string,Material>();
            var chairTemplates=new Dictionary<string,GameObject>();
            Bounds field=new Bounds();int count=0,chairs=0;
            var seatMaterial=Material("SeatSlate",new Color(.12f,.25f,.32f));
            foreach(var entry in layout.entries) {
                // Open the roof for the requested night arena; remove indoor advertising fixtures.
                if(entry.category==142 || entry.category==184 || entry.category==136)continue;
                bool chair=entry.category>=69 && entry.category<=73;
                GameObject instance;
                if(chair && chairTemplates.TryGetValue(entry.model,out var template)) instance=Object.Instantiate(template);
                else {
                    var model=AssetDatabase.LoadAssetAtPath<GameObject>(entry.model);
                    if(model==null)throw new Exception("Missing stadium model: "+entry.model);
                    instance=Object.Instantiate(model);instance.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                    if(chair) {
                        var b=BoundsOf(instance); Object.DestroyImmediate(instance);
                        instance=CreateSeat(b,seatMaterial);chairTemplates[entry.model]=instance;
                    } else {
                        foreach(var r in instance.GetComponentsInChildren<Renderer>()) {
                            r.sharedMaterials=entry.materials.Select(p=>Convert(p,converted)).ToArray();
                            r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
                        }
                    }
                }
                instance.name=entry.name;instance.transform.SetParent(root.transform,false);
                instance.transform.localPosition=entry.position;instance.transform.localRotation=entry.rotation;instance.transform.localScale=entry.scale;
                foreach(var c in instance.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
                if(entry.category==141) {field=BoundsOf(instance);Object.DestroyImmediate(instance);continue;}
                if(chair)chairs++;count++;
            }
            report.AppendLine("Source field bounds: "+field);
            // Rotate only the environment when the source court's long axis is X.
            if(field.size.x>field.size.z)root.transform.rotation=Quaternion.Euler(0,90,0);
            var center=root.transform.TransformPoint(field.center);
            float width=Mathf.Min(field.size.x,field.size.z), length=Mathf.Max(field.size.x,field.size.z);
            if(width<10 || length<20)throw new Exception("Unexpected source court bounds: "+field);
            // Wider penalty training pitch inside the original bowl; gameplay stays in metres.
            root.transform.localScale=new Vector3(54/width,1,68/length);
            if(field.size.x>field.size.z)root.transform.localScale=new Vector3(68/length,1,54/width);
            center=root.transform.TransformPoint(field.center);
            root.transform.position=new Vector3(-center.x,-field.max.y,28-center.z);
            report.AppendLine("Environment scale: "+root.transform.localScale+" offset="+root.transform.position);
            report.AppendLine("Placements="+count+"; simplified chairs="+chairs+"; converted materials="+converted.Count);
            foreach(var t in root.transform.Cast<Transform>().ToArray()) {
                if(t.name.Contains("Chair_"))continue;
                var bounds=BoundsOf(t.gameObject);
                if(Mathf.Abs(bounds.center.x)<21 && bounds.center.z>-2 && bounds.center.z<54 && bounds.min.y<1 && bounds.max.y<2.5f && bounds.size.x<10 && bounds.size.z<10) {
                    report.AppendLine("Removed court fixture: "+t.name);Object.DestroyImmediate(t.gameObject);
                }
            }
            BuildGrass(placeholder.transform);
            IntegrateGoal(game,report);
            IntegrateStriker(game);
            Lighting(game);
            CombineChairs(root,report);
            // Keep one renderer per material/chunk for the static architecture as well.
            CombineStructures(root,report);
            var all=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<MeshFilter>()).ToArray();
            long triangles=all.Where(f=>f.sharedMesh!=null).Sum(f=>(long)f.sharedMesh.triangles.Length/3);
            report.AppendLine("Scene mesh triangles="+triangles+" renderers="+scene.GetRootGameObjects().Sum(o=>o.GetComponentsInChildren<Renderer>().Length));
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Docs/stadium-integration.txt",report.ToString());
            if(File.Exists(Request))File.Delete(Request);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Output,true)};
            EditorApplication.delayCall+=Preview;
            Debug.Log("STADIUM_INTEGRATION_OK");
        }
        static void IntegrateStriker(PenaltyGame game) {
            game.striker.position=new Vector3(1.2f,0,12.1f);
            var keeper=game.keeper.Find("GoalieVisual");if(keeper==null)return;
            foreach(var r in game.striker.GetComponentsInChildren<Renderer>())r.enabled=false;
            var visual=Object.Instantiate(keeper.gameObject,game.striker);visual.name="StrikerVisual";
            visual.transform.localRotation=Quaternion.Euler(0,180,0);
            var presentation=visual.GetComponent<KeeperPresentation>();if(presentation!=null)Object.DestroyImmediate(presentation);
            var animator=visual.GetComponent<Animator>();
            var source=AssetDatabase.LoadAllAssetsAtPath("Assets/SuperGoalie/Animations/GoalKeeperAnimations/IdleNonAlert.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
            string clipPath=Art+"/StrikerIdle.anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if(clip==null){clip=Object.Instantiate(source);AssetDatabase.CreateAsset(clip,clipPath);}else EditorUtility.CopySerialized(source,clip);
            AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
            string controllerPath=Art+"/Striker.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var sm=controller.layers[0].stateMachine;foreach(var state in sm.states)sm.RemoveState(state.state);
            var idle=sm.AddState("Idle");idle.motion=clip;sm.defaultState=idle;animator.runtimeAnimatorController=controller;
            var kit=Material("StrikerKit",new Color(.9f,.92f,.88f));
            foreach(var r in visual.GetComponentsInChildren<Renderer>()) {
                if(r.name=="kit")r.sharedMaterial=kit;
                if(r.name=="gloaves"){r.enabled=true;r.sharedMaterial=Material("StrikerHands",new Color(.28f,.17f,.11f));}
            }
            animator.Rebind();animator.Play("Idle",0,0);animator.Update(0);
            float feet=Mathf.Min(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,animator.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
            visual.transform.position+=Vector3.up*(.1f-feet);
        }
        static void IntegrateGoal(PenaltyGame game,StringBuilder report) {
            var goal=GameObject.Find("Goal_7.32x2.44");
            foreach(var renderer in goal.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Penalty/Art/Goal.fbx");
            var visual=Object.Instantiate(model,goal.transform);visual.name="BlenderGoal";
            var b=BoundsOf(visual);visual.transform.localScale*=7.56f/b.size.x;
            var net=visual.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("Net_")).ToArray();
            if(net.Length!=1)throw new Exception("Blender goal must have one combined net renderer.");
            if(net[0].bounds.center.z>0) {var s=visual.transform.localScale;s.z=-s.z;visual.transform.localScale=s;}
            var white=Material("GoalIvory",new Color(.91f,.93f,.91f));
            foreach(var r in visual.GetComponentsInChildren<Renderer>()){r.sharedMaterial=white;r.shadowCastingMode=ShadowCastingMode.Off;}
            game.goalNet=net;
            var kit=Material("KeeperKit",new Color(.035f,.5f,.54f));
            foreach(var r in game.keeper.GetComponentsInChildren<Renderer>())if(r.name=="kit")r.sharedMaterial=kit;
            report.AppendLine("Blender goal bounds="+BoundsOf(visual)+"; net renderers="+net.Length);
        }
        static GameObject CreateSeat(Bounds b,Material mat) {
            var root=new GameObject("SimpleSeat");
            // A 36-triangle seat replaces a multi-thousand-triangle source chair.
            Box(root.transform,new Vector3(b.center.x,b.min.y+b.size.y*.45f,b.center.z),new Vector3(b.size.x*.9f,b.size.y*.12f,b.size.z*.9f),mat);
            Box(root.transform,new Vector3(b.center.x,b.min.y+b.size.y*.77f,b.min.z+b.size.z*.12f),new Vector3(b.size.x*.9f,b.size.y*.46f,b.size.z*.15f),mat);
            Box(root.transform,new Vector3(b.center.x,b.min.y+b.size.y*.22f,b.center.z),new Vector3(b.size.x*.18f,b.size.y*.4f,b.size.z*.3f),mat);
            return root;
        }
        static void Box(Transform parent,Vector3 pos,Vector3 scale,Material mat) {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=scale;
            Object.DestroyImmediate(o.GetComponent<Collider>());var r=o.GetComponent<Renderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        }
        static void CombineChairs(GameObject root,StringBuilder report) {
            var seats=root.transform.Cast<Transform>().Where(t=>t.name.Contains("Chair_")).ToArray();
            var groups=new Dictionary<string,List<MeshFilter>>();
            foreach(var t in seats)foreach(var f in t.GetComponentsInChildren<MeshFilter>()) {
                // Spatial chunks retain useful frustum culling.
                var p=f.transform.position;string key=Mathf.FloorToInt(p.x/12)+"_"+Mathf.FloorToInt(p.z/12);
                if(!groups.ContainsKey(key))groups[key]=new List<MeshFilter>();groups[key].Add(f);
            }
            foreach(var g in groups)Combine(g.Value,root.transform,"Seats_"+g.Key);
            foreach(var t in seats)Object.DestroyImmediate(t.gameObject);
            report.AppendLine("Seat chunks="+groups.Count);
        }
        static void CombineStructures(GameObject root,StringBuilder report) {
            var sources=root.transform.Cast<Transform>().Where(t=>!t.name.StartsWith("Seats_")).ToArray();
            var groups=new Dictionary<Material,List<MeshFilter>>();
            foreach(var t in sources)foreach(var f in t.GetComponentsInChildren<MeshFilter>()) {
                var r=f.GetComponent<MeshRenderer>();if(r==null || f.sharedMesh==null || f.sharedMesh.subMeshCount!=1 || r.sharedMaterials.Length!=1)continue;
                var m=r.sharedMaterial;if(!groups.ContainsKey(m))groups[m]=new List<MeshFilter>();groups[m].Add(f);
            }
            int chunks=0;
            foreach(var g in groups) {
                if(g.Value.Count<2)continue;
                Combine(g.Value,root.transform,"Structure_"+chunks++);
                foreach(var f in g.Value)Object.DestroyImmediate(f.gameObject);
            }
            report.AppendLine("Combined structure groups="+chunks);
        }
        static void Combine(List<MeshFilter> filters,Transform parent,string name) {
            var mesh=new Mesh {name=name,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(filters.Select(f=>new CombineInstance {mesh=f.sharedMesh,transform=parent.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
            string path=Art+"/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null) {AssetDatabase.CreateAsset(mesh,path);existing=mesh;}
            else {EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);}
            var o=new GameObject(name);o.transform.SetParent(parent,false);o.AddComponent<MeshFilter>().sharedMesh=existing;
            var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=filters[0].GetComponent<Renderer>().sharedMaterial;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        }
        static void BuildGrass(Transform pitch) {
            var grass=Material("PitchGrass",Color.white);var stripe=Material("PitchStripe",new Color(.77f,.86f,.72f));
            string path=Art+"/Turf.png";
            var tex=new Texture2D(512,512,TextureFormat.RGB24,false);var random=new System.Random(7401);
            var pixels=new Color[512*512];
            for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
                float n=Mathf.PerlinNoise(x*.025f,y*.025f)*.3f+(float)random.NextDouble()*.24f;
                float blade=(x%3==0?.04f:0);pixels[y*512+x]=new Color(.1f+n*.24f,.22f+n*.37f+blade,.045f+n*.16f);
            }
            tex.SetPixels(pixels);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
            grass.mainTexture=stripe.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            grass.SetFloat("_Smoothness",.03f);stripe.SetFloat("_Smoothness",.03f);
            foreach(var t in pitch.GetComponentsInChildren<Transform>().ToArray())if(t.name=="MowingStripe")Object.DestroyImmediate(t.gameObject);
            var ground=pitch.Find("PitchCollider");ground.localPosition=new Vector3(0,-.16f,28);ground.localScale=new Vector3(60,.3f,80);ground.GetComponent<Renderer>().enabled=false;
            for(int i=0;i<20;i++) {
                float z=-12+i*4;
                var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-30,0,z),new Vector3(-30,0,z+4),new Vector3(30,0,z+4),new Vector3(30,0,z)};
                mesh.uv=new[]{new Vector2(0,z*.5f),new Vector2(0,(z+4)*.5f),new Vector2(30,(z+4)*.5f),new Vector2(30,z*.5f)};
                mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();
                string meshPath=Art+"/TurfStrip"+i+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(old==null){AssetDatabase.CreateAsset(mesh,meshPath);old=mesh;}else {EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);}
                var o=new GameObject("MowingStripe");o.transform.SetParent(pitch,false);o.AddComponent<MeshFilter>().sharedMesh=old;o.AddComponent<MeshRenderer>().sharedMaterial=i%2==0?grass:stripe;
            }
        }
        static void Lighting(PenaltyGame game) {
            var key=GameObject.Find("ArenaSoftKey").GetComponent<Light>();key.intensity=1.65f;key.color=new Color(.92f,.96f,1);key.transform.rotation=Quaternion.Euler(65,-25,0);RenderSettings.sun=key;
            var fill=GameObject.Find("PlayerFrontFill");if(fill!=null)fill.GetComponent<Light>().intensity=.8f;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.2f,.24f,.3f);
            var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(.2f,.24f,.3f));RenderSettings.ambientProbe=probe;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.025f,.04f,.065f);RenderSettings.fogDensity=.006f;
            game.gameCamera.farClipPlane=180;game.gameCamera.nearClipPlane=.08f;
            var mobile=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            mobile.msaaSampleCount=2;mobile.renderScale=1;EditorUtility.SetDirty(mobile);
            var lamps=new GameObject("ArenaLightPanels");
            var glow=Material("FloodlightGlow",Color.white);glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(2.2f,2.4f,2.7f));
            foreach(float x in new[]{-24f,24f})foreach(float z in new[]{-4f,16f,36f,56f}) {
                var bank=new GameObject("LightBank").transform;bank.SetParent(lamps.transform);bank.position=new Vector3(x,13,z);bank.LookAt(new Vector3(0,0,z));
                for(int i=0;i<5;i++)Box(bank,new Vector3((i-2)*.5f,0,0),new Vector3(.35f,.3f,.12f),glow);
            }
        }
        [MenuItem("Penalty Arena/06 — Stadyum görsel kontrolü")]
        public static void Preview() {
            if(SceneManager.GetActiveScene().path!=Output)return;
            var game=Object.FindFirstObjectByType<PenaltyGame>();var camera=game.gameCamera;
            var animator=game.keeper.GetComponentInChildren<Animator>();if(animator!=null){animator.Rebind();animator.Play("Idle",0,.5f);animator.Update(0);}
            var strikerAnimator=game.striker.GetComponentInChildren<Animator>();if(strikerAnimator!=null){strikerAnimator.Rebind();strikerAnimator.Play("Idle",0,0);strikerAnimator.Update(0);}
            var p=camera.transform.position;var q=camera.transform.rotation;var previous=RenderTexture.active;
            var rt=new RenderTexture(1600,900,24);var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try {
                Vector3[] positions={new Vector3(0,1.6f,15.5f),new Vector3(0,1.4f,-3),new Vector3(35,24,-22)};
                Vector3[] targets={new Vector3(0,-1,0),new Vector3(0,.85f,10),new Vector3(0,0,24)};
                string[] names={"shooter","keeper","overview"};
                for(int i=0;i<positions.Length;i++) {
                    game.SetGoalNetVisible(i!=1);
                    camera.transform.position=positions[i];camera.transform.LookAt(targets[i]);camera.targetTexture=rt;
                    // Warm up material variants before capturing the first camera in batch mode.
                    for(int frame=0;frame<4;frame++)camera.Render();RenderTexture.active=rt;
                    tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes("Docs/stadium-"+names[i]+".png",tex.EncodeToPNG());
                }
            } finally {game.SetGoalNetVisible(true);camera.targetTexture=null;RenderTexture.active=previous;camera.transform.SetPositionAndRotation(p,q);Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}
            File.WriteAllText("Docs/stadium-preview.done","Three camera renders captured.");Debug.Log("STADIUM_PREVIEW_OK");
            if(File.Exists("Temp/stadium-build.request")) {
                File.Delete("Temp/stadium-build.request");
                try {RuleChecks.Run();ArenaBuilder.BuildAndroid();File.WriteAllText("Docs/stadium-build.done","Android stadium build succeeded.");}
                catch(Exception e){File.WriteAllText("Docs/stadium-build-error.txt",e.ToString());Debug.LogException(e);}
            }
        }
    }
}
