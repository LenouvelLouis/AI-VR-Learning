using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MuseumAI.Core;

namespace MuseumAI.Gameplay
{
    /// <summary>
    /// Gere l'interaction du joueur avec les tableaux via Raycast.
    /// Supporte: Meta XR (casque), XR Simulator, et Desktop (clavier/souris).
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Configuration du Raycast")]
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private float rayDistance = 3f; // 3 metres - distance realiste pour interagir
        [SerializeField] private LayerMask interactableLayers;

        [Header("Input VR (Meta XR / Casque)")]
        [SerializeField] private bool useOVRInput = true;
        [SerializeField] private OVRInput.Controller vrController = OVRInput.Controller.RTouch;

        [Header("Input Desktop & Simulateur XR")]
        [SerializeField] private bool allowDesktopInput = true;
        [SerializeField] private KeyCode desktopInteractKey = KeyCode.Space;

        [Header("Visualisation du Rayon")]
        [SerializeField] private bool showRayVisual = true;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Color rayColorIdle = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color rayColorTargeting = new Color(0f, 1f, 0.5f, 0.8f);
        [SerializeField] private Color rayColorUI = new Color(0.3f, 0.7f, 1f, 0.9f);

        [Header("Feedback Haptique")]
        [SerializeField] private bool enableHapticFeedback = true;

        #endregion

        #region Private Fields

