using System;
using System.Collections.Generic;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Canonical names shared by Patch 4 runtime, editor validation and art export.
    /// Keep these values aligned with the Figma layer map and Unity Sprite Skin rig.
    /// </summary>
    public static class Patch4RigContract
    {
        public const string PatchName = "GameWork Patch 4.0";
        public const string CharacterRootName = "CharacterRoot";
        public const string RuntimeContinuousBodyLayerPath =
            "Body/TorsoBase";

        public static IReadOnlyList<string> RequiredBoneNames { get; } =
            Array.AsReadOnly(new[]
            {
                "Root",
                "CharacterRoot",
                "Pelvis",
                "SpineLower",
                "BellyBase",
                "BellyTip",
                "SpineUpper",
                "ChestSoftL",
                "ChestSoftR",
                "Neck",
                "Head",
                "Jaw",
                "BrowL",
                "BrowR",
                "EyeL",
                "EyeR",
                "ClavicleL",
                "UpperArmL",
                "ForearmL",
                "HandL",
                "ClavicleR",
                "UpperArmR",
                "ForearmR",
                "HandR",
                "ThighL",
                "ShinL",
                "FootL",
                "ThighR",
                "ShinR",
                "FootR",
                "GroundShadow"
            });

        private static readonly IReadOnlyDictionary<string, string>
            RequiredBoneParents = new Dictionary<string, string>(
                StringComparer.Ordinal)
            {
                ["CharacterRoot"] = "Root",
                ["Pelvis"] = "CharacterRoot",
                ["SpineLower"] = "Pelvis",
                ["BellyBase"] = "SpineLower",
                ["BellyTip"] = "BellyBase",
                ["SpineUpper"] = "SpineLower",
                ["ChestSoftL"] = "SpineUpper",
                ["ChestSoftR"] = "SpineUpper",
                ["Neck"] = "SpineUpper",
                ["Head"] = "Neck",
                ["Jaw"] = "Head",
                ["BrowL"] = "Head",
                ["BrowR"] = "Head",
                ["EyeL"] = "Head",
                ["EyeR"] = "Head",
                ["ClavicleL"] = "SpineUpper",
                ["UpperArmL"] = "ClavicleL",
                ["ForearmL"] = "UpperArmL",
                ["HandL"] = "ForearmL",
                ["ClavicleR"] = "SpineUpper",
                ["UpperArmR"] = "ClavicleR",
                ["ForearmR"] = "UpperArmR",
                ["HandR"] = "ForearmR",
                ["ThighL"] = "Pelvis",
                ["ShinL"] = "ThighL",
                ["FootL"] = "ShinL",
                ["ThighR"] = "Pelvis",
                ["ShinR"] = "ThighR",
                ["FootR"] = "ShinR",
                ["GroundShadow"] = "Root"
            };

        public static IReadOnlyList<string> RequiredLayerPaths { get; } =
            Array.AsReadOnly(new[]
            {
                "Body/TorsoBase",
                "Body/BellyFront",
                "Body/ChestSoft",
                "Body/Neck",
                "Head/HeadBase",
                "Head/EarL",
                "Head/EarR",
                "Face/BrowL",
                "Face/BrowR",
                "Face/EyeWhiteL",
                "Face/EyeWhiteR",
                "Face/IrisL",
                "Face/IrisR",
                "Face/LidL",
                "Face/LidR",
                "Face/Nose",
                "Face/MouthClosed",
                "Face/MouthOpen",
                "Face/MouthSmile",
                "Face/CheekL",
                "Face/CheekR",
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
                "Clothes/ShirtBase",
                "Clothes/ShirtBellyOverlay",
                "Clothes/Bottoms",
                "Clothes/Shoes",
                "FX/Sweat",
                "FX/ImpactFold",
                "FX/Shadow"
            });

        /// <summary>
        /// The neutral runtime stack is the exact continuous painted master.
        /// Its original eyes, irises and closed mouth stay intact; rebuilding
        /// those details from sparse color extraction visibly erased the face.
        /// Alternate expressions remain mutually exclusive overlays, while all
        /// anatomical cutouts stay hidden review/reference artwork.
        /// </summary>
        public static IReadOnlyList<string> RuntimeNeutralLayerPaths { get; } =
            Array.AsReadOnly(new[]
            {
                "Body/TorsoBase"
            });

        /// <summary>
        /// Sparse replacement/FX layers that must follow exactly one bone. The
        /// painted body itself uses one dense continuous Canvas deformation
        /// surface instead of separately transformed anatomical cutouts.
        /// </summary>
        public static IReadOnlyList<string> RuntimeRigidLayerPaths { get; } =
            Array.AsReadOnly(new[]
            {
                "Face/EyeWhiteL",
                "Face/EyeWhiteR",
                "Face/LidL",
                "Face/LidR",
                "Face/MouthClosed",
                "Face/MouthOpen",
                "Face/MouthSmile",
                "FX/Sweat",
                "FX/Shadow"
            });

        public static IReadOnlyList<string> RequiredClipNames { get; } =
            Array.AsReadOnly(new[]
            {
                "FatMan_Idle_Breathe",
                "FatMan_Idle_ShiftWeight",
                "FatMan_Blink_Random",
                "FatMan_LookAround",
                "FatMan_TapReact_01",
                "FatMan_TapReact_02",
                "FatMan_Walk_InRoom",
                "FatMan_Turn",
                "FatMan_SitOrLean",
                "FatMan_UpgradeReact"
            });

        public static IReadOnlyList<string> ProtectedPathFragments { get; } =
            Array.AsReadOnly(new[]
            {
                "MainMenuLoop.mp4",
                "/MainMenu/",
                "/Menu/",
                "/Music/",
                "/Audio/Mixers/",
                "/Settings/"
            });

        public static bool IsRuntimeNeutralLayer(string contractPath)
        {
            return Contains(RuntimeNeutralLayerPaths, contractPath);
        }

        public static bool IsRuntimeLayerVisibleByDefault(
            string contractPath)
        {
            return IsRuntimeNeutralLayer(contractPath) ||
                   string.Equals(
                       contractPath,
                       "FX/Shadow",
                       StringComparison.Ordinal);
        }

        public static bool RequiresRigidCanvasBinding(string contractPath)
        {
            return Contains(RuntimeRigidLayerPaths, contractPath);
        }

        public static bool IsRuntimeContinuousBodyLayer(
            string contractPath)
        {
            return string.Equals(
                contractPath,
                RuntimeContinuousBodyLayerPath,
                StringComparison.Ordinal);
        }

        public static bool TryGetRequiredParent(
            string boneName,
            out string parentName)
        {
            return RequiredBoneParents.TryGetValue(
                boneName ?? string.Empty,
                out parentName);
        }

        private static bool Contains(
            IReadOnlyList<string> paths,
            string contractPath)
        {
            if (paths == null || string.IsNullOrWhiteSpace(contractPath))
            {
                return false;
            }

            for (int i = 0; i < paths.Count; i++)
            {
                if (string.Equals(
                    paths[i],
                    contractPath,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
