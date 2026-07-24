using System.Collections;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CharacterSkinController : MonoBehaviour
    {
        private CharacterSkinDefinition[] definitions;
        private CharacterRigController rigController;
        private CanvasGroup characterGroup;
        private Coroutine transitionRoutine;
        private int currentArtIndex = -1;
        private bool configured;

        public int CurrentArtIndex => currentArtIndex;
        public int ActiveBaseSkinCount => currentArtIndex >= 0 ? 1 : 0;
        public CharacterSkinDefinition CurrentDefinition =>
            definitions != null &&
            currentArtIndex >= 0 &&
            currentArtIndex < definitions.Length
                ? definitions[currentArtIndex]
                : null;

        public void Configure(
            Sprite[] frontSprites,
            Texture2D[] directionalWalkSheets,
            CharacterRigController rig,
            CanvasGroup group)
        {
            rigController = rig;
            characterGroup = group;

            int count = frontSprites != null ? frontSprites.Length : 0;
            definitions = new CharacterSkinDefinition[count];
            for (int i = 0; i < count; i++)
            {
                Texture2D walkSheet =
                    directionalWalkSheets != null && i < directionalWalkSheets.Length
                        ? directionalWalkSheets[i]
                        : null;
                definitions[i] = CharacterSkinDefinition.Create(
                    i,
                    frontSprites[i],
                    walkSheet);
            }

            configured = definitions.Length > 0 && rigController != null;
            if (configured)
            {
                ApplyImmediate(0);
            }
        }

        public void ApplySkin(int artIndex, bool animate)
        {
            if (!configured)
            {
                return;
            }

            int safeIndex = Mathf.Clamp(artIndex, 0, definitions.Length - 1);
            if (safeIndex == currentArtIndex)
            {
                EnsureVisibleState();
                return;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
                EnsureVisibleState();
            }

            if (!animate || !isActiveAndEnabled)
            {
                ApplyImmediate(safeIndex);
                return;
            }

            transitionRoutine = StartCoroutine(SwapSkinRoutine(safeIndex));
        }

        private IEnumerator SwapSkinRoutine(int nextIndex)
        {
            rigController.TriggerStageChange();

            float elapsed = 0f;
            const float hideDuration = 0.11f;
            float from = characterGroup != null ? characterGroup.alpha : 1f;
            while (elapsed < hideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / hideDuration);
                SetAlpha(Mathf.Lerp(from, 0f, t));
                yield return null;
            }

            SetAlpha(0f);
            ApplyDefinition(nextIndex);

            elapsed = 0f;
            const float showDuration = 0.34f;
            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / showDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                SetAlpha(eased);
                yield return null;
            }

            SetAlpha(1f);
            transitionRoutine = null;
        }

        private void ApplyImmediate(int nextIndex)
        {
            ApplyDefinition(nextIndex);
            EnsureVisibleState();
        }

        private void ApplyDefinition(int nextIndex)
        {
            CharacterSkinDefinition definition = definitions[nextIndex];
            if (definition == null || !definition.IsValid)
            {
                Debug.LogError($"Character skin {nextIndex} is missing or invalid.");
                return;
            }

            // One rig receives one texture. No stage GameObject is enabled beside it,
            // so an old body can never remain under the new body.
            rigController.ApplySkin(definition);
            currentArtIndex = nextIndex;
        }

        private void EnsureVisibleState()
        {
            SetAlpha(1f);
            if (characterGroup != null)
            {
                characterGroup.interactable = false;
                characterGroup.blocksRaycasts = false;
            }
        }

        private void SetAlpha(float alpha)
        {
            if (characterGroup != null)
            {
                characterGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        private void OnDisable()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            EnsureVisibleState();
        }
    }
}
