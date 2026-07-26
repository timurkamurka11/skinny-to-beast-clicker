using SkinnyToBeast.Gameplay;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Editor
{
    internal static class FatManSpriteRigBaker
    {
        private const string TexturePath =
            "Assets/Resources/Characters/FatMan/" +
            "fat-man-turnaround-reference.png";
        private const string CatalogPath =
            "Assets/Resources/Characters/FatMan/" +
            "FatManSpriteCatalog.asset";
        private const string PrefabPath =
            "Assets/Resources/UI/Gameplay/Living/" +
            "CharacterRig2D.prefab";

        [MenuItem(
            "Tools/Skinny to Beast/Bake Real Fat Man Sprite Rig 3.3")]
        private static void Bake()
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(TexturePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError(
                    $"Real fat-man PNG is missing: {TexturePath}");
                return;
            }

            bool importerChanged = false;
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importerChanged = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                importerChanged = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                importerChanged = true;
            }
            if (importer.textureCompression !=
                TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importerChanged = true;
            }
            if (importerChanged)
            {
                importer.SaveAndReimport();
            }

            FatManSpriteSet catalog =
                AssetDatabase.LoadAssetAtPath<FatManSpriteSet>(CatalogPath);
            if (catalog == null || !catalog.IsValid)
            {
                Debug.LogError(
                    $"FatManSpriteCatalog is missing or invalid: " +
                    CatalogPath);
                return;
            }

            GameObject root =
                PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError(
                    $"Character prefab is missing: {PrefabPath}");
                return;
            }

            try
            {
                if (root.GetComponent<CharacterSpriteRigController>() == null)
                {
                    root.AddComponent<CharacterSpriteRigController>();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Real Fat Man Sprite Rig 3.3 baked: the prefab renders one " +
                "intact painted body for front, side and back, while the old " +
                "skeleton remains an invisible animation driver.");
        }
    }
}
