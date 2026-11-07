using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LoneRetr0.NeonAsteroids
{
    [DefaultExecutionOrder(-200)]
    public class MobileControls : MonoBehaviour
    {
        public static bool Active { get; private set; }
        public static bool GameplayVisible { get; set; }
        public static Vector2 Stick { get; private set; }
        public static bool FireHeld { get; private set; }
        public static bool HyperspacePressed { get; private set; }
        public static bool PausePressed { get; private set; }
        public static bool TapThisFrame { get; private set; }
        public static Vector2 TapPosition { get; private set; }

        const int NoFinger = int.MinValue;

        struct Pointer
        {
            public int id;
            public Vector2 pos;
            public bool began;
        }

        readonly List<Pointer> pointers = new List<Pointer>();
        int stickFinger = NoFinger, fireFinger = NoFinger;
        Vector2 stickOrigin;

        Rect safe;
        float unit, stickR, fireR, hyperR, pauseR;
        Vector2 stickHome, fireCenter, hyperCenter, pauseCenter;
        Vector2Int lastScreen;
        Rect lastSafe;

        GameObject canvasGO;
        RectTransform stickBase, stickKnob, fireButton, hyperButton, pauseButton;
        Image stickBaseImg, stickKnobImg, fireImg, hyperImg, pauseImg;
        Font font;

        public void Init(Font uiFont)
        {
            font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildVisuals();
            Active = true;
        }

        void OnDisable()
        {
            Active = false;
            Stick = Vector2.zero;
            FireHeld = HyperspacePressed = PausePressed = TapThisFrame = false;
        }

        void Update()
        {
            HyperspacePressed = PausePressed = TapThisFrame = false;
            if (!Active) return;

            Layout();
            Gather();

            bool stickAlive = false, fireAlive = false;
            foreach (var p in pointers)
            {
                if (p.began)
                {
                    if (GameplayVisible && Hit(p.pos, pauseCenter, pauseR * 1.5f)) { PausePressed = true; continue; }
                    if (GameplayVisible && Hit(p.pos, hyperCenter, hyperR * 1.35f)) { HyperspacePressed = true; continue; }

                    if (GameplayVisible && p.pos.x >= safe.center.x)
                    {
                        fireFinger = p.id;
                    }
                    else if (GameplayVisible && stickFinger == NoFinger)
                    {
                        stickFinger = p.id;
                        stickOrigin = p.pos;
                    }
                    else
                    {
                        TapThisFrame = true;
                        TapPosition = p.pos;
                    }
                }

                if (p.id == stickFinger)
                {
                    stickAlive = true;
                    Vector2 d = (p.pos - stickOrigin) / stickR;
                    if (d.magnitude > 1f)
                    {
                        stickOrigin = p.pos - d.normalized * stickR;
                        d = d.normalized;
                    }
                    Stick = d;
                }

                if (p.id == fireFinger) fireAlive = true;
            }

            if (!stickAlive || !GameplayVisible)
            {
                stickFinger = NoFinger;
                Stick = Vector2.zero;
            }
            if (!fireAlive || !GameplayVisible) fireFinger = NoFinger;
            FireHeld = fireFinger != NoFinger;

            UpdateVisuals();
        }

        static bool Hit(Vector2 point, Vector2 center, float radius) => (point - center).sqrMagnitude <= radius * radius;

        void Gather()
        {
            pointers.Clear();
#if ENABLE_INPUT_SYSTEM
            var ts = Touchscreen.current;
            if (ts != null)
            {
                foreach (var t in ts.touches)
                {
                    bool began = t.press.wasPressedThisFrame;
                    if (!t.press.isPressed && !began) continue;
                    pointers.Add(new Pointer { id = t.touchId.ReadValue(), pos = t.position.ReadValue(), began = began });
                }
            }
            if (pointers.Count == 0)
            {
                var m = Mouse.current;
                if (m != null && (m.leftButton.isPressed || m.leftButton.wasPressedThisFrame))
                    pointers.Add(new Pointer { id = -1, pos = m.position.ReadValue(), began = m.leftButton.wasPressedThisFrame });
            }
#else
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) continue;
                pointers.Add(new Pointer { id = t.fingerId, pos = t.position, began = t.phase == TouchPhase.Began });
            }
            if (Input.touchCount == 0 && (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0)))
                pointers.Add(new Pointer { id = -1, pos = Input.mousePosition, began = Input.GetMouseButtonDown(0) });
