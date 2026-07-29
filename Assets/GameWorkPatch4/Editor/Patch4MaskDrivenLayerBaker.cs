using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Builds a complete full-canvas draft layer set from the approved master.
    /// Valid Adobe masks are preferred; precise geometric regions are used when
    /// Adobe could not identify a stylized body part. The result is deliberately
    /// marked as draft and cannot unlock Patch 4 production activation.
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

        [MenuItem("Tools/GameWork/Patch 4.0/Art/Bake Draft Layer Pack")]
        public static void BakeDraftLayerPack()
        {
            ImageData master = LoadImage(MasterPath);
            if (master == null)
            {
                Debug.LogError(
                    "Patch 4 master is missing. Run Art/Download Adobe Sources first.");
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
                $"Patch 4 created {specs.Count} draft layers from " +
                $"{masks.Count} valid Adobe masks. Production activation remains locked.");
        }

        private static List<Spec> BuildSpecs()
        {
            List<Spec> result = new();

            result.Add(S("Body/TorsoBase", R(.20f,.225f,.60f,.29f), "upperClothes", reason: "replace shirt stand-in with painted hidden torso"));
            result.Add(S("Body/BellyFront", R(.26f,.315f,.48f,.20f), "upperClothes", reason: "repaint hidden belly and lower continuation"));
            result.Add(S("Body/ChestSoft", R(.27f,.235f,.46f,.15f), "upperClothes", reason: "paint independent chest-soft artwork"));
            result.Add(S("Body/Neck", R(.39f,.195f,.22f,.105f), "neck", reason: "extend beneath head and shirt by at least 24 px"));

            result.Add(S("Head/HeadBase", R(.38f,.09f,.24f,.18f), "hair,faceBase,ears", reason: "repaint hidden neck overlap"));
            result.Add(S("Head/EarL", R(.39f,.16f,.12f,.07f), "ears", Side.Left, reason: "repaint hidden ear attachment"));
            result.Add(S("Head/EarR", R(.49f,.16f,.12f,.07f), "ears", Side.Right, reason: "repaint hidden ear attachment"));

            result.Add(S("Face/BrowL", R(.43f,.15f,.09f,.035f), "eyebrows", Side.Left, manual: false));
            result.Add(S("Face/BrowR", R(.48f,.15f,.09f,.035f), "eyebrows", Side.Right, manual: false));
            result.Add(S("Face/EyeWhiteL", R(.443f,.169f,.055f,.035f), side: Side.Left, shape: Shape.Ellipse, reason: "Adobe eye mask was incomplete"));
            result.Add(S("Face/EyeWhiteR", R(.502f,.169f,.055f,.035f), side: Side.Right, shape: Shape.Ellipse, reason: "Adobe eye mask was incomplete"));
            result.Add(S("Face/IrisL", R(.469f,.174f,.017f,.025f), side: Side.Left, shape: Shape.Ellipse, reason: "geometric iris fallback"));
            result.Add(S("Face/IrisR", R(.515f,.174f,.017f,.025f), side: Side.Right, shape: Shape.Ellipse, reason: "geometric iris fallback"));
            result.Add(S("Face/LidL", R(.441f,.166f,.059f,.018f), side: Side.Left, shape: Shape.Ellipse, reason: "paint dedicated eyelid"));
            result.Add(S("Face/LidR", R(.500f,.166f,.059f,.018f), side: Side.Right, shape: Shape.Ellipse, reason: "paint dedicated eyelid"));
            result.Add(S("Face/Nose", R(.467f,.158f,.066f,.055f), "nose", manual: false));
            result.Add(S("Face/MouthClosed", R(.466f,.205f,.068f,.035f), reason: "clean visible-source edge"));
            result.Add(S("Face/MouthOpen", R(.466f,.202f,.068f,.043f), reason: "paint genuine open-mouth pose"));
            result.Add(S("Face/MouthSmile", R(.458f,.201f,.084f,.043f), reason: "paint genuine smile pose"));
            result.Add(S("Face/CheekL", R(.413f,.184f,.082f,.058f), side: Side.Left, shape: Shape.Ellipse, reason: "paint clean soft cheek overlay"));
            result.Add(S("Face/CheekR", R(.505f,.184f,.082f,.058f), side: Side.Right, shape: Shape.Ellipse, reason: "paint clean soft cheek overlay"));

            result.Add(S("ArmL/Upper", R(.185f,.245f,.205f,.185f), side: Side.Left, reason: "repaint hidden shoulder and elbow"));
            result.Add(S("ArmL/Forearm", R(.17f,.375f,.19f,.17f), side: Side.Left, reason: "repaint hidden elbow and wrist"));
            result.Add(S("ArmL/Hand", R(.19f,.455f,.17f,.105f), "hands", Side.Left, reason: "repaint wrist overlap"));
            result.Add(S("ArmR/Upper", R(.61f,.245f,.205f,.185f), side: Side.Right, reason: "repaint hidden shoulder and elbow"));
            result.Add(S("ArmR/Forearm", R(.64f,.375f,.19f,.17f), side: Side.Right, reason: "repaint hidden elbow and wrist"));
            result.Add(S("ArmR/Hand", R(.64f,.455f,.17f,.105f), "hands", Side.Right, reason: "repaint wrist overlap"));

            result.Add(S("LegL/Thigh", R(.275f,.465f,.235f,.20f), "lowerClothes", Side.Left, reason: "repaint hidden hip and knee"));
            result.Add(S("LegL/Shin", R(.255f,.59f,.25f,.18f), "lowerClothes", Side.Left, reason: "repaint hidden knee and ankle"));
            result.Add(S("LegL/Foot", R(.25f,.715f,.255f,.095f), "shoes", Side.Left, reason: "repaint ankle overlap"));
            result.Add(S("LegR/Thigh", R(.49f,.465f,.235f,.20f), "lowerClothes", Side.Right, reason: "repaint hidden hip and knee"));
            result.Add(S("LegR/Shin", R(.495f,.59f,.25f,.18f), "lowerClothes", Side.Right, reason: "repaint hidden knee and ankle"));
            result.Add(S("LegR/Foot", R(.495f,.715f,.255f,.095f), "shoes", Side.Right, reason: "repaint ankle overlap"));

            result.Add(S("Clothes/ShirtBase", R(.20f,.225f,.60f,.295f), "upperClothes", reason: "repair armholes and hidden torso paint"));
            result.Add(S("Clothes/ShirtBellyOverlay", R(.255f,.32f,.49f,.205f), "upperClothes", reason: "paint independent shirt hem"));
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

            ApplyDraftScaffolding(master, spec, result);

            if (requestedMasks && !foundMask)
            {
                warnings.Add(spec.path + " used geometry because its Adobe mask was unavailable.");
            }

            return result;
        }

        private static void ApplyDraftScaffolding(
            ImageData master,
            Spec spec,
            Color32[] result)
        {
            if (string.Equals(spec.path, "FX/Shadow", StringComparison.Ordinal))
            {
                PaintSyntheticShadow(spec, result);
            }

            Vector2[] jointPoints = ResolveJointPoints(spec.path);
            for (int i = 0; i < jointPoints.Length; i++)
            {
                PaintJointScaffold(master, result, jointPoints[i]);
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

        private static Vector2[] ResolveJointPoints(string path)
        {
            switch (path)
            {
                case "Body/Neck":
                case "Head/HeadBase":
                    return new[] { new Vector2(.5f, .225f) };

                case "Body/ChestSoft":
                    return new[]
                    {
                        new Vector2(.34f, .285f),
                        new Vector2(.66f, .285f)
                    };

                case "ArmL/Upper":
                    return new[]
                    {
                        new Vector2(.34f, .285f),
                        new Vector2(.285f, .405f)
                    };

                case "ArmR/Upper":
                    return new[]
                    {
                        new Vector2(.66f, .285f),
                        new Vector2(.715f, .405f)
                    };

                case "ArmL/Forearm":
                    return new[]
                    {
                        new Vector2(.285f, .405f),
                        new Vector2(.255f, .495f)
                    };

                case "ArmR/Forearm":
                    return new[]
                    {
                        new Vector2(.715f, .405f),
                        new Vector2(.745f, .495f)
                    };

                case "ArmL/Hand":
                    return new[] { new Vector2(.255f, .495f) };

                case "ArmR/Hand":
                    return new[] { new Vector2(.745f, .495f) };

                case "Body/TorsoBase":
                    return new[]
                    {
                        new Vector2(.42f, .505f),
                        new Vector2(.58f, .505f)
                    };

                case "LegL/Thigh":
                    return new[]
                    {
                        new Vector2(.42f, .505f),
                        new Vector2(.40f, .625f)
                    };

                case "LegR/Thigh":
                    return new[]
                    {
                        new Vector2(.58f, .505f),
                        new Vector2(.60f, .625f)
                    };

                case "LegL/Shin":
                    return new[]
                    {
                        new Vector2(.40f, .625f),
                        new Vector2(.385f, .735f)
                    };

                case "LegR/Shin":
                    return new[]
                    {
                        new Vector2(.60f, .625f),
                        new Vector2(.615f, .735f)
                    };

                case "LegL/Foot":
                    return new[] { new Vector2(.385f, .735f) };

                case "LegR/Foot":
                    return new[] { new Vector2(.615f, .735f) };

                case "Body/BellyFront":
                case "Clothes/ShirtBellyOverlay":
                    return new[] { new Vector2(.5f, .48f) };

                default:
                    return Array.Empty<Vector2>();
            }
        }

        private static void PaintJointScaffold(
            ImageData master,
            Color32[] result,
            Vector2 normalizedTopPoint)
        {
            const int radius = 5;
            int centerX = Mathf.Clamp(
                Mathf.RoundToInt(normalizedTopPoint.x * Width),
                0,
                Width - 1);
            int centerY = Mathf.Clamp(
                Mathf.RoundToInt(Height - normalizedTopPoint.y * Height),
                0,
                Height - 1);
            Color32 sample =
                FindNearestVisibleColor(master, centerX, centerY);

            int minX = Mathf.Max(0, centerX - radius);
            int maxX = Mathf.Min(Width - 1, centerX + radius);
            int minY = Mathf.Max(0, centerY - radius);
            int maxY = Mathf.Min(Height - 1, centerY + radius);
            int radiusSquared = radius * radius;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx * dx + dy * dy > radiusSquared)
                    {
                        continue;
                    }

                    int index = y * Width + x;
                    Color32 color = master.pixels[index];
                    if (color.a <= 8)
                    {
                        color = sample;
                    }

                    color.a = 255;
                    result[index] = color;
                }
            }
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
