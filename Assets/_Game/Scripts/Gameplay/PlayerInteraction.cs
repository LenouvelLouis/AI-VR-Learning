using UnityEngine;
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
        [SerializeField] private float rayDistance = 10f;
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

        [Header("Feedback Haptique")]
        [SerializeField] private bool enableHapticFeedback = true;

        #endregion

        #region Private Fields

        private PaintingController currentTarget;
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

            Debug.Log($"[PlayerInteraction] === INITIALISATION COMPLETE ===");
        }

        private void Update()
        {
            // === DIAGNOSTIC #1: Confirmer que Update() tourne ===
            frameCount++;
            if (frameCount % 300 == 1) // Log toutes les 5 secondes environ (60fps * 5)
            {
                Debug.Log($"[PlayerInteraction] Update() ACTIF - Frame #{frameCount}");
            }

            // === DIAGNOSTIC: Test Input.anyKeyDown ===
            if (Input.anyKeyDown)
            {
                Debug.Log($">>> [INPUT] anyKeyDown DETECTE! Cible: {(currentTarget != null ? currentTarget.PaintingTitle : "AUCUNE")} <<<");
            }

            // === RAYCAST ===
            PerformRaycast();

            // === INTERACTION (SANS condition isInteractionBlocked pour debug) ===
            // ANCIENNE VERSION: if (!isInteractionBlocked) CheckInput();
            // NOUVELLE VERSION: Appel direct pour diagnostic
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

            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, rayDistance, interactableLayers))
            {
                PaintingController painting = hit.collider.GetComponent<PaintingController>();
                if (painting == null)
                {
                    painting = hit.collider.GetComponentInParent<PaintingController>();
                }

                SetCurrentTarget(painting);
                UpdateRayVisual(hit.point);
            }
            else
            {
                SetCurrentTarget(null);
                UpdateRayVisual(origin + direction * rayDistance);
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
                        OVRInput.SetControllerVibration(0.1f, 0.1f, vrController);
                        Invoke(nameof(StopHaptic), 0.1f);
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
        /// Detection universelle - accepte TOUT input (clavier, souris, manette)
        /// </summary>
        private void CheckInputDirect()
        {
            bool interactionTriggered = false;
            string inputSource = "Unknown";

            // === METHODE UNIVERSELLE: Input.anyKeyDown ===
            // Detecte: toutes les touches clavier + tous les boutons souris + boutons manette
            if (Input.anyKeyDown)
            {
                inputSource = "anyKeyDown (universel)";
                interactionTriggered = true;
            }

            // === INPUT VR (en parallele) ===
            if (useOVRInput)
            {
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                {
                    inputSource = "VR Gachette Droite";
                    interactionTriggered = true;
                }
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
                {
                    inputSource = "VR Gachette Gauche";
                    interactionTriggered = true;
                }
            }

            // === EXECUTER L'INTERACTION ===
            if (interactionTriggered)
            {
                Debug.Log($"[INTERACTION] Input detecte: {inputSource}");

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
                else
                {
                    // Pas de cible - log silencieux (eviter le spam)
                    // Debug.Log("[ACTION] Pas de cible");
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

            Color color = (currentTarget != null) ? rayColorTargeting : rayColorIdle;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color * 0.5f;
        }

        private void CreateDefaultLineRenderer()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.005f;
            lineRenderer.endWidth = 0.002f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = rayColorIdle;
            lineRenderer.endColor = rayColorIdle;
        }

        private void StopHaptic()
        {
            OVRInput.SetControllerVibration(0, 0, vrController);
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
