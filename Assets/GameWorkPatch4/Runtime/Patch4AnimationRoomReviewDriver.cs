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
            public bool singleCompleteFramePassed;
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
            public float v23LeftArmSilhouetteDifference;
            public float v23RightArmSilhouetteDifference;
            public float v23LeftLegSilhouetteDifference;
            public float v23RightLegSilhouetteDifference;
            public float v23MinimumAdjacentFrameDifference;
            public bool v23FrameSequenceReady;
            public bool v23FrameSequenceUsed;
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
            public bool v23WalkFrameSequenceReady;
            public bool v23TenStateFullFrameReady;
            public bool v23FaceArticulationReady;
            public float v23BlinkDifference;
            public float v23LookDifference;
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
        private const int WalkContactPhaseIndex = 2;
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
        private const float MinimumFocusedFaceMotionCoverage = 0.015f;
        private const float MinimumWalkLimbMotionCoverage = 0.04f;
        private const float MinimumWalkArmMotionCoverage = 0.035f;
        private const float MinimumWalkLegMotionCoverage = 0.04f;
        private const float MinimumV23WalkArmSilhouetteDifference = 0.14f;
        private const float MinimumV23WalkLegSilhouetteDifference = 0.14f;
        private const float MinimumV23AdjacentFrameDifference = 0.075f;
        private const float MinimumV23FaceDifference = 0.02f;
        private const float MinimumWalkTravelWidthRatio = 0.55f;

        private Patch4CharacterRigController rigController;
        private Patch4CharacterVisibilityGuard visibilityGuard;
        private Patch4CanvasPresentation canvasPresentation;
        private Patch4FaceController faceController;
        private Patch4SecondaryMotionController secondaryMotion;
        private Patch4V23FullFramePresentation v23FullFramePresentation;
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
            Patch4V23FullFramePresentation fullFramePresentation,
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
            v23FullFramePresentation = fullFramePresentation;
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
            float blinkDifference = 0f;
            float lookDifference = 0f;
            bool faceArticulationMeasured =
                v23FullFramePresentation != null &&
                v23FullFramePresentation.TryMeasureFaceArticulation(
                    out blinkDifference,
                    out lookDifference);
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
                v23WalkFrameSequenceReady =
                    v23FullFramePresentation != null &&
                    v23FullFramePresentation.IsReady &&
                    v23FullFramePresentation.FrameCount ==
                        Patch4V23FullFramePresentation.RequiredWalkFrameCount,
                v23TenStateFullFrameReady =
                    v23FullFramePresentation != null &&
                    v23FullFramePresentation.IsReady &&
                    v23FullFramePresentation.StateCount ==
                        Patch4RigContract.RequiredClipNames.Count,
                v23FaceArticulationReady =
                    faceArticulationMeasured &&
                    blinkDifference >= MinimumV23FaceDifference &&
                    lookDifference >= MinimumV23FaceDifference,
                v23BlinkDifference = blinkDifference,
                v23LookDifference = lookDifference,
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
                  