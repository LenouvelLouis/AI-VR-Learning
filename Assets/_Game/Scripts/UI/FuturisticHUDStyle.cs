using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MuseumAI.UI
{
    /// <summary>
    /// Applique un style futuriste VR holographique au HUD (montre connectee).
    /// Couleurs cyan/bleu avec effets glow, coherent avec QuizUI et GameOverUI.
    /// </summary>
    public class FuturisticHUDStyle : MonoBehaviour
    {
        #region Color Palette

        [Header("Palette Futuriste")]
        [SerializeField] private Color primaryCyan = new Color(0f, 0.9f, 1f, 1f);
        [SerializeField] private Color secondaryCyan = new Color(0f, 0.7f, 0.85f, 1f);
        [SerializeField] private Color darkBackground = new Color(0.02f, 0.05f, 0.1f, 0.92f);
        [SerializeField] private Color timerNormal = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color timerLow = new Color(1f, 0.3f, 0.4f, 1f);
        [SerializeField] private Color scoreColor = new Color(0f, 1f, 0.6f, 1f);

        [Header("Glow Settings")]
        [SerializeField] private float outlineWidth = 2f;

        #endregion

        #region References

        private Image backgroundImage;
        private TMP_Text timerText;
        private TMP_Text scoreText;
        private TMP_Text progressText;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            FindReferences();
            ApplyFuturisticStyle();
        }

        #endregion

        #region Style Application

        private void FindReferences()
        {
            // Background - Panel
            Transform panel = transform.Find("Panel");
            if (panel != null)
                backgroundImage = panel.GetComponent<Image>();

            // Timer
            Transform timerT = transform.Find("TimerText");
            if (timerT != null)
                timerText = timerT.GetComponent<TMP_Text>();

            // Score
            Transform scoreT = transform.Find("ScoreText");
            if (scoreT != null)
                scoreText = scoreT.GetComponent<TMP_Text>();

            // Progress Text
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

            Debug.Log("[FuturisticHUD] Style futuriste applique!");
        }

        private void StyleBackground()
        {
            if (backgroundImage == null) return;

            backgroundImage.color = darkBackground;

            // Bordure glow cyan
            Outline outline = backgroundImage.gameObject.GetComponent<Outline>();
            if (outline == null)
                outline = backgroundImage.gameObject.AddComponent<Outline>();
            outline.effectColor = primaryCyan;
            outline.effectDistance = new Vector2(outlineWidth, outlineWidth);

            // Glow diffus
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

            // Glow subtil
            Outline glow = timerText.gameObject.GetComponent<Outline>();
            if (glow == null)
                glow = timerText.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(primaryCyan.r, primaryCyan.g, primaryCyan.b, 0.3f);
            glow.effectDistance = new Vector2(2, 2);

            // Shadow
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

            // Glow vert
            Outline glow = scoreText.gameObject.GetComponent<Outline>();
            if (glow == null)
                glow = scoreText.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(scoreColor.r, scoreColor.g, scoreColor.b, 0.4f);
            glow.effectDistance = new Vector2(2, 2);

            // Shadow
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

            // Glow cyan
            Outline glow = progressText.gameObject.GetComponent<Outline>();
            if (glow == null)
                glow = progressText.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(primaryCyan.r, primaryCyan.g, primaryCyan.b, 0.3f);
            glow.effectDistance = new Vector2(2, 2);

            // Shadow
            Outline shadow = progressText.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(1, -1);
        }

        #endregion

        #region Public Methods

        public Color GetPrimaryColor() => primaryCyan;
        public Color GetScoreColor() => scoreColor;
        public Color GetTimerLowColor() => timerLow;

        /// <summary>
        /// Met a jour la couleur du timer (normal ou bas)
        /// </summary>
        public void SetTimerLowMode(bool isLow)
        {
            if (timerText == null) return;
            timerText.color = isLow ? timerLow : timerNormal;
        }

        /// <summary>
        /// Animation de pulse sur un element
        /// </summary>
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

            // Scale up
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                rect.localScale = Vector3.Lerp(originalScale, originalScale * 1.1f, t);
                yield return null;
            }

            // Scale down
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

        #endregion
    }
}
