using System;
using System.Collections.Generic;
namespace PenaltyArena
{
    public enum ShotResult { Goal, Save, Miss }
    public enum MatchPhase { Lobby, Aiming, Flying, Result, Finished, Disconnected, Windup }
    [Serializable] public sealed class MatchRules
    {
        public int Shooter { get; private set; }
        public int ShotId { get; private set; }
        public bool Finished { get; private set; }
        public int Winner => Finished ? (Score[0] > Score[1] ? 0 : 1) : -1;
        public readonly int[] Score = new int[2];
        public readonly List<ShotResult>[] History = { new List<ShotResult>(), new List<ShotResult>() };
        bool resolved;
        public bool Resolve(int shotId, ShotResult result)
        {
            if (Finished || resolved || shotId != ShotId) return false;
            resolved = true;
            History[Shooter].Add(result);
            if (result == ShotResult.Goal) Score[Shooter]++;
            Finished = History[0].Count >= 5 && History[0].Count == History[1].Count && Score[0] != Score[1];
            return true;
        }
        public bool NextShot()
        {
            if (!resolved || Finished) return false;
            ShotId++; Shooter = ShooterForShot(ShotId); resolved = false; return true;
        }
        public static int ShooterForShot(int shotId) => shotId < 5 ? 0 : shotId < 10 ? 1 : (shotId - 10) % 2;
        public static int AttemptNumber(int shotId) => shotId < 10 ? shotId % 5 + 1 : 6 + (shotId - 10) / 2;
        public static bool CanAct(int actor, int shooter, MatchPhase phase, bool shot)
        {
            if (actor < 0 || actor > 1) return false;
            return phase == MatchPhase.Aiming && (shot ? actor == shooter : actor != shooter);
        }
    }
}
