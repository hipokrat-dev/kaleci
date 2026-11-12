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
            Check(!m.Resolve(99,ShotResult.Goal),"Stale/invalid shot rejected");
            Check(m.Resolve(0,ShotResult.Goal),"Goal accepted");
            Check(!m.Resolve(0,ShotResult.Goal)&&m.Score[0]==1,"Duplicate scoring rejected");
            Check(m.NextShot()&&m.Shooter==1,"Role alternates");
            for(int i=1;i<10;i++) { m.Resolve(i,i%2==0?ShotResult.Goal:ShotResult.Miss); if(i<9) { Check(!m.Finished,"All five shots required "+i); m.NextShot(); } }
            Check(m.Finished&&m.Winner==0&&m.Score[0]==5&&m.History[1].Count==5,"Five-each winner");
            Check(!m.NextShot(),"Finished match cannot advance");
            m=new MatchRules();
            for(int i=0;i<10;i++) { m.Resolve(i,ShotResult.Save); m.NextShot(); }
            Check(!m.Finished&&m.ShotId==10,"Draw enters sudden death");
            m.Resolve(10,ShotResult.Goal); Check(!m.Finished,"Sudden death waits for equal attempts"); m.NextShot();
            m.Resolve(11,ShotResult.Goal); Check(!m.Finished,"Matched goals extend sudden death"); m.NextShot();
            m.Resolve(12,ShotResult.Miss); m.NextShot(); m.Resolve(13,ShotResult.Goal);
            Check(m.Finished&&m.Winner==1,"Sudden death decided after pair");
            Check(!MatchRules.CanAct(1,0,MatchPhase.Aiming,true),"Keeper cannot shoot");
            Check(!MatchRules.CanAct(0,0,MatchPhase.Flying,false),"Shooter cannot dive");
            Check(!MatchRules.CanAct(0,0,MatchPhase.Flying,true),"Second shot rejected");
            Check(!MatchRules.CanAct(0,0,MatchPhase.Windup,true),"Duplicate during foot windup rejected");
            Check(MatchRules.CanAct(1,0,MatchPhase.Windup,false),"Keeper can react to run-up");
            Check(!MatchRules.CanAct(0,0,MatchPhase.Windup,false),"Shooter cannot dive during windup");
            Check(!MatchRules.CanAct(2,0,MatchPhase.Aiming,false),"Third player rejected by command rule");
            Check(!MatchRules.CanAct(1,0,MatchPhase.Finished,false),"Finished input rejected");
            Check(MatchRules.CanAct(1,0,MatchPhase.Flying,false),"Keeper can react in flight");
            Directory.CreateDirectory("Docs"); File.WriteAllText("Docs/rule-test-results.txt",DateTime.UtcNow.ToString("O")+"\n"+count+" assertions passed in Unity Editor.\nNo device or Relay test implied.\n");
            Debug.Log("PENALTY_RULE_CHECKS_PASSED "+count);
        }
    }
}
