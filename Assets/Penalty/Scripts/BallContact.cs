using UnityEngine;
namespace PenaltyArena
{
    public sealed class BallContact : MonoBehaviour
    {
        public PenaltyGame game;
        void OnCollisionEnter(Collision c) { if (game != null) game.Contact(c.transform.IsChildOf(game.keeper)); }
    }
}
