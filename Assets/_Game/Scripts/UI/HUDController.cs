/*
 * ============================================================================
 * HUD CONTROLLER - Affichage Montre Connectee (Timer + Score) pour VR
 * ============================================================================
 *
 * INSTRUCTIONS DE CONFIGURATION DANS UNITY:
 *
 * 1. CREER LE CANVAS WORLD SPACE:
 *    - GameObject > UI > Canvas
 *    - Renommer en "WristHUD"
 *    - Render Mode: "World Space"
 *    - Width: 200, Height: 80
 *    - Scale: X=0.0005, Y=0.0005, Z=0.0005 (tres petit pour tenir sur le poignet)
 *
 * 2. HIERARCHIE DU HUD:
 *
 *    WristHUD (Canvas + HUDController)
 *    |
 *    +-- Background (Image, semi-transparent)
 *    |   - Color: (20, 20, 30, 200)
 *    |   - Stretch to fill
 *    |
 *    +-- TimerText (TextMeshPro)
 *    |   - Text: "5:00"
 *    |   - Font Size: 42
 *    |   - Alignment: Center
 *    |
 *    +-- ScoreText (TextMeshPro)
 *        - Text: "0"
 *        - Font Size: 32
 *        - Alignment: Center
 *
 * 3. POSITIONNEMENT SUR LA MAIN GAUCHE:
 *    - Parenter le Canvas a: OVRCameraRig > TrackingSpace > LeftHandAnchor
 *    - Position locale: X=0, Y=0.05, Z=0.08 (au-dessus du poignet)
 *    - Rotation locale: X=90, Y=0, Z=0 (face vers le haut)
 *
 * 4. ASSIGNER DANS GAMEMANAGER:
 *    - Glisser le WristHUD dans le champ "Current HUD" du GameManager
 *
 * ============================================================================
 */

using UnityEngine;
using TMPro;
using MuseumAI.Core;

namespace MuseumAI.UI
{
    /// <summary>
    /// Controleur du HUD style montre connectee.
    /// Attache a la main gauche du joueur, affiche le timer et le score.
    /// Aucune logique de deplacement - le HUD suit naturellement son parent (la main).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References UI")]
        [Tooltip("Texte affichant le temps restant")]
        [SerializeField] private TMP_Text timerText;

        [Tooltip("Texte affichant le score")]
        [SerializeField] private TMP_Text scoreText;

        [Tooltip("Texte affichant la progression (ex: 2/5 tableaux)")]
        [SerializeField] private TMP_Text progressText;

        [Header("Animation Timer")]
        [Tooltip("Couleur normale du timer")]
        [SerializeField] private Color normalTimerColor = Color.white;

        [Tooltip("Couleur du timer quand le temps est bas (< 30s)")]
        [SerializeField] private Color lowTimeColor = new Color(1f, 0.3f, 0.3f, 1f);

        [Tooltip("Seuil en secondes pour le mode 'temps bas'")]
        [SerializeField] private float lowTimeThreshold = 30f;

        [Header("Animation Score")]
        [Tooltip("Couleur flash quand le score augmente")]
        [SerializeField] private Color scoreFlashColor = new Color(0.3f, 1f, 0.5f, 1f);

        #endregion

        #region Private Fields

        private int lastDisplayedScore = -1;
        private Coroutine scoreAnimationCoroutine;
        private Coroutine timerPulseCoroutine;
        private bool isLowTimeMode = false;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Appliquer le style futuriste
            ApplyFuturisticStyle();

            // S'abonner aux evenements du GameManager
            SubscribeToEvents();

            // Initialiser l'affichage
            if (GameManager.Instance != null)
            {
                UpdateTimerDisplay(GameManager.Instance.TimeRemaining);
                UpdateScoreDisplay(GameManager.Instance.Score);
                UpdateProgressDisplay(GameManager.Instance.PaintingsCompleted);
            }
            else
            {
                // Valeurs par defaut si pas de GameManager
                UpdateTimerDisplay(300f);
                UpdateScoreDisplay(0);
                UpdateProgressDisplay(0);
            }

