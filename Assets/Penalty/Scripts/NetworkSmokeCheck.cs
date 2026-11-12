using System;
using UnityEngine;
namespace PenaltyArena
{
    // Only activated by explicit CLI flags on development players. Not a game mode.
    public sealed class NetworkSmokeCheck : MonoBehaviour
    {
        PenaltyGame game; bool host, started, ready, done; int sent = -1; float began, finishAt;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (!Debug.isDebugBuild) return;
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-arena-smoke-host")<0 && Array.IndexOf(args,"-arena-smoke-client")<0) return;
            new GameObject("NetworkSmokeCheck").AddComponent<NetworkSmokeCheck>();
        }
        void Start() { game=FindFirstObjectByType<PenaltyGame>(); host=Array.IndexOf(Environment.GetCommandLineArgs(),"-arena-smoke-host")>=0; began=Time.realtimeSinceStartup; }
        void Update()
        {
            if(Time.realtimeSinceStartup-began>120) { Debug.LogError("SMOKE_TIMEOUT"); Application.Quit(2); return; }
            if(!started && Time.realtimeSinceStartup-began>1) { started=true; game.PlayerName=host?"Test Host":"Test Client"; game.room.Local(host); }
            if(!game.room.Connected) return;
            if(!ready && game.View.count==2) { ready=true; game.Ready(); }
            var v=game.View;
            if(game.Phase==MatchPhase.Aiming && game.Shooting && sent!=v.shotId) {
                sent=v.shotId;
                // Host shoots into a corner; client deliberately shoots wide.
                game.room.Send(new PlayerCommand {action="shoot",shotId=v.shotId,x=host?2.8f:4.7f,y=1,power=.65f});
                // A duplicate shot must not restart the ball.
                game.room.Send(new PlayerCommand {action="shoot",shotId=v.shotId,x=0,y=1,power=.2f});
            }
            if(game.Phase==MatchPhase.Finished && !done) {
                done=true; finishAt=Time.realtimeSinceStartup;
                Debug.Log($"SMOKE_RESULT score={v.score0}:{v.score1} history={v.history0}/{v.history1} winner={v.winner} shots={v.shotId+1}");
                if(v.score0!=5 || v.score1!=0 || v.history0!="GGGGG" || v.history1!="DDDDD") { Debug.LogError("SMOKE_BAD_RESULT"); Application.Quit(3); }
            }
            if(done && Time.realtimeSinceStartup-finishAt>2) Application.Quit(0);
        }
    }
}
