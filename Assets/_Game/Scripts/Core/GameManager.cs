using System;
using UnityEngine;
using MuseumAI.API;
using MuseumAI.Gameplay;
using MuseumAI.UI;

namespace MuseumAI.Core
{
    /// <summary>
    /// Etats possibles du jeu
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// Chef d'orchestre du jeu. Gere l'etat global, le score, le timer et les transitions.
    /// Pattern Singleton pour un acces global.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton

        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeGame();
        }

        #endregion

        #region Configuration

        [Header("Configuration du Jeu")]
        [SerializeField] private float gameDuration = 300f; // 5 minutes par defaut
        [SerializeField] private int pointsPerCorrectAnswer = 100;
        [SerializeField] private int bonusTimePoints = 10; // Points bonus par seconde restante

        [Header("Demarrage Automatique")]
        [Tooltip("Demarre automatiquement le jeu au lancement de la scene")]
        [SerializeField] private bool autoStartGame = true;

        [Tooltip("Delai avant le demarrage automatique (secondes)")]
        [SerializeField] private float autoStartDelay = 1f;

        [Header("References")]
        [SerializeField] private GameObject quizPanelPrefab;

        #endregion

        #region Properties

        /// <summary>
        /// Score actuel du joueur
        /// </summary>
        public int Score { get; private set; }

        /// <summary>
        /// Temps restant en secondes
        /// </summary>
        public float TimeRemaining { get; private set; }

        /// <summary>
        /// Etat actuel du jeu
        /// </summary>
        public GameState CurrentState { get; private set; }

        /// <summary>
        /// Nombre de tableaux completes
        /// </summary>
        public int PaintingsCompleted { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Declenche quand le score change. Parametre: nouveau score
        /// </summary>
        public event Action<int> OnScoreUpdated;

        /// <summary>
        /// Declenche chaque frame quand le timer change. Parametre: temps restant
        /// </summary>
        public event Action<float> OnTimerUpdated;

        /// <summary>
        /// Declenche quand l'etat du jeu change. Parametre: nouvel etat
        /// </summary>
        public event Action<GameState> OnGameStateChanged;

        /// <summary>
        /// Declenche quand un quiz demarre. Parametre: le PaintingController concerne
        /// </summary>
        public event Action<PaintingController> OnQuizStarted;

        /// <summary>
        /// Declenche quand les donnees du quiz sont pretes. Parametres: QuizData, PaintingController
        /// </summary>
        public event Action<QuizData, PaintingController> OnQuizDataReady;

        /// <summary>
        /// Declenche quand un quiz se termine. Parametres: succes, points gagnes
        /// </summary>
        public event Action<bool, int> OnQuizCompleted;

        /// <summary>
        /// Declenche en cas d'erreur API. Parametre: message d'erreur
        /// </summary>
        public event Action<string> OnQuizError;

        #endregion

        #region Private Fields

        private bool isTimerRunning;
        private PaintingController currentPainting;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Demarrage automatique si configure
            if (autoStartGame)
            {
                if (autoStartDelay > 0)
                {
                    Invoke(nameof(StartGame), autoStartDelay);
                    Debug.Log($"[GameManager] Demarrage automatique dans {autoStartDelay}s...");
                }
                else
                {
                    StartGame();
                }
            }
        }

        private void Update()
        {
            if (isTimerRunning && CurrentState == GameState.Playing)
            {
                UpdateTimer();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Demarre une nouvelle partie
        /// </summary>
        public void StartGame()
        {
            Score = 0;
            PaintingsCompleted = 0;
            TimeRemaining = gameDuration;
            isTimerRunning = true;

            SetGameState(GameState.Playing);

            OnScoreUpdated?.Invoke(Score);
            OnTimerUpdated?.Invoke(TimeRemaining);

            Debug.Log("[GameManager] Partie demarree!");
        }

        /// <summary>
        /// Termine la partie
        /// </summary>
        /// <param name="timeUp">True si le temps est ecoule, False si fin volontaire</param>
        public void EndGame(bool timeUp)
        {
            isTimerRunning = false;

            // Bonus de temps si fin avant le chrono
            if (!timeUp && TimeRemaining > 0)
            {
                int timeBonus = Mathf.FloorToInt(TimeRemaining) * bonusTimePoints;
                AddScore(timeBonus);
                Debug.Log($"[GameManager] Bonus temps: +{timeBonus} points");
            }

            SetGameState(GameState.GameOver);

            Debug.Log($"[GameManager] Partie terminee! Score final: {Score}, Tableaux completes: {PaintingsCompleted}");
        }

        /// <summary>
        /// Ajoute des points au score
        /// </summary>
        /// <param name="points">Nombre de points a ajouter</param>
        public void AddScore(int points)
        {
            if (points <= 0) return;

            Score += points;
            OnScoreUpdated?.Invoke(Score);

            Debug.Log($"[GameManager] +{points} points! Score total: {Score}");
        }

        /// <summary>
        /// Demarre un quiz pour un tableau specifique
        /// </summary>
        /// <param name="painting">Le controleur du tableau selectionne</param>
        public void StartQuizForPainting(PaintingController painting)
        {
            if (CurrentState != GameState.Playing)
            {
                Debug.LogWarning("[GameManager] Impossible de demarrer un quiz hors du mode Playing");
                return;
            }

            if (painting == null)
            {
                Debug.LogError("[GameManager] PaintingController null!");
                return;
            }

            currentPainting = painting;
            OnQuizStarted?.Invoke(painting);

            Debug.Log($"[GameManager] Quiz demarre pour: {painting.gameObject.name}");

            // L'APIManager sera appele ici pour generer le quiz
            // Pour l'instant, on log l'intention
            RequestQuizFromAPI(painting);
        }

        /// <summary>
        /// Met le jeu en pause
        /// </summary>
        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                isTimerRunning = false;
                SetGameState(GameState.Paused);
                Debug.Log("[GameManager] Jeu en pause");
            }
        }

        /// <summary>
        /// Reprend le jeu apres une pause
        /// </summary>
        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                isTimerRunning = true;
                SetGameState(GameState.Playing);
                Debug.Log("[GameManager] Jeu repris");
            }
        }

        /// <summary>
        /// Appele quand un quiz est complete
        /// </summary>
        /// <param name="isCorrect">True si la reponse etait correcte</param>
        public void OnQuizAnswered(bool isCorrect)
        {
            int pointsEarned = 0;

            if (isCorrect)
            {
                pointsEarned = pointsPerCorrectAnswer;
                AddScore(pointsEarned);
                PaintingsCompleted++;

                if (currentPainting != null)
                {
                    currentPainting.MarkAsCompleted();
                }
            }

            OnQuizCompleted?.Invoke(isCorrect, pointsEarned);
            currentPainting = null;

            Debug.Log($"[GameManager] Quiz termine - Correct: {isCorrect}, Points: {pointsEarned}");
        }

        /// <summary>
        /// Retourne au menu principal
        /// </summary>
        public void ReturnToMainMenu()
        {
            isTimerRunning = false;
            Score = 0;
            TimeRemaining = gameDuration;
            SetGameState(GameState.MainMenu);

            Debug.Log("[GameManager] Retour au menu principal");
        }

        #endregion

        #region Private Methods

        private void InitializeGame()
        {
            CurrentState = GameState.MainMenu;
            Score = 0;
            TimeRemaining = gameDuration;
            isTimerRunning = false;

            Debug.Log("[GameManager] Initialise");
        }

        private void SetGameState(GameState newState)
        {
            if (CurrentState == newState) return;

            GameState previousState = CurrentState;
            CurrentState = newState;

            OnGameStateChanged?.Invoke(newState);

            Debug.Log($"[GameManager] Etat: {previousState} -> {newState}");
        }

        private void UpdateTimer()
        {
            TimeRemaining -= Time.deltaTime;
            OnTimerUpdated?.Invoke(TimeRemaining);

            if (TimeRemaining <= 0)
            {
                TimeRemaining = 0;
                EndGame(timeUp: true);
            }
        }

        private void RequestQuizFromAPI(PaintingController painting)
        {
            // Verifier que l'APIManager est disponible
            if (APIManager.Instance == null)
            {
                string error = "APIManager non disponible!";
                Debug.LogError($"[GameManager] {error}");
                OnQuizError?.Invoke(error);
                return;
            }

            if (!APIManager.Instance.IsConfigured)
            {
                string error = "APIManager non configure. Verifiez la cle API.";
                Debug.LogError($"[GameManager] {error}");
                OnQuizError?.Invoke(error);
                return;
            }

            string context = painting.GetFullContext();
            Debug.Log($"[GameManager] Demande de quiz a l'API pour: {context.Substring(0, Mathf.Min(50, context.Length))}...");

            // Appeler l'APIManager avec callbacks
            APIManager.Instance.GenerateQuiz(
                context,
                onSuccess: (quizData) => OnQuizGenerated(quizData, painting),
                onError: (error) => OnQuizGenerationFailed(error)
            );
        }

        private void OnQuizGenerated(QuizData quizData, PaintingController painting)
        {
            Debug.Log($"[GameManager] Quiz recu: {quizData.question}");

            // Notifier les listeners (UI, etc.)
            OnQuizDataReady?.Invoke(quizData, painting);

            // Afficher le quiz
            DisplayQuizUI(quizData, painting);
        }

        private void OnQuizGenerationFailed(string error)
        {
            Debug.LogError($"[GameManager] Echec generation quiz: {error}");
            OnQuizError?.Invoke(error);

            // Liberer le painting actuel pour permettre une nouvelle tentative
            currentPainting = null;
        }

        private void DisplayQuizUI(QuizData quizData, PaintingController painting)
        {
            if (quizPanelPrefab == null)
            {
                Debug.LogWarning("[GameManager] QuizPanelPrefab non assigne!");
                return;
            }

            // === POSITIONNEMENT DEVANT LA CAMERA ===
            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("[GameManager] Camera.main introuvable! L'UI ne peut pas etre positionnee.");
                return;
            }

            Transform camTransform = playerCamera.transform;

            // Position: 0.8m devant la camera, a hauteur des yeux
            float spawnDistance = 0.8f;
            Vector3 spawnPosition = camTransform.position + camTransform.forward * spawnDistance;

            // Rotation: L'UI doit FAIRE FACE au joueur
            // Un Canvas World Space affiche son contenu sur sa face AVANT (Z-)
            // Donc le forward du Canvas doit pointer VERS le joueur pour que le texte soit lisible
            // => LookRotation(direction VERS camera) mais on inverse pour que la face avant soit visible
            Vector3 directionAwayFromCamera = spawnPosition - camTransform.position;
            directionAwayFromCamera.y = 0; // Garder l'UI verticale
            Quaternion spawnRotation = Quaternion.LookRotation(directionAwayFromCamera);

            Debug.Log($"[GameManager] Quiz UI spawn: pos={spawnPosition}, dist={spawnDistance}m devant camera");

            GameObject quizPanelInstance = Instantiate(quizPanelPrefab, spawnPosition, spawnRotation);

            // Configurer le QuizUIController
            QuizUIController uiController = quizPanelInstance.GetComponent<QuizUIController>();
            if (uiController != null)
            {
                uiController.ShowQuiz(quizData, painting);
                Debug.Log($"[GameManager] Quiz UI affiche pour: {painting.PaintingTitle}");
            }
            else
            {
                Debug.LogError("[GameManager] QuizUIController non trouve sur le prefab!");
                Destroy(quizPanelInstance);
            }
        }

        #endregion
    }
}
