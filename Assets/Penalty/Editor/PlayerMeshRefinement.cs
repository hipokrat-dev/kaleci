using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace PenaltyArena.Editor
{
    // One offline Loop pass. Geometric welds cross UV seams; UVs and skin bindings stay separate.
    static class PlayerMeshRefinement
    {
        sealed class Edge { public int a,b; public readonly List<int> opposite=new List<int>(); }
        static long Key(int a,int b)=>((long)Math.Min(a,b)<<32)|(uint)Math.Max(a,b);
        public static Mesh Refine(Mesh source)
        {
            var p=source.vertices;var uv=source.uv;var weights=source.boneWeights;var ids=source.triangles;
            var groups=new Dictionary<Vector3Int,int>();var remap=new int[p.Length];var positions=new List<Vector3>();
            for(int i=0;i<p.Length;i++) {
                var key=Vector3Int.RoundToInt(p[i]*1000000);
                if(!groups.TryGetValue(key,out int group)){group=positions.Count;positions.Add(p[i]);groups.Add(key,group);}remap[i]=group;
            }
            var edges=new Dictionary<long,Edge>();var neighbors=positions.Select(_=>new HashSet<int>()).ToArray();
            void AddEdge(int a,int b,int c){a=remap[a];b=remap[b];c=remap[c];if(a==b)return;long key=Key(a,b);if(!edges.TryGetValue(key,out var e)){e=new Edge{a=a,b=b};edges.Add(key,e);}e.opposite.Add(c);neighbors[a].Add(b);neighbors[b].Add(a);}
            for(int i=0;i<ids.Length;i+=3){AddEdge(ids[i],ids[i+1],ids[i+2]);AddEdge(ids[i+1],ids[i+2],ids[i]);AddEdge(ids[i+2],ids[i],ids[i+1]);}
            var boundary=new HashSet<int>();foreach(var e in edges.Values)if(e.opposite.Count!=2){boundary.Add(e.a);boundary.Add(e.b);}
            var smooth=new Vector3[positions.Count];
            for(int i=0;i<smooth.Length;i++) {
                smooth[i]=positions[i];int count=neighbors[i].Count;if(boundary.Contains(i)||count<3)continue;
                float beta=count==3?3f/16:3f/(8*count);smooth[i]=positions[i]*(1-count*beta);foreach(int j in neighbors[i])smooth[i]+=positions[j]*beta;
            }
            var points=p.Select((v,i)=>smooth[remap[i]]).ToList();var coords=uv.ToList();var skin=weights.ToList();var triangles=new List<int>();var splits=new Dictionary<long,int>();
            int Mid(int a,int b) {
                long key=Key(a,b);if(splits.TryGetValue(key,out int index))return index;
                index=points.Count;splits.Add(key,index);var e=edges[Key(remap[a],remap[b])];
                Vector3 pos=(p[a]+p[b])*.5f;if(e.opposite.Count==2)pos=(p[a]+p[b])*.375f+(positions[e.opposite[0]]+positions[e.opposite[1]])*.125f;
                points.Add(pos);coords.Add((uv[a]+uv[b])*.5f);skin.Add(Blend(weights[a],weights[b]));return index;
            }
            for(int i=0;i<ids.Length;i+=3) {
                int a=ids[i],b=ids[i+1],c=ids[i+2];if(remap[a]==remap[b]||remap[b]==remap[c]||remap[c]==remap[a])continue;
                int ab=Mid(a,b),bc=Mid(b,c),ca=Mid(c,a);triangles.AddRange(new[]{a,ab,ca,ab,b,bc,ca,bc,c,ab,bc,ca});
            }
            var mesh=new Mesh{name=source.name+"Refined",indexFormat=points.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(points);mesh.SetUVs(0,coords);mesh.SetTriangles(triangles,0);mesh.bindposes=source.bindposes;mesh.boneWeights=skin.ToArray();mesh.colors=Enumerable.Repeat(Color.white,points.Count).ToArray();Finish(mesh);return mesh;
        }
        static BoneWeight Blend(BoneWeight a,BoneWeight b)
        {
            var blend=new Dictionary<int,float>();void Add(int i,float w){if(w>0)blend[i]=blend.TryGetValue(i,out float old)?old+w*.5f:w*.5f;}
            Add(a.boneIndex0,a.weight0);Add(a.boneIndex1,a.weight1);Add(a.boneIndex2,a.weight2);Add(a.boneIndex3,a.weight3);
            Add(b.boneIndex0,b.weight0);Add(b.boneIndex1,b.weight1);Add(b.boneIndex2,b.weight2);Add(b.boneIndex3,b.weight3);
            var ordered=blend.OrderByDescending(e=>e.Value).Take(4).ToArray();float sum=ordered.Sum(e=>e.Value);var w=new BoneWeight();
            for(int i=0;i<ordered.Length;i++){int bone=ordered[i].Key;float value=ordered[i].Value/sum;if(i==0){w.boneIndex0=bone;w.weight0=value;}else if(i==1){w.boneIndex1=bone;w.weight1=value;}else if(i==2){w.boneIndex2=bone;w.weight2=value;}else{w.boneIndex3=bone;w.weight3=value;}}return w;
        }
        public static void Finish(Mesh mesh)
        {
            mesh.RecalculateNormals();var p=mesh.vertices;var n=mesh.normals;var sums=new Dictionary<Vector3Int,Vector3>();
            for(int i=0;i<p.Length;i++){var key=Vector3Int.RoundToInt(p[i]*1000000);sums[key]=sums.TryGetValue(key,out var sum)?sum+n[i]:n[i];}
            for(int i=0;i<p.Length;i++)n[i]=sums[Vector3Int.RoundToInt(p[i]*1000000)].normalized;
            mesh.normals=n;mesh.RecalculateTangents();mesh.RecalculateBounds();
        }
    }
}
