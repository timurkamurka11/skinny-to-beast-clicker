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
        private int pendingArtIndex = -1;
        private bool configured;

        public int CurrentArtIndex => currentArtIndex;
        public int TargetArtIndex =>
            transitionRoutine != null ? pendingArtIndex : currentArtIndex;
        public bool IsTransitioning => transitionRoutine != null;
        public int DefinitionCount =>
            definitions != null ? definitions.Length : 0;
        public int ActiveBaseSkinCount =>
            activeSlots.ContainsKey(CharacterSkinSlot.Body) ? 1 : 0;
        public int ActiveSlotCount => activeSlots.Count;
        public bool IsVisualReady =>
            configured &&
            currentArtIndex >= 0 &&
            (characterGroup == null ||
             characterGroup.alpha > 0.999f) &&
            rigController != null &&
            rigController.HasVisibleSkin &&
            rigController.AnimatorReady;
        public CharacterSkinDefinition CurrentDefinition =>
            definitions != null &&
            currentArtIndex >= 0 &&
            currentArtIndex < definitions.Length
                ? definitions[currentArtIndex]
                : null;

        public void Configure(
            CharacterRigController rig,
            CanvasGroup group,
            int stageCount = 4)
        {
            rigController = rig;
            characterGroup = group;

            int count = Mathf.Max(1, stageCount);
            definitions = new CharacterSkinDefinition[count];
            for (int i = 0; i < count; i++)
            {
                definitions[i] = CharacterSkinDefinition.Create(i);
            }

            configured = rigController != null;
            activeSlots.Clear();
            currentArtIndex = -1;
            pendingArtIndex = -1;
            rigController?.ClearSkin();
            SetAlpha(0f);
        }

        public void ApplySkin(int artIndex, bool animate)
        {
            if (!configured)
            {
                return;
            }

            int safeIndex =
                Mathf.Clamp(artIndex, 0, definitions.Length - 1);
            if (safeIndex == currentArtIndex)
            {
                if (transitionRoutine != null)
                {
                    StopCoroutine(transitionRoutine);
                    transitionRoutine = null;
                }

                pendingArtIndex = -1;
                EnsureVisibleState();
                return;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
                pendingArtIndex = -1;
            }

            if (!animate ||
                !isActiveAndEnabled ||
                currentArtIndex < 0)
            {
                ApplyDefinition(safeIndex, true);
                EnsureVisibleState();
                return;
            }

            pendingArtIndex = safeIndex;
            transitionRoutine =
                StartCoroutine(SwapSkinRoutine(safeIndex));
        }

        private IEnumerator SwapSkinRoutine(int nextIndex)
        {
            // The outgoing body remains visible until the new palette and
            // proportions are installed atomically on the same mesh objects.
            // There is never an empty frame and never a second body overlay.
            SetAlpha(1f);
            rigController.TriggerStageChange();
            yield return new WaitForSecondsRealtime(0.18f);

            ApplyDefinition(nextIndex, false);
            if (currentArtIndex != nextIndex)
            {
                transitionRoutine = null;
                pendingArtIndex = -1;
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.64f);
            SetAlpha(1f);
            transitionRoutine = null;
            pendingArtIndex = -1;
        }

        private void ApplyDefinition(
            int nextIndex,
            bool resetAnimation)
        {
            CharacterSkinDefinition definition =
                definitions[nextIndex];
            if (definition == null || !definition.IsValid)
            {
                Debug.LogError(
                    $"Character skin {nextIndex} is missing or invalid.",
                    this);
                return;
            }

            Dictionary<CharacterSkinSlot, string> nextSlots = new();
            CharacterSkinSlotSelection[] selections = definition.Slots;
            if (selections != null)
            {
                for (int i = 0; i < selections.Length; i++)
                {
                    CharacterSkinSlotSelection selection =
                        selections[i];
                    if (!selection.Visible)
                    {
                        continue;
                    }

                    if (nextSlots.ContainsKey(selection.Slot))
                    {
                        Debug.LogError(
                            $"Skin '{definition.Id}' contains more than " +
                            $"one active item in slot {selection.Slot}.",
                            this);
                        return;
                    }

                    nextSlots.Add(
                        selection.Slot,
                        selection.ItemId);
                }
            }

            if (!nextSlots.ContainsKey(CharacterSkinSlot.Body))
            {
                Debug.LogError(
                    $"Skin '{definition.Id}' has no active Body slot.",
                    this);
                return;
            }

            rigController.ApplySkin(definition);
            if (!rigController.HasAppliedSkin)
            {
                Debug.LogError(
                    $"Skin '{definition.Id}' could not be installed " +
                    "on the persistent mesh rig.",
                    this);
                return;
            }

            activeSlots.Clear();
            foreach (KeyValuePair<CharacterSkinSlot, string> entry
                     in nextSlots)
            {
                activeSlots.Add(entry.Key, entry.Value);
            }

            currentArtIndex = nextIndex;
            if (resetAnimation)
            {
                rigController.SynchronizeAnimationState();
            }

            rigController.EnsureSkinVisible();
            SetAlpha(1f);
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
            foreach (KeyValuePair<CharacterSkinSlot, string> entry
                     in activeSlots)
            {
                if (string.IsNullOrWhiteSpace(entry.Value))
                {
                    error =
                        $"Slot {entry.Key} contains an empty item id.";
                    return false;
                }
            }

            if (currentArtIndex >= 0 &&
                !activeSlots.ContainsKey(CharacterSkinSlot.Body))
            {
                error =
                    "A selected skin has no active Body slot.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool EnsureVisibleSkin()
        {
            if (!configured || currentArtIndex < 0)
            {
                return false;
            }

            EnsureVisibleState();
            return IsVisualReady;
        }

        private void EnsureVisibleState()
        {
            SetAlpha(1f);
            rigController?.EnsureSkinVisible();
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

            pendingArtIndex = -1;
            if (currentArtIndex >= 0)
            {
                EnsureVisibleState();
            }
        }
    }
}
