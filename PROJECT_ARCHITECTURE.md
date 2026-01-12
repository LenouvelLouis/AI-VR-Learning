# Architecture du Projet : Museum AI

Ce document détaille l'architecture logicielle et la structure des dossiers pour le projet de Serious Game en VR "Museum AI". L'objectif est de créer une base de code modulaire, évolutive et facile à maintenir.

## 1. Structure des Dossiers

Pour isoler les ressources du projet des assets tiers (Museum, Meta SDK, etc.), nous adoptons une hiérarchie de dossiers préfixée par `_Game`. Cela permet une navigation claire et évite les conflits.

```
Assets/
├── _Game/
│   ├── Materials/           # Matériaux spécifiques au jeu (UI, effets)
│   ├── Models/              # Modèles 3D custom (si nécessaire)
│   ├── Prefabs/             # GameObjects préfabriqués
│   │   ├── UI/              # Préfabs d'éléments d'interface (QuizPanel, etc.)
│   │   └── Interactables/   # Objets interactifs (Tableaux configurés, etc.)
│   ├── Scenes/              # Scènes du jeu (MainMenu, Museum, etc.)
│   ├── ScriptableObjects/   # Conteneurs de données (Config API, Infos Tableaux)
│   ├── Scripts/             # Tous les scripts C# du projet
│   │   ├── Core/            # Logique centrale (GameManager, SceneLoader)
│   │   ├── API/             # Gestion des appels à l'API Google
│   │   ├── Gameplay/        # Scripts liés à la mécanique de jeu (Painting, Player)
│   │   ├── UI/              # Contrôleurs d'interface utilisateur
│   │   └── Utilities/       # Classes utilitaires (Helpers, Extensions)
│   └── Shaders/             # Shaders custom
│
├── Museum/                  # (EXISTANT) Asset du musée
├── Oculus/                  # (EXISTANT) SDK Meta XR
└── ...                      # Autres dossiers d'assets & packages
```

## 2. Architecture des Scripts (Logique Applicative)

L'architecture logicielle s'articule autour de plusieurs scripts managers et contrôleurs qui communiquent via des références directes ou un système d'événements pour découpler la logique.

---

### `GameManager.cs` (Singleton)
- **Rôle :** Chef d'orchestre du jeu. Gère l'état global, le score, le timer et les transitions de scènes.
- **Emplacement :** `_Game/Scripts/Core/`
- **Diagramme simplifié :**
  ```csharp
  public class GameManager : MonoBehaviour
  {
      public static GameManager Instance { get; private set; }

      // --- Propriétés ---
      public int Score { get; private set; }
      public float TimeRemaining { get; private set; }
      public GameState CurrentState { get; private set; }

      // --- Événements ---
      public event Action<int> OnScoreUpdated;
      public event Action<float> OnTimerUpdated;
      public event Action<GameState> OnGameStateChanged;

      // --- Méthodes Publiques ---
      public void StartGame();
      public void EndGame(bool timeUp);
      public void AddScore(int points);
      public void StartQuizForPainting(PaintingController painting);
  }

  public enum GameState { MainMenu, Playing, Paused, GameOver }
  ```

---

### `APIManager.cs` (Singleton)
- **Rôle :** Gère toutes les communications avec l'API Google (Gemini/PaLM). Il est conçu pour être asynchrone afin de ne pas bloquer le thread principal.
- **Emplacement :** `_Game/Scripts/API/`
- **Diagramme simplifié :**
  ```csharp
  // Fichier de configuration pour la clé API
  [CreateAssetMenu(fileName = "ApiConfig", menuName = "_Game/ApiConfig")]
  public class ApiConfig : ScriptableObject {
      public string ApiKey;
  }

  // Classe pour la réponse de l'API
  public class QuizData {
      public string Question; // Ex: "Une de ces affirmations est vraie."
      public List<QuizChoice> Choices; // 1 vraie, 3 fausses
  }
  public class QuizChoice {
      public string Text;
      public bool IsCorrect;
  }

  public class APIManager : MonoBehaviour
  {
      public static APIManager Instance { get; private set; }
      
      [SerializeField] private ApiConfig apiConfig;

      // --- Méthode Publique ---
      // Prend le contexte du tableau et renvoie un quiz structuré
      public async Task<QuizData> GenerateQuizAsync(string paintingContext);
  }
  ```

---

### `PaintingController.cs`
- **Rôle :** Attaché à chaque tableau interactif. Il stocke les informations contextuelles du tableau et détecte l'interaction du joueur.
- **Emplacement :** `_Game/Scripts/Gameplay/`
- **Diagramme simplifié :**
  ```csharp
  public class PaintingController : MonoBehaviour
  {
      // --- Données du Tableau (éditable dans l'inspecteur) ---
      [TextArea(5, 10)]
      public string PaintingContext; // "La Nuit étoilée de Van Gogh, peint en 1889..."

      private bool isCompleted = false;

      // --- Logique d'interaction ---
      // Appelée par l'événement "Select" du XRSimpleInteractable ou équivalent
      public void OnPaintingSelected()
      {
          if (!isCompleted)
          {
              GameManager.Instance.StartQuizForPainting(this);
          }
      }

      public void MarkAsCompleted()
      {
          isCompleted = true;
          // Optionnel : ajouter un feedback visuel (ex: changer le matériau)
      }
  }
  ```

