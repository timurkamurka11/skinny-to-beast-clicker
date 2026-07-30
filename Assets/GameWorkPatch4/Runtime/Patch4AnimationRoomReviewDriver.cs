#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            public int canvasSkinDeformerCount;
            public int weightedLayerCount;
            public bool readinessGateRemainedLocked;
            public bool patch35Restored;
            public bool humanReviewRequired = true;
            public bool activationAllowed;
            public string contactSheetPath = string.Empty;
            public string error = string.Empty;
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
        private ReviewReport report;
        private string currentClip = string.Empty;
        private int currentClipIndex = -1;
        private bool started;

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
            for (int i = 0; i < report.clips.Count; i++)
            {
                report.allTenClipsReviewed &=
                    report.clips[i].captured;
            }

            Finish(report.allTenClipsReviewed, string.Empty);
        }

        private IEnumerator ReviewClip(
            AnimationClip clip,
            ClipReview clipReport,
            int clipIndex)
        {
            currentClip = clip.name;
            currentClipIndex = clipIndex;
            ConfigureFaceForClip(clip.name);

            float reviewDuration = Mathf.Clamp(
                clip.length,
                0.55f,
                2.4f);
            float captureAt = reviewDuration * 0.5f;
            if (string.Equals(
                    clip.name,
                    "FatMan_Blink_Random",
                    StringComparison.Ordinal))
            {
                reviewDuration = 0.55f;
                captureAt = 0.09f;
                faceController.BlinkNow();
            }

            clipReport.reviewDuration = reviewDuration;
            animator.speed =
                clip.length > 0.001f
                    ? clip.length / reviewDuration
                    : 1f;
            animator.Play(clip.name, 0, 0f);
            animator.Update(0f);

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
                    captured = CaptureCurrentRoomFrame(clipIndex);
                }

                yield return null;
            }

            if (!captured)
            {
                yield return new WaitForEndOfFrame();
                captured = CaptureCurrentRoomFrame(clipIndex);
            }

            clipReport.captured = captured;
            faceController.SetMouth(
                Patch4FaceController.MouthPose.Closed);
            animator.speed = 1f;
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
                canvasPresentation.SkinDeformerCount !=
                Patch4RigContract.RequiredLayerPaths.Count ||
                canvasPresentation.WeightedLayerCount < 20)
            {
                return "The Canvas skinning presentation is incomplete.";
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
            patch35RollbackRoot.SetActive(false);
            patch4VisualRoot.SetActive(true);
            faceController.SetEditorReviewActive(true);
            secondaryMotion.SetEditorReviewActive(true);
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
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

        private bool CaptureCurrentRoomFrame(int clipIndex)
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
            report.completed = true;
            report.passedTechnicalChecks =
                passed &&
                report.actualLivingGameplayRoom &&
                report.allTenClipsReviewed &&
                report.canvasSkinBindingsReady &&
                report.readinessGateRemainedLocked &&
                report.patch35Restored;
            report.error = AppendError(report.error, failure);

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

            if (visibilityGuard != null)
            {
                visibilityGuard.enabled = true;
            }

            report.patch35Restored =
                patch35RollbackRoot != null &&
                patch35RollbackRoot.activeSelf &&
                patch4VisualRoot != null &&
                !patch4VisualRoot.activeSelf &&
                rigController != null &&
                !rigController.Patch4Enabled;
            currentClip = string.Empty;
            currentClipIndex = -1;
        }

        private void OnGUI()
        {
            if (!started || currentClipIndex < 0)
            {
                return;
            }

            GUIStyle style = new(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.white
                }
            };
            GUI.Box(
                new Rect(18f, 18f, 620f, 76f),
                "PATCH 4 — LOCKED ROOM REVIEW\n" +
                (currentClipIndex + 1) + "/10  " + currentClip,
                style);
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
