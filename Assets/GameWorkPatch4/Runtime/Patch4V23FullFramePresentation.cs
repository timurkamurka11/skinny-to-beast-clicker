using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Keeps the complete-frame sheets as read-only art references while the
    /// live character is rendered by the continuous V21 Canvas rig. V33 never
    /// swaps a whole painted body during an animation: the same torso, head,
    /// face and four continuous limbs remain visible while Animator curves
    /// interpolate their bones every rendered frame.
    /// </summary>
    [DefaultExecutionOrder(1240)]
    [DisallowMultipleComponent]
    public sealed class Patch4V23FullFramePresentation : MonoBehaviour
    {
        private const string PresentationName =
            "V23FullFramePresentation";
        private const int Columns = 4;
        private const int StandardRows = 2;
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
#endif
        private string reviewClipName = "FatMan_Idle_Breathe";
        private float reviewNormalizedTime;
        private bool displayed;
        private bool layeredRigActive;
        private string activeClipName = string.Empty;
        private int activeFrameIndex;
        private Vector3 baseAnchoredPosition3D;
        private Vector3 baseLocalScale = Vector3.one;
        private bool frameCalibrationReady;
        private readonly Dictionary<Texture2D, FrameAlphaBounds[]>
            frameBoundsBySheet = new();

        public const int RequiredStateCount = 10;
        public const int RequiredWalkFrameCount = 16;

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
        public bool IsLayeredRigActive => layeredRigActive;
        public bool UsesContinuousLayeredRig => true;
        public int FrameCount => RequiredWalkFrameCount;
        public int StateCount => RequiredStateCount;
        public int ActiveFrameIndex => activeFrameIndex;
        public string ActiveClipName => activeClipName;
        public Texture2D WalkSheet => walkRightSheet;
        public RectTransform PresentationRoot => presentationRoot;
        public bool FrameCalibrationReady => frameCalibrationReady;
        public bool HasSingleVisibleLayeredCharacter =>
            layeredRigActive &&
            !displayed &&
            generatedLayersGroup != null &&
            generatedLayersGroup.alpha > 0.001f;

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
            layeredRigActive = false;
#if UNITY_EDITOR
            editorGameplayPreviewActive = false;
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
                layeredRigActive = false;
                SetDisplayed(false);
                return;
            }

            if (reviewActive)
            {
                ApplyPose(reviewClipName, reviewNormalizedTime);
                SetLayeredRigActive(true);
                return;
            }

            bool shouldDisplayGameplay =
                rigController != null && rigController.Patch4Enabled;
#if UNITY_EDITOR
            shouldDisplayGameplay |= editorGameplayPreviewActive;
