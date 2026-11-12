using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace PenaltyArena.Editor
{
    public static partial class PremiumArena
    {
        public static void Preview()
        {
            var original=SceneManager.GetActiveScene();string working=Art+"/Review.unity";File.Copy(Output,working,true);AssetDatabase.ImportAsset(working);
            var scene=EditorSceneManager.OpenScene(working,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            var game=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PenaltyGame>()).Single();var camera=game.gameCamera;
            File.WriteAllLines("Docs/presentation-renderers.txt",scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Renderer>()).Where(r=>r.enabled).Select(r=>r.name+" mesh="+(r.GetComponent<MeshFilter>()!=null ? (r.GetComponent<MeshFilter>().sharedMesh.vertexCount+" "+AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh)):"skin")+" bounds="+r.bounds+" mats="+string.Join(",",r.sharedMaterials.Select(m=>m.name))));
            var hidden=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene).SelectMany(s=>s.GetRootGameObjects()).Where(o=>o.activeSelf).ToArray();foreach(var o in hidden)o.SetActive(false);
            var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.antiAliasing=4;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);var old=RenderTexture.active;
            var skins=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<SkinnedMeshRenderer>()).Where(r=>r.enabled).ToArray();var meshes=new Mesh[skins.Length];
            for(int i=0;i<skins.Length;i++) {var o=new GameObject("ReviewSkin");o.transform.SetParent(skins[i].transform,false);meshes[i]=new Mesh();o.AddComponent<MeshFilter>().sharedMesh=meshes[i];o.AddComponent<MeshRenderer>().sharedMaterials=skins[i].sharedMaterials;skins[i].enabled=false;}
            try {
                game.strikerMotion.Evaluate(0,0,false,0,0);game.keeperMotion.Evaluate(0,0,false,0,0);
                camera.targetTexture=rt;camera.aspect=16f/9;
                Vector3[] positions={game.shooterCameraPosition,game.keeperCameraPosition,new Vector3(-1.3f,1.4f,13.5f),new Vector3(0,1.2f,4.2f),new Vector3(49,22,-20)};
                Vector3[] targets={game.shooterCameraTarget,game.keeperCameraTarget,new Vector3(1.2f,1,12.1f),new Vector3(0,1,.45f),new Vector3(0,4,40)};
                string[] names={"shooter","keeper","kit","face","overview"};
                for(int frame=0;frame<names.Length;frame++) {
                    camera.transform.position=positions[frame];camera.transform.LookAt(targets[frame]);game.SetGoalNetVisible(frame!=1);
                    for(int i=0;i<skins.Length;i++)skins[i].BakeMesh(meshes[i],true);
                    VolumeManager.instance.Update(camera.transform,camera.GetUniversalAdditionalCameraData().volumeLayerMask);
                    for(int warmup=0;warmup<3;warmup++)RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes("Docs/presentation-"+names[frame]+".png",tex.EncodeToPNG());
                }
            } finally {camera.targetTexture=null;RenderTexture.active=old;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);foreach(var m in meshes)Object.DestroyImmediate(m);EditorSceneManager.CloseScene(scene,true);foreach(var o in hidden)if(o!=null)o.SetActive(true);if(original.IsValid())SceneManager.SetActiveScene(original);}
            Debug.Log("PRESENTATION_PREVIEW_OK");
        }
    }
}
