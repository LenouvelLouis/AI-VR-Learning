/*
 * ============================================================================
 * QUIZ UI CONTROLLER - Interface Quiz VR World Space
 * ============================================================================
 *
 * INSTRUCTIONS DE CONFIGURATION DU PREFAB UNITY:
 *
 * 1. CREER LE CANVAS WORLD SPACE:
 *    - GameObject > UI > Canvas
 *    - Renommer en "QuizPanel"
 *    - Render Mode: "World Space"
 *    - Event Camera: Laisser vide (sera auto-detecte)
 *
 * 2. CONFIGURER LE RECT TRANSFORM DU CANVAS:
 *    - Width: 800, Height: 600
 *    - Scale: X=0.001, Y=0.001, Z=0.001 (CRUCIAL pour la VR!)
 *    - Cela donne un panneau d'environ 80cm x 60cm en monde reel
 *
 * 3. AJOUTER LES COMPOSANTS REQUIS AU CANVAS:
 *    - Canvas (deja present)
 *    - Graphic Raycaster (SUPPRIMER celui par defaut)
 *    - AJOUTER: "Tracked Device Graphic Raycaster" (pour Meta XR)
 *      OU utiliser le GraphicRaycaster standard si XR Interaction Toolkit
 *    - Canvas Scaler: Scale With Screen Size, Reference 1920x1080
 *
 * 4. HIERARCHIE RECOMMANDEE DU PREFAB:
 *
 *    QuizPanel (Canvas)
 *    |
 *    +-- Background (Image, couleur sombre semi-transparente)
 *    |   - Color: (30, 30, 40, 230)
 *    |   - Stretch to fill
 *    |
 *    +-- LoadingPanel (GameObject)
 *    |   +-- LoadingText (TextMeshPro)
 *    |       - Text: "Analyse du tableau..."
 *    |       - Font Size: 48
 *    |       - Alignment: Center
 *    |   +-- LoadingSpinner (Image, optionnel)
 *    |       - Rotation animee via script ou Animator
 *    |
 *    +-- QuizContentPanel (GameObject)
 *    |   +-- QuestionText (TextMeshPro)
 *    |   |   - Font Size: 42
 *    |   |   - Alignment: Center Top
 *    |   |   - Height: ~150px
 *    |   |
 *    |   +-- AnswersContainer (Vertical Layout Group)
 *    |       - Spacing: 20
 *    |       - Child Force Expand: Width=true, Height=false
 *    |       |
 *    |       +-- AnswerButton_0 (Button)
 *    |       |   +-- AnswerText_0 (TextMeshPro, child)
 *    |       +-- AnswerButton_1 (Button)
 *    |       |   +-- AnswerText_1 (TextMeshPro, child)
 *    |       +-- AnswerButton_2 (Button)
 *    |       |   +-- AnswerText_2 (TextMeshPro, child)
 *    |       +-- AnswerButton_3 (Button)
 *    |           +-- AnswerText_3 (TextMeshPro, child)
 *    |
 *    +-- ErrorPanel (GameObject)
 *        +-- ErrorText (TextMeshPro)
 *        |   - Color: Rouge clair
 *        +-- RetryButton (Button, optionnel)
 *
 * 5. CONFIGURATION DES BOUTONS:
 *    - Taille recommandee: Width=700, Height=80
 *    - Navigation: None (pour eviter les problemes en VR)
 *    - Transition: Color Tint
 *    - Normal Color: (60, 60, 80, 255)
 *    - Highlighted: (80, 80, 100, 255)
 *    - Pressed: (40, 40, 60, 255)
 *
 * 6. ASSIGNATION DANS L'INSPECTEUR:
 *    - Glisser ce script sur le Canvas "QuizPanel"
 *    - Assigner toutes les references dans l'inspecteur
 *    - Les boutons seront auto-configures au runtime
 *
 * 7. SAUVEGARDER COMME PREFAB:
 *    - Glisser dans Assets/_Game/Prefabs/UI/
 *    - Assigner ce prefab dans le GameManager
 *
 * ============================================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MuseumAI.API;
using MuseumAI.Core;
using MuseumAI.Gameplay;

namespace MuseumAI.UI
{
    /// <summary>
    /// Controleur de l'interface utilisateur du quiz en World Space VR.
    /// Gere l'affichage des questions, la selection des reponses et le feedback visuel.
    /// </summary>
    public class QuizUIController : MonoBehaviour
    {
        #region Serialized Fields - UI References

        [Header("Panels")]
        [Tooltip("Le Canvas principal (pour activer/desactiver tout)")]
        [SerializeField] private Canvas mainCanvas;

        [Tooltip("Panel affiche pendant le chargement")]
        [SerializeField] private GameObject loadingPanel;

        [Tooltip("Panel contenant le quiz (question + reponses)")]
        [SerializeField] private GameObject quizPanel;

        [Tooltip("Panel affiche en cas d'erreur")]
        [SerializeField] private GameObject errorPanel;

        [Header("Textes")]
        [Tooltip("Texte de la question")]
        [SerializeField] private TMP_Text questionText;

        [Tooltip("Texte affiche pendant le chargement")]
        [SerializeField] private TMP_Text loadingText;

        [Tooltip("Texte affiche en cas d'erreur")]
        [SerializeField] private TMP_Text errorText;

        [Tooltip("Texte de feedback apres reponse (+100 pts! ou Rate...)")]
        [SerializeField] private TMP_Text feedbackText;

        [Header("Boutons de Reponse")]
        [Tooltip("Les 4 boutons de reponse (dans l'ordre)")]
        [SerializeField] private Button[] answerButtons = new Button[4];

        [Tooltip("Les textes a l'interieur des boutons")]
        [SerializeField] private TMP_Text[] answerTexts = new TMP_Text[4];

        [Header("Bouton Retry (Optionnel)")]
        [SerializeField] private Button retryButton;

        #endregion

        #region Serialized Fields - Configuration

        [Header("Couleurs de Feedback")]
        [SerializeField] private Color normalButtonColor = new Color(0.24f, 0.24f, 0.31f, 1f);
        [SerializeField] private Color correctAnswerColor = new Color(0.2f, 0.7f, 0.3f, 1f);
        [SerializeField] private Color wrongAnswerColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color highlightedColor = new Color(0.31f, 0.31f, 0.39f, 1f);

        [Header("Timing")]
        [Tooltip("Duree d'affichage du feedback avant fermeture (secondes)")]
        [SerializeField] private float feedbackDuration = 2.5f;

        [Tooltip("Duree de l'animation de fade (secondes)")]
        [SerializeField] private float fadeDuration = 0.3f;

        [Header("Animation Chargement")]
        [Tooltip("Spinner a faire tourner pendant le chargement")]
        [SerializeField] private RectTransform loadingSpinner;
        [SerializeField] private float spinnerSpeed = 200f;

        #endregion

        #region Private Fields

        private QuizData currentQuizData;
        private PaintingController currentPainting;
        private List<QuizChoice> currentChoices;
        private bool isInteractable = true;
        private Coroutine spinnerCoroutine;
        private Image[] buttonImages;
        private ColorBlock[] originalColorBlocks;

        #endregion

        #region Events

        /// <summary>
        /// Declenche quand une reponse est selectionnee. Params: isCorrect, choiceIndex
        /// </summary>
        public event Action<bool, int> OnAnswerSelected;

        /// <summary>
        /// Declenche quand l'UI est fermee
        /// </summary>
        public event Action OnUIClosed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheButtonComponents();
            SetupButtonListeners();
            ValidateReferences();
            // Note: SetupButtonColliders() est appele dans Start() apres le layout
        }

        private void Start()
        {
            // NOTE: Ne PAS appeler Hide() ici!
            // L'UI est instanciee dynamiquement par GameManager qui appelle ShowQuiz() immediatement.
            // Si on appelle Hide() dans Start(), ca ecrase ShowQuiz() car Start() s'execute apres.
            // L'etat initial est gere par le code qui instancie le prefab.
            Debug.Log("[QuizUI] Start() - Etat initial preserve (pas de Hide automatique)");

            // Forcer le calcul du layout Canvas PUIS ajouter les colliders
            StartCoroutine(SetupCollidersAfterLayout());
        }

        private System.Collections.IEnumerator SetupCollidersAfterLayout()
        {
            // Attendre la fin du frame pour que le layout soit calcule
            yield return null;

            // Forcer la mise a jour du Canvas
            Canvas.ForceUpdateCanvases();

            // Maintenant les RectTransform ont leurs vraies dimensions
            SetupButtonColliders();
        }

        private void OnEnable()
        {
            // S'abonner aux evenements du GameManager si necessaire
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnQuizDataReady += OnQuizDataReceived;
                GameManager.Instance.OnQuizError += OnQuizErrorReceived;
                GameManager.Instance.OnQuizStarted += OnQuizStartedHandler;
            }
        }

        private void OnDisable()
        {
            // Se desabonner
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnQuizDataReady -= OnQuizDataReceived;
                GameManager.Instance.OnQuizError -= OnQuizErrorReceived;
                GameManager.Instance.OnQuizStarted -= OnQuizStartedHandler;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Affiche le panneau de chargement
        /// </summary>
        public void ShowLoading()
        {
            ShowCanvas();

            loadingPanel?.SetActive(true);
            quizPanel?.SetActive(false);
            errorPanel?.SetActive(false);

            if (loadingText != null)
            {
                loadingText.text = "Analyse du tableau...";
            }

            // Demarrer l'animation du spinner
            if (loadingSpinner != null)
            {
                spinnerCoroutine = StartCoroutine(AnimateSpinner());
            }

            Debug.Log("[QuizUI] Affichage du chargement");
        }

        /// <summary>
        /// Affiche le quiz avec les donnees fournies
        /// </summary>
        /// <param name="data">Donnees du quiz generees par l'API</param>
        public void ShowQuiz(QuizData data)
        {
            if (data == null || !data.IsValid())
            {
                ShowError("Donnees du quiz invalides");
                return;
            }

            StopSpinner();
            ShowCanvas();

            loadingPanel?.SetActive(false);
            quizPanel?.SetActive(true);
            errorPanel?.SetActive(false);

            // Cacher le texte de feedback du quiz precedent
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }

            currentQuizData = data;
            currentChoices = data.GetShuffledChoices();

            // Remplir la question
            if (questionText != null)
            {
                questionText.text = data.question;
            }

            // Remplir les boutons avec les choix melanges
            for (int i = 0; i < answerButtons.Length && i < currentChoices.Count; i++)
            {
                if (answerTexts[i] != null)
                {
                    answerTexts[i].text = currentChoices[i].text;
                }

                // Reinitialiser la couleur du bouton
                SetButtonColor(i, normalButtonColor);

                // Activer l'interactivite
                if (answerButtons[i] != null)
                {
                    answerButtons[i].interactable = true;
                }
            }

            isInteractable = true;

            Debug.Log($"[QuizUI] Quiz affiche: {data.question}");
        }

        /// <summary>
        /// Affiche le quiz et associe le tableau source
        /// </summary>
        public void ShowQuiz(QuizData data, PaintingController painting)
        {
            currentPainting = painting;
            ShowQuiz(data);
        }

        /// <summary>
        /// Affiche un message d'erreur
        /// </summary>
        /// <param name="message">Message d'erreur a afficher</param>
        public void ShowError(string message)
        {
            StopSpinner();
            ShowCanvas();

            loadingPanel?.SetActive(false);
            quizPanel?.SetActive(false);
            errorPanel?.SetActive(true);

            if (errorText != null)
            {
                errorText.text = message;
            }

            // Configurer le bouton retry si present
            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            Debug.LogWarning($"[QuizUI] Erreur affichee: {message}");
        }

        /// <summary>
        /// Cache completement l'UI
        /// </summary>
        public void Hide()
        {
            StopSpinner();

            if (mainCanvas != null)
            {
                mainCanvas.enabled = false;
            }

            loadingPanel?.SetActive(false);
            quizPanel?.SetActive(false);
            errorPanel?.SetActive(false);

            // Reset des donnees
            currentQuizData = null;
            currentPainting = null;
            currentChoices = null;
            isInteractable = true;

            OnUIClosed?.Invoke();

            Debug.Log("[QuizUI] UI masquee");
        }

        /// <summary>
        /// Positionne l'UI devant un tableau
        /// </summary>
        public void PositionNearPainting(PaintingController painting, float distance = 1.5f)
        {
            if (painting == null) return;

            Transform paintingTransform = painting.transform;

            // Position: devant le tableau
            Vector3 position = paintingTransform.position
                             + paintingTransform.forward * distance
                             + Vector3.up * 0.3f;

            // Rotation: face au joueur (oppose a la normale du tableau)
            Quaternion rotation = Quaternion.LookRotation(-paintingTransform.forward, Vector3.up);

            transform.position = position;
            transform.rotation = rotation;

            Debug.Log($"[QuizUI] Positionne devant {painting.PaintingTitle}");
        }

        #endregion

        #region Private Methods - Setup

        private void CacheButtonComponents()
        {
            buttonImages = new Image[answerButtons.Length];
            originalColorBlocks = new ColorBlock[answerButtons.Length];

            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null)
                {
                    buttonImages[i] = answerButtons[i].GetComponent<Image>();
                    originalColorBlocks[i] = answerButtons[i].colors;
                }
            }
        }

        private void SetupButtonListeners()
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null)
                {
                    int index = i; // Capture pour la closure
                    answerButtons[i].onClick.RemoveAllListeners();
                    answerButtons[i].onClick.AddListener(() => OnAnswerButtonClicked(index));
                }
            }
        }

        /// <summary>
        /// Ajoute des BoxColliders aux boutons pour permettre la detection par Raycast VR
        /// </summary>
        private void SetupButtonColliders()
        {
            // Taille par defaut basee sur le design du prefab (Width=700, Height=80)
            const float DEFAULT_WIDTH = 700f;
            const float DEFAULT_HEIGHT = 80f;
            const float COLLIDER_DEPTH = 20f;

            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null)
                {
                    SetupSingleButtonCollider(answerButtons[i], $"AnswerButton_{i}", DEFAULT_WIDTH, DEFAULT_HEIGHT, COLLIDER_DEPTH);
                }
            }

            // Ajouter aussi au bouton Retry si present
            if (retryButton != null)
            {
                SetupSingleButtonCollider(retryButton, "RetryButton", 300f, 60f, COLLIDER_DEPTH);
            }
        }

        private void SetupSingleButtonCollider(Button button, string debugName, float defaultWidth, float defaultHeight, float depth)
        {
            // Supprimer l'ancien collider s'il existe (pour recalculer)
            BoxCollider existingCollider = button.GetComponent<BoxCollider>();
            if (existingCollider != null)
            {
                Destroy(existingCollider);
            }

            // Ajouter un nouveau BoxCollider
            BoxCollider collider = button.gameObject.AddComponent<BoxCollider>();

            RectTransform rect = button.GetComponent<RectTransform>();
            float width = defaultWidth;
            float height = defaultHeight;

            if (rect != null && rect.rect.width > 0 && rect.rect.height > 0)
            {
                width = rect.rect.width;
                height = rect.rect.height;
            }

            collider.size = new Vector3(width, height, depth);
            collider.center = Vector3.zero;

            Debug.Log($"[QuizUI] BoxCollider {debugName}: size=({width}, {height}, {depth}), layer={button.gameObject.layer}");
        }

        private void ValidateReferences()
        {
            if (mainCanvas == null)
            {
                mainCanvas = GetComponent<Canvas>();
            }

            if (mainCanvas == null)
            {
                Debug.LogError("[QuizUI] Canvas non trouve! Ajoutez ce script a un Canvas.");
            }

            if (questionText == null)
            {
                Debug.LogWarning("[QuizUI] questionText non assigne!");
            }

            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null)
                {
                    Debug.LogWarning($"[QuizUI] answerButtons[{i}] non assigne!");
                }
            }
        }

        #endregion

        #region Private Methods - UI Logic

        private void ShowCanvas()
        {
            if (mainCanvas != null)
            {
                mainCanvas.enabled = true;
            }
        }

        private void SetButtonColor(int buttonIndex, Color color)
        {
            if (buttonIndex < 0 || buttonIndex >= buttonImages.Length) return;

            if (buttonImages[buttonIndex] != null)
            {
                buttonImages[buttonIndex].color = color;
            }

            // Aussi mettre a jour le ColorBlock du bouton pour la coherence
            if (answerButtons[buttonIndex] != null)
            {
                ColorBlock colors = answerButtons[buttonIndex].colors;
                colors.normalColor = color;
                colors.highlightedColor = color * 1.1f;
                colors.pressedColor = color * 0.9f;
                colors.selectedColor = color;
                answerButtons[buttonIndex].colors = colors;
            }
        }

        private void SetAllButtonsInteractable(bool interactable)
        {
            foreach (Button button in answerButtons)
            {
                if (button != null)
                {
                    button.interactable = interactable;
                }
            }
            isInteractable = interactable;
        }

        private int FindCorrectAnswerIndex()
        {
            if (currentChoices == null) return -1;

            for (int i = 0; i < currentChoices.Count; i++)
            {
                if (currentChoices[i].isCorrect)
                {
                    return i;
                }
            }
            return -1;
        }

        #endregion

        #region Private Methods - Event Handlers

        private void OnAnswerButtonClicked(int buttonIndex)
        {
            if (!isInteractable || currentChoices == null || buttonIndex >= currentChoices.Count)
            {
                return;
            }

            Debug.Log($"[QuizUI] Reponse selectionnee: index {buttonIndex}");

            // Desactiver l'interactivite immediatement
            SetAllButtonsInteractable(false);

            // Verifier si la reponse est correcte
            bool isCorrect = currentChoices[buttonIndex].isCorrect;

            // Lancer le feedback
            StartCoroutine(ShowFeedbackAndClose(buttonIndex, isCorrect));
        }

        private void OnRetryClicked()
        {
            Hide();

            // Relancer le quiz pour le meme tableau
            if (currentPainting != null && GameManager.Instance != null)
            {
                GameManager.Instance.StartQuizForPainting(currentPainting);
            }
        }

        private void OnQuizStartedHandler(PaintingController painting)
        {
            currentPainting = painting;
            PositionNearPainting(painting);
            ShowLoading();
        }

        private void OnQuizDataReceived(QuizData data, PaintingController painting)
        {
            if (painting == currentPainting)
            {
                ShowQuiz(data, painting);
            }
        }

        private void OnQuizErrorReceived(string error)
        {
            ShowError(error);
        }

        #endregion

        #region Coroutines

        private IEnumerator ShowFeedbackAndClose(int selectedIndex, bool isCorrect)
        {
            // 1. Feedback visuel immediat sur le bouton clique
            SetButtonColor(selectedIndex, isCorrect ? correctAnswerColor : wrongAnswerColor);

            // 2. Si faux, montrer aussi la bonne reponse en vert
            if (!isCorrect)
            {
                int correctIndex = FindCorrectAnswerIndex();
                if (correctIndex >= 0 && correctIndex < answerButtons.Length)
                {
                    SetButtonColor(correctIndex, correctAnswerColor);
                }
            }

            // 3. Afficher le texte de feedback avec les points
            int pointsEarned = 0;
            if (GameManager.Instance != null)
            {
                pointsEarned = isCorrect ? 100 : 0; // Valeur par defaut, sera ecrasee par GameManager
            }
            ShowFeedbackText(isCorrect, pointsEarned);

            // 4. Feedback audio/haptique (a implementer selon le SDK)
            PlayFeedback(isCorrect);

            // 5. Attendre la duree configuree
            yield return new WaitForSeconds(feedbackDuration);

            // 6. Notifier le GameManager
            NotifyGameManager(isCorrect);

            // 7. Declencher l'evenement local
            OnAnswerSelected?.Invoke(isCorrect, selectedIndex);

            // 8. Fermer l'UI
            Hide();
        }

        private void ShowFeedbackText(bool isCorrect, int points)
        {
            if (feedbackText == null) return;

            feedbackText.gameObject.SetActive(true);

            if (isCorrect)
            {
                feedbackText.text = $"+{points} pts!";
                feedbackText.color = correctAnswerColor;
            }
            else
            {
                feedbackText.text = "Rate...";
                feedbackText.color = wrongAnswerColor;
            }

            // Animation de scale (punch effect)
            StartCoroutine(AnimateFeedbackText());
        }

        private IEnumerator AnimateFeedbackText()
        {
            if (feedbackText == null) yield break;

            RectTransform rect = feedbackText.GetComponent<RectTransform>();
            if (rect == null) yield break;

            Vector3 originalScale = Vector3.one;
            Vector3 punchScale = Vector3.one * 1.3f;

            // Scale up
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rect.localScale = Vector3.Lerp(originalScale, punchScale, t);
                yield return null;
            }

            // Scale down
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rect.localScale = Vector3.Lerp(punchScale, originalScale, t);
                yield return null;
            }

            rect.localScale = originalScale;
        }

        private IEnumerator AnimateSpinner()
        {
            while (loadingSpinner != null)
            {
                loadingSpinner.Rotate(0, 0, -spinnerSpeed * Time.deltaTime);
                yield return null;
            }
        }

        private void StopSpinner()
        {
            if (spinnerCoroutine != null)
            {
                StopCoroutine(spinnerCoroutine);
                spinnerCoroutine = null;
            }
        }

        #endregion

        #region Private Methods - Game Integration

        private void NotifyGameManager(bool isCorrect)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnQuizAnswered(isCorrect);
            }
            else
            {
                Debug.LogWarning("[QuizUI] GameManager.Instance est null, impossible de notifier");
            }
        }

        private void PlayFeedback(bool isCorrect)
        {
            // Feedback haptique pour les controleurs VR (Meta XR)
            // OVRInput.SetControllerVibration(frequency, amplitude, controller);

            if (isCorrect)
            {
                // Vibration courte et douce pour succes
                // OVRInput.SetControllerVibration(0.5f, 0.3f, OVRInput.Controller.RTouch);
                Debug.Log("[QuizUI] Feedback: CORRECT!");
            }
            else
            {
                // Vibration plus longue pour echec
                // OVRInput.SetControllerVibration(0.8f, 0.5f, OVRInput.Controller.RTouch);
                Debug.Log("[QuizUI] Feedback: INCORRECT");
            }

            // TODO: Ajouter des sons (AudioSource.PlayOneShot)
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        [ContextMenu("Auto-Find UI References")]
        private void AutoFindReferences()
        {
            // Tenter de trouver automatiquement les references
            if (mainCanvas == null)
                mainCanvas = GetComponent<Canvas>();

            if (loadingPanel == null)
                loadingPanel = transform.Find("LoadingPanel")?.gameObject;

            if (quizPanel == null)
                quizPanel = transform.Find("QuizContentPanel")?.gameObject;

            if (errorPanel == null)
                errorPanel = transform.Find("ErrorPanel")?.gameObject;

            if (questionText == null)
                questionText = GetComponentInChildren<TMP_Text>();

            Debug.Log("[QuizUI] Auto-recherche des references terminee. Verifiez l'inspecteur.");
        }

        private void OnValidate()
        {
            // Validation dans l'editeur
            if (answerButtons.Length != 4)
            {
                System.Array.Resize(ref answerButtons, 4);
            }
            if (answerTexts.Length != 4)
            {
                System.Array.Resize(ref answerTexts, 4);
            }
        }
#endif

        #endregion
    }
}
