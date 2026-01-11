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

            SetupSliders();
            SetupButton();
            ApplyFuturisticStyle();
        }

        private void Start()
        {
            // Initialiser avec les valeurs par defaut
            InitializeDefaultValues();

            // Animation d'entree
            StartCoroutine(FadeInAnimation());

            Debug.Log("[MainMenu] Menu principal initialise");
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
            }

            // Paintings Slider (1-10)
            if (paintingsSlider != null)
            {
                paintingsSlider.minValue = 1;
                paintingsSlider.maxValue = 10;
                paintingsSlider.wholeNumbers = true;
                paintingsSlider.onValueChanged.AddListener(OnPaintingsSliderChanged);
            }

            // Time Slider (1-10 minutes)
            if (timeSlider != null)
            {
                timeSlider.minValue = 1;
                timeSlider.maxValue = 10;
                timeSlider.wholeNumbers = true;
                timeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
            }
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
