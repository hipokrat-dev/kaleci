using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace PenaltyArena.Editor
{
    public static partial class PremiumArena
    {
        sealed class Geometry
        {
            public readonly List<Vector3> p=new List<Vector3>(),n=new List<Vector3>();
            public readonly List<Vector2> uv=new List<Vector2>();public readonly List<Color> colors=new List<Color>();public readonly List<int> ids=new List<int>();
            public void Tri(Vector3 a,Vector3 b,Vector3 c,Color color)
            {
                int i=p.Count;var normal=Vector3.Cross(b-a,c-a).normalized;
                p.AddRange(new[]{a,b,c});n.AddRange(new[]{normal,normal,normal});uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one});colors.AddRange(new[]{color,color,color});ids.AddRange(new[]{i,i+1,i+2});
            }
            public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
            {int i=p.Count;var normal=Vector3.Cross(b-a,c-a).normalized;p.AddRange(new[]{a,b,c,d});n.AddRange(new[]{normal,normal,normal,normal});uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});colors.AddRange(new[]{color,color,color,color});ids.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});}
            public void Box(Vector3 center,Vector3 size,Color color,Quaternion rotation)
            {
                var v=new Vector3[8];for(int i=0;i<8;i++)v[i]=center+rotation*Vector3.Scale(size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                Quad(v[0],v[2],v[3],v[1],color);Quad(v[4],v[5],v[7],v[6],color);Quad(v[0],v[4],v[6],v[2],color);Quad(v[1],v[3],v[7],v[5],color);Quad(v[2],v[6],v[7],v[3],color);Quad(v[0],v[1],v[5],v[4],color);
            }
            public void Rod(Vector3 a,Vector3 b,float radius,Color color,int sides=6)
            {
                var axis=(b-a).normalized;var right=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.right:Vector3.up).normalized;var up=Vector3.Cross(axis,right);
                for(int i=0;i<sides;i++){float t=i*Mathf.PI*2/sides,u=(i+1)*Mathf.PI*2/sides;var x=(right*Mathf.Cos(t)+up*Mathf.Sin(t))*radius;var y=(right*Mathf.Cos(u)+up*Mathf.Sin(u))*radius;Quad(a+x,a+y,b+y,b+x,color);}
            }
            public void Head(Vector3 center,Vector3 scale,Color skin,Color hair)
            {
                Vector3 up=Vector3.up*scale.y;var ring=new Vector3[4];for(int i=0;i<4;i++)ring[i]=center+new Vector3(Mathf.Cos(i*Mathf.PI/2)*scale.x,0,Mathf.Sin(i*Mathf.PI/2)*scale.z);
                for(int i=0;i<4;i++){Tri(ring[(i+1)%4],ring[i],center+up,hair);Tri(ring[i],ring[(i+1)%4],center-up,skin);}
            }
            public Mesh Mesh(string name) {var m=new Mesh{name=name,indexFormat=IndexFormat.UInt32};m.SetVertices(p);m.SetNormals(n);m.SetUVs(0,uv);m.SetColors(colors);m.SetTriangles(ids,0);m.RecalculateBounds();return m;}
        }
        static T Save<T>(T asset,string filename) where T:Object
        {string path=Art+"/"+filename;var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old==null){AssetDatabase.CreateAsset(asset,path);return asset;}if(asset is Mesh source && old is Mesh target) {
                // Update native vertex/index buffers as well as serialized data when rebuilding in a live Editor.
                target.Clear();target.indexFormat=source.indexFormat;target.vertices=source.vertices;target.normals=source.normals;target.tangents=source.tangents;target.uv=source.uv;target.colors=source.colors;
                target.bindposes=source.bindposes;target.boneWeights=source.boneWeights;target.subMeshCount=source.subMeshCount;
                for(int i=0;i<source.subMeshCount;i++)target.SetTriangles(source.GetTriangles(i),i);target.RecalculateBounds();target.UploadMeshData(false);
            } else if(asset is Material sourceMaterial && old is Material targetMaterial) {
                targetMaterial.shader=sourceMaterial.shader;targetMaterial.CopyPropertiesFromMaterial(sourceMaterial);targetMaterial.enableInstancing=sourceMaterial.enableInstancing;
            } else EditorUtility.CopySerialized(asset,old);
            EditorUtility.SetDirty(old);Object.DestroyImmediate(asset);return old;}
        static Material Mat(string name,Color color,float smooth=.25f,string shader="Penalty/Stadium Surface")
        {var m=new Material(Shader.Find(shader)){name=name,enableInstancing=true};m.SetColor("_BaseColor",color);if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smooth);return Save(m,name+".mat");}
        static GameObject MeshObject(string name,Geometry geo,Material material,Transform parent,bool shadows=false)
        {var mesh=Save(geo.Mesh(name),name+".asset");var o=new GameObject(name);o.transform.SetParent(parent,false);o.AddComponent<MeshFilter>().sharedMesh=mesh;var r=o.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;return o;}
        static Vector3 Ring(float angle,float row,float y)
        {float x=Mathf.Cos(angle),z=Mathf.Sin(angle);return new Vector3(Mathf.Sign(x)*Mathf.Pow(Mathf.Abs(x),.72f)*(39+row*.85f),y,52.5f+Mathf.Sign(z)*Mathf.Pow(Mathf.Abs(z),.72f)*(70+row*.85f));}
        static float RowHeight(int row)=>.55f+row*.5f+(row/6)*1.7f;
        static void Bowl(Transform parent)
        {
            var concrete=Mat("TerraceConcrete",new Color(.105f,.125f,.15f));var edge=Mat("ConcourseOnyx",new Color(.025f,.035f,.045f),.35f);
            var crowdMat=Mat("SpectatorColors",Color.white);var lit=Mat("HospitalityLights",new Color(.6f,.4f,.19f));lit.SetColor("_Emission",new Color(1.4f,.85f,.34f));
            var rails=Mat("RailSteel",new Color(.22f,.26f,.29f),.65f);
            var canopy=new Geometry();
            for(int seg=0;seg<160;seg++){float a=seg*Mathf.PI*2/160,b=(seg+1)*Mathf.PI*2/160;canopy.Quad(Ring(a,16,14),Ring(a,24,14.5f),Ring(b,24,14.5f),Ring(b,16,14),Color.white);}
            MeshObject("CanopyUnderside",canopy,Mat("CanopyMetal",new Color(.12f,.16f,.20f),.4f),parent);
            var tiers=new Geometry();var fascia=new Geometry();var windows=new Geometry();var rail=new Geometry();
            for(int row=0;row<18;row++)for(int seg=0;seg<160;seg++) {
                float a=seg*Mathf.PI*2/160,b=(seg+1)*Mathf.PI*2/160,y=RowHeight(row);
                tiers.Quad(Ring(a,row-.55f,y),Ring(b,row-.55f,y),Ring(b,row+.5f,y),Ring(a,row+.5f,y),Color.white);
                tiers.Quad(Ring(a,row-.55f,y-.5f),Ring(b,row-.55f,y-.5f),Ring(b,row-.55f,y),Ring(a,row-.55f,y),Color.white);
                if(row%6==0) {
                    fascia.Quad(Ring(a,row-1,y-.8f),Ring(b,row-1,y-.8f),Ring(b,row-1,y+.25f),Ring(a,row-1,y+.25f),Color.white);
                    rail.Rod(Ring(a,row-1,y+.35f),Ring(b,row-1,y+.35f),.027f,Color.white,4);
                    if(row>0 && seg%3!=0)windows.Quad(Ring(a+.004f,row-1.02f,y-.5f),Ring(b-.004f,row-1.02f,y-.5f),Ring(b-.004f,row-1.02f,y-.17f),Ring(a+.004f,row-1.02f,y-.17f),Color.white);
                }
            }
            MeshObject("ThreeTierTerraces",tiers,concrete,parent);MeshObject("ConcourseBands",fascia,edge,parent);MeshObject("WarmHospitality",windows,lit,parent);MeshObject("BalconyRails",rail,rails,parent);
            var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Penalty/Art/Lookdev/CrowdAtlas.png");
            var imp=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(atlas));imp.alphaIsTransparency=true;imp.mipmapEnabled=true;imp.maxTextureSize=2048;imp.wrapMode=TextureWrapMode.Clamp;imp.mipMapsPreserveCoverage=true;imp.alphaTestReferenceValue=.5f;imp.SaveAndReimport();
            crowdMat=Mat("PhotographicCrowd",new Color(.72f,.76f,.82f),.2f,"Penalty/Stadium Crowd");crowdMat.SetTexture("_BaseMap",atlas);
            var random=new System.Random(291);
            for(int section=0;section<24;section++) {
                var people=new Geometry();
                for(int row=0;row<18;row++)for(int column=0;column<30;column++) {
                    if(column==0 || random.NextDouble()<.018)continue;
                    float a=(section*30+column)*Mathf.PI*2/720;var pos=Ring(a,row,RowHeight(row)+.1f);
                    var facing=(new Vector3(0,0,52.5f)-pos);facing.y=0;facing.Normalize();var right=Vector3.Cross(Vector3.up,facing);
                    float width=.58f+(float)random.NextDouble()*.08f,height=.78f+(float)random.NextDouble()*.12f;
                    float tone=.64f+(float)random.NextDouble()*.36f;var color=new Color(tone,tone,tone,1);
                    int first=people.uv.Count,index=random.Next(16);people.Quad(pos-right*width*.5f,pos-right*width*.5f+Vector3.up*height,pos+right*width*.5f+Vector3.up*height,pos+right*width*.5f,color);
                    var offset=new Vector2(index%4,index/4)*.25f;
                    for(int v=first;v<people.uv.Count;v++)people.uv[v]=offset+(people.uv[v]*.994f+Vector2.one*.003f)*.25f;
                }
                MeshObject("CrowdSector"+section,people,crowdMat,parent);
            }
        }
        static void Pitch(Transform parent)
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Penalty/Art/Lookdev/TurfColor.png");
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=8;importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.SaveAndReimport();
            var grass=Mat("MatchTurf",new Color(.53f,.68f,.50f),.22f);grass.SetTexture("_BaseMap",texture);grass.SetFloat("_Mode",1);grass.SetFloat("_Tiling",.85f);
            var g=new Geometry();g.Quad(new Vector3(-40,0,-8),new Vector3(-40,0,113),new Vector3(40,0,113),new Vector3(40,0,-8),Color.white);MeshObject("FootballTurf",g,grass,parent);
            var bladeMat=Mat("GrassTips",Color.white,.2f);var random=new System.Random(2006);
            for(int chunk=0;chunk<12;chunk++) {
                var blades=new Geometry();float bx=-12+(chunk%4)*6,bz=-2+(chunk/4)*6;
                for(int i=0;i<2800;i++) {
                    float x=bx+(float)random.NextDouble()*6,z=bz+(float)random.NextDouble()*6;
                    if((new Vector2(x/1.2f,(z-11)/.55f).sqrMagnitude<1 || new Vector2(x/2,(z-.45f)/.7f).sqrMagnitude<1) && random.NextDouble()<.86)continue;
                    float h=.018f+(float)random.NextDouble()*.022f,w=.0035f;var p=new Vector3(x,.001f,z);var axis=Quaternion.Euler(0,(float)random.NextDouble()*360,0)*Vector3.right;
                    Color c=Color.Lerp(new Color(.10f,.18f,.04f),new Color(.22f,.32f,.08f),(float)random.NextDouble());
                    c=c.linear;
                    blades.Tri(p-axis*w,p+Vector3.up*h+axis*.006f,p+axis*w,c);blades.Tri(p+axis*w,p+Vector3.up*h+axis*.006f,p-axis*w,c);
                    for(int j=blades.n.Count-6;j<blades.n.Count;j++)blades.n[j]=Vector3.up;
                }
                MeshObject("NearGrass"+chunk,blades,bladeMat,parent);
            }
        }
        static void Goal(PenaltyGame game,Transform parent)
        {
            var old=game.goalNet[0].transform.parent;foreach(var r in old.GetComponentsInChildren<Renderer>())r.enabled=false;
            var metal=Mat("PorcelainGoal",new Color(.86f,.88f,.84f),.38f);var rope=Mat("WovenNet",new Color(.53f,.57f,.54f),.12f);
            var frame=new Geometry();foreach(float x in new[]{-3.72f,3.72f})frame.Rod(new Vector3(x,.03f,0),new Vector3(x,2.5f,0),.06f,Color.white,24);
            frame.Rod(new Vector3(-3.72f,2.5f,0),new Vector3(3.72f,2.5f,0),.06f,Color.white,24);MeshObject("EnamelGoalFrame",frame,metal,parent,true);
            var net=new Geometry();
            for(int i=0;i<=46;i++) {
                float x=-3.66f+i*7.32f/46;
                net.Rod(new Vector3(x,.04f,-1.85f),new Vector3(x,2.44f,-1.7f),.0045f,Color.white,4);
                net.Rod(new Vector3(x,2.44f,-1.7f),new Vector3(x,2.44f,0),.0045f,Color.white,4);
            }
            for(int i=0;i<=16;i++) {
                float y=.04f+i*2.4f/16,z=Mathf.Lerp(-1.85f,-1.7f,i/16f);
                net.Rod(new Vector3(-3.66f,y,z),new Vector3(3.66f,y,z),.0045f,Color.white,4);
                foreach(float x in new[]{-3.66f,3.66f})net.Rod(new Vector3(x,y,z),new Vector3(x,y,0),.0045f,Color.white,4);
            }
            for(int i=1;i<11;i++)foreach(float x in new[]{-3.66f,3.66f})net.Rod(new Vector3(x,.04f,-i*.16f),new Vector3(x,2.44f,-i*.16f),.0045f,Color.white,4);
            var obj=MeshObject("GoalNet",net,rope,parent);game.goalNet=new[]{obj.GetComponent<Renderer>()};
        }
    }
}
