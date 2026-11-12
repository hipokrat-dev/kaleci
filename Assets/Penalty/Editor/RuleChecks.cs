using System;
using System.IO;
using UnityEngine;
namespace PenaltyArena.Editor
{
    public static class RuleChecks
    {
        static int count;
        static void Check(bool ok,string name) { if(!ok) throw new Exception("FAIL: "+name); count++; Debug.Log("PASS: "+name); }
        public static void SetupAndRun() { ArenaBuilder.Create(); Run(); }
        public static void Run()
        {
            count=0;
            var m=new MatchRules();
            Check(!m.NextShot(),"Unresolved turn cannot advance");
            Check(!m.Resolve(99,ShotResult.Goal),"Stale shot rejected");
            for(int i=0;i<10;i++) {
                Check(m.Shooter==(i<5?0:1),"Five consecutive shots per player "+i);
                Check(MatchRules.AttemptNumber(i)==i%5+1,"Attempt within block "+i);
                Check(m.Resolve(i,i<5?ShotResult.Goal:ShotResult.Miss),"Resolution "+i);
                Check(!m.Resolve(i,ShotResult.Goal),"Duplicate resolution "+i);
                if(i<9){Check(!m.Finished,"No premature winner "+i);Check(m.NextShot(),"Advance "+i);}
            }
            Check(m.Finished&&m.Winner==0&&m.Score[0]==5&&m.History[1].Count==5,"Five-each winner");
            Check(!m.NextShot(),"Finished match cannot advance");
            m=new MatchRules();
            for(int i=0;i<10;i++) {m.Resolve(i,ShotResult.Save);m.NextShot();}
            Check(!m.Finished&&m.ShotId==10&&m.Shooter==0,"Draw starts paired sudden death");
            m.Resolve(10,ShotResult.Goal);Check(!m.Finished,"Sudden death waits for equal attempts");m.NextShot();
            Check(m.Shooter==1,"Second sudden death shooter");
            m.Resolve(11,ShotResult.Goal);Check(!m.Finished,"Matched goals extend sudden death");m.NextShot();
            Check(m.Shooter==0&&MatchRules.AttemptNumber(12)==7,"New sudden death pair");
            m.Resolve(12,ShotResult.Miss);m.NextShot();m.Resolve(13,ShotResult.Goal);
            Check(m.Finished&&m.Winner==1,"Sudden death decided after pair");
            foreach(MatchPhase phase in Enum.GetValues(typeof(MatchPhase))) {
                Check(MatchRules.CanAct(0,0,phase,true)==(phase==MatchPhase.Aiming),"Shooter selection phase "+phase);
                Check(MatchRules.CanAct(1,0,phase,false)==(phase==MatchPhase.Aiming),"Keeper selection phase "+phase);
                Check(!MatchRules.CanAct(1,0,phase,true)&&!MatchRules.CanAct(0,0,phase,false),"Wrong role "+phase);
            }
            Check(!MatchRules.CanAct(2,0,MatchPhase.Aiming,false),"Third player rejected");
            foreach(bool keeperFirst in new[]{false,true})foreach(int shooter in new[]{0,1}) {
                var gate=new TurnChoices(7,shooter);
                var shot=new PlayerCommand{action="shoot",shotId=7,x=2.8f,y=1,power=.65f};
                var save=new PlayerCommand{action="dive",shotId=7,x=-1,y=.5f};
                Check(gate.TryLock(keeperFirst?1-shooter:shooter,keeperFirst?save:shot,MatchPhase.Aiming),"First selection accepted");
                Check(!gate.BothLocked,"One selection never launches");
                Check(!gate.TryLock(keeperFirst?1-shooter:shooter,keeperFirst?save:shot,MatchPhase.Aiming),"First selection immutable");
                Check(gate.TryLock(keeperFirst?shooter:1-shooter,keeperFirst?shot:save,MatchPhase.Aiming)&&gate.BothLocked,"Both orders launch");
                shot.x=-4;save.x=1;
                Check(gate.Shot.X==2.8f&&gate.Save.X==-1,"Commands copied, outside mutations harmless");
                Check(!gate.TryLock(shooter,shot,MatchPhase.Aiming)&&!gate.TryLock(1-shooter,save,MatchPhase.Flying),"No relock or late reaction");
            }
            var fresh=new TurnChoices(8,1);
            Check(!fresh.IsLocked(0)&&!fresh.IsLocked(1),"New turn clears both selections");
            Check(!fresh.TryLock(1,null,MatchPhase.Aiming),"Null command rejected");
            var invalid=new PlayerCommand{action="shoot",shotId=7};
            Check(!fresh.TryLock(1,invalid,MatchPhase.Aiming),"Wrong shot id rejected");invalid.shotId=8;
            foreach(float n in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity}){
                invalid.x=n;Check(!fresh.TryLock(1,invalid,MatchPhase.Aiming),"Invalid x");invalid.x=0;
                invalid.y=n;Check(!fresh.TryLock(1,invalid,MatchPhase.Aiming),"Invalid y");invalid.y=0;
                invalid.power=n;Check(!fresh.TryLock(1,invalid,MatchPhase.Aiming),"Invalid power");invalid.power=0;
            }
            invalid.x=20;invalid.y=-1;invalid.power=10;
            Check(fresh.TryLock(1,invalid,MatchPhase.Aiming)&&fresh.Shot.X==4.8f&&fresh.Shot.Y==.15f&&fresh.Shot.Power==1,"Shot bounded");
            Check(!fresh.TryLock(0,new PlayerCommand{action="unknown",shotId=8},MatchPhase.Aiming),"Unknown action rejected");
            Check(fresh.TryLock(0,new PlayerCommand{action="dive",shotId=8,x=-20,y=20},MatchPhase.Aiming)&&fresh.Save.X==-1&&fresh.Save.Y==1,"Dive bounded");
            Directory.CreateDirectory("Docs"); File.WriteAllText("Docs/rule-test-results.txt",DateTime.UtcNow.ToString("O")+"\n"+count+" assertions passed in Unity Editor.\nNo device or Relay test implied.\n");
            Debug.Log("PENALTY_RULE_CHECKS_PASSED "+count);
        }
    }
}
