using System;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PenaltyArena.Editor
{
    public static class StadiumChecks
    {
        static void Require(bool condition,string message) { if(!condition)throw new Exception(message); }
        public static void Run()
        {
            EditorSceneManager.OpenScene(StadiumIntegration.Output);
            var game=Object.FindFirstObjectByType<PenaltyGame>();
            var camera=game.gameCamera;var position=camera.transform.position;var rotation=camera.transform.rotation;
            // Assert actual screen projection from both role cameras, not just arithmetic signs.
            foreach(bool keeper in new[]{false,true}) {
                camera.transform.position=keeper?new Vector3(0,1.4f,-3):new Vector3(0,1.6f,15.5f);
                camera.transform.LookAt(keeper?new Vector3(0,.85f,10):new Vector3(0,-1,0));
                var center=keeper?game.keeper.position:Vector3.up;
                var before=camera.WorldToViewportPoint(center);
                foreach(float delta in new[]{-1f,1f}) {
                    var after=camera.WorldToViewportPoint(center+Vector3.right*game.ScreenDirectionToWorld(delta));
                    Require(Mathf.Sign(after.x-before.x)==Mathf.Sign(delta),"Swipe projected in the wrong direction: keeper="+keeper);
                }
            }
            camera.transform.SetPositionAndRotation(position,rotation);
            Require(game.goalNet.Length==1,"Expected the single Blender net mesh.");
            int collisions=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Count(c=>c.enabled);
            game.SetGoalNetVisible(false);Require(!game.goalNet[0].enabled,"Keeper net was not hidden.");
            Require(collisions==Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Count(c=>c.enabled),"Hiding the net changed physics.");
            game.SetGoalNetVisible(true);Require(game.goalNet[0].enabled,"Shooter net was not restored.");
            Require(game.goalNet[0].bounds.center.z<0,"Net is in front of goal plane.");
            Require(Mathf.Abs(game.ball.position.z-11)<.01f,"Penalty spot is not 11m.");
            Require(Mathf.Abs(game.striker.position.x)>1f,"Striker is obstructing the ball.");
            var striker=game.striker.GetComponentInChildren<Animator>();striker.Rebind();striker.Play("Idle",0,0);striker.Update(0);
            float feet=Mathf.Min(striker.GetBoneTransform(HumanBodyBones.LeftFoot).position.y,striker.GetBoneTransform(HumanBodyBones.RightFoot).position.y);
            Require(feet>=.02f && feet<.3f,"Striker feet are not on the pitch: "+feet);
            foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled))
                Require(r.sharedMaterials.All(m=>m!=null && m.shader!=null),"Missing material: "+r.name);
            File.WriteAllText("Docs/stadium-checks.txt","PASS: role-camera swipe projection (four directions), role-specific net visibility, unchanged colliders, net behind line, 11m spot, striker offset, material references.\n");
            Debug.Log("STADIUM_CHECKS_OK");
        }
        public static void OpenForReview() { EditorSceneManager.OpenScene(StadiumIntegration.Output); }
        public static void BuildAndVerify()
        {
            Run();RuleChecks.Run();ArenaBuilder.BuildAndroid();ArenaBuilder.BuildMac();
            File.WriteAllText("Docs/stadium-build.done","Android ARM64 and macOS development builds succeeded.");
        }
        public static void IntegrateAndVerify()
        {
            StadiumIntegration.IntegrateAndPreview();
            BuildAndVerify();
        }
    }
}
