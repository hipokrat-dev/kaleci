#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace PenaltyArena
{
    // Opt-in visual verification in the built player; never exposed as a gameplay mode.
    public sealed class PresentationCapture : MonoBehaviour
    {
        string destination;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-arena-look-capture");
            if(i<0||i+1>=args.Length)return;
            new GameObject("PresentationCapture").AddComponent<PresentationCapture>().destination=args[i+1];
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(destination);
            var game=FindFirstObjectByType<PenaltyGame>();
            yield return null;
            game.enabled=false;game.GetComponent<ArenaUI>().enabled=false;
            game.strikerMotion.enabled=false;game.keeperMotion.enabled=false;
            game.ball.isKinematic=true;
            string[] names={"shooter","keeper","save"};
            for(int view=0;view<names.Length;view++) {
                bool keeper=view==1;game.SetGoalNetVisible(!keeper);
                game.gameCamera.transform.position=keeper?game.keeperCameraPosition:game.shooterCameraPosition;
                game.gameCamera.transform.LookAt(keeper?game.keeperCameraTarget:game.shooterCameraTarget);
                game.strikerMotion.Evaluate(0,0,false,0,0);
                game.keeperMotion.Evaluate(view==2?8:0,view==2?game.diveDuration:0,false,0,0);
                for(int frame=0;frame<20;frame++)yield return null;
                yield return new WaitForEndOfFrame();
                var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(destination,names[view]+".png"),image.EncodeToPNG());Destroy(image);
                for(int frame=0;frame<30;frame++)yield return null;
            }
            Debug.Log("PRESENTATION_PLAYER_CAPTURE_OK");
            Application.Quit(0);
        }
    }
}
#endif
