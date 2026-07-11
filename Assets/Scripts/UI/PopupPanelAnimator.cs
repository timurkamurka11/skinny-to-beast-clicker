using System.Collections;
using UnityEngine;

namespace SkinnyToBeast.UI
{
    [DisallowMultipleComponent]
    public class PopupPanelAnimator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform card;
        [SerializeField] private float duration = 0.2f;
        [SerializeField] private bool startHidden = true;

        private Coroutine animationRoutine;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.99f && canvasGroup.blocksRaycasts;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (startHidden)
            {
                HideImmediate();
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            StartAnimation(true);
        }

        public void Hide()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            StartAnimation(false);
        }

        public void HideImmediate()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (card != null)
            {
                card.localScale = Vector3.one * 0.86f;
            }
        }

        private void StartAnimation(bool show)
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }

            animationRoutine = StartCoroutine(Animate(show));
        }

        private IEnumerator Animate(bool show)
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            canvasGroup.blocksRaycasts = show;
            canvasGroup.interactable = show;

            float startAlpha = canvasGroup.alpha;
            float targetAlpha = show ? 1f : 0f;
            Vector3 startScale = card != null ? card.localScale : Vector3.one;
            Vector3 targetScale = Vector3.one * (show ? 1f : 0.9f);
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float eased = show ? EaseOutBack(t) : SmoothStep(t);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, SmoothStep(t));

                if (card != null)
                {
                    card.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
                }

                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            if (card != null)
            {
                card.localScale = targetScale;
            }

            canvasGroup.blocksRaycasts = show;
            canvasGroup.interactable = show;
            animationRoutine = null;
        }

        private static float SmoothStep(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private static float EaseOutBack(float value)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float shifted = value - 1f;
            return 1f + c3 * shifted * shifted * shifted + c1 * shifted * shifted;
        }
    }
}
