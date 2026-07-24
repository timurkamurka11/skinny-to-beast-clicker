using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CharacterSkinController : MonoBehaviour
    {
        private readonly Dictionary<CharacterSkinSlot, string> activeSlots = new();

        private CharacterSkinDefinition[] definitions;
        private CharacterRigController rigController;
        private CanvasGroup characterGroup;
        private Coroutine transitionRoutine;
        private int currentArtIndex = -1;
        private bool configured;

        public int CurrentArtIndex => currentArtIndex;
        public int DefinitionCount => definitions != null ? definitions.Length : 0;
        public int ActiveBaseSkinCount =>
            activeSlots.ContainsKey(CharacterSkinSlot.Body) ? 1 : 0;
        public int ActiveSlotCount => activeSlots.Count;
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
                // Keep the actor hidden until GameplayVisualStageController
                // resolves the saved body stage. Showing stage 1 here first
                // caused the old body to flash for one rendered frame.
                activeSlots.Clear();
                currentArtIndex = -1;
                rigController.ClearSkin();
                SetAlpha(0f);
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
            if (currentArtIndex == nextIndex)
            {
                // SynchronizeAnimationState deliberately clears every previous
                // action. Start the transformation only after that reset so the
                // newly installed skin, rather than the outgoing one, receives it.
                rigController.TriggerStageChange();
            }

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

            // CharacterSkinController is the only writer for visual slots.
            // Clear every old renderer and logical selection before publishing
            // the new stage as one atomic visible state.
            SetAlpha(0f);
            rigController.ClearSkin();
            activeSlots.Clear();

            CharacterSkinSlotSelection[] nextSlots = definition.Slots;
            if (nextSlots != null)
            {
                for (int i = 0; i < nextSlots.Length; i++)
                {
                    CharacterSkinSlotSelection selection = nextSlots[i];
                    if (!selection.Visible)
                    {
                        continue;
                    }

                    if (activeSlots.ContainsKey(selection.Slot))
                    {
                        Debug.LogError(
                            $"Skin '{definition.Id}' contains more than one " +
                            $"active item in slot {selection.Slot}.",
                            this);
                        activeSlots.Clear();
                        currentArtIndex = -1;
                        return;
                    }

                    activeSlots.Add(selection.Slot, selection.ItemId);
                }
            }

            if (!activeSlots.ContainsKey(CharacterSkinSlot.Body))
            {
                Debug.LogError(
                    $"Skin '{definition.Id}' has no active Body slot.",
                    this);
                activeSlots.Clear();
                currentArtIndex = -1;
                return;
            }

            rigController.ApplySkin(definition);
            rigController.SynchronizeAnimationState();
            currentArtIndex = nextIndex;
        }

        public bool TryGetActiveItem(
            CharacterSkinSlot slot,
            out string itemId)
        {
            return activeSlots.TryGetValue(slot, out itemId);
        }

        public int GetActiveCount(CharacterSkinSlot slot)
        {
            return activeSlots.ContainsKey(slot) ? 1 : 0;
        }

        public bool ValidateSlotExclusivity(out string error)
        {
            foreach (KeyValuePair<CharacterSkinSlot, string> entry in activeSlots)
            {
                if (string.IsNullOrWhiteSpace(entry.Value))
                {
                    error = $"Slot {entry.Key} contains an empty item id.";
                    return false;
                }
            }

            if (currentArtIndex >= 0 &&
                !activeSlots.ContainsKey(CharacterSkinSlot.Body))
            {
                error = "A selected skin has no active Body slot.";
                return false;
            }

            error = string.Empty;
            return true;
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
