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

        private const float SafeCharacterScale = 0.7f;

        private Patch4CharacterRigController rigController;
        private CharacterRigController legacyRigController;
        private CharacterSpriteRigController legacySpriteRigController;
        private CharacterRoutineController routineController;
        private CharacterFaceController legacyFaceController;
        private RectTransform legacyCharacterRoot;
        private Patch4CharacterStateMachine stateMachine;
        private Patch4FaceController faceController;
        private Patch4SecondaryMotionController secondaryMotion;
        private Patch4CharacterVisibilityGuard visibilityGuard;
        private Patch4V23FullFramePresentation fullFramePresentation;
        private Animator animator;
        private GameObject patch4VisualRoot;
        private GameObject patch35RollbackRoot;

        private bool previewActive;
        private bool visibilityGuardWasEnabled;
        private bool rollbackRootWasActive;
        private bool legacyPixelsWereSuppressed;
        private AnimatorUpdateMode animatorUpdateMode;
        private AnimatorCullingMode animatorCullingMode;
        private float animatorSpeed;
        private bool routineWasEnabled;
        private bool safeRoomConfigured;
        private readonly List<RoomAnchorSnapshot> roomAnchorSnapshots = new();

        public bool IsActive => previewActive;

        public bool Begin(
            Patch4CharacterRigController patchRig,
            CharacterRigController approvedLegacyRig,
            Patch4CharacterStateMachine patchStateMachine,
            Patch4FaceController patchFaceController,
            Patch4SecondaryMotionController patchSecondaryMotion,
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
                patchFaceController == null ||
                patchSecondaryMotion == null ||
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
            legacySpriteRigController =
                legacyRigController.GetComponent<
                    CharacterSpriteRigController>();
            routineController =
                legacyRigController.GetComponent<CharacterRoutineController>();
            legacyFaceController =
                legacyRigController.GetComponent<CharacterFaceController>();
            stateMachine = patchStateMachine;
            faceController = patchFaceController;
            secondaryMotion = patchSecondaryMotion;
            visibilityGuard = patchVisibilityGuard;
            fullFramePresentation = presentation;
            animator = patchAnimator;
            patch4VisualRoot = patchVisual;
            patch35RollbackRoot = rollbackVisual;

            visibilityGuardWasEnabled = visibilityGuard.enabled;
            rollbackRootWasActive = patch35RollbackRoot.activeSelf;
            legacyPixelsWereSuppressed =
                legacySpriteRigController != null &&
                legacySpriteRigController.EditorPreviewSuppressed;
            animatorUpdateMode = animator.updateMode;
            animatorCullingMode = animator.cullingMode;
            animatorSpeed = animator.speed;

            if (legacyCharacterRoot == null ||
                legacySpriteRigController == null ||
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
            faceController.SetEditorReviewActive(true);
            secondaryMotion.SetEditorReviewActive(true);
            visibilityGuard.enabled = false;
            // Keep the complete legacy visual hierarchy active so the real
            // GameplayVisualStageController can continue validating Stage 4.
            // The renderer owner suppresses only its pixels, including the
            // bounded Patch 3.5 puppet and face overlays, without producing a
            // second body or triggering an endless stage-repair retry.
            legacySpriteRigController.SetEditorPreviewSuppressed(true);
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
            KeepFrontFacingRig();
            Canvas.ForceUpdateCanvases();
            return true;
        }

        private void LateUpdate()
        {
            if (previewActive)
            {
                KeepFrontFacingRig();
            }
        }

        public void EndPreview()
        {
            bool hadPreviewState =
                previewActive ||
                rigController != null ||
                patch35RollbackRoot != null;
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

            if (faceController != null)
            {
                faceController.SetLookPose(false);
                faceController.SetMouth(
                    Patch4FaceController.MouthPose.Closed);
                faceController.SetEditorReviewActive(false);
            }

            if (secondaryMotion != null)
            {
                secondaryMotion.SetEditorReviewActive(false);
            }

            if (rigController != null)
            {
                rigController.SetPatch4Enabled(false);
            }

            if (patch4VisualRoot != null)
            {
                patch4VisualRoot.SetActive(false);
            }

            if (legacySpriteRigController != null)
            {
                legacySpriteRigController.SetEditorPreviewSuppressed(
                    legacyPixelsWereSuppressed);
            }

            if (patch35RollbackRoot != null)
            {
                patch35RollbackRoot.SetActive(rollbackRootWasActive);
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
            legacySpriteRigController = null;
            routineController = null;
            legacyFaceController = null;
            legacyCharacterRoot = null;
            stateMachine = null;
            faceController = null;
            secondaryMotion = null;
            visibilityGuard = null;
            fullFramePresentation = null;
            animator = null;
            patch4VisualRoot = null;
            patch35RollbackRoot = null;
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
                        ResolveSafeRouteY(snapshot.kind)),
                    ResolveSafeRouteScale(snapshot.kind),
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

        private void KeepFrontFacingRig()
        {
            if (fullFramePresentation == null)
            {
                return;
            }

            // The available painted rig is frontal. Flipping it from the
            // legacy SideLeft/SideRight signal made depth travel read as a
            // paper cutout walking sideways. Route anchors already request
            // Front, so keep one stable orientation until a true layered side
            // rig exists.
            fullFramePresentation.SetEditorWalkFacingSign(1);
        }

        private static float ResolveSafeRouteX(RoomAnchorKind kind)
        {
            switch (kind)
            {
                case RoomAnchorKind.Center:
                    return -0.06f;
                case RoomAnchorKind.Sofa:
                    return -0.18f;
                case RoomAnchorKind.Training:
                    return 0f;
                case RoomAnchorKind.Window:
                    return 0.06f;
                case RoomAnchorKind.Mirror:
                    return 0.18f;
                default:
                    return 0f;
            }
        }

        private static float ResolveSafeRouteY(RoomAnchorKind kind)
        {
            // The continuous front-facing rig travels through room depth
            // instead of sliding along one horizontal screen line. Keeping
            // the lateral span narrow also prevents a front view from reading
            // as an implausible sideways walk.
            switch (kind)
            {
                case RoomAnchorKind.Center:
                    return 0.52f;
                case RoomAnchorKind.Sofa:
                    return 0.55f;
                case RoomAnchorKind.Training:
                    return 0.49f;
                case RoomAnchorKind.Window:
                    return 0.59f;
                case RoomAnchorKind.Mirror:
                    return 0.55f;
                default:
                    return 0.52f;
            }
        }

        private static float ResolveSafeRouteScale(RoomAnchorKind kind)
        {
            // Depth destinations grow slightly toward the foreground. The
            // legacy routine interpolates this value continuously, so travel
            // reads as walking through the room rather than a horizontal slide.
            switch (kind)
            {
                case RoomAnchorKind.Training:
                    return SafeCharacterScale;
                case RoomAnchorKind.Center:
                    return 0.68f;
                case RoomAnchorKind.Sofa:
                    return 0.66f;
                case RoomAnchorKind.Window:
                    return 0.61f;
                case RoomAnchorKind.Mirror:
                    return 0.65f;
                default:
                    return SafeCharacterScale;
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
