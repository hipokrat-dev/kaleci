using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace PenaltyArena.Editor
{
    public static partial class PremiumArena
    {
        static void Football(PenaltyGame game)
        {
            foreach (var renderer in game.ball.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            // Build the 12 pentagons and 20 hexagons by truncating an icosahedron.
            float g = (1f + Mathf.Sqrt(5f)) / 2f;
            var corners = new[]
            {
                new Vector3(-1,g,0), new Vector3(1,g,0), new Vector3(-1,-g,0), new Vector3(1,-g,0),
                new Vector3(0,-1,g), new Vector3(0,1,g), new Vector3(0,-1,-g), new Vector3(0,1,-g),
                new Vector3(g,0,-1), new Vector3(g,0,1), new Vector3(-g,0,-1), new Vector3(-g,0,1)
            };
            int[,] faces =
            {
                {0,11,5},{0,5,1},{0,1,7},{0,7,10},{0,10,11},
                {1,5,9},{5,11,4},{11,10,2},{10,7,6},{7,1,8},
                {3,9,4},{3,4,2},{3,2,6},{3,6,8},{3,8,9},
                {4,9,5},{2,4,11},{6,2,10},{8,6,7},{9,8,1}
            };
            var surface = new Geometry();
            var neighbors = new HashSet<int>[corners.Length];
            for (int i = 0; i < neighbors.Length; i++) neighbors[i] = new HashSet<int>();
            for (int i = 0; i < faces.GetLength(0); i++)
            {
                int a = faces[i,0], b = faces[i,1], c = faces[i,2];
                neighbors[a].Add(b); neighbors[a].Add(c);
                neighbors[b].Add(a); neighbors[b].Add(c);
                neighbors[c].Add(a); neighbors[c].Add(b);
                AddBallPanel(surface, new[] {
                    Cut(a,b), Cut(b,a), Cut(b,c), Cut(c,b), Cut(c,a), Cut(a,c)
                }, new Color(.84f,.83f,.77f));
            }

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 normal = corners[i].normalized;
                Vector3 axis = Vector3.Cross(normal, Vector3.up).normalized;
                Vector3 bitangent = Vector3.Cross(normal, axis);
                int center = i;
                var perimeter = neighbors[i].Select(j => Cut(center,j)).OrderBy(p =>
                    Mathf.Atan2(Vector3.Dot(p,bitangent),Vector3.Dot(p,axis))).ToArray();
                AddBallPanel(surface, perimeter, new Color(.033f,.049f,.065f));
            }

            var ball = MeshObject("MatchBallPanels", surface, Mat("MatchBallLeather", Color.white,.39f),
                game.ball.transform, true);
            ball.transform.localScale = Vector3.one * (.22f / game.ball.transform.lossyScale.x);
            ball.GetComponent<Renderer>().sharedMaterial.SetFloat("_Mode", 3);
            var seam = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            seam.name = "MatchBallSeams";
            UnityEngine.Object.DestroyImmediate(seam.GetComponent<Collider>());
            seam.transform.SetParent(ball.transform, false);
            seam.transform.localScale = Vector3.one * .989f;
            seam.GetComponent<Renderer>().sharedMaterial = Mat("MatchBallSeams",new Color(.031f,.032f,.029f),.24f);

            Vector3 Cut(int from,int to) => Vector3.Lerp(corners[from], corners[to], 1f / 3f);
        }

        static void AddBallPanel(Geometry geo, Vector3[] points, Color color)
        {
            const int divisions = 7;
            Vector3 center = points.Aggregate(Vector3.zero, (sum,p) => sum + p) / points.Length;
            Vector3 axis = (points[0] - center).normalized;
            Vector3 bitangent = Vector3.Cross(center.normalized, axis);
            if (Vector3.Dot(Vector3.Cross(points[0] - center, points[1] - center), center) < 0)
                Array.Reverse(points);

            for (int edge = 0; edge < points.Length; edge++)
            {
                Vector3 a = (points[edge] - center) * .984f;
                Vector3 b = (points[(edge + 1) % points.Length] - center) * .984f;
                for (int row = 0; row < divisions; row++)
                    for (int col = 0; col < divisions - row; col++)
                    {
                        Emit(row,col); Emit(row+1,col); Emit(row,col+1);
                        if (row + col + 2 <= divisions)
                        {
                            Emit(row+1,col); Emit(row+1,col+1); Emit(row,col+1);
                        }
                    }

                void Emit(int row,int col)
                {
                    float u = row / (float)divisions, v = col / (float)divisions;
                    Vector3 offset = a * u + b * v;
                    Vector3 normal = (center + offset).normalized;
                    float bevel = Mathf.Pow(u + v, 12f);
                    geo.ids.Add(geo.p.Count);
                    geo.p.Add(normal * (.5f - .0023f * bevel));
                    geo.n.Add((normal + offset.normalized * bevel * .13f).normalized);
                    geo.uv.Add(new Vector2(Vector3.Dot(offset,axis),Vector3.Dot(offset,bitangent)));
                    geo.colors.Add(color);
                }
            }
        }
    }
}