#endif
            if (!shouldDisplayGameplay)
            {
                layeredRigActive = false;
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
            SetLayeredRigActive(true);
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
            presentationImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            presentationRoot.SetAsLastSibling();
            ApplyPose(reviewClipName, reviewNormalizedTime);
            SetLayeredRigActive(reviewActive);
            return IsReady;
        }

        /// <summary>
        /// Target Animator cadence used by both normal gameplay and the
        /// uninterrupted locked-room preview. Diagnostic screenshot pauses do
        /// not alter these durations.
        /// </summary>
        public static float ResolvePlaybackDuration(string clipName)
        {
            switch (clipName)
            {
                case "FatMan_Idle_Breathe":
                    return 3.2f;
                case "FatMan_Idle_ShiftWeight":
                    return 3.2f;
                case "FatMan_Blink_Random":
                    return 0.18f;
                case "FatMan_LookAround":
                    return 3f;
                case "FatMan_TapReact_01":
                    return 0.65f;
                case "FatMan_TapReact_02":
                    return 0.72f;
                case "FatMan_Walk_InRoom":
                    return 1.6f;
                case "FatMan_Turn":
                    return 0.72f;
                case "FatMan_SitOrLean":
                    return 1.15f;
                case "FatMan_UpgradeReact":
                    return 1.05f;
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
                int cellHeight = entry.Key.height / ResolveRows(entry.Key);
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

            // The live V33 rig never applies per-state image scale. The
            // reference sheets are measured only for source-art QA.
            maximumArtworkScaleAdjustment = 0f;

            return true;
        }

        public bool SetReviewPose(
            string clipName,
            float normalizedTime)
        {
            if (!IsSupportedClip(clipName))
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
            SetLayeredRigActive(true);
            return HasSingleVisibleLayeredCharacter;
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

            SetLayeredRigActive(active);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Shows the locked continuous layered rig from the live Animator while
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
                Patch4V21HybridPuppetController hybrid =
                    GetComponent<Patch4V21HybridPuppetController>();
                if (hybrid != null)
                {
                    hybrid.SetFacingSign(1);
                }
            }

            if (!EnsurePresentation())
            {
                SetDisplayed(false);
                return false;
            }

            if (!active)
            {
                SetLayeredRigActive(
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
            SetLayeredRigActive(true);
            return HasSingleVisibleLayeredCharacter;
        }

        /// <summary>
        /// Mirrors the one continuous layered character from the same live
        /// left/right signal that drives room travel.
        /// </summary>
        public void SetEditorWalkFacingSign(int sign)
        {
            Patch4V21HybridPuppetController hybrid =
                GetComponent<Patch4V21HybridPuppetController>();
            if (hybrid != null)
            {
                hybrid.SetFacingSign(sign);
            }
        }
#endif

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
                    ? walkRightSheet.height /
                        (float)ResolveRows(walkRightSheet)
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
        }

        private bool ApplyPose(string clipName, float normalizedTime)
        {
            if (!IsSupportedClip(clipName))
            {
                return false;
            }

            activeClipName = clipName;
            float phase = normalizedTime - Mathf.Floor(normalizedTime);
            activeFrameIndex = Mathf.Clamp(
                Mathf.FloorToInt(phase * RequiredWalkFrameCount),
                0,
                RequiredWalkFrameCount - 1);
            RestorePresentationTransform();
            return true;
        }

        private static bool IsSupportedClip(string clipName)
        {
            IReadOnlyList<string> names = Patch4RigContract.RequiredClipNames;
            for (int i = 0; i < names.Count; i++)
            {
                if (string.Equals(names[i], clipName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void SetDisplayed(bool _)
        {
            // V33 keeps this RawImage as a disabled art-reference surface.
            // Runtime motion always comes from the continuous layered rig;
            // no gameplay or review path may hide it and swap whole frames.
            if (presentationImage != null)
            {
                presentationImage.enabled = false;
            }

            RestoreUnderlayAlpha();
            RestorePresentationTransform();
            displayed = false;
        }

        private void SetLayeredRigActive(bool active)
        {
            SetDisplayed(false);
            layeredRigActive = active && generatedLayersGroup != null;
            if (layeredRigActive && generatedLayersGroup.alpha <= 0.001f)
            {
                generatedLayersGroup.alpha = 1f;
            }
        }

        private void RestoreUnderlayAlpha()
        {
            if (generatedLayersGroup != null &&
                generatedLayersGroup.alpha <= 0.001f)
            {
                generatedLayersGroup.alpha = 1f;
            }
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
                    // source/target ratio again would make the rig phase finish
                    // early and visibly hitch between otherwise valid actions.
                    float targetPhase = state.normalizedTime;
                    normalizedTime = state.loop
                        ? targetPhase
                        : Mathf.Clamp(targetPhase, 0f, 0.9999f);
                    return true;
                }
            }

            return false;
        }

        private bool IsValidSheet(Texture2D sheet)
        {
            return
                sheet != null &&
                sheet.width > 0 &&
                sheet.height > 0 &&
                sheet.width % Columns == 0 &&
                sheet.height % ResolveRows(sheet) == 0 &&
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
                TryCacheFrameBounds(upgradeSheet);
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
                int cellHeight = sheet.height / ResolveRows(sheet);
                int frameCount = Columns * StandardRows;
                FrameAlphaBounds[] bounds =
                    new FrameAlphaBounds[frameCount];
                for (int frame = 0;
                     frame < frameCount;
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

        private void RestorePresentationTransform()
        {
            if (presentationRoot == null)
            {
                return;
            }

            presentationRoot.anchoredPosition3D =
                baseAnchoredPosition3D;
            presentationRoot.localScale = baseLocalScale;
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
            int cellHeight = sheet.height / ResolveRows(sheet);
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
            int rows = ResolveRows(sheet);
            int cellHeight = sheet.height / rows;
            int column = frameIndex % Columns;
            int topRow = frameIndex / Columns;
            int bottomRow = rows - 1 - topRow;
            x = column * cellWidth;
            y = bottomRow * cellHeight;
        }

        private static int ResolveRows(Texture2D sheet)
        {
            return StandardRows;
        }
    }
}
