using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MuseumAI.UI
{
    public class FuturisticHUDStyle : MonoBehaviour
    {
        [Header("Palette Futuriste")]
        [SerializeField] private Color primaryCyan = new Color(0f, 0.9f, 1f, 1f);
        [SerializeField] private Color secondaryCyan = new Color(0f, 0.7f, 0.85f, 1f);
        [SerializeField] private Color darkBackground = new Color(0.02f, 0.05f, 0.1f, 0.92f);
        [SerializeField] private Color timerNormal = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color timerLow = new Color(1f, 0.3f, 0.4f, 1f);
        [SerializeField] private Color scoreColor = new Color(0f, 1f, 0.6f, 1f);

        [Header("Glow Settings")]
        [SerializeField] private float outlineWidth = 2f;

        private Image backgroundImage;
        private TMP_Text timerText;
        private TMP_Text scoreText;
        private TMP_Text progressText;

        private void Awake()
        {
            FindReferences();
            ApplyFuturisticStyle();
        }

        private void FindReferences()
        {
            Transform panel = transform.Find("Panel");
            if (panel != null)
                backgroundImage = panel.GetComponent<Image>();

            Transform timerT = transform.Find("TimerText");
            if (timerT != null)
                timerText = timerT.GetComponent<TMP_Text>();

            Transform scoreT = transform.Find("ScoreText");
            if (scoreT != null)
                scoreText = scoreT.GetComponent<TMP_Text>();

            Transform progressT = transform.Find("ProgressText");
            if (progressT != null)
                progressText = progressT.GetComponent<TMP_Text>();
        }

        private void ApplyFuturisticStyle()
        {
            StyleBackground();
            StyleTimer();
            StyleScore();
            StyleProgress();
        }

        private void StyleBackground()
        {
            if (backgroundImage == null) return;

            backgroundImage.color = darkBackground;

            Outline outline = backgroundImage.gameObject.GetComponent<Outline>();
            if (outline == null)
                outline = backgroundImage.gameObject.AddComponent<Outline>();
            outline.effectColor = primaryCyan;
            outline.effectDistance = new Vector2(outlineWidth, outlineWidth);

            Outline glow = backgroundImage.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(primaryCyan.r, primaryCyan.g, primaryCyan.b, 0.2f);
            glow.effectDistance = new Vector2(outlineWidth * 2f, outlineWidth * 2f);
        }

        private void StyleTimer()
        {
            if (timerText == null) return;

            timerText.color = timerNormal;
            timerText.fontSize = 48;
            timerText.fontStyle = FontStyles.Bold;

            Outline glow = timerText.gameObject.GetComponent<Outline>();
            if (glow == null)
                glow = timerText.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(primaryCyan.r, primaryCyan.g, primaryCyan.b, 0.3f);
            glow.effectDistance = new Vector2(2, 2);

            Outline shadow = timerText.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0, 0, 0, 0.7f);
            shadow.effectDistance = new Vector2(1, -1);
        }

        private void StyleScore()
        {
            if (scoreText == null) return;

            scoreText.color = scoreColor;
            scoreText.fontSize = 36;
            scoreText.fontStyle = FontStyles.Bold;

            Outline glow = scoreText.gameObject.GetComponent<Outline>();
            if (glow == null)
                glow = scoreText.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(scoreColor.r, scoreColor.g, scoreColor.b, 0.4f);
            glow.effectDistance = new Vector2(2, 2);

            Outline shadow = scoreText.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(1, -1);
        }

        private void StyleProgress()
        {
            if (progressText == null) return;

            progressText.color = primaryCyan;
            progressText.fontSize = 32;
            progressText.fontStyle = FontStyles.Bold;

            Outline glow = progressText.gameObject.GetComponent<Outline>();
            if (glow == null)
                glow = progressText.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(primaryCyan.r, primaryCyan.g, primaryCyan.b, 0.3f);
            glow.effectDistance = new Vector2(2, 2);

            Outline shadow = progressText.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(1, -1);
        }

        public Color GetPrimaryColor() => primaryCyan;
        public Color GetScoreColor() => scoreColor;
        public Color GetTimerLowColor() => timerLow;

        public void SetTimerLowMode(bool isLow)
        {
            if (timerText == null) return;
            timerText.color = isLow ? timerLow : timerNormal;
        }

        public void PulseElement(TMP_Text element)
        {
            if (element != null)
                StartCoroutine(PulseCoroutine(element));
        }

        private System.Collections.IEnumerator PulseCoroutine(TMP_Text text)
        {
            RectTransform rect = text.GetComponent<RectTransform>();
            if (rect == null) yield break;

            Vector3 originalScale = rect.localScale;
            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                rect.localScale = Vector3.Lerp(originalScale, originalScale * 1.1f, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                rect.localScale = Vector3.Lerp(originalScale * 1.1f, originalScale, t);
                yield return null;
            }

            rect.localScale = originalScale;
        }
    }
}
