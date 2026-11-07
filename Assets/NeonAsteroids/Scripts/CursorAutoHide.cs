using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LoneRetr0.NeonAsteroids
{
    public class CursorAutoHide : MonoBehaviour
    {
        [SerializeField] float hideAfterSeconds = 1.5f;

        float idleTimer;
#if !ENABLE_INPUT_SYSTEM
        Vector3 lastLegacyPosition;
#endif

        void Start()
        {
            Cursor.visible = false;
            idleTimer = hideAfterSeconds;
        }

        void Update()
        {
            if (MouseWasUsed())
            {
                idleTimer = 0f;
                Cursor.visible = true;
                return;
            }

            idleTimer += Time.unscaledDeltaTime;
            if (idleTimer >= hideAfterSeconds && Cursor.visible) Cursor.visible = false;
        }

        bool MouseWasUsed()
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            if (m == null) return false;
            return m.delta.ReadValue().sqrMagnitude > 0.25f
                || m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame;
#else
            Vector3 p = Input.mousePosition;
            bool moved = (p - lastLegacyPosition).sqrMagnitude > 0.25f;
            lastLegacyPosition = p;
            return moved || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
#endif
        }

        void OnDisable() => Cursor.visible = true;
    }
}
