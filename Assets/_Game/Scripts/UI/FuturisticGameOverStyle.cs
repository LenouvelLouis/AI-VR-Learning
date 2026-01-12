using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MuseumAI.UI
{
    /// <summary>
    /// Applique un style futuriste VR holographique au GameOver UI.
    /// Couleurs cyan/bleu avec effets glow, coherent avec QuizUI.
    /// </summary>
    public class FuturisticGameOverStyle : MonoBehaviour
    {
        #region Color Palette

        [Header("Palette Futuriste")]
        [SerializeField] private Color primaryCyan = new Color(0f, 0.9f, 1f, 1f);
        [SerializeField] private Color secondaryCyan = new Color(0f, 0.7f, 0.85f, 1f);
        [SerializeField] private Color darkBackground = new Color(0.02f, 0.05f, 0.1f, 0.95f);
        [SerializeField] private Color buttonNormal = new Color(0.05f, 0.12f, 0.18f, 0.95f);
        [SerializeField] private Color buttonHighlight = new Color(0f, 0.25f, 0.35f, 1f);
        [SerializeField] private Color buttonPressed = new Color(0f, 0.4f, 0.5f, 1f);
        [SerializeField] private Color victoryGold = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color timeUpRed = new Color(1f, 0.3f, 0.4f, 1f);
        [SerializeField] private Color scoreGlow = new Color(0f, 1f, 0.6f, 1f);

        [Header("Glow Settings")]
        [SerializeField] private float outlineWidth = 4f;

        #endregion

        #region References

        private Image backgroundImage;
        private TMP_Text titleText;
        private TMP_Text finalScoreText;
        private TMP_Text paintingsCountText;
        private TMP_Text[] labelTexts;
        private Button retryButton;
        private Button quitButton;

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
            backgroundImage = transform.Find("Background")?.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = GetComponentInChildren<Image>();
            }

            Transform titleT = transform.Find("TitleText");
            if (titleT != null)
                titleText = titleT.GetComponent<TMP_Text>();

            Transform statsPanel = transform.Find("StatsPanel");
            if (statsPanel != null)
            {
                finalScoreText = statsPanel.Find("FinalScoreText")?.GetComponent<TMP_Text>();
                paintingsCountText = statsPanel.Find("PaintingsCountText")?.GetComponent<TMP_Text>();
            }

            if (finalScoreText == null)
                finalScoreText = transform.Find("FinalScoreText")?.GetComponent<TMP_Text>();
            if (paintingsCountText == null)
                paintingsCountText = transform.Find("PaintingsCountText")?.GetComponent<TMP_Text>();

            Transform buttonsPanel = transform.Find("ButtonsPanel");
            if (buttonsPanel != null)
            {
                retryButton = buttonsPanel.Find("RetryButton")?.GetComponent<Button>();
                quitButton = buttonsPanel.Find("QuitButton")?.GetComponent<Button>();
            }

            if (retryButton == null)
                retryButton = transform.Find("RetryButton")?.GetComponent<Button>();
            if (quitButton == null)
                quitButton = transform.Find("QuitButton")?.GetComponent<Button>();

            labelTexts = GetComponentsInChildren<TMP_Text>();
        }

        private void ApplyFuturisticStyle()
        {
            StyleBackground();
            StyleTitle();
            StyleScoreDisplay();
            StyleButtons();
            StyleLabels();

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

            Outline glowOutline = backgroundImage.gameObject.AddComponent<Outline>();
            glowOutline.effectColor = new Color(primaryCyan.r, primaryCyan.g, primaryCyan.b, 0.25f);
            glowOutline.effectDistance = new Vector2(outlineWidth * 2.5f, outlineWidth * 2.5f);
        }

        private void StyleTitle()
        {
            if (titleText == null) return;

            titleText.fontSize = 72;
            titleText.fontStyle = FontStyles.Bold;

            Outline outline = titleText.gameObject.GetComponent<Outline>();
            if (outline == null)
                outline = titleText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.9f);
            outline.effectDistance = new Vector2(3, -3);

            Outline glow = titleText.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(primaryCyan.r, primaryCyan.g, primaryCyan.b, 0.4f);
            glow.effectDistance = new Vector2(5, 5);
        }

        private void StyleScoreDisplay()
        {
            if (finalScoreText != null)
            {
                finalScoreText.color = scoreGlow;
                finalScoreText.fontSize = 96;
                finalScoreText.fontStyle = FontStyles.Bold;

                Outline glow = finalScoreText.gameObject.GetComponent<Outline>();
                if (glow == null)
                    glow = finalScoreText.gameObject.AddComponent<Outline>();
                glow.effectColor = new Color(scoreGlow.r, scoreGlow.g, scoreGlow.b, 0.5f);
                glow.effectDistance = new Vector2(4, 4);

                Outline shadow = finalScoreText.gameObject.AddComponent<Outline>();
                shadow.effectColor = new Color(0, 0, 0, 0.8f);
                shadow.effectDistance = new Vector2(2, -2);
            }

            if (paintingsCountText != null)
            {
                paintingsCountText.color = primaryCyan;
                paintingsCountText.fontSize = 56;
                paintingsCountText.fontStyle = FontStyles.Bold;

                Outline outline = paintingsCountText.gameObject.GetComponent<Outline>();
                if (outline == null)
                    outline = paintingsCountText.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0, 0, 0, 0.7f);
                outline.effectDistance = new Vector2(2, -2);
            }
        }

        private void StyleButtons()
        {
            StyleSingleButton(retryButton, primaryCyan, true);
            StyleSingleButton(quitButton, timeUpRed, false);
        }

        private void StyleSingleButton(Button button, Color accentColor, bool isPrimary)
        {
            if (button == null) return;

            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = isPrimary ? buttonNormal : new Color(0.15f, 0.08f, 0.1f, 0.95f);

                Outline btnOutline = buttonImage.gameObject.GetComponent<Outline>();
                if (btnOutline == null)
                    btnOutline = buttonImage.gameObject.AddComponent<Outline>();
                btnOutline.effectColor = accentColor;
                btnOutline.effectDistance = new Vector2(3, 3);
            }

            ColorBlock colors = button.colors;
            colors.normalColor = isPrimary ? buttonNormal : new Color(0.15f, 0.08f, 0.1f, 0.95f);
            colors.highlightedColor = isPrimary ? buttonHighlight : new Color(0.25f, 0.1f, 0.15f, 1f);
            colors.pressedColor = isPrimary ? buttonPressed : new Color(0.35f, 0.15f, 0.2f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            colors.fadeDuration = 0.12f;
            button.colors = colors;

            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.color = Color.white;
                buttonText.fontSize = 36;
                buttonText.fontStyle = FontStyles.Bold;

                Outline txtGlow = buttonText.gameObject.GetComponent<Outline>();
                if (txtGlow == null)
                    txtGlow = buttonText.gameObject.AddComponent<Outline>();
                txtGlow.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.4f);
                txtGlow.effectDistance = new Vector2(2, 2);
            }
        }

        private void StyleLabels()
        {
            foreach (TMP_Text label in labelTexts)
            {
                if (label == null) continue;
                if (label == titleText || label == finalScoreText || label == paintingsCountText) continue;

                // C'est un label (Score Final, Tableaux Completes, etc.)
                string text = label.text.ToLower();
                if (text.Contains("score") || text.Contains("tableau") || text.Contains("temps"))
                {
                    label.color = secondaryCyan;
                    label.fontSize = 28;

                    Outline outline = label.gameObject.GetComponent<Outline>();
                    if (outline == null)
                        outline = label.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0, 0, 0, 0.6f);
                    outline.effectDistance = new Vector2(1, -1);
                }
            }
        }

        #endregion

        #region Public Methods

        public Color GetVictoryColor() => victoryGold;
        public Color GetTimeUpColor() => timeUpRed;
        public Color GetPrimaryColor() => primaryCyan;
        public Color GetScoreColor() => scoreGlow;

        /// <summary>
        /// Met a jour la couleur du titre selon le resultat
        /// </summary>
        public void UpdateTitleStyle(bool isVictory)
        {
            if (titleText == null) return;

            Color targetColor = isVictory ? victoryGold : timeUpRed;
            titleText.color = targetColor;

            Outline[] outlines = titleText.GetComponents<Outline>();
            foreach (Outline o in outlines)
            {
                if (o.effectColor.a < 0.5f) // C'est le glow
                {
                    o.effectColor = new Color(targetColor.r, targetColor.g, targetColor.b, 0.4f);
                }
            }
        }

        /// <summary>
        /// Animation de pulse sur le score
        /// </summary>
        public void PulseScore()
        {
            if (finalScoreText != null)
                StartCoroutine(PulseCoroutine(finalScoreText));
        }

        private System.Collections.IEnumerator PulseCoroutine(TMP_Text text)
        {
            RectTransform rect = text.GetComponent<RectTransform>();
            if (rect == null) yield break;

            Vector3 originalScale = rect.localScale;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                rect.localScale = Vector3.Lerp(originalScale, originalScale * 1.15f, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                rect.localScale = Vector3.Lerp(originalScale * 1.15f, originalScale, t);
                yield return null;
            }

            rect.localScale = originalScale;
        }

        #endregion
    }
}
