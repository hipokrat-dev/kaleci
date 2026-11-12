using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace PenaltyArena.Editor
{
    public static partial class PremiumArena
    {
        static void Players(PenaltyGame game)
        {
            foreach(var rig in new[]{game.strikerMotion,game.keeperMotion}) {
                bool keeper=rig.goalkeeper;string prefix=keeper?"Keeper":"Striker";
                foreach(var skin in rig.visual.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                    skin.quality=SkinQuality.Bone4;skin.shadowCastingMode=ShadowCastingMode.On;skin.receiveShadows=true;
                    if(skin.name=="kit" || skin.name=="boots" || skin.name=="socks" || skin.name=="gloaves") {
                        var material=Mat(prefix+skin.name,Color.white,skin.name=="boots"?.4f:.22f);material.SetFloat("_Mode",2);
                        string part=skin.name;
                        material.SetTexture("_BaseMap",Paint(skin,rig,p=>Garment(part,p,keeper),prefix+part));skin.sharedMaterial=material;
                        // Explicit colors make the procedural cloth shader independent of optional source channels.
                        var mesh=Object.Instantiate(skin.sharedMesh);mesh.colors=Enumerable.Repeat(Color.white,mesh.vertexCount).ToArray();skin.sharedMesh=Save(mesh,prefix+part+".asset");
                    } else {
                        var material=Object.Instantiate(skin.sharedMaterial);material.name=prefix+skin.name;
                        if(skin.name=="head" && File.Exists(Art+"/HeadDetail.png"))material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/HeadDetail.png"));
                        material.SetColor("_BaseColor",new Color(1.12f,1.1f,1.05f));material.SetFloat("_Smoothness",skin.name=="eyes"?.65f:.31f);skin.sharedMaterial=Save(material,material.name+".mat");
                    }
                }
                Hair(rig,prefix);
            }
        }
        static Color Garment(string part,Vector3 p,bool keeper)
        {
            Color navy=new Color(.018f,.032f,.055f),ivory=new Color(.84f,.85f,.81f),teal=new Color(.018f,.29f,.31f),gold=new Color(.55f,.42f,.19f);
            if(part=="gloaves")return keeper?(Mathf.Abs(p.x)<.65f?navy:ivory):new Color(.25f,.13f,.075f);
            if(part=="socks") {
                Color c=keeper?navy:ivory;if(p.y>.46f&&p.y<.49f || p.y>.51f&&p.y<.53f)c=keeper?ivory:navy;return c;
            }
            if(part=="boots") {
                if(p.y<.04f)return new Color(.04f,.045f,.04f);
                if(p.y<.065f || (p.z>.05f&&p.z<.08f))return gold;
                return keeper?new Color(.07f,.08f,.07f):ivory;
            }
            bool shorts=p.y<1.045f;Color color=shorts?navy:keeper?teal:ivory;
            if(!shorts && !keeper && (Mathf.Abs(p.x)>.225f || p.y>1.59f))color=navy;
            if(shorts && Mathf.Abs(p.x)>.245f)color=keeper?teal:ivory;
            if(!shorts && keeper) {
                float stripe=Mathf.Sin((p.x*60+Mathf.Abs(p.y-1.35f)*45));color*=.93f+.07f*stripe;
            }
            float stitch=Mathf.Sin(p.x*450)*Mathf.Sin(p.y*450);color*=.97f+stitch*.025f;
            // Athletic numerals are painted in bind-pose coordinates onto the original UVs.
            if(p.z<.005f && p.y>1.23f && p.y<1.53f && Mathf.Abs(p.x)<.115f) {
                float x=p.x/.23f+.5f,y=(p.y-1.23f)/.30f;
                if(Digit(keeper?1:9,x,y))color=keeper?ivory:navy;
            }
            if(p.z>.15f && (new Vector2((p.x+.105f)/.033f,(p.y-1.45f)/.043f)).sqrMagnitude<1)color=gold;
            if(Mathf.Abs(p.x)>.35f && p.y>1.28f&&p.y<1.34f)color=keeper?navy:ivory;
            color.a=1;return color;
        }
        static bool Digit(int digit,float x,float y)
        {
            if(digit==1)return (x>.46f&&x<.64f&&y>.1f&&y<.9f)||(x>.30f&&x<.77f&&y>.06f&&y<.2f)||(y>.65f&&y<.83f&&x>.23f+(y-.65f)*.9f&&x<.56f);
            float ring=Mathf.Pow((x-.5f)/.32f,2)+Mathf.Pow((y-.72f)/.23f,2);
            return (ring<1&&ring>.37f)||(x>.65f&&x<.82f&&y>.2f&&y<.73f)||(y>.08f&&y<.24f&&x>.25f&&x<.76f);
        }
        static Texture2D Paint(SkinnedMeshRenderer skin,CharacterMotion rig,Func<Vector3,Color> shade,string name)
        {
            const int size=1024;var source=skin.sharedMesh;var vertices=source.vertices.Select(v=>rig.visual.InverseTransformPoint(skin.transform.TransformPoint(v))).ToArray();var uv=source.uv;var tris=source.triangles;
            var colors=new Color[size*size];var filled=new bool[colors.Length];
            for(int k=0;k<tris.Length;k+=3) {
                int ia=tris[k],ib=tris[k+1],ic=tris[k+2];Vector2 a=uv[ia]*(size-1),b=uv[ib]*(size-1),c=uv[ic]*(size-1);
                float denominator=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);if(Mathf.Abs(denominator)<.0001f)continue;
                int left=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x,b.x,c.x)),0,size-1),right=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x,b.x,c.x)),0,size-1);
                int bottom=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y,b.y,c.y)),0,size-1),top=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y,b.y,c.y)),0,size-1);
                for(int y=bottom;y<=top;y++)for(int x=left;x<=right;x++) {
                    float wa=((b.y-c.y)*(x-c.x)+(c.x-b.x)*(y-c.y))/denominator,wb=((c.y-a.y)*(x-c.x)+(a.x-c.x)*(y-c.y))/denominator,wc=1-wa-wb;
                    if(wa<-.001f||wb<-.001f||wc<-.001f)continue;
                    int n=y*size+x;colors[n]=shade(vertices[ia]*wa+vertices[ib]*wb+vertices[ic]*wc);filled[n]=true;
                }
            }
            for(int pass=0;pass<6;pass++) {
                var before=(bool[])filled.Clone();for(int y=1;y<size-1;y++)for(int x=1;x<size-1;x++) {
                    int i=y*size+x;if(before[i])continue;int j=before[i-1]?i-1:before[i+1]?i+1:before[i-size]?i-size:before[i+size]?i+size:-1;
                    if(j>=0){colors[i]=colors[j];filled[i]=true;}
                }
            }
            for(int i=0;i<colors.Length;i++)if(!filled[i])colors[i]=Color.black;
            var texture=new Texture2D(size,size,TextureFormat.RGB24,false);texture.SetPixels(colors);texture.Apply();string path=Art+"/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.maxTextureSize=1024;importer.anisoLevel=4;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static void Hair(CharacterMotion rig,string prefix)
        {
            var skin=rig.visual.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="head");var source=skin.sharedMesh;
            var points=source.vertices.ToList();var normals=source.normals.ToList();var coords=source.uv.ToList();var weights=source.boneWeights.ToList();var colors=Enumerable.Repeat(Color.white,points.Count).ToList();var indices=new List<int>();
            Vector3[] rest=points.Select(v=>rig.visual.InverseTransformPoint(skin.transform.TransformPoint(v))).ToArray();
            bool Scalp(Vector3 p)=>p.y>(p.z>.1f?1.846f:p.z>0?1.80f:1.77f);
            var random=new System.Random(rig.goalkeeper?17:29);
            for(int i=0;i<points.Count;i++) {
                float push=Scalp(rest[i])?.006f+.003f*Mathf.Sin(rest[i].x*85)*Mathf.Sin(rest[i].z*75):0;
                var outward=rig.visual.TransformVector(new Vector3(rest[i].x,Mathf.Max(.03f,rest[i].y-1.77f),rest[i].z-.06f).normalized*push);
                points[i]+=skin.transform.InverseTransformVector(outward);
            }
            var sourceIds=source.triangles;
            for(int k=0;k<sourceIds.Length;k+=3) {
                int a=sourceIds[k],b=sourceIds[k+1],c=sourceIds[k+2];var mid=(rest[a]+rest[b]+rest[c])/3;
                if(!Scalp(mid))continue;indices.AddRange(new[]{a,b,c});

            }
            // Small smooth curls break up the cap silhouette without long triangular spikes.
            for(int v=0;v<source.vertexCount;v++) {
                if(!Scalp(rest[v]) || random.NextDouble()>.4)continue;
                float radius=.009f+(float)random.NextDouble()*.006f;
                var center=points[v];int first=points.Count;
                for(int ring=0;ring<=4;ring++)for(int segment=0;segment<8;segment++) {
                    float phi=ring*Mathf.PI/4,theta=segment*Mathf.PI/4;
                    var normal=new Vector3(Mathf.Sin(phi)*Mathf.Cos(theta),Mathf.Cos(phi),Mathf.Sin(phi)*Mathf.Sin(theta));
                    points.Add(center+skin.transform.InverseTransformVector(rig.visual.TransformVector(normal*radius)));
                    normals.Add(skin.transform.InverseTransformDirection(rig.visual.TransformDirection(normal)));coords.Add(source.uv[v]);weights.Add(source.boneWeights[v]);
                    float tint=.85f+(float)random.NextDouble()*.25f;colors.Add(new Color(tint,tint,tint,1));
                }
                for(int ring=0;ring<4;ring++)for(int segment=0;segment<8;segment++) {
                    int a=first+ring*8+segment,b=first+ring*8+(segment+1)%8,c=a+8,d=b+8;
                    indices.AddRange(new[]{a,b,c,b,d,c});
                }
            }
            var mesh=new Mesh{name=prefix+"Hair"};mesh.SetVertices(points);mesh.SetUVs(0,coords);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.bindposes=source.bindposes;mesh.boneWeights=weights.ToArray();mesh.SetNormals(normals);mesh.RecalculateBounds();
            var obj=new GameObject(prefix+"TexturedHair");obj.transform.SetParent(skin.transform.parent,false);obj.transform.localPosition=skin.transform.localPosition;obj.transform.localRotation=skin.transform.localRotation;obj.transform.localScale=skin.transform.localScale;
            var hair=obj.AddComponent<SkinnedMeshRenderer>();hair.sharedMesh=Save(mesh,prefix+"Hair.asset");hair.bones=skin.bones;hair.rootBone=skin.rootBone;hair.quality=SkinQuality.Bone4;hair.updateWhenOffscreen=true;hair.localBounds=skin.localBounds;
            hair.sharedMaterial=Mat(prefix+"Hair",new Color(.065f,.035f,.018f),.16f);hair.shadowCastingMode=ShadowCastingMode.On;
        }
    }
}
