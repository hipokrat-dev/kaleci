using UnityEngine;
namespace PenaltyArena
{
    public static class MotionTiming
    {
        public const float StrikeContact = .58f, StrikeEnd = 2.4f;
        public const float SaveReach = .55f, SaveLand = 1.05f, SaveStand = 2.25f, SaveEnd = 3.15f;
        public static float Smooth(float from,float to,float time) => Mathf.SmoothStep(0,1,Mathf.InverseLerp(from,to,time));
        public static float SaveTime(float elapsed, float reachDuration)
        {
            if (elapsed >= reachDuration) return SaveReach + elapsed - reachDuration;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(.1f, reachDuration));
            // The keeper loads their weight first, then accelerates into the reach.
            float load = Mathf.InverseLerp(.65f, 1.15f, reachDuration) * .38f;
            return SaveReach * progress * (1f - load + load * progress);
        }
        public static float SaveLength(float reachDuration) => SaveEnd-SaveReach+Mathf.Max(.1f,reachDuration);
        public static float Travel(float time) => Smooth(0,SaveReach,time)*(1-Smooth(SaveStand,SaveEnd,time));
        public static int DiveCode(float x,float y) => (y<.25f?1:y<.7f?2:3)+(x<-.15f?3:x>.15f?6:0);
    }
}
