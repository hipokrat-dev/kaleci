using UnityEngine;
namespace PenaltyArena
{
    // Visual animation follows the authoritative snapshot; it never awards a save or goal.
    public sealed class KeeperPresentation : MonoBehaviour
    {
        public PenaltyGame game;
        public Animator animator;
        static readonly string[] States = { "Idle", "CentreLow", "CentreMiddle", "CentreHigh", "LeftLow", "LeftMiddle", "LeftHigh", "RightLow", "RightMiddle", "RightHigh" };
        int lastCode = -1, lastShot = -1;
        float progress;
        void LateUpdate()
        {
            if (game == null || animator == null) return;
            var v = game.View;
            int code = Mathf.Clamp(v.diveCode, 0, States.Length - 1);
            if (v.shotId != lastShot || code != lastCode) {
                lastShot = v.shotId; lastCode = code; progress = v.diveProgress;
                animator.Play(States[code], 0, code == 0 ? 0 : Mathf.Clamp01(progress));
            }
            animator.applyRootMotion = false;
            if (code == 0) { animator.speed = 1; return; }
            animator.speed = 0;
            progress = Mathf.MoveTowards(progress, v.diveProgress, Time.deltaTime * 5);
            animator.Play(States[code], 0, Mathf.Clamp(progress,0,.98f));
            animator.Update(0);
        }
    }
}
