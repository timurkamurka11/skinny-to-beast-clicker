using System;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    public static class Patch4V19StableSkinInstaller
    {
        public static void Apply()
        {
            const string prefabPath =
                "Assets/GameWorkPatch4/Resources/FatMan_Patch4.prefab";

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (root == null)
                {
                    throw new InvalidOperationException(
                        "Patch 4 v19 could not load the runtime prefab.");
                }

                Patch4StableBodySkinController controller =
                    root.GetComponent<Patch4StableBodySkinController>();
                if (controller == null)
                {
                    controller = root.AddComponent<
                        Patch4StableBodySkinController>();
                }

                EditorUtility.SetDirty(controller);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    "Patch 4 v19 stable-body skin installed: the broad " +
                    "continuous LBS pass is retained only for contract data; " +
                    "the visible master now uses rigid torso + articulated " +
                    "head/arm/leg regions with narrow seam blends.");
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }
    }
}
