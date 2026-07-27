using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Produces a complete full-canvas draft layer pack from the approved master,
    /// valid Adobe masks and controlled geometric fallbacks.
    ///
    /// This baker never claims that visible-source pixels are finished rig art.
    /// Hidden joint continuations, mouth poses and soft-body interior artwork must
    /// still be hand-painted before Patch4ArtReadinessAsset can be approved.
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

        private const int ExpectedWidth = 1024;
        private const int ExpectedHeight = 1536;

        private enum HorizontalSide
        {
            Any,
            Left,
            Right
        }

        private enum ShapeMode
        {
            Rectangle,
            Ellipse
        }

        private sealed class SourceImage
        {
            public int width;
            public int height;
            public Color32[] pixels;
        }

        private sealed class LayerSpec
        {
            public string contractPath;
            public string[] maskIds;
            public Rect regionTopNormalized;
            public HorizontalSide side;
            public ShapeMode shape;
            public bool manualRedrawRequired;
            public string reason;

            public LayerSpec(
                string contractPath,
                Rect region,
                HorizontalSide side = HorizontalSide.Any,
                ShapeMode shape = ShapeMode.Rectangle,
                bool manualRedrawRequired = true,
                string reason = null,
                params string[] maskIds)
            {
                this.contractPath = contractPath;
                regionTopNormalized = region;
                this.side = side;
                this.shape = shape;
                this.manualRedrawRequired = manualRedrawRequired;
                this.reason = reason ?? string.Empty;
                this.maskIds = maskIds ?? Array.Empty<string>();
            }
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Art/Bake Draft Layer Pack")]
        public static void BakeDraftLayerPack()
        {
            if (!File.Exists(ToAbsolutePath(MasterPath)))
            {
                Debug.LogError(
                    "Patch 4 master is missing. Run Art/Download Adobe Sources " +
                    "before baking layers. Expected: " + MasterPath);
                return;
            }

            SourceImage master = LoadImage(MasterPath);
            if (master == null ||
                master.width != ExpectedWidth ||
                master.height != ExpectedHeight)
            {
                Debug.LogError(
                    $"Patch 4 master must be {ExpectedWidth}×{ExpectedHeight}. " +
                    $"Actual: {master?.width ?? 0}×{master?.height ?? 0}.");
                return;
            }

            Dictionary<string, SourceImage> masks = LoadKnownMasks();
            List<LayerSpec> specs = BuildLayerSpecs();
            EnsureFolder(LayerRoot);

            List<string> generated = new();
            List<string> manual = new();
            List<string> warnings = new();

            try
            {
                for (int i = 0; i < specs.Count; i++)
                {
                    LayerSpec spec = specs[i];
                    EditorUtility.DisplayProgressBar(
                        "GameWork Patch 4.0",
                        "Baking " + spec.contractPath,
                        (float)i / Mathf.Max(1, specs.Count));

                    Color32[] layerPixels = BakeLayer(master, masks, spec, warnings);
                    string fileName = spec.contractPath.Replace('/', '_') + ".png";
                    string assetPath = LayerRoot + "/" + fileName;
                    WritePng(assetPath, master.width, master.height, layerPixels);
                    generated.Add(spec.contractPath);

                    if (spec.manualRedrawRequired)
                    {
                        manual.Add(spec.contractPath +
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

            WriteDraftMetadata(generated, manual, warnings, masks);
            AssetDatabase.Refresh();
            Patch4LayerCatalogBuilder.RebuildCatalog();
            Patch4DraftLayerValidator.ValidateAndWriteReport();

            Debug.Log(
                $"Patch 4 draft layer pack baked: {generated.Count} PNGs. " +
                $"Manual redraw remains required for {manual.Count} entries. " +
                "Patch 4 remains locked by the art-readiness gate.");
        }

        private static Dictionary<string, SourceImage> LoadKnownMasks()
        {
            Dictionary<string, SourceImage> masks = new(
                StringComparer.OrdinalIgnoreCase);

            AddMask(masks, "hair", "Mask_Hair.png");
            AddMask(masks, "faceBase", "Mask_FaceBase.png");
            AddMask(masks, "eyebrows", "Mask_Eyebrows.png");
            AddMask(masks, "nose", "Mask_Nose.png");
            AddMask(masks, "ears", "Mask_Ears.png");
            AddMask(masks, "neck", "Mask_Neck.png");
            AddMask(masks, "upperClothes", "Mask_UpperClothes.png");
            AddMask(masks, "lowerClothes", "Mask_LowerClothes.png");
            AddMask(masks, "hands", "Mask_Hands.png");
            AddMask(masks, "shoes", "Mask_Shoes.png");
            return masks;
        }

        private static void AddMask(
            IDictionary<string, SourceImage> target,
            string id,
            string fileName)
        {
            string path = Patch4AdobeMaskDownloader.DownloadedMaskRoot +
                          "/" + fileName;
            if (!File.Exists(ToAbsolutePath(path)))
            {
                return;
            }

            SourceImage image = LoadImage(path);
            if (image != null &&
                image.width == ExpectedWidth &&
                image.height == ExpectedHeight)
            {
                target[id] = image;
            }
        }

        private static List<LayerSpec> BuildLayerSpecs()
        {
            const bool manual = true;
            List<LayerSpec> specs = new()
            {
                new("Body/TorsoBase", R(0.20f, 0.225f, 0.60f, 0.29f),
                    reason: "visible shirt pixels are only a stand-in for the hidden torso base",
                    maskIds: new[] { "upperClothes" }),
                new("Body/BellyFront", R(0.26f, 0.315f, 0.48f, 0.20f),
                    reason: "needs clean belly deformation edge and hidden lower continuation",
                    maskIds: new[] { "upperClothes" }),
                new("Body/ChestSoft", R(0.27f, 0.235f, 0.46f, 0.15f),
                    reason: "needs independent chest paint beneath the shirt",
                    maskIds: new[] { "upperClothes" }),
                new("Body/Neck", R(0.39f, 0.195f, 0.22f, 0.105f),
                    reason: "extend at least 24 px beneath head and shirt",
                    maskIds: new[] { "neck" }),

                new("Head/HeadBase", R(0.38f, 0.09f, 0.24f, 0.18f),
                    reason: "union of visible face, hair and ears requires hidden neck overlap",
                    maskIds: new[] { "hair", "faceBase", "ears" }),
                new("Head/EarL", R(0.39f, 0.16f, 0.12f, 0.07f), HorizontalSide.Left,
                    reason: "separate Adobe ear pair and repaint hidden attachment",
                    maskIds: new[] { "ears" }),
                new("Head/EarR", R(0.49f, 0.16f, 0.12f, 0.07f), HorizontalSide.Right,
                    reason: "separate Adobe ear pair and repaint hidden attachment",
                    maskIds: new[] { "ears" }),

                new("Face/BrowL", R(0.43f, 0.15f, 0.09f, 0.035f), HorizontalSide.Left,
                    manualRedrawRequired: false, maskIds: new[] { "eyebrows" }),
                new("Face/BrowR", R(0.48f, 0.15f, 0.09f, 0.035f), HorizontalSide.Right,
                    manualRedrawRequired: false, maskIds: new[] { "eyebrows" }),
                new("Face/EyeWhiteL", R(0.443f, 0.169f, 0.055f, 0.035f), HorizontalSide.Left,
                    ShapeMode.Ellipse, manual, "Adobe eye mask was incomplete; verify exact sclera contour"),
                new("Face/EyeWhiteR", R(0.502f, 0.169f, 0.055f, 0.035f), HorizontalSide.Right,
                    ShapeMode.Ellipse, manual, "Adobe eye mask was incomplete; verify exact sclera contour"),
                new("Face/IrisL", R(0.469f, 0.174f, 0.017f, 0.025f), HorizontalSide.Left,
                    ShapeMode.Ellipse, manual, "iris mask is geometric fallback"),
                new("Face/IrisR", R(0.515f, 0.174f, 0.017f, 0.025f), HorizontalSide.Right,
                    ShapeMode.Ellipse, manual, "iris mask is geometric fallback"),
                new("Face/LidL", R(0.441f, 0.166f, 0.059f, 0.018f), HorizontalSide.Left,
                    ShapeMode.Ellipse, manual, "redraw dedicated eyelid artwork"),
                new("Face/LidR", R(0.500f, 0.166f, 0.059f, 0.018f), HorizontalSide.Right,
                    ShapeMode.Ellipse, manual, "redraw dedicated eyelid artwork"),
                new("Face/Nose", R(0.467f, 0.158f, 0.066f, 0.055f),
                    manualRedrawRequired: false, maskIds: new[] { "nose" }),
                new("Face/MouthClosed", R(0.466f, 0.205f, 0.068f, 0.035f),
                    reason: "tight visible-source fallback; clean transparent edge manually"),
                new("Face/MouthOpen", R(0.466f, 0.202f, 0.068f, 0.043f),
                    reason: "must be independently painted; current pixels only reserve alignment"),
                new("Face/MouthSmile", R(0.458f, 0.201f, 0.084f, 0.043f),
                    reason: "must be independently painted; current pixels only reserve alignment"),
                new("Face/CheekL", R(0.413f, 0.184f, 0.082f, 0.058f), HorizontalSide.Left,
                    ShapeMode.Ellipse, manual, "needs soft cheek overlay without facial-line duplication"),
                new("Face/CheekR", R(0.505f, 0.184f, 0.082f, 0.058f), HorizontalSide.Right,
                    ShapeMode.Ellipse, manual, "needs soft cheek overlay without facial-line duplication"),

                new("ArmL/Upper", R(0.185f, 0.245f, 0.205f, 0.185f), HorizontalSide.Left,
                    reason: "geometric fallback; redraw hidden shoulder and elbow continuation"),
                new("ArmL/Forearm", R(0.17f, 0.375f, 0.19f, 0.17f), HorizontalSide.Left,
                    reason: "geometric fallback; redraw hidden elbow and wrist continuation"),
                new("ArmL/Hand", R(0.19f, 0.455f, 0.17f, 0.105f), HorizontalSide.Left,
                    reason: "split Adobe hand pair and repaint wrist overlap",
                    maskIds: new[] { "hands" }),
                new("ArmR/Upper", R(0.61f, 0.245f, 0.205f, 0.185f), HorizontalSide.Right,
                    reason: "geometric fallback; redraw hidden shoulder and elbow continuation"),
                new("ArmR/Forearm", R(0.64f, 0.375f, 0.19f, 0.17f), HorizontalSide.Right,
                    reason: "geometric fallback; redraw hidden elbow and wrist continuation"),
                new("ArmR/Hand", R(0.64f, 0.455f, 0.17f, 0.105f), HorizontalSide.Right,
                    reason: "split Adobe hand pair and repaint wrist overlap",
                    maskIds: new[] { "hands" }),

                new("LegL/Thigh", R(0.275f, 0.465f, 0.235f, 0.20f), HorizontalSide.Left,
                    reason: "split pants with geometric hip/knee boundaries and add hidden overlap",
                    maskIds: new[] { "lowerClothes" }),
                new("LegL/Shin", R(0.255f, 0.59f, 0.25f, 0.18f), HorizontalSide.Left,
                    reason: "split pants with geometric knee/ankle boundaries and add hidden overlap",
                    maskIds: new[] { "lowerClothes" }),
                new("LegL/Foot", R(0.25f, 0.715f, 0.255f, 0.095f), HorizontalSide.Left,
                    reason: "split Adobe shoe pair and repaint ankle overlap",
                    maskIds: new[] { "shoes" }),
                new("LegR/Thigh", R(0.49f, 0.465f, 0.235f, 0.20f), HorizontalSide.Right,
                    reason: "split pants with geometric hip/knee boundaries and add hidden overlap",
                    maskIds: new[] { "lowerClothes" }),
                new("LegR/Shin", R(0.495f, 0.59f, 0.25f, 0.18f), HorizontalSide.Right,
                    reason: "split pants with geometric knee/ankle boundaries and add hidden overlap",
                    maskIds: new[] { "lowerClothes" }),
                new("LegR/Foot", R(0.495f, 0.715f, 0.255f, 0.095f), HorizontalSide.Right,
                    reason: "split Adobe shoe pair and repaint ankle overlap",
                    maskIds: new[] { "shoes" }),

                new("Clothes/ShirtBase", R(0.20f, 0.225f, 0.60f, 0.295f),
                    reason: "clean mask, repair armholes and reconstruct hidden torso paint",
                    maskIds: new[] { "upperClothes" }),
                new("Clothes/ShirtBellyOverlay", R(0.255f, 0.32f, 0.49f, 0.205f),
                    reason: "needs independent shirt-hem extension and soft deformation paint",
                    maskIds: new[] { "upperClothes" }),
                new("Clothes/Bottoms", R(0.275f, 0.465f, 0.45f, 0.30f),
                    reason: "full visible pants reference; limb layers still need manual hidden art",
                    maskIds: new[] { "lowerClothes" }),
                new("Clothes/Shoes", R(0.25f, 0.715f, 0.50f, 0.10f),
                    reason: "pair reference only; runtime uses individual feet",
                    maskIds: new[] { "shoes" }),

                new("FX/Sweat", R(0.56f, 0.205f, 0.025f, 0.045f),
                    ShapeMode.Ellipse, manual, "paint optional sweat sprite independently"),
                new("FX/ImpactFold", R(0.39f, 0.39f, 0.22f, 0.055f),
                    reason: "paint optional impact fold as a dedicated effect"),
                new("FX/Shadow", R(0.27f, 0.79f, 0.46f, 0.045f),
                    ShapeMode.Ellipse, manual, "paint or generate a dedicated soft ground shadow")
            };

            return specs;
        }

        private static Color32[] BakeLayer(
            SourceImage master,
            IReadOnlyDictionary<string, SourceImage> masks,
            LayerSpec spec,
            ICollection<string> warnings)
        {
            Color32[] result = new Color32[master.pixels.Length];
            bool hasRequestedMask = spec.maskIds.Length > 0;
            bool foundAnyMask = false;

            for (int y = 0; y < master.height; y++)
            {
                float topY = 1f - (y + 0.5f) / master.height;
                for (int x = 0; x < master.width; x++)
                {
                    int index = y * master.width + x;
                    Color32 source = master.pixels[index];
                    if (source.a == 0)
                    {
                        continue;
                    }

                    float normalizedX = (x + 0.5f) / master.width;
                    if (!MatchesSide(normalizedX, spec.side) ||
                        !Contains(spec.regionTopNormalized, spec.shape, normalizedX, topY))
                    {
                        continue;
                    }

                    byte maskAlpha = 255;
                    if (hasRequestedMask)
                    {
                        maskAlpha = 0;
                        for (int maskIndex = 0; maskIndex < spec.maskIds.Length; maskIndex++)
                        {
                            if (!masks.TryGetValue(spec.maskIds[maskIndex], out SourceImage mask))
                            {
                                continue;
                            }

                            foundAnyMask = true;
                            Color32 maskPixel = mask.pixels[index];
                            byte value = (byte)Mathf.Max(
                                maskPixel.r,
                                Mathf.Max(maskPixel.g, maskPixel.b));
                            maskAlpha = Math.Max(maskAlpha, value);
                        }
                    }

                    if (maskAlpha < 8)
                    {
                        continue;
                    }

                    source.a = (byte)(source.a * maskAlpha / 255);
                    result[index] = source;
                }
            }

            if (hasRequestedMask && !foundAnyMask)
            {
                warnings.Add(
                    spec.contractPath +
                    " used geometry only because its Adobe mask was not downloaded.");
            }

            return result;
        }

        private static bool MatchesSide(float x, HorizontalSide side)
        {
            return side switch
            {
                HorizontalSide.Left => x < 0.5f,
                HorizontalSide.Right => x >= 0.5f,
                _ => true
            };
        }

        private static bool Contains(Rect rect, ShapeMode shape, float x, float y)
        {
            if (!rect.Contains(new Vector2(x, y)))
            {
                return false;
            }

            if (shape == ShapeMode.Rectangle)
            {
                return true;
            }

            float centerX = rect.x + rect.width * 0.5f;
            float centerY = rect.y + rect.height * 0.5f;
            float radiusX = Mathf.Max(0.0001f, rect.width * 0.5f);
            float radiusY = Mathf.Max(0.0001f, rect.height * 0.5f);
            float dx = (x - centerX) / radiusX;
            float dy = (y - centerY) / radiusY;
            return dx * dx + dy * dy <= 1f;
        }

        private static Rect R(float x, float y, float width, float height)
        {
            return new Rect(x, y, width, height);
        }

        private static SourceImage LoadImage(string assetPath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(ToAbsolutePath(assetPath));
                Texture2D texture = new(2, 2, TextureFormat.RGBA32, false, false);
                if (!texture.LoadImage(bytes, false))
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                    return null;
                }

                SourceImage image = new()
                {
                    width = texture.width,
                    height = texture.height,
                    pixels = texture.GetPixels32()
                };
                UnityEngine.Object.DestroyImmediate(texture);
                return image;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Patch 4 could not read " + assetPath + ": " + exception.Message);
                return null;
            }
        }

        private static void WritePng(
            string assetPath,
            int width,
            int height,
            Color32[] pixels)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            byte[] png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            string absolutePath = ToAbsolutePath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllBytes(absolutePath, png);
        }

        private static void WriteDraftMetadata(
            IReadOnlyCollection<string> generated,
            IReadOnlyCollection<string> manual,
            IReadOnlyCollection<string> warnings,
            IReadOnlyDictionary<string, SourceImage> masks)
        {
            string JsonArray(IEnumerable<string> values)
            {
                List<string> escaped = new();
                foreach (string value in values)
                {
                    escaped.Add("\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
                }

                return "[\n    " + string.Join(",\n    ", escaped) + "\n  ]";
            }

            string json =
                "{\n" +
                "  \"schemaVersion\": 1,\n" +
                "  \"status\": \"draft-do-not-activate\",\n" +
                "  \"canvas\": [1024, 1536],\n" +
                "  \"downloadedValidMaskCount\": " + masks.Count + ",\n" +
                "  \"generatedLayers\": " + JsonArray(generated) + ",\n" +
                "  \"manualRedrawRequired\": " + JsonArray(manual) + ",\n" +
                "  \"warnings\": " + JsonArray(warnings) + ",\n" +
                "  \"activationAllowed\": false\n" +
                "}\n";

            string absolutePath = ToAbsolutePath(DraftMetadataPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllText(absolutePath, json);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
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
