using System.IO;
using SkinnyToBeast.Gameplay;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SkinnyToBeast.Editor
{
    internal sealed class RealFatManSpriteArchitectureGuard :
        IPreprocessBuildWithReport
    {
        private const string ManifestPath =
            "Assets/Resources/Characters/FatManLayered/Generated/manifest.json";
        private const string GeneratorPath =
            "Tools/FatManLayeredArt/generate_layered_art.py";
        private const string LayeredControllerPath =
            "Assets/Scripts/Gameplay/CharacterLayeredRigController.cs";
        private const string VisibilityGatePath =
            "Assets/Scripts/Gameplay/CharacterVisibilityGate.cs";
        private const string ViewControllerPath =
            "Assets/Scripts/Gameplay/CharacterViewController.cs";
        private const string ObsoleteSkinnedGraphicPath =
            "Assets/Scripts/Gameplay/CharacterSkinnedSpriteGraphic.cs";
        private const string PrefabPath =
            "Assets/Resources/UI/Gameplay/Living/CharacterRig2D.prefab";

        private static readonly string[] RequiredRepresentativeLayers =
        {
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Front/Pelvis.png",
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Front/Belly.png",
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Front/Head.png",
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Front/UpperArm_L.png",
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Front/Forearm_R.png",
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Front/Thigh_L.png",
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Front/Shin_R.png",
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Front/EyeL_Closed.png",
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Front/Mouth_Yawn.png",
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Side/Chest.png",
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Side/Foot_R.png",
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Back/Chest.png",
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Back/Foot_L.png"
        };

        public int callbackOrder => -950;

        public void OnPreprocessBuild(BuildReport report)
        {
            ValidateOrThrow();
        }

        [MenuItem(
            "Tools/Skinny to Beast/Validate Real Fat Man Layered Art 3.6")]
        private static void ValidateFromMenu()
        {
            ValidateOrThrow();
            Debug.Log(
                "Real Fat Man Layered Art Patch 3.6 guard passed: separate " +
                "transparent body parts, proxy-bone animation, facial states, " +
                "three directions, four stages and black-screen protection " +
                "are installed.");
        }

        private static void ValidateOrThrow()
        {
            string[] requiredFiles =
            {
                ManifestPath,
                GeneratorPath,
                LayeredControllerPath,
                VisibilityGatePath,
                ViewControllerPath,
                PrefabPath
            };
            for (int i = 0; i < requiredFiles.Length; i++)
            {
                RequireFile(requiredFiles[i]);
            }
            for (int i = 0; i < RequiredRepresentativeLayers.Length; i++)
            {
                RequireFile(RequiredRepresentativeLayers[i]);
            }

            if (File.Exists(ObsoleteSkinnedGraphicPath))
            {
                throw new BuildFailedException(
                    "Patch 3.6 forbids CharacterSkinnedSpriteGraphic. The " +
                    "whole-PNG deformation script must be removed.");
            }

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null ||
                prefab.GetComponent<CharacterLayeredRigController>() == null)
            {
                throw new BuildFailedException(
                    "CharacterRig2D.prefab must contain " +
                    "CharacterLayeredRigController.");
            }

            CharacterSpriteRigController flat =
                prefab.GetComponent<CharacterSpriteRigController>();
            if (flat != null && flat.enabled)
            {
                throw new BuildFailedException(
                    "The flat CharacterSpriteRigController must be disabled " +
                    "in the Patch 3.6 prefab. Run the 3.6 baker.");
            }

            string layeredSource =
                File.ReadAllText(LayeredControllerPath);
            RequireTokens(
                layeredSource,
                new[]
                {
                    "RealFatMan.LayeredArt3_6",
                    "Resources.Load<Texture2D>",
                    "ArtBone.",
                    "Layer.Pelvis",
                    "SuppressLegacyVisuals",
                    "SetFaceGroup",
                    "ScheduleBlink",
                    "TryGetWorldBounds",
                    "FitToScreenHeight",
                    "UsesNativeSideProfile"
                },
                "Patch 3.6 layered controller is missing token: ");
            ForbidTokens(
                layeredSource,
                new[]
                {
                    "CharacterSkinnedSpriteGraphic",
                    "Sprite.RealFatManLayeredSurface",
                    "RefreshDeformation",
                    "CaptureBindPose",
                    "GridColumns",
                    "whole-body mesh"
                },
                "Obsolete whole-PNG skinning returned in Patch 3.6: ");

            string gateSource = File.ReadAllText(VisibilityGatePath);
            RequireTokens(
                gateSource,
                new[]
                {
                    "CharacterLayeredRigController",
                    "layeredRigController.TryGetWorldBounds",
                    "layeredRigController.FitToScreenHeight",
                    "SafeVisibleMinimum",
                    "UsedVisibleSpriteFallback"
                },
                "Patch 3.6 visibility protection is missing token: ");

            string viewSource = File.ReadAllText(ViewControllerPath);
            RequireTokens(
                viewSource,
                new[]
                {
                    "UsesNativeSideProfile",
                    "facing == CharacterFacing.SideLeft",
                    "horizontal *= -1f"
                },
                "Patch 3.6 native side-view handling is missing token: ");
        }

        private static void RequireFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new BuildFailedException(
                    "Patch 3.6 required file is missing: " + path);
            }
        }

        private static void RequireTokens(
            string source,
            string[] tokens,
            string errorPrefix)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (!source.Contains(
                        tokens[i],
                        System.StringComparison.Ordinal))
                {
                    throw new BuildFailedException(
                        errorPrefix + tokens[i]);
                }
            }
        }

        private static void ForbidTokens(
            string source,
            string[] tokens,
            string errorPrefix)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (source.Contains(
                        tokens[i],
                        System.StringComparison.Ordinal))
                {
                    throw new BuildFailedException(
                        errorPrefix + tokens[i]);
                }
            }
        }
    }
}