#endif
        }

        void Layout()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (screen == lastScreen && Screen.safeArea == lastSafe) return;
            lastScreen = screen;
            lastSafe = Screen.safeArea;

            safe = Screen.safeArea;
            unit = Mathf.Min(Screen.width, Screen.height);
            stickR = unit * 0.13f;
            fireR = unit * 0.12f;
            hyperR = unit * 0.075f;
            pauseR = unit * 0.05f;

            stickHome = new Vector2(safe.xMin + unit * 0.30f, safe.yMin + unit * 0.28f);
            fireCenter = new Vector2(safe.xMax - unit * 0.27f, safe.yMin + unit * 0.26f);
            hyperCenter = new Vector2(fireCenter.x - unit * 0.27f, fireCenter.y + unit * 0.10f);
            pauseCenter = new Vector2(safe.xMax - unit * 0.17f, safe.yMax - unit * 0.24f);

            Place(stickBase, stickHome, stickR);
            Place(stickKnob, stickHome, stickR * 0.45f);
            Place(fireButton, fireCenter, fireR);
            Place(hyperButton, hyperCenter, hyperR);
            Place(pauseButton, pauseCenter, pauseR);
        }

        static void Place(RectTransform rt, Vector2 center, float radius)
        {
            rt.anchoredPosition = center;
            rt.sizeDelta = Vector2.one * radius * 2f;
            var label = rt.GetComponentInChildren<Text>();
            if (label == null) return;
            float k = label.text.Length <= 2 ? 0.8f : 0.36f;
            label.resizeTextMaxSize = Mathf.Max(8, Mathf.RoundToInt(radius * k));
        }

        void UpdateVisuals()
        {
            canvasGO.SetActive(GameplayVisible);
            if (!GameplayVisible) return;

            bool usingStick = stickFinger != NoFinger;
            Vector2 baseCenter = usingStick ? stickOrigin : stickHome;
            stickBase.anchoredPosition = baseCenter;
            stickKnob.anchoredPosition = baseCenter + Stick * stickR;

            SetAlpha(stickBaseImg, usingStick ? 0.55f : 0.25f);
            SetAlpha(stickKnobImg, usingStick ? 0.7f : 0.3f);
            SetAlpha(fireImg, FireHeld ? 0.75f : 0.35f);
            SetAlpha(hyperImg, 0.45f);
            SetAlpha(pauseImg, 0.45f);
        }

        static void SetAlpha(Image img, float a)
        {
            var c = img.color;
            c.a = a;
            img.color = c;
        }

        void BuildVisuals()
        {
            canvasGO = new GameObject("MobileControlsUI", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            var ring = MakeCircleSprite(256, 0.08f);
            var disc = MakeCircleSprite(256, 1f);

            stickBase = MakeCircle("StickBase", ring, NeonPalette.Cyan, null, out stickBaseImg);
            stickKnob = MakeCircle("StickKnob", disc, NeonPalette.Cyan, null, out stickKnobImg);
            fireButton = MakeCircle("Fire", ring, NeonPalette.Magenta, "FIRE", out fireImg);
            hyperButton = MakeCircle("Hyperspace", ring, NeonPalette.Purple, "HYPER", out hyperImg);
            pauseButton = MakeCircle("Pause", ring, NeonPalette.Cyan, "II", out pauseImg);

            lastScreen = Vector2Int.zero;
            Layout();
        }

        RectTransform MakeCircle(string name, Sprite sprite, Color color, string label, out Image img)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvasGO.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;

            if (label == null) return rt;
            var textGO = new GameObject("Label", typeof(RectTransform));
            textGO.transform.SetParent(rt, false);
            var trt = (RectTransform)textGO.transform;
            trt.anchorMin = new Vector2(0.2f, 0.3f);
            trt.anchorMax = new Vector2(0.8f, 0.7f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var t = textGO.AddComponent<Text>();
            t.text = label;
            t.font = font;
            t.alignment = TextAnchor.MiddleCenter;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 8;
            t.resizeTextMaxSize = 40;
            t.color = new Color(color.r, color.g, color.b, 0.8f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return rt;
        }

        static Sprite MakeCircleSprite(int size, float thickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[size * size];
            float r = size * 0.5f - 1f;
            float inner = r - r * thickness;
            Vector2 c = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float outer = Mathf.Clamp01(r - d + 0.5f);
                float innerEdge = thickness >= 1f ? 1f : Mathf.Clamp01(d - inner + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(outer * innerEdge * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
