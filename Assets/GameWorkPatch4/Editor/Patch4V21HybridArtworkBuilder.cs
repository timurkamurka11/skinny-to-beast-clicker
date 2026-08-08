using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// v21 bridge artwork. Unlike v20, this never assigns each master pixel to
    /// an exclusive rigid body segment. Elbow/wrist and knee/ankle pieces are
    /// merged back into one continuous painted limb, while only the proximal
    /// shoulder/hip attachment is hidden behind the torso. This gives local
    /// skinning a continuous surface instead of a paper-doll cut line.
    ///
    /// The final production target remains a hand-authored layered PSB with
    /// real hidden underpaint. These deterministic bridge assets exist so the
    /// current repository can stop showing the known v19/v20 failure modes now.
    /// </summary>
    public static class Patch4V21HybridArtworkBuilder
    {
        public const string HybridRoot =
            "Assets/GameWorkPatch4/Art/Character/FatMan/HybridV21";
        public const string TorsoPath = HybridRoot + "/FatMan_V21_TorsoCore.png";
        public const string ArmLPath = HybridRoot + "/FatMan_V21_ArmL_Whole.png";
        public const string ArmRPath = HybridRoot + "/FatMan_V21_ArmR_Whole.png";
        public const string LegLPath = HybridRoot + "/FatMan_V21_LegL_Whole.png";
        public const string LegRPath = HybridRoot + "/FatMan_V21_LegR_Whole.png";

        private const int Width = 1024;
        private const int Height = 1536;
        private const byte VisibleThreshold = 8;

        private readonly struct PivotSpec
        {
            public readonly string path;
            public readonly Vector2 pivot;

            public PivotSpec(string path, float pixelX, float pixelTopY)
            {
                this.path = path;
                pivot = new Vector2(
                    pixelX / Width,
                    1f - pixelTopY / Height);
            }
        }

        public static void Build()
        {
            EnsureFolder(HybridRoot);

            Color32[] master = LoadRequired(
                Patch4MaskDrivenLayerBaker.MasterPath);

            Color32[] torso = NewCanvas();
            MergeInto(torso, LoadLayer("Clothes/ShirtBase"));
            MergeCentralPelvis(
                torso,
                LoadLayer("Clothes/Bottoms"),
                master);

            Color32[] armL = BuildContinuousLimb(
                master,
                new[] { "ArmL/Upper", "ArmL/Forearm", "ArmL/Hand" },
                .340f,
                .285f,
                38f,
                31f);
            Color32[] armR = BuildContinuousLimb(
                master,
                new[] { "ArmR/Upper", "ArmR/Forearm", "ArmR/Hand" },
                .660f,
                .285f,
                38f,
                31f);
            Color32[] legL = BuildContinuousLimb(
                master,
                new[] { "LegL/Thigh", "LegL/Shin", "LegL/Foot" },
                .420f,
                .505f,
                40f,
                34f);
            Color32[] legR = BuildContinuousLimb(
                master,
                new[] { "LegR/Thigh", "LegR/Shin", "LegR/Foot" },
                .580f,
                .505f,
                40f,
                34f);

            WritePng(TorsoPath, torso);
            WritePng(ArmLPath, armL);
            WritePng(ArmRPath, armR);
            WritePng(LegLPath, legL);
            WritePng(LegRPath, legR);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            PivotSpec[] pivots =
            {
                new(TorsoPath, 512f, 675f),
                new(ArmLPath, 355f, 480f),
                new(ArmRPath, 669f, 480f),
                new(LegLPath, 440f, 870f),
                new(LegRPath, 584f, 870f)
            };
            for (int i = 0; i < pivots.Length; i++)
            {
                ConfigureImporter(pivots[i]);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log(
                "Patch 4 v21 hybrid artwork built: torso + four continuous " +
                "whole-limb sprites. No elbow/wrist/knee/ankle pixel ownership " +
                "cuts are produced; proximal shoulder/hip underpaint is kept " +
                "behind the torso as a guarded overlap.");
        }

        private static Color32[] BuildContinuousLimb(
            IReadOnlyList<Color32> master,
            IReadOnlyList<string> sourcePaths,
            float jointX,
            float jointTopY,
            float radiusX,
            float radiusY)
        {
            Color32[] result = NewCanvas();
            for (int i = 0; i < sourcePaths.Count; i++)
            {
                MergeInto(result, LoadLayer(sourcePaths[i]));
            }

            // The overlap is deliberately copied from the approved master and
            // remains behind the torso at runtime. It is not an exposed exact
            // cut, and it is larger than the old v20 micro-overlap.
            RestoreMasterEllipse(
                result,
                master,
                jointX,
                jointTopY,
                radiusX,
                radiusY);
            return result;
        }

        private static void MergeCentralPelvis(
            Color32[] target,
            IReadOnlyList<Color32> bottoms,
            IReadOnlyList<Color32> master)
        {
            for (int y = 0; y < Height; y++)
            {
                float topY = 1f - (y + .5f) / Height;
                if (topY < .455f || topY > .565f)
                {
                    continue;
                }

                for (int x = 0; x < Width; x++)
                {
                    float nx = (x + .5f) / Width;
                    float edge = Mathf.Min(
                        Mathf.InverseLerp(.30f, .345f, nx),
                        Mathf.InverseLerp(.70f, .655f, nx));
                    float coverage = Mathf.Clamp01(edge);
                    if (coverage <= 0f)
                    {
                        continue;
                    }

                    int index = y * Width + x;
                    Color32 source = bottoms[index];
                    if (source.a <= VisibleThreshold)
                    {
                        source = master[index];
                    }
                    if (source.a <= VisibleThreshold)
                    {
                        continue;
                    }

                    source.a = (byte)Mathf.RoundToInt(source.a * coverage);
                    KeepStronger(target, index, source);
                }
            }
        }

        private static void RestoreMasterEllipse(
            Color32[] target,
            IReadOnlyList<Color32> master,
            float normalizedX,
            float topY,
            float radiusX,
            float radiusY)
        {
            int centerX = Mathf.RoundToInt(normalizedX * Width);
            int centerY = Mathf.RoundToInt(Height - topY * Height);
            int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - radiusX));
            int maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(centerX + radiusX));
            int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - radiusY));
            int maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(centerY + radiusY));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x - centerX) / Mathf.Max(1f, radiusX);
                    float dy = (y - centerY) / Mathf.Max(1f, radiusY);
                    float squared = dx * dx + dy * dy;
                    if (squared > 1f)
                    {
                        continue;
                    }

                    int index = y * Width + x;
                    Color32 source = master[index];
                    if (source.a <= VisibleThreshold)
                    {
                        continue;
                    }

                    // Feather only the outer rim. The center is fully retained
                    // as hidden overlap; the torso covers it in neutral pose.
                    float coverage = 1f - Mathf.SmoothStep(.78f, 1f, squared);
                    source.a = (byte)Mathf.RoundToInt(source.a * coverage);
                    KeepStronger(target, index, source);
                }
            }
        }

        private static void MergeInto(
            Color32[] target,
            IReadOnlyList<Color32> source)
        {
            if (source == null || source.Count != target.Length)
            {
                throw new InvalidOperationException(
                    "Patch 4 v21 cannot merge a missing or mismatched layer.");
            }

            for (int i = 0; i < target.Length; i++)
            {
                KeepStronger(target, i, source[i]);
            }
        }

        private static void KeepStronger(
            Color32[] target,
            int index,
            Color32 source)
        {
            if (source.a > target[index].a)
            {
                target[index] = source;
            }
        }

        private static Color32[] LoadLayer(string contractPath)
        {
            return LoadRequired(
                Patch4MaskDrivenLayerBaker.LayerRoot + "/" +
                contractPath.Replace('/', '_') + ".png");
        }

        private static Color32[] LoadRequired(string assetPath)
        {
            string absolute = ToAbsolutePath(assetPath);
            if (!File.Exists(absolute))
            {
                throw new FileNotFoundException(
                    "Patch 4 v21 source image is missing.",
                    assetPath);
            }

            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(absolute), false) ||
                    texture.width != Width ||
                    texture.height != Height)
                {
                    throw new InvalidDataException(
                        "Patch 4 v21 source must be 1024x1536: " + assetPath);
                }
                return texture.GetPixels32();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Color32[] NewCanvas()
        {
            return new Color32[Width * Height];
        }

        private static void WritePng(string assetPath, Color32[] pixels)
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

        private static void ConfigureImporter(PivotSpec spec)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(spec.path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "Patch 4 v21 could not configure sprite importer: " + spec.path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            TextureImporterPlatformSettings platform =
                importer.GetDefaultPlatformTextureSettings();
            platform.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(platform);

            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = 100f;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = spec.pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(
                root,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
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
