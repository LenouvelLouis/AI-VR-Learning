using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MuseumAI.UI
{
    /// <summary>
    /// Bouton pour ajuster la valeur d'un slider (+/-)
    /// Ce bouton ignore les interactions EventSystem (hover, pointer)
    /// et ne repond QU'AUX clics explicites via PlayerInteraction (gachette VR).
    /// </summary>
    public class SliderAdjustButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Configuration")]
        [SerializeField] private Slider targetSlider;
        [SerializeField] private float adjustmentValue = 1f;
        [SerializeField] private bool isIncrement = true;

        [Header("Anti-Spam")]
        [SerializeField] private float clickCooldown = 0.2f;

        private Button button;
        private float lastClickTime;
        private bool isSetup = false;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }

            if (GetComponent<BoxCollider>() == null)
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                RectTransform rect = GetComponent<RectTransform>();
                if (rect != null && rect.rect.width > 0)
                {
                    collider.size = new Vector3(rect.rect.width, rect.rect.height, 20f);
                }
                else
                {
                    collider.size = new Vector3(80f, 80f, 20f);
                }
            }
        }

        /// <summary>
        /// Appele quand le bouton est clique (via PlayerInteraction.onClick.Invoke())
        /// </summary>
        public void OnButtonClick()
        {
            if (Time.time - lastClickTime < clickCooldown)
            {
                return;
            }

            if (targetSlider == null)
            {
                return;
            }

            lastClickTime = Time.time;

            float delta = isIncrement ? adjustmentValue : -adjustmentValue;
            float newValue = targetSlider.value + delta;

            newValue = Mathf.Clamp(newValue, targetSlider.minValue, targetSlider.maxValue);

            targetSlider.value = newValue;

        }

        /// <summary>
        /// Configure le bouton (appele depuis MainMenuController)
        /// </summary>
        public void Setup(Slider slider, float adjustment, bool increment)
        {
            targetSlider = slider;
            adjustmentValue = adjustment;
            isIncrement = increment;
            isSetup = true;
        }

        #region Block EventSystem Interactions


        public void OnPointerEnter(PointerEventData eventData)
        {
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // BLOQUER les clics automatiques de l'EventSystem
        }

        #endregion
    }
}
