#if UNITY_EDITOR
using System.Collections.Generic;
using SkinnyToBeast.Gameplay;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Transient Editor-only visual override for inspecting Patch 4 through
    /// the real gameplay room and its real action signals. It never opens the
    /// production-art gate and restores the rollback visual when it ends.
    /// </summary>
    [DefaultExecutionOrder(1210)]
    [DisallowMultipleComponent]
    public sealed class Patch4InteractiveGameplayPreviewDriver : MonoBehaviour
    {
        private struct RoomAnchorSnapshot
        {
            public RoomAnchor anchor;
            public RoomAnchorKind kind;
            public Vector2 position;
            public float characterScale;
            public CharacterFacing restingFacing;
            public float minimumStay;
            public float maximumStay;
        }

        private const float SafeRouteY = 0.515f;
        private const float SafeCharacterScale = 0.7f;

        private Patch4CharacterRigController rigController;
        private CharacterRigController legacyRigController;
        private CharacterRoutineController routineController;
        private CharacterFaceController legacyFaceController;
        private RectTransform legacyCharacterRoot;
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
        private bool routineWasEnabled;
        private bool safeRoomConfigured;
        private int walkFacingSign = 1;
        private readonly List<RoomAnchorSnapshot> roomAnchorSnapshots = new();

        public bool IsActive => previewActive;

        public bool Begin(
            Patch4CharacterRigController patchRig,
            CharacterRigController approvedLegacyRig,
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
                approvedLegacyRig == null ||
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
            legacyRigController = approvedLegacyRig;
            legacyCharacterRoot =
                legacyRigController.transform as RectTransform;
            routineController =
                legacyRigController.GetComponent<CharacterRoutineController>();
            legacyFaceController =
                legacyRigController.GetComponent<CharacterFaceController>();
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

            if (legacyCharacterRoot == null ||
                routineController == null ||
                legacyFaceController == null ||
                !ConfigureSafeRoomRoute())
            {
                EndPreview();
                return false;
            }

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
            UpdateWalkFacing();
            Canvas.ForceUpdateCanvases();
            return true;
        }

        private void LateUpdate()
        {
            if (previewActive)
            {
                UpdateWalkFacing();
            }
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

            RestoreRoomRoute();

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
            legacyRigController = null;
            routineController = null;
            legacyFaceController = null;
            legacyCharacterRoot = null;
            stateMachine = null;
            visibilityGuard = null;
            fullFramePresentation = null;
            animator = null;
            patch4VisualRoot = null;
            patch35RollbackRoot = null;
            rollbackGroup = null;
            walkFacingSign = 1;
        }

        private bool ConfigureSafeRoomRoute()
        {
            RectTransform actorLayer =
                legacyCharacterRoot != null
                    ? legacyCharacterRoot.parent as RectTransform
                    : null;
            if (actorLayer == null || routineController == null)
            {
                return false;
            }

            RoomAnchor[] anchors =
                actorLayer.GetComponentsInChildren<RoomAnchor>(true);
            if (anchors.Length < 2)
            {
                return false;
            }

            RoomAnchor trainingAnchor = null;
            for (int i = 0; i < anchors.Length; i++)
            {
                RoomAnchor anchor = anchors[i];
                if (anchor == null)
                {
                    continue;
                }

                roomAnchorSnapshots.Add(new RoomAnchorSnapshot
                {
                    anchor = anchor,
                    kind = anchor.Kind,
                    position = anchor.Position,
                    characterScale = anchor.CharacterScale,
                    restingFacing = anchor.RestingFacing,
                    minimumStay = anchor.MinimumStay,
                    maximumStay = anchor.MaximumStay
                });
                if (anchor.Kind == RoomAnchorKind.Training)
                {
                    trainingAnchor = anchor;
                }
            }

            if (trainingAnchor == null)
            {
                roomAnchorSnapshots.Clear();
                return false;
            }

            routineWasEnabled = routineController.enabled;
            routineController.enabled = false;
            for (int i = 0; i < roomAnchorSnapshots.Count; i++)
            {
                RoomAnchorSnapshot snapshot = roomAnchorSnapshots[i];
                bool training =
                    snapshot.kind == RoomAnchorKind.Training;
                snapshot.anchor.ConfigureNormalized(
                    training
                        ? RoomAnchorKind.Training
                        : RoomAnchorKind.Center,
                    new Vector2(
                        ResolveSafeRouteX(snapshot.kind),
                        SafeRouteY),
                    SafeCharacterScale,
                    CharacterFacing.Front,
                    snapshot.minimumStay,
                    snapshot.maximumStay);
            }

            routineController.Configure(
                legacyCharacterRoot,
                legacyRigController,
                legacyFaceController,
                anchors);
            routineController.enabled = routineWasEnabled;
            safeRoomConfigured = true;
            return true;
        }

        private void RestoreRoomRoute()
        {
            if (!safeRoomConfigured)
            {
                roomAnchorSnapshots.Clear();
                return;
            }

            safeRoomConfigured = false;
            if (routineController != null)
            {
                routineController.enabled = false;
            }

            List<RoomAnchor> restoredAnchors = new();
            for (int i = 0; i < roomAnchorSnapshots.Count; i++)
            {
                RoomAnchorSnapshot snapshot = roomAnchorSnapshots[i];
                if (snapshot.anchor == null)
                {
                    continue;
                }

                snapshot.anchor.Configure(
                    snapshot.kind,
                    snapshot.position,
                    snapshot.characterScale,
                    snapshot.restingFacing,
                    snapshot.minimumStay,
                    snapshot.maximumStay);
                restoredAnchors.Add(snapshot.anchor);
            }

            if (routineController != null &&
                legacyCharacterRoot != null &&
                legacyRigController != null &&
                legacyFaceController != null &&
                restoredAnchors.Count > 0)
            {
                routineController.Configure(
                    legacyCharacterRoot,
                    legacyRigController,
                    legacyFaceController,
                    restoredAnchors);
                routineController.enabled = routineWasEnabled;
            }

            roomAnchorSnapshots.Clear();
        }

        private void UpdateWalkFacing()
        {
            if (legacyRigController == null ||
                fullFramePresentation == null)
            {
                return;
            }

            CharacterFacing facing = legacyRigController.Facing;
            if (facing == CharacterFacing.SideLeft)
            {
                walkFacingSign = -1;
            }
            else if (facing == CharacterFacing.SideRight)
            {
                walkFacingSign = 1;
            }

            fullFramePresentation.SetEditorWalkFacingSign(walkFacingSign);
        }

        private static float ResolveSafeRouteX(RoomAnchorKind kind)
        {
            switch (kind)
            {
                case RoomAnchorKind.Center:
                    return 0.07f;
                case RoomAnchorKind.Sofa:
                    return 0.075f;
                case RoomAnchorKind.Training:
                    return 0.08f;
                case RoomAnchorKind.Window:
                    return 0.085f;
                case RoomAnchorKind.Mirror:
                    return 0.09f;
                default:
                    return 0f;
            }
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
