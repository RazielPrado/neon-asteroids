using UnityEngine;

namespace LoneRetr0.NeonAsteroids
{
    public class PlayerShip : MonoBehaviour
    {
        public const float Radius = 0.4f;

        public static readonly Vector3[] HullPoints =
        {
            new Vector3(0f, 0.65f, 0f),
            new Vector3(0.45f, -0.45f, 0f),
            new Vector3(0f, -0.22f, 0f),
            new Vector3(-0.45f, -0.45f, 0f),
        };

        static readonly Vector3[] FlamePoints =
        {
            new Vector3(0.18f, -0.32f, 0f),
            new Vector3(0f, -0.7f, 0f),
            new Vector3(-0.18f, -0.32f, 0f),
        };

        [Header("Movement")]
        [SerializeField] float turnSpeed = 280f;
        [SerializeField] float thrustAcceleration = 16f;
        [SerializeField] float maxSpeed = 12f;
        [SerializeField] float drag = 0.55f;

        [Header("Shooting")]
        [SerializeField] float fireCooldown = 0.13f;
        [SerializeField] float bulletSpeed = 20f;
        [SerializeField] float bulletLife = 0.85f;

        [Header("Hyperspace")]
        [SerializeField] float hyperspaceCooldown = 2f;

        public Vector2 Velocity { get; private set; }
        public bool Invulnerable => invulnerableTimer > 0f;

        NeonAsteroidsGame game;
        LineRenderer hull, flame, shield;
        float fireTimer, invulnerableTimer, sparkTimer, hyperTimer;
        float flashTimer, flashDuration = 1f;

        public void Init(NeonAsteroidsGame owner)
        {
            game = owner;
            hull = NeonFactory.CreateLine(transform, "Hull", HullPoints, NeonPalette.Cyan, 0.08f, true);
            flame = NeonFactory.CreateLine(transform, "Flame", FlamePoints, NeonPalette.Orange, 0.06f, false);
            flame.enabled = false;

            var circle = new Vector3[20];
            for (int i = 0; i < circle.Length; i++)
            {
                float a = i / (float)circle.Length * Mathf.PI * 2f;
                circle[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.85f;
            }
            shield = NeonFactory.CreateLine(transform, "Shield", circle, NeonPalette.Purple, 0.035f, true);
            shield.enabled = false;

            gameObject.AddComponent<ScreenWrapper>().margin = 0.5f;
        }

        public void Respawn(Vector3 position, float invulnerability)
        {
            transform.SetPositionAndRotation(position, Quaternion.identity);
            Velocity = Vector2.zero;
            invulnerableTimer = invulnerability;
            fireTimer = 0.2f;
            gameObject.SetActive(true);
        }

        public void GrantInvulnerability(float seconds)
        {
            invulnerableTimer = Mathf.Max(invulnerableTimer, seconds);
        }

        public void FlashWhite(float seconds)
        {
            flashDuration = Mathf.Max(0.01f, seconds);
            flashTimer = flashDuration;
        }

        public void Teleport(Vector3 position)
        {
            transform.position = position;
            Velocity *= 0.3f;
        }

        void Update()
        {
            if (Time.timeScale == 0f) return;
            float dt = Time.deltaTime;

            if (NeonInput.TryGetAim(out Vector2 aim))
            {
                float target = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg - 90f;
                float z = Mathf.MoveTowardsAngle(transform.eulerAngles.z, target, turnSpeed * 1.6f * dt);
                transform.rotation = Quaternion.Euler(0f, 0f, z);
            }
            else
            {
                transform.Rotate(0f, 0f, NeonInput.Turn * turnSpeed * dt);
            }

            bool thrusting = NeonInput.Thrust;
            Vector2 v = Velocity;
            if (thrusting) v += (Vector2)transform.up * thrustAcceleration * dt;
            v = Vector2.ClampMagnitude(v, maxSpeed);
            v *= 1f / (1f + drag * dt);
            Velocity = v;
            transform.position += (Vector3)(v * dt);

            flame.enabled = thrusting;
            if (thrusting)
            {
                flame.SetPosition(1, new Vector3(Random.Range(-0.05f, 0.05f), -0.65f - Random.value * 0.4f, 0f));
                NeonFactory.SetColor(flame, Color.Lerp(NeonPalette.Orange, NeonPalette.Magenta, Random.value));

                sparkTimer -= dt;
                if (sparkTimer <= 0f)
                {
                    sparkTimer = 0.035f;
                    Vector3 tail = transform.TransformPoint(new Vector3(0f, -0.6f, 0f));
                    Vector2 back = -(Vector2)transform.up + Random.insideUnitCircle * 0.35f;
                    NeonDebris.Spark(tail, NeonPalette.Orange, Velocity + back * Random.Range(4f, 7f), 0.18f, 0.35f);
                }
            }

            fireTimer -= dt;
            if (NeonInput.Fire && fireTimer <= 0f)
            {
                Vector3 muzzle = transform.TransformPoint(new Vector3(0f, 0.7f, 0f));
                if (game.TrySpawnBullet(muzzle, transform.up, Velocity, bulletSpeed, bulletLife))
                    fireTimer = fireCooldown;
            }

            hyperTimer -= dt;
            if (NeonInput.HyperspacePressed && hyperTimer <= 0f)
            {
                hyperTimer = hyperspaceCooldown;
                game.Hyperspace(this);
            }

            if (flashTimer > 0f)
            {
                flashTimer = Mathf.Max(0f, flashTimer - dt);
                NeonFactory.SetColor(hull, Color.Lerp(NeonPalette.Cyan, Color.white, flashTimer / flashDuration));
            }

            if (invulnerableTimer > 0f)
            {
                invulnerableTimer -= dt;
                hull.enabled = Mathf.Repeat(invulnerableTimer, 0.2f) > 0.08f;
                shield.enabled = true;
            }
            else
            {
                hull.enabled = true;
                shield.enabled = false;
            }
        }
    }
}
