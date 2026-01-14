using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MuseumAI.UI
{
    public class FuturisticMainMenuStyle : MonoBehaviour
    {
        [Header("Palette Futuriste")]
        [SerializeField] private Color primaryCyan = new Color(0f, 0.9f, 1f, 1f);
        [SerializeField] private Color secondaryCyan = new Color(0f, 0.7f, 0.85f, 1f);
        [SerializeField] private Color darkBackground = new Color(0.02f, 0.05f, 0.1f, 0.92f);
        [SerializeField] private Color titleColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color labelColor = new Color(0.7f, 0.85f, 0.9f, 1f);
        [SerializeField] private Color valueColor = new Color(0f, 1f, 0.6f, 1f);
        [SerializeField] private Color buttonColor = new Color(0f, 0.8f, 0.9f, 1f);

        [Header("Glow Settings")]
        [SerializeField] private float outlineWidth = 2f;

        private Image backgroundImage;
        private TMP_Text titleText;
        private Slider[] sliders;
        private TMP_Text[] labels;
        private TMP_Text[] valueTexts;
        private Button startButton;

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

            Transform titleT = transform.Find("TitleText");
            if (titleT != null)
                titleText = titleT.GetComponent<TMP_Text>();

            sliders = GetComponentsInChildren<Slider>(true);

            labels = new TMP_Text[3];
            valueTexts = new TMP_Text[3];

            Transform scoreRow = FindDeepChild(transform, "ScoreRow");
            if (scoreRow != null)
            {
                Transform label = scoreRow.Find("ScoreLabel");
                Transform value = scoreRow.Find("ScoreValueText");
                if (label != null) labels[0] = label.GetComponent<TMP_Text>();
                if (value != null) valueTexts[0] = value.GetComponent<TMP_Text>();
            }

            Transform paintingsRow = FindDeepChild(transform, "PaintingsRow");
            if (paintingsRow != null)
            {
                Transform label = paintingsRow.Find("PaintingsLabel");
                Transform value = paintingsRow.Find("PaintingsValueText");
                if (label != null) labels[1] = label.GetComponent<TMP_Text>();
                if (value != null) valueTexts[1] = value.GetComponent<TMP_Text>();
            }

            Transform timeRow = FindDeepChild(transform, "TimeRow");
            if (timeRow != null)
            {
                Transform label = timeRow.Find("TimeLabel");
                Transform value = timeRow.Find("TimeValueText");
                if (label != null) labels[2] = label.GetComponent<TMP_Text>();
                if (value != null) valueTexts[2] = value.GetComponent<TMP_Text>();
            }

            Transform buttonT = FindDeepChild(transform, "StartButton");
            if (buttonT != null)
                startButton = buttonT.GetComponent<Button>();
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void ApplyFuturisticStyle()
        {
            StyleBackground();
            StyleTitle();
            StyleSliders();
            StyleLabels();
            StyleValueTexts();
            StyleButton();
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
            glow.effectColor = new Color(primaryCyan.r, primaryCyan.g, primaryCyan.b, 0.15f);
            glow.effectDistance = new Vector2(outlineWidth * 2.5f, outlineWidth * 2.5f);
        }

        private void StyleTitle()
        {
            if (titleText == null) return;

            titleText.color = titleColor;
            titleText.fontSize = 72;
            titleText.fontStyle = FontStyles.Bold;

            Outline glow = titleText.gameObject.GetComponent<Outline>();
            if (glow == null)
                glow = titleText.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(primaryCyan.r, primaryCyan.g, primaryCyan.b, 0.6f);
            glow.effectDistance = new Vector2(3, 3);

            Outline shadow = titleText.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(2, -2);
        }

        private void StyleSliders()
        {
            foreach (Slider slider in sliders)
            {
                if (slider == null) continue;

                Image background = slider.transform.Find("Background")?.GetComponent<Image>();
                if (background != null)
                {
                    background.color = new Color(0.1f, 0.15f, 0.2f, 0.8f);
                }

                Transform fillArea = slider.transform.Find("Fill Area");
                if (fillArea != null)
                {
                    Image fill = fillArea.Find("Fill")?.GetComponent<Image>();
                    if (fill != null)
                    {
                        fill.color = primaryCyan;
                    }
                }

                Transform handleArea = slider.transform.Find("Handle Slide Area");
                if (handleArea != null)
                {
                    Image handle = handleArea.Find("Handle")?.GetComponent<Image>();
                    if (handle != null)
                    {
                        handle.color = Color.white;

                        Outline glow = handle.gameObject.GetComponent<Outline>();
                        if (glow == null)
                            glow = handle.gameObject.AddComponent<Outline>();
                        glow.effectColor = new Color(primaryCyan.r, primaryCyan.g, primaryCyan.b, 0.7f);
                        glow.effectDistance = new Vector2(2, 2);
                    }
                }
            }
        }

        private void StyleLabels()
        {
            foreach (TMP_Text label in labels)
            {
                if (label == null) continue;

                label.color = labelColor;
                label.fontSize = 32;
                label.fontStyle = FontStyles.Normal;
            }
        }

        private void StyleValueTexts()
        {
            foreach (TMP_Text valueText in valueTexts)
            {
                if (valueText == null) continue;

                valueText.color = valueColor;
                valueText.fontSize = 36;
                valueText.fontStyle = FontStyles.Bold;

                Outline glow = valueText.gameObject.GetComponent<Outline>();
                if (glow == null)
                    glow = valueText.gameObject.AddComponent<Outline>();
                glow.effectColor = new Color(valueColor.r, valueColor.g, valueColor.b, 0.4f);
                glow.effectDistance = new Vector2(2, 2);
            }
        }

        private void StyleButton()
        {
            if (startButton == null) return;

            Image buttonImage = startButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = new Color(0.05f, 0.15f, 0.2f, 0.95f);

                Outline outline = buttonImage.gameObject.GetComponent<Outline>();
                if (outline == null)
                    outline = buttonImage.gameObject.AddComponent<Outline>();
                outline.effectColor = buttonColor;
                outline.effectDistance = new Vector2(outlineWidth, outlineWidth);

                Outline glow = buttonImage.gameObject.AddComponent<Outline>();
                glow.effectColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.3f);
                glow.effectDistance = new Vector2(outlineWidth * 2f, outlineWidth * 2f);
            }

            TMP_Text buttonText = startButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.color = buttonColor;
                buttonText.fontSize = 42;
                buttonText.fontStyle = FontStyles.Bold;

                Outline textGlow = buttonText.gameObject.GetComponent<Outline>();
                if (textGlow == null)
                    textGlow = buttonText.gameObject.AddComponent<Outline>();
                textGlow.effectColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.5f);
                textGlow.effectDistance = new Vector2(2, 2);
            }

            ColorBlock colors = startButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = colors.highlightedColor;
            startButton.colors = colors;
        }

        public Color GetPrimaryColor() => primaryCyan;
        public Color GetButtonColor() => buttonColor;
    }
}
