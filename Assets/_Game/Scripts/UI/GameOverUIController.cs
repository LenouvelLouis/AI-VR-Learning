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

        [Tooltip("Texte affichant la liste des monuments visites")]
        [SerializeField] private TMP_Text monumentsListText;

        [Tooltip("Texte affichant le detail du score (base + bonus)")]
        [SerializeField] private TMP_Text scoreBreakdownText;

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
            ApplyFuturisticStyle();
        }

        private void ApplyFuturisticStyle()
        {
            FuturisticGameOverStyle style = GetComponent<FuturisticGameOverStyle>();
            if (style == null)
            {
                style = gameObject.AddComponent<FuturisticGameOverStyle>();
            }

            // Mettre a jour les couleurs depuis le style
            if (style != null)
            {
                timeUpTitleColor = style.GetTimeUpColor();
                victoryTitleColor = style.GetVictoryColor();
            }
        }

        private void Start()
        {
            // Creer le texte pour la liste des monuments s'il n'existe pas
            CreateMonumentsListTextIfNeeded();

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

        /// <summary>
        /// Cree automatiquement le texte pour la liste des monuments s'il n'existe pas
        /// </summary>
        private void CreateMonumentsListTextIfNeeded()
        {
            // Chercher d'abord si un texte existe deja
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            foreach (var text in texts)
            {
                string nameLower = text.name.ToLower();
                if (nameLower.Contains("monument") || nameLower.Contains("list") || nameLower.Contains("visite"))
                {
                    monumentsListText = text;
                    return;
                }
            }

            // Creer un nouveau texte pour la liste des monuments
            GameObject monumentsTextGO = new GameObject("MonumentsListText");
            monumentsTextGO.transform.SetParent(transform, false);

            monumentsListText = monumentsTextGO.AddComponent<TextMeshProUGUI>();
            monumentsListText.fontSize = 18;
            monumentsListText.alignment = TextAlignmentOptions.Left;
            monumentsListText.color = Color.white;
            monumentsListText.enableWordWrapping = true;
            monumentsListText.richText = true;

            // Positionner en bas du panel (sous les boutons)
            RectTransform rect = monumentsListText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.05f);
            rect.anchorMax = new Vector2(0.9f, 0.28f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Debug.Log("[GameOverUI] MonumentsListText cree automatiquement");
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
                    titleText.text = "VICTOIRE!";
                    titleText.color = victoryTitleColor;
                }
            }

            // Mettre a jour le style du titre
            FuturisticGameOverStyle style = GetComponent<FuturisticGameOverStyle>();
            if (style != null)
            {
                style.UpdateTitleStyle(!timeUp);
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

            // Afficher le detail du score (base + bonus temps)
            DisplayScoreBreakdown(finalScore, paintingsCompleted, timeUp);

            // Liste des monuments visites
            DisplayMonumentsList();

            Debug.Log($"[GameOverUI] Resultats affiches - Score: {finalScore}, Tableaux: {paintingsCompleted}");
        }

        /// <summary>
        /// Affiche le detail du score (points de base + bonus temps)
        /// </summary>
        private void DisplayScoreBreakdown(int finalScore, int paintingsCompleted, bool timeUp)
        {
            // Creer le texte automatiquement s'il n'existe pas
            if (scoreBreakdownText == null)
            {
                CreateScoreBreakdownText();
            }

            if (scoreBreakdownText == null) return;

            // Calculer le score de base (100 points par tableau)
            int baseScore = paintingsCompleted * 100;
            int timeBonus = finalScore - baseScore;

            // Si le temps est ecoule, pas de bonus
            if (timeUp || timeBonus <= 0)
            {
                scoreBreakdownText.text = $"<color=#AAAAAA>Quiz: {baseScore} pts</color>";
            }
            else
            {
                scoreBreakdownText.text = $"<color=#AAAAAA>Quiz: {baseScore} pts</color>\n<color=#00FF88>+ Bonus temps: {timeBonus} pts</color>";
            }

            scoreBreakdownText.gameObject.SetActive(true);
        }

        /// <summary>
        /// Cree automatiquement le texte pour le detail du score
        /// </summary>
        private void CreateScoreBreakdownText()
        {
            GameObject breakdownTextGO = new GameObject("ScoreBreakdownText");
            breakdownTextGO.transform.SetParent(transform, false);

            scoreBreakdownText = breakdownTextGO.AddComponent<TextMeshProUGUI>();
            scoreBreakdownText.fontSize = 20;
            scoreBreakdownText.alignment = TextAlignmentOptions.Center;
            scoreBreakdownText.color = Color.white;
            scoreBreakdownText.enableWordWrapping = true;
            scoreBreakdownText.richText = true;

            // Positionner juste en dessous du score final
            RectTransform rect = scoreBreakdownText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.55f);
            rect.anchorMax = new Vector2(0.9f, 0.65f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Debug.Log("[GameOverUI] ScoreBreakdownText cree automatiquement");
        }

        /// <summary>
        /// Appele quand le joueur clique sur Rejouer
        /// </summary>
        public void OnRetryClicked()
        {
            Debug.Log("[GameOverUI] Retry clique");

            // Desactiver les boutons pour eviter les double-clics
            if (retryButton != null) retryButton.interactable = false;
            if (quitButton != null) quitButton.interactable = false;

            // Lancer la transition avec fade out
            StartCoroutine(RestartWithTransition());
        }

        private System.Collections.IEnumerator RestartWithTransition()
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

            // Utiliser le restart rapide du GameManager (sans recharger la scene)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                // Fallback: recharger la scene si GameManager n'existe pas
                Debug.LogWarning("[GameOverUI] GameManager introuvable, rechargement de la scene...");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            // Detruire ce panel (le GameManager le fera aussi, mais au cas ou)
            Destroy(gameObject);
        }

        /// <summary>
        /// Appele quand le joueur clique sur Quitter - retourne au menu principal
        /// </summary>
        public void OnQuitClicked()
        {
            Debug.Log("[GameOverUI] Quit clique - retour au menu principal");

            // Desactiver les boutons pour eviter les double-clics
            if (retryButton != null) retryButton.interactable = false;
            if (quitButton != null) quitButton.interactable = false;

            // Retourner au menu principal
            StartCoroutine(ReturnToMenuWithTransition());
        }

        private System.Collections.IEnumerator ReturnToMenuWithTransition()
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

            // Retourner au menu principal via GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }

            // Detruire ce panel
            Destroy(gameObject);
        }

        /// <summary>
        /// Affiche la liste des monuments visites
        /// </summary>
        private void DisplayMonumentsList()
        {
            // Chercher automatiquement le texte si non assigne
            if (monumentsListText == null)
            {
                TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
                foreach (var text in texts)
                {
                    string nameLower = text.name.ToLower();
                    if (nameLower.Contains("monument") || nameLower.Contains("list") || nameLower.Contains("visite"))
                    {
                        monumentsListText = text;
                        Debug.Log($"[GameOverUI] monumentsListText AUTO-ASSIGNE: {text.name}");
                        break;
                    }
                }
            }

            if (monumentsListText == null)
            {
                Debug.Log("[GameOverUI] Pas de texte pour la liste des monuments (normal si non configure)");
                return;
            }

            if (GameManager.Instance == null || GameManager.Instance.CompletedMonumentNames.Count == 0)
            {
                monumentsListText.text = "Aucun monument visite";
                monumentsListText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                return;
            }

            // Construire la liste des monuments
            var monuments = GameManager.Instance.CompletedMonumentNames;
            string listText = "<color=#00E5FF>Monuments decouverts:</color>\n";

            for (int i = 0; i < monuments.Count; i++)
            {
                listText += $"<color=#00FF88>✓</color> {monuments[i]}";
                if (i < monuments.Count - 1)
                {
                    listText += "\n";
                }
            }

            monumentsListText.text = listText;
            monumentsListText.color = Color.white;

            Debug.Log($"[GameOverUI] {monuments.Count} monument(s) affiches");
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

            // Effet pulse a la fin
            FuturisticGameOverStyle style = GetComponent<FuturisticGameOverStyle>();
            if (style != null)
            {
                style.PulseScore();
            }
        }

        #endregion
    }
}
