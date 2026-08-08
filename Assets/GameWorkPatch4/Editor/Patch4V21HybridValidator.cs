using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Gates the architectural regressions found in v19/v20 before the full room
    /// review starts. This is intentionally complementary to the existing
    /// silhouette review: it checks the source/presentation structure itself.
    /// </summary>
    public static class Patch4V21HybridValidator
    {
        private const int Width = 1024;
        private const int Height = 1536;
        private const byte AlphaThreshold = 8;
        private const string PrefabPath =
            "Assets/GameWorkPatch4/Resources/FatMan_Patch4.prefab";

        private readonly struct JointProbe
        {
            public readonly string assetPath;
            public readonly string name;
            public readonly float x;
            public readonly float topY;
            public readonly float radius;

            public JointProbe(
                string assetPath,
                string name,
                float x,
                float topY,
                float radius)
            {
                this.assetPath = assetPath;
                this.name = name;
                this.x = x;
                this.topY = topY;
                this.radius = radius;
            }
        }

        public static void ValidateOrThrow()
        {
            List<string> errors = new();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                errors.Add("v21 runtime prefab is missing.");
            }
            else
            {
                if (prefab.GetComponent<Patch4V21HybridPuppetController>() == null)
                {
                    errors.Add("v21 hybrid puppet controller is missing.");
                }
                if (prefab.GetComponent<Patch4V21FootPlantController>() == null)
                {
                    errors.Add("v21 planted-foot controller is missing.");
                }
                if (prefab.GetComponent<Patch4CutoutPuppetController>() != null)
                {
                    errors.Add("v20 rigid cutout controller is still installed.");
                }
            }

            ValidateSprite(Patch4V21HybridArtworkBuilder.TorsoPath, errors);
            ValidateSprite(Patch4V21HybridArtworkBuilder.ArmLPath, errors);
            ValidateSprite(Patch4V21HybridArtworkBuilder.ArmRPath, errors);
            ValidateSprite(Patch4V21HybridArtworkBuilder.LegLPath, errors);
            ValidateSprite(Patch4V21HybridArtworkBuilder.LegRPath, errors);

            JointProbe[] probes =
            {
                new(Patch4V21HybridArtworkBuilder.ArmLPath, "left elbow", .285f, .405f, 31f),
                new(Patch4V21HybridArtworkBuilder.ArmLPath, "left wrist", .255f, .495f, 24f),
                new(Patch4V21HybridArtworkBuilder.ArmRPath, "right elbow", .715f, .405f, 31f),
                new(Patch4V21HybridArtworkBuilder.ArmRPath, "right wrist", .745f, .495f, 24f),
                new(Patch4V21HybridArtworkBuilder.LegLPath, "left knee", .400f, .625f, 34f),
                new(Patch4V21HybridArtworkBuilder.LegLPath, "left ankle", .385f, .735f, 27f),
                new(Patch4V21HybridArtworkBuilder.LegRPath, "right knee", .600f, .625f, 34f),
                new(Patch4V21HybridArtworkBuilder.LegRPath, "right ankle", .615f, .735f, 27f)
            };
            for (int i = 0; i < probes.Length; i++)
            {
                ValidateJointCoverage(probes[i], errors);
            }

            ValidateScaleProhibition(errors);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Patch 4 v21 hybrid validation failed:\n- " +
                    string.Join("\n- ", errors));
            }

            Debug.Log(
                "Patch 4 v21 hybrid validation passed: continuous limb art is " +
                "present through elbow/wrist/knee/ankle zones, v20 is not the " +
                "active presentation, foot planting is installed, and no core " +
                "body/limb Transform scale animation remains.");
        }

        private static void ValidateSprite(string path, ICollection<string> errors)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                errors.Add("Hybrid sprite missing: " + path);
                return;
            }
            if (Mathf.RoundToInt(sprite.rect.width) != Width ||
                Mathf.RoundToInt(sprite.rect.height) != Height)
            {
                errors.Add("Hybrid sprite is not full-canvas 1024x1536: " + path);
            }
        }

        private static void ValidateJointCoverage(
            JointProbe probe,
            ICollection<string> errors)
        {
            Color32[] pixels = LoadPixels(probe.assetPath);
            if (pixels == null)
            {
                return;
            }

            int centerX = Mathf.RoundToInt(probe.x * Width);
            int centerY = Mathf.RoundToInt(Height - probe.topY * Height);
            int radius = Mathf.CeilToInt(probe.radius);
            int visible = 0;
            int leftHalf = 0;
            int rightHalf = 0;

            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (y < 0 || y >= Height)
                {
                    continue;
                }
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x < 0 || x >= Width)
                    {
                        continue;
                    }
                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx * dx + dy * dy > radius * radius)
                    {
                        continue;
                    }
                    if (pixels[y * Width + x].a <= AlphaThreshold)
                    {
                        continue;
                    }

                    visible++;
                    if (y < centerY)
                    {
                        leftHalf++;
                    }
                    else
                    {
                        rightHalf++;
                    }
                }
            }

            if (visible < 180 || leftHalf < 45 || rightHalf < 45)
            {
                errors.Add(
                    probe.name + " does not have continuous painted coverage " +
                    "through both sides of its joint band (visible=" + visible +
                    ", halves=" + leftHalf + "/" + rightHalf + ").");
            }
        }

        private static void ValidateScaleProhibition(ICollection<string> errors)
        {
            string[] clips =
            {
                "FatMan_Idle_Breathe",
                "FatMan_Idle_ShiftWeight",
                "FatMan_LookAround",
                "FatMan_TapReact_01",
                "FatMan_TapReact_02",
                "FatMan_Walk_InRoom",
                "FatMan_Turn",
                "FatMan_SitOrLean",
                "FatMan_UpgradeReact"
            };

            for (int clipIndex = 0; clipIndex < clips.Length; clipIndex++)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    "Assets/GameWorkPatch4/Animations/" + clips[clipIndex] + ".anim");
                if (clip == null)
                {
                    errors.Add("Animation clip missing: " + clips[clipIndex]);
                    continue;
                }

                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
                for (int i = 0; i < bindings.Length; i++)
                {
                    EditorCurveBinding binding = bindings[i];
                    if (binding.type != typeof(Transform) ||
                        string.IsNullOrEmpty(binding.path) ||
                        !binding.propertyName.StartsWith(
                            "m_LocalScale.",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (binding.path.EndsWith(
                        "/BellyTip",
                        StringComparison.Ordinal))
                    {
                        continue;
                    }

                    errors.Add(
                        clips[clipIndex] + " still animates forbidden scale at " +
                        binding.path + " / " + binding.propertyName);
                }
            }
        }

        private static Color32[] LoadPixels(string assetPath)
        {
            string absolute = ToAbsolutePath(assetPath);
            if (!File.Exists(absolute))
            {
                return null;
            }

            Texture2D texture = new(2, 2, TextureFormat.RGBA32, false, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(absolute), false) ||
                    texture.width != Width || texture.height != Height)
                {
                    return null;
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
