using UnityEngine;
using PassthroughCameraSamples;
using System;

public class PassthroughCameraDisplay : MonoBehaviour
{
    public WebCamTextureManager webcamManager;
    public Renderer quadRenderer;
    public String TextureName;

    // REFERENCE VERS L'AUTRE SCRIPT
    [Header("Liaison IA")]
    public GeminiVisionTester scriptAnalyseIA; 

    private Texture2D picture2D;

    void Start()
    {
    }

    void Update()
    {
        // Si la caméra est prête [cite: 3]
        if (webcamManager.WebCamTexture != null)
        {
            // Détection du bouton (Manette ou Clavier pour tester)
            if (OVRInput.GetDown(OVRInput.Button.One) || Input.GetKeyDown(KeyCode.Space))
            {
                TakePicture();
            }
        }
    }

    public void TakePicture()
    {
        int width = webcamManager.WebCamTexture.width;
        int height = webcamManager.WebCamTexture.height;

        if(picture2D == null)
        {
            picture2D = new Texture2D(width, height);
        }

        // Récupération des pixels de la webcam [cite: 6]
        Color32[] pixels = new Color32[width * height];
        webcamManager.WebCamTexture.GetPixels32(pixels);
        picture2D.SetPixels32(pixels);
        picture2D.Apply();

        // Affichage local sur le quad [cite: 7]
        quadRenderer.material.SetTexture(TextureName, picture2D);

        // --- ENVOI VERS L'IA ---
        // On vérifie si le script est bien relié dans l'Inspector
        if (scriptAnalyseIA != null)
        {
            // On appelle la fonction publique créée à l'étape 1
            scriptAnalyseIA.LancerAnalyse(picture2D);
        }
        else
        {
            Debug.LogWarning("Attention : Le script GeminiVisionTester n'est pas assigné dans l'Inspector !");
        }
    }
}