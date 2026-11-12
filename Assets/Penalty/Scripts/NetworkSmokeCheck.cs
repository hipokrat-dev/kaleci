using System;
using UnityEngine;
namespace PenaltyArena
{
    // Only activated by explicit CLI flags on development players. Not a game mode.
    public sealed class NetworkSmokeCheck : MonoBehaviour
    {
        PenaltyGame game; bool host, relay, motion, reportedCode, started, ready, done, left; string joinCode=""; int sent = -1,dived=-1; float began, finishAt;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (!Debug.isDebugBuild) return;
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-arena-smoke-host")<0 && Array.IndexOf(args,"-arena-smoke-client")<0 && Array.IndexOf(args,"-arena-relay-host")<0 && Array.IndexOf(args,"-arena-relay-client")<0) return;
            new GameObject("NetworkSmokeCheck").AddComponent<NetworkSmokeCheck>();
        }
        void Start() {
            game=FindFirstObjectByType<PenaltyGame>();var args=Environment.GetCommandLineArgs();
            host=Array.IndexOf(args,"-arena-smoke-host")>=0 || Array.IndexOf(args,"-arena-relay-host")>=0;
            int client=Array.IndexOf(args,"-arena-relay-client");relay=client>=0 || Array.IndexOf(args,"-arena-relay-host")>=0;
            if(client>=0 && client+1<args.Length)joinCode=args[client+1];
            motion=Array.IndexOf(args,"-arena-motion-test")>=0;
            if(motion) {
                try {MotionPhysicsCheck.Run(game);Debug.Log("MOTION_PHYSICS_OK");}
                catch(Exception e){Debug.LogException(e);Application.Quit(4);enabled=false;return;}
            }
            began=Time.realtimeSinceStartup;
        }
        void Update()
        {
            if(done) {
                float elapsed=Time.realtimeSinceStartup-finishAt;
                // Keep sending the final snapshot before ending the private test session.
                if(relay && !left && elapsed>2){left=true;game.room.Leave();}
                if(elapsed>4)Application.Quit(0);return;
            }
            if(Time.realtimeSinceStartup-began>180) { Debug.LogError("SMOKE_TIMEOUT"); Application.Quit(2); return; }
            if(!started && Time.realtimeSinceStartup-began>1) {
                started=true; game.PlayerName=host?"Test Çağrı":"Test İpek";
                if(relay)game.room.Open(host,joinCode);else game.room.Local(host);
            }
            if(relay && host && !reportedCode && !string.IsNullOrEmpty(game.room.Code)) {reportedCode=true;Debug.Log("SMOKE_RELAY_CODE="+game.room.Code);}
            if(!game.room.Connected) return;
            if(!ready && game.View.count==2) { ready=true; game.Ready(); }
            var v=game.View;
            if(game.Phase==MatchPhase.Aiming && game.Shooting && sent!=v.shotId) {
                sent=v.shotId;
                // Host shoots into a corner; client deliberately shoots wide.
                float x=host?2.8f:4.7f;
                if(motion && host && v.shotId>0)x=v.shotId%4==0?2.9f:-2.9f;
                game.room.Send(new PlayerCommand {action="shoot",shotId=v.shotId,x=x,y=1,power=.65f});
                // A duplicate shot must not restart the ball.
                game.room.Send(new PlayerCommand {action="shoot",shotId=v.shotId,x=0,y=1,power=.2f});
            }
            // React to the visible run-up. Waiting for a Flying snapshot adds a network round trip
            // and intentionally becomes too late for a full-stretch corner save.
            bool diveMoment=(game.Phase==MatchPhase.Windup && game.NetworkNow-v.shotStartedAt>=.48) || game.Phase==MatchPhase.Flying;
            if(motion && !game.Shooting && v.shotId>0 && v.shooter==0 && diveMoment && dived!=v.shotId) {
                dived=v.shotId;game.room.Send(new PlayerCommand {action="dive",shotId=v.shotId,x=v.shotId%4==0?1:-1,y=.5f});
            }
            if(game.Phase==MatchPhase.Finished && !done) {
                done=true; finishAt=Time.realtimeSinceStartup;
                Debug.Log($"SMOKE_RESULT score={v.score0}:{v.score1} history={v.history0}/{v.history1} winner={v.winner} shots={v.shotId+1}");
                if(v.name0!="Test Çağrı" || v.name1!="Test İpek") {Debug.LogError("SMOKE_BAD_NAMES");Application.Quit(5);return;}
                if(v.score0!=(motion?1:5) || v.score1!=0 || v.history0!=(motion?"GKKKK":"GGGGG") || v.history1!="DDDDD") { Debug.LogError("SMOKE_BAD_RESULT"); Application.Quit(3); }
            }
        }
    }
}
