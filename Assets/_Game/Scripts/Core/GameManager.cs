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
        [SerializeField] private float mainMenuDistance = 0.8f;

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
        /// Declenche en cas d'erreur API. Parametre: message d'erreur
        /// </summary>
        public event Action<string> OnQuizError;

        #endregion

        #region Private Fields

        private bool isTimerRunning;
        private PaintingController currentPainting;
        private GameObject gameOverInstance;
        private GameObject mainMenuInstance;

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
            else
            {
                // Afficher le menu principal
                SpawnMainMenu();
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

            // Activer les controles du joueur
            SetPlayerControlsEnabled(true);

            // Activer le HUD (deja place dans la scene)
            if (sceneHUD != null)
            {
                sceneHUD.Show();
            }
            else
            {
                Debug.LogError("[GameManager] sceneHUD non assigne! Glissez le WristHUD dans l'Inspector.");
            }

            // Detruire le menu principal s'il existe
            if (mainMenuInstance != null)
            {
                Destroy(mainMenuInstance);
                mainMenuInstance = null;
            }

            // Detruire l'ecran Game Over s'il existe
            if (gameOverInstance != null)
            {
                Destroy(gameOverInstance);
                gameOverInstance = null;
            }

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

            // Desactiver les controles du joueur
            SetPlayerControlsEnabled(false);

            // Cacher le HUD
            if (sceneHUD != null)
            {
                sceneHUD.Hide();
            }

            // Afficher l'ecran Game Over
            SpawnGameOverUI();

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

            // Verifier les conditions de victoire apres une bonne reponse
            if (isCorrect && CheckVictoryConditions())
            {
                Debug.Log("[GameManager] CONDITIONS DE VICTOIRE ATTEINTES!");
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
                // Mode "OU": l'une ou l'autre condition suffit
                return scoreReached || paintingsReached;
            }
            else
            {
                // Mode "ET": les deux conditions doivent etre remplies
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
            TimeRemaining = gameDuration;
            SetGameState(GameState.MainMenu);

            // Bloquer le mouvement
            SetPlayerControlsEnabled(false);

            // Afficher le menu
            SpawnMainMenu();

            Debug.Log("[GameManager] Retour au menu principal");
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

            Debug.Log($"[GameManager] Parametres mis a jour - Score: {targetScore}, Tableaux: {targetPaintings}, Duree: {gameDuration}s");
        }

        /// <summary>
        /// Redémarre la partie sans recharger la scène (plus rapide, meilleur pour VR)
        /// </summary>
        public void RestartGame()
        {
            Debug.Log("[GameManager] Redemarrage rapide...");

            // Detruire l'ecran Game Over
            if (gameOverInstance != null)
            {
                Destroy(gameOverInstance);
                gameOverInstance = null;
            }

            // Reinitialiser tous les tableaux
            ResetAllPaintings();

            // Reinitialiser les stats
            Score = 0;
            PaintingsCompleted = 0;
            TimeRemaining = gameDuration;
            currentPainting = null;

            // Redemarrer le timer
            isTimerRunning = true;

            // Reactiver les controles joueur
            SetPlayerControlsEnabled(true);

            // Afficher le HUD
            if (sceneHUD != null)
            {
                sceneHUD.Show();
            }

            // Changer l'etat
            SetGameState(GameState.Playing);

            // Notifier les listeners
            OnScoreUpdated?.Invoke(Score);
            OnTimerUpdated?.Invoke(TimeRemaining);

            Debug.Log("[GameManager] Partie redemarree!");
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
            Debug.Log($"[GameManager] {allPaintings.Length} tableau(x) reinitialise(s)");
        }

        #endregion

        #region Private Methods

        private void InitializeGame()
        {
            CurrentState = GameState.MainMenu;
            Score = 0;
            TimeRemaining = gameDuration;
            isTimerRunning = false;

            // Bloquer le mouvement au demarrage (en MainMenu)
            SetPlayerControlsEnabled(false);

            Debug.Log("[GameManager] Initialise");
        }

        private void SpawnMainMenu()
        {
            if (mainMenuPrefab == null)
            {
                Debug.LogWarning("[GameManager] MainMenu Prefab non assigne! Le jeu va demarrer directement.");
                StartGame();
                return;
            }

            // Detruire l'ancien menu s'il existe
            if (mainMenuInstance != null)
            {
                Destroy(mainMenuInstance);
            }

            // Position devant la camera
            Camera playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("[GameManager] Camera.main introuvable!");
                StartGame();
                return;
            }

            Transform camTransform = playerCamera.transform;

            // Position: devant la camera a la distance specifiee, a hauteur des yeux
            Vector3 spawnPosition = camTransform.position + camTransform.forward * mainMenuDistance;
            spawnPosition.y = camTransform.position.y;

            // Rotation: face au joueur
            Vector3 lookDirection = spawnPosition - camTransform.position;
            lookDirection.y = 0;
            Quaternion spawnRotation = Quaternion.LookRotation(lookDirection);

            mainMenuInstance = Instantiate(mainMenuPrefab, spawnPosition, spawnRotation);
            mainMenuInstance.name = "MainMenu_Instance";

            // S'assurer que le mouvement est bloque
            SetPlayerControlsEnabled(false);

            SetGameState(GameState.MainMenu);

            Debug.Log("[GameManager] Menu principal affiche");
        }

        private void SpawnGameOverUI()
        {
            if (gameOverPrefab == null)
            {
                Debug.LogWarning("[GameManager] GameOver Prefab non assigne!");
                return;
            }

            // Detruire l'ancien s'il existe
            if (gameOverInstance != null)
            {
                Destroy(gameOverInstance);
            }

            // Position devant la camera
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

            Vector3 lookDirection = spawnPosition - camTransform.position;
            lookDirection.y = 0;
            Quaternion spawnRotation = Quaternion.LookRotation(lookDirection);

            gameOverInstance = Instantiate(gameOverPrefab, spawnPosition, spawnRotation);
            gameOverInstance.name = "GameOver_Instance";

            Debug.Log("[GameManager] Ecran Game Over affiche");
        }

        private void SetPlayerControlsEnabled(bool enabled)
        {
            // NOTE: On ne desactive PAS PlayerInteraction!
            // Le joueur doit garder son laser pour interagir avec l'UI (Game Over, etc.)
            // Seul le deplacement est desactive.

            // Desactiver/activer UNIQUEMENT le mouvement (CharacterController, OVRPlayerController, etc.)
            if (playerMovement != null)
            {
                playerMovement.enabled = enabled;
                Debug.Log($"[GameManager] PlayerMovement: {(enabled ? "active" : "desactive")}");
            }
            else
            {
                Debug.LogWarning("[GameManager] playerMovement non assigne - le joueur peut toujours se deplacer!");
            }
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

            string monumentName = painting.PaintingTitle;
            string context = painting.GetFullContext();
            Debug.Log($"[GameManager] Demande de quiz a l'API pour: {monumentName}");

            // Appeler l'APIManager avec callbacks (nom du monument + contexte)
            APIManager.Instance.GenerateQuiz(
                monumentName,
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
