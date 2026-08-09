using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Presents every Patch 4 animation as one complete painted body frame.
    /// The rejected V21 Canvas mesh remains available for rollback diagnostics,
    /// but it is never blended with this surface. A discrete texture swap keeps
    /// the face attached and prevents doubled limbs, sliced joints and rubbery
    /// interpolation between poses. P4.0-AB adds deterministic per-clip scale
    /// calibration and per-frame shoe-line alignment so source-atlas padding
    /// can never make the character pop larger, smaller or float between
    /// otherwise valid complete-body frames.
    /// </summary>
    [DefaultExecutionOrder(1240)]
    [DisallowMultipleComponent]
    public sealed class Patch4V23FullFramePresentation : MonoBehaviour
    {
        private const string PresentationName =
            "V23FullFramePresentation";
        private const int Columns = 4;
        private const int Rows = 2;
        private const int FramesPerRow = 4;
        private const int IdlePingPongFrameCount = 6;
        private const byte VisibleAlphaThreshold = 32;
        private const float TargetGroundPixel = 22f;

        private struct FrameAlphaBounds
        {
            public int xMin;
            public int xMax;
            public int yMin;
            public int yMax;
            public bool valid;

            public bool TouchesCellEdge(int width, int height)
            {
                return valid &&
                    (xMin <= 1 || yMin <= 1 ||
                     xMax >= width - 1 || yMax >= height - 1);
            }
        }

        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private Patch4CanvasPresentation canvasPresentation;
        [SerializeField] private Animator animator;
        [SerializeField] private Texture2D idleSheet;
        [SerializeField] private Texture2D faceSheet;
        [SerializeField] private Texture2D tapSheet;
        [SerializeField] private Texture2D poseSheet;
        [SerializeField] private Texture2D upgradeSheet;
        [SerializeField] private Texture2D walkRightSheet;
        [SerializeField, Range(0.5f, 1f)]
        private float canvasHeightRatio = 0.8f;
        [SerializeField, Range(0f, 0.3f)]
        private float canvasBottomOffsetRatio = 0.156f;

        private RectTransform presentationRoot;
        private RawImage presentationImage;
        private RectTransform boundGeneratedRoot;
        private CanvasGroup generatedLayersGroup;
        private bool reviewActive;
#if UNITY_EDITOR
        private bool editorGameplayPreviewActive;
        private float editorWalkFacingSign = 1f;
#endif
        private string reviewClipName = "FatMan_Idle_Breathe";
        private float reviewNormalizedTime;
        private bool displayed;
        private bool underlayAlphaCaptured;
        private float underlayAlpha = 1f;
        private string activeClipName = string.Empty;
        private int activeFrameIndex;
        private Texture2D activeSheet;
        private Vector3 baseAnchoredPosition3D;
        private Vector3 baseLocalScale = Vector3.one;
        private float presentationHeight;
        private float activeArtworkScale = 1f;
        private float activeGroundCorrectionPixels;
        private bool frameCalibrationReady;
        private readonly Dictionary<Texture2D, FrameAlphaBounds[]>
            frameBoundsBySheet = new();

        public const int RequiredStateCount = 10;
        public const int RequiredWalkFrameCount = 8;

        public bool IsReady =>
            IsValidSheet(idleSheet) &&
            IsValidSheet(faceSheet) &&
            IsValidSheet(tapSheet) &&
            IsValidSheet(poseSheet) &&
            IsValidSheet(upgradeSheet) &&
            IsValidSheet(walkRightSheet) &&
            frameCalibrationReady &&
            presentationRoot != null &&
            presentationImage != null &&
            generatedLayersGroup != null;

        public bool IsDisplaying => displayed;
        public int FrameCount => RequiredWalkFrameCount;
        public int StateCount => RequiredStateCount;
        public int ActiveFrameIndex => activeFrameIndex;
        public string ActiveClipName => activeClipName;
        public Texture2D ActiveSheet => activeSheet;
        public Texture2D WalkSheet => walkRightSheet;
        public RectTransform PresentationRoot => presentationRoot;
        public bool FrameCalibrationReady => frameCalibrationReady;
        public float ActiveArtworkScale => activeArtworkScale;
        public float ActiveGroundCorrectionPixels =>
            activeGroundCorrectionPixels;
        public bool LegacyUnderlayHidden =>
            displayed &&
            generatedLayersGroup != null &&
            generatedLayersGroup.alpha <= 0.001f;
        public bool HasSingleVisibleCompleteFrame =>
            displayed &&
            presentationImage != null &&
            presentationImage.enabled &&
            presentationImage.texture != null &&
            LegacyUnderlayHidden;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            RebuildPresentation();
        }

        private void OnEnable()
        {
            RebuildPresentation();
        }

        private void OnDisable()
        {
            reviewActive = false;
#if UNITY_EDITOR
            editorGameplayPreviewActive = false;
            editorWalkFacingSign = 1f;
#endif
            SetDisplayed(false);
        }

        private void OnDestroy()
        {
            RestoreUnderlayAlpha();
        }

        private void LateUpdate()
        {
            if (!EnsurePresentation())
            {
                SetDisplayed(false);
                return;
            }

            if (reviewActive)
            {
                ApplyPose(reviewClipName, reviewNormalizedTime);
                SetDisplayed(true);
                return;
            }

            bool shouldDisplayGameplay =
                rigController != null && rigController.Patch4Enabled;
#if UNITY_EDITOR
            shouldDisplayGameplay |= editorGameplayPreviewActive;
#endif
            if (!shouldDisplayGameplay)
            {
                SetDisplayed(false);
                return;
            }

            if (!TryResolveAnimatorPose(
                    out string clipName,
                    out float normalizedTime))
            {
                clipName = "FatMan_Idle_Breathe";
                normalizedTime = 0f;
            }

            ApplyPose(clipName, normalizedTime);
            SetDisplayed(true);
        }

        public bool RebuildPresentation()
        {
            ResolveReferences();
            RectTransform generated = canvasPresentation != null
                ? canvasPresentation.GeneratedRoot
                : null;
            if (generated == null || generated.parent == null)
            {
                return false;
            }

            if (boundGeneratedRoot != generated)
            {
                RestoreUnderlayAlpha();
                boundGeneratedRoot = generated;
                generatedLayersGroup =
                    generated.GetComponent<CanvasGroup>();
                if (generatedLayersGroup == null)
                {
                    generatedLayersGroup =
                        generated.gameObject.AddComponent<CanvasGroup>();
                }

                underlayAlphaCaptured = false;
            }

            Transform existing = generated.parent.Find(PresentationName);
            if (existing != null)
            {
                presentationRoot = existing as RectTransform;
                presentationImage = existing.GetComponent<RawImage>();
            }

            if (presentationRoot == null)
            {
                GameObject rootObject = new(
                    PresentationName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(RawImage));
                rootObject.layer = gameObject.layer;
                presentationRoot =
                    rootObject.GetComponent<RectTransform>();
                presentationRoot.SetParent(generated.parent, false);
                presentationImage = rootObject.GetComponent<RawImage>();
            }
            else if (presentationImage == null)
            {
                presentationImage =
                    presentationRoot.gameObject.AddComponent<RawImage>();
            }

            if (!frameCalibrationReady)
            {
                frameCalibrationReady = RebuildFrameCalibration();
            }
            ConfigureRect(generated);
            presentationImage.color = Color.white;
            presentationImage.raycastTarget = false;
            presentationImage.maskable = false;
            presentationRoot.SetAsLastSibling();
            ApplyPose(reviewClipName, reviewNormalizedTime);
            SetDisplayed(reviewActive);
            return IsReady;
        }

        /// <summary>
        /// Target whole-frame cadence used by both normal gameplay and the
        /// uninterrupted locked-room preview. Diagnostic screenshot pauses do
        /// not alter these durations.
        /// </summary>
        public static float ResolvePlaybackDuration(string clipName)
        {
            switch (clipName)
            {
                case "FatMan_Idle_Breathe":
                    return 0.72f;
                case "FatMan_Idle_ShiftWeight":
                    return 0.48f;
                case "FatMan_Blink_Random":
                    return 0.2f;
                case "FatMan_LookAround":
                    return 0.48f;
                case "FatMan_TapReact_01":
                case "FatMan_TapReact_02":
                    return 0.32f;
                case "FatMan_Walk_InRoom":
                    return 0.56f;
                case "FatMan_Turn":
                    return 0.4f;
                case "FatMan_SitOrLean":
                    return 0.56f;
                case "FatMan_UpgradeReact":
                    return 0.48f;
                default:
                    return 1f;
            }
        }

        public bool TryMeasureFrameCalibration(
            out int clippedFrameCount,
            out float maximumRawGroundDeviationPixels,
            out float maximumArtworkScaleAdjustment)
        {
            clippedFrameCount = 0;
            maximumRawGroundDeviationPixels = 0f;
            maximumArtworkScaleAdjustment = 0f;
            if (!frameCalibrationReady)
            {
                return false;
            }

            foreach (KeyValuePair<Texture2D, FrameAlphaBounds[]> entry
                     in frameBoundsBySheet)
            {
                int cellWidth = entry.Key.width / Columns;
                int cellHeight = entry.Key.height / Rows;
                FrameAlphaBounds[] bounds = entry.Value;
                for (int i = 0; i < bounds.Length; i++)
                {
                    if (!bounds[i].valid)
                    {
                        return false;
                    }

                    if (bounds[i].TouchesCellEdge(cellWidth, cellHeight))
                    {
                        clippedFrameCount++;
                    }

                    maximumRawGroundDeviationPixels = Mathf.Max(
                        maximumRawGroundDeviationPixels,
                        Mathf.Abs(bounds[i].yMin - TargetGroundPixel));
                }
            }

            IReadOnlyList<string> clips =
                Patch4RigContract.RequiredClipNames;
            for (int i = 0; i < clips.Count; i++)
            {
                maximumArtworkScaleAdjustment = Mathf.Max(
                    maximumArtworkScaleAdjustment,
                    Mathf.Abs(ResolveArtworkScale(clips[i]) - 1f));
            }

            return true;
        }

        public bool SetReviewPose(
            string clipName,
            float normalizedTime)
        {
            if (!TryResolvePose(
                    clipName,
                    normalizedTime,
                    out _,
                    out _))
            {
                return false;
            }

            reviewActive = true;
#if UNITY_EDITOR
            editorGameplayPreviewActive = false;
#endif
            reviewClipName = clipName;
            reviewNormalizedTime = normalizedTime;
            if (!EnsurePresentation())
            {
                return false;
            }

            ApplyPose(reviewClipName, reviewNormalizedTime);
            SetDisplayed(true);
            return HasSingleVisibleCompleteFrame;
        }

        public void SetReviewActive(bool active)
        {
            reviewActive = active;
            if (!EnsurePresentation())
            {
                return;
            }

            if (active)
            {
                ApplyPose(reviewClipName, reviewNormalizedTime);
            }

            SetDisplayed(active);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Shows the locked complete-frame surface from the live Animator while
        /// leaving the production readiness gate closed. This override is
        /// compiled only into the Unity Editor and is used by the interactive
        /// actual-room preview after the technical review has passed.
        /// </summary>
        public bool SetEditorGameplayPreviewActive(bool active)
        {
            editorGameplayPreviewActive = active;
            if (active)
            {
                reviewActive = false;
            }
            else
            {
                editorWalkFacingSign = 1f;
            }

            if (!EnsurePresentation())
            {
                SetDisplayed(false);
                return false;
            }

            if (!active)
            {
                SetDisplayed(
                    reviewActive ||
                    (rigController != null && rigController.Patch4Enabled));
                return true;
            }

            if (!TryResolveAnimatorPose(
                    out string clipName,
                    out float normalizedTime))
            {
                clipName = "FatMan_Idle_Breathe";
                normalizedTime = 0f;
            }

            ApplyPose(clipName, normalizedTime);
            SetDisplayed(true);
            return HasSingleVisibleCompleteFrame;
        }

        /// <summary>
        /// Mirrors only the right-authored walk atlas while the Editor-only
        /// normal-game preview follows a live left-facing legacy walk. Front
        /// poses and every production-locked path keep their authored scale.
        /// </summary>
        public void SetEditorWalkFacingSign(int sign)
        {
            editorWalkFacingSign = sign < 0 ? -1f : 1f;
        }
#endif

        public bool TryMeasureGaitArticulation(
            out float leftArmDifference,
            out float rightArmDifference,
            out float leftLegDifference,
            out float rightLegDifference,
            out float minimumAdjacentFrameDifference)
        {
            leftArmDifference = 0f;
            rightArmDifference = 0f;
            leftLegDifference = 0f;
            rightLegDifference = 0f;
            minimumAdjacentFrameDifference = 0f;
            if (!IsValidSheet(walkRightSheet))
            {
                return false;
            }

            try
            {
                Color32[] pixels = walkRightSheet.GetPixels32();
                bool measured =
                    TryMeasureAlphaDifference(
                        walkRightSheet,
                        pixels,
                        0,
                        2,
                        new Rect(0.10f, 0.33f, 0.38f, 0.52f),
                        out leftArmDifference) &&
                    TryMeasureAlphaDifference(
                        walkRightSheet,
                        pixels,
                        0,
                        2,
                        new Rect(0.52f, 0.33f, 0.38f, 0.52f),
                        out rightArmDifference) &&
                    TryMeasureAlphaDifference(
                        walkRightSheet,
                        pixels,
                        0,
                        2,
                        new Rect(0.22f, 0.02f, 0.28f, 0.46f),
                        out leftLegDifference) &&
                    TryMeasureAlphaDifference(
                        walkRightSheet,
                        pixels,
                        0,
                        2,
                        new Rect(0.50f, 0.02f, 0.28f, 0.46f),
                        out rightLegDifference);

                minimumAdjacentFrameDifference = 1f;
                for (int frame = 0;
                     frame < RequiredWalkFrameCount;
                     frame++)
                {
                    measured &= TryMeasureAlphaDifference(
                        walkRightSheet,
                        pixels,
                        frame,
                        (frame + 1) % RequiredWalkFrameCount,
                        new Rect(0f, 0f, 1f, 1f),
                        out float adjacentDifference);
                    minimumAdjacentFrameDifference = Mathf.Min(
                        minimumAdjacentFrameDifference,
                        adjacentDifference);
                }

                return measured;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        public bool TryMeasureFaceArticulation(
            out float blinkDifference,
            out float lookDifference)
        {
            blinkDifference = 0f;
            lookDifference = 0f;
            if (!IsValidSheet(faceSheet))
            {
                return false;
            }

            try
            {
                Color32[] pixels = faceSheet.GetPixels32();
                Rect faceRegion = new(0.31f, 0.67f, 0.38f, 0.24f);
                return
                    TryMeasureColorDifference(
                        faceSheet,
                        pixels,
                        0,
                        2,
                        faceRegion,
                        out blinkDifference) &&
                    TryMeasureColorDifference(
                        faceSheet,
                        pixels,
                        4,
                        6,
                        faceRegion,
                        out lookDifference);
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private bool EnsurePresentation()
        {
            RectTransform generated = canvasPresentation != null
                ? canvasPresentation.GeneratedRoot
                : null;
            return IsReady && boundGeneratedRoot == generated
                ? true
                : RebuildPresentation();
        }

        private void ResolveReferences()
        {
            if (rigController == null)
            {
                rigController =
                    GetComponent<Patch4CharacterRigController>();
            }

            if (canvasPresentation == null)
            {
                canvasPresentation =
                    GetComponent<Patch4CanvasPresentation>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void ConfigureRect(RectTransform generated)
        {
            float sourceHeight = Mathf.Max(0.001f, generated.rect.height);
            float height = sourceHeight * canvasHeightRatio;
            float cellAspect =
                (walkRightSheet != null
                    ? walkRightSheet.width / (float)Columns
                    : 384f) /
                (walkRightSheet != null
                    ? walkRightSheet.height / (float)Rows
                    : 512f);

            presentationRoot.anchorMin = generated.anchorMin;
            presentationRoot.anchorMax = generated.anchorMax;
            presentationRoot.pivot = new Vector2(0.5f, 0f);
            presentationRoot.sizeDelta = new Vector2(
                height * cellAspect,
                height);
            presentationRoot.anchoredPosition3D =
                generated.anchoredPosition3D +
                Vector3.up * sourceHeight * canvasBottomOffsetRatio;
            presentationRoot.localRotation = generated.localRotation;
            presentationRoot.localScale = generated.localScale;
            baseAnchoredPosition3D = presentationRoot.anchoredPosition3D;
            baseLocalScale = presentationRoot.localScale;
            presentationHeight = height;
        }

        private bool ApplyPose(string clipName, float normalizedTime)
        {
            if (!TryResolvePose(
                    clipName,
                    normalizedTime,
                    out Texture2D sheet,
                    out int frameIndex))
            {
                return false;
            }

            activeClipName = clipName;
            activeFrameIndex = frameIndex;
            activeSheet = sheet;
            presentationImage.texture = sheet;
            SetFrame(frameIndex);
            ApplyFrameCalibration(clipName, sheet, frameIndex);
            return true;
        }

        private bool TryResolvePose(
            string clipName,
            float normalizedTime,
            out Texture2D sheet,
            out int frameIndex)
        {
            sheet = null;
            frameIndex = 0;
            int row = 0;
            bool walk = false;
            bool idlePingPong = false;

            switch (clipName)
            {
                case "FatMan_Idle_Breathe":
                    sheet = idleSheet;
                    idlePingPong = true;
                    break;
                case "FatMan_Idle_ShiftWeight":
                    sheet = idleSheet;
                    row = 1;
                    break;
                case "FatMan_Blink_Random":
                    sheet = faceSheet;
                    break;
                case "FatMan_LookAround":
                    sheet = faceSheet;
                    row = 1;
                    break;
                case "FatMan_TapReact_01":
                    sheet = tapSheet;
                    break;
                case "FatMan_TapReact_02":
                    sheet = tapSheet;
                    row = 1;
                    break;
                case "FatMan_Walk_InRoom":
                    sheet = walkRightSheet;
                    walk = true;
                    break;
                case "FatMan_Turn":
                    sheet = poseSheet;
                    break;
                case "FatMan_SitOrLean":
                    sheet = poseSheet;
                    row = 1;
                    break;
                case "FatMan_UpgradeReact":
                    sheet = upgradeSheet;
                    break;
                default:
                    return false;
            }

            if (!IsValidSheet(sheet))
            {
                return false;
            }

            float phase = normalizedTime - Mathf.Floor(normalizedTime);
            int phaseCount = walk
                ? RequiredWalkFrameCount
                : idlePingPong
                    ? IdlePingPongFrameCount
                    : FramesPerRow;
            int phaseFrame = Mathf.Clamp(
                Mathf.FloorToInt(phase * phaseCount),
                0,
                phaseCount - 1);
            int localFrame = idlePingPong && phaseFrame >= FramesPerRow
                ? IdlePingPongFrameCount - phaseFrame
                : phaseFrame;
            frameIndex = walk
                ? localFrame
                : row * FramesPerRow + localFrame;
            return true;
        }

        private void SetFrame(int frameIndex)
        {
            int clamped = Mathf.Clamp(
                frameIndex,
                0,
                RequiredWalkFrameCount - 1);
            int column = clamped % Columns;
            int topRow = clamped / Columns;
            float width = 1f / Columns;
            float height = 1f / Rows;
            float bottomRow = Rows - 1 - topRow;
            presentationImage.uvRect = new Rect(
                column * width,
                bottomRow * height,
                width,
                height);
        }

        private void SetDisplayed(bool visible)
        {
            bool valid = visible && activeSheet != null;
            if (presentationImage != null)
            {
                presentationImage.enabled = valid;
            }

            if (generatedLayersGroup == null)
            {
                displayed = false;
                return;
            }

            if (valid)
            {
                if (!underlayAlphaCaptured)
                {
                    underlayAlpha = generatedLayersGroup.alpha;
                    underlayAlphaCaptured = true;
                }

                generatedLayersGroup.alpha = 0f;
            }
            else
            {
                RestoreUnderlayAlpha();
                RestorePresentationTransform();
            }

            displayed = valid;
        }

        private void RestoreUnderlayAlpha()
        {
            if (generatedLayersGroup != null && underlayAlphaCaptured)
            {
                generatedLayersGroup.alpha = underlayAlpha;
            }

            underlayAlphaCaptured = false;
        }

        private bool TryResolveAnimatorPose(
            out string clipName,
            out float normalizedTime)
        {
            clipName = string.Empty;
            normalizedTime = 0f;
            if (animator == null || animator.layerCount <= 0)
            {
                return false;
            }

            AnimatorStateInfo state =
                animator.GetCurrentAnimatorStateInfo(0);
            string layerName = animator.GetLayerName(0);
            IReadOnlyList<string> names =
                Patch4RigContract.RequiredClipNames;
            for (int i = 0; i < names.Count; i++)
            {
                string candidate = names[i];
                if (state.shortNameHash ==
                        Animator.StringToHash(candidate) ||
                    state.fullPathHash ==
                        Animator.StringToHash(
                            layerName + "." + candidate))
                {
                    clipName = candidate;
                    // The generated Animator state already scales its source
                    // clip to ResolvePlaybackDuration. Multiplying by the
                    // source/target ratio again made the complete-frame strip
                    // finish early and hold its final frame, which read as a
                    // hitch between otherwise valid actions.
                    float targetPhase = state.normalizedTime;
                    normalizedTime = state.loop
                        ? targetPhase
                        : Mathf.Clamp(targetPhase, 0f, 0.9999f);
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidSheet(Texture2D sheet)
        {
            return
                sheet != null &&
                sheet.width > 0 &&
                sheet.height > 0 &&
                sheet.width % Columns == 0 &&
                sheet.height % Rows == 0 &&
                sheet.isReadable;
        }

        private bool RebuildFrameCalibration()
        {
            frameBoundsBySheet.Clear();
            return
                TryCacheFrameBounds(idleSheet) &&
                TryCacheFrameBounds(faceSheet) &&
                TryCacheFrameBounds(tapSheet) &&
                TryCacheFrameBounds(poseSheet) &&
                TryCacheFrameBounds(upgradeSheet) &&
                TryCacheFrameBounds(walkRightSheet);
        }

        private bool TryCacheFrameBounds(Texture2D sheet)
        {
            if (!IsValidSheet(sheet))
            {
                return false;
            }

            try
            {
                Color32[] pixels = sheet.GetPixels32();
                int cellWidth = sheet.width / Columns;
                int cellHeight = sheet.height / Rows;
                FrameAlphaBounds[] bounds =
                    new FrameAlphaBounds[RequiredWalkFrameCount];
                for (int frame = 0;
                     frame < RequiredWalkFrameCount;
                     frame++)
                {
                    ResolveFrameOrigin(
                        sheet,
                        frame,
                        out int originX,
                        out int originY);
                    FrameAlphaBounds current = new()
                    {
                        xMin = cellWidth,
                        xMax = -1,
                        yMin = cellHeight,
                        yMax = -1,
                        valid = false
                    };

                    for (int y = 0; y < cellHeight; y++)
                    {
                        int row = (originY + y) * sheet.width + originX;
                        for (int x = 0; x < cellWidth; x++)
                        {
                            if (pixels[row + x].a < VisibleAlphaThreshold)
                            {
                                continue;
                            }

                            current.valid = true;
                            current.xMin = Mathf.Min(current.xMin, x);
                            current.xMax = Mathf.Max(current.xMax, x);
                            current.yMin = Mathf.Min(current.yMin, y);
                            current.yMax = Mathf.Max(current.yMax, y);
                        }
                    }

                    if (!current.valid)
                    {
                        return false;
                    }

                    bounds[frame] = current;
                }

                frameBoundsBySheet.Add(sheet, bounds);
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private void ApplyFrameCalibration(
            string clipName,
            Texture2D sheet,
            int frameIndex)
        {
            activeArtworkScale = ResolveArtworkScale(clipName);
            activeGroundCorrectionPixels = 0f;
            if (presentationRoot == null ||
                sheet == null ||
                !frameBoundsBySheet.TryGetValue(
                    sheet,
                    out FrameAlphaBounds[] bounds) ||
                frameIndex < 0 ||
                frameIndex >= bounds.Length ||
                !bounds[frameIndex].valid)
            {
                RestorePresentationTransform();
                return;
            }

            float facingSign = 1f;
#if UNITY_EDITOR
            if (editorGameplayPreviewActive &&
                string.Equals(
                    clipName,
                    "FatMan_Walk_InRoom",
                    StringComparison.Ordinal))
            {
                facingSign = editorWalkFacingSign;
            }
#endif
            presentationRoot.localScale = new Vector3(
                baseLocalScale.x * activeArtworkScale * facingSign,
                baseLocalScale.y * activeArtworkScale,
                baseLocalScale.z);
            activeGroundCorrectionPixels =
                TargetGroundPixel -
                bounds[frameIndex].yMin * activeArtworkScale;
            int cellHeight = sheet.height / Rows;
            float correction =
                activeGroundCorrectionPixels /
                Mathf.Max(1, cellHeight) *
                presentationHeight;
            presentationRoot.anchoredPosition3D =
                baseAnchoredPosition3D + Vector3.up * correction;
        }

        private void RestorePresentationTransform()
        {
            if (presentationRoot == null)
            {
                return;
            }

            presentationRoot.anchoredPosition3D =
                baseAnchoredPosition3D;
            presentationRoot.localScale = baseLocalScale;
            activeArtworkScale = 1f;
            activeGroundCorrectionPixels = 0f;
        }

        private static float ResolveArtworkScale(string clipName)
        {
            switch (clipName)
            {
                case "FatMan_Blink_Random":
                    return 0.986f;
                case "FatMan_TapReact_01":
                case "FatMan_TapReact_02":
                    return 0.94f;
                case "FatMan_Walk_InRoom":
                    return 1.06f;
                case "FatMan_Turn":
                    return 0.933f;
                case "FatMan_UpgradeReact":
                    // The V24 correction is already authored at neutral-body
                    // scale. The older 1.135 compensation over-expanded it and
                    // caused the fresh room report to reject the silhouette.
                    return 1f;
                default:
                    return 1f;
            }
        }

        private static bool TryMeasureAlphaDifference(
            Texture2D sheet,
            Color32[] pixels,
            int firstFrame,
            int secondFrame,
            Rect normalizedRegion,
            out float coverage)
        {
            coverage = 0f;
            if (sheet == null ||
                pixels == null ||
                pixels.Length != sheet.width * sheet.height ||
                firstFrame < 0 ||
                firstFrame >= RequiredWalkFrameCount ||
                secondFrame < 0 ||
                secondFrame >= RequiredWalkFrameCount)
            {
                return false;
            }

            ResolveRegion(
                sheet,
                normalizedRegion,
                out int xMin,
                out int xMax,
                out int yMin,
                out int yMax);
            ResolveFrameOrigin(
                sheet,
                firstFrame,
                out int firstX,
                out int firstY);
            ResolveFrameOrigin(
                sheet,
                secondFrame,
                out int secondX,
                out int secondY);
            int different = 0;
            int union = 0;

            for (int y = yMin; y < yMax; y++)
            {
                int firstRow = (firstY + y) * sheet.width;
                int secondRow = (secondY + y) * sheet.width;
                for (int x = xMin; x < xMax; x++)
                {
                    bool firstVisible =
                        pixels[firstRow + firstX + x].a >=
                        VisibleAlphaThreshold;
                    bool secondVisible =
                        pixels[secondRow + secondX + x].a >=
                        VisibleAlphaThreshold;
                    if (firstVisible || secondVisible)
                    {
                        union++;
                    }

                    if (firstVisible != secondVisible)
                    {
                        different++;
                    }
                }
            }

            coverage = union > 0 ? different / (float)union : 0f;
            return union > 0;
        }

        private static bool TryMeasureColorDifference(
            Texture2D sheet,
            Color32[] pixels,
            int firstFrame,
            int secondFrame,
            Rect normalizedRegion,
            out float coverage)
        {
            coverage = 0f;
            ResolveRegion(
                sheet,
                normalizedRegion,
                out int xMin,
                out int xMax,
                out int yMin,
                out int yMax);
            ResolveFrameOrigin(
                sheet,
                firstFrame,
                out int firstX,
                out int firstY);
            ResolveFrameOrigin(
                sheet,
                secondFrame,
                out int secondX,
                out int secondY);
            int changed = 0;
            int reference = 0;

            for (int y = yMin; y < yMax; y++)
            {
                int firstRow = (firstY + y) * sheet.width;
                int secondRow = (secondY + y) * sheet.width;
                for (int x = xMin; x < xMax; x++)
                {
                    Color32 first = pixels[firstRow + firstX + x];
                    Color32 second = pixels[secondRow + secondX + x];
                    if (first.a < VisibleAlphaThreshold &&
                        second.a < VisibleAlphaThreshold)
                    {
                        continue;
                    }

                    reference++;
                    int delta =
                        Math.Abs(first.r - second.r) +
                        Math.Abs(first.g - second.g) +
                        Math.Abs(first.b - second.b) +
                        Math.Abs(first.a - second.a);
                    if (delta >= 48)
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

        private static void ResolveRegion(
            Texture2D sheet,
            Rect normalizedRegion,
            out int xMin,
            out int xMax,
            out int yMin,
            out int yMax)
        {
            int cellWidth = sheet.width / Columns;
            int cellHeight = sheet.height / Rows;
            xMin = Mathf.Clamp(
                Mathf.FloorToInt(normalizedRegion.xMin * cellWidth),
                0,
                cellWidth - 1);
            xMax = Mathf.Clamp(
                Mathf.CeilToInt(normalizedRegion.xMax * cellWidth),
                xMin + 1,
                cellWidth);
            yMin = Mathf.Clamp(
                Mathf.FloorToInt(normalizedRegion.yMin * cellHeight),
                0,
                cellHeight - 1);
            yMax = Mathf.Clamp(
                Mathf.CeilToInt(normalizedRegion.yMax * cellHeight),
                yMin + 1,
                cellHeight);
        }

        private static void ResolveFrameOrigin(
            Texture2D sheet,
            int frameIndex,
            out int x,
            out int y)
        {
            int cellWidth = sheet.width / Columns;
            int cellHeight = sheet.height / Rows;
            int column = frameIndex % Columns;
            int topRow = frameIndex / Columns;
            int bottomRow = Rows - 1 - topRow;
            x = column * cellWidth;
            y = bottomRow * cellHeight;
        }
    }
}
