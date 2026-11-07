using UnityEngine;

namespace LoneRetr0.NeonAsteroids
{
    public class NeonFx : MonoBehaviour
    {
        const int RingSegments = 40;

        enum Kind { Ring, Beam }

        Kind kind;
        LineRenderer line;
        Color color;
        float life, maxLife;
        float startRadius, endRadius, startWidth;
        Vector3[] ringPoints;

        public static void Ring(Vector3 center, Color color, float startRadius, float endRadius, float duration, float width)
        {
            var fx = Create("FxRing", center, color, duration);
            fx.kind = Kind.Ring;
            fx.startRadius = startRadius;
            fx.endRadius = endRadius;
            fx.startWidth = width;
            fx.ringPoints = new Vector3[RingSegments];
            fx.line = NeonFactory.CreateLine(fx.transform, "Ring", fx.BuildRing(startRadius), color, width, true);
        }

        public static void Beam(Vector3 from, Vector3 to, Color color, float duration, float width)
        {
            var fx = Create("FxBeam", Vector3.zero, color, duration);
            fx.kind = Kind.Beam;
            fx.startWidth = width;
            fx.line = NeonFactory.CreateLine(fx.transform, "Beam", new[] { from, to }, color, width, false);
        }

        static NeonFx Create(string name, Vector3 position, Color color, float duration)
        {
            var go = new GameObject(name);
            go.transform.SetParent(NeonDebris.Root, false);
            go.transform.position = position;
            var fx = go.AddComponent<NeonFx>();
            fx.color = color;
            fx.life = fx.maxLife = Mathf.Max(0.05f, duration);
            return fx;
        }

        Vector3[] BuildRing(float radius)
        {
            for (int i = 0; i < RingSegments; i++)
            {
                float a = i / (float)RingSegments * Mathf.PI * 2f;
                ringPoints[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            }
            return ringPoints;
        }

        void Update()
        {
            life -= Time.unscaledDeltaTime;
            if (life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float t = 1f - life / maxLife;
            float ease = 1f - (1f - t) * (1f - t);

            if (kind == Kind.Ring)
            {
                line.SetPositions(BuildRing(Mathf.Lerp(startRadius, endRadius, ease)));
                line.widthMultiplier = startWidth * (1f - t * 0.6f);
            }
            else
            {
                line.widthMultiplier = startWidth * (1f - t);
            }

            Color c = color;
            c.a = 1f - t;
            NeonFactory.SetColor(line, c);
        }
    }
}
