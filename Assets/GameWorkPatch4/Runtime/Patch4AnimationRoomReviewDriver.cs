#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SkinnyToBeast.Gameplay;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Editor-only Play Mode driver for the locked animation review. It samples
    /// the real LivingGameplayScene presentation, never approves readiness and
    /// restores Patch 3.5 before leaving Play Mode. This type is absent from
    /// player builds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Patch4AnimationRoomReviewDriver : MonoBehaviour
    {
        [Serializable]
        private sealed class ClipReview
        {
            public string clipName = string.Empty;
            public float sourceLength;
            public float reviewDuration;
            public string animatorStatePath = string.Empty;
            public int animatorStateHash;
            public int startStateFullPathHash;
            public int peakStateFullPathHash;
            public bool animatorStateAvailable;
            public bool startStateEntered;
            public bool peakStateEntered;
            public bool animatorStateBindingPassed;
            public bool captured;
            public bool visualSanityPassed;
            public int changedPixelCount;
            public int silhouetteWidth;
            public int silhouetteHeight;
            public float widthCoverage;
            public float heightCoverage;
            public float areaCoverage;
            public float neutralWidthRetention;
            public float neutralHeightRetention;
            public float neutralAreaRetention;
            public int motionChangedPixelCount;
            public float motionCoverage;
            public float minimumMotionCoverage;
            public int globalAlignmentX;
            public int globalAlignmentY;
            public bool focusedFaceMotionRequired;
            public int faceMotionChangedPixelCount;
            public int faceReferencePixelCount;
            public float faceMotionCoverage;
            public float minimumFaceMotionCoverage;
            public bool focusedFaceMotionPassed;
            public bool limbArticulationRequired;
            public int limbMotionChangedPixelCount;
            public int limbReferencePixelCount;
            public float limbMotionCoverage;
            public float minimumLimbMotionCoverage;
            public float leftArmMotionCoverage;
            public float rightArmMotionCoverage;
            public float leftLegMotionCoverage;
            public float rightLegMotionCoverage;
            public bool allLimbRegionsPassed;
            public bool limbArticulationPassed;
            public int walkPhaseCount;
            public float walkRootTravelPixels;
            public float minimumWalkRootTravelPixels;
            public float v22LeftArmSilhouetteDifference;
            public float v22RightArmSilhouetteDifference;
            public float v22LeftLegSilhouetteDifference;
            public float v22RightLegSilhouetteDifference;
            public float v22MinimumAdjacentFrameDifference;
            public bool v22FrameSequenceReady;
            public bool v22FrameSequenceUsed;
            public bool walkRootTravelPassed;
            public bool walkPhaseAlternationPassed;
            public bool walkCycleCaptured;
            public bool visibleMotionPassed;
        }

        [Serializable]
        private sealed class ReviewReport
        {
            public string runToken = string.Empty;
            public string generatedUtc = string.Empty;
            public bool completed;
            public bool passedTechnicalChecks;
            public bool actualLivingGameplayRoom;
            public bool allTenClipsReviewed;
            public bool canvasSkinBindingsReady;
            public bool canvasBindAnchorsFrozen;
            public int canvasSkinDeformerCount;
            public int weightedLayerCount;
            public int rigidRuntimeLayerCount;
            public bool continuousBodyBindingReady;
            public bool readinessGateRemainedLocked;
            public bool patch35Restored;
            public bool legacyRigStayedLogicallyActive;
            public bool visualSanityPassed;
            public bool visibleMotionPassed;
            public bool animatorStateBindingPassed;
            public bool walkCycleCaptured;
            public bool walkRootTravelPassed;
            public bool walkPhaseAlternationPassed;
            public bool v22WalkFrameSequenceReady;
            public bool legacyRoutinePausedForReview;
            public bool legacyRoutineRestored;
            public bool legacySignalBridgePausedForReview;
            public bool legacySignalBridgeRestored;
            public bool legacyOneShotAudioStopped;
            public int reviewConsoleErrorCount;
            public bool humanReviewRequired = true;
            public bool activationAllowed;
            public string contactSheetPath = string.Empty;
            public string walkCyclePath = string.Empty;
            public string error = string.Empty;
            public List<string> reviewConsoleErrors = new();
            public List<ClipReview> clips = new();
        }

        public const string ReportFileName =
            "patch4-animation-room-review.json";
        public const string ContactSheetFileName =
            "patch4-animation-room-review.png";
        public const string WalkCycleFileName =
            "patch4-walk-cycle-review.png";

        private const int ContactColumns = 5;
        private const int ContactRows = 2;
        private const int ThumbnailWidth = 300;
        private const int ThumbnailHeight = 420;
        private const int WalkPhaseCount = 8;
        private const int WalkContactPhaseIndex = 4;
        private const int WalkThumbnailWidth = 220;
        private const int WalkThumbnailHeight = 310;
        private const int PixelDifferenceThreshold = 42;
        private const int MotionPixelDifferenceThreshold = 34;
        private const float MinimumSilhouetteWidthCoverage = 0.36f;
        private const float MinimumSilhouetteHeightCoverage = 0.60f;
        private const float MinimumSilhouetteAreaCoverage = 0.10f;
        private const float MinimumNeutralWidthRetention = 0.72f;
        private const float MinimumNeutralHeightRetention = 0.78f;
        private const float MinimumNeutralAreaRetention = 0.58f;
        private const float MaximumNeutralWidthExpansion = 1.16f;
        private const float MaximumNeutralHeightExpansion = 1.12f;
        private const float MaximumNeutralAreaExpansion = 1.20f;
        private const float MinimumBlinkFaceMotionCoverage = 0.015f;
        private const float MinimumWalkLimbMotionCoverage = 0.04f;
        private const float MinimumWalkArmMotionCoverage = 0.035f;
        private const float MinimumWalkLegMotionCoverage = 0.04f;
        private const float MinimumV22WalkArmSilhouetteDifference = 0.14f;
        private const float MinimumV22WalkLegSilhouetteDifference = 0.14f;
        private const float MinimumV22AdjacentFrameDifference = 0.075f;
        private const float MinimumWalkTravelWidthRatio = 0.55f;

        private Patch4CharacterRigController rigController;
        private Patch4CharacterVisibilityGuard visibilityGuard;
        private Patch4CanvasPresentation canvasPresentation;
        private Patch4FaceController faceController;
        private Patch4SecondaryMotionController secondaryMotion;
        private Patch4V22WalkCyclePresentation v22WalkPresentation;
        private Animator animator;
        private GameObject patch4VisualRoot;
        private GameObject patch35RollbackRoot;
        private string outputDirectory = string.Empty;
        private string reviewRunToken = string.Empty;
        private Texture2D contactSheet;
        private Texture2D walkCycleSheet;
        private Color32[] backgroundPixels = Array.Empty<Color32>();
        private int backgroundWidth;
        private int backgroundHeight;
        private CanvasGroup rollbackReviewGroup;
        private CharacterRoutineController legacyRoutine;
        private Patch4LegacySignalBridge legacySignalBridge;
        private bool legacyRoutineWasEnabled;
        private bool legacySignalBridgeWasEnabled;
        private bool rollbackGroupAddedForReview;
        private float rollbackGroupPreviousAlpha = 1f;
        private bool rollbackGroupPreviousInteractable;
        private bool rollbackGroupPreviousBlocksRaycasts;
        private ReviewReport report;
        private Color32[] clipStartPixels = Array.Empty<Color32>();
        private Rect neutralExpectedScreenRect;
        private int neutralSilhouetteWidth;
        private int neutralSilhouetteHeight;
        private int neutralSilhouetteArea;
        private bool neutralReferenceCaptured;
        private Vector3 reviewBaseLocalPosition;
        private bool reviewBasePositionCaptured;
        private float reviewTravelLocalDistance;
        private readonly float[] walkPhaseRootScreenX =
            new float[WalkPhaseCount];
        private string currentClip = string.Empty;
        private bool started;
        private bool logCaptureRegistered;

        public event Action<bool, string> ReviewFinished;

        public void Begin(
            Patch4CharacterRigController rig,
            Patch4CharacterVisibilityGuard visibility,
            Patch4CanvasPresentation presentation,
            Patch4FaceController face,
            Patch4SecondaryMotionController motion,
            Patch4V22WalkCyclePresentation walkPresentation,
            Animator targetAnimator,
            GameObject visualRoot,
            GameObject rollbackRoot,
            string reportDirectory,
            string runToken)
        {
            if (started)
            {
                return;
            }

            started = true;
            rigController = rig;
            visibilityGuard = visibility;
            canvasPresentation = presentation;
            faceController = face;
            secondaryMotion = motion;
            v22WalkPresentation = walkPresentation;
            animator = targetAnimator;
            patch4VisualRoot = visualRoot;
            patch35RollbackRoot = rollbackRoot;
            outputDirectory = reportDirectory ?? string.Empty;
            reviewRunToken = runToken ?? string.Empty;
            Application.logMessageReceived += OnReviewLog;
            logCaptureRegistered = true;
            StartCoroutine(RunReview());
        }

        private IEnumerator RunReview()
        {
            report = new ReviewReport
            {
                runToken = reviewRunToken,
                generatedUtc = DateTime.UtcNow.ToString("O"),
                actualLivingGameplayRoom =
                    IsInsideLivingGameplayRoom(
                        rigController != null
                            ? rigController.transform
                            : null),
                canvasSkinBindingsReady =
                    canvasPresentation != null &&
                    canvasPresentation.SkinBindingsReady,
                canvasBindAnchorsFrozen =
                    canvasPresentation != null &&
                    canvasPresentation.BindAnchorsFrozen,
                canvasSkinDeformerCount =
                    canvasPresentation != null
                        ? canvasPresentation.SkinDeformerCount
                        : 0,
                weightedLayerCount =
                    canvasPresentation != null
                        ? canvasPresentation.WeightedLayerCount
                        : 0,
                rigidRuntimeLayerCount =
                    canvasPresentation != null
                        ? canvasPresentation.RuntimeRigidLayerCount
                        : 0,
                continuousBodyBindingReady =
                    canvasPresentation != null &&
                    canvasPresentation.ContinuousBodyBindingReady,
                v22WalkFrameSequenceReady =
                    v22WalkPresentation != null &&
                    v22WalkPresentation.IsReady &&
                    v22WalkPresentation.FrameCount ==
                        Patch4V22WalkCyclePresentation.RequiredFrameCount,
                readinessGateRemainedLocked =
                    rigController != null &&
                    !rigController.Patch4Enabled,
                activationAllowed = false
            };

            string validationError = ValidateSetup();
            if (!string.IsNullOrEmpty(validationError))
            {
                Finish(false, validationError);
                yield break;
            }

            Dictionary<string, AnimationClip> clips =
                ResolveRequiredClips();
            if (clips.Count != Patch4RigContract.RequiredClipNames.Count)
            {
                Finish(
                    false,
                    "The Animator does not expose all ten required clips.");
                yield break;
            }

            PrepareLockedReview();
            InitializeContactSheet();
            InitializeWalkCycleSheet();

            yield return null;
            yield return new WaitForEndOfFrame();
            patch4VisualRoot.SetActive(false);
            yield return null;
            yield return new WaitForEndOfFrame();
            if (!CaptureReviewBackground())
            {
                Finish(
                    false,
                    "The clean gameplay-room background could not be captured.");
                yield break;
            }

            patch4VisualRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return new WaitForEndOfFrame();

            for (int i = 0;
                 i < Patch4RigContract.RequiredClipNames.Count;
                 i++)
            {
                string clipName =
                    Patch4RigContract.RequiredClipNames[i];
                AnimationClip clip = clips[clipName];
                ClipReview clipReport = new()
                {
                    clipName = clipName,
                    sourceLength = clip.length
                };
                report.clips.Add(clipReport);
                yield return ReviewClip(clip, clipReport, i);
            }

            report.allTenClipsReviewed =
                report.clips.Count ==
                Patch4RigContract.RequiredClipNames.Count;
            report.visualSanityPassed =
                report.allTenClipsReviewed;
            report.visibleMotionPassed =
                report.allTenClipsReviewed;
            report.animatorStateBindingPassed =
                report.allTenClipsReviewed;
            report.walkCycleCaptured = false;
            report.walkRootTravelPassed = false;
            report.walkPhaseAlternationPassed = false;
            for (int i = 0; i < report.clips.Count; i++)
            {
                report.allTenClipsReviewed &=
                    report.clips[i].captured;
                report.visualSanityPassed &=
                    report.clips[i].visualSanityPassed;
                report.visibleMotionPassed &=
                    report.clips[i].visibleMotionPassed;
                report.animatorStateBindingPassed &=
                    report.clips[i].animatorStateBindingPassed;
                if (string.Equals(
                        report.clips[i].clipName,
                        "FatMan_Walk_InRoom",
                        StringComparison.Ordinal))
                {
                    report.walkCycleCaptured =
                        report.clips[i].walkCycleCaptured;
                    report.walkRootTravelPassed =
                        report.clips[i].walkRootTravelPassed;
                    report.walkPhaseAlternationPassed =
                        report.clips[i].walkPhaseAlternationPassed;
                }
            }

            Finish(
                report.allTenClipsReviewed &&
                report.visualSanityPassed &&
                report.visibleMotionPassed &&
                report.animatorStateBindingPassed &&
                report.v22WalkFrameSequenceReady &&
                report.walkCycleCaptured &&
                report.walkRootTravelPassed &&
                report.walkPhaseAlternationPassed,
                report.visualSanityPassed &&
                report.visibleMotionPassed &&
                report.animatorStateBindingPassed &&
                report.v22WalkFrameSequenceReady &&
                report.walkCycleCaptured &&
                report.walkRootTravelPassed &&
                report.walkPhaseAlternationPassed
                    ? string.Empty
                    : "One or more animation clips did not enter the required " +
                      "Animator state, collapsed or failed the visible " +
                      "motion check, or the walk did not show eight " +
                      "opposing limb phases with real room travel.");
        }

        private IEnumerator ReviewClip(
            AnimationClip clip,
            ClipReview clipReport,
            int clipIndex)
        {
            currentClip = clip.name;
            ConfigureFaceForClip(clip.name);
            bool isWalk = string.Equals(
                clip.name,
                "FatMan_Walk_InRoom",
                StringComparison.Ordinal);
            v22WalkPresentation.SetReviewActive(isWalk);
            clipReport.v22FrameSequenceReady =
                v22WalkPresentation.IsReady &&
                v22WalkPresentation.FrameCount == WalkPhaseCount;
            clipReport.v22FrameSequenceUsed =
                isWalk && clipReport.v22FrameSequenceReady;

            float reviewDuration = Mathf.Clamp(
                clip.length,
                0.55f,
                2.4f);
            float captureNormalizedTime =
                ResolveCaptureNormalizedTime(clip.name);
            float captureAt =
                reviewDuration * captureNormalizedTime;
            if (string.Equals(
                    clip.name,
                    "FatMan_Blink_Random",
                    StringComparison.Ordinal))
            {
                reviewDuration = 0.55f;
                captureAt = 0.09f;
                captureNormalizedTime = captureAt / reviewDuration;
            }

            clipReport.reviewDuration = reviewDuration;
            clipReport.minimumMotionCoverage =
                ResolveMinimumMotionCoverage(clip.name);
            clipReport.animatorStatePath =
                ResolveAnimatorStatePath(clip.name);
            clipReport.animatorStateHash = Animator.StringToHash(
                clipReport.animatorStatePath);
            clipReport.animatorStateAvailable = animator.HasState(
                0,
                clipReport.animatorStateHash);
            if (!clipReport.animatorStateAvailable)
            {
                FailAnimatorStateBinding(
                    clipReport,
                    "the controller has no state at " +
                    clipReport.animatorStatePath + ".");
                yield break;
            }

            float playbackSpeed =
                clip.length > 0.001f
                    ? clip.length / reviewDuration
                    : 1f;
            if (isWalk)
            {
                PrepareWalkReviewTravel();
                SetWalkReviewTravel(0f);
                v22WalkPresentation.SetReviewFrame(0);
            }

            ConfigureAnimatorParametersForClip(clip.name);
            animator.speed = 0f;
            clipReport.startStateEntered = PlayVerifiedAnimatorState(
                clipReport.animatorStateHash,
                0f,
                out int startStateHash);
            clipReport.startStateFullPathHash = startStateHash;
            Canvas.ForceUpdateCanvases();

            yield return null;
            yield return new WaitForEndOfFrame();
            clipReport.startStateEntered &= VerifyCurrentAnimatorState(
                clipReport.animatorStateHash,
                out startStateHash);
            clipReport.startStateFullPathHash = startStateHash;
            if (!clipReport.startStateEntered)
            {
                FailAnimatorStateBinding(
                    clipReport,
                    "the requested start state was not active after Canvas " +
                    "deformation updated.");
                yield break;
            }

            if (!CaptureClipStartPose(clipIndex))
            {
                clipReport.captured = false;
                report.error = AppendError(
                    report.error,
                    clip.name +
                    ": start-pose capture failed.");
                RestoreAnimatorAfterClip();
                yield break;
            }

            if (isWalk)
            {
                yield return ReviewWalkCycle(
                    clipReport,
                    clipIndex,
                    reviewDuration,
                    playbackSpeed);
                faceController.SetMouth(
                    Patch4FaceController.MouthPose.Closed);
                RestoreAnimatorAfterClip();
                yield break;
            }

            if (string.Equals(
                    clip.name,
                    "FatMan_Blink_Random",
                    StringComparison.Ordinal))
            {
                faceController.BlinkNow();
            }

            animator.speed = playbackSpeed;
            float elapsed = 0f;
            while (elapsed < captureAt)
            {
                elapsed += Mathf.Max(
                    0.001f,
                    Time.unscaledDeltaTime);
                yield return null;
            }

            animator.speed = 0f;
            clipReport.peakStateEntered = PlayVerifiedAnimatorState(
                clipReport.animatorStateHash,
                captureNormalizedTime,
                out int peakStateHash);
            clipReport.peakStateFullPathHash = peakStateHash;
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return new WaitForEndOfFrame();
            clipReport.peakStateEntered &= VerifyCurrentAnimatorState(
                clipReport.animatorStateHash,
                out peakStateHash);
            clipReport.peakStateFullPathHash = peakStateHash;
            clipReport.animatorStateBindingPassed =
                clipReport.startStateEntered &&
                clipReport.peakStateEntered;
            if (!clipReport.animatorStateBindingPassed)
            {
                FailAnimatorStateBinding(
                    clipReport,
                    "the requested peak state was not active at normalized " +
                    captureNormalizedTime.ToString("0.000") + ".");
                yield break;
            }

            clipReport.captured = CaptureCurrentRoomFrame(
                clipIndex,
                clipReport);
            animator.speed = playbackSpeed;
            while (elapsed < reviewDuration)
            {
                elapsed += Mathf.Max(
                    0.001f,
                    Time.unscaledDeltaTime);
                yield return null;
            }

            faceController.SetMouth(
                Patch4FaceController.MouthPose.Closed);
            RestoreAnimatorAfterClip();
        }

        private IEnumerator ReviewWalkCycle(
            ClipReview clipReport,
            int clipIndex,
            float reviewDuration,
            float playbackSpeed)
        {
            bool phaseFramesCaptured = true;
            bool contactFrameCaptured = false;
            float elapsed = 0f;
            float lastCaptureNormalizedTime =
                (WalkPhaseCount - 1f) / WalkPhaseCount;

            for (int phaseIndex = 0;
                 phaseIndex < WalkPhaseCount;
                 phaseIndex++)
            {
                float normalizedTime =
                    phaseIndex / (float)WalkPhaseCount;
                float captureAt = reviewDuration * normalizedTime;
                animator.speed = playbackSpeed;
                while (elapsed < captureAt)
                {
                    elapsed += Mathf.Max(
                        0.001f,
                        Time.unscaledDeltaTime);
                    float liveNormalizedTime = Mathf.Clamp01(
                        elapsed / Mathf.Max(0.001f, reviewDuration));
                    SetWalkReviewTravel(
                        liveNormalizedTime /
                        lastCaptureNormalizedTime);
                    yield return null;
                }

                animator.speed = 0f;
                float travelProgress =
                    phaseIndex / (float)(WalkPhaseCount - 1);
                SetWalkReviewTravel(travelProgress);

                bool stateEntered = PlayVerifiedAnimatorState(
                    clipReport.animatorStateHash,
                    normalizedTime,
                    out int stateHash);
                v22WalkPresentation.SetReviewFrame(phaseIndex);
                Canvas.ForceUpdateCanvases();
                yield return null;
                yield return new WaitForEndOfFrame();
                stateEntered &= VerifyCurrentAnimatorState(
                    clipReport.animatorStateHash,
                    out stateHash);
                if (!stateEntered)
                {
                    FailAnimatorStateBinding(
                        clipReport,
                        "the walk state was lost while sampling phase " +
                        (phaseIndex + 1) + " of " + WalkPhaseCount + ".");
                    yield break;
                }

                phaseFramesCaptured &=
                    v22WalkPresentation.IsDisplaying &&
                    v22WalkPresentation.ActiveFrameIndex == phaseIndex &&
                    CaptureWalkPhaseFrame(phaseIndex);
                Rect currentRect = ResolveExpectedScreenRect(
                    backgroundWidth,
                    backgroundHeight);
                walkPhaseRootScreenX[phaseIndex] = currentRect.center.x;

                if (phaseIndex == WalkContactPhaseIndex)
                {
                    clipReport.peakStateEntered = true;
                    clipReport.peakStateFullPathHash = stateHash;
                    contactFrameCaptured = CaptureCurrentRoomFrame(
                        clipIndex,
                        clipReport);
                }
            }

            // Let the final planted foot settle back to the loop seam while
            // the character remains at the review destination. This makes the
            // live Game view show one continuous step cycle rather than eight
            // disconnected diagnostic poses.
            animator.speed = playbackSpeed;
            while (elapsed < reviewDuration)
            {
                elapsed += Mathf.Max(
                    0.001f,
                    Time.unscaledDeltaTime);
                SetWalkReviewTravel(1f);
                yield return null;
            }
            animator.speed = 0f;

            clipReport.animatorStateBindingPassed =
                clipReport.startStateEntered &&
                clipReport.peakStateEntered;
            clipReport.walkPhaseCount = WalkPhaseCount;
            clipReport.walkCycleCaptured =
                clipReport.v22FrameSequenceReady &&
                clipReport.v22FrameSequenceUsed &&
                phaseFramesCaptured;
            AnalyzeWalkSequence(clipReport);
            clipReport.captured =
                contactFrameCaptured &&
                clipReport.walkCycleCaptured;
            clipReport.visibleMotionPassed &=
                clipReport.walkRootTravelPassed &&
                clipReport.walkPhaseAlternationPassed;
        }

        private void AnalyzeWalkSequence(ClipReview clipReport)
        {
            clipReport.walkRootTravelPixels = Mathf.Abs(
                walkPhaseRootScreenX[WalkPhaseCount - 1] -
                walkPhaseRootScreenX[0]);
            clipReport.minimumWalkRootTravelPixels = Mathf.Max(
                48f,
                neutralSilhouetteWidth * MinimumWalkTravelWidthRatio);
            bool monotonicTravel = true;
            for (int i = 1; i < WalkPhaseCount; i++)
            {
                monotonicTravel &=
                    walkPhaseRootScreenX[i] >=
                    walkPhaseRootScreenX[i - 1] - 0.5f;
            }

            clipReport.walkRootTravelPassed =
                monotonicTravel &&
                clipReport.walkRootTravelPixels >=
                clipReport.minimumWalkRootTravelPixels;

            bool sourceArticulationMeasured =
                v22WalkPresentation.TryMeasureGaitArticulation(
                    out clipReport.v22LeftArmSilhouetteDifference,
                    out clipReport.v22RightArmSilhouetteDifference,
                    out clipReport.v22LeftLegSilhouetteDifference,
                    out clipReport.v22RightLegSilhouetteDifference,
                    out clipReport.v22MinimumAdjacentFrameDifference);
            clipReport.walkPhaseAlternationPassed =
                sourceArticulationMeasured &&
                clipReport.v22LeftArmSilhouetteDifference >=
                    MinimumV22WalkArmSilhouetteDifference &&
                clipReport.v22RightArmSilhouetteDifference >=
                    MinimumV22WalkArmSilhouetteDifference &&
                clipReport.v22LeftLegSilhouetteDifference >=
                    MinimumV22WalkLegSilhouetteDifference &&
                clipReport.v22RightLegSilhouetteDifference >=
                    MinimumV22WalkLegSilhouetteDifference &&
                clipReport.v22MinimumAdjacentFrameDifference >=
                    MinimumV22AdjacentFrameDifference;

            if (!clipReport.walkRootTravelPassed)
            {
                report.error = AppendError(
                    report.error,
                    clipReport.clipName +
                    ": the eight-phase walk stayed in place (" +
                    clipReport.walkRootTravelPixels.ToString("0.0") +
                    " px, minimum " +
                    clipReport.minimumWalkRootTravelPixels.ToString("0.0") +
                    " px with monotonic room travel).");
            }

            if (!clipReport.walkPhaseAlternationPassed)
            {
                report.error = AppendError(
                    report.error,
                    clipReport.clipName +
                    ": the visible V22 frames do not contain a complete " +
                    "articulated gait (left/right arm silhouette difference " +
                    clipReport.v22LeftArmSilhouetteDifference.ToString("0.000") +
                    "/" +
                    clipReport.v22RightArmSilhouetteDifference.ToString("0.000") +
                    ", minimum " +
                    MinimumV22WalkArmSilhouetteDifference.ToString("0.000") +
                    "; left/right leg silhouette difference " +
                    clipReport.v22LeftLegSilhouetteDifference.ToString("0.000") +
                    "/" +
                    clipReport.v22RightLegSilhouetteDifference.ToString("0.000") +
                    ", minimum " +
                    MinimumV22WalkLegSilhouetteDifference.ToString("0.000") +
                    "; weakest adjacent-frame difference " +
                    clipReport.v22MinimumAdjacentFrameDifference.ToString(
                        "0.000") +
                    ", minimum " +
                    MinimumV22AdjacentFrameDifference.ToString("0.000") +
                    "). Repeated poses or body-only twitch cannot pass.");
            }
        }

        private string ResolveAnimatorStatePath(string clipName)
        {
            return animator.GetLayerName(0) + "." + clipName;
        }

        private void ConfigureAnimatorParametersForClip(string clipName)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        bool enabled =
                            (string.Equals(
                                 parameter.name,
                                 "Look",
                                 StringComparison.Ordinal) &&
                             string.Equals(
                                 clipName,
                                 "FatMan_LookAround",
                                 StringComparison.Ordinal)) ||
                            (string.Equals(
                                 parameter.name,
                                 "Sit",
                                 StringComparison.Ordinal) &&
                             string.Equals(
                                 clipName,
                                 "FatMan_SitOrLean",
                                 StringComparison.Ordinal));
                        animator.SetBool(parameter.nameHash, enabled);
                        break;
                    case AnimatorControllerParameterType.Float:
                        float value =
                            string.Equals(
                                parameter.name,
                                "Speed",
                                StringComparison.Ordinal) &&
                            string.Equals(
                                clipName,
                                "FatMan_Walk_InRoom",
                                StringComparison.Ordinal)
                                ? 1f
                                : 0f;
                        animator.SetFloat(parameter.nameHash, value);
                        break;
                    case AnimatorControllerParameterType.Int:
                        animator.SetInteger(parameter.nameHash, 0);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        animator.ResetTrigger(parameter.nameHash);
                        break;
                }
            }
        }

        private bool PlayVerifiedAnimatorState(
            int stateHash,
            float normalizedTime,
            out int currentFullPathHash)
        {
            animator.Play(
                stateHash,
                0,
                Mathf.Clamp01(normalizedTime));
            animator.Update(0f);
            return VerifyCurrentAnimatorState(
                stateHash,
                out currentFullPathHash);
        }

        private bool VerifyCurrentAnimatorState(
            int expectedStateHash,
            out int currentFullPathHash)
        {
            AnimatorStateInfo current =
                animator.GetCurrentAnimatorStateInfo(0);
            currentFullPathHash = current.fullPathHash;
            return currentFullPathHash == expectedStateHash &&
                   !animator.IsInTransition(0);
        }

        private void FailAnimatorStateBinding(
            ClipReview clipReport,
            string reason)
        {
            clipReport.animatorStateBindingPassed = false;
            clipReport.visibleMotionPassed = false;
            clipReport.captured = false;
            report.error = AppendError(
                report.error,
                clipReport.clipName +
                ": Animator state binding failed: " +
                reason);
            RestoreAnimatorAfterClip();
        }

        private void RestoreAnimatorAfterClip()
        {
            RestoreWalkReviewTravel();
            if (v22WalkPresentation != null)
            {
                v22WalkPresentation.SetReviewActive(false);
            }

            if (animator != null)
            {
                ConfigureAnimatorParametersForClip(string.Empty);
                animator.speed = 1f;
            }

            if (faceController != null)
            {
                faceController.SetMouth(
                    Patch4FaceController.MouthPose.Closed);
            }
        }

        private bool CaptureClipStartPose(int clipIndex)
        {
            Texture2D screenshot = null;
            try
            {
                screenshot =
                    ScreenCapture.CaptureScreenshotAsTexture(1);
                if (screenshot == null ||
                    screenshot.width != backgroundWidth ||
                    screenshot.height != backgroundHeight)
                {
                    return false;
                }

                clipStartPixels = screenshot.GetPixels32();
                if (clipStartPixels.Length !=
                    screenshot.width * screenshot.height)
                {
                    return false;
                }

                if (clipIndex != 0)
                {
                    return neutralReferenceCaptured;
                }

                neutralExpectedScreenRect = ResolveExpectedScreenRect(
                    screenshot.width,
                    screenshot.height);
                if (!MeasureSilhouette(
                        clipStartPixels,
                        screenshot.width,
                        screenshot.height,
                        neutralExpectedScreenRect,
                        out int changed,
                        out int width,
                        out int height))
                {
                    return false;
                }

                neutralSilhouetteArea = changed;
                neutralSilhouetteWidth = width;
                neutralSilhouetteHeight = height;
                neutralReferenceCaptured =
                    neutralSilhouetteArea > 0 &&
                    neutralSilhouetteWidth > 0 &&
                    neutralSilhouetteHeight > 0;
                return neutralReferenceCaptured;
            }
            catch (Exception exception)
            {
                report.error = AppendError(
                    report.error,
                    currentClip +
                    " start pose: " +
                    exception.Message);
                return false;
            }
            finally
            {
                if (screenshot != null)
                {
                    Destroy(screenshot);
                }
            }
        }

        private static float ResolveCaptureNormalizedTime(
            string clipName)
        {
            switch (clipName)
            {
                case "FatMan_Idle_ShiftWeight":
                case "FatMan_LookAround":
                case "FatMan_Walk_InRoom":
                    return 0.25f;
                case "FatMan_TapReact_01":
                case "FatMan_TapReact_02":
                    return 0.22f;
                case "FatMan_Turn":
                    return 0.58f;
                case "FatMan_SitOrLean":
                    return 0.56f;
                case "FatMan_UpgradeReact":
                    return 0.2f;
                default:
                    return 0.5f;
            }
        }

        private static float ResolveMinimumMotionCoverage(
            string clipName)
        {
            switch (clipName)
            {
                case "FatMan_Idle_Breathe":
                    return 0.004f;
                case "FatMan_Blink_Random":
                    // Blink is a deliberately small face-only replacement.
                    // It must still alter the character and also pass the much
                    // stricter focused face-region test below.
                    return 0.0005f;
                case "FatMan_LookAround":
                    return 0.008f;
                case "FatMan_Idle_ShiftWeight":
                    return 0.015f;
                case "FatMan_TapReact_01":
                case "FatMan_TapReact_02":
                case "FatMan_SitOrLean":
                    return 0.02f;
                case "FatMan_Walk_InRoom":
                case "FatMan_Turn":
                    return 0.025f;
                case "FatMan_UpgradeReact":
                    return 0.03f;
                default:
                    return 0.01f;
            }
        }

        private string ValidateSetup()
        {
            if (rigController == null ||
                visibilityGuard == null ||
                canvasPresentation == null ||
                faceController == null ||
                secondaryMotion == null ||
                v22WalkPresentation == null ||
                !v22WalkPresentation.IsReady ||
                v22WalkPresentation.FrameCount != WalkPhaseCount ||
                animator == null ||
                animator.runtimeAnimatorController == null ||
                patch4VisualRoot == null ||
                patch35RollbackRoot == null ||
                string.IsNullOrWhiteSpace(reviewRunToken))
            {
                return "The locked room-review binding is incomplete.";
            }

            if (!report.actualLivingGameplayRoom)
            {
                return "LivingGameplayScene was not created.";
            }

            if (!canvasPresentation.IsCanvasReady ||
                !canvasPresentation.SkinBindingsReady ||
                !canvasPresentation.BindAnchorsFrozen ||
                canvasPresentation.SkinDeformerCount !=
                Patch4RigContract.RequiredLayerPaths.Count ||
                canvasPresentation.WeightedLayerCount < 1 ||
                !canvasPresentation.ContinuousBodyBindingReady ||
                !canvasPresentation.RuntimeRigidBindingsReady)
            {
                return "The continuous full-body Canvas deformation surface " +
                    "or its frozen bind anchors are incomplete.";
            }

            if (rigController.Patch4Enabled ||
                rigController.IsArtApproved)
            {
                return "The production readiness gate is not safely locked.";
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                return "The animation review report directory is missing.";
            }

            return string.Empty;
        }

        private Dictionary<string, AnimationClip> ResolveRequiredClips()
        {
            Dictionary<string, AnimationClip> result =
                new(StringComparer.Ordinal);
            AnimationClip[] available =
                animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < available.Length; i++)
            {
                AnimationClip clip = available[i];
                if (clip != null &&
                    !result.ContainsKey(clip.name))
                {
                    result.Add(clip.name, clip);
                }
            }

            for (int i = 0;
                 i < Patch4RigContract.RequiredClipNames.Count;
                 i++)
            {
                string required =
                    Patch4RigContract.RequiredClipNames[i];
                if (!result.ContainsKey(required))
                {
                    return new Dictionary<string, AnimationClip>(
                        StringComparer.Ordinal);
                }
            }

            return result;
        }

        private void PrepareLockedReview()
        {
            reviewBaseLocalPosition = rigController.transform.localPosition;
            reviewBasePositionCaptured = true;
            reviewTravelLocalDistance = 0f;
            rigController.SetPatch4Enabled(false);
            visibilityGuard.enabled = false;
            patch35RollbackRoot.SetActive(true);
            rollbackReviewGroup =
                patch35RollbackRoot.GetComponent<CanvasGroup>();
            if (rollbackReviewGroup == null)
            {
                rollbackReviewGroup =
                    patch35RollbackRoot.AddComponent<CanvasGroup>();
                rollbackGroupAddedForReview = true;
            }

            rollbackGroupPreviousAlpha = rollbackReviewGroup.alpha;
            rollbackGroupPreviousInteractable =
                rollbackReviewGroup.interactable;
            rollbackGroupPreviousBlocksRaycasts =
                rollbackReviewGroup.blocksRaycasts;
            rollbackReviewGroup.alpha = 0f;
            rollbackReviewGroup.interactable = false;
            rollbackReviewGroup.blocksRaycasts = false;
            report.legacyRigStayedLogicallyActive =
                patch35RollbackRoot.activeInHierarchy;
            PauseLegacyMotionAndFootsteps();
            patch4VisualRoot.SetActive(true);
            v22WalkPresentation.SetReviewActive(false);
            faceController.SetEditorReviewActive(true);
            secondaryMotion.SetEditorReviewActive(true);
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Canvas.ForceUpdateCanvases();
        }

        private void PrepareWalkReviewTravel()
        {
            Transform target =
                rigController != null ? rigController.transform : null;
            Transform parent = target != null ? target.parent : null;
            if (target == null || parent == null)
            {
                reviewTravelLocalDistance = 0f;
                return;
            }

            Canvas canvas =
                canvasPresentation != null
                    ? canvasPresentation.HostCanvas
                    : null;
            Camera camera =
                canvas != null &&
                canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;
            Vector2 screenOrigin =
                RectTransformUtility.WorldToScreenPoint(
                    camera,
                    parent.TransformPoint(Vector3.zero));
            Vector2 screenUnit =
                RectTransformUtility.WorldToScreenPoint(
                    camera,
                    parent.TransformPoint(Vector3.right));
            float pixelsPerLocalUnit = Mathf.Abs(
                screenUnit.x - screenOrigin.x);
            if (pixelsPerLocalUnit < 0.001f)
            {
                pixelsPerLocalUnit = 1f;
            }

            float targetTravelPixels = Mathf.Clamp(
                neutralSilhouetteWidth * 0.72f,
                64f,
                180f);
            reviewTravelLocalDistance =
                targetTravelPixels / pixelsPerLocalUnit;
        }

        private void SetWalkReviewTravel(float progress)
        {
            if (rigController == null || !reviewBasePositionCaptured)
            {
                return;
            }

            Vector3 position = reviewBaseLocalPosition;
            position.x += reviewTravelLocalDistance *
                Mathf.Clamp01(progress);
            rigController.transform.localPosition = position;
        }

        private void RestoreWalkReviewTravel()
        {
            if (rigController != null && reviewBasePositionCaptured)
            {
                rigController.transform.localPosition =
                    reviewBaseLocalPosition;
            }
        }

        private void PauseLegacyMotionAndFootsteps()
        {
            Transform legacyRoot =
                rigController != null
                    ? rigController.transform.parent
                    : null;
            legacyRoutine =
                legacyRoot != null
                    ? legacyRoot.GetComponent<CharacterRoutineController>()
                    : null;
            legacyRoutineWasEnabled =
                legacyRoutine != null &&
                legacyRoutine.enabled;
            if (legacyRoutineWasEnabled)
            {
                legacyRoutine.enabled = false;
            }

            report.legacyRoutinePausedForReview =
                legacyRoutine == null ||
                !legacyRoutine.enabled;

            legacySignalBridge =
                rigController != null
                    ? rigController.GetComponent<
                        Patch4LegacySignalBridge>()
                    : null;
            legacySignalBridgeWasEnabled =
                legacySignalBridge != null &&
                legacySignalBridge.enabled;
            if (legacySignalBridgeWasEnabled)
            {
                legacySignalBridge.enabled = false;
            }

            report.legacySignalBridgePausedForReview =
                legacySignalBridge == null ||
                !legacySignalBridge.enabled;

            GameplayAudioController gameplayAudio =
                legacyRoot != null
                    ? legacyRoot.GetComponentInParent<
                        GameplayAudioController>()
                    : null;
            if (gameplayAudio == null)
            {
                report.legacyOneShotAudioStopped = true;
                return;
            }

            AudioSource[] sources =
                gameplayAudio.GetComponents<AudioSource>();
            bool foundOneShot = false;
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null || source.loop)
                {
                    continue;
                }

                foundOneShot = true;
                source.Stop();
            }

            report.legacyOneShotAudioStopped =
                !foundOneShot ||
                Array.TrueForAll(
                    sources,
                    source =>
                        source == null ||
                        source.loop ||
                        !source.isPlaying);
        }

        private void ConfigureFaceForClip(string clipName)
        {
            Patch4FaceController.MouthPose pose =
                Patch4FaceController.MouthPose.Closed;
            if (string.Equals(
                    clipName,
                    "FatMan_TapReact_01",
                    StringComparison.Ordinal) ||
                string.Equals(
                    clipName,
                    "FatMan_TapReact_02",
                    StringComparison.Ordinal))
            {
                pose = Patch4FaceController.MouthPose.Open;
            }
            else if (string.Equals(
                clipName,
                "FatMan_UpgradeReact",
                StringComparison.Ordinal))
            {
                pose = Patch4FaceController.MouthPose.Smile;
            }

            faceController.SetMouth(pose);
        }

        private void InitializeContactSheet()
        {
            contactSheet = new Texture2D(
                ContactColumns * ThumbnailWidth,
                ContactRows * ThumbnailHeight,
                TextureFormat.RGBA32,
                false,
                false)
            {
                name = "Patch4AnimationRoomReview",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] background =
                new Color32[contactSheet.width * contactSheet.height];
            Color32 fill = new(20, 24, 30, 255);
            for (int i = 0; i < background.Length; i++)
            {
                background[i] = fill;
            }

            contactSheet.SetPixels32(background);
        }

        private void InitializeWalkCycleSheet()
        {
            walkCycleSheet = new Texture2D(
                WalkPhaseCount * WalkThumbnailWidth,
                WalkThumbnailHeight,
                TextureFormat.RGBA32,
                false,
                false)
            {
                name = "Patch4WalkCycleReview",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] background = new Color32[
                walkCycleSheet.width * walkCycleSheet.height];
            Color32 fill = new(20, 24, 30, 255);
            for (int i = 0; i < background.Length; i++)
            {
                background[i] = fill;
            }

            walkCycleSheet.SetPixels32(background);
        }

        private bool CaptureWalkPhaseFrame(int phaseIndex)
        {
            Texture2D screenshot = null;
            Texture2D thumbnail = null;
            try
            {
                if (walkCycleSheet == null ||
                    phaseIndex < 0 ||
                    phaseIndex >= WalkPhaseCount)
                {
                    return false;
                }

                screenshot = ScreenCapture.CaptureScreenshotAsTexture(1);
                if (screenshot == null)
                {
                    return false;
                }

                thumbnail = BuildThumbnail(
                    screenshot,
                    WalkThumbnailWidth,
                    WalkThumbnailHeight);
                if (thumbnail == null)
                {
                    return false;
                }

                walkCycleSheet.SetPixels32(
                    phaseIndex * WalkThumbnailWidth,
                    0,
                    WalkThumbnailWidth,
                    WalkThumbnailHeight,
                    thumbnail.GetPixels32());
                return true;
            }
            catch (Exception exception)
            {
                report.error = AppendError(
                    report.error,
                    "Walk phase " + (phaseIndex + 1) + ": " +
                    exception.Message);
                return false;
            }
            finally
            {
                if (screenshot != null)
                {
                    Destroy(screenshot);
                }

                if (thumbnail != null)
                {
                    Destroy(thumbnail);
                }
            }
        }

        private bool CaptureReviewBackground()
        {
            Texture2D screenshot = null;
            try
            {
                screenshot =
                    ScreenCapture.CaptureScreenshotAsTexture(1);
                if (screenshot == null)
                {
                    return false;
                }

                backgroundWidth = screenshot.width;
                backgroundHeight = screenshot.height;
                backgroundPixels = screenshot.GetPixels32();
                return backgroundPixels.Length ==
                    backgroundWidth * backgroundHeight;
            }
            catch (Exception exception)
            {
                report.error = AppendError(
                    report.error,
                    "Background capture: " + exception.Message);
                return false;
            }
            finally
            {
                if (screenshot != null)
                {
                    Destroy(screenshot);
                }
            }
        }

        private bool CaptureCurrentRoomFrame(
            int clipIndex,
            ClipReview clipReport)
        {
            Texture2D screenshot = null;
            Texture2D thumbnail = null;
            try
            {
                screenshot =
                    ScreenCapture.CaptureScreenshotAsTexture(1);
                if (screenshot == null)
                {
                    return false;
                }

                clipReport.visualSanityPassed =
                    AnalyzeRoomSilhouette(
                        screenshot,
                        clipReport);
                clipReport.visibleMotionPassed =
                    AnalyzeVisibleMotion(
                        screenshot,
                        clipReport);
                thumbnail = BuildThumbnail(screenshot);
                if (thumbnail == null)
                {
                    return false;
                }

                int column = clipIndex % ContactColumns;
                int row = clipIndex / ContactColumns;
                int destinationY =
                    (ContactRows - 1 - row) * ThumbnailHeight;
                contactSheet.SetPixels32(
                    column * ThumbnailWidth,
                    destinationY,
                    ThumbnailWidth,
                    ThumbnailHeight,
                    thumbnail.GetPixels32());
                return true;
            }
            catch (Exception exception)
            {
                report.error = AppendError(
                    report.error,
                    currentClip + ": " + exception.Message);
                return false;
            }
            finally
            {
                if (screenshot != null)
                {
                    Destroy(screenshot);
                }

                if (thumbnail != null)
                {
                    Destroy(thumbnail);
                }
            }
        }

        private bool AnalyzeRoomSilhouette(
            Texture2D screenshot,
            ClipReview clipReport)
        {
            if (screenshot == null ||
                backgroundPixels == null ||
                backgroundPixels.Length == 0 ||
                screenshot.width != backgroundWidth ||
                screenshot.height != backgroundHeight ||
                canvasPresentation == null ||
                canvasPresentation.GeneratedRoot == null)
            {
                return false;
            }

            Color32[] current = screenshot.GetPixels32();
            // The walk review intentionally advances the generated character
            // through the room. Measure the silhouette at its current Canvas
            // bounds; using the neutral start rectangle would crop legitimate
            // travel and could turn locomotion into a false collapse.
            Rect expected = ResolveExpectedScreenRect(
                screenshot.width,
                screenshot.height);
            if (!MeasureSilhouette(
                    current,
                    screenshot.width,
                    screenshot.height,
                    expected,
                    out int changed,
                    out int silhouetteWidth,
                    out int silhouetteHeight))
            {
                return false;
            }

            int expectedWidth = Mathf.Max(
                1,
                Mathf.CeilToInt(expected.width));
            int expectedHeight = Mathf.Max(
                1,
                Mathf.CeilToInt(expected.height));
            clipReport.changedPixelCount = changed;
            clipReport.silhouetteWidth = silhouetteWidth;
            clipReport.silhouetteHeight = silhouetteHeight;
            clipReport.widthCoverage =
                silhouetteWidth / (float)expectedWidth;
            clipReport.heightCoverage =
                silhouetteHeight / (float)expectedHeight;
            clipReport.areaCoverage =
                changed / (float)(expectedWidth * expectedHeight);
            clipReport.neutralWidthRetention =
                neutralSilhouetteWidth > 0
                    ? silhouetteWidth /
                      (float)neutralSilhouetteWidth
                    : 0f;
            clipReport.neutralHeightRetention =
                neutralSilhouetteHeight > 0
                    ? silhouetteHeight /
                      (float)neutralSilhouetteHeight
                    : 0f;
            clipReport.neutralAreaRetention =
                neutralSilhouetteArea > 0
                    ? changed /
                      (float)neutralSilhouetteArea
                    : 0f;

            bool sane =
                clipReport.widthCoverage >=
                    MinimumSilhouetteWidthCoverage &&
                clipReport.heightCoverage >=
                    MinimumSilhouetteHeightCoverage &&
                clipReport.areaCoverage >=
                    MinimumSilhouetteAreaCoverage &&
                clipReport.neutralWidthRetention >=
                    MinimumNeutralWidthRetention &&
                clipReport.neutralWidthRetention <=
                    MaximumNeutralWidthExpansion &&
                clipReport.neutralHeightRetention >=
                    MinimumNeutralHeightRetention &&
                clipReport.neutralHeightRetention <=
                    MaximumNeutralHeightExpansion &&
                clipReport.neutralAreaRetention >=
                    MinimumNeutralAreaRetention &&
                clipReport.neutralAreaRetention <=
                    MaximumNeutralAreaExpansion;
            if (!sane)
            {
                report.error = AppendError(
                    report.error,
                    clipReport.clipName +
                    ": collapsed or over-stretched room silhouette (width " +
                    clipReport.widthCoverage.ToString("0.000") +
                    ", height " +
                    clipReport.heightCoverage.ToString("0.000") +
                    ", area " +
                    clipReport.areaCoverage.ToString("0.000") +
                    "; neutral retention " +
                    clipReport.neutralWidthRetention.ToString("0.000") +
                    " × " +
                    clipReport.neutralHeightRetention.ToString("0.000") +
                    ", area " +
                    clipReport.neutralAreaRetention.ToString("0.000") +
                    ").");
            }

            return sane;
        }

        private bool MeasureSilhouette(
            IReadOnlyList<Color32> current,
            int screenWidth,
            int screenHeight,
            Rect expected,
            out int changed,
            out int silhouetteWidth,
            out int silhouetteHeight)
        {
            changed = 0;
            silhouetteWidth = 0;
            silhouetteHeight = 0;
            if (current == null ||
                current.Count != screenWidth * screenHeight ||
                backgroundPixels == null ||
                backgroundPixels.Length != current.Count)
            {
                return false;
            }

            int xMin = Mathf.Clamp(
                Mathf.FloorToInt(expected.xMin),
                0,
                screenWidth - 1);
            int xMax = Mathf.Clamp(
                Mathf.CeilToInt(expected.xMax),
                xMin + 1,
                screenWidth);
            int yMin = Mathf.Clamp(
                Mathf.FloorToInt(expected.yMin),
                0,
                screenHeight - 1);
            int yMax = Mathf.Clamp(
                Mathf.CeilToInt(expected.yMax),
                yMin + 1,
                screenHeight);
            int changedXMin = xMax;
            int changedXMax = xMin;
            int changedYMin = yMax;
            int changedYMax = yMin;

            for (int y = yMin; y < yMax; y++)
            {
                int row = y * screenWidth;
                for (int x = xMin; x < xMax; x++)
                {
                    int index = row + x;
                    Color32 before = backgroundPixels[index];
                    Color32 after = current[index];
                    int delta =
                        Math.Abs(after.r - before.r) +
                        Math.Abs(after.g - before.g) +
                        Math.Abs(after.b - before.b);
                    if (delta < PixelDifferenceThreshold)
                    {
                        continue;
                    }

                    changed++;
                    changedXMin = Mathf.Min(changedXMin, x);
                    changedXMax = Mathf.Max(changedXMax, x);
                    changedYMin = Mathf.Min(changedYMin, y);
                    changedYMax = Mathf.Max(changedYMax, y);
                }
            }

            silhouetteWidth =
                changed > 0
                    ? changedXMax - changedXMin + 1
                    : 0;
            silhouetteHeight =
                changed > 0
                    ? changedYMax - changedYMin + 1
                    : 0;
            return changed > 0;
        }

        private bool AnalyzeVisibleMotion(
            Texture2D screenshot,
            ClipReview clipReport)
        {
            if (screenshot == null ||
                clipStartPixels == null ||
                clipStartPixels.Length !=
                    screenshot.width * screenshot.height ||
                !neutralReferenceCaptured ||
                neutralSilhouetteArea <= 0)
            {
                return false;
            }

            Color32[] current = screenshot.GetPixels32();
            int xMin = Mathf.Clamp(
                Mathf.FloorToInt(neutralExpectedScreenRect.xMin),
                0,
                screenshot.width - 1);
            int xMax = Mathf.Clamp(
                Mathf.CeilToInt(neutralExpectedScreenRect.xMax),
                xMin + 1,
                screenshot.width);
            int yMin = Mathf.Clamp(
                Mathf.FloorToInt(neutralExpectedScreenRect.yMin),
                0,
                screenshot.height - 1);
            int yMax = Mathf.Clamp(
                Mathf.CeilToInt(neutralExpectedScreenRect.yMax),
                yMin + 1,
                screenshot.height);
            int changed = 0;
            ResolveGlobalAlignment(
                current,
                screenshot.width,
                screenshot.height,
                out int alignmentX,
                out int alignmentY);
            clipReport.globalAlignmentX = alignmentX;
            clipReport.globalAlignmentY = alignmentY;

            for (int y = yMin; y < yMax; y++)
            {
                int row = y * screenshot.width;
                for (int x = xMin; x < xMax; x++)
                {
                    int index = row + x;
                    Color32 before = clipStartPixels[index];
                    Color32 clean = backgroundPixels[index];
                    int foregroundDelta =
                        Math.Abs(before.r - clean.r) +
                        Math.Abs(before.g - clean.g) +
                        Math.Abs(before.b - clean.b);
                    if (foregroundDelta < PixelDifferenceThreshold)
                    {
                        continue;
                    }

                    int alignedX = Mathf.Clamp(
                        x + alignmentX,
                        0,
                        screenshot.width - 1);
                    int alignedY = Mathf.Clamp(
                        y + alignmentY,
                        0,
                        screenshot.height - 1);
                    Color32 after = current[
                        alignedY * screenshot.width + alignedX];
                    int delta =
                        Math.Abs(after.r - before.r) +
                        Math.Abs(after.g - before.g) +
                        Math.Abs(after.b - before.b);
                    if (delta >= MotionPixelDifferenceThreshold)
                    {
                        changed++;
                    }
                }
            }

            clipReport.motionChangedPixelCount = changed;
            clipReport.motionCoverage =
                changed / (float)neutralSilhouetteArea;
            bool fullCharacterPassed =
                clipReport.motionCoverage >=
                clipReport.minimumMotionCoverage;
            if (!fullCharacterPassed)
            {
                report.error = AppendError(
                    report.error,
                    clipReport.clipName +
                    ": visible motion is too small (" +
                    clipReport.motionCoverage.ToString("0.000") +
                    ", minimum " +
                    clipReport.minimumMotionCoverage.ToString("0.000") +
                    ").");
            }

            bool focusedFacePassed = true;
            if (string.Equals(
                clipReport.clipName,
                "FatMan_Blink_Random",
                StringComparison.Ordinal))
            {
                focusedFacePassed = AnalyzeFocusedFaceMotion(
                    current,
                    screenshot.width,
                    screenshot.height,
                    clipReport);
            }
            else
            {
                clipReport.focusedFaceMotionRequired = false;
                clipReport.focusedFaceMotionPassed = true;
            }

            bool limbArticulationPassed = true;
            if (string.Equals(
                clipReport.clipName,
                "FatMan_Walk_InRoom",
                StringComparison.Ordinal))
            {
                limbArticulationPassed = AnalyzeWalkLimbMotion(
                    current,
                    screenshot.width,
                    screenshot.height,
                    clipReport);
            }
            else
            {
                clipReport.limbArticulationRequired = false;
                clipReport.limbArticulationPassed = true;
            }

            return fullCharacterPassed &&
                   focusedFacePassed &&
                   limbArticulationPassed;
        }

        private bool AnalyzeWalkLimbMotion(
            IReadOnlyList<Color32> current,
            int screenWidth,
            int screenHeight,
            ClipReview clipReport)
        {
            clipReport.limbArticulationRequired = true;
            clipReport.minimumLimbMotionCoverage =
                MinimumWalkLimbMotionCoverage;
            if (current == null ||
                current.Count != screenWidth * screenHeight ||
                clipStartPixels == null ||
                clipStartPixels.Length != current.Count ||
                backgroundPixels == null ||
                backgroundPixels.Length != current.Count)
            {
                clipReport.limbArticulationPassed = false;
                return false;
            }

            Rect expected = neutralExpectedScreenRect;
            bool leftArmMeasured = MeasureAlignedRegionMotion(
                current,
                screenWidth,
                screenHeight,
                expected,
                .205f,
                .348f,
                .245f,
                .555f,
                clipReport.globalAlignmentX,
                clipReport.globalAlignmentY,
                out int leftArmChanged,
                out int leftArmReference,
                out float leftArmCoverage);
            bool rightArmMeasured = MeasureAlignedRegionMotion(
                current,
                screenWidth,
                screenHeight,
                expected,
                .652f,
                .795f,
                .245f,
                .555f,
                clipReport.globalAlignmentX,
                clipReport.globalAlignmentY,
                out int rightArmChanged,
                out int rightArmReference,
                out float rightArmCoverage);
            bool leftLegMeasured = MeasureAlignedRegionMotion(
                current,
                screenWidth,
                screenHeight,
                expected,
                .285f,
                .495f,
                .535f,
                .815f,
                clipReport.globalAlignmentX,
                clipReport.globalAlignmentY,
                out int leftLegChanged,
                out int leftLegReference,
                out float leftLegCoverage);
            bool rightLegMeasured = MeasureAlignedRegionMotion(
                current,
                screenWidth,
                screenHeight,
                expected,
                .505f,
                .715f,
                .535f,
                .815f,
                clipReport.globalAlignmentX,
                clipReport.globalAlignmentY,
                out int rightLegChanged,
                out int rightLegReference,
                out float rightLegCoverage);

            int changed =
                leftArmChanged +
                rightArmChanged +
                leftLegChanged +
                rightLegChanged;
            int reference =
                leftArmReference +
                rightArmReference +
                leftLegReference +
                rightLegReference;
            clipReport.leftArmMotionCoverage = leftArmCoverage;
            clipReport.rightArmMotionCoverage = rightArmCoverage;
            clipReport.leftLegMotionCoverage = leftLegCoverage;
            clipReport.rightLegMotionCoverage = rightLegCoverage;

            clipReport.limbMotionChangedPixelCount = changed;
            clipReport.limbReferencePixelCount = reference;
            clipReport.limbMotionCoverage =
                reference > 0 ? changed / (float)reference : 0f;
            clipReport.allLimbRegionsPassed =
                leftArmMeasured &&
                rightArmMeasured &&
                leftLegMeasured &&
                rightLegMeasured &&
                leftArmCoverage >= MinimumWalkArmMotionCoverage &&
                rightArmCoverage >= MinimumWalkArmMotionCoverage &&
                leftLegCoverage >= MinimumWalkLegMotionCoverage &&
                rightLegCoverage >= MinimumWalkLegMotionCoverage;
            clipReport.limbArticulationPassed =
                reference > 0 &&
                clipReport.limbMotionCoverage >=
                    clipReport.minimumLimbMotionCoverage &&
                clipReport.allLimbRegionsPassed;
            if (!clipReport.limbArticulationPassed)
            {
                report.error = AppendError(
                    report.error,
                    clipReport.clipName +
                    ": arm/leg articulation is too small (" +
                    clipReport.limbMotionCoverage.ToString("0.000") +
                    ", minimum " +
                    clipReport.minimumLimbMotionCoverage.ToString("0.000") +
                    "; left/right arms " +
                    leftArmCoverage.ToString("0.000") +
                    "/" +
                    rightArmCoverage.ToString("0.000") +
                    ", left/right legs " +
                    leftLegCoverage.ToString("0.000") +
                    "/" +
                    rightLegCoverage.ToString("0.000") +
                    "). Every visible arm and leg region must change its " +
                    "silhouette; room travel, body bob and texture shimmer " +
                    "cannot pass by themselves.");
            }

            return clipReport.limbArticulationPassed;
        }

        private bool MeasureAlignedRegionMotion(
            IReadOnlyList<Color32> current,
            int screenWidth,
            int screenHeight,
            Rect expected,
            float normalizedXMin,
            float normalizedXMax,
            float topYMin,
            float topYMax,
            int alignmentX,
            int alignmentY,
            out int changed,
            out int reference,
            out float coverage)
        {
            changed = 0;
            reference = 0;
            coverage = 0f;
            int xMin = Mathf.Clamp(
                Mathf.FloorToInt(
                    expected.xMin + expected.width * normalizedXMin),
                0,
                screenWidth - 1);
            int xMax = Mathf.Clamp(
                Mathf.CeilToInt(
                    expected.xMin + expected.width * normalizedXMax),
                xMin + 1,
                screenWidth);
            int yMin = Mathf.Clamp(
                Mathf.FloorToInt(
                    expected.yMax - expected.height * topYMax),
                0,
                screenHeight - 1);
            int yMax = Mathf.Clamp(
                Mathf.CeilToInt(
                    expected.yMax - expected.height * topYMin),
                yMin + 1,
                screenHeight);

            for (int y = yMin; y < yMax; y++)
            {
                int row = y * screenWidth;
                for (int x = xMin; x < xMax; x++)
                {
                    int index = row + x;
                    reference++;
                    int alignedX = Mathf.Clamp(
                        x + alignmentX,
                        0,
                        screenWidth - 1);
                    int alignedY = Mathf.Clamp(
                        y + alignmentY,
                        0,
                        screenHeight - 1);
                    int alignedIndex =
                        alignedY * screenWidth + alignedX;
                    bool startForeground = IsForeground(
                        clipStartPixels[index],
                        backgroundPixels[index]);
                    bool afterForeground = IsForeground(
                        current[alignedIndex],
                        backgroundPixels[alignedIndex]);
                    if (!startForeground && !afterForeground)
                    {
                        reference--;
                        continue;
                    }

                    if (startForeground != afterForeground)
                    {
                        changed++;
                    }
                }
            }

            coverage = reference > 0
                ? changed / (float)reference
                : 0f;
            return reference > 0;
        }

        private static bool IsForeground(Color32 pixel, Color32 background)
        {
            int delta =
                Math.Abs(pixel.r - background.r) +
                Math.Abs(pixel.g - background.g) +
                Math.Abs(pixel.b - background.b);
            return delta >= PixelDifferenceThreshold;
        }

        private void ResolveGlobalAlignment(
            IReadOnlyList<Color32> current,
            int screenWidth,
            int screenHeight,
            out int alignmentX,
            out int alignmentY)
        {
            alignmentX = 0;
            alignmentY = 0;
            if (!TryMeasureForegroundCentroid(
                    clipStartPixels,
                    screenWidth,
                    screenHeight,
                    out Vector2 startCentroid) ||
                !TryMeasureForegroundCentroid(
                    current,
                    screenWidth,
                    screenHeight,
                    out Vector2 currentCentroid))
            {
                return;
            }

            alignmentX = Mathf.RoundToInt(
                currentCentroid.x - startCentroid.x);
            alignmentY = Mathf.RoundToInt(
                currentCentroid.y - startCentroid.y);
        }

        private bool TryMeasureForegroundCentroid(
            IReadOnlyList<Color32> pixels,
            int screenWidth,
            int screenHeight,
            out Vector2 centroid)
        {
            centroid = Vector2.zero;
            if (pixels == null ||
                pixels.Count != screenWidth * screenHeight ||
                backgroundPixels == null ||
                backgroundPixels.Length != pixels.Count)
            {
                return false;
            }

            Rect expected = neutralExpectedScreenRect;
            int xMin = Mathf.Clamp(
                Mathf.FloorToInt(expected.xMin),
                0,
                screenWidth - 1);
            int xMax = Mathf.Clamp(
                Mathf.CeilToInt(expected.xMax),
                xMin + 1,
                screenWidth);
            int yMin = Mathf.Clamp(
                Mathf.FloorToInt(expected.yMin),
                0,
                screenHeight - 1);
            int yMax = Mathf.Clamp(
                Mathf.CeilToInt(expected.yMax),
                yMin + 1,
                screenHeight);
            long sumX = 0;
            long sumY = 0;
            int count = 0;

            for (int y = yMin; y < yMax; y++)
            {
                int row = y * screenWidth;
                for (int x = xMin; x < xMax; x++)
                {
                    int index = row + x;
                    Color32 pixel = pixels[index];
                    Color32 clean = backgroundPixels[index];
                    int foregroundDelta =
                        Math.Abs(pixel.r - clean.r) +
                        Math.Abs(pixel.g - clean.g) +
                        Math.Abs(pixel.b - clean.b);
                    if (foregroundDelta < PixelDifferenceThreshold)
                    {
                        continue;
                    }

                    sumX += x;
                    sumY += y;
                    count++;
                }
            }

            if (count <= 0)
            {
                return false;
            }

            centroid = new Vector2(
                sumX / (float)count,
                sumY / (float)count);
            return true;
        }

        private bool AnalyzeFocusedFaceMotion(
            IReadOnlyList<Color32> current,
            int screenWidth,
            int screenHeight,
            ClipReview clipReport)
        {
            clipReport.focusedFaceMotionRequired = true;
            clipReport.minimumFaceMotionCoverage =
                MinimumBlinkFaceMotionCoverage;
            if (current == null ||
                current.Count != screenWidth * screenHeight ||
                clipStartPixels == null ||
                clipStartPixels.Length != current.Count ||
                backgroundPixels == null ||
                backgroundPixels.Length != current.Count)
            {
                clipReport.focusedFaceMotionPassed = false;
                return false;
            }

            Rect expected = neutralExpectedScreenRect;
            int xMin = Mathf.Clamp(
                Mathf.FloorToInt(
                    expected.xMin + expected.width * .405f),
                0,
                screenWidth - 1);
            int xMax = Mathf.Clamp(
                Mathf.CeilToInt(
                    expected.xMin + expected.width * .595f),
                xMin + 1,
                screenWidth);
            int yMin = Mathf.Clamp(
                Mathf.FloorToInt(
                    expected.yMax - expected.height * .255f),
                0,
                screenHeight - 1);
            int yMax = Mathf.Clamp(
                Mathf.CeilToInt(
                    expected.yMax - expected.height * .135f),
                yMin + 1,
                screenHeight);

            int changed = 0;
            int reference = 0;
            for (int y = yMin; y < yMax; y++)
            {
                int row = y * screenWidth;
                for (int x = xMin; x < xMax; x++)
                {
                    int index = row + x;
                    Color32 start = clipStartPixels[index];
                    Color32 clean = backgroundPixels[index];
                    int foregroundDelta =
                        Math.Abs(start.r - clean.r) +
                        Math.Abs(start.g - clean.g) +
                        Math.Abs(start.b - clean.b);
                    if (foregroundDelta >= PixelDifferenceThreshold)
                    {
                        reference++;
                    }

                    Color32 after = current[index];
                    int motionDelta =
                        Math.Abs(after.r - start.r) +
                        Math.Abs(after.g - start.g) +
                        Math.Abs(after.b - start.b);
                    if (motionDelta >= MotionPixelDifferenceThreshold)
                    {
                        changed++;
                    }
                }
            }

            clipReport.faceMotionChangedPixelCount = changed;
            clipReport.faceReferencePixelCount = reference;
            clipReport.faceMotionCoverage =
                reference > 0 ? changed / (float)reference : 0f;
            clipReport.focusedFaceMotionPassed =
                reference > 0 &&
                clipReport.faceMotionCoverage >=
                clipReport.minimumFaceMotionCoverage;
            if (!clipReport.focusedFaceMotionPassed)
            {
                report.error = AppendError(
                    report.error,
                    clipReport.clipName +
                    ": painted eyelid motion is too small inside the face " +
                    "region (" +
                    clipReport.faceMotionCoverage.ToString("0.000") +
                    ", minimum " +
                    clipReport.minimumFaceMotionCoverage.ToString("0.000") +
                    ").");
            }

            return clipReport.focusedFaceMotionPassed;
        }

        private Rect ResolveExpectedScreenRect(
            int screenWidth,
            int screenHeight)
        {
            RectTransform root =
                v22WalkPresentation != null &&
                v22WalkPresentation.IsDisplaying &&
                v22WalkPresentation.PresentationRoot != null
                    ? v22WalkPresentation.PresentationRoot
                    : canvasPresentation.GeneratedRoot;
            Canvas canvas = canvasPresentation.HostCanvas;
            Camera camera =
                canvas != null &&
                canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;
            Vector3[] corners = new Vector3[4];
            root.GetWorldCorners(corners);

            float xMin = float.PositiveInfinity;
            float xMax = float.NegativeInfinity;
            float yMin = float.PositiveInfinity;
            float yMax = float.NegativeInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 screen =
                    RectTransformUtility.WorldToScreenPoint(
                        camera,
                        corners[i]);
                xMin = Mathf.Min(xMin, screen.x);
                xMax = Mathf.Max(xMax, screen.x);
                yMin = Mathf.Min(yMin, screen.y);
                yMax = Mathf.Max(yMax, screen.y);
            }

            xMin = Mathf.Clamp(xMin, 0f, screenWidth);
            xMax = Mathf.Clamp(xMax, 0f, screenWidth);
            yMin = Mathf.Clamp(yMin, 0f, screenHeight);
            yMax = Mathf.Clamp(yMax, 0f, screenHeight);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private static Texture2D BuildThumbnail(Texture2D source)
        {
            return BuildThumbnail(
                source,
                ThumbnailWidth,
                ThumbnailHeight);
        }

        private static Texture2D BuildThumbnail(
            Texture2D source,
            int targetWidth,
            int targetHeight)
        {
            RenderTexture target = RenderTexture.GetTemporary(
                targetWidth,
                targetHeight,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            try
            {
                float sourceAspect =
                    source.width / (float)source.height;
                float targetAspect =
                    targetWidth / (float)targetHeight;
                Vector2 scale = Vector2.one;
                Vector2 offset = Vector2.zero;

                if (sourceAspect > targetAspect)
                {
                    scale.x = targetAspect / sourceAspect;
                    offset.x = (1f - scale.x) * 0.5f;
                }
                else
                {
                    scale.y = sourceAspect / targetAspect;
                    offset.y = (1f - scale.y) * 0.5f;
                }

                Graphics.Blit(source, target, scale, offset);
                RenderTexture.active = target;
                Texture2D thumbnail = new(
                    targetWidth,
                    targetHeight,
                    TextureFormat.RGBA32,
                    false,
                    false);
                thumbnail.ReadPixels(
                    new Rect(
                        0f,
                        0f,
                        targetWidth,
                        targetHeight),
                    0,
                    0,
                    false);
                thumbnail.Apply(false, false);
                return thumbnail;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private void Finish(bool passed, string failure)
        {
            CleanupLockedReview();
            StopLogCapture();
            report.completed = true;
            report.passedTechnicalChecks =
                passed &&
                report.actualLivingGameplayRoom &&
                report.allTenClipsReviewed &&
                report.canvasSkinBindingsReady &&
                report.canvasBindAnchorsFrozen &&
                report.readinessGateRemainedLocked &&
                report.patch35Restored &&
                report.legacyRigStayedLogicallyActive &&
                report.visualSanityPassed &&
                report.visibleMotionPassed &&
                report.animatorStateBindingPassed &&
                report.v22WalkFrameSequenceReady &&
                report.walkCycleCaptured &&
                report.walkRootTravelPassed &&
                report.walkPhaseAlternationPassed &&
                report.legacyRoutinePausedForReview &&
                report.legacyRoutineRestored &&
                report.legacySignalBridgePausedForReview &&
                report.legacySignalBridgeRestored &&
                report.legacyOneShotAudioStopped &&
                report.reviewConsoleErrorCount == 0;
            report.error = AppendError(report.error, failure);
            if (report.reviewConsoleErrorCount > 0)
            {
                report.error = AppendError(
                    report.error,
                    "The room review emitted " +
                    report.reviewConsoleErrorCount +
                    " Console error(s).");
            }

            Directory.CreateDirectory(outputDirectory);
            string contactPath = Path.Combine(
                outputDirectory,
                ContactSheetFileName);
            string walkCyclePath = Path.Combine(
                outputDirectory,
                WalkCycleFileName);
            string reportPath = Path.Combine(
                outputDirectory,
                ReportFileName);
            report.contactSheetPath =
                contactPath.Replace('\\', '/');
            report.walkCyclePath =
                walkCyclePath.Replace('\\', '/');

            try
            {
                if (contactSheet != null)
                {
                    contactSheet.Apply(false, false);
                    File.WriteAllBytes(
                        contactPath,
                        contactSheet.EncodeToPNG());
                }

                if (walkCycleSheet != null)
                {
                    walkCycleSheet.Apply(false, false);
                    File.WriteAllBytes(
                        walkCyclePath,
                        walkCycleSheet.EncodeToPNG());
                }

                File.WriteAllText(
                    reportPath,
                    JsonUtility.ToJson(report, true));
            }
            catch (Exception exception)
            {
                report.passedTechnicalChecks = false;
                report.error = AppendError(
                    report.error,
                    "Could not write review artifacts: " +
                    exception.Message);
            }

            ReviewFinished?.Invoke(
                report.passedTechnicalChecks,
                report.error);
        }

        private void CleanupLockedReview()
        {
            RestoreWalkReviewTravel();
            if (animator != null)
            {
                animator.speed = 1f;
            }

            if (faceController != null)
            {
                faceController.SetMouth(
                    Patch4FaceController.MouthPose.Closed);
                faceController.SetEditorReviewActive(false);
            }

            if (secondaryMotion != null)
            {
                secondaryMotion.SetEditorReviewActive(false);
            }

            if (v22WalkPresentation != null)
            {
                v22WalkPresentation.SetReviewActive(false);
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
                patch35RollbackRoot.SetActive(true);
            }

            bool rollbackGroupRestored = true;
            if (rollbackReviewGroup != null)
            {
                rollbackReviewGroup.alpha =
                    rollbackGroupPreviousAlpha;
                rollbackReviewGroup.interactable =
                    rollbackGroupPreviousInteractable;
                rollbackReviewGroup.blocksRaycasts =
                    rollbackGroupPreviousBlocksRaycasts;
                rollbackGroupRestored =
                    Mathf.Abs(
                        rollbackReviewGroup.alpha -
                        rollbackGroupPreviousAlpha) < 0.001f;
                if (rollbackGroupAddedForReview)
                {
                    Destroy(rollbackReviewGroup);
                }
            }

            if (visibilityGuard != null)
            {
                visibilityGuard.enabled = true;
            }

            if (legacySignalBridge != null)
            {
                legacySignalBridge.enabled =
                    legacySignalBridgeWasEnabled;
            }

            report.legacySignalBridgeRestored =
                legacySignalBridge == null ||
                legacySignalBridge.enabled ==
                    legacySignalBridgeWasEnabled;

            if (legacyRoutine != null)
            {
                legacyRoutine.enabled =
                    legacyRoutineWasEnabled;
            }

            report.legacyRoutineRestored =
                legacyRoutine == null ||
                legacyRoutine.enabled ==
                    legacyRoutineWasEnabled;

            report.patch35Restored =
                patch35RollbackRoot != null &&
                patch35RollbackRoot.activeSelf &&
                patch4VisualRoot != null &&
                !patch4VisualRoot.activeSelf &&
                rigController != null &&
                !rigController.Patch4Enabled &&
                rollbackGroupRestored &&
                report.legacyRoutineRestored &&
                report.legacySignalBridgeRestored;
            currentClip = string.Empty;
        }

        private void OnDestroy()
        {
            StopLogCapture();
        }

        private void OnReviewLog(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (report == null ||
                (type != LogType.Error &&
                 type != LogType.Exception &&
                 type != LogType.Assert))
            {
                return;
            }

            report.reviewConsoleErrorCount++;
            if (report.reviewConsoleErrors.Count < 20)
            {
                report.reviewConsoleErrors.Add(
                    string.IsNullOrWhiteSpace(condition)
                        ? type.ToString()
                        : condition);
            }
        }

        private void StopLogCapture()
        {
            if (!logCaptureRegistered)
            {
                return;
            }

            Application.logMessageReceived -= OnReviewLog;
            logCaptureRegistered = false;
        }

        private static string AppendError(
            string current,
            string addition)
        {
            if (string.IsNullOrWhiteSpace(addition))
            {
                return current ?? string.Empty;
            }

            return string.IsNullOrWhiteSpace(current)
                ? addition
                : current + Environment.NewLine + addition;
        }

        private static bool IsInsideLivingGameplayRoom(
            Transform candidate)
        {
            Transform current = candidate;
            while (current != null)
            {
                if (string.Equals(
                    current.name,
                    "LivingGameplayScene",
                    StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
#endif
