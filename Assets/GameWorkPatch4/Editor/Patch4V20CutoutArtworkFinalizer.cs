using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Converts the already-baked reference anatomy layers into an exclusive
    /// cutout puppet. Every visible master pixel belongs to one body piece,
    /// except for deliberately small joint overlaps. This prevents duplicated
    /// pixels from splitting apart when separate bones rotate.
    /// </summary>
    public static class Patch4V20CutoutArtworkFinalizer
    {
        private const int Width = 1024;
        private const int Height = 1536;
        private const byte AlphaThreshold = 8;

        private sealed class LayerData
        {
            public string path = string.Empty;
            public Color32[] pixels = Array.Empty<Color32>();
        }

        private readonly struct JointPair
        {
            public readonly string first;
            public readonly string second;
            public readonly float x;
            public readonly float topY;
            public readonly float radiusX;
            public readonly float radiusY;

            public JointPair(
                string first,
                string second,
                float x,
                float topY,
                float radiusX,
                float radiusY)
            {
                this.first = first;
                this.second = second;
                this.x = x;
                this.topY = topY;
                this.radiusX = radiusX;
                this.radiusY = radiusY;
            }
        }

        private static readonly string[] CutoutPaths =
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
        };

        private static readonly JointPair[] JointPairs =
        {
            new("Head/HeadBase", "Body/Neck", .500f, .225f, 20f, 18f),
            new("Body/Neck", "Clothes/ShirtBase", .500f, .300f, 26f, 18f),
            new("Clothes/ShirtBase", "ArmL/Upper", .340f, .285f, 26f, 20f),
            new("Clothes/ShirtBase", "ArmR/Upper", .660f, .285f, 26f, 20f),
            new("ArmL/Upper", "ArmL/Forearm", .285f, .405f, 20f, 24f),
            new("ArmR/Upper", "ArmR/Forearm", .715f, .405f, 20f, 24f),
            new("ArmL/Forearm", "ArmL/Hand", .255f, .495f, 16f, 20f),
            new("ArmR/Forearm", "ArmR/Hand", .745f, .495f, 16f, 20f),
            new("Clothes/ShirtBase", "LegL/Thigh", .420f, .505f, 24f, 20f),
            new("Clothes/ShirtBase", "LegR/Thigh", .580f, .505f, 24f, 20f),
            new("LegL/Thigh", "LegL/Shin", .400f, .625f, 22f, 25f),
            new("LegR/Thigh", "LegR/Shin", .600f, .625f, 22f, 25f),
            new("LegL/Shin", "LegL/Foot", .385f, .735f, 18f, 22f),
            new("LegR/Shin", "LegR/Foot", .615f, .735f, 18f, 22f)
        };

        public static void Apply()
        {
            Color32[] master = LoadPixels(
                Patch4MaskDrivenLayerBaker.MasterPath,
                out int masterWidth,
                out int masterHeight);
            if (master == null || masterWidth != Width || masterHeight != Height)
            {
                throw new InvalidOperationException(
                    "Patch 4 v20 cutout finalizer could not read the 1024x1536 master.");
            }

            Dictionary<string, LayerData> layers =
                new(StringComparer.Ordinal);
            for (int i = 0; i < CutoutPaths.Length; i++)
            {
                string path = CutoutPaths[i];
                string assetPath = LayerAssetPath(path);
                Color32[] pixels = LoadPixels(
                    assetPath,
                    out int width,
                    out int height);
                if (pixels == null || width != Width || height != Height)
                {
                    throw new InvalidOperationException(
                        "Patch 4 v20 cutout layer is missing or has the wrong size: " +
                        path);
                }

                layers[path] = new LayerData
                {
                    path = path,
                    pixels = pixels
                };
            }

            int assignedPixels = 0;
            int recoveredPixels = 0;
            int removedDuplicates = 0;

            for (int y = 0; y < Height; y++)
            {
                float topY = 1f - (y + .5f) / Height;
                for (int x = 0; x < Width; x++)
                {
                    int index = y * Width + x;
                    Color32 source = master[index];
                    if (source.a <= AlphaThreshold)
                    {
                        for (int i = 0; i < CutoutPaths.Length; i++)
                        {
                            layers[CutoutPaths[i]].pixels[index] = default;
                        }
                        continue;
                    }

                    float nx = (x + .5f) / Width;
                    string preferred = ResolvePreferredOwner(nx, topY);
                    string owner = string.Empty;
                    float bestDistance = float.PositiveInfinity;
                    int candidateCount = 0;

                    for (int i = 0; i < CutoutPaths.Length; i++)
                    {
                        string path = CutoutPaths[i];
                        if (layers[path].pixels[index].a <= AlphaThreshold)
                        {
                            continue;
                        }

                        candidateCount++;
                        float distance = string.Equals(
                            path,
                            preferred,
                            StringComparison.Ordinal)
                                ? -1f
                                : OwnerDistance(path, nx, topY);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            owner = path;
                        }
                    }

                    if (candidateCount == 0)
                    {
                        owner = preferred;
                        layers[owner].pixels[index] = source;
                        recoveredPixels++;
                    }

                    for (int i = 0; i < CutoutPaths.Length; i++)
                    {
                        string path = CutoutPaths[i];
                        if (string.Equals(path, owner, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        if (layers[path].pixels[index].a > AlphaThreshold)
                        {
                            removedDuplicates++;
                        }
                        layers[path].pixels[index] = default;
                    }

                    assignedPixels++;
                }
            }

            for (int i = 0; i < JointPairs.Length; i++)
            {
                RestoreJointOverlap(master, layers, JointPairs[i]);
            }

            int missingAfter = 0;
            int duplicateAfter = 0;
            for (int index = 0; index < master.Length; index++)
            {
                if (master[index].a <= AlphaThreshold)
                {
                    continue;
                }

                int owners = 0;
                for (int i = 0; i < CutoutPaths.Length; i++)
                {
                    if (layers[CutoutPaths[i]].pixels[index].a > AlphaThreshold)
                    {
                        owners++;
                    }
                }

                if (owners == 0)
                {
                    missingAfter++;
                }
                else if (owners > 1)
                {
                    duplicateAfter += owners - 1;
                }
            }

            if (missingAfter != 0)
            {
                throw new InvalidOperationException(
                    "Patch 4 v20 cutout ownership left " + missingAfter +
                    " visible master pixels without a body piece.");
            }

            for (int i = 0; i < CutoutPaths.Length; i++)
            {
                string path = CutoutPaths[i];
                WritePixels(LayerAssetPath(path), layers[path].pixels);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log(
                "Patch 4 v20 exclusive cutout art finalized: " +
                assignedPixels + " master pixels assigned, " +
                recoveredPixels + " recovered from region gaps, " +
                removedDuplicates + " broad duplicate pixels removed; " +
                duplicateAfter + " intentional joint-overlap pixels remain.");
        }

        private static void RestoreJointOverlap(
            IReadOnlyList<Color32> master,
            IReadOnlyDictionary<string, LayerData> layers,
            JointPair joint)
        {
            int centerX = Mathf.RoundToInt(joint.x * Width);
            int centerY = Mathf.RoundToInt(Height - joint.topY * Height);
            int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - joint.radiusX));
            int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(centerX + joint.radiusX));
            int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - joint.radiusY));
            int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(centerY + joint.radiusY));

            Color32[] first = layers[joint.first].pixels;
            Color32[] second = layers[joint.second].pixels;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - centerX) / Mathf.Max(1f, joint.radiusX);
                    float dy = (y - centerY) / Mathf.Max(1f, joint.radiusY);
                    if (dx * dx + dy * dy > 1f)
                    {
                        continue;
                    }

                    int index = y * Width + x;
                    Color32 source = master[index];
                    if (source.a <= AlphaThreshold)
                    {
                        continue;
                    }

                    first[index] = source;
                    second[index] = source;
                }
            }
        }

        private static string ResolvePreferredOwner(float x, float topY)
        {
            if (topY < .245f)
            {
                return "Head/HeadBase";
            }

            if (x < .37f && topY < .57f)
            {
                return ResolveArmOwner(true, topY);
            }

            if (x > .63f && topY < .57f)
            {
                return ResolveArmOwner(false, topY);
            }

            if (topY >= .48f)
            {
                return ResolveLegOwner(x < .5f, topY);
            }

            if (topY < .305f && x >= .39f && x <= .61f)
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

        private static float OwnerDistance(
            string path,
            float x,
            float topY)
        {
            Vector2 anchor = path switch
            {
                "Body/Neck" => new Vector2(.500f, .264f),
                "Head/HeadBase" => new Vector2(.500f, .185f),
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
                _ => new Vector2(.500f, .390f)
            };
            float dx = x - anchor.x;
            float dy = topY - anchor.y;
            return dx * dx + dy * dy;
        }

        private static string LayerAssetPath(string contractPath)
        {
            return Patch4MaskDrivenLayerBaker.LayerRoot + "/" +
                   contractPath.Replace('/', '_') + ".png";
        }

        private static Color32[] LoadPixels(
            string assetPath,
            out int width,
            out int height)
        {
            width = 0;
            height = 0;
            string absolute = ToAbsolutePath(assetPath);
            if (!File.Exists(absolute))
            {
                return null;
            }

            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(absolute), false))
                {
                    return null;
                }
                width = texture.width;
                height = texture.height;
                return texture.GetPixels32();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void WritePixels(string assetPath, Color32[] pixels)
        {
            Texture2D texture = new(
                Width,
                Height,
                TextureFormat.RGBA32,
                false,
                false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                string absolute = ToAbsolutePath(assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolute));
                File.WriteAllBytes(absolute, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(
                projectRoot,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
