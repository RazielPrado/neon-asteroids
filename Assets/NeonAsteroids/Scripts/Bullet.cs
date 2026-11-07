using UnityEngine;
using UnityEngine.Rendering;

namespace LoneRetr0.NeonAsteroids
{
    public class Bullet : MonoBehaviour
    {
        public const float Radius = 0.12f;

        public Vector2 Velocity { get; private set; }
        public bool Expired => life <= 0f;

        float life;

        public static Bullet Create(Transform parent, Vector3 position, Vector2 direction,
                                    Vector2 inheritVelocity, float speed, float lifeTime)
        {
            var go = new GameObject("Bullet");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.forward, direction));

            var b = go.AddComponent<Bullet>();
            b.Velocity = direction.normalized * speed + inheritVelocity;
            b.life = lifeTime;

            NeonFactory.CreateLine(go.transform, "Core",
                new[] { new Vector3(0f, -0.15f, 0f), new Vector3(0f, 0.15f, 0f) },
                NeonPalette.Yellow, 0.1f, false);

            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.1f;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = 0.09f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(NeonPalette.Yellow, 0f), new GradientColorKey(NeonPalette.Magenta, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.sharedMaterial = NeonFactory.LineMaterial;
            trail.alignment = LineAlignment.View;
            trail.numCapVertices = 2;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;

            go.AddComponent<ScreenWrapper>().margin = 0.1f;
            return b;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            transform.position += (Vector3)(Velocity * dt);
            life -= dt;
        }
    }
}
