using System.IO;
using SkinnyToBeast.Gameplay;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Editor
{
    internal static class FatManSpriteRigBaker
    {
        private const string ManifestPath =
            "Assets/Resources/Characters/FatManLayered/Generated/manifest.json";
        private const string FrontChestPath =
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Front/Chest.png";
        private const string SideChestPath =
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Side/Chest.png";
        private const string BackChestPath =
            "Assets/Resources/Characters/FatManLayered/Generated/Common/Back/Chest.png";
        private const string PrefabPath =
            "Assets/Resources/UI/Gameplay/Living/CharacterRig2D.prefab";

        [MenuItem(
            "Tools/Skinny to Beast/Bake Real Fat Man Layered Art 3.6")]
        private static void Bake()
        {
            string[] required =
            {
                ManifestPath,
                FrontChestPath,
                SideChestPath,
                BackChestPath
            };
            for (int i = 0; i < required.Length; i++)
            {
                if (!File.Exists(required[i]))
                {
                    Debug.LogError(
                        "Patch 3.6 layered asset is missing: " + required[i] +
                        ". Pull the generated-art commit or run " +
                        "Tools/FatManLayeredArt/generate_layered_art.py.");
                    return;
                }
                AssetDatabase.ImportAsset(
                    required[i],
                    ImportAssetOptions.ForceUpdate);
            }

            GameObject root =
                PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError(
                    "Character prefab is missing: " + PrefabPath);
                return;
            }

            try
            {
                if (root.GetComponent<CharacterLayeredRigController>() == null)
                {
                    root.AddComponent<CharacterLayeredRigController>();
                }

                CharacterSpriteRigController flat =
                    root.GetComponent<CharacterSpriteRigController>();
                if (flat != null)
                {
                    // The full-body source remains available for old saves but
                    // may not render in Patch 3.6.
                    flat.enabled = false;
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
                "Real Fat Man Layered Art 3.6 baked: separate transparent " +
                "body parts and face states are attached to art-specific " +
                "proxy bones; the flat full-body renderer is disabled.");
        }
    }
}
