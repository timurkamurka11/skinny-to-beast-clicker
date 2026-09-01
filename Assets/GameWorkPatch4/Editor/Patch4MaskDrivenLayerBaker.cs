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
        private const byte VisibleAlphaThreshold = 8;
        private const float EyeReplacementFeatherInnerRadius = .60f;
        private const float EyeReplacementFeatherOuterRadius = .80f;

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

        private sealed class SkinPatchData
        {
            public int minX;
            public int maxX;
            public int minY;
            public int maxY;
            public int width;
            public int height;
            public Color32[] colors;
            public float[] blendCoverage;
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

        private readonly struct FaceFeature
        {
            public readonly Rect underlayPatch;
            public readonly Vector2 normalizedTopCenter;
            public readonly Vector2 radiusPixels;

            public FaceFeature(
                Rect underlayPatch,
                float centerX,
                float centerTopY,
                float radiusX,
                float radiusY)
            {
                this.underlayPatch = underlayPatch;
                normalizedTopCenter =
                    new Vector2(centerX, centerTopY);
                radiusPixels =
                    new Vector2(radiusX, radiusY);
            }
        }

        private static readonly Rect LeftEyePatch =
            new(.438f, .164f, .063f, .045f);
        private static readonly Rect RightEyePatch =
            new(.499f, .164f, .063f, .045f);
        private static readonly Rect MouthPatch =
            new(.452f, .198f, .096f, .052f);
        private static readonly FaceFeature LeftEyeFeature =
            new(LeftEyePatch, .4695f, .1855f, 32f, 25f);
        private static readonly FaceFeature RightEyeFeature =
            new(RightEyePatch, .5305f, .1855f, 32f, 25f);
        private static readonly FaceFeature LeftIrisFeature =
            new(LeftEyePatch, .4775f, .1865f, 12f, 17f);
        private static readonly FaceFeature RightIrisFeature =
            new(RightEyePatch, .5235f, .1865f, 12f, 17f);
        private static readonly FaceFeature ClosedMouthFeature =
            new(MouthPatch, .5f, .220f, 40f, 24f);
        private static readonly IReadOnlyList<string> ReferenceCutoutPaths =
            Array.AsReadOnly(new[]
            {
                "Body/Neck",
                "Head/HeadBase",
                "ArmL/Upper",
                "ArmL/Forearm",
                "ArmL/Hand",
                "ArmR/Upper",
                "ArmR/Forearm",
                "ArmR/Hand",
                "LegL/Thigh",
                "LegL/Shin",
                "LegL/Foot",
                "LegR/Thigh",
                "LegR/Shin",
                "LegR/Foot",
                "Clothes/ShirtBase"
            });

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
                "one continuous runtime body. Human review and " +
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
            if (Patch4RigContract.IsRuntimeContinuousBodyLayer(spec.path))
            {
                // Neutral runtime art must remain byte-for-byte equivalent to
                // the quality master. P4.0-R inpainted its eyes and mouth here
                // and then tried to reconstruct them from sparse extraction;
                // real Unity review showed a blank face.
                return (Color32[])master.pixels.Clone();
            }

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

        /// <summary>
        /// Repository masks are conservative rectangular source regions. A
        /// neutral frame can hide that several such regions contain the same
        /// master pixel, but those copies split apart as soon as their bones
        /// rotate. Assign every neutral body pixel to exactly one live cutout,
        /// then restore only the small joint overlaps that are intentionally
        /// required to avoid seams.
        /// </summary>
        private static void EnforceExclusiveRuntimeArtworkOwnership(
            ImageData master,
            IDictionary<string, Color32[]> bakedLayers)
        {
            IReadOnlyList<string> paths = ReferenceCutoutPaths;

            for (int y = 0; y < Height; y++)
            {
                float topY = 1f - (y + .5f) / Height;
                for (int x = 0; x < Width; x++)
                {
                    int index = y * Width + x;
                    Color32 source = master.pixels[index];
                    if (source.a <= 8)
                    {
                        continue;
                    }

                    float normalizedX = (x + .5f) / Width;
                    string preferredPath = ResolveFallbackArtworkOwner(
                        normalizedX,
                        topY);
                    string ownerPath = string.Empty;
                    float ownerDistance = float.PositiveInfinity;
                    int candidateCount = 0;

                    for (int i = 0; i < paths.Count; i++)
                    {
                        string path = paths[i];
                        if (!bakedLayers.TryGetValue(
                                path,
                                out Color32[] pixels) ||
                            pixels[index].a <= 8)
                        {
                            continue;
                        }

                        candidateCount++;
                        float distance = ResolveArtworkOwnerDistance(
                            path,
                            normalizedX,
                            topY);
                        if (string.Equals(
                            path,
                            preferredPath,
                            StringComparison.Ordinal))
                        {
                            distance = -1f;
                        }
                        if (distance < ownerDistance)
                        {
                            ownerDistance = distance;
                            ownerPath = path;
                        }
                    }

                    if (candidateCount == 0)
                    {
                        ownerPath = preferredPath;
                        if (bakedLayers.TryGetValue(
                            ownerPath,
                            out Color32[] fallback))
                        {
                            fallback[index] = source;
                        }

                        continue;
                    }

                    if (candidateCount == 1)
                    {
                        continue;
                    }

                    for (int i = 0; i < paths.Count; i++)
                    {
                        string path = paths[i];
                        if (string.Equals(
                                path,
                                ownerPath,
                                StringComparison.Ordinal) ||
                            !bakedLayers.TryGetValue(
                                path,
                                out Color32[] pixels))
                        {
                            continue;
                        }

                        pixels[index] = default;
                    }
                }
            }

            // Only these small ellipses may overlap after ownership is split.
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                if (!bakedLayers.TryGetValue(
                    path,
                    out Color32[] pixels))
                {
                    continue;
                }

                JointContinuation[] continuations =
                    ResolveJointContinuations(path);
                for (int jointIndex = 0;
                     jointIndex < continuations.Length;
                     jointIndex++)
                {
                    PaintJointContinuation(
                        master,
                        pixels,
                        continuations[jointIndex]);
                }
            }
        }

        private static bool IsExclusiveRuntimeArtworkPath(string path)
        {
            IReadOnlyList<string> paths = ReferenceCutoutPaths;
            for (int i = 0; i < paths.Count; i++)
            {
                if (string.Equals(
                    paths[i],
                    path,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static float ResolveArtworkOwnerDistance(
            string path,
            float normalizedX,
            float topY)
        {
            Vector2 anchor = path switch
            {
                "Body/Neck" => new Vector2(.5f, .264f),
                "Head/HeadBase" => new Vector2(.5f, .185f),
                "ArmL/Upper" => new Vector2(.347f, .313f),
                "ArmL/Forearm" => new Vector2(.293f, .423f),
                "ArmL/Hand" => new Vector2(.264f, .527f),
                "ArmR/Upper" => new Vector2(.653f, .313f),
                "ArmR/Forearm" => new Vector2(.707f, .423f),
                "ArmR/Hand" => new Vector2(.736f, .527f),
                "LegL/Thigh" => new Vector2(.430f, .566f),
                "LegL/Shin" => new Vector2(.420f, .677f),
                "LegL/Foot" => new Vector2(.405f, .775f),
                "LegR/Thigh" => new Vector2(.570f, .566f),
                "LegR/Shin" => new Vector2(.580f, .677f),
                "LegR/Foot" => new Vector2(.595f, .775f),
                _ => new Vector2(.5f, .39f)
            };
            float dx = normalizedX - anchor.x;
            float dy = topY - anchor.y;
            return dx * dx + dy * dy;
        }

        private static string ResolveFallbackArtworkOwner(
            float normalizedX,
            float topY)
        {
            if (topY < .245f)
            {
                return "Head/HeadBase";
            }

            if (normalizedX < .37f && topY < .57f)
            {
                return ResolveArmOwner(true, topY);
            }

            if (normalizedX > .63f && topY < .57f)
            {
                return ResolveArmOwner(false, topY);
            }

            if (topY >= .48f)
            {
                return ResolveLegOwner(normalizedX < .5f, topY);
            }

            if (topY < .305f &&
                normalizedX >= .39f &&
                normalizedX <= .61f)
            {
                return "Body/Neck";
            }

            return "Clothes/ShirtBase";
        }

        private static string ResolveArmOwner(bool left, float topY)
        {
            string prefix = left ? "ArmL/" : "ArmR/";
            if (topY < .405f)
            {
                return prefix + "Upper";
            }

            return topY < .495f
                ? prefix + "Forearm"
                : prefix + "Hand";
        }

        private static string ResolveLegOwner(bool left, float topY)
        {
            string prefix = left ? "LegL/" : "LegR/";
            if (topY < .625f)
            {
                return prefix + "Thigh";
            }

            return topY < .735f
                ? prefix + "Shin"
                : prefix + "Foot";
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
                    // HeadBase is the only owner of the neutral painted face.
                    // Retain the exact eyes, irises, nose and closed mouth from
                    // the approved master instead of blanking them and trying
                    // to rebuild the neutral face from sparse overlay cutouts.
                    break;

                case "Face/EyeWhiteL":
                    CopyFeatheredMasterPatch(
                        master,
                        result,
                        LeftEyePatch,
                        EyeReplacementFeatherInnerRadius,
                        EyeReplacementFeatherOuterRadius);
                    PaintSkinUnderlay(master, result, LeftEyePatch);
                    OverlayShiftedMasterFeature(
                        master,
                        result,
                        LeftEyeFeature,
                        4);
                    break;

                case "Face/EyeWhiteR":
                    CopyFeatheredMasterPatch(
                        master,
                        result,
                        RightEyePatch,
                        EyeReplacementFeatherInnerRadius,
                        EyeReplacementFeatherOuterRadius);
                    PaintSkinUnderlay(master, result, RightEyePatch);
                    OverlayShiftedMasterFeature(
                        master,
                        result,
                        RightEyeFeature,
                        4);
                    break;

                case "Face/IrisL":
                    ExtractMasterFeature(
                        master,
                        result,
                        LeftIrisFeature);
                    break;

                case "Face/IrisR":
                    ExtractMasterFeature(
                        master,
                        result,
                        RightIrisFeature);
                    break;

                case "Face/LidL":
                    CopyFeatheredMasterPatch(
                        master,
                        result,
                        LeftEyePatch);
                    PaintSkinUnderlay(
                        master,
                        result,
                        LeftEyePatch);
                    PaintClosedLid(result, LeftEyePatch);
                    break;

                case "Face/LidR":
                    CopyFeatheredMasterPatch(
                        master,
                        result,
                        RightEyePatch);
                    PaintSkinUnderlay(
                        master,
                        result,
                        RightEyePatch);
                    PaintClosedLid(result, RightEyePatch);
                    break;

                case "Face/MouthClosed":
                    ExtractMasterFeature(
                        master,
                        result,
                        ClosedMouthFeature);
                    break;

                case "Face/MouthOpen":
                    CopyFeatheredMasterPatch(
                        master,
                        result,
                        MouthPatch);
                    PaintSkinUnderlay(
                        master,
                        result,
                        MouthPatch);
                    PaintOpenMouth(result);
                    break;

                case "Face/MouthSmile":
                    CopyFeatheredMasterPatch(
                        master,
                        result,
                        MouthPatch);
                    PaintSkinUnderlay(
                        master,
                        result,
                        MouthPatch);
                    PaintSmile(result);
                    break;

                case "Face/CheekL":
                    FeatherClearFeature(
                        result,
                        LeftEyeFeature);
                    FeatherClearFeature(
                        result,
                        ClosedMouthFeature);
                    break;

                case "Face/CheekR":
                    FeatherClearFeature(
                        result,
                        RightEyeFeature);
                    FeatherClearFeature(
                        result,
                        ClosedMouthFeature);
                    break;
            }
        }

        private static void ExtractMasterFeature(
            ImageData master,
            Color32[] result,
            FaceFeature feature)
        {
            ClearLayer(result);
            SkinPatchData patch = BuildSkinPatch(
                master,
                feature.underlayPatch);
            Vector2 center = ToPixel(
                feature.normalizedTopCenter.x,
                feature.normalizedTopCenter.y);

            for (int y = patch.minY; y <= patch.maxY; y++)
            {
                for (int x = patch.minX; x <= patch.maxX; x++)
                {
                    int localIndex =
                        (y - patch.minY) * patch.width +
                        x - patch.minX;
                    int sourceIndex = y * Width + x;
                    Color32 source = master.pixels[sourceIndex];
                    Color32 skin = patch.colors[localIndex];
                    int colorDifference = Mathf.Max(
                        Mathf.Abs(source.r - skin.r),
                        Mathf.Max(
                            Mathf.Abs(source.g - skin.g),
                            Mathf.Abs(source.b - skin.b)));
                    float detailCoverage =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(
                                14f,
                                38f,
                                colorDifference));
                    float dx =
                        (x - center.x) /
                        Mathf.Max(1f, feature.radiusPixels.x);
                    float dy =
                        (y - center.y) /
                        Mathf.Max(1f, feature.radiusPixels.y);
                    float distance = Mathf.Sqrt(
                        dx * dx + dy * dy);
                    float shapeCoverage =
                        1f -
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(
                                .72f,
                                1f,
                                distance));
                    float coverage =
                        detailCoverage * shapeCoverage;
                    if (coverage <= .01f)
                    {
                        continue;
                    }

                    source.a = ScaleAlpha(
                        source.a,
                        coverage);
                    if (source.a > 0)
                    {
                        result[sourceIndex] = source;
                    }
                }
            }
        }

        private static void OverlayShiftedMasterFeature(
            ImageData master,
            Color32[] result,
            FaceFeature feature,
            int horizontalOffset)
        {
            Color32[] featurePixels = new Color32[Width * Height];
            ExtractMasterFeature(master, featurePixels, feature);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color32 source = featurePixels[y * Width + x];
                    if (source.a == 0)
                    {
                        continue;
                    }

                    int destinationX = x + horizontalOffset;
                    if (destinationX < 0 || destinationX >= Width)
                    {
                        continue;
                    }

                    int destinationIndex = y * Width + destinationX;
                    // The shifted painted eye must remain inside the already
                    // feathered skin replacement. Letting feature alpha create
                    // new pixels outside that ellipse expanded EyeWhiteL/R to
                    // ~53% of the region and produced a broad skin patch.
                    if (result[destinationIndex].a <= VisibleAlphaThreshold)
                    {
                        continue;
                    }

                    result[destinationIndex] = BlendOver(
                        result[destinationIndex],
                        source);
                }
            }
        }

        private static void FeatherClearFeature(
            Color32[] result,
            FaceFeature feature)
        {
            Vector2 center = ToPixel(
                feature.normalizedTopCenter.x,
                feature.normalizedTopCenter.y);
            int minX = Mathf.Max(
                0,
                Mathf.FloorToInt(
                    center.x - feature.radiusPixels.x - 1f));
            int maxX = Mathf.Min(
                Width - 1,
                Mathf.CeilToInt(
                    center.x + feature.radiusPixels.x + 1f));
            int minY = Mathf.Max(
                0,
                Mathf.FloorToInt(
                    center.y - feature.radiusPixels.y - 1f));
            int maxY = Mathf.Min(
                Height - 1,
                Mathf.CeilToInt(
                    center.y + feature.radiusPixels.y + 1f));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx =
                        (x - center.x) /
                        Mathf.Max(1f, feature.radiusPixels.x);
                    float dy =
                        (y - center.y) /
                        Mathf.Max(1f, feature.radiusPixels.y);
                    float distance = Mathf.Sqrt(
                        dx * dx + dy * dy);
                    if (distance >= 1f)
                    {
                        continue;
                    }

                    float clearCoverage =
                        1f -
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(
                                .64f,
                                1f,
                                distance));
                    int index = y * Width + x;
                    Color32 current = result[index];
                    current.a = ScaleAlpha(
                        current.a,
                        1f - clearCoverage);
                    result[index] =
                        current.a <= 8
                            ? default
                            : current;
                }
            }
        }

        private static void PaintSkinUnderlay(
            ImageData master,
            Color32[] result,
            Rect patch)
        {
            SkinPatchData skinPatch =
                BuildSkinPatch(master, patch);

            for (int y = skinPatch.minY;
                 y <= skinPatch.maxY;
                 y++)
            {
                for (int x = skinPatch.minX;
                     x <= skinPatch.maxX;
                     x++)
                {
                    int localIndex =
                        (y - skinPatch.minY) * skinPatch.width +
                        x - skinPatch.minX;
                    float coverage =
                        skinPatch.blendCoverage[localIndex];
                    if (coverage <= 0f)
                    {
                        continue;
                    }

                    int resultIndex = y * Width + x;
                    Color32 current = result[resultIndex];
                    if (current.a <= 8)
                    {
                        continue;
                    }

                    Color32 skin = skinPatch.colors[localIndex];
                    skin.a = current.a;
                    result[resultIndex] = LerpColor(
                        current,
                        skin,
                        coverage);
                }
            }
        }

        private static void CopyFeatheredMasterPatch(
            ImageData master,
            Color32[] result,
            Rect patch,
            float innerRadius = .66f,
            float outerRadius = .86f)
        {
            ClearLayer(result);
            GetPixelBounds(
                patch,
                out int minX,
                out int maxX,
                out int minY,
                out int maxY);
            Vector2 center = ToPixel(
                patch.center.x,
                patch.center.y);
            float radiusX = Mathf.Max(1f, patch.width * Width * .5f);
            float radiusY = Mathf.Max(1f, patch.height * Height * .5f);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - center.x) / radiusX;
                    float dy = (y - center.y) / radiusY;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float coverage =
                        1f -
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(
                                innerRadius,
                                outerRadius,
                                distance));
                    if (coverage <= 0f)
                    {
                        continue;
                    }

                    int index = y * Width + x;
                    Color32 source = master.pixels[index];
                    source.a = ScaleAlpha(source.a, coverage);
                    if (source.a > 0)
                    {
                        result[index] = source;
                    }
                }
            }
        }

        private static SkinPatchData BuildSkinPatch(
            ImageData master,
            Rect patch)
        {
            GetPixelBounds(
                patch,
                out int minX,
                out int maxX,
                out int minY,
                out int maxY);

            int localWidth = maxX - minX + 1;
            int localHeight = maxY - minY + 1;
            Color32[] skinField =
                new Color32[localWidth * localHeight];
            bool[] inpaintMask =
                new bool[skinField.Length];
            float[] blendCoverage =
                new float[skinField.Length];

            float centerX = (minX + maxX) * .5f;
            float centerY = (minY + maxY) * .5f;
            float radiusX = Mathf.Max(1f, localWidth * .49f);
            float radiusY = Mathf.Max(1f, localHeight * .47f);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int localIndex =
                        (y - minY) * localWidth +
                        x - minX;
                    int sourceIndex = y * Width + x;
                    float dx = (x - centerX) / radiusX;
                    float dy = (y - centerY) / radiusY;
                    float radialDistance =
                        Mathf.Sqrt(dx * dx + dy * dy);

                    skinField[localIndex] =
                        master.pixels[sourceIndex];
                    blendCoverage[localIndex] =
                        1f -
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(
                                .76f,
                                .96f,
                                radialDistance));

                    if (radialDistance > .92f)
                    {
                        continue;
                    }

                    inpaintMask[localIndex] = true;
                    skinField[localIndex] = SampleSkinField(
                        master,
                        patch,
                        x,
                        y,
                        minX,
                        maxX,
                        minY,
                        maxY);
                }
            }

            SolveSkinInpaint(
                skinField,
                inpaintMask,
                localWidth,
                localHeight);

            return new SkinPatchData
            {
                minX = minX,
                maxX = maxX,
                minY = minY,
                maxY = maxY,
                width = localWidth,
                height = localHeight,
                colors = skinField,
                blendCoverage = blendCoverage
            };
        }

        private static void SolveSkinInpaint(
            Color32[] pixels,
            IReadOnlyList<bool> inpaintMask,
            int width,
            int height)
        {
            const int iterationCount = 128;
            Color32[] output = pixels;
            Color32[] next = new Color32[pixels.Length];

            for (int iteration = 0;
                 iteration < iterationCount;
                 iteration++)
            {
                Array.Copy(pixels, next, pixels.Length);
                for (int y = 1; y < height - 1; y++)
                {
                    for (int x = 1; x < width - 1; x++)
                    {
                        int index = y * width + x;
                        if (!inpaintMask[index])
                        {
                            continue;
                        }

                        Color32 left = pixels[index - 1];
                        Color32 right = pixels[index + 1];
                        Color32 below = pixels[index - width];
                        Color32 above = pixels[index + width];
                        next[index] = new Color32(
                            ClampByte(
                                (left.r + right.r + below.r + above.r + 2) /
                                4),
                            ClampByte(
                                (left.g + right.g + below.g + above.g + 2) /
                                4),
                            ClampByte(
                                (left.b + right.b + below.b + above.b + 2) /
                                4),
                            255);
                    }
                }

                Color32[] swap = pixels;
                pixels = next;
                next = swap;
            }

            if (!ReferenceEquals(pixels, output))
            {
                Array.Copy(pixels, output, pixels.Length);
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
            float edgeY = eyePatch.yMin + eyePatch.height * .45f;
            float centerY = edgeY + .005f;

            DrawQuadraticStroke(
                result,
                ToPixel(left, edgeY),
                ToPixel(center, centerY),
                ToPixel(right, edgeY),
                new Color32(178, 102, 88, 135),
                5.5f);
            DrawQuadraticStroke(
                result,
                ToPixel(left, edgeY),
                ToPixel(center, centerY),
                ToPixel(right, edgeY),
                new Color32(68, 43, 46, 235),
                2.6f);
            DrawQuadraticStroke(
                result,
                ToPixel(left + .005f, edgeY - .003f),
                ToPixel(center, centerY - .003f),
                ToPixel(right - .005f, edgeY - .003f),
                new Color32(245, 176, 132, 105),
                1.2f);
        }

        private static void PaintOpenMouth(Color32[] result)
        {
            Vector2 center = ToPixel(.5f, .2205f);
            DrawSoftEllipse(
                result,
                center,
                25f,
                19f,
                new Color32(143, 74, 71, 225),
                2.8f);
            DrawSoftEllipse(
                result,
                center,
                21f,
                15.5f,
                new Color32(56, 31, 39, 255),
                2.2f);
            DrawSoftEllipse(
                result,
                ToPixel(.5f, .214f),
                13.5f,
                4.5f,
                new Color32(238, 226, 208, 240),
                1.8f);
            DrawSoftEllipse(
                result,
                ToPixel(.5f, .227f),
                12.5f,
                5.5f,
                new Color32(174, 82, 88, 220),
                1.8f);
            DrawQuadraticStroke(
                result,
                ToPixel(.481f, .209f),
                ToPixel(.5f, .205f),
                ToPixel(.519f, .209f),
                new Color32(232, 141, 118, 145),
                1.8f);
        }

        private static void PaintSmile(Color32[] result)
        {
            DrawQuadraticStroke(
                result,
                ToPixel(.474f, .216f),
                ToPixel(.5f, .229f),
                ToPixel(.526f, .216f),
                new Color32(84, 46, 49, 225),
                4.2f);
            DrawQuadraticStroke(
                result,
                ToPixel(.477f, .217f),
                ToPixel(.5f, .2245f),
                ToPixel(.523f, .217f),
                new Color32(154, 78, 77, 220),
                2.2f);
            DrawQuadraticStroke(
                result,
                ToPixel(.481f, .2205f),
                ToPixel(.5f, .228f),
                ToPixel(.519f, .2205f),
                new Color32(229, 139, 119, 145),
                1.5f);
            DrawSoftCircle(
                result,
                ToPixel(.474f, .216f),
                2.2f,
                new Color32(73, 42, 45, 185));
            DrawSoftCircle(
                result,
                ToPixel(.526f, .216f),
                2.2f,
                new Color32(73, 42, 45, 185));
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

                case "Clothes/ShirtBase":
                    return new[]
                    {
                        J(.34f, .285f, 30f, 24f),
                        J(.66f, .285f, 30f, 24f),
                        J(.42f, .505f, 30f, 24f),
                        J(.58f, .505f, 30f, 24f)
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
