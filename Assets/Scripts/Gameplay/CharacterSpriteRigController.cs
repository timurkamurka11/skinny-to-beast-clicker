using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Displays one complete painted character for each direction. Patch 3.2
    /// tried to cut a flattened turnaround into independent limbs at runtime;
    /// that cannot produce clean overlapping joints and duplicated neighbouring
    /// pixels. Patch 3.3 keeps the existing skeleton as an invisible animation
    /// driver, but renders one intact body image so the player can never see a
    /// torn or duplicated mannequin.
    /// </summary>
    [DefaultExecutionOrder(900)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterRigController))]
    [RequireComponent(typeof(CharacterSkinController))]
    public sealed class CharacterSpriteRigController : MonoBehaviour
    {
        private const string DefaultCatalogPath =
            "Characters/FatMan/FatManSpriteCatalog";
        private const float DefaultBodyHeight = 1120f;

        [SerializeField] private string catalogResourcePath =
            DefaultCatalogPath;
        [SerializeField] private bool removeFlatBackground = true;
        [SerializeField, Range(900f, 1240f)]
        private float bodyDisplayHeight = DefaultBodyHeight;
        [SerializeField] private Vector2 bodyOffset =
            new Vector2(0f, -18f);

        private readonly List<CanvasRenderer> legacyRenderers = new(96);
        private readonly RectInt[] directionBounds = new RectInt[3];
        private readonly Vector3[] worldCorners = new Vector3[4];

        private CharacterRigController rigController;
        private CharacterSkinController skinController;
        private CharacterLayeredRigController layeredRigController;
        private FatManSpriteSet catalog;
        private Texture2D runtimeTexture;
        private RectTransform visualRoot;
        private RectTransform bodyRect;
        private Image bodyImage;
        private Sprite bodySprite;
        private Image leftEyelid;
        private Image rightEyelid;
        private Image mouthOverlay;

        private CharacterFacing lastFacing = (CharacterFacing)(-1);
        private int lastStage = -1;
        private float stageScale = 1f;
        private float fitScale = 1f;
        private float nextBlinkAt;
        private float blinkUntil;
        private bool buildAttemptLogged;
        private bool ready;
        private bool pixelsSuppressedByReplacement;

        public bool IsReady =>
            ready &&
            runtimeTexture != null &&
            bodyRect != null &&
            bodyImage != null &&
            bodySprite != null;

        // Kept for diagnostics and compatibility with the 3.2 test surface.
        public int ActiveSpritePartCount => IsReady ? 1 : 0;
        public int LoadedDirectionCount =>
            directionBounds[0].width > 0 &&
            directionBounds[1].width > 0 &&
            directionBounds[2].width > 0
                ? 3
                : 0;
        public Texture2D RuntimeTexture => runtimeTexture;
        public bool PixelsSuppressedByReplacement =>
            pixelsSuppressedByReplacement;

        /// <summary>
        /// Keeps the selected gameplay skin, Animator, routine and hierarchy
        /// logically alive while an authoritative replacement owns the visible
        /// pixels. Patch 4 uses this instead of deactivating VisualRoot, which
        /// would make the Stage 4 readiness gate reject its own replacement.
        /// </summary>
        public void SetReplacementPixelsSuppressed(bool suppressed)
        {
            pixelsSuppressedByReplacement = suppressed;
            ApplyReplacementVisibility();
            layeredRigController ??= GetComponent<CharacterLayeredRigController>();
            layeredRigController?.ApplyReplacementSuppression(suppressed);
        }

#if UNITY_EDITOR
        public bool EditorPreviewSuppressed =>
            PixelsSuppressedByReplacement;

        /// <summary>
        /// Hides the renderer-owned Patch 3.5 pixels without deactivating the
        /// gameplay visual hierarchy. Patch 4 review tools use this so the
        /// stage controller can keep validating and driving the selected skin
        /// while exactly one character generation is rendered.
        /// </summary>
        public void SetEditorPreviewSuppressed(bool suppressed)
        {
            SetReplacementPixelsSuppressed(suppressed);
        }
#endif

        private void Awake()
        {
            rigController = GetComponent<CharacterRigController>();
            skinController = GetComponent<CharacterSkinController>();
            layeredRigController = GetComponent<CharacterLayeredRigController>();
        }

        private void Update()
        {
            if (!ready)
            {
                TryBuild();
            }
        }

        private void LateUpdate()
        {
            if (!ready)
            {
                return;
            }

            // Skin changes may reactivate the old vector graphics during the
            // same frame. Hide them immediately before the Canvas renders.
            HideLegacyGeometry();
            SyncFacing();
            SyncStage();
            if (pixelsSuppressedByReplacement)
            {
                HideFaceOverlays();
                return;
            }
            UpdateFaceOverlay();
        }

        private void TryBuild()
        {
            if (rigController == null ||
                skinController == null ||
                rigController.VisualRoot == null ||
                !rigController.HasAppliedSkin)
            {
                return;
            }

            catalog ??= Resources.Load<FatManSpriteSet>(
                string.IsNullOrWhiteSpace(catalogResourcePath)
                    ? DefaultCatalogPath
                    : catalogResourcePath);
            if (catalog == null ||
                !catalog.IsValid ||
                catalog.Turnaround == null)
            {
                LogBuildFailureOnce(
                    "Real Fat Man Sprite Patch 3.3 could not load " +
                    "FatManSpriteCatalog from Resources.");
                return;
            }

            Texture2D prepared = CreateRuntimeTexture(
                catalog.Turnaround,
                removeFlatBackground);
            if (prepared == null)
            {
                return;
            }

            runtimeTexture = prepared;
            visualRoot = rigController.VisualRoot;
            for (int column = 0; column < directionBounds.Length; column++)
            {
                directionBounds[column] = FindOpaqueBounds(
                    runtimeTexture,
                    column,
                    directionBounds.Length);
                if (directionBounds[column].width < 4 ||
                    directionBounds[column].height < 4)
                {
                    LogBuildFailureOnce(
                        $"Fat-man turnaround column {column} has no " +
                        "usable opaque character pixels.");
                    ClearRuntimeObjects();
                    return;
                }
            }

            CreateWholeBody();
            if (bodyImage == null || bodyRect == null)
            {
                LogBuildFailureOnce(
                    "Could not create the intact fat-man body image.");
                ClearRuntimeObjects();
                return;
            }

            CacheLegacyRenderers();
            CreateFaceOverlay();
            ready = true;
            buildAttemptLogged = false;
            lastFacing = (CharacterFacing)(-1);
            lastStage = -1;
            fitScale = 1f;
            ScheduleBlink();
            SyncFacing();
            SyncStage();
            HideLegacyGeometry();

            Debug.Log(
                "Real Fat Man Sprite Patch 3.3 active: one intact PNG body, " +
                "three directions, four stages and automatic screen fitting.",
                this);
        }

        private void CreateWholeBody()
        {
            GameObject target = new(
                "Sprite.RealFatManBody",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.layer = gameObject.layer;

            bodyRect = target.GetComponent<RectTransform>();
            bodyRect.SetParent(visualRoot, false);
            bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.anchoredPosition = bodyOffset;
            bodyRect.localRotation = Quaternion.identity;
            bodyRect.localScale = Vector3.one;
            bodyRect.SetAsLastSibling();

            bodyImage = target.GetComponent<Image>();
            bodyImage.raycastTarget = false;
            bodyImage.maskable = false;
            bodyImage.preserveAspect = false;
            bodyImage.type = Image.Type.Simple;
            bodyImage.color = Color.white;
        }

        private void SyncFacing()
        {
            CharacterFacing facing = rigController.Facing;
            if (facing == lastFacing || bodyImage == null)
            {
                return;
            }

            int column = catalog.GetColumn(facing);
            RectInt bounds = directionBounds[column];
            if (bounds.width < 4 || bounds.height < 4)
            {
                Debug.LogError(
                    $"The real fat-man view for column {column} is empty.",
                    this);
                return;
            }

            if (bodySprite != null)
            {
                Destroy(bodySprite);
                bodySprite = null;
            }

            bodySprite = Sprite.Create(
                runtimeTexture,
                new Rect(bounds.x, bounds.y, bounds.width, bounds.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            bodySprite.name =
                $"FatMan.Whole.{facing}.{bounds.x}.{bounds.y}";
            bodyImage.sprite = bodySprite;
            bodyImage.SetAllDirty();

            float safeHeight = Mathf.Clamp(
                bodyDisplayHeight,
                900f,
                1240f);
            float aspect = bounds.width / (float)Mathf.Max(1, bounds.height);
            bodyRect.sizeDelta = new Vector2(
                Mathf.Clamp(safeHeight * aspect, 310f, 760f),
                safeHeight);
            bodyRect.anchoredPosition = bodyOffset;

            bool back = facing == CharacterFacing.Back;
            bool side =
                facing == CharacterFacing.SideLeft ||
                facing == CharacterFacing.SideRight;
            PositionFaceOverlay(side, back);
            lastFacing = facing;
        }

        private void SyncStage()
        {
            int stage = Mathf.Max(0, skinController.CurrentArtIndex);
            if (stage == lastStage)
            {
                return;
            }

            stageScale = catalog.GetStageScale(stage);
            ApplyCombinedScale();
            lastStage = stage;
        }

        private void ApplyCombinedScale()
        {
            if (bodyRect == null)
            {
                return;
            }

            float combined = Mathf.Clamp(
                stageScale * fitScale,
                0.55f,
                2.5f);
            bodyRect.localScale = new Vector3(combined, combined, 1f);
        }

        /// <summary>
        /// Returns the bounds of the pixels the player actually sees. The old
        /// vector skeleton is intentionally excluded because it is transparent.
        /// </summary>
        public bool TryGetWorldBounds(out Bounds bounds)
        {
            bounds = default;
            if (!IsReady ||
                !bodyRect.gameObject.activeInHierarchy ||
                bodyImage.color.a <= 0.001f)
            {
                return false;
            }

            bodyRect.GetWorldCorners(worldCorners);
            bounds = new Bounds(worldCorners[0], Vector3.zero);
            for (int i = 1; i < worldCorners.Length; i++)
            {
                bounds.Encapsulate(worldCorners[i]);
            }

            return bounds.size.x > 2f && bounds.size.y > 2f;
        }

        public bool TryGetScreenHeightFraction(out float fraction)
        {
            fraction = 0f;
            if (!TryGetWorldBounds(out Bounds bounds) ||
                Screen.width <= 1 ||
                Screen.height <= 1)
            {
                return false;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null &&
                            canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(
                camera,
                bounds.min);
            Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(
                camera,
                bounds.max);
            fraction = Mathf.Abs(screenMax.y - screenMin.y) / Screen.height;
            return fraction > 0.001f;
        }

        /// <summary>
        /// Calibrates only the painted body. It does not alter the movement
        /// root, room anchors or Animator, so entry and gameplay use the same
        /// placement while still meeting their own visibility ranges.
        /// </summary>
        public bool FitToScreenHeight(float targetFraction)
        {
            if (!TryGetScreenHeightFraction(out float currentFraction) ||
                currentFraction <= 0.001f)
            {
                return false;
            }

            float target = Mathf.Clamp(targetFraction, 0.08f, 0.82f);
            float ratio = target / currentFraction;
            if (Mathf.Abs(1f - ratio) < 0.015f)
            {
                return true;
            }

            fitScale = Mathf.Clamp(fitScale * ratio, 0.55f, 2.5f);
            ApplyCombinedScale();
            Canvas.ForceUpdateCanvases();
            return true;
        }

        private void CreateFaceOverlay()
        {
            if (bodyRect == null)
            {
                return;
            }

            Color skinColor = SampleFaceColor();
            leftEyelid = CreateSolidImage(
                bodyRect,
                "SpriteFace.Eyelid.L",
                skinColor,
                new Vector2(28f, 7f));
            rightEyelid = CreateSolidImage(
                bodyRect,
                "SpriteFace.Eyelid.R",
                skinColor,
                new Vector2(28f, 7f));
            mouthOverlay = CreateSolidImage(
                bodyRect,
                "SpriteFace.Mouth",
                new Color(0.16f, 0.07f, 0.06f, 0.92f),
                new Vector2(34f, 8f));

            leftEyelid.gameObject.SetActive(false);
            rightEyelid.gameObject.SetActive(false);
            mouthOverlay.gameObject.SetActive(false);
        }

        private static Image CreateSolidImage(
            RectTransform parent,
            string objectName,
            Color color,
            Vector2 size)
        {
            GameObject target = new(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.layer = parent.gameObject.layer;
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();

            Image image = target.GetComponent<Image>();
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            image.maskable = false;
            image.color = color;
            return image;
        }

        private void PositionFaceOverlay(bool side, bool back)
        {
            if (bodyRect == null ||
                leftEyelid == null ||
                rightEyelid == null ||
                mouthOverlay == null)
            {
                return;
            }

            if (back)
            {
                leftEyelid.gameObject.SetActive(false);
                rightEyelid.gameObject.SetActive(false);
                mouthOverlay.gameObject.SetActive(false);
                blinkUntil = 0f;
                return;
            }

            float width = bodyRect.sizeDelta.x;
            float height = bodyRect.sizeDelta.y;
            float faceY = height * 0.355f;
            if (side)
            {
                Vector2 eye = new Vector2(width * 0.055f, faceY);
                leftEyelid.rectTransform.anchoredPosition = eye;
                rightEyelid.rectTransform.anchoredPosition = eye;
                mouthOverlay.rectTransform.anchoredPosition =
                    new Vector2(width * 0.075f, height * 0.298f);
            }
            else
            {
                leftEyelid.rectTransform.anchoredPosition =
                    new Vector2(-width * 0.062f, faceY);
                rightEyelid.rectTransform.anchoredPosition =
                    new Vector2(width * 0.062f, faceY);
                mouthOverlay.rectTransform.anchoredPosition =
                    new Vector2(0f, height * 0.294f);
            }
        }

        private void UpdateFaceOverlay()
        {
            if (leftEyelid == null ||
                rightEyelid == null ||
                mouthOverlay == null)
            {
                return;
            }

            CharacterFacing facing = rigController.Facing;
            bool back = facing == CharacterFacing.Back;
            bool side =
                facing == CharacterFacing.SideLeft ||
                facing == CharacterFacing.SideRight;
            float now = Time.unscaledTime;

            if (!back && now >= nextBlinkAt)
            {
                blinkUntil = now + 0.12f;
                ScheduleBlink();
            }

            bool blink = !back && now < blinkUntil;
            leftEyelid.gameObject.SetActive(blink);
            rightEyelid.gameObject.SetActive(blink && !side);

            bool expressive =
                !back &&
                (rigController.IsTapReacting ||
                 rigController.ActiveAction == CharacterRoutineAction.Yawn ||
                 rigController.ActiveAction == CharacterRoutineAction.Flex);
            mouthOverlay.gameObject.SetActive(expressive);
            if (expressive)
            {
                bool yawn =
                    rigController.ActiveAction == CharacterRoutineAction.Yawn;
                mouthOverlay.rectTransform.sizeDelta =
                    new Vector2(yawn ? 38f : 44f, yawn ? 34f : 9f);
            }
        }

        private void ScheduleBlink()
        {
            nextBlinkAt =
                Time.unscaledTime + UnityEngine.Random.Range(2.1f, 5.2f);
        }

        private void CacheLegacyRenderers()
        {
            legacyRenderers.Clear();

            CharacterMeshGraphic[] meshes =
                GetComponentsInChildren<CharacterMeshGraphic>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                CanvasRenderer renderer =
                    meshes[i] != null ? meshes[i].canvasRenderer : null;
                if (renderer != null &&
                    renderer != bodyImage.canvasRenderer &&
                    !legacyRenderers.Contains(renderer))
                {
                    legacyRenderers.Add(renderer);
                }
            }

            CharacterSurfaceGraphic[] surfaces =
                GetComponentsInChildren<CharacterSurfaceGraphic>(true);
            for (int i = 0; i < surfaces.Length; i++)
            {
                CanvasRenderer renderer =
                    surfaces[i] != null ? surfaces[i].canvasRenderer : null;
                if (renderer != null &&
                    !legacyRenderers.Contains(renderer))
                {
                    legacyRenderers.Add(renderer);
                }
            }
        }

        private void HideLegacyGeometry()
        {
            for (int i = 0; i < legacyRenderers.Count; i++)
            {
                CanvasRenderer renderer = legacyRenderers[i];
                if (renderer != null)
                {
                    renderer.SetAlpha(0f);
                }
            }

            if (bodyImage != null)
            {
                float bodyAlpha = pixelsSuppressedByReplacement
                    ? 0f
                    : 1f;
                bodyImage.canvasRenderer.SetAlpha(bodyAlpha);
            }
        }

        private void HideFaceOverlays()
        {
            if (leftEyelid != null)
            {
                leftEyelid.gameObject.SetActive(false);
            }

            if (rightEyelid != null)
            {
                rightEyelid.gameObject.SetActive(false);
            }

            if (mouthOverlay != null)
            {
                mouthOverlay.gameObject.SetActive(false);
            }
        }

        private void ApplyReplacementVisibility()
        {
            if (!ready)
            {
                return;
            }

            HideLegacyGeometry();
            if (pixelsSuppressedByReplacement)
            {
                HideFaceOverlays();
            }
            else
            {
                UpdateFaceOverlay();
            }
        }

        private Color SampleFaceColor()
        {
            RectInt frontBounds = directionBounds[
                catalog.GetColumn(CharacterFacing.Front)];
            int centerX = frontBounds.x +
                          Mathf.RoundToInt(frontBounds.width * 0.5f);
            int centerY = frontBounds.y +
                          Mathf.RoundToInt(frontBounds.height * 0.82f);

            for (int radius = 0; radius <= 24; radius += 4)
            {
                for (int y = -radius; y <= radius; y += 4)
                {
                    for (int x = -radius; x <= radius; x += 4)
                    {
                        int sampleX = Mathf.Clamp(
                            centerX + x,
                            0,
                            runtimeTexture.width - 1);
                        int sampleY = Mathf.Clamp(
                            centerY + y,
                            0,
                            runtimeTexture.height - 1);
                        Color sample = runtimeTexture.GetPixel(sampleX, sampleY);
                        if (sample.a > 0.8f &&
                            sample.r > 0.28f &&
                            sample.g > 0.18f)
                        {
                            sample.a = 1f;
                            return sample;
                        }
                    }
                }
            }

            return new Color(0.78f, 0.56f, 0.43f, 1f);
        }

        private Texture2D CreateRuntimeTexture(
            Texture2D source,
            bool removeBackground)
        {
            Color32[] sourcePixels;
            try
            {
                sourcePixels = source.GetPixels32();
            }
            catch (UnityException exception)
            {
                LogBuildFailureOnce(
                    "Fat-man PNG must be imported as Read/Write Enabled. " +
                    exception.Message);
                return null;
            }

            Color32[] output = new Color32[sourcePixels.Length];
            Array.Copy(sourcePixels, output, sourcePixels.Length);

            if (removeBackground && output.Length > 0)
            {
                Color32 cornerA = output[0];
                Color32 cornerB = output[Mathf.Max(0, source.width - 1)];
                Color32 cornerC = output[Mathf.Max(
                    0,
                    output.Length - source.width)];
                Color32 cornerD = output[output.Length - 1];

                for (int i = 0; i < output.Length; i++)
                {
                    Color32 pixel = output[i];
                    bool nearWhite =
                        pixel.r >= 245 &&
                        pixel.g >= 245 &&
                        pixel.b >= 245;
                    bool nearCorner =
                        IsNearOpaqueBackground(pixel, cornerA) ||
                        IsNearOpaqueBackground(pixel, cornerB) ||
                        IsNearOpaqueBackground(pixel, cornerC) ||
                        IsNearOpaqueBackground(pixel, cornerD);
                    if (pixel.a <= 8 || nearWhite || nearCorner)
                    {
                        output[i] = new Color32(
                            pixel.r,
                            pixel.g,
                            pixel.b,
                            0);
                    }
                }
            }

            Texture2D generated = new(
                source.width,
                source.height,
                TextureFormat.RGBA32,
                false,
                false)
            {
                name = "FatManTurnaround.Runtime.3.3",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            generated.SetPixels32(output);
            generated.Apply(false, false);
            return generated;
        }

        private static bool IsNearOpaqueBackground(
            Color32 pixel,
            Color32 background)
        {
            if (background.a < 245 || pixel.a < 16)
            {
                return false;
            }

            int dr = pixel.r - background.r;
            int dg = pixel.g - background.g;
            int db = pixel.b - background.b;
            return dr * dr + dg * dg + db * db <= 900;
        }

        private static RectInt FindOpaqueBounds(
            Texture2D texture,
            int column,
            int columnCount)
        {
            Color32[] pixels = texture.GetPixels32();
            int startX = Mathf.FloorToInt(
                texture.width * (column / (float)columnCount));
            int endX = Mathf.FloorToInt(
                texture.width * ((column + 1f) / columnCount)) - 1;
            startX = Mathf.Clamp(startX, 0, texture.width - 1);
            endX = Mathf.Clamp(endX, startX, texture.width - 1);

            int minX = endX;
            int maxX = startX;
            int minY = texture.height - 1;
            int maxY = 0;
            bool found = false;

            for (int y = 0; y < texture.height; y++)
            {
                int row = y * texture.width;
                for (int x = startX; x <= endX; x++)
                {
                    if (pixels[row + x].a <= 12)
                    {
                        continue;
                    }

                    found = true;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (!found)
            {
                return default;
            }

            const int padding = 2;
            minX = Mathf.Max(startX, minX - padding);
            maxX = Mathf.Min(endX, maxX + padding);
            minY = Mathf.Max(0, minY - padding);
            maxY = Mathf.Min(texture.height - 1, maxY + padding);
            return new RectInt(
                minX,
                minY,
                maxX - minX + 1,
                maxY - minY + 1);
        }

        private void LogBuildFailureOnce(string message)
        {
            if (buildAttemptLogged)
            {
                return;
            }

            buildAttemptLogged = true;
            Debug.LogError(message, this);
        }

        private void ClearRuntimeObjects()
        {
            if (bodyRect != null)
            {
                Destroy(bodyRect.gameObject);
            }

            bodyRect = null;
            bodyImage = null;
            leftEyelid = null;
            rightEyelid = null;
            mouthOverlay = null;

            if (bodySprite != null)
            {
                Destroy(bodySprite);
                bodySprite = null;
            }

            if (runtimeTexture != null)
            {
                Destroy(runtimeTexture);
                runtimeTexture = null;
            }

            ready = false;
        }

        private void OnDestroy()
        {
            ClearRuntimeObjects();
        }
    }
}
