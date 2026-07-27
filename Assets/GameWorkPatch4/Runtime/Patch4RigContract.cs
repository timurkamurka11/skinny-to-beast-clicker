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
    }
}
