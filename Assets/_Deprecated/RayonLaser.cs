using UnityEngine;

public class RayonLaser : MonoBehaviour
{
    // On crée une case pour glisser la main droite (le point de départ du laser)
    public Transform pointDeDepart;

    void Update()
    {
        // 1. On détecte l'appui sur la Gâchette Droite (Index)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            TirerLeRayon();
        }
        
        // (Optionnel) Pour tester au clavier dans le simulateur : Espace + Clic Gauche
        if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.Space))
        {
            TirerLeRayon();
        }
    }

    void TirerLeRayon()
    {
        RaycastHit impact; // Cette variable stockera les infos de ce qu'on touche

        // On lance un rayon depuis la main, vers l'avant (forward), sur une distance de 10 mètres
        if (Physics.Raycast(pointDeDepart.position, pointDeDepart.forward, out impact, 10))
        {
            // Si l'objet touché a le tag "Tableau"
            if (impact.transform.CompareTag("Tableau"))
            {
                Debug.Log("TOUCHÉ ! C'est une œuvre d'art magnifique.");
                // C'est ici qu'on affichera le texte 3D plus tard
            }
            else
            {
                Debug.Log("Raté, vous avez touché : " + impact.transform.name);
            }
        }
    }
}