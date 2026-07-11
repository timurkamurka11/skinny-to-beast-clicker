using System.Collections;
using UnityEngine;

namespace SkinnyToBeast.UI
{
    [DisallowMultipleComponent]
    public class MainMenuIntroAnimator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform content;
        [SerializeField] private Vector2 startOffset = new Vector2(0f, -70f);
        [SerializeField] private float duration = 0.55f;

        private Vector2 restingPosition;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (content == null)
            {
                content = transform as RectTransform;
            }

            if (content != null)
            {
                restingPosition = content.anchoredPosition;
            }
        }

        private void OnEnable()
        {
            StartCoroutine(PlayIntro());
        }

        private IEnumerator PlayIntro()
        {
            if (canvasGroup == null || content == null)
            {
                yield break;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            content.anchoredPosition = restingPosition + startOffset;

            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                canvasGroup.alpha = t;
                content.anchoredPosition = Vector2.LerpUnclamped(restingPosition + startOffset, restingPosition, eased);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            content.anchoredPosition = restingPosition;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}
