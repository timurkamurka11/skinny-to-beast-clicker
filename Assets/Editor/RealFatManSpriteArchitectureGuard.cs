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
        private const string ControllerPath =
            "Assets/Scripts/Gameplay/" +
            "CharacterSpriteRigController.cs";
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
            "Tools/Skinny to Beast/Validate Real Fat Man Sprite 3.3")]
        private static void ValidateFromMenu()
        {
            ValidateOrThrow();
            Debug.Log(
                "Real Fat Man Sprite Patch 3.3 guard passed: one intact " +
                "painted body, three directions, four stages, real sprite " +
                "bounds and black-screen-safe visibility fitting.");
        }

        private static void ValidateOrThrow()
        {
            string[] requiredFiles =
            {
                TexturePath,
                CatalogPath,
                ControllerPath,
                VisibilityGatePath,
                PrefabPath
            };
            for (int i = 0; i < requiredFiles.Length; i++)
            {
                if (!File.Exists(requiredFiles[i]))
                {
                    throw new BuildFailedException(
                        $"Real Fat Man Sprite 3.3 file is missing: " +
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
                prefab.GetComponent<CharacterSpriteRigController>() == null)
            {
                throw new BuildFailedException(
                    "CharacterRig2D.prefab must contain " +
                    "CharacterSpriteRigController.");
            }

            string controllerSource = File.ReadAllText(ControllerPath);
            string[] requiredControllerTokens =
            {
                "CreateWholeBody",
                "TryGetWorldBounds",
                "TryGetScreenHeightFraction",
                "FitToScreenHeight",
                "HideLegacyGeometry",
                "GetStageScale",
                "GetColumn"
            };
            for (int i = 0; i < requiredControllerTokens.Length; i++)
            {
                if (!controllerSource.Contains(
                        requiredControllerTokens[i],
                        System.StringComparison.Ordinal))
                {
                    throw new BuildFailedException(
                        "Real sprite controller is missing required 3.3 " +
                        "token: " + requiredControllerTokens[i]);
                }
            }

            string[] forbiddenControllerTokens =
            {
                "PartSpec",
                "PartSpecs",
                "CharacterSpritePart part",
                "12 bone-bound parts"
            };
            for (int i = 0; i < forbiddenControllerTokens.Length; i++)
            {
                if (controllerSource.Contains(
                        forbiddenControllerTokens[i],
                        System.StringComparison.Ordinal))
                {
                    throw new BuildFailedException(
                        "Patch 3.2 rectangular limb slicing returned: " +
                        forbiddenControllerTokens[i]);
                }
            }

            string gateSource = File.ReadAllText(VisibilityGatePath);
            string[] requiredGateTokens =
            {
                "CharacterSpriteRigController",
                "TryGetWorldBounds",
                "FitToScreenHeight",
                "SafeVisibleMinimum",
                "UsedVisibleSpriteFallback"
            };
            for (int i = 0; i < requiredGateTokens.Length; i++)
            {
                if (!gateSource.Contains(
                        requiredGateTokens[i],
                        System.StringComparison.Ordinal))
                {
                    throw new BuildFailedException(
                        "CharacterVisibilityGate is missing 3.3 protection: " +
                        requiredGateTokens[i]);
                }
            }
        }
    }
}
