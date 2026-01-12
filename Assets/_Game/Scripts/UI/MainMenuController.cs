/*
 * ============================================================================
 * MAIN MENU CONTROLLER - Menu Principal VR avec Configuration
 * ============================================================================
 *
 * INSTRUCTIONS DE CONFIGURATION DANS UNITY:
 *
 * 1. CREER LE CANVAS WORLD SPACE:
 *    - GameObject > UI > Canvas
 *    - Renommer en "MainMenuPanel"
 *    - Render Mode: "World Space"
 *    - Width: 800, Height: 600
 *    - Scale: X=0.001, Y=0.001, Z=0.001
 *    - Position: devant le joueur (0, 1.5, 3)
 *    - Rotation: Y=180 (face au joueur)
 *
 * 2. HIERARCHIE DU MENU:
 *
 *    MainMenuPanel (Canvas + MainMenuController)
 *    |
 *    +-- Panel (Background)
 *    |   - Color: (5, 13, 26, 235)
 *    |
 *    +-- TitleText (TextMeshPro)
 *    |   - Text: "MUSEUM QUIZ VR"
 *    |   - Font Size: 72
 *    |
 *    +-- SettingsPanel (Vertical Layout)
 *    |   +-- ScoreRow (Horizontal Layout)
 *    |   |   +-- ScoreLabel "Score Cible"
 *    |   |   +-- ScoreSlider
 *    |   |   +-- ScoreValueText
 *    |   |
 *    |   +-- PaintingsRow (Horizontal Layout)
 *    |   |   +-- PaintingsLabel "Tableaux"
 *    |   |   +-- PaintingsSlider
 *    |   |   +-- PaintingsValueText
 *    |   |
 *    |   +-- TimeRow (Horizontal Layout)
 *    |       +-- TimeLabel "Temps (min)"
 *    |       +-- TimeSlider
 *    |       +-- TimeValueText
 *    |
 *    +-- StartButton (Button)
 *        - Text: "COMMENCER"
 *        - Ajouter BoxCollider pour VR
 *
 * 3. CONFIGURATION DES SLIDERS:
 *    - ScoreSlider: Min=100, Max=1000, WholeNumbers=true
 *    - PaintingsSlider: Min=1, Max=10, WholeNumbers=true
 *    - TimeSlider: Min=1, Max=10, WholeNumbers=true (en minutes)
 *
 * ============================================================================
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MuseumAI.Core;

namespace MuseumAI.UI
{
    /// <summary>
    /// Controleur du menu principal VR.
    /// Permet de configurer les parametres du jeu avant de commencer.
    /// Bloque le deplacement du joueur pendant l'affichage.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Sliders de Configuration")]
        [Tooltip("Slider pour le score cible (100-1000)")]
        [SerializeField] private Slider scoreSlider;

        [Tooltip("Slider pour le nombre de tableaux (1-10)")]
        [SerializeField] private Slider paintingsSlider;

        [Tooltip("Slider pour le temps en minutes (1-10)")]
        [SerializeField] private Slider timeSlider;

        [Header("Textes des Valeurs")]
        [SerializeField] private TMP_Text scoreValueText;
        [SerializeField] private TMP_Text paintingsValueText;
        [SerializeField] private TMP_Text timeValueText;

        [Header("Bouton Start")]
        [SerializeField] private Button startButton;

        [Header("Valeurs par Defaut")]
        [SerializeField] private int defaultScore = 500;
        [SerializeField] private int defaultPaintings = 5;
        [SerializeField] private int defaultTimeMinutes = 5;

        #endregion

        #region Private Fields

        private CanvasGroup canvasGroup;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Agrandir le Canvas pour VR
            ScaleUpCanvas();

            // Chercher les references AVANT de setup les sliders
            FindMissingReferences();

            SetupSliders();
            SetupButton();
            ApplyFuturisticStyle();
        }

        /// <summary>
        /// Ajuste le canvas pour VR
        /// </summary>
        private void ScaleUpCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                // Echelle du canvas (1.2x)
                transform.localScale *= 1.2f;
                Debug.Log($"[MainMenu] Canvas scale: {transform.localScale}");
            }

            // Ajuster le RectTransform
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null && rect.sizeDelta.x < 900)
            {
                rect.sizeDelta = new Vector2(900, 650);
                Debug.Log($"[MainMenu] Canvas size: {rect.sizeDelta}");
            }

            // Monter le titre
            MoveTitle();
        }

        /// <summary>
        /// Deplace le titre vers le haut
        /// </summary>
        private void MoveTitle()
        {
            // Chercher le titre (TitleText ou contenant "Title")
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>();
            foreach (var text in texts)
            {
                if (text.name.ToLower().Contains("title") || text.fontSize > 50)
                {
                    RectTransform titleRect = text.GetComponent<RectTransform>();
                    if (titleRect != null)
                    {
                        // Monter de 30 pixels
                        titleRect.anchoredPosition += new Vector2(0, 30f);
                        Debug.Log($"[MainMenu] Titre '{text.name}' monte de 30px");
                    }
                    break;
                }
            }
        }

        private void Start()
        {
            // Initialiser avec les valeurs par defaut
            InitializeDefaultValues();

            // Animation d'entree
            StartCoroutine(FadeInAnimation());

            Debug.Log("[MainMenu] Menu principal initialise");
        }

        /// <summary>
        /// Cherche automatiquement les references non assignees
        /// </summary>
        private void FindMissingReferences()
        {
            TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);

            // Chercher paintingsValueText si non assigne
            if (paintingsValueText == null)
            {
                foreach (var text in allTexts)
                {
                    string nameLower = text.name.ToLower();
                    if (nameLower.Contains("painting") && nameLower.Contains("value"))
                    {
                        paintingsValueText = text;
                        Debug.Log($"[MainMenu] paintingsValueText trouve automatiquement: {text.name}");
                        break;
                    }
                }

                // Si toujours null, chercher par contenu ou parent
                if (paintingsValueText == null)
                {
                    foreach (var text in allTexts)
                    {
                        Transform parent = text.transform.parent;
                        if (parent != null && parent.name.ToLower().Contains("painting"))
                        {
                            if (text.name.ToLower().Contains("value") || text.text == "5")
                            {
                                paintingsValueText = text;
                                Debug.Log($"[MainMenu] paintingsValueText trouve par parent: {text.name}");
                                break;
                            }
                        }
                    }
                }
            }

            // Chercher scoreValueText si non assigne
            if (scoreValueText == null)
            {
                foreach (var text in allTexts)
                {
                    string nameLower = text.name.ToLower();
                    if (nameLower.Contains("score") && nameLower.Contains("value"))
                    {
                        scoreValueText = text;
                        Debug.Log($"[MainMenu] scoreValueText trouve: {text.name}");
                        break;
                    }
                }
            }

            // Chercher timeValueText si non assigne
            if (timeValueText == null)
            {
                foreach (var text in allTexts)
                {
                    string nameLower = text.name.ToLower();
                    if (nameLower.Contains("time") && nameLower.Contains("value"))
                    {
                        timeValueText = text;
                        Debug.Log($"[MainMenu] timeValueText trouve: {text.name}");
                        break;
                    }
                }
            }

            // Log des references finales
            Debug.Log($"[MainMenu] References - Score: {(scoreValueText != null ? scoreValueText.name : "NULL")}, " +
                      $"Paintings: {(paintingsValueText != null ? paintingsValueText.name : "NULL")}, " +
                      $"Time: {(timeValueText != null ? timeValueText.name : "NULL")}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Affiche le menu principal
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            StartCoroutine(FadeInAnimation());
        }

        /// <summary>
        /// Cache le menu principal
        /// </summary>
        public void Hide()
        {
            StartCoroutine(FadeOutAndDestroy());
        }

        /// <summary>
        /// Appele quand le joueur clique sur Commencer
        /// </summary>
        public void OnStartClicked()
        {
            Debug.Log("[MainMenu] Bouton Start clique!");

            // Desactiver le bouton pour eviter les double-clics
            if (startButton != null)
            {
                startButton.interactable = false;
            }

            // Appliquer les parametres au GameManager
            ApplySettingsToGameManager();

            // Lancer le jeu avec transition
            StartCoroutine(StartGameWithTransition());
        }

        #endregion

        #region Private Methods - Setup

        private void SetupSliders()
        {
            // Score Slider (100-1000, step 100)
            if (scoreSlider != null)
            {
                scoreSlider.minValue = 100;
                scoreSlider.maxValue = 1000;
                scoreSlider.wholeNumbers = true;
                scoreSlider.onValueChanged.AddListener(OnScoreSliderChanged);
                DisableSliderInteraction(scoreSlider);
                CreateSliderButtons(scoreSlider, 100f, "Score");
            }

            // Paintings Slider (1-31)
            if (paintingsSlider != null)
            {
                paintingsSlider.minValue = 1;
                paintingsSlider.maxValue = 31;
                paintingsSlider.wholeNumbers = true;
                paintingsSlider.onValueChanged.AddListener(OnPaintingsSliderChanged);
                DisableSliderInteraction(paintingsSlider);
                CreateSliderButtons(paintingsSlider, 1f, "Paintings");
            }

            // Time Slider (1-15 minutes)
            if (timeSlider != null)
            {
                timeSlider.minValue = 1;
                timeSlider.maxValue = 15;
                timeSlider.wholeNumbers = true;
                timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
                DisableSliderInteraction(timeSlider);
                CreateSliderButtons(timeSlider, 1f, "Time");
            }
        }

        /// <summary>
        /// Desactive l'interaction directe avec le slider (pas de drag)
        /// Le slider ne sert qu'a afficher la valeur visuellement
        /// </summary>
        private void DisableSliderInteraction(Slider slider)
        {
            if (slider == null) return;

            // Desactiver l'interactabilite du slider pour empecher le drag
            slider.interactable = false;

            // Supprimer les colliders du slider pour eviter les clics accidentels
            foreach (var collider in slider.GetComponentsInChildren<Collider>())
            {
                Destroy(collider);
            }

            // Optionnel: rendre le handle invisible ou le supprimer
            if (slider.handleRect != null)
            {
                Transform handle = slider.handleRect.transform;
                if (handle != null)
                {
                    // Garder le handle mais le rendre non-cliquable
                    var handleCollider = handle.GetComponent<Collider>();
                    if (handleCollider != null)
                    {
                        Destroy(handleCollider);
                    }
                }
            }

            Debug.Log($"[MainMenu] Slider {slider.name} interaction desactivee - utilisez +/- boutons");
        }

        private void CreateSliderButtons(Slider slider, float stepValue, string sliderName)
        {
            if (slider == null) return;

            // Trouver le texte de valeur correspondant
            TMP_Text valueText = null;
            if (sliderName == "Score") valueText = scoreValueText;
            else if (sliderName == "Paintings") valueText = paintingsValueText;
            else if (sliderName == "Time") valueText = timeValueText;

            if (valueText == null)
            {
                Debug.LogWarning($"[MainMenu] Pas de valueText pour {sliderName}");
                return;
            }

            // Parent = le texte de valeur (les boutons seront a cote)
            Transform textParent = valueText.transform.parent;
            RectTransform valueRect = valueText.GetComponent<RectTransform>();

            float buttonSize = 40f;
            float spacing = 10f;

            // Creer bouton Moins (-) a GAUCHE du texte
            GameObject minusBtn = CreateAdjustButton(textParent, "-", false, sliderName + "_Minus", buttonSize);
            RectTransform minusRect = minusBtn.GetComponent<RectTransform>();
            minusRect.anchorMin = valueRect.anchorMin;
            minusRect.anchorMax = valueRect.anchorMax;
            minusRect.pivot = new Vector2(0.5f, 0.5f);
            // Positionner a gauche du texte
            minusRect.anchoredPosition = new Vector2(
                valueRect.anchoredPosition.x - valueRect.rect.width/2f - buttonSize/2f - spacing,
                valueRect.anchoredPosition.y
            );

            SliderAdjustButton minusAdjust = minusBtn.AddComponent<SliderAdjustButton>();
            minusAdjust.Setup(slider, stepValue, false);

            // Creer bouton Plus (+) a DROITE du texte
            GameObject plusBtn = CreateAdjustButton(textParent, "+", true, sliderName + "_Plus", buttonSize);
            RectTransform plusRect = plusBtn.GetComponent<RectTransform>();
            plusRect.anchorMin = valueRect.anchorMin;
            plusRect.anchorMax = valueRect.anchorMax;
            plusRect.pivot = new Vector2(0.5f, 0.5f);
            // Positionner a droite du texte
            plusRect.anchoredPosition = new Vector2(
                valueRect.anchoredPosition.x + valueRect.rect.width/2f + buttonSize/2f + spacing,
                valueRect.anchoredPosition.y
            );

            SliderAdjustButton plusAdjust = plusBtn.AddComponent<SliderAdjustButton>();
            plusAdjust.Setup(slider, stepValue, true);

            Debug.Log($"[MainMenu] Boutons +/- crees pour {sliderName} a cote de {valueText.name}");
        }

        private GameObject CreateAdjustButton(Transform parent, string text, bool isPlus, string buttonName, float size = 50f)
        {
            // Creer le GameObject du bouton
            GameObject btnObj = new GameObject(buttonName);
            btnObj.transform.SetParent(parent, false);

            // RectTransform
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);

            // Image (fond du bouton) - couleur differente pour + et -
            UnityEngine.UI.Image img = btnObj.AddComponent<UnityEngine.UI.Image>();
            Color baseColor = isPlus ? new Color(0f, 0.9f, 0.4f, 0.9f) : new Color(1f, 0.3f, 0.3f, 0.9f); // Vert pour +, Rouge pour -
            img.color = baseColor;

            // Button
            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = new Color(baseColor.r + 0.2f, baseColor.g + 0.2f, baseColor.b + 0.2f, 1f);
            colors.pressedColor = new Color(baseColor.r * 0.7f, baseColor.g * 0.7f, baseColor.b * 0.7f, 1f);
            colors.selectedColor = baseColor;
            btn.colors = colors;

            // BoxCollider pour VR
            BoxCollider collider = btnObj.AddComponent<BoxCollider>();
            collider.size = new Vector3(size, size, 20f);

            // Creer le texte
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI tmpText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = size * 0.7f; // Taille proportionnelle
            tmpText.fontStyle = TMPro.FontStyles.Bold;
            tmpText.alignment = TMPro.TextAlignmentOptions.Center;
            tmpText.color = Color.white;
            tmpText.enableAutoSizing = false;

            return btnObj;
        }

        private void SetupButton()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(OnStartClicked);

                // Ajouter BoxCollider pour VR
                SetupButtonCollider(startButton);
            }
        }

        private void SetupButtonCollider(Button button)
        {
            if (button == null) return;

            BoxCollider existingCollider = button.GetComponent<BoxCollider>();
            if (existingCollider != null)
            {
                Destroy(existingCollider);
            }

            BoxCollider collider = button.gameObject.AddComponent<BoxCollider>();
            RectTransform rect = button.GetComponent<RectTransform>();

            float width = 300f;
            float height = 80f;

            if (rect != null && rect.rect.width > 0 && rect.rect.height > 0)
            {
                width = rect.rect.width;
                height = rect.rect.height;
            }

            collider.size = new Vector3(width, height, 20f);
            collider.center = Vector3.zero;

            Debug.Log($"[MainMenu] BoxCollider StartButton: size=({width}, {height}, 20)");
        }

        private void ApplyFuturisticStyle()
        {
            FuturisticMainMenuStyle style = GetComponent<FuturisticMainMenuStyle>();
            if (style == null)
            {
                style = gameObject.AddComponent<FuturisticMainMenuStyle>();
            }
        }

        private void InitializeDefaultValues()
        {
            // Arrondir les valeurs par defaut aux steps
            int roundedScore = Mathf.RoundToInt(defaultScore / 100f) * 100;

            if (scoreSlider != null)
            {
                scoreSlider.value = roundedScore;
                OnScoreSliderChanged(roundedScore);
            }

            if (paintingsSlider != null)
            {
                paintingsSlider.value = defaultPaintings;
                OnPaintingsSliderChanged(defaultPaintings);
            }

            if (timeSlider != null)
            {
                timeSlider.value = defaultTimeMinutes;
                OnTimeSliderChanged(defaultTimeMinutes);
            }
        }

        #endregion

        #region Private Methods - Slider Callbacks

        private void OnScoreSliderChanged(float value)
        {
            // Arrondir au multiple de 100
            int roundedValue = Mathf.RoundToInt(value / 100f) * 100;

            if (scoreValueText != null)
            {
                scoreValueText.text = roundedValue.ToString();
            }

            // Mettre a jour le slider pour afficher la valeur arrondie
            if (scoreSlider != null && Mathf.Abs(scoreSlider.value - roundedValue) > 1)
            {
                scoreSlider.SetValueWithoutNotify(roundedValue);
            }
        }

        private void OnPaintingsSliderChanged(float value)
        {
            int intValue = Mathf.RoundToInt(value);

            if (paintingsValueText != null)
            {
                paintingsValueText.text = intValue.ToString();
                Debug.Log($"[MainMenu] Paintings value updated: {intValue}");
            }
            else
            {
                Debug.LogWarning($"[MainMenu] paintingsValueText est NULL! Valeur: {intValue}");
            }
        }

        private void OnTimeSliderChanged(float value)
        {
            int minutes = Mathf.RoundToInt(value);

            if (timeValueText != null)
            {
                timeValueText.text = $"{minutes} min";
            }
        }

        #endregion

        #region Private Methods - Game Start

        private void ApplySettingsToGameManager()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[MainMenu] GameManager.Instance est null!");
                return;
            }

            int targetScore = scoreSlider != null ? Mathf.RoundToInt(scoreSlider.value / 100f) * 100 : defaultScore;
            int targetPaintings = paintingsSlider != null ? Mathf.RoundToInt(paintingsSlider.value) : defaultPaintings;
            float duration = timeSlider != null ? Mathf.RoundToInt(timeSlider.value) * 60f : defaultTimeMinutes * 60f;

            GameManager.Instance.SetGameSettings(targetScore, targetPaintings, duration);

            Debug.Log($"[MainMenu] Parametres appliques - Score: {targetScore}, Tableaux: {targetPaintings}, Temps: {duration}s");
        }

        private System.Collections.IEnumerator StartGameWithTransition()
        {
            // Fade out
            float fadeDuration = 0.4f;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                }
                yield return null;
            }

            // Demarrer le jeu
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }

            // Detruire le menu
            Destroy(gameObject);
        }

        #endregion

        #region Animations

        private System.Collections.IEnumerator FadeInAnimation()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;

            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private System.Collections.IEnumerator FadeOutAndDestroy()
        {
            if (canvasGroup == null)
            {
                Destroy(gameObject);
                yield break;
            }

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            Destroy(gameObject);
        }

        #endregion
    }
}
