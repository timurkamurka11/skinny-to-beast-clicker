using System;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    public static class Patch4V21HybridRigInstaller
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
                        "Patch 4 v21 could not load the runtime prefab.");
                }

                RemoveIfPresent<Patch4StableBodySkinController>(root);
                RemoveIfPresent<Patch4CutoutPuppetController>(root);

                Patch4V21HybridPuppetController controller =
                    root.GetComponent<Patch4V21HybridPuppetController>();
                if (controller == null)
                {
                    controller = root.AddComponent<Patch4V21HybridPuppetController>();
                }

                Sprite torso = LoadSprite(Patch4V21HybridArtworkBuilder.TorsoPath);
                Sprite armL = LoadSprite(Patch4V21HybridArtworkBuilder.ArmLPath);
                Sprite armR = LoadSprite(Patch4V21HybridArtworkBuilder.ArmRPath);
                Sprite legL = LoadSprite(Patch4V21HybridArtworkBuilder.LegLPath);
                Sprite legR = LoadSprite(Patch4V21HybridArtworkBuilder.LegRPath);

                SerializedObject serialized = new(controller);
                serialized.FindProperty("rigController").objectReferenceValue =
                    root.GetComponent<Patch4CharacterRigController>();
                serialized.FindProperty("torsoSprite").objectReferenceValue = torso;
                serialized.FindProperty("armLSprite").objectReferenceValue = armL;
                serialized.FindProperty("armRSprite").objectReferenceValue = armR;
                serialized.FindProperty("legLSprite").objectReferenceValue = legL;
                serialized.FindProperty("legRSprite").objectReferenceValue = legR;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(controller);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "Patch 4 v21 hybrid rig installed: v20 rigid puppet removed; " +
                    "continuous whole-arm/whole-leg artwork bound to localized " +
                    "joint deformation with a stable torso presentation.");
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    "Patch 4 v21 hybrid sprite was not imported: " + path);
            }
            return sprite;
        }

        private static void RemoveIfPresent<T>(GameObject root)
            where T : Component
        {
            T component = root.GetComponent<T>();
            if (component != null)
            {
                UnityEngine.Object.DestroyImmediate(component, true);
            }
        }
    }
}
