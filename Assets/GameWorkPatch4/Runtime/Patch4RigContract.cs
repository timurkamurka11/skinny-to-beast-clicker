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
        /// The one neutral visual stack that is allowed to render at runtime.
        /// The remaining required layers are alternate poses, transient FX or
        /// review/reference artwork; rendering those reference copies together
        /// is what previously produced doubled limbs and a detached face.
        /// </summary>
        public static IReadOnlyList<string> RuntimeNeutralLayerPaths { get; } =
            Array.AsReadOnly(new[]
            {
                "Body/Neck",
                "Head/HeadBase",
                "Face/EyeWhiteL",
                "Face/EyeWhiteR",
                "Face/MouthClosed",
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

        /// <summary>
        /// Source-art layers that divide the neutral master into one exclusive
        /// owner per pixel. Face replacements are intentionally excluded: the
        /// head contains a painted skin underlay beneath those features.
        /// </summary>
        public static IReadOnlyList<string>
            RuntimeExclusiveArtworkLayerPaths { get; } =
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

        /// <summary>
        /// Cutout pieces that must follow exactly one bone. Only the central
        /// shirt is allowed to use a soft multi-bone Canvas grid.
        /// </summary>
        public static IReadOnlyList<string> RuntimeRigidLayerPaths { get; } =
            Array.AsReadOnly(new[]
            {
                "Body/Neck",
                "Head/HeadBase",
                "Face/EyeWhiteL",
                "Face/EyeWhiteR",
                "Face/LidL",
                "Face/LidR",
                "Face/MouthClosed",
                "Face/MouthOpen",
                "Face/MouthSmile",
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
