using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DisallowMultipleComponent]
    public class MenuButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Scale Animation")]
        [SerializeField] private RectTransform target;
        [SerializeField] private float animationSpeed = 14f;
        [SerializeField] private float pressedScale = 0.94f;
        [SerializeField] private float hoverScale = 1.025f;

        [Header("Idle Pulse")]
        [SerializeField] private bool idlePulse;
        [SerializeField] private float pulseAmount = 0.025f;
        [SerializeField] private float pulseSpeed = 2.2f;

        [Header("Highlight")]
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private float hoverBrightness = 0.12f;

        private Vector3 baseScale = Vector3.one;
        private Color baseColor = Color.white;
        private float interactionScale = 1f;
        private float highlightAmount;
        private bool pointerInside;
        private bool pointerDown;

        private void Awake()
        {
            if (target == null)
            {
                target = transform as RectTransform;
            }

            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            if (target != null)
            {
                baseScale = target.localScale;
            }

            if (targetGraphic != null)
            {
                baseColor = targetGraphic.color;
            }
        }

        private void OnEnable()
        {
            pointerInside = false;
            pointerDown = false;
            interactionScale = 1f;
            highlightAmount = 0f;

            if (target != null)
            {
                target.localScale = baseScale;
            }

            if (targetGraphic != null)
            {
                targetGraphic.color = baseColor;
            }
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            float wantedInteractionScale = pointerDown ? pressedScale : pointerInside ? hoverScale : 1f;
            interactionScale = Mathf.Lerp(interactionScale, wantedInteractionScale, Time.unscaledDeltaTime * animationSpeed);

            float pulse = 1f;
            if (idlePulse && !pointerDown)
            {
                pulse += (Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.5f + 0.5f) * pulseAmount;
            }

            target.localScale = baseScale * interactionScale * pulse;

            if (targetGraphic != null)
            {
                float wantedHighlight = pointerInside && !pointerDown ? 1f : 0f;
                highlightAmount = Mathf.Lerp(highlightAmount, wantedHighlight, Time.unscaledDeltaTime * animationSpeed);
                targetGraphic.color = Color.Lerp(baseColor, Color.white, highlightAmount * hoverBrightness);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerDown = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            pointerDown = false;
        }
    }
}
