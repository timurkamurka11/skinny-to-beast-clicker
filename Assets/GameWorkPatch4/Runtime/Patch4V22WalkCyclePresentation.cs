using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Presents the reviewed V22 walk as eight complete painted frames.
    /// The walk deliberately bypasses the experimental full-body Canvas mesh:
    /// one complete silhouette is shown at a time, so a step cannot expose
    /// sliced joints, detach the face or stretch the body like rubber.
    /// </summary>
    [DefaultExecutionOrder(1230)]
    [DisallowMultipleComponent]
    public sealed class Patch4V22WalkCyclePresentation : MonoBehaviour
    {
        private const string PresentationName =
            "V22WalkCyclePresentation";
        private const string WalkStateName = "FatMan_Walk_InRoom";
        private const byte VisibleAlphaThreshold = 32;

        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private Patch4CanvasPresentation canvasPresentation;
        [SerializeField] private Animator animator;
        [SerializeField] private Texture2D walkSheet;
        [SerializeField, Min(1)] private int columns = 4;
        [SerializeField, Min(1)] private int rows = 2;
        [SerializeField, Range(0.5f, 1f)]
        private float canvasHeightRatio = 0.8f;
        [SerializeField, Range(0f, 0.3f)]
        private float canvasBottomOffsetRatio = 0.156f;

        private RectTransform presentationRoot;
        private RawImage presentationImage;
        private RectTransform boundGeneratedRoot;
        private CanvasGroup generatedLayersGroup;
        private bool reviewActive;
        private int reviewFrame;
        private bool displayed;
        private bool underlayAlphaCaptured;
        private float underlayAlpha = 1f;

        public const int RequiredFrameCount = 8;

        public bool IsReady =>
            walkSheet != null &&
            columns * rows == RequiredFrameCount &&
            walkSheet.width % columns == 0 &&
            walkSheet.height % rows == 0 &&
            walkSheet.isReadable &&
            presentationRoot != null &&
            presentationImage != null &&
            generatedLayersGroup != null;

        public bool IsDisplaying => displayed;
        public int FrameCount => columns * rows;
        public int ActiveFrameIndex => reviewActive
            ? Mathf.Clamp(reviewFrame, 0, FrameCount - 1)
            : ResolveRuntimeFrame();
        public Texture2D WalkSheet => walkSheet;
        public RectTransform PresentationRoot => presentationRoot;

        private void Reset()
        {
            rigController = GetComponent<Patch4CharacterRigController>();
            canvasPresentation = GetComponent<Patch4CanvasPresentation>();
            animator = GetComponent<Animator>();
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
            SetDisplayed(false);
        }

        private void LateUpdate()
        {
            if (!EnsurePresentation())
            {
                SetDisplayed(false);
                return;
            }

            bool runtimeWalk =
                rigController != null &&
                rigController.Patch4Enabled &&
                IsAnimatorWalking();
            bool shouldDisplay = reviewActive || runtimeWalk;
            if (shouldDisplay)
            {
                SetFrame(
                    reviewActive
                        ? reviewFrame
                        : ResolveRuntimeFrame());
            }

            SetDisplayed(shouldDisplay);
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
                presentationImage =
                    existing.GetComponent<RawImage>();
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

            ConfigureRect(generated);
            presentationImage.texture = walkSheet;
            presentationImage.color = Color.white;
            presentationImage.raycastTarget = false;
            presentationImage.maskable = false;
            presentationRoot.SetAsLastSibling();
            SetFrame(reviewFrame);
            SetDisplayed(reviewActive);
            return IsReady;
        }

        public void SetReviewActive(bool active)
        {
            reviewActive = active;
            if (active)
            {
                reviewFrame = 0;
            }

            if (EnsurePresentation())
            {
                SetFrame(reviewFrame);
                SetDisplayed(active);
            }
        }

        public void SetReviewFrame(int frameIndex)
        {
            reviewFrame = Mathf.Clamp(
                frameIndex,
                0,
                Mathf.Max(0, FrameCount - 1));
            if (reviewActive && EnsurePresentation())
            {
                SetFrame(reviewFrame);
                SetDisplayed(true);
            }
        }

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
            if (walkSheet == null ||
                columns * rows != RequiredFrameCount ||
                walkSheet.width % columns != 0 ||
                walkSheet.height % rows != 0 ||
                !walkSheet.isReadable)
            {
                return false;
            }

            try
            {
                Color32[] pixels = walkSheet.GetPixels32();
                if (pixels.Length != walkSheet.width * walkSheet.height)
                {
                    return false;
                }

                // Frames zero and four are the opposing contact poses. The
                // regions are measured in bottom-origin cell coordinates and
                // cover the visible arm and leg silhouettes, not hidden bones.
                bool measured =
                    TryMeasureAlphaDifference(
                        pixels,
                        0,
                        4,
                        new Rect(0.10f, 0.33f, 0.38f, 0.52f),
                        out leftArmDifference) &&
                    TryMeasureAlphaDifference(
                        pixels,
                        0,
                        4,
                        new Rect(0.52f, 0.33f, 0.38f, 0.52f),
                        out rightArmDifference) &&
                    TryMeasureAlphaDifference(
                        pixels,
                        0,
                        4,
                        new Rect(0.22f, 0.02f, 0.28f, 0.46f),
                        out leftLegDifference) &&
                    TryMeasureAlphaDifference(
                        pixels,
                        0,
                        4,
                        new Rect(0.50f, 0.02f, 0.28f, 0.46f),
                        out rightLegDifference);

                minimumAdjacentFrameDifference = 1f;
                for (int frame = 0; frame < RequiredFrameCount; frame++)
                {
                    measured &= TryMeasureAlphaDifference(
                        pixels,
                        frame,
                        (frame + 1) % RequiredFrameCount,
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

        private bool TryMeasureAlphaDifference(
            Color32[] pixels,
            int firstFrame,
            int secondFrame,
            Rect normalizedRegion,
            out float coverage)
        {
            coverage = 0f;
            if (pixels == null ||
                firstFrame < 0 ||
                firstFrame >= FrameCount ||
                secondFrame < 0 ||
                secondFrame >= FrameCount)
            {
                return false;
            }

            int cellWidth = walkSheet.width / columns;
            int cellHeight = walkSheet.height / rows;
            int xMin = Mathf.Clamp(
                Mathf.FloorToInt(normalizedRegion.xMin * cellWidth),
                0,
                cellWidth - 1);
            int xMax = Mathf.Clamp(
                Mathf.CeilToInt(normalizedRegion.xMax * cellWidth),
                xMin + 1,
                cellWidth);
            int yMin = Mathf.Clamp(
                Mathf.FloorToInt(normalizedRegion.yMin * cellHeight),
                0,
                cellHeight - 1);
            int yMax = Mathf.Clamp(
                Mathf.CeilToInt(normalizedRegion.yMax * cellHeight),
                yMin + 1,
                cellHeight);
            int firstColumn = firstFrame % columns;
            int firstBottomRow = rows - 1 - firstFrame / columns;
            int secondColumn = secondFrame % columns;
            int secondBottomRow = rows - 1 - secondFrame / columns;
            int different = 0;
            int union = 0;

            for (int y = yMin; y < yMax; y++)
            {
                int firstRow =
                    (firstBottomRow * cellHeight + y) * walkSheet.width;
                int secondRow =
                    (secondBottomRow * cellHeight + y) * walkSheet.width;
                for (int x = xMin; x < xMax; x++)
                {
                    bool firstVisible = pixels[
                        firstRow + firstColumn * cellWidth + x].a >=
                        VisibleAlphaThreshold;
                    bool secondVisible = pixels[
                        secondRow + secondColumn * cellWidth + x].a >=
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
            float cellAspect = walkSheet != null && rows > 0 && columns > 0
                ? (walkSheet.width / (float)columns) /
                  (walkSheet.height / (float)rows)
                : 2f / 3f;
            float height = sourceHeight * canvasHeightRatio;

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
        }

        private void SetFrame(int frameIndex)
        {
            if (presentationImage == null || FrameCount <= 0)
            {
                return;
            }

            int clamped = Mathf.Clamp(frameIndex, 0, FrameCount - 1);
            int column = clamped % columns;
            int topRow = clamped / columns;
            float width = 1f / columns;
            float height = 1f / rows;
            float bottomRow = rows - 1 - topRow;
            presentationImage.uvRect = new Rect(
                column * width,
                bottomRow * height,
                width,
                height);
        }

        private void SetDisplayed(bool visible)
        {
            if (presentationImage != null)
            {
                presentationImage.enabled = visible && walkSheet != null;
            }

            if (generatedLayersGroup == null)
            {
                displayed = false;
                return;
            }

            if (visible)
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
            }

            displayed = visible && walkSheet != null;
        }

        private void RestoreUnderlayAlpha()
        {
            if (generatedLayersGroup != null && underlayAlphaCaptured)
            {
                generatedLayersGroup.alpha = underlayAlpha;
            }

            underlayAlphaCaptured = false;
        }

        private bool IsAnimatorWalking()
        {
            if (animator == null || animator.layerCount == 0)
            {
                return false;
            }

            AnimatorStateInfo state =
                animator.GetCurrentAnimatorStateInfo(0);
            int shortHash = Animator.StringToHash(WalkStateName);
            string fullPath =
                animator.GetLayerName(0) + "." + WalkStateName;
            return
                state.shortNameHash == shortHash ||
                state.fullPathHash == Animator.StringToHash(fullPath);
        }

        private int ResolveRuntimeFrame()
        {
            if (animator == null || FrameCount <= 0)
            {
                return 0;
            }

            float normalized =
                animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            float phase = normalized - Mathf.Floor(normalized);
            return Mathf.Clamp(
                Mathf.FloorToInt(phase * FrameCount),
                0,
                FrameCount - 1);
        }
    }
}
