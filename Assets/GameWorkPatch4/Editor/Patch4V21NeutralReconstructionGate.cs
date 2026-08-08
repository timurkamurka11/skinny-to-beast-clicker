using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// First visual gate from the v21 research: before any animation is trusted,
    /// the union of the hybrid neutral artwork must still reconstruct almost all
    /// of the approved master silhouette. This catches missing shoulder/hip/body
    /// regions before Animator motion can hide the underlying art problem.
    /// </summary>
    public static class Patch4V21NeutralReconstructionGate
    {
        private const int Width = 1024;
        private const int Height = 1536;
        private const byte AlphaThreshold = 8;
        private const float MinimumMasterCoverage = .94f;
        private const float MaximumLeakage = .004f;

        public static void ValidateOrThrow()
        {
            Color32[] master = Load(Patch4MaskDrivenLayerBaker.MasterPath);
            Color32[][] layers =
            {
                Load(Patch4V21HybridArtworkBuilder.TorsoPath),
                Load(Patch4V21HybridArtworkBuilder.ArmLPath),
                Load(Patch4V21HybridArtworkBuilder.ArmRPath),
                Load(Patch4V21HybridArtworkBuilder.LegLPath),
                Load(Patch4V21HybridArtworkBuilder.LegRPath),
                Load(LayerPath("Body/Neck")),
                Load(LayerPath("Head/HeadBase"))
            };

            int masterVisible = 0;
            int covered = 0;
            int leak = 0;
            for (int i = 0; i < master.Length; i++)
            {
                bool masterOn = master[i].a > AlphaThreshold;
                bool unionOn = false;
                for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
                {
                    if (layers[layerIndex][i].a > AlphaThreshold)
                    {
                        unionOn = true;
                        break;
                    }
                }

                if (masterOn)
                {
                    masterVisible++;
                    if (unionOn)
                    {
                        covered++;
                    }
                }
                else if (unionOn)
                {
                    leak++;
                }
            }

            float coverage = masterVisible > 0
                ? covered / (float)masterVisible
                : 0f;
            float leakage = masterVisible > 0
                ? leak / (float)masterVisible
                : 1f;

            if (coverage < MinimumMasterCoverage || leakage > MaximumLeakage)
            {
                throw new InvalidOperationException(
                    "Patch 4 v21 neutral reconstruction gate failed: master " +
                    "coverage=" + (coverage * 100f).ToString("F2") + "% (min " +
                    (MinimumMasterCoverage * 100f).ToString("F1") + "%), leakage=" +
                    (leakage * 100f).ToString("F3") + "% (max " +
                    (MaximumLeakage * 100f).ToString("F2") + "%). Animation review " +
                    "is blocked because the neutral hybrid art is incomplete.");
            }

            Debug.Log(
                "Patch 4 v21 neutral reconstruction passed: " +
                (coverage * 100f).ToString("F2") + "% of the approved master " +
                "silhouette is reconstructed before animation; leakage=" +
                (leakage * 100f).ToString("F3") + "%.");
        }

        private static string LayerPath(string contractPath)
        {
            return Patch4MaskDrivenLayerBaker.LayerRoot + "/" +
                   contractPath.Replace('/', '_') + ".png";
        }

        private static Color32[] Load(string assetPath)
        {
            string absolute = ToAbsolutePath(assetPath);
            if (!File.Exists(absolute))
            {
                throw new FileNotFoundException(
                    "Patch 4 v21 neutral source is missing.",
                    assetPath);
            }

            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(absolute), false) ||
                    texture.width != Width || texture.height != Height)
                {
                    throw new InvalidDataException(
                        "Patch 4 v21 neutral source must be 1024x1536: " +
                        assetPath);
                }
                return texture.GetPixels32();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(
                root,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
