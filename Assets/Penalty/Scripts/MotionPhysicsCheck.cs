using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace PenaltyArena
{
    public static class MotionPhysicsCheck
    {
        static void Require(bool ok,string label) {if(!ok)throw new Exception("MOTION_PHYSICS: "+label);}
        public static void Run(PenaltyGame game)
        {
            var scene=SceneManager.CreateScene("MotionPhysics",new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            var clone=Object.Instantiate(game.keeper.gameObject);SceneManager.MoveGameObjectToScene(clone,scene);
            var motion=clone.GetComponentInChildren<CharacterMotion>();motion.game=game;motion.actorRoot=clone.transform;
            var ball=new GameObject("TestBall");SceneManager.MoveGameObjectToScene(ball,scene);
            var col=ball.AddComponent<SphereCollider>();col.radius=.11f;
            var rb=ball.AddComponent<Rigidbody>();rb.useGravity=false;rb.mass=.43f;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var physics=scene.GetPhysicsScene();
            try {
                for(int code=1;code<=9;code++) {
                    motion.Evaluate(code,game.diveDuration,false,0,0);motion.hitboxes.SetActive(true);motion.hitboxes.Sync();
                    var hand=code>=7?motion.rightHand:motion.leftHand;
                    rb.position=hand.position+Vector3.forward*2;rb.linearVelocity=Vector3.back*20;rb.angularVelocity=Vector3.zero;
                    Physics.SyncTransforms();
                    for(int n=0;n<20;n++)physics.Simulate(.01f);
                    Require(rb.linearVelocity.z>-10,"Actual glove collision "+code+" velocity="+rb.linearVelocity);
                    Debug.Log("Physics save "+code+": velocity="+rb.linearVelocity.ToString("F2"));
                }
                // A shot at the opposite side must pass, proving no old upright/wide proxy remains.
                motion.Evaluate(5,game.diveDuration,false,0,0);motion.hitboxes.Sync();
                rb.position=new Vector3(2.9f,1.2f,2.5f);rb.linearVelocity=Vector3.back*20;Physics.SyncTransforms();
                for(int n=0;n<20;n++)physics.Simulate(.01f);
                Require(rb.position.z<0&&rb.linearVelocity.z<-19,"Wrong-side dive cannot save");
            } finally {Object.Destroy(clone);Object.Destroy(ball);SceneManager.UnloadSceneAsync(scene);}
        }
    }
}
