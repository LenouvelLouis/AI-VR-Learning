using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro; // Nécessaire pour l'affichage du texte

public class GeminiVisionTester : MonoBehaviour
{
    [Header("Configuration API")]
    [SerializeField] private string apiKey = "AIzaSyD9K8r-ISeVW5FfDPYPmxhYWAyE1nqiHwc"; // Remplissez votre clé ici
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-09-2025:generateContent";

    [Header("Interface")]
    public TextMeshProUGUI texteReponse; // Glissez votre objet TextMeshPro ici

    // Cette fonction est désormais publique pour être appelée par l'autre script
    public void LancerAnalyse(Texture2D textureRecue)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("Clé API manquante !");
            return;
        }

        if (texteReponse != null) texteReponse.text = "Analyse de l'image reçue...";
        
        // On lance la coroutine en lui passant la texture reçue
        StartCoroutine(ProcessImageRoutine(textureRecue));
    }

    IEnumerator ProcessImageRoutine(Texture2D texture)
    {
        // 1. ENCODAGE (Plus besoin de ReadPixels, on a déjà la texture)
        byte[] imageBytes = texture.EncodeToJPG(50);
        string base64Image = System.Convert.ToBase64String(imageBytes);

        // 2. PROMPT
        string prompt = "Identifie l'objet précis dans cette image. Donne le nom de la TOUTE PREMIÈRE invention de ce type, l'inventeur et l'année. Format: 'Objet: [Nom] | Inventeur: [Nom], [Année]'. En français.";

        // 3. CREATION DU JSON
        var payload = new
        {
            contents = new[]
            {
                new {
                    parts = new object[] {
                        new { text = prompt },
                        new { inline_data = new { mime_type = "image/jpeg", data = base64Image } }
                    }
                }
            }
        };

        string jsonPayload = JsonConvert.SerializeObject(payload);

        // 4. ENVOI A L'API
        string url = $"{apiEndpoint}?key={apiKey}";
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                if (texteReponse != null) texteReponse.text = "Erreur API: " + request.responseCode;
                Debug.LogError("Erreur Gemini: " + request.downloadHandler.text);
            }
            else
            {
                TraiterReponse(request.downloadHandler.text);
            }
        }
    }

    void TraiterReponse(string json)
    {
        try
        {
            JObject retour = JObject.Parse(json);
            if (retour["candidates"] == null) return;
            string texte = retour["candidates"][0]["content"]["parts"][0]["text"].ToString();
            
            if (texteReponse != null) texteReponse.text = texte;
            Debug.Log("Réponse IA : " + texte);
        }
        catch (System.Exception e)
        {
            if (texteReponse != null) texteReponse.text = "Erreur lecture réponse.";
        }
    }
}