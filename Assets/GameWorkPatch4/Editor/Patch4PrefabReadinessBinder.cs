using System;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Ensures every generated Patch 4 prefab references the locked readiness
    /// asset. Saves only when values actually differ, preventing import loops.
    /// </summary>
    public sealed class Patch4PrefabReadinessBinder : AssetPostprocessor
    {
        private static bool binding;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            for (int i = 0; i < importedAssets.Length; i++)
            {
                if (string.Equals(
                        importedAssets[i],
                        Patch4PrefabBuilder.PrefabPath,
                        StringComparison.Ordinal))
                {
                    EditorApplication.delayCall += BindReadinessGate;
                    return;
                }
            }
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Build/Bind Readiness Gate")]
        public static void BindReadinessGate()
        {
            if (binding)
            {
                return;
            }

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(Patch4PrefabBuilder.PrefabPath);
            if (prefab == null)
            {
                return;
            }

            binding = true;
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(Patch4PrefabBuilder.PrefabPath);
                Patch4CharacterRigController rig =
                    contents.GetComponent<Patch4CharacterRigController>();
                if (rig == null)
                {
                    Debug.LogError(
                        "Patch 4 prefab has no Patch4CharacterRigController.",
                        prefab);
                    return;
                }

                Patch4ArtReadinessAsset readiness =
                    Patch4ArtReadinessAssetBuilder.EnsureAsset();
                SerializedObject serialized = new(rig);
                SerializedProperty readinessProperty =
                    serialized.FindProperty("artReadiness");
                SerializedProperty enabledProperty =
                    serialized.FindProperty("patch4Enabled");
                if (readinessProperty == null || enabledProperty == null)
                {
                    Debug.LogError(
                        "Patch 4 rig controller readiness fields are unavailable.",
                        prefab);
                    return;
                }

                bool changed =
                    readinessProperty.objectReferenceValue != readiness ||
                    enabledProperty.boolValue;
                if (!changed)
                {
                    return;
                }

                readinessProperty.objectReferenceValue = readiness;
                enabledProperty.boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, Patch4PrefabBuilder.PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (contents != null)
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }

                binding = false;
            }
        }
    }
}
