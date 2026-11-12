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
            var cloth=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/KitRelief.png");
            Texture2D clothNormal=null;
            if(cloth!=null) {
                string path=Art+"/KitReliefNormal.png";File.Copy(Art+"/KitRelief.png",path,true);AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.NormalMap;importer.convertToNormalmap=true;importer.heightmapScale=.006f;importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();clothNormal=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var colorImporter=(TextureImporter)AssetImporter.GetAtPath(Art+"/KitRelief.png");colorImporter.sRGBTexture=false;colorImporter.maxTextureSize=1024;colorImporter.mipmapEnabled=true;colorImporter.wrapMode=TextureWrapMode.Clamp;colorImporter.SaveAndReimport();
            }
            foreach(var rig in new[]{game.strikerMotion,game.keeperMotion}) {
                bool keeper=rig.goalkeeper;string prefix=keeper?"Keeper":"Striker";
                foreach(var skin in rig.visual.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                    skin.quality=SkinQuality.Bone4;skin.shadowCastingMode=ShadowCastingMode.On;skin.receiveShadows=true;
                    string part=skin.name;bool garment=part=="kit"||part=="boots"||part=="socks"||part=="gloaves";
                    int kind=part=="kit"||part=="socks"?1:part=="boots"||part=="gloaves"&&keeper?2:part=="eyes"?4:0;
                    var original=skin.sharedMaterial;
                    var material=PlayerMaterial(prefix+part,kind,part=="eyes"?.72f:part=="boots"?.38f:kind==0?.33f:.24f);
                    if(!garment) {
                        material.SetTexture("_BaseMap",original.GetTexture("_BaseMap"));material.SetColor("_BaseColor",new Color(1.25f,1.19f,1.14f));
                        if(part=="head") {string head=File.Exists(Art+"/HeadDetail-v2.png")?Art+"/HeadDetail-v2.png":Art+"/HeadDetail.png";if(File.Exists(head))material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(head));}
                    }
                    if(part=="kit"&&cloth!=null){material.SetTexture("_ClothDetail",cloth);material.SetFloat("_ClothAmount",.45f);material.SetTexture("_BumpMap",clothNormal);material.SetFloat("_BumpScale",.30f);}
                    skin.sharedMaterial=material;
                    var mesh=part=="eyes"||part=="boots"||part=="body"||part=="socks"?Object.Instantiate(skin.sharedMesh):PlayerMeshRefinement.Refine(skin.sharedMesh);
                    Sculpt(mesh,skin,rig,part);mesh.colors=Enumerable.Repeat(Color.white,mesh.vertexCount).ToArray();PlayerMeshRefinement.Finish(mesh);skin.sharedMesh=Save(mesh,prefix+part+".asset");
                    if(garment)material.SetTexture("_BaseMap",Paint(skin,rig,p=>Garment(part,p,keeper),prefix+part));
                }
                if(keeper)LongSleeves(rig);
                Hair(rig,prefix);
            }
        }
        static Material PlayerMaterial(string name,int kind,float smooth)
        {
            var material=Mat(name,Color.white,smooth,"Penalty/Player Surface");material.SetFloat("_SurfaceKind",kind);return material;
        }
        static void Sculpt(Mesh mesh,SkinnedMeshRenderer skin,CharacterMotion rig,string part)
        {
            var vertices=mesh.vertices;var normals=mesh.normals;
            for(int i=0;i<vertices.Length;i++) {
                var p=rig.visual.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));
                var n=rig.visual.InverseTransformDirection(skin.transform.TransformDirection(normals[i])).normalized;
                if(part=="kit") {
                    float sides=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.07f,.22f,Mathf.Abs(p.x)));
                    if(p.y>1.045f) {
                        float hem=Mathf.Exp(-Mathf.Pow((p.y-1.10f)/.10f,2));
                        p.x*=1-.035f*hem;
                        float fold=(Mathf.Sin(p.y*68+Mathf.Abs(p.x)*19)*.0035f+Mathf.Sin(p.y*117-p.x*31)*.0014f)*(sides*.8f+hem*.5f);
                        p+=n*fold;
                    }else{
                        float hem=1-Mathf.SmoothStep(.61f,.83f,p.y);p.y+=.025f*hem;
                        p+=n*(Mathf.Sin(Mathf.Abs(p.x)*85+p.y*12)*.0035f*(1-hem*.5f));
                    }
                } else if(part=="gloaves"&&!rig.goalkeeper) {
                    // The bare hand uses the existing five fingers. Narrow the glove cuff while retaining its overlap with the forearm.
                    float x=Mathf.Abs(p.x),taper=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.57f,.65f,x));
                    var center=new Vector3(p.x,1.168f-(x-.61f)*.9f,.064f+(x-.61f)*.12f);
                    p=center+Vector3.Scale(p-center,new Vector3(1,1-.18f*taper,1-.26f*taper));
                }else if(part=="body") {
                    float forearm=Mathf.Exp(-Mathf.Pow((Mathf.Abs(p.x)-.48f)/.085f,2));p+=n*forearm*.003f;
                }else if(part=="head") {
                    float jaw=Mathf.Exp(-Mathf.Pow((p.y-1.685f)/.042f,2));p.x*=1+jaw*(rig.goalkeeper?.085f:.035f);
                }
                vertices[i]=skin.transform.InverseTransformPoint(rig.visual.TransformPoint(p));
            }
            mesh.vertices=vertices;

        }
        static void LongSleeves(CharacterMotion rig)
        {
            var source=rig.visual.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="body");var mesh=Object.Instantiate(source.sharedMesh);var points=mesh.vertices;var normals=mesh.normals;
            var rest=points.Select(p=>rig.visual.InverseTransformPoint(source.transform.TransformPoint(p))).ToArray();var ids=mesh.triangles;var keep=new List<int>();
            for(int i=0;i<ids.Length;i+=3){var c=(rest[ids[i]]+rest[ids[i+1]]+rest[ids[i+2]])/3;if(Mathf.Abs(c.x)>.265f&&c.y>.95f)keep.AddRange(new[]{ids[i],ids[i+1],ids[i+2]});}
            for(int i=0;i<points.Length;i++) {
                var n=rig.visual.InverseTransformDirection(source.transform.TransformDirection(normals[i]));float fold=Mathf.Sin(Mathf.Abs(rest[i].x)*140+rest[i].z*25)*.0018f;
                points[i]=source.transform.InverseTransformPoint(rig.visual.TransformPoint(rest[i]+n*(.003f+fold)));
            }
            mesh.vertices=points;mesh.triangles=keep.ToArray();PlayerMeshRefinement.Finish(mesh);
            var sleeve=CloneSkin(source,"KeeperLongSleeves",Save(mesh,"KeeperLongSleeves.asset"));
            var material=PlayerMaterial("KeeperLongSleeves",1,.24f);material.SetTexture("_BaseMap",Paint(source,rig,p=>Mathf.Abs(p.x)>.596f?new Color(.02f,.04f,.055f):new Color(.018f,.29f,.31f)*(1+.035f*Mathf.Sin(p.x*110)),"KeeperSleeve"));sleeve.sharedMaterial=material;
        }
        static SkinnedMeshRenderer CloneSkin(SkinnedMeshRenderer source,string name,Mesh mesh)
        {
            var obj=new GameObject(name);obj.transform.SetParent(source.transform.parent,false);obj.transform.localPosition=source.transform.localPosition;obj.transform.localRotation=source.transform.localRotation;obj.transform.localScale=source.transform.localScale;
            var skin=obj.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.bones=source.bones;skin.rootBone=source.rootBone;skin.quality=SkinQuality.Bone4;skin.updateWhenOffscreen=true;skin.localBounds=source.localBounds;skin.shadowCastingMode=ShadowCastingMode.On;return skin;
        }
        static Color Garment(string part,Vector3 p,bool keeper)
        {
            Color navy=new Color(.018f,.032f,.055f),ivory=new Color(.84f,.85f,.81f),teal=new Color(.018f,.29f,.31f),gold=new Color(.55f,.42f,.19f);
            if(part=="gloaves") {
                float x=Mathf.Abs(p.x);if(!keeper)return new Color(.43f,.26f,.18f)*(1+.035f*Mathf.Sin(x*230)*Mathf.Sin(p.y*150));
                if(x<.63f)return navy;if(x<.642f)return teal;
                float panel=Mathf.Sin(p.y*160+p.z*20);return ivory*(panel>.87f?.63f:.97f);
            }
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
            if(!shorts) {
                float side=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.11f,.24f,Mathf.Abs(p.x)));
                float fold=Mathf.Sin(p.y*74+Mathf.Abs(p.x)*19)*Mathf.Sin(p.y*33-p.x*16);
                color*=1-side*.09f+fold*(.025f+side*.055f);
                if(!keeper&&Mathf.Abs(p.x)>.365f&&Mathf.Abs(p.x)<.385f)color=ivory;
            }
            float stitch=Mathf.Sin(p.x*450)*Mathf.Sin(p.y*450);color*=.99f+stitch*.008f;
            // Athletic numerals are painted in bind-pose coordinates onto the original UVs.
            if(p.z<.005f && p.y>1.23f && p.y<1.53f && Mathf.Abs(p.x)<.115f) {
                float x=p.x/.23f+.5f,y=(p.y-1.23f)/.30f;
                if(Digit(keeper?1:9,x,y))color=keeper?ivory:navy;
            }
            var crest=new Vector2((p.x+.105f)/.029f,(p.y-1.45f)/.037f);
            if(p.z>.15f&&crest.y<1&&crest.y>-1&&Mathf.Abs(crest.x)<Mathf.Min(.82f,crest.y+1.02f)) {
                color=gold;if(crest.y>-.65f&&crest.y<.7f&&Mathf.Abs(crest.x)<Mathf.Min(.57f,crest.y+.68f))color=navy;
            }
            if(!keeper&&Mathf.Abs(p.x)>.35f && p.y>1.28f&&p.y<1.34f)color=ivory;
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
    }
}
