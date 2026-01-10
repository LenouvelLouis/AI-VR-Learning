/*
 * ============================================================================
 * GAME OVER UI CONTROLLER - Ecran de Fin de Partie VR
 * ============================================================================
 *
 * INSTRUCTIONS DE CONFIGURATION DU PREFAB UNITY:
 *
 * 1. CREER LE CANVAS WORLD SPACE:
 *    - GameObject > UI > Canvas
 *    - Renommer en "GameOverPanel"
 *    - Render Mode: "World Space"
 *    - Width: 600, Height: 500
 *    - Scale: X=0.001, Y=0.001, Z=0.001
 *
 * 2. HIERARCHIE DU PANEL:
 *
 *    GameOverPanel (Canvas)
 *    |
 *    +-- Background (Image)
 *    |   - Color: (20, 20, 40, 240)
 *    |   - Stretch to fill
 *    |
 *    +-- TitleText (TextMeshPro)
 *    |   - Text: "TEMPS ECOULE!"
 *    |   - Font Size: 60
 *    |   - Color: Rouge/Or selon contexte
 *    |
 *    +-- StatsPanel (Vertical Layout)
 *    |   +-- ScoreLabel (TextMeshPro) "Score Final"
 *    |   +-- FinalScoreText (TextMeshPro) "1,250"
 *    |   +-- Spacer
 *    |   +-- PaintingsLabel (TextMeshPro) "Tableaux Completes"
 *    |   +-- PaintingsCountText (TextMeshPro) "5"
 *    |
 *    +-- ButtonsPanel (Horizontal Layout)
 *        +-- RetryButton (Button)
 *        |   - Text: "REJOUER"
 *        +-- QuitButton (Button)
 *            - Text: "QUITTER"
 *
 * 3. CONFIGURATION DES BOUTONS:
 *    - Ajouter BoxCollider pour detection par raycast VR
 *    - Taille recommandee: Width=200, Height=60
 *
 * ============================================================================
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using MuseumAI.Core;

namespace MuseumAI.UI
{
    /// <summary>
    /// Controleur de l'ecran de fin de partie.
    /// Affiche le score final et permet de rejouer.
    /// </summary>
    public class GameOverUIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Textes")]
        [Tooltip("Titre de l'ecran (Temps Ecoule! / Victoire! etc.)")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("Score final du joueur")]
        [SerializeField] private TMP_Text finalScoreText;

        [Tooltip("Nombre de tableaux completes")]
        [SerializeField] private TMP_Text paintingsCountText;

        [Tooltip("Texte optionnel pour le temps joue")]
        [SerializeField] private TMP_Text timePlayedText;

        [Header("Boutons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button quitButton;

        [Header("Couleurs")]
        [SerializeField] private Color timeUpTitleColor = new Color(1f, 0.4f, 0.3f, 1f);
        [SerializeField] private Color victoryTitleColor = new Color(1f, 0.85f, 0.2f, 1f);

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float statsRevealDelay = 0.3f;

        #endregion

        #region Private Fields

        private CanvasGroup canvasGroup;
        private float gameDuration;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            SetupButtons();
            SetupButtonColliders();
        }

        private void Start()
        {
            // Recuperer les stats du GameManager
            if (GameManager.Instance != null)
            {
                DisplayResults(
                    GameManager.Instance.Score,
                    GameManager.Instance.PaintingsCompleted,
                    GameManager.Instance.TimeRemaining <= 0
                );

                // Calculer le temps joue
                gameDuration = 300f; // Valeur par defaut, idealement recuperer du GameManager
            }

            // Animation d'entree
            StartCoroutine(FadeInAnimation());
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Affiche les resultats de la partie
        /// </summary>
        public void DisplayResults(int finalScore, int paintingsCompleted, bool timeUp)
        {
            // Titre
            if (titleText != null)
            {
                if (timeUp)
                {
                    titleText.text = "TEMPS ECOULE!";
                    titleText.color = timeUpTitleColor;
                }
                else
                {
                    titleText.text = "PARTIE TERMINEE!";
                    titleText.color = victoryTitleColor;
                }
            }

            // Score
            if (finalScoreText != null)
            {
                finalScoreText.text = finalScore.ToString("N0");
            }

            // Tableaux
            if (paintingsCountText != null)
            {
                paintingsCountText.text = paintingsCompleted.ToString();
            }

            Debug.Log($"[GameOverUI] Resultats affiches - Score: {finalScore}, Tableaux: {paintingsCompleted}");
        }

        /// <summary>
        /// Appele quand le joueur clique sur Rejouer
        /// </summary>
        public void OnRetryClicked()
        {
            Debug.Log("[GameOverUI] Retry clique");

            // Recharger la scene actuelle
            string currentScene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentScene);
        }

        /// <summary>
        /// Appele quand le joueur clique sur Quitter
        /// </summary>
        public void OnQuitClicked()
        {
            Debug.Log("[GameOverUI] Quit clique");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Private Methods

        private void SetupButtons()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        private void SetupButtonColliders()
        {
            // Ajouter des BoxColliders pour la detection par raycast VR
            SetupSingleButtonCollider(retryButton, "RetryButton");
            SetupSingleButtonCollider(quitButton, "QuitButton");
        }

        private void SetupSingleButtonCollider(Button button, string debugName)
        {
            if (button == null) return;

            BoxCollider existingCollider = button.GetComponent<BoxCollider>();
            if (existingCollider != null)
            {
                Destroy(existingCollider);
            }

            BoxCollider collider = button.gameObject.AddComponent<BoxCollider>();
            RectTransform rect = button.GetComponent<RectTransform>();

            float width = 200f;
            float height = 60f;

            if (rect != null && rect.rect.width > 0 && rect.rect.height > 0)
            {
                width = rect.rect.width;
                height = rect.rect.height;
            }

            collider.size = new Vector3(width, height, 20f);
            collider.center = Vector3.zero;

            Debug.Log($"[GameOverUI] BoxCollider {debugName}: size=({width}, {height}, 20)");
        }

        #endregion

        #region Animations

        private System.Collections.IEnumerator FadeInAnimation()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;

            // Reveler les stats avec animation
            yield return new WaitForSeconds(statsRevealDelay);

            // Animation du score (compteur)
            if (finalScoreText != null && GameManager.Instance != null)
            {
                yield return StartCoroutine(AnimateScoreCounter(GameManager.Instance.Score));
            }
        }

        private System.Collections.IEnumerator AnimateScoreCounter(int targetScore)
        {
            if (finalScoreText == null) yield break;

            float duration = 1.5f;
            float elapsed = 0f;
            int startScore = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Courbe d'acceleration/deceleration
                t = t * t * (3f - 2f * t);

                int currentScore = Mathf.FloorToInt(Mathf.Lerp(startScore, targetScore, t));
                finalScoreText.text = currentScore.ToString("N0");

                yield return null;
            }

            finalScoreText.text = targetScore.ToString("N0");
        }

        #endregion
    }
}
