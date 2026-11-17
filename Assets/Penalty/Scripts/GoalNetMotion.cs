using UnityEngine;

namespace PenaltyArena
{
    // A fixed-step tension grid drives the segmented rear net. The frame anchors remain fixed.
    public sealed class GoalNetMotion : MonoBehaviour
    {
        public const int Columns = 47, Rows = 17;
        public PenaltyGame game;
        const float Step = 1f / 120f;
        const float Duration = 2.6f;
        readonly float[] displacement = new float[Columns * Rows];
        readonly float[] velocity = new float[Columns * Rows];
        Mesh net;
        Vector3[] rest, vertices;
        int[] knots;
        int steps;
        double impactAt;

        public static float RearDepth(float y) => Mathf.Lerp(-1.85f, -1.7f, (y - .04f) / 2.4f);

        void LateUpdate()
        {
            var state = game.View;
            float elapsed = (float)(game.NetworkNow - state.netHitAt);
            if (state.netHitAt <= 0 || (elapsed >= Duration && state.netBulge < .005f))
            {
                if (impactAt > 0)
                {
                    net.vertices = rest;
                    impactAt = 0;
                }
                return;
            }

            if (net == null) Initialize();
            if (impactAt != state.netHitAt)
            {
                impactAt = state.netHitAt;
                steps = 0;
                System.Array.Clear(displacement, 0, displacement.Length);
                System.Array.Clear(velocity, 0, velocity.Length);
                Vector3 point = transform.InverseTransformPoint(state.netHitPoint);
                float strength = Mathf.Clamp(state.netHitSpeed * .9f, .1f, 16f);
                for (int y = 1; y < Rows - 1; y++)
                    for (int x = 1; x < Columns - 1; x++)
                    {
                        float dx = x * 7.32f / (Columns - 1) - 3.66f - point.x;
                        float dy = y * 2.4f / (Rows - 1) + .04f - point.y;
                        velocity[y * Columns + x] = strength * Mathf.Exp(-(dx * dx + dy * dy) / .14f);
                    }
            }

            int targetSteps = Mathf.FloorToInt(Mathf.Clamp(elapsed, 0, Duration) / Step);

            while (steps < targetSteps)
            {
                Advance();
                steps++;
            }
            float settle = 1f - Mathf.SmoothStep(0f, 1f, (elapsed - 2.2f) / .4f);
            Vector3 contact = transform.InverseTransformPoint(state.netHitPoint);
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = rest[i];
                if (knots[i] < 0) continue;
                float distance = new Vector2(rest[i].x - contact.x, rest[i].y - contact.y).sqrMagnitude;
                float tension = Mathf.Max(displacement[knots[i]] * settle,
                    state.netBulge * Mathf.Exp(-distance / .32f));
                vertices[i].z -= tension;
                vertices[i].y += Mathf.Abs(tension) * .26f;
            }
            net.vertices = vertices;
        }

        void Initialize()
        {
            net = GetComponent<MeshFilter>().mesh;
            net.MarkDynamic();
            rest = net.vertices;
            vertices = (Vector3[])rest.Clone();
            knots = new int[vertices.Length];
            for (int i = 0; i < rest.Length; i++)
            {
                var p = rest[i];
                int x = Mathf.RoundToInt((p.x + 3.66f) * (Columns - 1) / 7.32f);
                int y = Mathf.RoundToInt((p.y - .04f) * (Rows - 1) / 2.4f);
                knots[i] = Mathf.Abs(p.z - RearDepth(p.y)) < .012f
                    && x > 0 && x < Columns - 1 && y > 0 && y < Rows - 1 ? y * Columns + x : -1;
            }
            var bounds = net.bounds;
            bounds.Expand(2f);
            net.bounds = bounds;
        }

        void Advance()
        {
            for (int y = 1; y < Rows - 1; y++)
                for (int x = 1; x < Columns - 1; x++)
                {
                    int i = y * Columns + x;
                    float stretch = displacement[i - 1] + displacement[i + 1]
                        + displacement[i - Columns] + displacement[i + Columns] - 4f * displacement[i];
                    velocity[i] += Step * (380f * stretch - 48f * displacement[i] - 4.5f * velocity[i]);
                }
            for (int i = 0; i < displacement.Length; i++) displacement[i] += velocity[i] * Step;
        }

        void OnDestroy()
        {
            if (net != null) Destroy(net);
        }
    }
}
