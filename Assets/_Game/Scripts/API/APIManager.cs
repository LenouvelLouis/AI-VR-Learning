using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace MuseumAI.API
{
    /// <summary>
    /// Gestionnaire des appels a l'API Google Gemini.
    /// Genere des quiz bases sur le contexte des tableaux.
    /// Pattern Singleton pour un acces global.
    /// </summary>
    public class APIManager : MonoBehaviour
    {
        #region Singleton

        public static APIManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            ValidateConfiguration();
        }

        #endregion

        #region Configuration

        [Header("Configuration API")]
        [SerializeField] private ApiConfig apiConfig;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool logFullResponses = false;

        [Header("Timeout et Retry")]
        [SerializeField] private int requestTimeoutSeconds = 30;
        [SerializeField] private int maxRetryAttempts = 2;
        [SerializeField] private float retryDelaySeconds = 2f;

        #endregion

        #region Private Fields

        private bool isRequestInProgress = false;
        private const string QUIZ_PROMPT_TEMPLATE = @"Tu es un expert en histoire et monuments. Tu dois générer un quiz sur le monument suivant :

Nom du monument : {0}
Informations : {1}

INSTRUCTIONS TRÈS STRICTES :
1.  **Format des réponses** : Chaque réponse (vraie ou fausse) doit faire **UN ou DEUX mots MAXIMUM**.
2.  **Format de la question** : La question DOIT INCLURE le nom du monument ""{0}"". Par exemple : ""En quelle année {0} a-t-il été construit ?"" ou ""Dans quel pays se trouve {0} ?"".
3.  **Contenu du quiz** :
    *   Crée UNE SEULE question qui mentionne explicitement ""{0}"".
    *   Fournis EXACTEMENT 1 réponse vraie et 3 réponses fausses.
    *   Les réponses fausses doivent être plausibles mais incorrectes.
4.  **Fait historique** : Ajoute un fait historique interessant et peu connu sur ""{0}"" (1-2 phrases maximum).
5.  **Format de sortie** : Réponds OBLIGATOIREMENT avec le JSON suivant, sans aucun texte avant ou après, et sans utiliser de markdown :
{{""question"":""[Ta question incluant {0}]"",""trueAnswer"":""[La bonne réponse]"",""falseAnswers"":[""[Fausse 1]"",""[Fausse 2]"",""[Fausse 3]""],""historicalFact"":""[Fait historique interessant]""}}";

        #endregion

        #region Properties

        /// <summary>
        /// Indique si une requete est en cours
        /// </summary>
        public bool IsRequestInProgress => isRequestInProgress;

        /// <summary>
        /// Indique si la configuration est valide
        /// </summary>
        public bool IsConfigured => apiConfig != null && apiConfig.IsValid;

        #endregion

        #region Events

        /// <summary>
        /// Declenche quand une requete commence
        /// </summary>
        public event Action OnRequestStarted;

        /// <summary>
        /// Declenche quand une requete se termine (succes ou echec)
        /// </summary>
        public event Action OnRequestCompleted;

        #endregion

        #region Public Methods

        /// <summary>
        /// Genere un quiz a partir du contexte d'un tableau
        /// </summary>
        /// <param name="monumentName">Nom du monument</param>
        /// <param name="paintingContext">Description/contexte du tableau</param>
        /// <param name="onSuccess">Callback appele en cas de succes avec les donnees du quiz</param>
        /// <param name="onError">Callback appele en cas d'erreur avec le message</param>
        public void GenerateQuiz(string monumentName, string paintingContext, Action<QuizData> onSuccess, Action<string> onError)
        {
            if (!ValidateRequest(paintingContext, onError))
            {
                return;
            }

            StartCoroutine(GenerateQuizCoroutine(monumentName, paintingContext, onSuccess, onError));
        }

        /// <summary>
        /// Version async/await pour les contextes qui le supportent
        /// </summary>
        public async System.Threading.Tasks.Task<QuizData> GenerateQuizAsync(string monumentName, string paintingContext)
        {
            if (!IsConfigured)
            {
                Debug.LogError("[APIManager] Configuration manquante!");
                return null;
            }

            if (string.IsNullOrEmpty(paintingContext))
            {
                Debug.LogError("[APIManager] Contexte vide!");
                return null;
            }

            // Utiliser TaskCompletionSource pour convertir le callback en Task
            var tcs = new System.Threading.Tasks.TaskCompletionSource<QuizData>();

            GenerateQuiz(
                monumentName,
                paintingContext,
                onSuccess: (quiz) => tcs.SetResult(quiz),
                onError: (error) =>
                {
                    Debug.LogError($"[APIManager] Erreur: {error}");
                    tcs.SetResult(null);
                }
            );

            return await tcs.Task;
        }

        /// <summary>
        /// Annule la requete en cours (si possible)
        /// </summary>
        public void CancelCurrentRequest()
        {
            if (isRequestInProgress)
            {
                StopAllCoroutines();
                isRequestInProgress = false;
                OnRequestCompleted?.Invoke();
                Log("[APIManager] Requete annulee");
            }
        }

        /// <summary>
        /// Teste la connexion a l'API avec un prompt simple
        /// </summary>
        public void TestConnection(Action<bool, string> callback)
        {
            if (!IsConfigured)
            {
                callback?.Invoke(false, "Configuration API manquante");
                return;
            }

            StartCoroutine(TestConnectionCoroutine(callback));
        }

        #endregion

        #region Private Methods - Core

        private IEnumerator GenerateQuizCoroutine(string monumentName, string paintingContext, Action<QuizData> onSuccess, Action<string> onError)
        {
            isRequestInProgress = true;
            OnRequestStarted?.Invoke();

            // Construire le prompt avec le nom du monument et le contexte
            string prompt = string.Format(QUIZ_PROMPT_TEMPLATE, monumentName, paintingContext);
            Log($"[APIManager] Envoi du prompt pour '{monumentName}' ({prompt.Length} caracteres)");

            // Construire la requete AVEC les parametres de configuration
            GeminiRequest geminiRequest = new GeminiRequest(
                prompt,
                apiConfig.Temperature,
                apiConfig.MaxOutputTokens
            );
            string jsonBody = JsonUtility.ToJson(geminiRequest);

            Log($"[APIManager] Config: temperature={apiConfig.Temperature}, maxTokens={apiConfig.MaxOutputTokens}");

            if (logFullResponses)
            {
                Debug.Log($"[APIManager] Request JSON:\n{jsonBody}");
            }

            // Systeme de retry pour les erreurs temporaires (503, 429, etc.)
            int attempts = 0;
            bool success = false;
            string lastError = "";

            while (attempts <= maxRetryAttempts && !success)
            {
                attempts++;

                if (attempts > 1)
                {
                    Log($"[APIManager] Tentative {attempts}/{maxRetryAttempts + 1} apres erreur...");
                    yield return new WaitForSeconds(retryDelaySeconds);
                }

                // Envoyer la requete
                using (UnityWebRequest request = CreateWebRequest(jsonBody))
                {
                    request.timeout = requestTimeoutSeconds;

                    yield return request.SendWebRequest();

                    // Traiter la reponse
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        success = true;
                        isRequestInProgress = false;
                        OnRequestCompleted?.Invoke();
                        ProcessSuccessResponse(request.downloadHandler.text, onSuccess, onError);
                    }
                    else
                    {
                        // Verifier si c'est une erreur qui merite un retry
                        long responseCode = request.responseCode;
                        bool shouldRetry = (responseCode == 503 || responseCode == 429 || responseCode == 500 || responseCode == 502);

                        if (shouldRetry && attempts <= maxRetryAttempts)
                        {
                            lastError = $"Erreur {responseCode} - nouvelle tentative...";
                            Log($"[APIManager] {lastError}");
                        }
                        else
                        {
                            // Erreur finale ou pas de retry possible
                            isRequestInProgress = false;
                            OnRequestCompleted?.Invoke();
                            ProcessErrorResponse(request, onError);
                            yield break;
                        }
                    }
                }
            }
        }

        private UnityWebRequest CreateWebRequest(string jsonBody)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            UnityWebRequest request = new UnityWebRequest(apiConfig.FullEndpointUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            return request;
        }

        private void ProcessSuccessResponse(string responseText, Action<QuizData> onSuccess, Action<string> onError)
        {
            if (logFullResponses)
            {
                Debug.Log($"[APIManager] Response JSON:\n{responseText}");
            }

            try
            {
                // Parser la reponse Gemini
                GeminiResponse geminiResponse = JsonUtility.FromJson<GeminiResponse>(responseText);

                if (geminiResponse == null)
                {
                    onError?.Invoke("Impossible de parser la reponse Gemini");
                    return;
                }

                if (geminiResponse.HasError())
                {
                    onError?.Invoke($"Erreur Gemini: {geminiResponse.error.message}");
                    return;
                }

                // Extraire le texte de la reponse
                string quizJson = geminiResponse.GetResponseText();

                if (string.IsNullOrEmpty(quizJson))
                {
                    onError?.Invoke("Reponse vide de l'API");
                    return;
                }

                // Nettoyer et parser le JSON du quiz
                QuizData quizData = ParseQuizJson(quizJson, onError);

                if (quizData != null)
                {
                    Log($"[APIManager] Quiz genere avec succes: {quizData.question}");
                    onSuccess?.Invoke(quizData);
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Exception lors du parsing: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        private void ProcessErrorResponse(UnityWebRequest request, Action<string> onError)
        {
            string errorMessage;

            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                    errorMessage = "Erreur de connexion. Verifiez votre internet.";
                    break;

                case UnityWebRequest.Result.ProtocolError:
                    errorMessage = ParseHttpError(request);
                    break;

                case UnityWebRequest.Result.DataProcessingError:
                    errorMessage = "Erreur de traitement des donnees.";
                    break;

                default:
                    errorMessage = $"Erreur inconnue: {request.error}";
                    break;
            }

            Debug.LogError($"[APIManager] {errorMessage}");
            onError?.Invoke(errorMessage);
        }

        private string ParseHttpError(UnityWebRequest request)
        {
            long responseCode = request.responseCode;
            string responseBody = request.downloadHandler?.text ?? "";

            switch (responseCode)
            {
                case 400:
                    return "Requete invalide (400). Verifiez le format.";

                case 401:
                case 403:
                    return "Cle API invalide ou non autorisee (401/403).";

                case 404:
                    return "Endpoint non trouve (404). Verifiez le nom du modele.";

                case 429:
                    return "Trop de requetes (429). Attendez un moment.";

                case 500:
                case 502:
                case 503:
                    return $"Erreur serveur Google ({responseCode}). Reessayez plus tard.";

                default:
                    // Essayer d'extraire le message d'erreur du corps
                    if (!string.IsNullOrEmpty(responseBody))
                    {
                        try
                        {
                            GeminiResponse errorResponse = JsonUtility.FromJson<GeminiResponse>(responseBody);
                            if (errorResponse?.error != null)
                            {
                                return $"Erreur {responseCode}: {errorResponse.error.message}";
                            }
                        }
                        catch { }
                    }
                    return $"Erreur HTTP {responseCode}: {request.error}";
            }
        }

        #endregion

        #region Private Methods - JSON Parsing

        private QuizData ParseQuizJson(string rawJson, Action<string> onError)
        {
            // Nettoyer le JSON
            string cleanedJson = CleanJsonResponse(rawJson);

            if (string.IsNullOrEmpty(cleanedJson))
            {
                onError?.Invoke("JSON vide apres nettoyage");
                return null;
            }

            Log($"[APIManager] JSON nettoye: {cleanedJson.Substring(0, Math.Min(200, cleanedJson.Length))}...");

            try
            {
                QuizData quizData = JsonUtility.FromJson<QuizData>(cleanedJson);

                if (quizData == null)
                {
                    onError?.Invoke("Echec du parsing JSON");
                    return null;
                }

                if (!quizData.IsValid())
                {
                    onError?.Invoke("Donnees du quiz incompletes");
                    return null;
                }

                return quizData;
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Erreur parsing quiz: {ex.Message}");
                Debug.LogError($"[APIManager] JSON problematique:\n{cleanedJson}");
                return null;
            }
        }

        private string CleanJsonResponse(string rawResponse)
        {
            if (string.IsNullOrEmpty(rawResponse))
                return null;

            string cleaned = rawResponse.Trim();

            // Supprimer les blocs markdown ```json ... ```
            cleaned = Regex.Replace(cleaned, @"^```json\s*", "", RegexOptions.Multiline);
            cleaned = Regex.Replace(cleaned, @"^```\s*$", "", RegexOptions.Multiline);
            cleaned = Regex.Replace(cleaned, @"```$", "");

            // Supprimer les espaces/newlines au debut et a la fin
            cleaned = cleaned.Trim();

            // Trouver le debut et la fin du JSON object
            int startIndex = cleaned.IndexOf('{');
            int endIndex = cleaned.LastIndexOf('}');

            if (startIndex >= 0 && endIndex > startIndex)
            {
                cleaned = cleaned.Substring(startIndex, endIndex - startIndex + 1);
            }

            // Remplacer les caracteres problematiques
            cleaned = cleaned.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

            // Supprimer les espaces multiples
            cleaned = Regex.Replace(cleaned, @"\s+", " ");

            return cleaned;
        }

        #endregion

        #region Private Methods - Validation & Testing

        private bool ValidateRequest(string paintingContext, Action<string> onError)
        {
            if (!IsConfigured)
            {
                string error = "APIManager non configure. Assignez un ApiConfig valide.";
                Debug.LogError($"[APIManager] {error}");
                onError?.Invoke(error);
                return false;
            }

            if (string.IsNullOrEmpty(paintingContext))
            {
                string error = "Le contexte du tableau est vide.";
                Debug.LogError($"[APIManager] {error}");
                onError?.Invoke(error);
                return false;
            }

            if (isRequestInProgress)
            {
                string error = "Une requete est deja en cours.";
                Debug.LogWarning($"[APIManager] {error}");
                onError?.Invoke(error);
                return false;
            }

            return true;
        }

        private void ValidateConfiguration()
        {
            if (apiConfig == null)
            {
                Debug.LogWarning("[APIManager] ApiConfig non assigne! Creez un asset via Create > Museum AI > Api Config");
            }
            else if (!apiConfig.IsValid)
            {
                Debug.LogWarning("[APIManager] ApiConfig invalide. Verifiez la cle API.");
            }
            else
            {
                Log("[APIManager] Configuration validee.");
            }
        }

        private IEnumerator TestConnectionCoroutine(Action<bool, string> callback)
        {
            string testPrompt = "Reponds uniquement: OK";

            GeminiRequest request = new GeminiRequest(testPrompt, apiConfig.Temperature, 50); // 50 tokens suffisent pour "OK"
            string jsonBody = JsonUtility.ToJson(request);

            using (UnityWebRequest webRequest = CreateWebRequest(jsonBody))
            {
                webRequest.timeout = 10;

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    callback?.Invoke(true, "Connexion reussie!");
                }
                else
                {
                    callback?.Invoke(false, $"Echec: {webRequest.error}");
                }
            }
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion
    }
}
