using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Placement metadata for 1024x1536 full-canvas transparent layer PNGs.
    /// Every pivot matches the parent bone position from the approved master.
    /// </summary>
    public static class Patch4LayerPlacement
    {
        private const float MasterWidth = 1024f;
        private const float MasterHeight = 1536f;

        private static readonly IReadOnlyDictionary<string, Vector2> BonePixels =
            new Dictionary<string, Vector2>(StringComparer.Ordinal)
            {
                ["Root"] = new(512f, 1536f),
                ["CharacterRoot"] = new(512f, 1536f),
                ["Pelvis"] = new(512f, 830f),
                ["SpineLower"] = new(512f, 675f),
                ["BellyBase"] = new(512f, 610f),
                ["BellyTip"] = new(512f, 750f),
                ["SpineUpper"] = new(512f, 510f),
                ["Neck"] = new(512f, 405f),
                ["Head"] = new(512f, 285f),
                ["Jaw"] = new(512f, 350f),
                ["BrowL"] = new(472f, 267f),
                ["BrowR"] = new(552f, 267f),
                ["EyeL"] = new(472f, 292f),
                ["EyeR"] = new(552f, 292f),
                ["UpperArmL"] = new(355f, 480f),
                ["ForearmL"] = new(300f, 650f),
                ["HandL"] = new(270f, 810f),
                ["UpperArmR"] = new(669f, 480f),
                ["ForearmR"] = new(724f, 650f),
                ["HandR"] = new(754f, 810f),
                ["ThighL"] = new(440f, 870f),
                ["ShinL"] = new(430f, 1040f),
                ["FootL"] = new(415f, 1190f),
                ["ThighR"] = new(584f, 870f),
                ["ShinR"] = new(594f, 1040f),
                ["FootR"] = new(609f, 1190f),
                ["GroundShadow"] = new(512f, 1524f)
            };

        public static string ContractPathFromAssetPath(string assetPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            int separator = fileName.IndexOf('_');
            return separator > 0
                ? fileName.Substring(0, separator) + "/" +
                  fileName.Substring(separator + 1)
                : fileName;
        }

        public static Vector2 ResolvePivotNormalized(string contractPath)
        {
            string parentBone = ResolveParentBone(contractPath);
            if (!BonePixels.TryGetValue(parentBone, out Vector2 pixel))
            {
                return new Vector2(0.5f, 0.5f);
            }

            return new Vector2(
                Mathf.Clamp01(pixel.x / MasterWidth),
                Mathf.Clamp01(1f - pixel.y / MasterHeight));
        }

        public static string ResolveParentBone(string path)
        {
            if (path.StartsWith("ArmL/Upper", StringComparison.Ordinal)) return "UpperArmL";
            if (path.StartsWith("ArmL/Forearm", StringComparison.Ordinal)) return "ForearmL";
            if (path.StartsWith("ArmL/Hand", StringComparison.Ordinal)) return "HandL";
            if (path.StartsWith("ArmR/Upper", StringComparison.Ordinal)) return "UpperArmR";
            if (path.StartsWith("ArmR/Forearm", StringComparison.Ordinal)) return "ForearmR";
            if (path.StartsWith("ArmR/Hand", StringComparison.Ordinal)) return "HandR";
            if (path.StartsWith("LegL/Thigh", StringComparison.Ordinal)) return "ThighL";
            if (path.StartsWith("LegL/Shin", StringComparison.Ordinal)) return "ShinL";
            if (path.StartsWith("LegL/Foot", StringComparison.Ordinal)) return "FootL";
            if (path.StartsWith("LegR/Thigh", StringComparison.Ordinal)) return "ThighR";
            if (path.StartsWith("LegR/Shin", StringComparison.Ordinal)) return "ShinR";
            if (path.StartsWith("LegR/Foot", StringComparison.Ordinal)) return "FootR";

            return path switch
            {
                "Body/TorsoBase" => "SpineLower",
                "Body/BellyFront" => "BellyBase",
                "Body/ChestSoft" => "SpineUpper",
                "Body/Neck" => "Neck",
                "Head/HeadBase" => "Head",
                "Head/EarL" => "Head",
                "Head/EarR" => "Head",
                "Face/BrowL" => "BrowL",
                "Face/BrowR" => "BrowR",
                "Face/EyeWhiteL" => "EyeL",
                "Face/EyeWhiteR" => "EyeR",
                "Face/IrisL" => "EyeL",
                "Face/IrisR" => "EyeR",
                "Face/LidL" => "EyeL",
                "Face/LidR" => "EyeR",
                "Face/Nose" => "Head",
                "Face/MouthClosed" => "Jaw",
                "Face/MouthOpen" => "Jaw",
                "Face/MouthSmile" => "Jaw",
                "Face/CheekL" => "Head",
                "Face/CheekR" => "Head",
                "Clothes/ShirtBase" => "SpineLower",
                "Clothes/ShirtBellyOverlay" => "BellyBase",
                "Clothes/Bottoms" => "Pelvis",
                "Clothes/Shoes" => "CharacterRoot",
                "FX/Sweat" => "Head",
                "FX/ImpactFold" => "BellyTip",
                "FX/Shadow" => "GroundShadow",
                _ => Patch4RigContract.CharacterRootName
            };
        }

        public static int ResolveSortingOrder(string path)
        {
            if (path == "FX/Shadow") return -100;
            if (path.StartsWith("Leg", StringComparison.Ordinal)) return 10;
            if (path == "Clothes/Bottoms") return 20;
            if (path.StartsWith("Body/", StringComparison.Ordinal)) return 40;
            if (path.StartsWith("Clothes/Shirt", StringComparison.Ordinal)) return 50;
            if (path.StartsWith("Arm", StringComparison.Ordinal)) return 60;
            if (path.StartsWith("Head/", StringComparison.Ordinal)) return 80;
            if (path.StartsWith("Face/", StringComparison.Ordinal)) return 100;
            if (path.StartsWith("FX/", StringComparison.Ordinal)) return 120;
            return 0;
        }
    }
}
