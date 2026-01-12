using UnityEngine;
using MuseumAI.Core;

namespace MuseumAI.Gameplay
{
    /// <summary>
    /// Controleur attache a chaque tableau interactif.
    /// Stocke les informations contextuelles et gere l'interaction du joueur.
    /// </summary>
    public class PaintingController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Informations du Tableau")]
        [Tooltip("Titre de l'oeuvre")]
        [SerializeField] private string paintingTitle;

        [Tooltip("Artiste de l'oeuvre")]
        [SerializeField] private string artistName;

        [Tooltip("Annee de creation")]
        [SerializeField] private int year;

        [Header("Contexte pour l'IA")]
        [Tooltip("Description detaillee pour generer le quiz. Ex: 'La Nuit etoilee de Van Gogh, peint en 1889...'")]
        [TextArea(5, 10)]
        [SerializeField] private string paintingContext;

        [Header("Feedback Visuel")]
        [Tooltip("Materiau a appliquer quand le tableau est complete")]
        [SerializeField] private Material completedMaterial;

        [Tooltip("Effet de particules a jouer lors de la completion")]
        [SerializeField] private ParticleSystem completionEffect;

        #endregion

        #region Properties

        /// <summary>
        /// Contexte textuel du tableau pour la generation de quiz
        /// </summary>
        public string PaintingContext => paintingContext;

        /// <summary>
        /// Titre de l'oeuvre
        /// </summary>
        public string PaintingTitle => paintingTitle;

        /// <summary>
        /// Nom de l'artiste
        /// </summary>
        public string ArtistName => artistName;

        /// <summary>
        /// Annee de creation
        /// </summary>
        public int Year => year;

        /// <summary>
        /// Indique si le quiz de ce tableau a ete complete
        /// </summary>
        public bool IsCompleted => isCompleted;

        #endregion

        #region Private Fields

        private bool isCompleted = false;
        private Renderer paintingRenderer;
        private Material originalMaterial;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            paintingRenderer = GetComponent<Renderer>();
            if (paintingRenderer != null)
            {
                originalMaterial = paintingRenderer.material;
            }
        }

        private void Start()
        {
            // Validation des donnees
            if (string.IsNullOrEmpty(paintingContext))
            {
                Debug.LogWarning($"[PaintingController] '{gameObject.name}' n'a pas de contexte defini!");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Appele quand le joueur selectionne ce tableau.
        /// A connecter a l'evenement 'Select Entered' du XRSimpleInteractable.
        /// </summary>
        public void OnPaintingSelected()
        {
            if (isCompleted)
            {
                Debug.Log($"[PaintingController] '{paintingTitle}' deja complete.");
                return;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("[PaintingController] GameManager.Instance est null!");
                return;
            }

            if (GameManager.Instance.CurrentState != GameState.Playing)
            {
                Debug.Log("[PaintingController] Le jeu n'est pas en cours.");
                return;
            }

            Debug.Log($"[PaintingController] Tableau selectionne: {paintingTitle}");
            GameManager.Instance.StartQuizForPainting(this);
        }

        /// <summary>
        /// Marque ce tableau comme complete apres un quiz reussi.
        /// </summary>
        public void MarkAsCompleted()
        {
            if (isCompleted) return;

            isCompleted = true;

            // Feedback visuel
            ApplyCompletedVisual();

            Debug.Log($"[PaintingController] '{paintingTitle}' marque comme complete!");
        }

        /// <summary>
        /// Reinitialise l'etat du tableau (utile pour rejouer)
        /// </summary>
        public void ResetState()
        {
            isCompleted = false;

            // Restaurer le materiau original
            if (paintingRenderer != null && originalMaterial != null)
            {
                paintingRenderer.material = originalMaterial;
            }

            Debug.Log($"[PaintingController] '{paintingTitle}' reinitialise.");
        }

        /// <summary>
        /// Genere le contexte complet pour l'API
        /// </summary>
        /// <returns>Contexte formate pour la generation de quiz</returns>
        public string GetFullContext()
        {
            if (!string.IsNullOrEmpty(paintingContext))
            {
                return paintingContext;
            }

            // Fallback: generer un contexte basique a partir des metadonnees
            return $"Oeuvre: {paintingTitle} par {artistName}, creee en {year}.";
        }

        #endregion

        #region Private Methods

        private void ApplyCompletedVisual()
        {
            // Appliquer le materiau de completion si disponible
            if (completedMaterial != null && paintingRenderer != null)
            {
                paintingRenderer.material = completedMaterial;
            }

            // Jouer l'effet de particules si disponible
            if (completionEffect != null)
            {
                completionEffect.Play();
            }
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-remplir le nom si vide
            if (string.IsNullOrEmpty(paintingTitle))
            {
                paintingTitle = gameObject.name;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Afficher une indication visuelle dans l'editeur
            Gizmos.color = isCompleted ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
#endif

        #endregion
    }
}
