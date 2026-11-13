using UnityEngine;
namespace PenaltyArena
{
    public sealed class BallContact : MonoBehaviour
    {
        public PenaltyGame game;
        void OnCollisionEnter(Collision collision)
        {
            if (game == null) return;
            game.Contact(collision.transform.IsChildOf(game.keeper));
            if (collision.collider.name == "NetCollision" && collision.contactCount > 0)
                game.ContactNet(collision.GetContact(0).point, Mathf.Abs(collision.relativeVelocity.z));
        }
    }
}
