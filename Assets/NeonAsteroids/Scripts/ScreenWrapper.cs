using UnityEngine;

namespace LoneRetr0.NeonAsteroids
{
    public class ScreenWrapper : MonoBehaviour
    {
        public float margin = 0.5f;
        TrailRenderer trail;

        void Awake() => trail = GetComponent<TrailRenderer>();

        void LateUpdate()
        {
            Vector3 p = transform.position;
            Vector2 e = NeonAsteroidsGame.HalfExtents;
            float mx = e.x + margin;
            float my = e.y + margin;
            bool wrapped = false;

            if (p.x > mx) { p.x = -mx; wrapped = true; }
            else if (p.x < -mx) { p.x = mx; wrapped = true; }

            if (p.y > my) { p.y = -my; wrapped = true; }
            else if (p.y < -my) { p.y = my; wrapped = true; }

            if (!wrapped) return;
            transform.position = p;
            if (trail != null) trail.Clear();
        }
    }
}
