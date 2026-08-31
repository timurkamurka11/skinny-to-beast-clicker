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
        private Patch4LegacySignalBridge signalBridge;
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
        private bool editorPresentationOwned;
        private bool manualControls;
        private bool signalBridgeWasEnabled;
        private Vector3 manualHomePosition;
        private Vector3 manualTargetPosition;
        private bool manualWalking;
        private int manualFacingSign = 1;
        private string currentDevelopmentClip = "FatMan_Idle_Breathe";
        private string lastError = string.Empty;
        private readonly List<RoomAnchorSnapshot> roomAnchorSnapshots = new();

        public bool IsActive => previewActive;
        public bool AnimatorReady => animator != null &&
            Patch4CharacterStateMachine.ValidateAnimatorContract(animator, out _);
        public bool LegacyReady => legacyRigController != null &&
            legacyRigController.AnimatorReady &&
            legacyRigController.GetComponent<CharacterSkinController>()?.IsVisualReady == true;
        public int VisiblePresentationCount =>
            rigController != null ? rigController.VisiblePresentationCount : 0;
        public string CurrentDevelopmentClip => currentDevelopmentClip;
        public string LastError => lastError;

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
            GameObject rollbackVisual,
            bool useManualControls = false)
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
            signalBridge = patchRig.GetComponent<Patch4LegacySignalBridge>();
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
            signalBridgeWasEnabled = signalBridge != null && signalBridge.enabled;
            manualControls = useManualControls;

            if (legacyCharacterRoot == null ||
                legacySpriteRigController == null ||
                routineController == null ||
                legacyFaceController == null ||
                !legacyRigController.AnimatorReady ||
                legacyRigController.GetComponent<CharacterSkinController>()?.IsVisualReady != true ||
                !ConfigureSafeRoomRoute())
            {
                lastError = legacyRigController != null
                    ? legacyRigController.AnimatorReadinessError
                    : "Legacy gameplay rig is missing.";
                EndPreview();
                return false;
            }

            // The approved gameplay system stays logically active and owns
            // movement, input and routine state. Only its pixels are hidden.
            rigController.SetPatch4Enabled(false);
            stateMachine.SetLockedReviewActive(true);
            faceController.SetEditorReviewActive(true);
            secondaryMotion.SetEditorReviewActive(true);

            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;

            fullFramePresentation.SetReviewActive(false);
            if (!fullFramePresentation.SetEditorGameplayPreviewActive(true))
            {
                EndPreview();
                return false;
            }

            if (!rigController.TryBeginEditorPresentationOverride(
                    this, false, out lastError))
            {
                EndPreview();
                return false;
            }
            editorPresentationOwned = true;
            previewActive = true;

            manualHomePosition = legacyCharacterRoot.localPosition;
            manualTargetPosition = manualHomePosition;
            if (manualControls)
            {
                routineController.enabled = false;
                if (signalBridge != null) signalBridge.enabled = false;
                stateMachine.SetWalkSpeed(0f);
                PlayDevelopmentClip("FatMan_Idle_Breathe");
            }
            else if (signalBridge == null ||
                     !signalBridge.SynchronizeCurrentGameplayState())
            {
                lastError = "The gameplay signals could not be synchronized " +
                    "before the Editor presentation handoff.";
                EndPreview();
                return false;
            }

            KeepFrontFacingRig();
            Canvas.ForceUpdateCanvases();
            return true;
        }

        private void LateUpdate()
        {
            if (previewActive)
            {
                UpdateManualWalk();
                KeepFrontFacingRig();
            }
        }

        public bool PlayDevelopmentClip(string clipName)
        {
            if (!previewActive || animator == null || string.IsNullOrEmpty(clipName))
            {
                return false;
            }
            int hash = Animator.StringToHash(
                Patch4CharacterStateMachine.AnimatorLayerName + "." + clipName);
            if (!animator.HasState(0, hash)) return false;
            manualWalking = false;
            manualFacingSign = 1;
            stateMachine.SetWalkSpeed(0f);
            animator.Play(hash, 0, 0f);
            animator.Update(0f);
            currentDevelopmentClip = clipName;
            return true;
        }

        public void WalkLeft() => BeginManualWalk(-1f);
        public void WalkRight() => BeginManualWalk(1f);

        public void ResetDevelopmentDemo()
        {
            if (!previewActive || legacyCharacterRoot == null) return;
            manualWalking = false;
            legacyCharacterRoot.localPosition = manualHomePosition;
            manualTargetPosition = manualHomePosition;
            stateMachine.SetWalkSpeed(0f);
            PlayDevelopmentClip("FatMan_Idle_Breathe");
        }

        private void BeginManualWalk(float direction)
        {
            if (!previewActive || !manualControls || legacyCharacterRoot == null) return;
            RectTransform parent = legacyCharacterRoot.parent as RectTransform;
            float span = parent != null ? Mathf.Max(80f, parent.rect.width * 0.16f) : 180f;
            manualTargetPosition = manualHomePosition + Vector3.right * span * Mathf.Sign(direction);
            manualTargetPosition.y = manualHomePosition.y;
            manualWalking = true;
            manualFacingSign = direction < 0f ? -1 : 1;
            currentDevelopmentClip = "FatMan_Walk_InRoom";
            fullFramePresentation.SetEditorWalkFacingSign(manualFacingSign);
            stateMachine.SetWalkSpeed(1f);
        }

        private void UpdateManualWalk()
        {
            if (!manualControls || !manualWalking || legacyCharacterRoot == null) return;
            legacyCharacterRoot.localPosition = Vector3.MoveTowards(
                legacyCharacterRoot.localPosition,
                manualTargetPosition,
                Mathf.Max(1f, 220f * Time.unscaledDeltaTime));
            legacyCharacterRoot.localPosition = new Vector3(
                legacyCharacterRoot.localPosition.x,
                manualHomePosition.y,
                manualHomePosition.z);
            if ((legacyCharacterRoot.localPosition - manualTargetPosition).sqrMagnitude <= 0.25f)
            {
                manualWalking = false;
                stateMachine.SetWalkSpeed(0f);
                currentDevelopmentClip = "FatMan_Idle_Breathe";
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

            if (manualControls && legacyCharacterRoot != null)
            {
                legacyCharacterRoot.localPosition = manualHomePosition;
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

            if (rigController != null && editorPresentationOwned)
            {
                rigController.EndEditorPresentationOverride(this);
                editorPresentationOwned = false;
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
            if (signalBridge != null) signalBridge.enabled = signalBridgeWasEnabled;

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
            signalBridge = null;
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

            if (manualControls)
            {
                fullFramePresentation.SetEditorWalkFacingSign(manualFacingSign);
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
