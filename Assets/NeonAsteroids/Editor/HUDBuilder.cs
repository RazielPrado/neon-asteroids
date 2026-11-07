using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LoneRetr0.NeonAsteroids.EditorTools
{
    public static class HUDBuilder
    {
        static readonly Color Cyan = new Color(0.1f, 0.95f, 1f);
        static readonly Color Magenta = new Color(1f, 0.15f, 0.8f);
        static readonly Color Yellow = new Color(1f, 0.9f, 0.3f);
        static readonly Color SoftText = new Color(0.7f, 0.66f, 0.85f);
        static readonly Color SoftPink = new Color(0.85f, 0.53f, 0.77f);
        static readonly Color PanelColor = new Color(0.02f, 0f, 0.06f, 0.82f);

        const string ControlsHelp =
            "<color=#FFD84D>A/D</color> ROTATE     <color=#FFD84D>W</color> THRUST     " +
            "<color=#FFD84D>SPACE</color> SHOOT     <color=#FFD84D>SHIFT</color> HYPERSPACE     " +
            "<color=#FFD84D>ESC</color> PAUSE";

        [MenuItem("Tools/Neon Asteroids/Create HUD")]
        static void Create()
        {
            if (Object.FindFirstObjectByType<GameHUD>() != null)
            {
                EditorUtility.DisplayDialog("Create HUD", "The scene already has a GameHUD.", "OK");
                return;
            }

            TMP_FontAsset uiFont = FindFont("Orbitron");
            TMP_FontAsset titleFont = FindFont("Audiowide");
            if (uiFont == null) uiFont = TMP_Settings.defaultFontAsset;
            if (titleFont == null) titleFont = uiFont;
            TMP_FontAsset symbolFont = TMP_Settings.defaultFontAsset != null ? TMP_Settings.defaultFontAsset : uiFont;
            Material shadow = ShadowMaterial(uiFont);

            var root = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(root, "Create HUD");

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 10;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var safe = Stretch("SafeArea", root.transform);
            safe.gameObject.AddComponent<SafeArea>();

            var topLeft = new Vector2(0f, 1f);
            var topCenter = new Vector2(0.5f, 1f);
            var topRight = new Vector2(1f, 1f);
            var center = new Vector2(0.5f, 0.5f);
            var wide = new Vector2(1600f, 140f);

            var score = Label(safe, "Score", topLeft, new Vector2(70f, -45f), new Vector2(600f, 80f), 50, Cyan, TextAlignmentOptions.TopLeft, uiFont, null, "000000");
            var high = Label(safe, "HighScore", topCenter, new Vector2(0f, -45f), new Vector2(600f, 60f), 32, SoftText, TextAlignmentOptions.Top, uiFont, shadow, "HI 000000");
            var lives = Label(safe, "Lives", topRight, new Vector2(-70f, -45f), new Vector2(600f, 70f), 44, Cyan, TextAlignmentOptions.TopRight, symbolFont, null, "▲ ▲ ▲");

            var title = Label(safe, "Title", center, new Vector2(0f, 80f), new Vector2(1600f, 200f), 110, Magenta, TextAlignmentOptions.Center, titleFont, null, "NEON ASTEROIDS");
            var subtitle = Label(safe, "Subtitle", center, new Vector2(0f, -50f), wide, 40, Cyan, TextAlignmentOptions.Center, uiFont, null, "PRESS SPACE");
            var flash = Label(safe, "Flash", center, new Vector2(0f, 220f), wide, 64, Yellow, TextAlignmentOptions.Center, titleFont, null, "WAVE 1");

            var signature = Label(safe, "Signature", Vector2.zero, new Vector2(110f, 95f), new Vector2(700f, 60f), 32, SoftPink, TextAlignmentOptions.BottomLeft, uiFont, shadow, "Made by LoneRetr0");
            var credit = Label(safe, "MusicCredit", new Vector2(1f, 0f), new Vector2(-110f, 95f), new Vector2(700f, 60f), 32, SoftPink, TextAlignmentOptions.BottomRight, uiFont, shadow, "Music by VOiD1 Gaming");

            var controlsBar = Panel("ControlsBar", root.transform, new Vector2(0.5f, 0f), new Vector2(0f, 175f), new Vector2(1500f, 64f), PanelColor);
            var controls = Label(controlsBar.transform, "ControlsText", center, Vector2.zero, new Vector2(1500f, 64f), 26, SoftText, TextAlignmentOptions.Center, uiFont, shadow, ControlsHelp);

            var pausePanel = Panel("PausePanel", root.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.02f, 0f, 0.05f, 0.8f));
            var prt = (RectTransform)pausePanel.transform;
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = prt.offsetMax = Vector2.zero;

            Label(pausePanel.transform, "PauseTitle", center, new Vector2(0f, 230f), new Vector2(1000f, 140f), 100, Magenta, TextAlignmentOptions.Center, titleFont, null, "PAUSED");

            var box = Panel("MenuBox", pausePanel.transform, center, new Vector2(0f, -60f), new Vector2(720f, 360f), new Color(0.03f, 0f, 0.08f, 0.9f));
            var border = box.AddComponent<Outline>();
            border.effectColor = new Color(Magenta.r, Magenta.g, Magenta.b, 0.6f);
            border.effectDistance = new Vector2(3f, 3f);

            string[] names = { "RESUME", "RESTART", "MAIN MENU", "QUIT GAME" };
            var options = new TMP_Text[names.Length];
            for (int i = 0; i < names.Length; i++)
                options[i] = Label(box.transform, $"Option_{names[i].Replace(' ', '_')}", center, new Vector2(0f, 120f - i * 80f),
                                   new Vector2(680f, 76f), 44, SoftText, TextAlignmentOptions.Center, uiFont, shadow, names[i]);

            pausePanel.SetActive(false);

            var hud = root.AddComponent<GameHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("canvas").objectReferenceValue = canvas;
            so.FindProperty("scoreText").objectReferenceValue = score;
            so.FindProperty("highScoreText").objectReferenceValue = high;
            so.FindProperty("livesText").objectReferenceValue = lives;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("subtitleText").objectReferenceValue = subtitle;
            so.FindProperty("flashText").objectReferenceValue = flash;
            so.FindProperty("signatureText").objectReferenceValue = signature;
            so.FindProperty("creditText").objectReferenceValue = credit;
            so.FindProperty("controlsBar").objectReferenceValue = controlsBar;
            so.FindProperty("controlsText").objectReferenceValue = controls;
            so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            var optionsProp = so.FindProperty("pauseOptions");
            optionsProp.arraySize = options.Length;
            for (int i = 0; i < options.Length; i++)
                optionsProp.GetArrayElementAtIndex(i).objectReferenceValue = options[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            var game = Object.FindFirstObjectByType<NeonAsteroidsGame>();
            if (game != null)
            {
                var gso = new SerializedObject(game);
                gso.FindProperty("hud").objectReferenceValue = hud;
                gso.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;

            EditorUtility.DisplayDialog("Create HUD",
                "HUD created" + (game != null ? " and linked to NeonAsteroidsGame." : ". Assign it to the Hud field of NeonAsteroidsGame.") +
                "\n\nSave the scene (Ctrl+S).", "OK");
        }

        static TMP_FontAsset FindFont(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:TMP_FontAsset {name}"))
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (font != null) return font;
            }
            return null;
        }

        static Material ShadowMaterial(TMP_FontAsset font)
        {
            if (font == null || font.material == null) return null;

            string folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(font)).Replace('\\', '/');
            string path = $"{folder}/{font.name} Shadow.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var mat = new Material(font.material);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.85f));
            mat.SetFloat("_UnderlayOffsetX", 0.5f);
            mat.SetFloat("_UnderlayOffsetY", -0.5f);
            mat.SetFloat("_UnderlaySoftness", 0.2f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static GameObject Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        static TextMeshProUGUI Label(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, float fontSize,
                                     Color color, TextAlignmentOptions alignment, TMP_FontAsset font, Material material, string text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            if (material != null) t.fontSharedMaterial = material;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = alignment;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }
    }
}
