using System;
using System.Collections.Generic;
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

        [Header("Conditions de Victoire")]
        [Tooltip("Score a atteindre pour gagner (0 = desactive)")]
        [SerializeField] private int targetScore = 500;

        [Tooltip("Nombre de tableaux a completer pour gagner (0 = desactive)")]
        [SerializeField] private int targetPaintings = 5;

        [Tooltip("Mode de victoire: atteindre l'un OU l'autre objectif")]
        [SerializeField] private bool eitherConditionWins = true;

        [Header("Menu Principal")]
        [Tooltip("Prefab du menu principal")]
        [SerializeField] private GameObject mainMenuPrefab;

        [Tooltip("Distance du menu devant le joueur (en metres)")]
        [SerializeField] private float mainMenuDistance = 6f;

        [Header("Demarrage Automatique")]
        [Tooltip("Demarre automatiquement le jeu au lancement de la scene (sans menu)")]
        [SerializeField] private bool autoStartGame = false;

        [Tooltip("Delai avant le demarrage automatique (secondes)")]
        [SerializeField] private float autoStartDelay = 1f;

        [Header("References UI")]
        [SerializeField] private GameObject quizPanelPrefab;

        [Tooltip("Reference au HUD place dans la scene (sur la main gauche)")]
        [SerializeField] private HUDController sceneHUD;

        [Tooltip("Prefab de l'ecran Game Over")]
        [SerializeField] private GameObject gameOverPrefab;

        [Header("References Joueur")]
        [Tooltip("Script de mouvement a desactiver en Game Over (ex: CharacterController, OVRPlayerController, SimpleMovement...)")]
        [SerializeField] private MonoBehaviour playerMovement;

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

        /// <summary>
        /// Liste des noms des monuments visites avec succes
        /// </summary>
        public List<string> CompletedMonumentNames { get; private set; } = new List<string>();

        /// <summary>
        /// Score cible pour gagner
        /// </summary>
        public int TargetScore => targetScore;

        /// <summary>
        /// Nombre de tableaux cible pour gagner
        /// </summary>
        public int TargetPaintings => targetPaintings;

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
        /// Declenche quand le nombre de tableaux completes change
        /// </summary>
        public event Action<int> OnPaintingsProgressUpdated;

        /// <summary>
        /// Declenche en cas d'erreur API. Parametre: message d'erreur
        /// </summary>
        public event Action<string> OnQuizError;

        #endregion

        #region Private Fields

        private bool isTimerRunning;
        private PaintingController currentPainting;
        private GameObject gameOverInstance;
        private GameObject mainMenuInstance;
        private GameObject currentQuizPanelInstance;
        private QuizUIController currentQuizUIController;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (autoStartGame)
            {
                if (autoStartDelay > 0)
                {
                    Invoke(nameof(StartGame), autoStartDelay);
                }
                else
                {
                    StartGame();
                }
            }
            else
            {
                StartCoroutine(SpawnMainMenuDelayed());
            }
        }

        private System.Collections.IEnumerator SpawnMainMenuDelayed()
        {
            yield return new WaitForSeconds(1.0f);


            SpawnMainMenu();
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
            CompletedMonumentNames.Clear();
            TimeRemaining = gameDuration;
            isTimerRunning = true;

            SetGameState(GameState.Playing);

            SetPlayerControlsEnabled(true);

            if (sceneHUD != null)
            {
                sceneHUD.Show();
            }
            else
            {
                Debug.LogError("[GameManager] sceneHUD non assigne! Glissez le WristHUD dans l'Inspector.");
            }

            if (mainMenuInstance != null)
            {
                Destroy(mainMenuInstance);
                mainMenuInstance = null;
            }

            if (gameOverInstance != null)
            {
                Destroy(gameOverInstance);
                gameOverInstance = null;
            }

            OnScoreUpdated?.Invoke(Score);
            OnTimerUpdated?.Invoke(TimeRemaining);

        }

        /// <summary>
        /// Termine la partie
        /// </summary>
        /// <param name="timeUp">True si le temps est ecoule, False si fin volontaire</param>
        public void EndGame(bool timeUp)
        {
            isTimerRunning = false;

            if (!timeUp && TimeRemaining > 0)
            {
                int timeBonus = Mathf.FloorToInt(TimeRemaining) * bonusTimePoints;
                AddScore(timeBonus);
            }

            SetGameState(GameState.GameOver);

            SetPlayerControlsEnabled(false);

            if (sceneHUD != null)
            {
                sceneHUD.Hide();
            }

            SpawnGameOverUI();

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

        }

        /// <summary>
        /// Demarre un quiz pour un tableau specifique
        /// </summary>
        /// <param name="painting">Le controleur du tableau selectionne</param>
        public void StartQuizForPainting(PaintingController painting)
        {
            if (CurrentState != GameState.Playing)
            {
                return;
            }

            if (painting == null)
            {
                Debug.LogError("[GameManager] PaintingController null!");
                return;
            }

            currentPainting = painting;
            OnQuizStarted?.Invoke(painting);


            // L'APIManager sera appele ici pour generer le quiz
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
                    CompletedMonumentNames.Add(currentPainting.PaintingTitle);
                    currentPainting.MarkAsCompleted();
                }

                OnPaintingsProgressUpdated?.Invoke(PaintingsCompleted);
            }

            OnQuizCompleted?.Invoke(isCorrect, pointsEarned);
            currentPainting = null;


            if (isCorrect && CheckVictoryConditions())
            {
                EndGame(timeUp: false);
            }
        }

        /// <summary>
        /// Verifie si les conditions de victoire sont atteintes
        /// </summary>
        private bool CheckVictoryConditions()
        {
            bool scoreReached = targetScore > 0 && Score >= targetScore;
            bool paintingsReached = targetPaintings > 0 && PaintingsCompleted >= targetPaintings;

            if (eitherConditionWins)
            {
                return scoreReached || paintingsReached;
            }
            else
            {
                bool scoreOk = targetScore <= 0 || scoreReached;
                bool paintingsOk = targetPaintings <= 0 || paintingsReached;
                return scoreOk && paintingsOk;
            }
        }

        /// <summary>
        /// Retourne au menu principal
        /// </summary>
        public void ReturnToMainMenu()
        {
            isTimerRunning = false;

            Score = 0;
            PaintingsCompleted = 0;
            CompletedMonumentNames.Clear();
            TimeRemaining = gameDuration;
            currentPainting = null;

            if (sceneHUD != null)
            {
                sceneHUD.Hide();
            }

            if (currentQuizPanelInstance != null)
            {
                Destroy(currentQuizPanelInstance);
                currentQuizPanelInstance = null;
                currentQuizUIController = null;
            }

            SetGameState(GameState.MainMenu);

            SetPlayerControlsEnabled(false);

            ResetAllPaintings();

            SpawnMainMenu();

        }

        /// <summary>
        /// Configure les parametres du jeu (appele par le menu principal)
        /// </summary>
        /// <param name="newTargetScore">Score cible pour gagner</param>
        /// <param name="newTargetPaintings">Nombre de tableaux cible</param>
        /// <param name="newDuration">Duree du jeu en secondes</param>
        public void SetGameSettings(int newTargetScore, int newTargetPaintings, float newDuration)
        {
            targetScore = newTargetScore;
            targetPaintings = newTargetPaintings;
            gameDuration = newDuration;
            TimeRemaining = gameDuration;

        }

        /// <summary>
        /// Redémarre la partie sans recharger la scène (plus rapide, meilleur pour VR)
        /// </summary>
        public void RestartGame()
        {

            if (gameOverInstance != null)
            {
                Destroy(gameOverInstance);
                gameOverInstance = null;
            }

            ResetAllPaintings();

            Score = 0;
            PaintingsCompleted = 0;
            CompletedMonumentNames.Clear();
            TimeRemaining = gameDuration;
            currentPainting = null;

            isTimerRunning = true;

            SetPlayerControlsEnabled(true);

            if (sceneHUD != null)
            {
                sceneHUD.Show();
            }

            SetGameState(GameState.Playing);

            OnScoreUpdated?.Invoke(Score);
            OnTimerUpdated?.Invoke(TimeRemaining);

        }

        /// <summary>
        /// Reinitialise tous les tableaux de la scene
        /// </summary>
        private void ResetAllPaintings()
        {
            PaintingController[] allPaintings = FindObjectsByType<PaintingController>(FindObjectsSortMode.None);
            foreach (PaintingController painting in allPaintings)
            {
                painting.ResetState();
            }
        }

        #endregion

        #region Private Methods

        private void InitializeGame()
        {
            CurrentState = GameState.MainMenu;
            Score = 0;
            TimeRemaining = gameDuration;
            isTimerRunning = false;

            SetPlayerControlsEnabled(false);

        }

        private void SpawnMainMenu()
        {
            if (mainMenuPrefab == null)
            {
                StartGame();
                return;
            }

            if (mainMenuInstance != null)
            {
                Destroy(mainMenuInstance);
            }

            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("[GameManager] Camera.main introuvable!");
                StartGame();
                return;
            }

            Transform camTransform = playerCamera.transform;

            Vector3 forwardFlat = camTransform.forward;
            forwardFlat.y = 0;
            forwardFlat.Normalize();

            if (forwardFlat.sqrMagnitude < 0.01f)
            {
                forwardFlat = Vector3.forward;
            }

            Vector3 spawnPosition = camTransform.position + forwardFlat * mainMenuDistance;
            spawnPosition.y = camTransform.position.y; // Garder a hauteur des yeux

            spawnPosition = GetSafeSpawnPosition(camTransform.position, spawnPosition, 0.5f);

            Quaternion spawnRotation = Quaternion.LookRotation(forwardFlat);

            mainMenuInstance = Instantiate(mainMenuPrefab, spawnPosition, spawnRotation);
            mainMenuInstance.name = "MainMenu_Instance";

            // S'assurer que le mouvement est bloque
            SetPlayerControlsEnabled(false);

            SetGameState(GameState.MainMenu);

        }

        private void SpawnGameOverUI()
        {
            if (gameOverPrefab == null)
            {
                return;
            }

            if (gameOverInstance != null)
            {
                Destroy(gameOverInstance);
            }

            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("[GameManager] Camera.main introuvable!");
                return;
            }

            Transform camTransform = playerCamera.transform;
            float spawnDistance = 1.0f;
            Vector3 spawnPosition = camTransform.position + camTransform.forward * spawnDistance;
            spawnPosition.y = camTransform.position.y; // A hauteur des yeux

            spawnPosition = GetSafeSpawnPosition(camTransform.position, spawnPosition, 0.3f);

            Vector3 lookDirection = spawnPosition - camTransform.position;
            lookDirection.y = 0;
            Quaternion spawnRotation = Quaternion.LookRotation(lookDirection);

            gameOverInstance = Instantiate(gameOverPrefab, spawnPosition, spawnRotation);
            gameOverInstance.name = "GameOver_Instance";

        }

        /// <summary>
        /// Verifie si la position cible est bloquee par un mur et retourne une position securisee
        /// </summary>
        /// <param name="origin">Position de depart (camera)</param>
        /// <param name="targetPosition">Position cible souhaitee</param>
        /// <param name="offsetFromWall">Distance de securite par rapport au mur</param>
        /// <returns>Position securisee (devant le mur si obstacle, sinon position cible)</returns>
        private Vector3 GetSafeSpawnPosition(Vector3 origin, Vector3 targetPosition, float offsetFromWall)
        {
            Vector3 direction = targetPosition - origin;
            float distance = direction.magnitude;

            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance))
            {
                Vector3 safePosition = hit.point - direction.normalized * offsetFromWall;
                return safePosition;
            }

            return targetPosition;
        }

        private void SetPlayerControlsEnabled(bool enabled)
        {
            // NOTE: On ne desactive PAS PlayerInteraction!

            if (playerMovement != null)
            {
                playerMovement.enabled = enabled;
            }
            else
            {
            }
        }

        private void SetGameState(GameState newState)
        {
            if (CurrentState == newState) return;

            GameState previousState = CurrentState;
            CurrentState = newState;

            OnGameStateChanged?.Invoke(newState);

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
            SpawnQuizPanelWithLoading(painting);

            if (APIManager.Instance == null)
            {
                string error = "APIManager non disponible!";
                Debug.LogError($"[GameManager] {error}");
                OnQuizError?.Invoke(error);
                ShowQuizError(error);
                return;
            }

            if (!APIManager.Instance.IsConfigured)
            {
                string error = "APIManager non configure. Verifiez la cle API dans Resources/ApiConfig.";
                Debug.LogError($"[GameManager] {error}");
                OnQuizError?.Invoke(error);
                ShowQuizError(error);
                return;
            }

            string monumentName = painting.PaintingTitle;
            string context = painting.GetFullContext();

            APIManager.Instance.GenerateQuiz(
                monumentName,
                context,
                onSuccess: (quizData) => OnQuizGenerated(quizData, painting),
                onError: (error) => OnQuizGenerationFailed(error)
            );
        }

        /// <summary>
        /// Spawn le panel quiz immediatement et affiche l'etat de chargement
        /// </summary>
        private void SpawnQuizPanelWithLoading(PaintingController painting)
        {
            if (quizPanelPrefab == null)
            {
                return;
            }

            if (currentQuizPanelInstance != null)
            {
                Destroy(currentQuizPanelInstance);
            }

            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("[GameManager] Camera.main introuvable!");
                return;
            }

            Transform camTransform = playerCamera.transform;
            float spawnDistance = 2f; // 2 metres devant le joueur
            Vector3 spawnPosition = camTransform.position + camTransform.forward * spawnDistance;

            spawnPosition = GetSafeSpawnPosition(camTransform.position, spawnPosition, 0.3f);

            Vector3 directionAwayFromCamera = spawnPosition - camTransform.position;
            directionAwayFromCamera.y = 0;
            Quaternion spawnRotation = Quaternion.LookRotation(directionAwayFromCamera);

            currentQuizPanelInstance = Instantiate(quizPanelPrefab, spawnPosition, spawnRotation);
            currentQuizPanelInstance.name = "QuizPanel_Instance";

            currentQuizUIController = currentQuizPanelInstance.GetComponent<QuizUIController>();
            if (currentQuizUIController != null)
            {
                currentQuizUIController.ShowLoading();
            }
        }

        /// <summary>
        /// Affiche une erreur sur le panel quiz existant
        /// </summary>
        private void ShowQuizError(string error)
        {
            if (currentQuizUIController != null)
            {
                currentQuizUIController.ShowError(error);
            }
        }

        private void OnQuizGenerated(QuizData quizData, PaintingController painting)
        {

            OnQuizDataReady?.Invoke(quizData, painting);

            if (currentQuizUIController != null)
            {
                currentQuizUIController.ShowQuiz(quizData, painting);
            }
            else
            {
                DisplayQuizUI(quizData, painting);
            }
        }

        private void OnQuizGenerationFailed(string error)
        {
            Debug.LogError($"[GameManager] Echec generation quiz: {error}");
            OnQuizError?.Invoke(error);

            ShowQuizError(error);

            currentPainting = null;
        }

        private void DisplayQuizUI(QuizData quizData, PaintingController painting)
        {
            if (quizPanelPrefab == null)
            {
                return;
            }

            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("[GameManager] Camera.main introuvable! L'UI ne peut pas etre positionnee.");
                return;
            }

            Transform camTransform = playerCamera.transform;

            float spawnDistance = 2f;
            Vector3 spawnPosition = camTransform.position + camTransform.forward * spawnDistance;

            // => LookRotation(direction VERS camera) mais on inverse pour que la face avant soit visible
            Vector3 directionAwayFromCamera = spawnPosition - camTransform.position;
            directionAwayFromCamera.y = 0; // Garder l'UI verticale
            Quaternion spawnRotation = Quaternion.LookRotation(directionAwayFromCamera);


            GameObject quizPanelInstance = Instantiate(quizPanelPrefab, spawnPosition, spawnRotation);

            QuizUIController uiController = quizPanelInstance.GetComponent<QuizUIController>();
            if (uiController != null)
            {
                uiController.ShowQuiz(quizData, painting);
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
