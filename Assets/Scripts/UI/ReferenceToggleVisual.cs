using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DisallowMultipleComponent]
    internal sealed class ReferenceToggleVisual : MonoBehaviour
    {
        private const float HiddenAlpha = 0.001f;
        private Toggle toggle;
        private Image background;
        private RectTransform knobRect;
        private TMP_Text valueText;
        private Color enabledColor;
        private Color disabledColor;
        private bool moveKnob;
        private float knobOffset;

        public void Configure(
            Toggle targetToggle,
            Image targetBackground,
            Image targetKnob,
            TMP_Text targetValueText,
            Color onColor,
            Color offColor,
            bool shouldMoveKnob,
            float movement)
        {
            toggle = targetToggle;
            background = targetBackground;
            knobRect = targetKnob != null ? targetKnob.rectTransform : null;
            valueText = targetValueText;
            enabledColor = onColor;
            disabledColor = offColor;
            moveKnob = shouldMoveKnob;
            knobOffset = movement;

            toggle.onValueChanged.AddListener(_ => Refresh());
            Refresh();
        }

        private void Refresh()
        {
            if (toggle == null)
            {
                return;
            }

            bool enabled = toggle.isOn;

            if (background != null)
            {
                Color hiddenBackground = enabled ? enabledColor : disabledColor;
                hiddenBackground.a = HiddenAlpha;
                background.color = hiddenBackground;
                background.canvasRenderer.SetAlpha(HiddenAlpha);
            }

            if (valueText != null)
            {
                valueText.text = enabled ? "ON" : "OFF";
                Color hiddenText = valueText.color;
                hiddenText.a = 0f;
                valueText.color = hiddenText;
            }

            if (knobRect != null)
            {
                if (moveKnob)
                {
                    Vector2 position = knobRect.anchoredPosition;
                    position.x = enabled ? knobOffset : -knobOffset;
                    knobRect.anchoredPosition = position;
                }

                Image knobImage = knobRect.GetComponent<Image>();
                if (knobImage != null)
                {
                    Color hiddenKnob = knobImage.color;
                    hiddenKnob.a = HiddenAlpha;
                    knobImage.color = hiddenKnob;
                    knobImage.canvasRenderer.SetAlpha(HiddenAlpha);
                }
            }
        }
    }
}
