using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    internal sealed class DumbbellTapAnimator : MonoBehaviour
    {
        private RectTransform dumbbellRoot;
        private RectTransform visualRect;
        private Image visualImage;
        private RectTransform innerRingRect;
        private Image innerRingImage;
        private RectTransform outerRingRect;
        private Image outerRingImage;
        private RectTransform shadowRect;

        private Vector2 basePosition;
        private Vector2 baseShadowSize;
        private float tapAge = 1f;
        private float tapImpulse;
        private int tapDirection = 1;
        private bool configured;

        public void Configure(
            RectTransform root,
            Image visual,
            Image innerRing,
            Image outerRing,
            RectTransform shadow)
        {
            dumbbellRoot = root;
            visualImage = visual;
            visualRect = visual.rectTransform;
            innerRingImage = innerRing;
            innerRingRect = innerRing.rectTransform;
            outerRingImage = outerRing;
            outerRingRect = outerRing.rectTransform;
            shadowRect = shadow;

            basePosition = root.anchoredPosition;
            baseShadowSize = shadow != null ? shadow.sizeDelta : Vector2.zero;
            configured = true;
        }

        public void TriggerTap()
        {
            if (!configured)
            {
                return;
            }

            tapAge = 0f;
            tapImpulse = Mathf.Min(1.75f, tapImpulse + 0.76f);
            tapDirection *= -1;
        }

        public void SetSprite(Sprite sprite)
        {
            if (visualImage != null && sprite != null)
            {
                visualImage.sprite = sprite;
            }
        }

        private void Update()
        {
            if (!configured || dumbbellRoot == null)
            {
                return;
            }

            float delta = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;
            tapAge += delta;
            tapImpulse = Mathf.MoveTowards(tapImpulse, 0f, delta * 5.4f);

            float idle = (Mathf.Sin(now * 3.35f) + 1f) * 0.5f;
            float tapT = Mathf.Clamp01(tapAge / 0.31f);
            float tapWave = tapAge < 0.31f
                ? Mathf.Sin(tapT * Mathf.PI) * tapImpulse
                : 0f;
            float shake = tapAge < 0.31f
                ? Mathf.Sin(tapT * Mathf.PI * 4.5f) * tapImpulse
                : 0f;

            dumbbellRoot.anchoredPosition = basePosition +
                                             Vector2.up * (idle * 2.5f + tapWave * 25f);

            if (visualRect != null)
            {
                visualRect.localScale = new Vector3(
                    1f + tapWave * 0.055f,
                    1f - tapWave * 0.085f,
                    1f);
                visualRect.localRotation =
                    Quaternion.Euler(0f, 0f, shake * tapDirection * 2.5f);
            }

            if (innerRingRect != null)
            {
                float innerScale = 0.94f + idle * 0.11f + tapWave * 0.18f;
                innerRingRect.localScale = Vector3.one * innerScale;
                innerRingImage.color = new Color(
                    1f,
                    0.49f,
                    0.04f,
                    0.22f + (1f - idle) * 0.18f + tapWave * 0.24f);
            }

            if (outerRingRect != null)
            {
                float wave = (Mathf.Sin(now * 2.7f + 1.4f) + 1f) * 0.5f;
                float outerScale = 0.96f + wave * 0.13f + tapWave * 0.09f;
                outerRingRect.localScale = Vector3.one * outerScale;
                outerRingImage.color = new Color(
                    0.04f,
                    0.55f,
                    1f,
                    0.09f + (1f - wave) * 0.17f + tapWave * 0.13f);
            }

            if (shadowRect != null)
            {
                float shadowScale = 1f - tapWave * 0.14f;
                shadowRect.sizeDelta = new Vector2(
                    baseShadowSize.x * shadowScale,
                    baseShadowSize.y * shadowScale);
            }
        }
    }
}
