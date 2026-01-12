using UnityEngine;

public class TestGachetteDroite : MonoBehaviour
{
    void Update()
    {
        // OVRInput.Controller.RTouch = Spécifique à la main DROITE (Right Touch)
        // OVRInput.Button.PrimaryIndexTrigger = La gâchette de l'index

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            Debug.Log("PAN ! Gâchette DROITE appuyée !");
        }

        // Si vous voulez détecter quand on la relâche
        if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            Debug.Log("Gâchette DROITE relâchée.");
        }
    }
}
