using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace PenaltyArena.Editor
{
    public static partial class PremiumArena
    {
        static void Hair(CharacterMotion rig,string prefix)
        {
            var source=rig.visual.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name=="head");var mesh=source.sharedMesh;
            var points=mesh.vertices.ToList();var coords=mesh.uv.ToList();var weights=mesh.boneWeights.ToList();var colors=Enumerable.Repeat(Color.white,points.Count).ToList();var triangles=new List<int>();
            var rest=points.Select(p=>rig.visual.InverseTransformPoint(source.transform.TransformPoint(p))).ToArray();
            float Hairline(Vector3 p)=>p.z>.13f?1.844f:p.z>.07f?1.814f:p.z>-.005f?1.785f:1.765f;
            bool Scalp(Vector3 p)=>p.y>Hairline(p);
            Vector3 Outward(Vector3 p)=>new Vector3(p.x/.095f,(p.y-1.775f)/.13f,(p.z-.065f)/.115f).normalized;
            Vector3 Cap(Vector3 p){float top=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.80f,1.90f,p.y));float edge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.025f,p.y-Hairline(p)));return p+Outward(p)*(.0005f+top*edge*(rig.goalkeeper?.017f:.023f));}
            Vector3 Local(Vector3 p)=>source.transform.InverseTransformPoint(rig.visual.TransformPoint(p));
            for(int i=0;i<points.Count;i++)if(Scalp(rest[i])){points[i]=Local(Cap(rest[i]));coords[i]=new Vector2(rest[i].x*9+rest[i].z*4,rest[i].z*8);}
            var ids=mesh.triangles;
            for(int i=0;i<ids.Length;i+=3){int a=ids[i],b=ids[i+1],c=ids[i+2];if(Scalp((rest[a]+rest[b]+rest[c])/3))triangles.AddRange(new[]{a,b,c});}
            // Tapered swept locks follow the scalp. Fine longitudinal shading replaces the old spherical curls.
            var random=new System.Random(rig.goalkeeper?337:715);int count=0;
            foreach(int vertex in Enumerable.Range(0,rest.Length).OrderBy(_=>random.Next())) {
                var p=rest[vertex];if(!Scalp(p)||p.y<1.81f||count>=140)continue;count++;
                var normal=Outward(p);var desired=new Vector3(rig.goalkeeper?-.3f:-.65f,.12f,-1);
                var along=Vector3.ProjectOnPlane(desired,normal).normalized;var across=Vector3.Cross(normal,along).normalized;
                float length=(p.y<1.86f?.025f:.048f)+(float)random.NextDouble()*.018f;
                float width=.0011f+(float)random.NextDouble()*.001f;float tint=.78f+(float)random.NextDouble()*.5f;var color=new Color(tint,tint,tint,1);
                int first=points.Count;
                for(int row=0;row<=7;row++) {
                    float t=row/7f;var center=Cap(p)+along*(length*t)-normal*(length*length*t*t/.24f)+normal*(Mathf.Sin(t*Mathf.PI)*.001f+.0004f);
                    float taper=Mathf.Pow(Mathf.Sin(Mathf.PI*(t*.94f+.025f)),.7f);
                    for(int side=0;side<6;side++) {
                        float angle=side*Mathf.PI/3;var q=center+across*(Mathf.Cos(angle)*width*taper)+normal*(Mathf.Sin(angle)*width*.45f*taper);
                        points.Add(Local(q));coords.Add(new Vector2(side/6f,t));weights.Add(mesh.boneWeights[vertex]);colors.Add(color);
                    }
                }
                for(int row=0;row<7;row++)for(int side=0;side<6;side++){int a=first+row*6+side,b=first+row*6+(side+1)%6,c=a+6,d=b+6;triangles.AddRange(new[]{a,b,c,b,d,c});}
            }
            var hairMesh=new Mesh{name=prefix+"SweptHair",indexFormat=points.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};hairMesh.SetVertices(points);hairMesh.SetUVs(0,coords);hairMesh.SetColors(colors);hairMesh.SetTriangles(triangles,0);hairMesh.bindposes=mesh.bindposes;hairMesh.boneWeights=weights.ToArray();PlayerMeshRefinement.Finish(hairMesh);
            var hair=CloneSkin(source,prefix+"TexturedHair",Save(hairMesh,prefix+"Hair.asset"));hair.sharedMaterial=PlayerMaterial(prefix+"Hair",3,.18f);hair.sharedMaterial.SetColor("_BaseColor",rig.goalkeeper?new Color(.027f,.016f,.01f):new Color(.035f,.022f,.014f));
        }
    }
}
