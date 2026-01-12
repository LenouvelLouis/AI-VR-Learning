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
                // Ecouter le onClick qui sera invoque par PlayerInteraction
                button.onClick.AddListener(OnButtonClick);
            }

            // Ajouter BoxCollider pour VR si absent
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
            // Verifier le cooldown pour eviter les clics multiples
            if (Time.time - lastClickTime < clickCooldown)
            {
                Debug.Log($"[SliderAdjust] Clic ignore (cooldown)");
                return;
            }

            if (targetSlider == null)
            {
                Debug.LogWarning("[SliderAdjust] targetSlider est null!");
                return;
            }

            lastClickTime = Time.time;

            float delta = isIncrement ? adjustmentValue : -adjustmentValue;
            float newValue = targetSlider.value + delta;

            // Clamp entre min et max
            newValue = Mathf.Clamp(newValue, targetSlider.minValue, targetSlider.maxValue);

            targetSlider.value = newValue;

            Debug.Log($"[SliderAdjust] {targetSlider.name}: {newValue} ({(isIncrement ? "+" : "-")}{adjustmentValue})");
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
            Debug.Log($"[SliderAdjust] Setup: slider={slider?.name}, step={adjustment}, increment={increment}");
        }

        #region Block EventSystem Interactions

        // Ces methodes bloquent les interactions automatiques de l'EventSystem
        // Seul le PlayerInteraction peut declencher un clic via button.onClick.Invoke()

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Ne rien faire - bloquer l'interaction hover de l'EventSystem
            // Debug.Log($"[SliderAdjust] OnPointerEnter BLOQUE");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Ne rien faire
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // BLOQUER les clics automatiques de l'EventSystem
            // Seul PlayerInteraction peut activer ce bouton via onClick.Invoke()
            Debug.Log($"[SliderAdjust] OnPointerClick EventSystem BLOQUE - utilisez la gachette VR");
        }

        #endregion
    }
}
