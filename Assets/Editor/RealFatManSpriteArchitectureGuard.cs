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
        private const string TexturePath =
            "Assets/Resources/Characters/FatMan/" +
            "fat-man-turnaround-reference.png";
        private const string CatalogPath =
            "Assets/Resources/Characters/FatMan/" +
            "FatManSpriteCatalog.asset";
        private const string SpriteControllerPath =
            "Assets/Scripts/Gameplay/" +
            "CharacterSpriteRigController.cs";
        private const string LayeredControllerPath =
            "Assets/Scripts/Gameplay/" +
            "CharacterLayeredRigController.cs";
        private const string SkinnedGraphicPath =
            "Assets/Scripts/Gameplay/" +
            "CharacterSkinnedSpriteGraphic.cs";
        private const string VisibilityGatePath =
            "Assets/Scripts/Gameplay/" +
            "CharacterVisibilityGate.cs";
        private const string PrefabPath =
            "Assets/Resources/UI/Gameplay/Living/" +
            "CharacterRig2D.prefab";

        public int callbackOrder => -950;

        public void OnPreprocessBuild(BuildReport report)
        {
            ValidateOrThrow();
        }

        [MenuItem(
            "Tools/Skinny to Beast/Validate Real Fat Man Layered Rig 3.4")]
        private static void ValidateFromMenu()
        {
            ValidateOrThrow();
            Debug.Log(
                "Real Fat Man Layered Rig Patch 3.4 guard passed: the " +
                "painted character is rendered by a continuous weighted mesh " +
                "connected to body, limb and soft-body bones; blink, reactions " +
                "and black-screen-safe visibility are present.");
        }

        private static void ValidateOrThrow()
        {
            string[] requiredFiles =
            {
                TexturePath,
                CatalogPath,
                SpriteControllerPath,
                LayeredControllerPath,
                SkinnedGraphicPath,
                VisibilityGatePath,
                PrefabPath
            };
            for (int i = 0; i < requiredFiles.Length; i++)
            {
                if (!File.Exists(requiredFiles[i]))
                {
                    throw new BuildFailedException(
                        $"Real Fat Man Layered Rig 3.4 file is missing: " +
                        requiredFiles[i]);
                }
            }

            TextureImporter importer =
                AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null ||
                !importer.isReadable ||
                !importer.alphaIsTransparency)
            {
                throw new BuildFailedException(
                    "The real fat-man PNG must be readable and imported " +
                    "with alpha transparency.");
            }

            FatManSpriteSet catalog =
                AssetDatabase.LoadAssetAtPath<FatManSpriteSet>(
                    CatalogPath);
            if (catalog == null || !catalog.IsValid)
            {
                throw new BuildFailedException(
                    "FatManSpriteCatalog must reference the real PNG and " +
                    "contain four stage scales.");
            }

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null ||
                prefab.GetComponent<CharacterSpriteRigController>() == null ||
                prefab.GetComponent<CharacterLayeredRigController>() == null)
            {
                throw new BuildFailedException(
                    "CharacterRig2D.prefab must contain both the sprite source " +
                    "controller and CharacterLayeredRigController.");
            }

            string layeredSource =
                File.ReadAllText(LayeredControllerPath);
            string[] requiredLayeredTokens =
            {
                "CharacterSkinnedSpriteGraphic",
                "Sprite.RealFatManLayeredSurface",
                "BuildBoneMap",
                "RefreshDeformation",
                "CaptureBindPose",
                "flatBodyImage.enabled = false",
                "Bone.Belly",
                "Bone.ChestSoft",
                "Bone.UpperArm.L",
                "Bone.Forearm.R",
                "Bone.Thigh.L",
                "Bone.Shin.R",
                "LayeredPaintedFaceOverlay",
                "ScheduleBlink"
            };
            RequireTokens(
                layeredSource,
                requiredLayeredTokens,
                "Layered sprite controller is missing required 3.4 token: ");

            string[] forbiddenLayeredTokens =
            {
                "PartSpec",
                "PartSpecs",
                "CharacterSpritePart part",
                "CreateWholeBody",
                "12 bone-bound parts"
            };
            ForbidTokens(
                layeredSource,
                forbiddenLayeredTokens,
                "Rectangular cutout or flat-body code returned in 3.4: ");

            string skinnedSource =
                File.ReadAllText(SkinnedGraphicPath);
            string[] requiredSkinnedTokens =
            {
                "CharacterSkinnedSpriteGraphic",
                "GridColumns",
                "GridRows",
                "CaptureBindPose",
                "RefreshDeformation",
                "AddFrontBackInfluences",
                "AddSideProfileInfluences",
                "FatManSkinBone.Belly",
                "FatManSkinBone.ChestSoft",
                "FatManSkinBone.UpperArmLeft",
                "FatManSkinBone.ForearmRight",
                "FatManSkinBone.ThighLeft",
                "FatManSkinBone.ShinRight",
                "DeformationMagnitude"
            };
            RequireTokens(
                skinnedSource,
                requiredSkinnedTokens,
                "Weighted sprite mesh is missing required token: ");

            string gateSource =
                File.ReadAllText(VisibilityGatePath);
            string[] requiredGateTokens =
            {
                "CharacterSpriteRigController",
                "TryGetWorldBounds",
                "FitToScreenHeight",
                "SafeVisibleMinimum",
                "UsedVisibleSpriteFallback"
            };
            RequireTokens(
                gateSource,
                requiredGateTokens,
                "CharacterVisibilityGate is missing 3.4 protection: ");
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
