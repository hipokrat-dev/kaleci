#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace PenaltyArena
{
    // Explicit development-only capture; fixture scores are labelled as a preview in the HUD.
    public sealed class PresentationCapture : MonoBehaviour
    {
        string destination;bool withUI,players;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-arena-ui-capture");bool ui=i>=0;
            if(i<0)i=Array.IndexOf(args,"-arena-player-capture");bool player=i>=0&&!ui;
            if(i<0)i=Array.IndexOf(args,"-arena-look-capture");if(i<0||i+1>=args.Length)return;
            var capture=new GameObject("PresentationCapture").AddComponent<PresentationCapture>();capture.destination=args[i+1];capture.withUI=ui;capture.players=player;
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(destination);var game=FindFirstObjectByType<PenaltyGame>();yield return null;
            var ui=game.GetComponent<ArenaUI>();game.enabled=false;ui.enabled=withUI;game.strikerMotion.enabled=false;game.keeperMotion.enabled=false;game.ball.isKinematic=true;
            string[] names=withUI?new[]{"home","shooter","keeper","save","result","settings","finished","tablet","wide"}:players?new[]{"shooter","keeper","striker-portrait","keeper-portrait","hands","strike","save-low","save-high","recovery"}:new[]{"shooter","keeper","save"};
            foreach(string name in names) {
                bool keeper=name=="keeper";game.SetGoalNetVisible(!keeper);
                game.gameCamera.transform.position=keeper?game.keeperCameraPosition:game.shooterCameraPosition;
                game.gameCamera.transform.LookAt(keeper?game.keeperCameraTarget:game.shooterCameraTarget);
                game.strikerMotion.Evaluate(0,0,false,0,0);game.keeperMotion.Evaluate(name=="save"?8:0,name=="save"?game.diveDuration:0,false,0,0);
                if(players) {
                    game.SetGoalNetVisible(name=="shooter"||name=="strike");
                    if(name.EndsWith("portrait")) {
                        var rig=name.StartsWith("striker")?game.strikerMotion:game.keeperMotion;var target=rig.head.position+Vector3.up*.06f;
                        game.gameCamera.transform.position=target+rig.visual.forward*1.1f+rig.visual.right*.27f+Vector3.up*.02f;game.gameCamera.transform.LookAt(target);
                    }else if(name=="hands") {
                        var target=(game.keeperMotion.leftHand.position+game.keeperMotion.rightHand.position)*.5f;
                        game.gameCamera.transform.position=target+Vector3.forward*1.05f+Vector3.up*.18f;game.gameCamera.transform.LookAt(target);
                    }else if(name=="strike")game.strikerMotion.Evaluate(0,0,true,MotionTiming.StrikeContact,0);
                    else if(name.StartsWith("save")||name=="recovery") {
                        int code=name=="save-low"?4:6;float t=name=="recovery"?2.4f:game.diveDuration;game.keeperMotion.Evaluate(code,t,false,0,0);
                        game.gameCamera.transform.position=new Vector3(0,1.3f,5.8f);game.gameCamera.transform.LookAt(new Vector3(-1.2f,1,.45f));
                    }
                }
                if(withUI){
                    if(name=="tablet")Screen.SetResolution(1200,900,FullScreenMode.Windowed);
                    if(name=="wide")Screen.SetResolution(1600,720,FullScreenMode.Windowed);
                    ui.captureView=name=="home"?null:new MatchSnapshot{count=2,shotId=6,shooter=0,score0=2,score1=1,name0="OYUNCU 1",name1="OYUNCU 2",history0="GGD",history1="GKD",phase=(int)(name=="result"?MatchPhase.Result:name=="finished"?MatchPhase.Finished:MatchPhase.Aiming),winner=0,message="KURTARIŞ"};
                    if(name=="tablet"){ui.captureView.name0="ÇAĞRI ŞİMŞEK";ui.captureView.name1="İPEK ÖZTÜRK";}
                    ui.captureShooting=!keeper;ui.captureAiming=name=="shooter"||name=="tablet"||name=="wide";ui.captureSettings=name=="settings";
                }
                for(int frame=0;frame<30;frame++){game.UpdateCameraFraming();yield return null;}yield return new WaitForEndOfFrame();
                var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(destination,name+".png"),image.EncodeToPNG());Destroy(image);
                for(int frame=0;frame<10;frame++)yield return null;
            }
            Debug.Log(withUI?"PRESENTATION_UI_CAPTURE_OK":players?"PRESENTATION_ACTORS_CAPTURE_OK":"PRESENTATION_PLAYER_CAPTURE_OK");Application.Quit(0);
        }
    }
}
#endif
