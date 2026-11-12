using UnityEngine;
namespace PenaltyArena
{
    // Local sound follows authoritative results; repeated snapshots never replay a cheer.
    public sealed class StadiumAudio : MonoBehaviour
    {
        PenaltyGame game;AudioSource bed,reaction;AudioClip goal,save;
        int lastResult=-1;bool paused;
        void Start()
        {
            game=GetComponent<PenaltyGame>();
            bed=gameObject.AddComponent<AudioSource>();bed.playOnAwake=false;bed.loop=true;bed.spatialBlend=0;bed.volume=0;
            bed.clip=Resources.Load<AudioClip>("Audio/CrowdBed");
            reaction=gameObject.AddComponent<AudioSource>();reaction.playOnAwake=false;reaction.spatialBlend=0;
            goal=Resources.Load<AudioClip>("Audio/CrowdGoal");save=Resources.Load<AudioClip>("Audio/CrowdSave");
            AudioListener.volume=PlayerPrefs.GetFloat("Arena.Volume",.8f);
            if(bed.clip!=null)bed.Play();
        }
        void Update()
        {
            if(paused)return;
            bool match=game.room.Connected&&game.Phase!=MatchPhase.Lobby&&game.Phase!=MatchPhase.Disconnected;
            bed.volume=Mathf.MoveTowards(bed.volume,match?.24f:.08f,Time.unscaledDeltaTime*.15f);
            var v=game.View;
            if(game.Phase==MatchPhase.Aiming&&v.shotId==0)lastResult=-1;
            if((game.Phase==MatchPhase.Result||game.Phase==MatchPhase.Finished)&&lastResult!=v.shotId) {
                lastResult=v.shotId;
                var history=v.shooter==0?v.history0:v.history1;
                if(!string.IsNullOrEmpty(history)){
                    char result=history[history.Length-1];
                    if(result=='G'&&goal!=null)reaction.PlayOneShot(goal,.65f);
                    else if(result=='K'&&save!=null)reaction.PlayOneShot(save,.45f);
                }
            }
        }
        void OnApplicationPause(bool value)
        {
            paused=value;SaveVolume();
            if(bed==null)return;
            if(value){bed.Pause();reaction.Pause();}else{bed.UnPause();reaction.UnPause();}
        }
        void OnApplicationQuit()=>SaveVolume();
        static void SaveVolume(){PlayerPrefs.SetFloat("Arena.Volume",AudioListener.volume);PlayerPrefs.Save();}
    }
}
