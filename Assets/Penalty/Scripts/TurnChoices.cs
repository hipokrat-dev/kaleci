using UnityEngine;
namespace PenaltyArena
{
    // Host-only intent storage. Snapshots expose readiness, never the first player's direction.
    public sealed class TurnChoices
    {
        public readonly struct Choice
        {
            public readonly float X, Y, Power;
            public Choice(float x,float y,float power){X=x;Y=y;Power=power;}
        }
        public int ShotId { get; }
        public int Shooter { get; }
        public bool HasShot { get; private set; }
        public bool HasSave { get; private set; }
        public bool BothLocked => HasShot && HasSave;
        public Choice Shot { get; private set; }
        public Choice Save { get; private set; }
        public TurnChoices(int shotId,int shooter){ShotId=shotId;Shooter=shooter;}
        public bool IsLocked(int slot)=>slot==Shooter?HasShot:HasSave;
        public bool TryLock(int actor,PlayerCommand command,MatchPhase phase)
        {
            if(command==null || command.shotId!=ShotId || !Finite(command.x) || !Finite(command.y) || !Finite(command.power))return false;
            bool shooting=command.action=="shoot";
            if(!shooting && command.action!="dive")return false;
            if(!MatchRules.CanAct(actor,Shooter,phase,shooting) || IsLocked(actor))return false;
            if(shooting){Shot=new Choice(Mathf.Clamp(command.x,-4.8f,4.8f),Mathf.Clamp(command.y,.15f,3.6f),Mathf.Clamp01(command.power));HasShot=true;}
            else {Save=new Choice(Mathf.Clamp(command.x,-1,1),Mathf.Clamp01(command.y),0);HasSave=true;}
            return true;
        }
        static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
    }
}
