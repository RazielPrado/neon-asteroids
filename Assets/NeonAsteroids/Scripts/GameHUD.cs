using System.Text;
using TMPro;
using UnityEngine;

namespace LoneRetr0.NeonAsteroids
{
    [DefaultExecutionOrder(-200)]
    public class GameHUD : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] Canvas canvas;

        [Header("Top Bar")]
        [SerializeField] TMP_Text scoreText;
        [SerializeField] TMP_Text highScoreText;
        [SerializeField] TMP_Text livesText;

        [Header("Center")]
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text subtitleText;
        [SerializeField] TMP_Text flashText;

        [Header("Footer")]
        [SerializeField] TMP_Text signatureText;
        [SerializeField] TMP_Text creditText;
        [SerializeField] GameObject controlsBar;
        [SerializeField] TMP_Text controlsText;

        [Header("Pause Menu")]
        [SerializeField] GameObject pausePanel;
        [SerializeField] TMP_Text[] pauseOptions;

        [Header("Colors")]
        [SerializeField] Color titleColorA = new Color(1f, 0.15f, 0.8f);
        [SerializeField] Color titleColorB = new Color(0.1f, 0.95f, 1f);
        [SerializeField] Color optionColor = new Color(0.7f, 0.66f, 0.85f);
        [SerializeField] Color selectedOptionColor = new Color(1f, 0.9f, 0.3f);

        float flashTimer, scorePulse, clock;
        float optionSize, selectedOptionSize;

        void Awake()
        {
            BindCamera();

            if (pauseOptions != null && pauseOptions.Length > 0 && pauseOptions[0] != null)
            {
                optionSize = pauseOptions[0].fontSize;
                selectedOptionSize = optionSize * 1.2f;
            }

            if (pausePanel != null) pausePanel.SetActive(false);
            if (flashText != null) flashText.enabled = false;
        }

        void Start() => BindCamera();

        void BindCamera()
        {
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;
        }

        Camera UICamera => canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        public void SetScore(int score)
        {
            scoreText.text = score.ToString("D6");
            scorePulse = 1f;
        }

        public void SetHighScore(int high) => highScoreText.text = $"HI {high:D6}";

        public void SetLives(int lives)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Mathf.Clamp(lives, 0, 8); i++) sb.Append(i == 0 ? "▲" : " ▲");
            livesText.text = sb.ToString();
        }

        public void ShowCenter(string title, string subtitle)
        {
            titleText.text = title;
            subtitleText.text = subtitle;
        }

        public void ShowSignature(string text) => signatureText.text = text;

        public void ShowMusicCredit(string text) => creditText.text = text;

        public void ShowControls(bool show) => controlsBar.SetActive(show);

        public void SetControlsHelp(string richText) => controlsText.text = richText;

        public void ShowPause(bool show)
        {
            pausePanel.SetActive(show);
            controlsBar.SetActive(show);
            controlsBar.transform.SetAsLastSibling();
        }

        public void SetPauseMenu(string[] options, int selected)
        {
            for (int i = 0; i < pauseOptions.Length; i++)
            {
                bool exists = i < options.Length;
                pauseOptions[i].gameObject.SetActive(exists);
                if (!exists) continue;

                bool isSelected = i == selected;
                pauseOptions[i].text = isSelected ? $">  {options[i]}  <" : options[i];
                pauseOptions[i].color = isSelected ? selectedOptionColor : optionColor;
                pauseOptions[i].fontSize = isSelected ? selectedOptionSize : optionSize;
            }
        }

        public int PauseOptionAt(Vector2 screenPosition)
        {
            if (!pausePanel.activeSelf) return -1;
            for (int i = 0; i < pauseOptions.Length; i++)
            {
                if (!pauseOptions[i].gameObject.activeSelf) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(pauseOptions[i].rectTransform, screenPosition, UICamera))
                    return i;
            }
            return -1;
        }

        public void Flash(string message, float duration)
        {
            flashText.text = message;
            flashTimer = duration;
        }

        public void Tick(float dt)
        {
            clock += dt;

            scorePulse = Mathf.MoveTowards(scorePulse, 0f, dt * 4f);
            scoreText.rectTransform.localScale = Vector3.one * (1f + 0.15f * scorePulse);

            var c = subtitleText.color;
            c.a = 0.55f + 0.45f * Mathf.Sin(clock * 5f);
            subtitleText.color = c;

            titleText.color = Color.Lerp(titleColorA, titleColorB, 0.5f + 0.5f * Mathf.Sin(clock * 0.9f));

            flashTimer -= dt;
            flashText.enabled = flashTimer > 0f;
            if (flashTimer > 0f)
            {
                var fc = flashText.color;
                fc.a = Mathf.Clamp01(flashTimer * 2f);
                flashText.color = fc;
            }
        }
    }
}
