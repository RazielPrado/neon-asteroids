using UnityEngine;

namespace LoneRetr0.NeonAsteroids
{
    public class NeonDebris : MonoBehaviour
    {
        public static Transform Root;

        LineRenderer line;
        Color color;
        Vector2 velocity;
        float spin, life, maxLife;
        const float Drag = 1.2f;

        public static void Spawn(Vector3 position, float angleDeg, Vector3 localA, Vector3 localB,
                                 Color color, Vector2 velocity, float spin, float life, float width)
        {
            var go = new GameObject("Debris");
            go.transform.SetParent(Root, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angleDeg));

            var d = go.AddComponent<NeonDebris>();
            d.line = NeonFactory.CreateLine(go.transform, "Seg", new[] { localA, localB }, color, width, false);
            d.color = color;
            d.velocity = velocity;
            d.spin = spin;
            d.life = d.maxLife = Mathf.Max(0.05f, life);
        }

        public static void Spark(Vector3 position, Color color, Vector2 velocity, float length, float life)
        {
            float ang = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            Spawn(position, ang, new Vector3(-length * 0.5f, 0f, 0f), new Vector3(length * 0.5f, 0f, 0f),
                  color, velocity, 0f, life, 0.04f);
        }

        public static void Burst(Vector3 position, Color color, int count, float speed, float length, float life)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                if (dir == Vector2.zero) dir = Vector2.up;
                Spark(position, color, dir * speed * Random.Range(0.3f, 1f),
                      length * Random.Range(0.5f, 1.2f), life * Random.Range(0.6f, 1.2f));
            }
        }

        public static void Shatter(Transform source, Vector3[] localPoints, bool loop, Color color,
                                   Vector2 baseVelocity, float speed, float life)
        {
            int segments = loop ? localPoints.Length : localPoints.Length - 1;
            Vector3 center = source.position;

            for (int i = 0; i < segments; i++)
            {
                Vector3 a = source.TransformPoint(localPoints[i]);
                Vector3 b = source.TransformPoint(localPoints[(i + 1) % localPoints.Length]);
                Vector3 mid = (a + b) * 0.5f;
                Vector2 outward = ((Vector2)(mid - center)).normalized;

                Spawn(mid, 0f, a - mid, b - mid, color,
                      baseVelocity + outward * speed * Random.Range(0.4f, 1.1f),
                      Random.Range(-240f, 240f), life * Random.Range(0.7f, 1.3f), 0.06f);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            transform.position += (Vector3)(velocity * dt);
            velocity *= 1f / (1f + Drag * dt);
            transform.Rotate(0f, 0f, spin * dt);

            life -= dt;
            if (life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Color c = color;
            float k = life / maxLife;
            c.a = k * k;
            NeonFactory.SetColor(line, c);
        }
    }
}
