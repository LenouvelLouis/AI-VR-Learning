using UnityEngine;

namespace MuseumAI.API
{
    // Config pour l'API Gemini - ne pas commit le fichier .asset avec ta cle!
    [CreateAssetMenu(fileName = "ApiConfig", menuName = "Museum AI/Api Config")]
    public class ApiConfig : ScriptableObject
    {
        [Header("Authentification")]
        [SerializeField] private string apiKey;

        [Header("Configuration du Modele")]
        [SerializeField] private string modelName = "gemini-1.5-flash";
        [SerializeField] private string baseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

        [Header("Parametres de Generation")]
        [Range(0f, 1f)]
        [SerializeField] private float temperature = 0.7f;
        [SerializeField] private int maxOutputTokens = 2048;

        public string ApiKey => apiKey;
        public string ModelName => modelName;
        public string BaseUrl => baseUrl;
        public float Temperature => temperature;
        public int MaxOutputTokens => maxOutputTokens;

        public string FullEndpointUrl => $"{baseUrl}/{modelName}:generateContent?key={apiKey}";
        public bool IsValid => !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(modelName);

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Rien pour l'instant
        }
#endif
    }
}
