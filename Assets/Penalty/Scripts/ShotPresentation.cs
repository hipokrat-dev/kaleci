using UnityEngine;

namespace PenaltyArena
{
    // The camera and kick cue follow the same server clock as the actors.
    public sealed class ShotPresentation : MonoBehaviour
    {
        static readonly Vector3 PenaltySpot = new Vector3(0f, .115f, 11f);
        PenaltyGame game;
        AudioSource speaker;
        AudioClip kick;
        double playedKickAt;

        void Start()
        {
            game = GetComponent<PenaltyGame>();
            if (Application.isBatchMode) return;
            speaker = gameObject.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 0f;
            kick = CreateKick();
        }

        void LateUpdate()
        {
            if (speaker == null || !game.room.Connected) return;
            var state = game.View;
            if (state.shotStartedAt <= 0) return;
            float elapsed = (float)(game.NetworkNow - state.shotStartedAt);
            if (elapsed <= .12f || elapsed >= 2.85f) return;

            float contactAge = elapsed - MotionTiming.StrikeContact;
            if (contactAge >= 0f && contactAge < .24f && playedKickAt != state.shotStartedAt)
            {
                playedKickAt = state.shotStartedAt;
                speaker.PlayOneShot(kick, .55f);
            }

            var ball = game.ball.transform.position;
            var close = PenaltySpot + new Vector3(-.84f, .31f, .63f);
            var follow = new Vector3(Mathf.Clamp(ball.x * .12f - .18f, -.75f, .75f),
                1.55f, Mathf.Max(10.4f, ball.z + 5.4f));
            float chase = MotionTiming.Smooth(.59f, .97f, elapsed);
            Vector3 position = Vector3.Lerp(close, follow, chase);
            Vector3 closeTarget = PenaltySpot + new Vector3(0f, .065f, 0f);
            Vector3 goalTarget = new Vector3(Mathf.Clamp(ball.x * .11f, -.55f, .55f),
                Mathf.Clamp(ball.y * .32f + .68f, .75f, 1.7f), 0f);
            Vector3 target = Vector3.Lerp(closeTarget, Vector3.Lerp(ball, goalTarget, .89f), chase);

            if (contactAge >= 0f && contactAge < .15f)
                position.y += Mathf.Sin(contactAge * 60f) * Mathf.Exp(-contactAge * 24f) * .023f;

            Vector3 normalPosition = game.Shooting ? game.shooterCameraPosition : game.keeperCameraPosition;
            Vector3 normalTarget = game.Shooting ? game.shooterCameraTarget : game.keeperCameraTarget;
            float blend = MotionTiming.Smooth(.12f, .46f, elapsed)
                * (1f - MotionTiming.Smooth(2.12f, 2.85f, elapsed));
            if (!game.Shooting)
            {
                // Cut across the pitch instead of flying through the goal and the keeper.
                if (elapsed < .25f || elapsed > 2.45f) return;
                blend = 1f;
            }
            position = Vector3.Lerp(normalPosition, position, blend);
            target = Vector3.Lerp(normalTarget, target, blend);
            game.gameCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
            if (chase > .1f && blend > .5f) game.SetGoalNetVisible(true);
        }

        static AudioClip CreateKick()
        {
            const int rate = 24000;
            var data = new float[rate / 5];
            var noise = new System.Random(37);
            float lowNoise = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float sample = (float)noise.NextDouble() * 2f - 1f;
                lowNoise = Mathf.Lerp(lowNoise, sample, .18f);
                float body = Mathf.Sin(2f * Mathf.PI * (112f * t - 130f * t * t)) * Mathf.Exp(-39f * t);
                float slap = lowNoise * Mathf.Exp(-105f * t);
                data[i] = Mathf.Clamp((body * .63f + slap * .5f) * Mathf.Min(1f, t * 1600f), -1f, 1f);
            }
            var clip = AudioClip.Create("Foot to ball", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void OnDestroy()
        {
            if (kick != null) Destroy(kick);
        }
    }
}
