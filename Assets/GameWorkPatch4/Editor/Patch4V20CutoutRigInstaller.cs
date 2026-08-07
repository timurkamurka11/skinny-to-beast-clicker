using System;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    public static class Patch4V20CutoutRigInstaller
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
                        "Patch 4 v20 could not load the runtime prefab.");
                }

                Patch4StableBodySkinController oldStable =
                    root.GetComponent<Patch4StableBodySkinController>();
                if (oldStable != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldStable, true);
                }

                Patch4CutoutPuppetController controller =
                    root.GetComponent<Patch4CutoutPuppetController>();
                if (controller == null)
                {
                    controller = root.AddComponent<Patch4CutoutPuppetController>();
                }

                EditorUtility.SetDirty(controller);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    "Patch 4 v20 cutout rig installed: v19 broad-body visual " +
                    "controller removed; rigid anatomical puppet controller added.");
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