        private PaintingController currentTarget;
        private Button currentButtonTarget;
        private bool isInteractionBlocked;
        private string lastLoggedTargetName = "";
        private int frameCount = 0;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            Debug.Log($"[PlayerInteraction] === OnEnable === GameObject: {gameObject.name}");
        }

        private void OnDisable()
        {
            Debug.Log($"[PlayerInteraction] === OnDisable === GameObject: {gameObject.name}");
        }

        private void Start()
        {
            // === DIAGNOSTIC: Afficher les valeurs AVANT modification ===
            Debug.Log($"[PlayerInteraction] START DIAGNOSTIC - AVANT modification:");
            Debug.Log($"    desktopInteractKey = {desktopInteractKey} (int: {(int)desktopInteractKey})");
            Debug.Log($"    allowDesktopInput = {allowDesktopInput}");
            Debug.Log($"    useOVRInput = {useOVRInput}");
            Debug.Log($"    GameObject actif: {gameObject.activeInHierarchy}");
            Debug.Log($"    Script enabled: {enabled}");

            // === FORCE INPUT DESKTOP ===
            allowDesktopInput = true;

            // === DIAGNOSTIC: Afficher les valeurs APRES modification ===
            Debug.Log($"[PlayerInteraction] START DIAGNOSTIC - APRES modification:");
            Debug.Log($"    desktopInteractKey = {desktopInteractKey} (int: {(int)desktopInteractKey})");
            Debug.Log($"    allowDesktopInput = {allowDesktopInput}");

            // Configuration du ray origin
            if (rayOrigin == null)
            {
                rayOrigin = transform;
                Debug.Log("[PlayerInteraction] rayOrigin = ce transform");
            }

            // Configuration du LineRenderer
            if (showRayVisual)
            {
                if (lineRenderer == null)
                {
                    lineRenderer = GetComponent<LineRenderer>();
                }
                if (lineRenderer == null)
                {
                    CreateDefaultLineRenderer();
                    Debug.Log("[PlayerInteraction] LineRenderer cree automatiquement");
                }
            }

            // Abonnement aux evenements
            SubscribeToEvents();

            // Desactiver l'InputModule standard pour eviter les clics automatiques sur hover
            DisableStandardUIInputModule();

            Debug.Log($"[PlayerInteraction] === INITIALISATION COMPLETE ===");
        }

        /// <summary>
        /// Desactive le module d'input UI standard pour eviter les clics automatiques
        /// quand le pointeur VR survole un bouton. Notre PlayerInteraction gere tout.
        /// </summary>
        private void DisableStandardUIInputModule()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.Log("[PlayerInteraction] Pas d'EventSystem trouve");
                return;
            }

            // Desactiver tous les modules d'input UI (StandaloneInputModule, InputSystemUIInputModule, etc.)
            BaseInputModule[] inputModules = eventSystem.GetComponents<BaseInputModule>();
            foreach (var module in inputModules)
            {
                module.enabled = false;
                Debug.Log($"[PlayerInteraction] Module UI desactive: {module.GetType().Name}");
            }

            Debug.Log("[PlayerInteraction] Modules UI desactives - seul PlayerInteraction gere les clics");
        }

        private void Update()
        {
            // === DIAGNOSTIC #1: Confirmer que Update() tourne ===
            frameCount++;
            if (frameCount % 300 == 1) // Log toutes les 5 secondes environ (60fps * 5)
            {
                Debug.Log($"[PlayerInteraction] Update() ACTIF - Frame #{frameCount}");
            }

            // === RAYCAST ===
            PerformRaycast();

            // === INTERACTION ===
            CheckInputDirect();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Raycast

        private void PerformRaycast()
        {
            Vector3 origin = rayOrigin.position;
            Vector3 direction = rayOrigin.forward;

            Debug.DrawRay(origin, direction * rayDistance, Color.red);

            // === RAYCAST 1: Detection UI (tous les layers) ===
            // On cherche d'abord les elements UI car ils ont priorite
            RaycastHit uiHit;
            if (Physics.Raycast(origin, direction, out uiHit, rayDistance))
            {
                // Chercher un Button
                Button button = uiHit.collider.GetComponent<Button>();
                if (button == null)
                {
                    button = uiHit.collider.GetComponentInParent<Button>();
                }

                if (button != null && button.interactable)
                {
                    // C'est un bouton UI cliquable
                    SetCurrentButtonTarget(button);
                    SetCurrentTarget(null);
                    UpdateRayVisual(uiHit.point);
                    return;
                }
            }

            // === RAYCAST 2: Detection tableaux (layers specifiques) ===
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, rayDistance, interactableLayers))
            {
                PaintingController painting = hit.collider.GetComponent<PaintingController>();
                if (painting == null)
                {
                    painting = hit.collider.GetComponentInParent<PaintingController>();
                }

                SetCurrentButtonTarget(null);
                SetCurrentTarget(painting);
                UpdateRayVisual(hit.point);
            }
            else
            {
                SetCurrentButtonTarget(null);
                SetCurrentTarget(null);
                UpdateRayVisual(origin + direction * rayDistance);
            }
        }

        private void SetCurrentButtonTarget(Button newButton)
        {
            if (currentButtonTarget == newButton) return;

            currentButtonTarget = newButton;

            if (currentButtonTarget != null)
            {
                Debug.Log($"[CIBLE UI] >> Bouton: {currentButtonTarget.gameObject.name} <<");

                if (enableHapticFeedback && useOVRInput)
                {
                    try
                    {
                        OVRInput.SetControllerVibration(0.05f, 0.05f, vrController);
                        Invoke(nameof(StopHaptic), 0.05f);
                    }
                    catch (System.Exception) { /* Ignore in simulator */ }
                }
            }
        }

        private void SetCurrentTarget(PaintingController newTarget)
        {
            if (currentTarget == newTarget) return;

            currentTarget = newTarget;

            if (currentTarget != null)
            {
                string targetName = currentTarget.PaintingTitle;
                if (targetName != lastLoggedTargetName)
                {
                    Debug.Log($"[CIBLE] >> {targetName} <<");
                    lastLoggedTargetName = targetName;

                    if (enableHapticFeedback && useOVRInput)
                    {
                        try
                        {
                            OVRInput.SetControllerVibration(0.1f, 0.1f, vrController);
                            Invoke(nameof(StopHaptic), 0.1f);
                        }
                        catch (System.Exception) { /* Ignore in simulator */ }
                    }
                }
            }
            else
            {
                if (lastLoggedTargetName != "")
                {
                    Debug.Log("[CIBLE] Perdue");
                    lastLoggedTargetName = "";
                }
            }
        }

        #endregion

        #region Input Detection

        /// <summary>
        /// Detection des inputs VR et Desktop pour les interactions
        /// </summary>
        private void CheckInputDirect()
        {
            bool interactionTriggered = false;
            string inputSource = "Unknown";

            // === INPUT VR: Gachettes index uniquement ===
            if (useOVRInput)
            {
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                {
                    inputSource = "VR Gachette Droite";
                    interactionTriggered = true;
                }
                else if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
                {
                    inputSource = "VR Gachette Gauche";
                    interactionTriggered = true;
                }
            }

            // === INPUT Desktop: Clic souris ou Espace ===
            if (allowDesktopInput && !interactionTriggered)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                {
                    inputSource = "Desktop (souris/espace)";
                    interactionTriggered = true;
                }
            }

            // === EXECUTER L'INTERACTION ===
            if (interactionTriggered)
            {
                Debug.Log($"[INTERACTION] Input detecte: {inputSource}");

                // Priorite 1: Bouton UI (Quiz)
                if (currentButtonTarget != null)
                {
                    Debug.Log($"[ACTION] === CLIC sur bouton UI: {currentButtonTarget.gameObject.name} ===");

                    // Invoquer le onClick du bouton
                    currentButtonTarget.onClick.Invoke();

                    // Feedback haptique pour le clic
                    if (enableHapticFeedback && useOVRInput)
                    {
                        try
                        {
                            OVRInput.SetControllerVibration(0.2f, 0.3f, vrController);
                            Invoke(nameof(StopHaptic), 0.15f);
                        }
                        catch (System.Exception) { /* Ignore in simulator */ }
                    }
                    return;
                }

                // Priorite 2: Tableau
                if (currentTarget != null)
                {
                    Debug.Log($"[ACTION] === INTERACTION avec: {currentTarget.PaintingTitle} ===");

                    // Verifier les conditions de blocage
                    if (isInteractionBlocked)
                    {
                        Debug.LogWarning("[ACTION] BLOQUE: Quiz en cours");
                        return;
                    }

                    if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
                    {
                        Debug.LogWarning($"[ACTION] BLOQUE: GameState = {GameManager.Instance.CurrentState}");
                        return;
                    }

                    // Tout est OK, executer l'interaction
                    Debug.Log("[ACTION] >>> OnPaintingSelected() <<<");
                    currentTarget.OnPaintingSelected();
                }
            }
        }

        #endregion

        #region Visual Feedback

        private void UpdateRayVisual(Vector3 endPoint)
        {
            if (lineRenderer == null) return;

            lineRenderer.SetPosition(0, rayOrigin.position);
            lineRenderer.SetPosition(1, endPoint);

            // Couleur selon la cible: UI (bleu) > Tableau (vert) > Rien (blanc)
            Color color;
            if (currentButtonTarget != null)
            {
                color = rayColorUI; // Bleu pour les elements UI cliquables
            }
            else if (currentTarget != null)
            {
                color = rayColorTargeting; // Vert pour les tableaux
            }
            else
            {
                color = rayColorIdle; // Blanc par defaut
            }

            lineRenderer.startColor = color;
            lineRenderer.endColor = color * 0.5f;
        }

        private void CreateDefaultLineRenderer()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.015f;  // Plus epais pour meilleure visibilite
            lineRenderer.endWidth = 0.005f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;

            // Creer un material qui s'affiche par-dessus TOUT (ignore le Z-buffer)
            Shader unlitShader = Shader.Find("UI/Default");
            if (unlitShader == null)
            {
                unlitShader = Shader.Find("Sprites/Default");
            }
            Material laserMat = new Material(unlitShader);
            laserMat.renderQueue = 5000; // Rendre tout a la fin (apres overlay UI)

            // Forcer le rendu par-dessus tout
            laserMat.SetInt("_ZWrite", 0);
            laserMat.SetInt("_ZTest", 0); // Always pass

            lineRenderer.material = laserMat;

            // Configurer le sorting pour etre au-dessus
            lineRenderer.sortingLayerName = "Overlay";
            lineRenderer.sortingOrder = 32767; // Max sorting order

            lineRenderer.startColor = rayColorIdle;
            lineRenderer.endColor = rayColorIdle;

            // Initialiser les positions pour eviter le point noir a (0,0,0)
            Vector3 startPos = rayOrigin != null ? rayOrigin.position : transform.position;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, startPos + (rayOrigin != null ? rayOrigin.forward : transform.forward) * 0.1f);

            Debug.Log("[PlayerInteraction] LineRenderer cree avec ZTest=Always pour visibilite maximale sur UI");
        }

        private void StopHaptic()
        {
            if (enableHapticFeedback && useOVRInput)
            {
                try
                {
                    OVRInput.SetControllerVibration(0, 0, vrController);
                }
                catch (System.Exception)
                {
                    // Ignore haptic errors in simulator mode
                }
            }
        }

        #endregion

        #region Event Handlers

        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnQuizStarted += OnQuizStarted;
                GameManager.Instance.OnQuizCompleted += OnQuizCompleted;
                GameManager.Instance.OnQuizError += OnQuizError;
                Debug.Log("[PlayerInteraction] Abonne aux evenements GameManager");
            }
            else
            {
                Debug.LogWarning("[PlayerInteraction] GameManager.Instance est NULL - Mode test sans GameManager");
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnQuizStarted -= OnQuizStarted;
                GameManager.Instance.OnQuizCompleted -= OnQuizCompleted;
                GameManager.Instance.OnQuizError -= OnQuizError;
            }
        }

        private void OnQuizStarted(PaintingController painting)
        {
            isInteractionBlocked = true;
            Debug.Log("[EVENT] Quiz demarre - Interactions BLOQUEES");
        }

        private void OnQuizCompleted(bool success, int points)
        {
            isInteractionBlocked = false;
            Debug.Log("[EVENT] Quiz termine - Interactions DEBLOQUEES");
        }

        private void OnQuizError(string error)
        {
            isInteractionBlocked = false;
            Debug.Log("[EVENT] Erreur quiz - Interactions DEBLOQUEES");
        }

        #endregion
    }
}
