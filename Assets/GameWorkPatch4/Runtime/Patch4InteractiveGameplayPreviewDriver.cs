#if UNITY_EDITOR
using SkinnyToBeast.Gameplay;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Transient Editor-only visual override for inspecting Patch 4 through
    /// the real gameplay room and its real action signals. It never opens the
    /// production-art gate and restores the rollback visual when it ends.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Patch4InteractiveGameplayPreviewDriver : MonoBehaviour
    {
        private Patch4CharacterRigController rigController;
        private Patch4CharacterStateMachine stateMachine;
        private Patch4CharacterVisibilityGuard visibilityGuard;
        private Patch4V23FullFramePresentation fullFramePresentation;
        private Animator animator;
        private GameObject patch4VisualRoot;
        private GameObject patch35RollbackRoot;
        private CanvasGroup rollbackGroup;

        private bool previewActive;
        private bool rollbackGroupAdded;
        private bool visibilityGuardWasEnabled;
        private bool rollbackRootWasActive;
        private AnimatorUpdateMode animatorUpdateMode;
        private AnimatorCullingMode animatorCullingMode;
        private float animatorSpeed;
        private float rollbackAlpha;
        private bool rollbackInteractable;
        private bool rollbackBlocksRaycasts;

        public bool IsActive => previewActive;

        public bool Begin(
            Patch4CharacterRigController patchRig,
            Patch4CharacterStateMachine patchStateMachine,
            Patch4CharacterVisibilityGuard patchVisibilityGuard,
            Patch4V23FullFramePresentation presentation,
            Animator patchAnimator,
            GameObject patchVisual,
            GameObject rollbackVisual)
        {
            if (previewActive)
            {
                return true;
            }

            if (patchRig == null ||
                patchStateMachine == null ||
                patchVisibilityGuard == null ||
                presentation == null ||
                !presentation.IsReady ||
                patchAnimator == null ||
                patchVisual == null ||
                rollbackVisual == null)
            {
                return false;
            }

            rigController = patchRig;
            stateMachine = patchStateMachine;
            visibilityGuard = patchVisibilityGuard;
            fullFramePresentation = presentation;
            animator = patchAnimator;
            patch4VisualRoot = patchVisual;
            patch35RollbackRoot = rollbackVisual;

            visibilityGuardWasEnabled = visibilityGuard.enabled;
            rollbackRootWasActive = patch35RollbackRoot.activeSelf;
            animatorUpdateMode = animator.updateMode;
            animatorCullingMode = animator.cullingMode;
            animatorSpeed = animator.speed;

            rollbackGroup = patch35RollbackRoot.GetComponent<CanvasGroup>();
            if (rollbackGroup == null)
            {
                rollbackGroup =
                    patch35RollbackRoot.AddComponent<CanvasGroup>();
                rollbackGroupAdded = true;
            }

            rollbackAlpha = rollbackGroup.alpha;
            rollbackInteractable = rollbackGroup.interactable;
            rollbackBlocksRaycasts = rollbackGroup.blocksRaycasts;

            // The approved gameplay system stays logically active and owns
            // movement, input and routine state. Only its pixels are hidden.
            rigController.SetPatch4Enabled(false);
            stateMachine.SetLockedReviewActive(true);
            visibilityGuard.enabled = false;
            patch35RollbackRoot.SetActive(true);
            rollbackGroup.alpha = 0f;
            rollbackGroup.interactable = false;
            rollbackGroup.blocksRaycasts = false;
            patch4VisualRoot.SetActive(true);

            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;

            fullFramePresentation.SetReviewActive(false);
            if (!fullFramePresentation.SetEditorGameplayPreviewActive(true))
            {
                EndPreview();
                return false;
            }

            previewActive = true;
            Canvas.ForceUpdateCanvases();
            return true;
        }

        public void EndPreview()
        {
            bool hadPreviewState =
                previewActive ||
                rigController != null ||
                rollbackGroup != null;
            previewActive = false;
            if (!hadPreviewState)
            {
                return;
            }

            if (fullFramePresentation != null)
            {
                fullFramePresentation.SetEditorGameplayPreviewActive(false);
            }

            if (stateMachine != null)
            {
                stateMachine.SetLockedReviewActive(false);
            }

            if (rigController != null)
            {
                rigController.SetPatch4Enabled(false);
            }

            if (patch4VisualRoot != null)
            {
                patch4VisualRoot.SetActive(false);
            }

            if (patch35RollbackRoot != null)
            {
                patch35RollbackRoot.SetActive(rollbackRootWasActive);
            }

            if (rollbackGroup != null)
            {
                rollbackGroup.alpha = rollbackAlpha;
                rollbackGroup.interactable = rollbackInteractable;
                rollbackGroup.blocksRaycasts = rollbackBlocksRaycasts;
                if (rollbackGroupAdded)
                {
                    Destroy(rollbackGroup);
                }
            }

            if (visibilityGuard != null)
            {
                visibilityGuard.enabled = visibilityGuardWasEnabled;
            }

            if (animator != null)
            {
                animator.updateMode = animatorUpdateMode;
                animator.cullingMode = animatorCullingMode;
                animator.speed = animatorSpeed;
            }

            rigController = null;
            stateMachine = null;
            visibilityGuard = null;
            fullFramePresentation = null;
            animator = null;
            patch4VisualRoot = null;
            patch35RollbackRoot = null;
            rollbackGroup = null;
        }

        private void OnDisable()
        {
            EndPreview();
        }

        private void OnDestroy()
        {
            EndPreview();
        }
    }
}
#endif
