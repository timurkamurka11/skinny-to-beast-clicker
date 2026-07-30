using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Builds a complete full-canvas production-candidate layer set from the
    /// locked quality master. Repository-generated masks are preferred, then
    /// deterministic texture-preserving continuations and painted face poses
    /// complete the areas that do not exist in the neutral source. The result
    /// always remains human-review-only and cannot unlock Patch 4 activation.
    /// </summary>
    public static class Patch4MaskDrivenLayerBaker
    {
        public const string MasterPath =
            Patch4AdobeMaskDownloader.SourceRoot +
            "/FatMan_NeutralFront_Master.png";
        public const string LayerRoot =
            "Assets/GameWorkPatch4/Art/Character/FatMan/Layers";
        public const string DraftMetadataPath =
            "Assets/GameWorkPatch4/Art/Character/FatMan/layer-draft-status.json";

        private const int Width = 1024;
        private const int Height = 1536;

        private enum Side
        {
            Any,
            Left,
            Right
        }

        private enum Shape
        {
            Rectangle,
            Ellipse
        }

        private sealed class ImageData
        {
            public int width;
            public int height;
            public Color32[] pixels;
        }

        private sealed class Spec
        {
            public string path;
            public Rect region;
            public Side side;
            public Shape shape;
            public string[] masks;
            public bool manual;
            public string reason;
        }

        private readonly struct JointContinuation
        {
            public readonly Vector2 normalizedTopPoint;
            public readonly Vector2 radiusPixels;

            public JointContinuation(
                float x,
                float y,
                float radiusX,
                float radiusY)
            {
                normalizedTopPoint = new Vector2(x, y);
                radiusPixels = new Vector2(radiusX, radiusY);
            }
        }

        private static readonly Rect LeftEyePatch =
            new(.438f, .164f, .063f, .045f);
        private static readonly Rect RightEyePatch =
            new(.499f, .164f, .063f, .045f);
        private static readonly Rect MouthPatch =
            new(.452f, .198f, .096f, .052f);

        [MenuItem("Tools/GameWork/Patch 4.0/Art/Bake Draft Layer Pack")]
        public static void BakeDraftLayerPack()
        {
            ImageData master = LoadImage(MasterPath);
            if (master == null)
            {
                Debug.LogError(
                    "Patch 4 master is missing. Restore Repository Sources first.");
                return;
            }

            if (master.width != Width || master.height != Height)
            {
                Debug.LogError(
                    $"Patch 4 master must be {Width}×{Height}; actual " +
                    $"{master.width}×{master.height}.");
                return;
            }

            Dictionary<string, ImageData> masks = LoadMasks();
            List<Spec> specs = BuildSpecs();
            EnsureFolder(LayerRoot);
            List<string> manualItems = new();
            List<string> warnings = new();

            try
            {
                for (int i = 0; i < specs.Count; i++)
                {
                    Spec spec = specs[i];
                    EditorUtility.DisplayProgressBar(
                        "GameWork Patch 4.0",
                        "Baking " + spec.path,
                        (float)i / Mathf.Max(1, specs.Count));

                    Color32[] pixels = Bake(master, masks, spec, warnings);
                    WriteLayer(spec.path, pixels);
                    if (spec.manual)
                    {
                        manualItems.Add(
                            spec.path +
                            (string.IsNullOrWhiteSpace(spec.reason)
                                ? string.Empty
                                : " — " + spec.reason));
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            WriteDraftStatus(specs.Count, masks.Count, manualItems, warnings);
            AssetDatabase.Refresh();
            Patch4LayerCatalogBuilder.RebuildCatalog();
            Patch4DraftLayerValidator.ValidateAndWriteReport();
            Debug.Log(
                $"Patch 4 created {specs.Count} production-candidate layers " +
                $"from {masks.Count} repository masks, painted face poses and " +
                "texture-preserving joint continuations. Human review and " +
                "production activation remain locked.");
        }

        private static List<Spec> BuildSpecs()
        {
            List<Spec> result = new();

            result.Add(S("Body/TorsoBase", R(.20f,.225f,.60f,.29f), "upperClothes", manual: false));
            result.Add(S("Body/BellyFront", R(.26f,.315f,.48f,.20f), "upperClothes", manual: false));
            result.Add(S("Body/ChestSoft", R(.27f,.235f,.46f,.15f), "upperClothes", manual: false));
            result.Add(S("Body/Neck", R(.39f,.195f,.22f,.105f), "neck", manual: false));

            result.Add(S("Head/HeadBase", R(.38f,.09f,.24f,.18f), "hair,faceBase,ears", manual: false));
            result.Add(S("Head/EarL", R(.39f,.16f,.12f,.07f), "ears", Side.Left, reason: "repaint hidden ear attachment"));
            result.Add(S("Head/EarR", R(.49f,.16f,.12f,.07f), "ears", Side.Right, reason: "repaint hidden ear attachment"));

            result.Add(S("Face/BrowL", R(.43f,.15f,.09f,.035f), "eyebrows", Side.Left, manual: false));
            result.Add(S("Face/BrowR", R(.48f,.15f,.09f,.035f), "eyebrows", Side.Right, manual: false));
            result.Add(S("Face/EyeWhiteL", LeftEyePatch, side: Side.Left, manual: false));
            result.Add(S("Face/EyeWhiteR", RightEyePatch, side: Side.Right, manual: false));
            result.Add(S("Face/IrisL", R(.469f,.174f,.017f,.025f), side: Side.Left, shape: Shape.Ellipse, manual: false));
            result.Add(S("Face/IrisR", R(.515f,.174f,.017f,.025f), side: Side.Right, shape: Shape.Ellipse, manual: false));
            result.Add(S("Face/LidL", LeftEyePatch, side: Side.Left, manual: false));
            result.Add(S("Face/LidR", RightEyePatch, side: Side.Right, manual: false));
            result.Add(S("Face/Nose", R(.467f,.158f,.066f,.055f), "nose", manual: false));
            result.Add(S("Face/MouthClosed", MouthPatch, manual: false));
            result.Add(S("Face/MouthOpen", MouthPatch, manual: false));
            result.Add(S("Face/MouthSmile", MouthPatch, manual: false));
            result.Add(S("Face/CheekL", R(.413f,.184f,.082f,.058f), side: Side.Left, shape: Shape.Ellipse, manual: false));
            result.Add(S("Face/CheekR", R(.505f,.184f,.082f,.058f), side: Side.Right, shape: Shape.Ellipse, manual: false));

            result.Add(S("ArmL/Upper", R(.185f,.245f,.205f,.185f), side: Side.Left, manual: false));
            result.Add(S("ArmL/Forearm", R(.17f,.375f,.19f,.17f), side: Side.Left, manual: false));
            result.Add(S("ArmL/Hand", R(.19f,.455f,.17f,.105f), "hands", Side.Left, manual: false));
            result.Add(S("ArmR/Upper", R(.61f,.245f,.205f,.185f), side: Side.Right, manual: false));
            result.Add(S("ArmR/Forearm", R(.64f,.375f,.19f,.17f), side: Side.Right, manual: false));
            result.Add(S("ArmR/Hand", R(.64f,.455f,.17f,.105f), "hands", Side.Right, manual: false));

            result.Add(S("LegL/Thigh", R(.275f,.465f,.235f,.20f), "lowerClothes", Side.Left, manual: false));
            result.Add(S("LegL/Shin", R(.255f,.59f,.25f,.18f), "lowerClothes", Side.Left, manual: false));
            result.Add(S("LegL/Foot", R(.25f,.715f,.255f,.095f), "shoes", Side.Left, manual: false));
            result.Add(S("LegR/Thigh", R(.49f,.465f,.235f,.20f), "lowerClothes", Side.Right, manual: false));
            result.Add(S("LegR/Shin", R(.495f,.59f,.25f,.18f), "lowerClothes", Side.Right, manual: false));
            result.Add(S("LegR/Foot", R(.495f,.715f,.255f,.095f), "shoes", Side.Right, manual: false));

            result.Add(S("Clothes/ShirtBase", R(.20f,.225f,.60f,.295f), "upperClothes", reason: "repair armholes and hidden torso paint"));
            result.Add(S("Clothes/ShirtBellyOverlay", R(.255f,.32f,.49f,.205f), "upperClothes", manual: false));
            result.Add(S("Clothes/Bottoms", R(.275f,.465f,.45f,.30f), "lowerClothes", reason: "reference layer only; legs need hidden art"));
            result.Add(S("Clothes/Shoes", R(.25f,.715f,.50f,.10f), "shoes", reason: "pair reference only"));

            result.Add(S("FX/Sweat", R(.56f,.205f,.025f,.045f), shape: Shape.Ellipse, reason: "paint optional sweat sprite"));
            result.Add(S("FX/ImpactFold", R(.39f,.39f,.22f,.055f), reason: "paint dedicated tap-impact fold"));
            result.Add(S("FX/Shadow", R(.27f,.79f,.46f,.045f), shape: Shape.Ellipse, reason: "paint dedicated soft ground shadow"));

            return result;
        }

        private static Spec S(
            string path,
            Rect region,
            string maskCsv = "",
            Side side = Side.Any,
            Shape shape = Shape.Rectangle,
            bool manual = true,
            string reason = "")
        {
            return new Spec
            {
                path = path,
                region = region,
                side = side,
                shape = shape,
                manual = manual,
                reason = reason ?? string.Empty,
                masks = string.IsNullOrWhiteSpace(maskCsv)
                    ? Array.Empty<string>()
                    : maskCsv.Split(',')
            };
        }

        private static Rect R(float x, float y, float width, float height)
        {
            return new Rect(x, y, width, height);
        }

        private static Dictionary<string, ImageData> LoadMasks()
        {
            Dictionary<string, ImageData> result = new(StringComparer.OrdinalIgnoreCase);
            LoadMask(result, "hair", "Mask_Hair.png");
            LoadMask(result, "faceBase", "Mask_FaceBase.png");
            LoadMask(result, "eyebrows", "Mask_Eyebrows.png");
            LoadMask(result, "nose", "Mask_Nose.png");
            LoadMask(result, "ears", "Mask_Ears.png");
            LoadMask(result, "neck", "Mask_Neck.png");
            LoadMask(result, "upperClothes", "Mask_UpperClothes.png");
            LoadMask(result, "lowerClothes", "Mask_LowerClothes.png");
            LoadMask(result, "hands", "Mask_Hands.png");
            LoadMask(result, "shoes", "Mask_Shoes.png");
            return result;
        }

        private static void LoadMask(
            IDictionary<string, ImageData> target,
            string id,
            string fileName)
        {
            string path = Patch4AdobeMaskDownloader.DownloadedMaskRoot + "/" + fileName;
            ImageData image = LoadImage(path);
            if (image != null && image.width == Width && image.height == Height)
            {
                target[id] = image;
            }
        }

        private static Color32[] Bake(
            ImageData master,
            IReadOnlyDictionary<string, ImageData> masks,
            Spec spec,
            ICollection<string> warnings)
        {
            Color32[] result = new Color32[master.pixels.Length];
            bool requestedMasks = spec.masks.Length > 0;
            bool foundMask = false;

            for (int y = 0; y < Height; y++)
            {
                float topY = 1f - (y + .5f) / Height;
                for (int x = 0; x < Width; x++)
                {
                    int index = y * Width + x;
                    Color32 source = master.pixels[index];
                    if (source.a == 0)
                    {
                        continue;
                    }

                    float nx = (x + .5f) / Width;
                    if (!MatchesSide(nx, spec.side) ||
                        !Contains(spec.region, spec.shape, nx, topY))
                    {
                        continue;
                    }

                    int maskAlpha = requestedMasks ? 0 : 255;
                    for (int i = 0; i < spec.masks.Length; i++)
                    {
                        if (!masks.TryGetValue(spec.masks[i], out ImageData mask))
                        {
                            continue;
                        }

                        foundMask = true;
                        Color32 maskPixel = mask.pixels[index];
                        int value = Mathf.Max(maskPixel.r, Mathf.Max(maskPixel.g, maskPixel.b));
                        maskAlpha = Mathf.Max(maskAlpha, value);
                    }

                    if (maskAlpha < 8)
                    {
                        continue;
                    }

                    source.a = (byte)(source.a * maskAlpha / 255);
                    result[index] = source;
                }
            }

            ApplyProductionArtwork(master, spec, result);

            if (requestedMasks && !foundMask)
            {
                warnings.Add(
                    spec.path +
                    " used geometry because its repository mask was unavailable.");
            }

            return result;
        }

        private static void ApplyProductionArtwork(
            ImageData master,
            Spec spec,
            Color32[] result)
        {
            if (string.Equals(spec.path, "FX/Shadow", StringComparison.Ordinal))
            {
                PaintSyntheticShadow(spec, result);
            }

            ApplyFacialArtwork(master, spec.path, result);

            JointContinuation[] continuations =
                ResolveJointContinuations(spec.path);
            for (int i = 0; i < continuations.Length; i++)
            {
                PaintJointContinuation(
                    master,
                    result,
                    continuations[i]);
            }
        }

        private static void PaintSyntheticShadow(Spec spec, Color32[] result)
        {
            for (int y = 0; y < Height; y++)
            {
                float topY = 1f - (y + .5f) / Height;
                for (int x = 0; x < Width; x++)
                {
                    float nx = (x + .5f) / Width;
                    if (!Contains(spec.region, spec.shape, nx, topY))
                    {
                        continue;
                    }

                    float dx =
                        (nx - spec.region.center.x) /
                        Mathf.Max(.0001f, spec.region.width * .5f);
                    float dy =
                        (topY - spec.region.center.y) /
                        Mathf.Max(.0001f, spec.region.height * .5f);
                    float falloff = Mathf.Clamp01(1f - dx * dx - dy * dy);
                    byte alpha = (byte)Mathf.RoundToInt(
                        Mathf.Lerp(32f, 96f, falloff));
                    result[y * Width + x] =
                        new Color32(24, 26, 31, alpha);
                }
            }
        }

        private static void ApplyFacialArtwork(
            ImageData master,
            string path,
            Color32[] result)
        {
            switch (path)
            {
                case "Head/HeadBase":
                    PaintSkinUnderlay(
                        master,
                        result,
                        LeftEyePatch,
                        overlayOnly: false);
                    PaintSkinUnderlay(
                        master,
                        result,
                        RightEyePatch,
                        overlayOnly: false);
                    PaintSkinUnderlay(
                        master,
                        result,
                        MouthPatch,
                        overlayOnly: false);
                    break;

                case "Face/EyeWhiteL":
                    CopyMasterPatch(master, result, LeftEyePatch);
                    break;

                case "Face/EyeWhiteR":
                    CopyMasterPatch(master, result, RightEyePatch);
                    break;

                case "Face/LidL":
                    ClearLayer(result);
                    PaintSkinUnderlay(
                        master,
                        result,
                        LeftEyePatch,
                        overlayOnly: true);
                    PaintClosedLid(result, LeftEyePatch);
                    break;

                case "Face/LidR":
                    ClearLayer(result);
                    PaintSkinUnderlay(
                        master,
                        result,
                        RightEyePatch,
                        overlayOnly: true);
                    PaintClosedLid(result, RightEyePatch);
                    break;

                case "Face/MouthClosed":
                    CopyMasterPatch(master, result, MouthPatch);
                    break;

                case "Face/MouthOpen":
                    ClearLayer(result);
                    PaintSkinUnderlay(
                        master,
                        result,
                        MouthPatch,
                        overlayOnly: true);
                    PaintOpenMouth(result);
                    break;

                case "Face/MouthSmile":
                    ClearLayer(result);
                    PaintSkinUnderlay(
                        master,
                        result,
                        MouthPatch,
                        overlayOnly: true);
                    PaintSmile(result);
                    break;
            }
        }

        private static void CopyMasterPatch(
            ImageData master,
            Color32[] result,
            Rect patch)
        {
            ClearLayer(result);
            GetPixelBounds(
                patch,
                out int minX,
                out int maxX,
                out int minY,
                out int maxY);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int index = y * Width + x;
                    result[index] = master.pixels[index];
                }
            }
        }

        private static void PaintSkinUnderlay(
            ImageData master,
            Color32[] result,
            Rect patch,
            bool overlayOnly)
        {
            GetPixelBounds(
                patch,
                out int minX,
                out int maxX,
                out int minY,
                out int maxY);

            const float featherPixels = 6f;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float edgeDistance = Mathf.Min(
                        Mathf.Min(x - minX, maxX - x),
                        Mathf.Min(y - minY, maxY - y));
                    float coverage = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(edgeDistance / featherPixels));
                    if (coverage <= 0f)
                    {
                        continue;
                    }

                    int index = y * Width + x;
                    Color32 skin = SampleSkinField(
                        master,
                        patch,
                        x,
                        y,
                        minX,
                        maxX,
                        minY,
                        maxY);

                    if (overlayOnly)
                    {
                        skin.a = ScaleAlpha(255, coverage);
                        result[index] = BlendOver(
                            result[index],
                            skin);
                    }
                    else
                    {
                        Color32 current = result[index];
                        if (current.a <= 8)
                        {
                            continue;
                        }

                        skin.a = current.a;
                        result[index] = LerpColor(
                            current,
                            skin,
                            coverage);
                    }
                }
            }
        }

        private static Color32 SampleSkinField(
            ImageData master,
            Rect patch,
            int x,
            int y,
            int minX,
            int maxX,
            int minY,
            int maxY)
        {
            const int padding = 14;
            int belowY = Mathf.Clamp(minY - padding, 0, Height - 1);
            int aboveY = Mathf.Clamp(maxY + padding, 0, Height - 1);
            int leftX = Mathf.Clamp(minX - padding, 0, Width - 1);
            int rightX = Mathf.Clamp(maxX + padding, 0, Width - 1);

            Color32 below = master.pixels[belowY * Width + x];
            Color32 above = master.pixels[aboveY * Width + x];
            Color32 left = master.pixels[y * Width + leftX];
            Color32 right = master.pixels[y * Width + rightX];

            int red = 0;
            int green = 0;
            int blue = 0;
            int weight = 0;
            AddSkinSample(below, 4, ref red, ref green, ref blue, ref weight);
            AddSkinSample(above, 2, ref red, ref green, ref blue, ref weight);
            AddSkinSample(left, 1, ref red, ref green, ref blue, ref weight);
            AddSkinSample(right, 1, ref red, ref green, ref blue, ref weight);

            if (weight == 0)
            {
                return FindNearestVisibleColor(master, x, y);
            }

            int grain =
                ((x * 17 + y * 29 + Mathf.RoundToInt(
                    patch.center.x * 1000f)) & 7) - 3;
            return new Color32(
                ClampByte(red / weight + grain),
                ClampByte(green / weight + grain / 2),
                ClampByte(blue / weight),
                255);
        }

        private static void AddSkinSample(
            Color32 sample,
            int sampleWeight,
            ref int red,
            ref int green,
            ref int blue,
            ref int weight)
        {
            if (sample.a <= 8)
            {
                return;
            }

            red += sample.r * sampleWeight;
            green += sample.g * sampleWeight;
            blue += sample.b * sampleWeight;
            weight += sampleWeight;
        }

        private static void PaintClosedLid(
            Color32[] result,
            Rect eyePatch)
        {
            float left = eyePatch.xMin + .007f;
            float right = eyePatch.xMax - .007f;
            float center = eyePatch.center.x;
            float edgeY = eyePatch.yMin + eyePatch.height * .51f;
            float centerY = edgeY + .007f;

            DrawQuadraticStroke(
                result,
                ToPixel(left, edgeY),
                ToPixel(center, centerY),
                ToPixel(right, edgeY),
                new Color32(183, 105, 88, 150),
                7f);
            DrawQuadraticStroke(
                result,
                ToPixel(left, edgeY),
                ToPixel(center, centerY),
                ToPixel(right, edgeY),
                new Color32(68, 43, 46, 235),
                3.2f);
            DrawQuadraticStroke(
                result,
                ToPixel(left + .004f, edgeY - .004f),
                ToPixel(center, centerY - .004f),
                ToPixel(right - .004f, edgeY - .004f),
                new Color32(245, 176, 132, 105),
                1.6f);
        }

        private static void PaintOpenMouth(Color32[] result)
        {
            Vector2 center = ToPixel(.5f, .2215f);
            DrawSoftEllipse(
                result,
                center,
                30f,
                22f,
                new Color32(143, 72, 69, 235),
                2.5f);
            DrawSoftEllipse(
                result,
                center,
                25f,
                18f,
                new Color32(58, 32, 40, 255),
                2f);
            DrawSoftEllipse(
                result,
                ToPixel(.5f, .2145f),
                17f,
                6f,
                new Color32(238, 226, 207, 245),
                1.5f);
            DrawSoftEllipse(
                result,
                ToPixel(.5f, .229f),
                16f,
                7f,
                new Color32(177, 83, 88, 225),
                1.5f);
            DrawQuadraticStroke(
                result,
                ToPixel(.478f, .2075f),
                ToPixel(.5f, .2035f),
                ToPixel(.522f, .2075f),
                new Color32(235, 145, 120, 155),
                2.2f);
        }

        private static void PaintSmile(Color32[] result)
        {
            Vector2 center = ToPixel(.5f, .218f);
            DrawSoftEllipse(
                result,
                center,
                36f,
                15f,
                new Color32(142, 72, 70, 235),
                2.5f);
            DrawSoftEllipse(
                result,
                ToPixel(.5f, .219f),
                31f,
                11f,
                new Color32(62, 34, 42, 255),
                2f);
            DrawSoftEllipse(
                result,
                ToPixel(.5f, .2145f),
                24f,
                6f,
                new Color32(239, 227, 209, 245),
                1.5f);
            DrawSoftEllipse(
                result,
                ToPixel(.5f, .2245f),
                19f,
                4.5f,
                new Color32(177, 84, 89, 210),
                1.5f);
            DrawQuadraticStroke(
                result,
                ToPixel(.466f, .213f),
                ToPixel(.5f, .232f),
                ToPixel(.534f, .213f),
                new Color32(83, 45, 49, 210),
                2.4f);
        }

        private static void DrawQuadraticStroke(
            Color32[] result,
            Vector2 start,
            Vector2 control,
            Vector2 end,
            Color32 color,
            float width)
        {
            const int steps = 48;
            float radius = Mathf.Max(.5f, width * .5f);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float inverse = 1f - t;
                Vector2 point =
                    inverse * inverse * start +
                    2f * inverse * t * control +
                    t * t * end;
                DrawSoftCircle(result, point, radius, color);
            }
        }

        private static void DrawSoftCircle(
            Color32[] result,
            Vector2 center,
            float radius,
            Color32 color)
        {
            DrawSoftEllipse(
                result,
                center,
                radius,
                radius,
                color,
                1f);
        }

        private static void DrawSoftEllipse(
            Color32[] result,
            Vector2 center,
            float radiusX,
            float radiusY,
            Color32 color,
            float featherPixels)
        {
            int minX = Mathf.Max(
                0,
                Mathf.FloorToInt(center.x - radiusX - 1f));
            int maxX = Mathf.Min(
                Width - 1,
                Mathf.CeilToInt(center.x + radiusX + 1f));
            int minY = Mathf.Max(
                0,
                Mathf.FloorToInt(center.y - radiusY - 1f));
            int maxY = Mathf.Min(
                Height - 1,
                Mathf.CeilToInt(center.y + radiusY + 1f));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - center.x) /
                        Mathf.Max(.001f, radiusX);
                    float dy = (y - center.y) /
                        Mathf.Max(.001f, radiusY);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance >= 1f)
                    {
                        continue;
                    }

                    float edgePixels =
                        (1f - distance) *
                        Mathf.Min(radiusX, radiusY);
                    float coverage = Mathf.Clamp01(
                        edgePixels / Mathf.Max(.5f, featherPixels));
                    Color32 paint = color;
                    paint.a = ScaleAlpha(color.a, coverage);
                    int index = y * Width + x;
                    result[index] = BlendOver(
                        result[index],
                        paint);
                }
            }
        }

        private static JointContinuation[] ResolveJointContinuations(
            string path)
        {
            switch (path)
            {
                case "Body/Neck":
                case "Head/HeadBase":
                    return new[] { J(.5f, .225f, 24f, 22f) };

                case "Body/ChestSoft":
                    return new[]
                    {
                        J(.34f, .285f, 30f, 24f),
                        J(.66f, .285f, 30f, 24f)
                    };

                case "ArmL/Upper":
                    return new[]
                    {
                        J(.34f, .285f, 30f, 24f),
                        J(.285f, .405f, 24f, 30f)
                    };

                case "ArmR/Upper":
                    return new[]
                    {
                        J(.66f, .285f, 30f, 24f),
                        J(.715f, .405f, 24f, 30f)
                    };

                case "ArmL/Forearm":
                    return new[]
                    {
                        J(.285f, .405f, 24f, 30f),
                        J(.255f, .495f, 18f, 24f)
                    };

                case "ArmR/Forearm":
                    return new[]
                    {
                        J(.715f, .405f, 24f, 30f),
                        J(.745f, .495f, 18f, 24f)
                    };

                case "ArmL/Hand":
                    return new[] { J(.255f, .495f, 18f, 24f) };

                case "ArmR/Hand":
                    return new[] { J(.745f, .495f, 18f, 24f) };

                case "Body/TorsoBase":
                    return new[]
                    {
                        J(.42f, .505f, 30f, 24f),
                        J(.58f, .505f, 30f, 24f)
                    };

                case "LegL/Thigh":
                    return new[]
                    {
                        J(.42f, .505f, 30f, 24f),
                        J(.40f, .625f, 26f, 30f)
                    };

                case "LegR/Thigh":
                    return new[]
                    {
                        J(.58f, .505f, 30f, 24f),
                        J(.60f, .625f, 26f, 30f)
                    };

                case "LegL/Shin":
                    return new[]
                    {
                        J(.40f, .625f, 26f, 30f),
                        J(.385f, .735f, 22f, 26f)
                    };

                case "LegR/Shin":
                    return new[]
                    {
                        J(.60f, .625f, 26f, 30f),
                        J(.615f, .735f, 22f, 26f)
                    };

                case "LegL/Foot":
                    return new[] { J(.385f, .735f, 22f, 26f) };

                case "LegR/Foot":
                    return new[] { J(.615f, .735f, 22f, 26f) };

                case "Body/BellyFront":
                case "Clothes/ShirtBellyOverlay":
                    return new[] { J(.5f, .48f, 36f, 22f) };

                default:
                    return Array.Empty<JointContinuation>();
            }
        }

        private static JointContinuation J(
            float x,
            float y,
            float radiusX,
            float radiusY)
        {
            return new JointContinuation(
                x,
                y,
                radiusX,
                radiusY);
        }

        private static void PaintJointContinuation(
            ImageData master,
            Color32[] result,
            JointContinuation continuation)
        {
            const int outsideFeatherPixels = 3;
            int centerX = Mathf.Clamp(
                Mathf.RoundToInt(
                    continuation.normalizedTopPoint.x * Width),
                0,
                Width - 1);
            int centerY = Mathf.Clamp(
                Mathf.RoundToInt(
                    Height -
                    continuation.normalizedTopPoint.y * Height),
                0,
                Height - 1);

            float radiusX = continuation.radiusPixels.x;
            float radiusY = continuation.radiusPixels.y;
            int minX = Mathf.Max(
                0,
                Mathf.FloorToInt(centerX - radiusX));
            int maxX = Mathf.Min(
                Width - 1,
                Mathf.CeilToInt(centerX + radiusX));
            int minY = Mathf.Max(
                0,
                Mathf.FloorToInt(centerY - radiusY));
            int maxY = Mathf.Min(
                Height - 1,
                Mathf.CeilToInt(centerY + radiusY));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - centerX) /
                        Mathf.Max(1f, radiusX);
                    float dy = (y - centerY) /
                        Mathf.Max(1f, radiusY);
                    if (dx * dx + dy * dy > 1f)
                    {
                        continue;
                    }

                    int index = y * Width + x;
                    Color32 color = master.pixels[index];
                    if (color.a > 8)
                    {
                        result[index] = color;
                        continue;
                    }

                    if (!TryFindNearestVisibleColor(
                        master,
                        x,
                        y,
                        outsideFeatherPixels,
                        out Color32 sample,
                        out int distanceSquared))
                    {
                        continue;
                    }

                    float distance = Mathf.Sqrt(distanceSquared);
                    float coverage = Mathf.Clamp01(
                        1f -
                        (distance - 1f) /
                        Mathf.Max(1f, outsideFeatherPixels));
                    sample.a = ScaleAlpha(112, coverage);
                    result[index] = BlendOver(
                        result[index],
                        sample);
                }
            }
        }

        private static bool TryFindNearestVisibleColor(
            ImageData master,
            int centerX,
            int centerY,
            int searchRadius,
            out Color32 color,
            out int distanceSquared)
        {
            color = default;
            distanceSquared = int.MaxValue;
            int minX = Mathf.Max(0, centerX - searchRadius);
            int maxX = Mathf.Min(Width - 1, centerX + searchRadius);
            int minY = Mathf.Max(0, centerY - searchRadius);
            int maxY = Mathf.Min(Height - 1, centerY + searchRadius);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Color32 candidate = master.pixels[y * Width + x];
                    if (candidate.a <= 8)
                    {
                        continue;
                    }

                    int dx = x - centerX;
                    int dy = y - centerY;
                    int candidateDistance = dx * dx + dy * dy;
                    if (candidateDistance >= distanceSquared)
                    {
                        continue;
                    }

                    color = candidate;
                    distanceSquared = candidateDistance;
                }
            }

            return distanceSquared != int.MaxValue;
        }

        private static Color32 FindNearestVisibleColor(
            ImageData master,
            int centerX,
            int centerY)
        {
            const int searchRadius = 96;
            Color32 best = new Color32(128, 128, 128, 255);
            int bestDistance = int.MaxValue;
            int minX = Mathf.Max(0, centerX - searchRadius);
            int maxX = Mathf.Min(Width - 1, centerX + searchRadius);
            int minY = Mathf.Max(0, centerY - searchRadius);
            int maxY = Mathf.Min(Height - 1, centerY + searchRadius);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Color32 candidate = master.pixels[y * Width + x];
                    if (candidate.a <= 8)
                    {
                        continue;
                    }

                    int dx = x - centerX;
                    int dy = y - centerY;
                    int distance = dx * dx + dy * dy;
                    if (distance >= bestDistance)
                    {
                        continue;
                    }

                    best = candidate;
                    bestDistance = distance;
                }
            }

            best.a = 255;
            return best;
        }

        private static void GetPixelBounds(
            Rect patch,
            out int minX,
            out int maxX,
            out int minY,
            out int maxY)
        {
            minX = Mathf.Clamp(
                Mathf.FloorToInt(patch.xMin * Width),
                0,
                Width - 1);
            maxX = Mathf.Clamp(
                Mathf.CeilToInt(patch.xMax * Width) - 1,
                0,
                Width - 1);
            minY = Mathf.Clamp(
                Mathf.FloorToInt(Height - patch.yMax * Height),
                0,
                Height - 1);
            maxY = Mathf.Clamp(
                Mathf.CeilToInt(Height - patch.yMin * Height) - 1,
                0,
                Height - 1);
        }

        private static Vector2 ToPixel(float normalizedX, float topY)
        {
            return new Vector2(
                normalizedX * Width,
                Height - topY * Height);
        }

        private static void ClearLayer(Color32[] result)
        {
            Array.Clear(result, 0, result.Length);
        }

        private static Color32 LerpColor(
            Color32 from,
            Color32 to,
            float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                ClampByte(Mathf.RoundToInt(
                    Mathf.Lerp(from.r, to.r, t))),
                ClampByte(Mathf.RoundToInt(
                    Mathf.Lerp(from.g, to.g, t))),
                ClampByte(Mathf.RoundToInt(
                    Mathf.Lerp(from.b, to.b, t))),
                ClampByte(Mathf.RoundToInt(
                    Mathf.Lerp(from.a, to.a, t))));
        }

        private static Color32 BlendOver(
            Color32 destination,
            Color32 source)
        {
            int sourceAlpha = source.a;
            if (sourceAlpha <= 0)
            {
                return destination;
            }

            if (sourceAlpha >= 255)
            {
                return source;
            }

            int destinationAlpha = destination.a;
            int inverseSourceAlpha = 255 - sourceAlpha;
            int outputAlpha =
                sourceAlpha +
                (destinationAlpha * inverseSourceAlpha + 127) / 255;
            if (outputAlpha <= 0)
            {
                return default;
            }

            int redPremultiplied =
                source.r * sourceAlpha +
                (destination.r * destinationAlpha *
                 inverseSourceAlpha + 127) / 255;
            int greenPremultiplied =
                source.g * sourceAlpha +
                (destination.g * destinationAlpha *
                 inverseSourceAlpha + 127) / 255;
            int bluePremultiplied =
                source.b * sourceAlpha +
                (destination.b * destinationAlpha *
                 inverseSourceAlpha + 127) / 255;

            return new Color32(
                ClampByte(
                    (redPremultiplied + outputAlpha / 2) /
                    outputAlpha),
                ClampByte(
                    (greenPremultiplied + outputAlpha / 2) /
                    outputAlpha),
                ClampByte(
                    (bluePremultiplied + outputAlpha / 2) /
                    outputAlpha),
                ClampByte(outputAlpha));
        }

        private static byte ScaleAlpha(int alpha, float factor)
        {
            return ClampByte(
                Mathf.RoundToInt(alpha * Mathf.Clamp01(factor)));
        }

        private static byte ClampByte(int value)
        {
            return (byte)Mathf.Clamp(value, 0, 255);
        }

        private static bool MatchesSide(float x, Side side)
        {
            return side == Side.Any ||
                   (side == Side.Left && x < .5f) ||
                   (side == Side.Right && x >= .5f);
        }

        private static bool Contains(Rect rect, Shape shape, float x, float y)
        {
            if (!rect.Contains(new Vector2(x, y)))
            {
                return false;
            }

            if (shape == Shape.Rectangle)
            {
                return true;
            }

            float dx = (x - rect.center.x) / Mathf.Max(.0001f, rect.width * .5f);
            float dy = (y - rect.center.y) / Mathf.Max(.0001f, rect.height * .5f);
            return dx * dx + dy * dy <= 1f;
        }

        private static void WriteLayer(string contractPath, Color32[] pixels)
        {
            Texture2D texture = new(Width, Height, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            string assetPath = LayerRoot + "/" + contractPath.Replace('/', '_') + ".png";
            string absolute = ToAbsolutePath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllBytes(absolute, png);
        }

        private static ImageData LoadImage(string assetPath)
        {
            string absolute = ToAbsolutePath(assetPath);
            if (!File.Exists(absolute))
            {
                return null;
            }

            try
            {
                Texture2D texture = new(2, 2, TextureFormat.RGBA32, false, false);
                if (!texture.LoadImage(File.ReadAllBytes(absolute), false))
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                    return null;
                }

                ImageData data = new()
                {
                    width = texture.width,
                    height = texture.height,
                    pixels = texture.GetPixels32()
                };
                UnityEngine.Object.DestroyImmediate(texture);
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Patch 4 could not read " + assetPath + ": " + exception.Message);
                return null;
            }
        }

        private static void WriteDraftStatus(
            int generatedCount,
            int maskCount,
            IEnumerable<string> manual,
            IEnumerable<string> warnings)
        {
            string ArrayJson(IEnumerable<string> values)
            {
                List<string> rows = new();
                foreach (string value in values)
                {
                    rows.Add("\"" + Escape(value) + "\"");
                }
                return "[\n    " + string.Join(",\n    ", rows) + "\n  ]";
            }

            string json =
                "{\n" +
                "  \"schemaVersion\": 1,\n" +
                "  \"status\": \"draft-do-not-activate\",\n" +
                "  \"generatedLayerCount\": " + generatedCount + ",\n" +
                "  \"downloadedValidMaskCount\": " + maskCount + ",\n" +
                "  \"activationAllowed\": false,\n" +
                "  \"manualRedrawRequired\": " + ArrayJson(manual) + ",\n" +
                "  \"warnings\": " + ArrayJson(warnings) + "\n" +
                "}\n";

            string absolute = ToAbsolutePath(DraftMetadataPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, json);
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(root, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