---

### `QuizUIController.cs`
- **Rôle :** Gère un panneau de quiz en World-Space (un préfab). Affiche les questions/réponses et gère la sélection du joueur.
- **Emplacement :** `_Game/Scripts/UI/`
- **Diagramme simplifié :**
  ```csharp
  public class QuizUIController : MonoBehaviour
  {
      [SerializeField] private TextMeshProUGUI questionText;
      [SerializeField] private Button[] answerButtons; // 4 boutons
      [SerializeField] private TextMeshProUGUI[] answerTexts;

      private QuizData currentQuiz;
      private PaintingController currentPainting;

      // --- Méthodes Publiques ---
      public void DisplayQuiz(QuizData quizData, PaintingController painting);
      public void OnAnswerSelected(int choiceIndex); // Liée aux boutons dans l'inspecteur
      private void ShowFeedback(bool isCorrect);
      private void ClosePanel();
  }
  ```

---

### `PlayerInteraction` (Concept, pas un script unique)
- **Rôle :** Il s'agit de la connexion entre le SDK Meta XR et la logique de jeu.
- **Composants Clés du SDK :**
  - **`XR Origin (VR)` / `OVRCameraRig` :** Le setup du joueur.
  - **`XRRayInteractor` / `OVRControllerHelper` :** Attaché aux contrôleurs, il émet un rayon pour pointer.
  - **`XRSimpleInteractable` (ou `IXRSelectInteractable`) :** Composant à ajouter aux GameObjects "Tableau". Il écoute les événements d'interaction (comme `selectEntered`).
- **Fonctionnement :**
  1. Le `XRRayInteractor` du joueur vise un tableau.
  2. Le tableau doit avoir un `Collider` pour être détecté.
  3. Il doit aussi avoir un composant `XRSimpleInteractable`.
  4. Dans l'inspecteur Unity, sur l'événement `Select Entered` du `XRSimpleInteractable`, on glisse le `PaintingController` du même GameObject et on sélectionne sa méthode publique `OnPaintingSelected()`.
  5. Ainsi, lorsque le joueur "clique" sur le tableau, le SDK exécute directement la logique de notre script.

## 3. Data Flow (Flux d'Information)

Le flux de données suit une séquence logique et événementielle :

1.  **Input Joueur :** Le joueur vise un tableau avec son `XRRayInteractor` et appuie sur la gâchette.
2.  **Détection d'Interaction :** Le composant `XRSimpleInteractable` sur le tableau intercepte l'événement et appelle `PaintingController.OnPaintingSelected()`.
3.  **Lancement du Quiz :** `PaintingController` notifie le `GameManager` : `GameManager.Instance.StartQuizForPainting(this)`.
4.  **Appel API :** Le `GameManager` demande à l'API de générer un quiz : `APIManager.Instance.GenerateQuizAsync(painting.PaintingContext)`. Le jeu affiche une icône de chargement.
5.  **Requête Asynchrone :** `APIManager` envoie une requête HTTP POST au service Google. Le jeu n'est pas figé pendant ce temps.
6.  **Réception & Parsing :** `APIManager` reçoit la réponse JSON, la désérialise en un objet `QuizData` C#.
7.  **Affichage UI :** Le `GameManager` reçoit l'objet `QuizData`. Il instancie le préfab du panneau de quiz (`QuizUIController`) à côté du tableau et appelle `quizUI.DisplayQuiz(quizData, painting)`.
8.  **Réponse du Joueur :** Le joueur clique sur un `Button` de l'interface. `QuizUIController.OnAnswerSelected(index)` est appelée.
9.  **Feedback & Score :** `QuizUIController` vérifie si la réponse est correcte, affiche un feedback (couleur, son) et appelle `GameManager.Instance.AddScore(points)` si la réponse est juste.
10. **Fin du Quiz :** Le `PaintingController` est marqué comme terminé (`MarkAsCompleted()`) et le panneau de quiz est détruit ou masqué.

## 4. Sécurité de la Clé API

**NE JAMAIS STOCKER UNE CLÉ API EN DUR DANS LE CODE SOURCE.** Cela la rend visible sur les dépôts Git et dans les binaires décompilés.

La méthode recommandée est d'utiliser un **`ScriptableObject`** dont le fichier d'asset est ignoré par le contrôle de version.

1.  **Créer le ScriptableObject :** Utilisez la classe `ApiConfig` définie plus haut.
2.  **Créer l'Asset :** Dans l'éditeur Unity, faites `Clic Droit > _Game > ApiConfig`. Cela crée un fichier `ApiConfig.asset` dans votre projet.
3.  **Renseigner la Clé :** Sélectionnez ce nouvel asset et collez votre clé API dans le champ "Api Key" de l'inspecteur.
4.  **Ignorer le Fichier :** Ajoutez la ligne suivante à votre fichier `.gitignore` à la racine du projet :
    ```
    # Ignorer la configuration contenant des clés secrètes
    Assets/_Game/ScriptableObjects/ApiConfig.asset
    ```
5.  **Utilisation :** Le script `APIManager` aura un champ `[SerializeField] private ApiConfig apiConfig;`. Glissez-y votre asset `ApiConfig.asset` depuis l'inspecteur. Le script lira la clé depuis cet objet sans qu'elle soit dans le code.
