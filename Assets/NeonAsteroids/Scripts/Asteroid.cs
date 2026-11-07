using UnityEngine;

namespace LoneRetr0.NeonAsteroids
{
    public class Asteroid : MonoBehaviour
    {
        static readonly float[] Radii = { 0f, 0.55f, 1.05f, 1.75f };

        public int Size { get; private set; }
        public float Radius { get; private set; }
        public Vector2 Velocity { get; private set; }
        public Vector3[] Points { get; private set; }
        public Color BaseColor { get; private set; }

        public int ScoreValue => Size switch { 3 => 20, 2 => 50, _ => 100 };

        LineRenderer shape;
        float spin;
        float flash;

        public static Asteroid Create(Transform parent, int size, Vector2 position, Vector2 velocity, bool startFlash)
        {
            var go = new GameObject($"Asteroid_L{size}");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));

            var a = go.AddComponent<Asteroid>();
            a.Build(size, velocity, startFlash);
            go.AddComponent<ScreenWrapper>().margin = a.Radius;
            return a;
        }

        void Build(int size, Vector2 velocity, bool startFlash)
        {
            Size = Mathf.Clamp(size, 1, 3);
            Radius = Radii[Size];
            Velocity = velocity;
            spin = Random.Range(-70f, 70f) / Size;

            BaseColor = Size switch
            {
                3 => NeonPalette.Magenta,
                2 => NeonPalette.Purple,
                _ => NeonPalette.Orange
            };

            int n = Random.Range(9, 14);
            var pts = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float ang = (i + Random.Range(-0.3f, 0.3f)) / n * Mathf.PI * 2f;
                float r = Radius * Random.Range(0.7f, 1.1f);
                pts[i] = new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
            }
            Points = pts;

            shape = NeonFactory.CreateLine(transform, "Shape", pts, BaseColor, Size == 3 ? 0.08f : 0.07f, true);

            flash = startFlash ? 1f : 0f;
            if (startFlash) NeonFactory.SetColor(shape, Color.white);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            transform.position += (Vector3)(Velocity * dt);
            transform.Rotate(0f, 0f, spin * dt);

            if (flash > 0f)
            {
                flash = Mathf.Max(0f, flash - dt * 5f);
                NeonFactory.SetColor(shape, Color.Lerp(BaseColor, Color.white, flash));
            }
        }
    }
}
