using UnityEngine;

namespace LoneRetr0.NeonAsteroids
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        RectTransform rect;
        Rect lastSafe;
        Vector2Int lastScreen;

        void Awake()
        {
            rect = (RectTransform)transform;
            Apply();
        }

        void Update() => Apply();

        void Apply()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen.x <= 0 || screen.y <= 0) return;
            if (screen == lastScreen && Screen.safeArea == lastSafe) return;
            lastScreen = screen;
            lastSafe = Screen.safeArea;

            Rect sa = Screen.safeArea;
            rect.anchorMin = new Vector2(sa.xMin / screen.x, sa.yMin / screen.y);
            rect.anchorMax = new Vector2(sa.xMax / screen.x, sa.yMax / screen.y);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
