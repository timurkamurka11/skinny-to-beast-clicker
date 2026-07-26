using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    [DefaultExecutionOrder(900)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterRigController))]
    [RequireComponent(typeof(CharacterSkinController))]
    public sealed class CharacterSpriteRigController : MonoBehaviour
    {
        [Serializable]
        private readonly struct PartSpec
        {
            public readonly FatManSpritePartId id;
            public readonly string boneName;
            public readonly Rect crop;
            public readonly Vector2 pivot;
            public readonly int sortingOrder;

            public PartSpec(
                FatManSpritePartId targetId,
                string targetBone,
                Rect targetCrop,
                Vector2 targetPivot,
                int order)
            {
                id = targetId;
                boneName = targetBone;
                crop = targetCrop;
                pivot = targetPivot;
                sortingOrder = order;
            }
        }

        private static readonly PartSpec[] PartSpecs =
        {
            new(
                FatManSpritePartId.ShinLeft,
                "Bone.Shin.L",
                new Rect(0.10f, 0.00f, 0.40f, 0.20f),
                new Vector2(0.52f, 0.92f),
                100),
            new(
                FatManSpritePartId.ShinRight,
                "Bone.Shin.R",
                new Rect(0.50f, 0.00f, 0.40f, 0.20f),
                new Vector2(0.48f, 0.92f),
                101),
            new(
                FatManSpritePartId.ThighLeft,
                "Bone.Thigh.L",
                new Rect(0.13f, 0.12f, 0.38f, 0.25f),
                new Vector2(0.58f, 0.92f),
                110),
            new(
                FatManSpritePartId.ThighRight,
                "Bone.Thigh.R",
                new Rect(0.49f, 0.12f, 0.38f, 0.25f),
                new Vector2(0.42f, 0.92f),
                111),
            new(
                FatManSpritePartId.Pelvis,
                "Bone.Pelvis",
                new Rect(0.16f, 0.27f, 0.68f, 0.17f),
                new Vector2(0.50f, 0.48f),
                120),
            new(
                FatManSpritePartId.UpperArmLeft,
                "Bone.UpperArm.L",
                new Rect(0.00f, 0.50f, 0.31f, 0.31f),
                new Vector2(0.86f, 0.86f),
                125),
            new(
                FatManSpritePartId.UpperArmRight,
                "Bone.UpperArm.R",
                new Rect(0.69f, 0.50f, 0.31f, 0.31f),
                new Vector2(0.14f, 0.86f),
                126),
            new(
                FatManSpritePartId.Belly,
                "Bone.Belly",
                new Rect(0.10f, 0.34f, 0.80f, 0.29f),
                new Vector2(0.50f, 0.66f),
                130),
            new(
                FatManSpritePartId.Chest,
                "Bone.ChestSoft",
                new Rect(0.10f, 0.55f, 0.80f, 0.26f),
                new Vector2(0.50f, 0.25f),
                140),
            new(
                FatManSpritePartId.ForearmLeft,
                "Bone.Forearm.L",
                new Rect(0.00f, 0.27f, 0.29f, 0.35f),
                new Vector2(0.82f, 0.86f),
                150),
            new(
                FatManSpritePartId.ForearmRight,
                "Bone.Forearm.R",
                new Rect(0.71f, 0.27f, 0.29f, 0.35f),
                new Vector2(0.18f, 0.86f),
                151),
            new(
                FatManSpritePartId.Head,
                "Bone.Head",
                new Rect(0.20f, 0.72f, 0.60f, 0.28f),
                new Vector2(0.50f, 0.10f),
                170)
        };

        private const string DefaultCatalogPath =
            "Characters/FatMan/FatManSpriteCatalog";

        [SerializeField] private string catalogResourcePath =
            DefaultCatalogPath;
        [SerializeField] private bool removeFlatBackground = true;

        private readonly List<CharacterSpritePart> spriteParts = new(16);
        private readonly List<CanvasRenderer> legacyRenderers = new(96);
        private readonly RectInt[] directionBounds = new RectInt[3];

        private CharacterRigController rigController;
        private CharacterSkinController skinController;
        private FatManSpriteSet catalog;
        private Texture2D runtimeTexture;
        private RectTransform visualRoot;
        private Image leftEyelid;
        private Image rightEyelid;
        private Image mouthOverlay;
        private Vector2 headDisplaySize;
        private CharacterFacing lastFacing = (CharacterFacing)(-1);
        private int lastStage = -1;
        private float nextBlinkAt;
        private float blinkUntil;
        private bool buildAttemptLogged;
        private bool ready;

        public bool IsReady =>
            ready &&
            runtimeTexture != null &&
            spriteParts.Count == PartSpecs.Length;
        public int ActiveSpritePartCount => spriteParts.Count;
        public int LoadedDirectionCount =>
            directionBounds[0].width > 0 &&
            directionBounds[1].width > 0 &&
            directionBounds[2].width > 0
                ? 3
                : 0;
        public Texture2D RuntimeTexture => runtimeTexture;

        private void Awake()
        {
            rigController = GetComponent<CharacterRigController>();
            skinController = GetComponent<CharacterSkinController>();
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

            HideLegacyGeometry();
            SyncFacing();
            SyncStage();
            UpdateFaceOverlay();
        }

        private void TryBuild()
        {
            if (rigController == null ||
                skinController == null ||
                rigController.VisualRoot == null ||
                !rigController.HasAppliedSkin ||
                rigController.GetVisibleGraphicCount() < 18)
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
                    "Real Fat Man Sprite Patch 3.2 could not load " +
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
            for (int column = 0; column < 3; column++)
            {
                directionBounds[column] = FindOpaqueBounds(
                    runtimeTexture,
                    column,
                    3);
                if (directionBounds[column].width < 4 ||
                    directionBounds[column].height < 4)
                {
                    LogBuildFailureOnce(
                        $"Fat-man turnaround column {column} has no " +
                        "usable opaque character pixels.");
                    Destroy(runtimeTexture);
                    runtimeTexture = null;
                    return;
                }
            }

            Vector2 displaySize = ResolveDisplaySize();
            for (int i = 0; i < PartSpecs.Length; i++)
            {
                PartSpec spec = PartSpecs[i];
                RectTransform bone =
                    rigController.GetBone(spec.boneName);
                if (bone == null)
                {
                    LogBuildFailureOnce(
                        $"Sprite rig bone is missing: {spec.boneName}");
                    ClearRuntimeObjects();
                    return;
                }

                CharacterSpritePart part = CreatePart(
                    bone,
                    spec,
                    displaySize);
                if (part == null)
                {
                    LogBuildFailureOnce(
                        $"Could not create sprite part {spec.id}.");
                    ClearRuntimeObjects();
                    return;
                }

                spriteParts.Add(part);
            }

            CacheLegacyRenderers();
            CreateFaceOverlay(displaySize);
            ready = true;
            buildAttemptLogged = false;
            lastFacing = (CharacterFacing)(-1);
            lastStage = -1;
            ScheduleBlink();
            SyncFacing();
            SyncStage();
            HideLegacyGeometry();

            Debug.Log(
                "Real Fat Man Sprite Patch 3.2 active: real PNG art, " +
                "12 bone-bound parts, three directions and four stages.",
                this);
        }

        private CharacterSpritePart CreatePart(
            RectTransform bone,
            PartSpec spec,
            Vector2 displaySize)
        {
            GameObject partObject = new(
                $"Sprite.{spec.id}",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasRenderer),
                typeof(Image));
            partObject.layer = gameObject.layer;
            RectTransform partRect =
                partObject.GetComponent<RectTransform>();
            partRect.SetParent(bone, false);

            CharacterSpritePart part =
                partObject.AddComponent<CharacterSpritePart>();
            part.Configure(
                spec.id,
                runtimeTexture,
                spec.crop,
                spec.pivot,
                new Vector2(
                    displaySize.x * spec.crop.width,
                    displaySize.y * spec.crop.height),
                spec.sortingOrder);
            return part;
        }

        private Vector2 ResolveDisplaySize()
        {
            Bounds worldBounds = rigController.GetWorldGeometryBounds();
            if (worldBounds.size.x > 10f &&
                worldBounds.size.y > 10f &&
                visualRoot != null)
            {
                Vector3 localMin =
                    visualRoot.InverseTransformPoint(worldBounds.min);
                Vector3 localMax =
                    visualRoot.InverseTransformPoint(worldBounds.max);
                float width = Mathf.Abs(localMax.x - localMin.x);
                float height = Mathf.Abs(localMax.y - localMin.y);
                if (width > 100f && height > 200f)
                {
                    return new Vector2(
                        Mathf.Clamp(width * 1.04f, 520f, 790f),
                        Mathf.Clamp(height * 1.03f, 900f, 1240f));
                }
            }

            return new Vector2(650f, 1120f);
        }

        private void SyncFacing()
        {
            CharacterFacing facing = rigController.Facing;
            if (facing == lastFacing)
            {
                return;
            }

            int column = catalog.GetColumn(facing);
            RectInt bounds = directionBounds[column];
            bool valid = true;
            for (int i = 0; i < spriteParts.Count; i++)
            {
                valid &= spriteParts[i].ApplyView(bounds);
            }

            if (!valid)
            {
                Debug.LogError(
                    "At least one real fat-man sprite crop failed to build.",
                    this);
            }

            bool back = facing == CharacterFacing.Back;
            bool side =
                facing == CharacterFacing.SideLeft ||
                facing == CharacterFacing.SideRight;
            if (leftEyelid != null && rightEyelid != null)
            {
                leftEyelid.gameObject.SetActive(false);
                rightEyelid.gameObject.SetActive(false);
                if (side)
                {
                    leftEyelid.rectTransform.anchoredPosition =
                        new Vector2(
                            headDisplaySize.x * 0.09f,
                            headDisplaySize.y * 0.54f);
                    rightEyelid.rectTransform.anchoredPosition =
                        leftEyelid.rectTransform.anchoredPosition;
                }
                else
                {
                    leftEyelid.rectTransform.anchoredPosition =
                        new Vector2(
                            -headDisplaySize.x * 0.13f,
                            headDisplaySize.y * 0.54f);
                    rightEyelid.rectTransform.anchoredPosition =
                        new Vector2(
                            headDisplaySize.x * 0.13f,
                            headDisplaySize.y * 0.54f);
                }
            }

            if (mouthOverlay != null)
            {
                mouthOverlay.gameObject.SetActive(false);
                mouthOverlay.rectTransform.anchoredPosition =
                    new Vector2(
                        side ? headDisplaySize.x * 0.08f : 0f,
                        headDisplaySize.y * 0.31f);
            }

            if (back)
            {
                blinkUntil = 0f;
            }

            lastFacing = facing;
        }

        private void SyncStage()
        {
            int stage = Mathf.Max(0, skinController.CurrentArtIndex);
            if (stage == lastStage)
            {
                return;
            }

            float scale = catalog.GetStageScale(stage);
            for (int i = 0; i < spriteParts.Count; i++)
            {
                spriteParts[i].ApplyStageScale(scale);
            }

            if (leftEyelid != null)
            {
                leftEyelid.rectTransform.localScale =
                    new Vector3(scale, scale, 1f);
            }
            if (rightEyelid != null)
            {
                rightEyelid.rectTransform.localScale =
                    new Vector3(scale, scale, 1f);
            }
            if (mouthOverlay != null)
            {
                mouthOverlay.rectTransform.localScale =
                    new Vector3(scale, scale, 1f);
            }

            lastStage = stage;
        }

        private void CreateFaceOverlay(Vector2 displaySize)
        {
            RectTransform headBone =
                rigController.GetBone("Bone.Head");
            if (headBone == null)
            {
                return;
            }

            headDisplaySize = new Vector2(
                displaySize.x * 0.60f,
                displaySize.y * 0.28f);
            Color skinColor = SampleFaceColor();

            leftEyelid = CreateSolidImage(
                headBone,
                "SpriteFace.Eyelid.L",
                skinColor,
                new Vector2(
                    headDisplaySize.x * 0.14f,
                    Mathf.Max(4f, headDisplaySize.y * 0.018f)),
                220);
            rightEyelid = CreateSolidImage(
                headBone,
                "SpriteFace.Eyelid.R",
                skinColor,
                new Vector2(
                    headDisplaySize.x * 0.14f,
                    Mathf.Max(4f, headDisplaySize.y * 0.018f)),
                221);
            mouthOverlay = CreateSolidImage(
                headBone,
                "SpriteFace.Mouth",
                new Color(0.16f, 0.07f, 0.06f, 0.92f),
                new Vector2(
                    headDisplaySize.x * 0.16f,
                    Mathf.Max(4f, headDisplaySize.y * 0.022f)),
                230);

            leftEyelid.gameObject.SetActive(false);
            rightEyelid.gameObject.SetActive(false);
            mouthOverlay.gameObject.SetActive(false);
        }

        private static Image CreateSolidImage(
            RectTransform parent,
            string objectName,
            Color color,
            Vector2 size,
            int sortingOrder)
        {
            GameObject target = new(
                objectName,
                typeof(RectTransform),
                typeof(Canvas),
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

            Canvas canvas = target.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            Image image = target.GetComponent<Image>();
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            image.maskable = false;
            image.color = color;
            return image;
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
                 rigController.ActiveAction ==
                 CharacterRoutineAction.Yawn ||
                 rigController.ActiveAction ==
                 CharacterRoutineAction.Flex);
            mouthOverlay.gameObject.SetActive(expressive);
            if (expressive)
            {
                bool yawn =
                    rigController.ActiveAction ==
                    CharacterRoutineAction.Yawn;
                mouthOverlay.rectTransform.sizeDelta =
                    new Vector2(
                        headDisplaySize.x * (yawn ? 0.17f : 0.20f),
                        headDisplaySize.y * (yawn ? 0.10f : 0.035f));
            }
        }

        private void ScheduleBlink()
        {
            nextBlinkAt =
                Time.unscaledTime +
                UnityEngine.Random.Range(2.1f, 5.2f);
        }

        private void CacheLegacyRenderers()
        {
            legacyRenderers.Clear();

            CharacterMeshGraphic[] meshes =
                GetComponentsInChildren<CharacterMeshGraphic>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                CanvasRenderer renderer =
                    meshes[i] != null
                        ? meshes[i].canvasRenderer
                        : null;
                if (renderer != null &&
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
                    surfaces[i] != null
                        ? surfaces[i].canvasRenderer
                        : null;
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
        }

        private Color SampleFaceColor()
        {
            RectInt frontBounds = directionBounds[
                catalog.GetColumn(CharacterFacing.Front)];
            int centerX = frontBounds.x +
                          Mathf.RoundToInt(
                              frontBounds.width * 0.5f);
            int centerY = frontBounds.y +
                          Mathf.RoundToInt(
                              frontBounds.height * 0.82f);

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
                        Color sample =
                            runtimeTexture.GetPixel(sampleX, sampleY);
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

            Color32[] output =
                new Color32[sourcePixels.Length];
            Array.Copy(sourcePixels, output, sourcePixels.Length);

            if (removeBackground && output.Length > 0)
            {
                Color32 cornerA = output[0];
                Color32 cornerB =
                    output[Mathf.Max(0, source.width - 1)];
                Color32 cornerC =
                    output[Mathf.Max(
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
                name = "FatManTurnaround.Runtime",
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
            int startX =
                Mathf.FloorToInt(
                    texture.width *
                    (column / (float)columnCount));
            int endX =
                Mathf.FloorToInt(
                    texture.width *
                    ((column + 1f) / columnCount)) - 1;
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
            for (int i = 0; i < spriteParts.Count; i++)
            {
                CharacterSpritePart part = spriteParts[i];
                if (part != null)
                {
                    Destroy(part.gameObject);
                }
            }

            spriteParts.Clear();
            if (leftEyelid != null)
            {
                Destroy(leftEyelid.gameObject);
            }
            if (rightEyelid != null)
            {
                Destroy(rightEyelid.gameObject);
            }
            if (mouthOverlay != null)
            {
                Destroy(mouthOverlay.gameObject);
            }

            leftEyelid = null;
            rightEyelid = null;
            mouthOverlay = null;

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
