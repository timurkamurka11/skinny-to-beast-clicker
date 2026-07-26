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
        private const string PuppetControllerPath =
            "Assets/Scripts/Gameplay/" +
            "CharacterLayeredRigController.cs";
        private const string PuppetGraphicPath =
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
            "Tools/Skinny to Beast/Validate Real Fat Man Rig Rebuild 3.5")]
        private static void ValidateFromMenu()
        {
            ValidateOrThrow();
            Debug.Log(
                "Real Fat Man Rig Rebuild Patch 3.5 guard passed: the old " +
                "skeleton is an animation signal only; directional art anchors, " +
                "bounded motion, displacement clamping, fold repair, stable " +
                "facial attachment and black-screen protection are present.");
        }

        private static void ValidateOrThrow()
        {
            string[] requiredFiles =
            {
                TexturePath,
                CatalogPath,
                SpriteControllerPath,
                PuppetControllerPath,
                PuppetGraphicPath,
                VisibilityGatePath,
                PrefabPath
            };
            for (int i = 0; i < requiredFiles.Length; i++)
            {
                if (!File.Exists(requiredFiles[i]))
                {
                    throw new BuildFailedException(
                        "Real Fat Man Rig Rebuild 3.5 file is missing: " +
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
                AssetDatabase.LoadAssetAtPath<FatManSpriteSet>(CatalogPath);
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
                    "CharacterRig2D.prefab must contain both " +
                    "CharacterSpriteRigController and " +
                    "CharacterLayeredRigController.");
            }

            string controllerSource =
                File.ReadAllText(PuppetControllerPath);
            RequireTokens(
                controllerSource,
                new[]
                {
                    "Sprite.RealFatManBoundedPuppet",
                    "CharacterSkinnedSpriteGraphic",
                    "BuildBoneMap",
                    "RefreshDeformation",
                    "TryGetDrivenPoint",
                    "flatBodyImage.enabled = false",
                    "Bone.Belly",
                    "Bone.ChestSoft",
                    "Bone.UpperArm.L",
                    "Bone.Forearm.R",
                    "Bone.Thigh.L",
                    "Bone.Shin.R",
                    "BoundedPaintedFaceOverlay",
                    "ScheduleBlink",
                    "FoldRepairCount",
                    "SafetyClampCount"
                },
                "Bounded puppet controller is missing required 3.5 token: ");
            ForbidTokens(
                controllerSource,
                new[]
                {
                    "Sprite.RealFatManLayeredSurface",
                    "LayeredPaintedFaceOverlay",
                    "skinnedGraphic.CaptureBindPose()",
                    "PartSpec",
                    "12 bone-bound parts"
                },
                "Unstable 3.2/3.4 controller logic returned: ");

            string graphicSource =
                File.ReadAllText(PuppetGraphicPath);
            RequireTokens(
                graphicSource,
                new[]
                {
                    "MinimumRootWeight",
                    "GetDirectionalAnchor",
                    "GetFrontBackAnchor",
                    "GetSideAnchor",
                    "GetMotionRule",
                    "GetMaximumVertexDisplacement",
                    "Vector2.ClampMagnitude",
                    "SmoothDisplacements",
                    "ApplySafetyEnvelope",
                    "RepairFoldedCells",
                    "IsValidCell",
                    "TryGetDrivenPoint",
                    "FatManSkinBone.Belly",
                    "FatManSkinBone.ChestSoft",
                    "FatManSkinBone.UpperArmLeft",
                    "FatManSkinBone.ForearmRight",
                    "FatManSkinBone.ThighLeft",
                    "FatManSkinBone.ShinRight"
                },
                "Bounded puppet mesh is missing required 3.5 token: ");
            ForbidTokens(
                graphicSource,
                new[]
                {
                    "bindInverse",
                    "boneDeltas",
                    "AddSideProfileInfluences",
                    "GridColumns = 24",
                    "GridRows = 38"
                },
                "Unrestricted Patch 3.4 matrix skinning returned: ");

            string gateSource = File.ReadAllText(VisibilityGatePath);
            RequireTokens(
                gateSource,
                new[]
                {
                    "CharacterSpriteRigController",
                    "TryGetWorldBounds",
                    "FitToScreenHeight",
                    "SafeVisibleMinimum",
                    "UsedVisibleSpriteFallback"
                },
                "CharacterVisibilityGate is missing 3.5 protection: ");
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
