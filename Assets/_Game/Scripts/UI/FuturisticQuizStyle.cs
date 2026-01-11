using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MuseumAI.UI
{
    /// <summary>
    /// Applique un style futuriste VR holographique au QuizUI.
    /// Couleurs cyan/bleu avec effets glow.
    /// </summary>
    public class FuturisticQuizStyle : MonoBehaviour
    {
        #region Color Palette
        
        [Header("Palette Futuriste")]
        [SerializeField] private Color primaryCyan = new Color(0f, 0.9f, 1f, 1f);           // #00E5FF
        [SerializeField] private Color secondaryCyan = new Color(0f, 0.7f, 0.85f, 1f);      // #00B3D9
        [SerializeField] private Color darkBackground = new Color(0.02f, 0.05f, 0.1f, 0.92f); // Fond sombre transparent
        [SerializeField] private Color buttonNormal = new Color(0.05f, 0.12f, 0.18f, 0.95f);  // Bouton normal
        [SerializeField] private Color buttonHighlight = new Color(0f, 0.25f, 0.35f, 1f);     // Bouton survol
        [SerializeField] private Color buttonPressed = new Color(0f, 0.4f, 0.5f, 1f);         // Bouton presse
        [SerializeField] private Color correctGreen = new Color(0f, 1f, 0.5f, 1f);           // Vert neon
        [SerializeField] private Color wrongRed = new Color(1f, 0.2f, 0.3f, 1f);             // Rouge neon
        
        [Header("Glow Settings")]
        [SerializeField] private float glowIntensity = 0.5f;
        [SerializeField] private float outlineWidth = 3f;
        
        #endregion
        
        #region References (Auto-detected)
        
        private Image backgroundImage;
        private Image[] buttonImages;
        private Button[] answerButtons;
        private TMP_Text questionText;
        private TMP_Text feedbackText;
        private TMP_Text[] buttonTexts;
        private Outline[] outlines;
        
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
            // Trouver le QuizPanel (fond principal)
            Transform quizPanel = transform.Find("QuizPanel");
            if (quizPanel != null)
            {
                backgroundImage = quizPanel.GetComponent<Image>();
                
                // Trouver le texte de question
                Transform questionT = quizPanel.Find("QuestionText");
                if (questionT != null)
                    questionText = questionT.GetComponent<TMP_Text>();
                
                // Trouver le feedback text
                Transform feedbackT = quizPanel.Find("FeedbackText");
                if (feedbackT != null)
                    feedbackText = feedbackT.GetComponent<TMP_Text>();
            }
            
            // Trouver tous les boutons de reponse
            answerButtons = new Button[4];
            buttonImages = new Image[4];
            buttonTexts = new TMP_Text[4];
            
            for (int i = 0; i < 4; i++)
            {
                string buttonName = $"AnswerButton{i}";
                Transform buttonT = quizPanel?.Find(buttonName);
                if (buttonT != null)
                {
                    answerButtons[i] = buttonT.GetComponent<Button>();
                    buttonImages[i] = buttonT.GetComponent<Image>();
                    buttonTexts[i] = buttonT.GetComponentInChildren<TMP_Text>();
                }
            }
        }
        
        private void ApplyFuturisticStyle()
        {
            // === BACKGROUND ===
            StyleBackground();
            
            // === QUESTION TEXT ===
            StyleQuestionText();
            
            // === ANSWER BUTTONS ===
            StyleAnswerButtons();
            
            // === FEEDBACK TEXT ===
            StyleFeedbackText();
            
            Debug.Log("[FuturisticStyle] Style futuriste VR applique!");
        }
        
        private void StyleBackground()
        {
            if (backgroundImage == null) return;
            
            // Fond sombre semi-transparent
            backgroundImage.color = darkBackground;
            
            // Ajouter une bordure glow (Outline component)
            Outline outline = backgroundImage.gameObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = backgroundImage.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = primaryCyan;
            outline.effectDistance = new Vector2(outlineWidth, outlineWidth);
            
            // Ajouter un deuxieme outline pour effet glow plus large
            Outline glowOutline = backgroundImage.gameObject.AddComponent<Outline>();
            glowOutline.effectColor = new Color(primaryCyan.r, primaryCyan.g, primaryCyan.b, 0.3f);
            glowOutline.effectDistance = new Vector2(outlineWidth * 2, outlineWidth * 2);
        }
        
        private void StyleQuestionText()
        {
            if (questionText == null) return;
            
            // Texte cyan lumineux
            questionText.color = primaryCyan;
            questionText.fontSize = 42;
            questionText.fontStyle = FontStyles.Bold;
            
            // Activer le glow TMP (si le material le supporte)
            if (questionText.fontSharedMaterial != null)
            {
                // Creer une instance du material pour ne pas affecter les autres
                Material glowMat = new Material(questionText.fontSharedMaterial);
                glowMat.EnableKeyword("GLOW_ON");
                glowMat.SetColor("_GlowColor", primaryCyan);
                glowMat.SetFloat("_GlowOffset", 0f);
                glowMat.SetFloat("_GlowInner", 0.1f);
                glowMat.SetFloat("_GlowOuter", glowIntensity);
                glowMat.SetFloat("_GlowPower", 1f);
                questionText.fontMaterial = glowMat;
            }
            
            // Outline sur le texte pour plus de lisibilite
            Outline textOutline = questionText.gameObject.GetComponent<Outline>();
            if (textOutline == null)
            {
                textOutline = questionText.gameObject.AddComponent<Outline>();
            }
            textOutline.effectColor = new Color(0, 0, 0, 0.8f);
            textOutline.effectDistance = new Vector2(1, -1);
        }
        
        private void StyleAnswerButtons()
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null) continue;
                
                // === BUTTON IMAGE ===
                if (buttonImages[i] != null)
                {
                    buttonImages[i].color = buttonNormal;
                    
                    // Bordure cyan sur le bouton
                    Outline btnOutline = buttonImages[i].gameObject.GetComponent<Outline>();
                    if (btnOutline == null)
                    {
                        btnOutline = buttonImages[i].gameObject.AddComponent<Outline>();
                    }
                    btnOutline.effectColor = secondaryCyan;
                    btnOutline.effectDistance = new Vector2(2, 2);
                }
                
                // === BUTTON COLORS ===
                ColorBlock colors = answerButtons[i].colors;
                colors.normalColor = buttonNormal;
                colors.highlightedColor = buttonHighlight;
                colors.pressedColor = buttonPressed;
                colors.selectedColor = buttonHighlight;
                colors.disabledColor = new Color(0.1f, 0.1f, 0.15f, 0.5f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.15f;
                answerButtons[i].colors = colors;
                
                // === BUTTON TEXT ===
                if (buttonTexts[i] != null)
                {
                    buttonTexts[i].color = Color.white;
                    buttonTexts[i].fontSize = 32;
                    
                    // Legere lueur sur le texte des boutons
                    Outline txtOutline = buttonTexts[i].gameObject.GetComponent<Outline>();
                    if (txtOutline == null)
                    {
                        txtOutline = buttonTexts[i].gameObject.AddComponent<Outline>();
                    }
                    txtOutline.effectColor = new Color(primaryCyan.r, primaryCyan.g, primaryCyan.b, 0.3f);
                    txtOutline.effectDistance = new Vector2(1, 1);
                }
            }
        }
        
        private void StyleFeedbackText()
        {
            if (feedbackText == null) return;
            
            feedbackText.fontSize = 48;
            feedbackText.fontStyle = FontStyles.Bold;
            
            // Le texte de feedback changera de couleur selon le resultat
            // On prepare juste le style de base
            Outline fbOutline = feedbackText.gameObject.GetComponent<Outline>();
            if (fbOutline == null)
            {
                fbOutline = feedbackText.gameObject.AddComponent<Outline>();
            }
            fbOutline.effectColor = new Color(0, 0, 0, 0.9f);
            fbOutline.effectDistance = new Vector2(2, -2);
        }
        
        #endregion
        
        #region Public Methods - Pour QuizUIController
        
        /// <summary>
        /// Retourne la couleur pour une bonne reponse (vert neon)
        /// </summary>
        public Color GetCorrectColor() => correctGreen;
        
        /// <summary>
        /// Retourne la couleur pour une mauvaise reponse (rouge neon)
        /// </summary>
        public Color GetWrongColor() => wrongRed;
        
        /// <summary>
        /// Retourne la couleur primaire cyan
        /// </summary>
        public Color GetPrimaryColor() => primaryCyan;
        
        /// <summary>
        /// Applique un effet pulse glow sur un element
        /// </summary>
        public void ApplyPulseGlow(Image target, float duration = 1f)
        {
            if (target == null) return;
            StartCoroutine(PulseGlowCoroutine(target, duration));
        }
        
        private System.Collections.IEnumerator PulseGlowCoroutine(Image target, float duration)
        {
            Outline outline = target.GetComponent<Outline>();
            if (outline == null) yield break;
            
            Color originalColor = outline.effectColor;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.PingPong(elapsed * 2f, 1f);
                float alpha = Mathf.Lerp(0.3f, 1f, t);
                outline.effectColor = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
            
            outline.effectColor = originalColor;
        }
        
        #endregion
    }
}