            Debug.Log("[HUD] Initialise (mode montre connectee) avec style futuriste");
        }

        private void ApplyFuturisticStyle()
        {
            FuturisticHUDStyle style = GetComponent<FuturisticHUDStyle>();
            if (style == null)
            {
                style = gameObject.AddComponent<FuturisticHUDStyle>();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Met a jour l'affichage du timer
        /// </summary>
        public void UpdateTimerDisplay(float timeRemaining)
        {
            if (timerText == null) return;

            // Formatter en MM:SS
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";

            // Mode temps bas
            if (timeRemaining <= lowTimeThreshold && !isLowTimeMode)
            {
                isLowTimeMode = true;
                StartTimerPulse();
            }
            else if (timeRemaining > lowTimeThreshold && isLowTimeMode)
            {
                isLowTimeMode = false;
                StopTimerPulse();
                timerText.color = normalTimerColor;
            }
        }

        /// <summary>
        /// Met a jour l'affichage du score
        /// </summary>
        public void UpdateScoreDisplay(int score)
        {
            if (scoreText == null) return;

            // Afficher avec l'objectif si defini
            if (GameManager.Instance != null && GameManager.Instance.TargetScore > 0)
            {
                int target = GameManager.Instance.TargetScore;
                scoreText.text = $"{score}/{target}";

                // Couleur verte si objectif atteint
                if (score >= target)
                {
                    scoreText.color = new Color(0.3f, 1f, 0.5f, 1f); // Vert
                }
                else
                {
                    scoreText.color = new Color(0f, 1f, 0.6f, 1f); // Vert-cyan
                }
            }
            else
            {
                scoreText.text = score.ToString("N0");
            }

            // Animation si le score a augmente
            if (score > lastDisplayedScore && lastDisplayedScore >= 0)
            {
                AnimateScoreIncrease();
            }

            lastDisplayedScore = score;
        }

        /// <summary>
        /// Met a jour l'affichage de la progression
        /// </summary>
        public void UpdateProgressDisplay(int paintingsCompleted)
        {
            int target = 0;

            if (GameManager.Instance != null && GameManager.Instance.TargetPaintings > 0)
            {
                target = GameManager.Instance.TargetPaintings;

                // Texte de progression
                if (progressText != null)
                {
                    progressText.text = $"{paintingsCompleted}/{target}";

                    // Couleur verte si objectif atteint
                    if (paintingsCompleted >= target)
                    {
                        progressText.color = new Color(0.3f, 1f, 0.5f, 1f); // Vert
                    }
                    else
                    {
                        progressText.color = new Color(0f, 0.9f, 1f, 1f); // Cyan
                    }
                }
            }
            else
            {
                // Pas d'objectif de tableaux, afficher juste le compte
                if (progressText != null)
                {
                    progressText.text = paintingsCompleted.ToString();
                    progressText.color = new Color(0f, 0.9f, 1f, 1f);
                }
            }
        }

        /// <summary>
        /// Affiche le HUD
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Cache le HUD
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTimerUpdated += UpdateTimerDisplay;
                GameManager.Instance.OnScoreUpdated += UpdateScoreDisplay;
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                GameManager.Instance.OnQuizCompleted += OnQuizCompleted;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTimerUpdated -= UpdateTimerDisplay;
                GameManager.Instance.OnScoreUpdated -= UpdateScoreDisplay;
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
                GameManager.Instance.OnQuizCompleted -= OnQuizCompleted;
            }
        }

        private void OnQuizCompleted(bool success, int points)
        {
            // Mettre a jour la progression quand un quiz est complete
            if (GameManager.Instance != null)
            {
                UpdateProgressDisplay(GameManager.Instance.PaintingsCompleted);
            }
        }

        private void OnGameStateChanged(GameState newState)
        {
            // Cacher le HUD si on n'est pas en jeu
            if (newState == GameState.Playing)
            {
                Show();
            }
            else if (newState == GameState.GameOver || newState == GameState.MainMenu)
            {
                Hide();
            }
        }

        #endregion

        #region Animations

        private void AnimateScoreIncrease()
        {
            if (scoreAnimationCoroutine != null)
            {
                StopCoroutine(scoreAnimationCoroutine);
            }
            scoreAnimationCoroutine = StartCoroutine(ScoreFlashAnimation());
        }

        private System.Collections.IEnumerator ScoreFlashAnimation()
        {
            if (scoreText == null) yield break;

            Color originalColor = Color.white;
            RectTransform rect = scoreText.GetComponent<RectTransform>();
            Vector3 originalScale = Vector3.one;

            // Flash color + scale up
            scoreText.color = scoreFlashColor;
            if (rect != null)
            {
                rect.localScale = originalScale * 1.2f;
            }

            yield return new WaitForSeconds(0.1f);

            // Retour progressif
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                scoreText.color = Color.Lerp(scoreFlashColor, originalColor, t);
                if (rect != null)
                {
                    rect.localScale = Vector3.Lerp(originalScale * 1.2f, originalScale, t);
                }

                yield return null;
            }

            scoreText.color = originalColor;
            if (rect != null)
            {
                rect.localScale = originalScale;
            }
        }

        private void StartTimerPulse()
        {
            if (timerPulseCoroutine != null)
            {
                StopCoroutine(timerPulseCoroutine);
            }
            timerPulseCoroutine = StartCoroutine(TimerPulseAnimation());
        }

        private void StopTimerPulse()
        {
            if (timerPulseCoroutine != null)
            {
                StopCoroutine(timerPulseCoroutine);
                timerPulseCoroutine = null;
            }
        }

        private System.Collections.IEnumerator TimerPulseAnimation()
        {
            if (timerText == null) yield break;

            while (isLowTimeMode)
            {
                // Pulse vers rouge
                float pulseDuration = 0.5f;
                float elapsed = 0f;

                while (elapsed < pulseDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.PingPong(elapsed * 2f, 1f);
                    timerText.color = Color.Lerp(normalTimerColor, lowTimeColor, t);
                    yield return null;
                }
            }
        }

        #endregion
    }
}
