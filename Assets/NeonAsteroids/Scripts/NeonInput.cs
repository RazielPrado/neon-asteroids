using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LoneRetr0.NeonAsteroids
{
    public static class NeonInput
    {
#if ENABLE_INPUT_SYSTEM
        static Keyboard K => Keyboard.current;
        static Gamepad G => Gamepad.current;

        static float RawTurn
        {
            get
            {
                float t = 0f;
                var k = K;
                if (k != null)
                {
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) t += 1f;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) t -= 1f;
                }
                var g = G;
                if (g != null) t -= g.leftStick.x.ReadValue();
                return Mathf.Clamp(t, -1f, 1f);
            }
        }

        static bool RawThrust =>
            (K != null && (K.wKey.isPressed || K.upArrowKey.isPressed)) ||
            (G != null && (G.leftStick.y.ReadValue() > 0.4f || G.leftTrigger.isPressed));

        static bool RawFire =>
            (K != null && (K.spaceKey.isPressed || K.jKey.isPressed)) ||
            (G != null && (G.rightTrigger.isPressed || G.buttonSouth.isPressed));

        static bool RawHyperspacePressed =>
            (K != null && (K.leftShiftKey.wasPressedThisFrame || K.kKey.wasPressedThisFrame)) ||
            (G != null && G.buttonEast.wasPressedThisFrame);

        static bool RawStartPressed =>
            (K != null && (K.spaceKey.wasPressedThisFrame || K.enterKey.wasPressedThisFrame)) ||
            (G != null && (G.startButton.wasPressedThisFrame || G.buttonSouth.wasPressedThisFrame));

        static bool RawPausePressed =>
            (K != null && (K.escapeKey.wasPressedThisFrame || K.pKey.wasPressedThisFrame)) ||
            (G != null && G.startButton.wasPressedThisFrame);

        static bool RawBackPressed =>
            (K != null && K.escapeKey.wasPressedThisFrame) ||
            (G != null && G.selectButton.wasPressedThisFrame);

        static bool RawMenuUp =>
            (K != null && (K.wKey.wasPressedThisFrame || K.upArrowKey.wasPressedThisFrame)) ||
            (G != null && G.dpad.up.wasPressedThisFrame);

        static bool RawMenuDown =>
            (K != null && (K.sKey.wasPressedThisFrame || K.downArrowKey.wasPressedThisFrame)) ||
            (G != null && G.dpad.down.wasPressedThisFrame);

        static bool RawConfirmPressed =>
            (K != null && (K.enterKey.wasPressedThisFrame || K.spaceKey.wasPressedThisFrame)) ||
            (G != null && G.buttonSouth.wasPressedThisFrame);
#else
        static float RawTurn
        {
            get
            {
                float t = 0f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) t += 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) t -= 1f;
                return t;
            }
        }

        static bool RawThrust => Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        static bool RawFire => Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J);
        static bool RawHyperspacePressed => Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K);
        static bool RawStartPressed => Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
        static bool RawPausePressed => Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
        static bool RawBackPressed => Input.GetKeyDown(KeyCode.Escape);
        static bool RawMenuUp => Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        static bool RawMenuDown => Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        static bool RawConfirmPressed => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#endif

        public static float Turn => RawTurn;

        public static bool TryGetAim(out Vector2 direction)
        {
            direction = Vector2.zero;
            if (!MobileControls.Active || MobileControls.Stick.magnitude < 0.25f) return false;
            direction = MobileControls.Stick.normalized;
            return true;
        }

        public static bool Thrust => RawThrust || (MobileControls.Active && MobileControls.Stick.magnitude > 0.6f);
        public static bool Fire => RawFire || MobileControls.FireHeld;
        public static bool HyperspacePressed => RawHyperspacePressed || MobileControls.HyperspacePressed;
        public static bool StartPressed => RawStartPressed || (MobileControls.Active && MobileControls.TapThisFrame);
        public static bool PausePressed => RawPausePressed || MobileControls.PausePressed;
        public static bool BackPressed => RawBackPressed;
        public static bool MenuUp => RawMenuUp;
        public static bool MenuDown => RawMenuDown;
        public static bool ConfirmPressed => RawConfirmPressed;

        public static bool TryGetPointerDown(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;
            if (MobileControls.Active)
            {
                if (!MobileControls.TapThisFrame) return false;
                screenPosition = MobileControls.TapPosition;
                return true;
            }
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            if (m == null || !m.leftButton.wasPressedThisFrame) return false;
            screenPosition = m.position.ReadValue();
            return true;
#else
            if (!Input.GetMouseButtonDown(0)) return false;
            screenPosition = Input.mousePosition;
            return true;
#endif
        }
    }
}
