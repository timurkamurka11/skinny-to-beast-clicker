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
            public bool visibleMotionPassed;
        }

        [Serializable]
        private sealed class ReviewReport
        {
            public string generatedUtc = string.Empty;
            public bool completed;
            public bool passedTechnicalChecks;
            public bool actualLivingGameplayRoom;
            public bool allTenClipsReviewed;
            public bool canvasSkinBindingsReady;
            public bool canvasBindAnchorsFrozen;
            public int canvasSkinDeformerCount;
            public int weightedLayerCount;
            public bool readinessGateRemainedLocked;
            public bool patch35Restored;
            public bool legacyRigStayedLogicallyActive;
            public bool visualSanityPassed;
            public bool visibleMotionPassed;
            public bool legacyRoutinePausedForReview;
            public bool legacyRoutineRestored;
            public bool legacySignalBridgePausedForReview;
            public bool legacySignalBridgeRestored;
            public bool legacyOneShotAudioStopped;
            public int reviewConsoleErrorCount;
            public bool humanReviewRequired = true;
            public bool activationAllowed;
            public string contactSheetPath = string.Empty;
            public string error = string.Empty;
            public List<string> reviewConsoleErrors = new();
            public List<ClipReview> clips = new();
        }

        public const string ReportFileName =
            "patch4-animation-room-review.json";
        public const string ContactSheetFileName =
            "patch4-animation-room-review.png";

        private const int ContactColumns = 5;
        private const int ContactRows = 2;
        private const int ThumbnailWidth = 300;
        private const int ThumbnailHeight = 420;
        private const int PixelDifferenceThreshold = 42;
        private const int MotionPixelDifferenceThreshold = 34;
        private const float MinimumSilhouetteWidthCoverage = 0.36f;
        private const float MinimumSilhouetteHeightCoverage = 0.60f;
        private const float MinimumSilhouetteAreaCoverage = 0.10f;
        private const float MinimumNeutralWidthRetention = 0.72f;
        private const float MinimumNeutralHeightRetention = 0.78f;
        private const float MinimumNeutralAreaRetention = 0.58f;

        private Patch4CharacterRigController rigController;
        private Patch4CharacterVisibilityGuard visibilityGuard;
        private Patch4CanvasPresentation canvasPresentation;
        private Patch4FaceController faceController;
        private Patch4SecondaryMotionController secondaryMotion;
        private Animator animator;
        private GameObject patch4VisualRoot;
        private GameObject patch35RollbackRoot;
        private string outputDirectory = string.Empty;
        private Texture2D contactSheet;
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
            Animator targetAnimator,
            GameObject visualRoot,
            GameObject rollbackRoot,
            string reportDirectory)
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
            animator = targetAnimator;
            patch4VisualRoot = visualRoot;
            patch35RollbackRoot = rollbackRoot;
            outputDirectory = reportDirectory ?? string.Empty;
            Application.logMessageReceived += OnReviewLog;
            logCaptureRegistered = true;
            StartCoroutine(RunReview());
        }

        private IEnumerator RunReview()
        {
            report = new ReviewReport
            {
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
            for (int i = 0; i < report.clips.Count; i++)
            {
                report.allTenClipsReviewed &=
                    report.clips[i].captured;
                report.visualSanityPassed &=
                    report.clips[i].visualSanityPassed;
                report.visibleMotionPassed &=
                    report.clips[i].visibleMotionPassed;
            }

            Finish(
                report.allTenClipsReviewed &&
                report.visualSanityPassed &&
                report.visibleMotionPassed,
                report.visualSanityPassed &&
                report.visibleMotionPassed
                    ? string.Empty
                    : "One or more animation clips collapsed or failed the " +
                      "visible start-to-peak motion check.");
        }

        private IEnumerator ReviewClip(
            AnimationClip clip,
            ClipReview clipReport,
            int clipIndex)
        {
            currentClip = clip.name;
            ConfigureFaceForClip(clip.name);

            float reviewDuration = Mathf.Clamp(
                clip.length,
                0.55f,
                2.4f);
            float captureAt =
                reviewDuration *
                ResolveCaptureNormalizedTime(clip.name);
            if (string.Equals(
                    clip.name,
                    "FatMan_Blink_Random",
                    StringComparison.Ordinal))
            {
                reviewDuration = 0.55f;
                captureAt = 0.09f;
            }

            clipReport.reviewDuration = reviewDuration;
            clipReport.minimumMotionCoverage =
                ResolveMinimumMotionCoverage(clip.name);
            animator.speed =
                clip.length > 0.001f
                    ? clip.length / reviewDuration
                    : 1f;
            animator.Play(clip.name, 0, 0f);
            animator.Update(0f);
            Canvas.ForceUpdateCanvases();

            yield return null;
            yield return new WaitForEndOfFrame();
            if (!CaptureClipStartPose(clipIndex))
            {
                clipReport.captured = false;
                report.error = AppendError(
                    report.error,
                    clip.name +
                    ": start-pose capture failed.");
                animator.speed = 1f;
                yield break;
            }

            if (string.Equals(
                    clip.name,
                    "FatMan_Blink_Random",
                    StringComparison.Ordinal))
            {
                faceController.BlinkNow();
            }

            float elapsed = 0f;
            bool captured = false;
            while (elapsed < reviewDuration)
            {
                elapsed += Mathf.Max(
                    0.001f,
                    Time.unscaledDeltaTime);
                if (!captured && elapsed >= captureAt)
                {
                    yield return new WaitForEndOfFrame();
                    captured = CaptureCurrentRoomFrame(
                        clipIndex,
                        clipReport);
                }

                yield return null;
            }

            if (!captured)
            {
                yield return new WaitForEndOfFrame();
                captured = CaptureCurrentRoomFrame(
                    clipIndex,
                    clipReport);
            }

            clipReport.captured = captured;
            faceController.SetMouth(
                Patch4FaceController.MouthPose.Closed);
            animator.speed = 1f;
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
                    return 0.003f;
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
                animator == null ||
                animator.runtimeAnimatorController == null ||
                patch4VisualRoot == null ||
                patch35RollbackRoot == null)
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
                canvasPresentation.WeightedLayerCount < 20)
            {
                return "The Canvas skinning presentation is incomplete or " +
                    "its bind anchors are not frozen.";
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
            faceController.SetEditorReviewActive(true);
            secondaryMotion.SetEditorReviewActive(true);
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Canvas.ForceUpdateCanvases();
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
            Rect expected = neutralReferenceCaptured
                ? neutralExpectedScreenRect
                : ResolveExpectedScreenRect(
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
                clipReport.neutralHeightRetention >=
                    MinimumNeutralHeightRetention &&
                clipReport.neutralAreaRetention >=
                    MinimumNeutralAreaRetention;
            if (!sane)
            {
                report.error = AppendError(
                    report.error,
                    clipReport.clipName +
                    ": collapsed room silhouette (width " +
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

            for (int y = yMin; y < yMax; y++)
            {
                int row = y * screenshot.width;
                for (int x = xMin; x < xMax; x++)
                {
                    int index = row + x;
                    Color32 before = clipStartPixels[index];
                    Color32 after = current[index];
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
            bool passed =
                clipReport.motionCoverage >=
                clipReport.minimumMotionCoverage;
            if (!passed)
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

            return passed;
        }

        private Rect ResolveExpectedScreenRect(
            int screenWidth,
            int screenHeight)
        {
            RectTransform root = canvasPresentation.GeneratedRoot;
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
            RenderTexture target = RenderTexture.GetTemporary(
                ThumbnailWidth,
                ThumbnailHeight,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            try
            {
                float sourceAspect =
                    source.width / (float)source.height;
                float targetAspect =
                    ThumbnailWidth / (float)ThumbnailHeight;
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
                    ThumbnailWidth,
                    ThumbnailHeight,
                    TextureFormat.RGBA32,
                    false,
                    false);
                thumbnail.ReadPixels(
                    new Rect(
                        0f,
                        0f,
                        ThumbnailWidth,
                        ThumbnailHeight),
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
            string reportPath = Path.Combine(
                outputDirectory,
                ReportFileName);
            report.contactSheetPath =
                contactPath.Replace('\\', '/');

            try
            {
                if (contactSheet != null)
                {
                    contactSheet.Apply(false, false);
                    File.WriteAllBytes(
                        contactPath,
                        contactSheet.EncodeToPNG());
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
