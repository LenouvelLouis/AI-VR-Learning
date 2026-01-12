using UnityEngine;

namespace MuseumAI.API
{
    /// <summary>
    /// Configuration pour l'API Google Gemini.
    /// IMPORTANT: Ne jamais commiter ce fichier avec une vraie cle API!
    /// Ajouter le fichier .asset genere dans .gitignore
    /// </summary>
    [CreateAssetMenu(fileName = "ApiConfig", menuName = "Museum AI/Api Config")]
    public class ApiConfig : ScriptableObject
    {
        [Header("Authentification")]
        [Tooltip("Cle API Google Gemini. Obtenez-la sur https://makersuite.google.com/app/apikey")]
        [SerializeField] private string apiKey;

        [Header("Configuration du Modele")]
        [Tooltip("Nom du modele Gemini a utiliser")]
        [SerializeField] private string modelName = "gemini-1.5-flash";

        [Tooltip("URL de base de l'API Gemini")]
        [SerializeField] private string baseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

        [Header("Parametres de Generation")]
        [Range(0f, 1f)]
        [Tooltip("Temperature: 0 = deterministe, 1 = creatif")]
        [SerializeField] private float temperature = 0.7f;

        [Tooltip("Nombre maximum de tokens dans la reponse (2048 recommande pour JSON complet)")]
        [SerializeField] private int maxOutputTokens = 2048;

        #region Properties

        public string ApiKey => apiKey;
        public string ModelName => modelName;
        public string BaseUrl => baseUrl;
        public float Temperature => temperature;
        public int MaxOutputTokens => maxOutputTokens;

        /// <summary>
        /// URL complete de l'endpoint generateContent
        /// </summary>
        public string FullEndpointUrl => $"{baseUrl}/{modelName}:generateContent?key={apiKey}";

        /// <summary>
        /// Verifie si la configuration est valide
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(modelName);

        #endregion

        #region Editor Validation

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(apiKey))
            {
            }

            if (apiKey != null && apiKey.StartsWith("AIza"))
            {
                // C'est probablement une vraie cle, rappeler la securite
            }
        }
#endif

        #endregion
    }
}
