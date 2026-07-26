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
        private const string PartPath =
            "Assets/Scripts/Gameplay/CharacterSpritePart.cs";
        private const string PrefabPath =
            "Assets/Resources/UI/Gameplay/Living/" +
            "CharacterRig2D.prefab";

        public int callbackOrder => -950;

        public void OnPreprocessBuild(BuildReport report)
        {
            ValidateOrThrow();
        }

        [MenuItem(
            "Tools/Skinny to Beast/Validate Real Fat Man Sprite 3.2")]
        private static void ValidateFromMenu()
        {
            ValidateOrThrow();
            Debug.Log(
                "Real Fat Man Sprite Patch 3.2 guard passed: PNG, " +
                "catalog, three directions, four stages and prefab " +
                "controller are present.");
        }

        private static void ValidateOrThrow()
        {
            string[] requiredFiles =
            {
                TexturePath,
                CatalogPath,
                ControllerPath,
                PartPath,
                PrefabPath
            };
            for (int i = 0; i < requiredFiles.Length; i++)
            {
                if (!File.Exists(requiredFiles[i]))
                {
                    throw new BuildFailedException(
                        $"Real Fat Man Sprite 3.2 file is missing: " +
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
                prefab.GetComponent<
                    CharacterSpriteRigController>() == null)
            {
                throw new BuildFailedException(
                    "CharacterRig2D.prefab must contain " +
                    "CharacterSpriteRigController.");
            }

            string controllerSource =
                File.ReadAllText(ControllerPath);
            string[] requiredTokens =
            {
                "CharacterSpritePart",
                "FindOpaqueBounds",
                "Bone.Belly",
                "Bone.ChestSoft",
                "Bone.Head",
                "HideLegacyGeometry",
                "GetStageScale",
                "GetColumn"
            };
            for (int i = 0; i < requiredTokens.Length; i++)
            {
                if (!controllerSource.Contains(
                        requiredTokens[i],
                        System.StringComparison.Ordinal))
                {
                    throw new BuildFailedException(
                        "Real sprite controller is missing required token: " +
                        requiredTokens[i]);
                }
            }
        }
    }
}
