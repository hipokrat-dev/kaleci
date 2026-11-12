using System;
using UnityEngine;
namespace PenaltyArena
{
    public sealed class KeeperHitboxes : MonoBehaviour
    {
        [Serializable] public struct Segment { public Transform a,b; public CapsuleCollider capsule; public float radius; }
        public Segment[] segments;
        public SphereCollider[] spheres;
        public Transform[] sphereBones;
        public void SetActive(bool active)
        {
            foreach(var segment in segments)segment.capsule.enabled=active;
            foreach(var sphere in spheres)sphere.enabled=active;
        }
        public void Sync()
        {
            foreach(var segment in segments) {
                Vector3 delta=segment.b.position-segment.a.position;
                var c=segment.capsule;c.transform.SetPositionAndRotation((segment.a.position+segment.b.position)*.5f,Quaternion.FromToRotation(Vector3.up,delta));
                c.radius=segment.radius;c.height=delta.magnitude+segment.radius*2;
            }
            for(int i=0;i<spheres.Length;i++)spheres[i].transform.position=sphereBones[i].position;
        }
    }
}
