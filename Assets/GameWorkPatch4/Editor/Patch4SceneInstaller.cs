using SkinnyToBeast.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Installs the isolated Patch 4 prefab beside the existing character and
    /// binds only public gameplay signals. The legacy visual stays active and
    /// Patch 4 stays disabled until art validation succeeds.
    /// </summary>
    public static class Patch4SceneInstaller
    {
        private const string InstanceName = "FatMan_Patch4_Instance";

        [MenuItem("Tools/GameWork/Patch 4.0/Build/Install Beside Selected Character")]
        public static void InstallBesideSelectedCharacter()
        {
            GameObject selected = Selection.activeGameObject;
            CharacterRigController legacyRig = selected != null
                ? selected.GetComponentInParent<CharacterRigController>()
                : null;
            if (legacyRig == null)
            {
                Debug.LogError(
                    "Select the existing gameplay character containing " +
                    "CharacterRigController before installing Patch 4.");
                return;
            }

            CharacterSkinController legacySkin =
                legacyRig.GetComponent<CharacterSkinController>();
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    Patch4PrefabBuilder.PrefabPath);
            if (prefab == null)
            {
                Patch4PrefabBuilder.RebuildPrefab();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    Patch4PrefabBuilder.PrefabPath);
            }

            if (prefab == null)
            {
                Debug.LogError("Patch 4 prefab could not be created.");
                return;
            }

            Transform existing = legacyRig.transform.Find(InstanceName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                prefab,
                legacyRig.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(instance, "Install Patch 4 character");
            instance.name = InstanceName;
            instance.transform.SetParent(legacyRig.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            Patch4CharacterRigController patchRig =
                instance.GetComponent<Patch4CharacterRigController>();
            Patch4CharacterVisibilityGuard visibility =
                instance.GetComponent<Patch4CharacterVisibilityGuard>();
            Patch4LegacySignalBridge bridge =
                instance.GetComponent<Patch4LegacySignalBridge>();

            GameObject rollbackRoot = legacyRig.VisualRoot != null
                ? legacyRig.VisualRoot.gameObject
                : legacyRig.gameObject;

            BindRollback(patchRig, rollbackRoot);
            BindVisibility(visibility, rollbackRoot);
            BindLegacySignals(bridge, legacyRig, legacySkin);
            patchRig?.SetPatch4Enabled(false);

            EditorSceneManager.MarkSceneDirty(instance.scene);
            Selection.activeGameObject = instance;
            Debug.Log(
                "Patch 4 installed in rollback mode. The existing character " +
                "remains visible until the new layer catalog is complete.",
                instance);
        }

        [MenuItem(
            "Tools/GameWork/Patch 4.0/Build/Install Beside Selected Character",
            true)]
        private static bool ValidateInstall()
        {
            GameObject selected = Selection.activeGameObject;
            return selected != null &&
                   selected.GetComponentInParent<CharacterRigController>() != null;
        }

        private static void BindRollback(
            Patch4CharacterRigController target,
            GameObject rollbackRoot)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serialized = new(target);
            serialized.FindProperty("patch35RollbackRoot").objectReferenceValue =
                rollbackRoot;
            serialized.FindProperty("patch4Enabled").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindVisibility(
            Patch4CharacterVisibilityGuard target,
            GameObject rollbackRoot)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serialized = new(target);
            serialized.FindProperty("patch35RollbackRoot").objectReferenceValue =
                rollbackRoot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindLegacySignals(
            Patch4LegacySignalBridge target,
            CharacterRigController legacyRig,
            CharacterSkinController legacySkin)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serialized = new(target);
            serialized.FindProperty("legacyRig").objectReferenceValue = legacyRig;
            serialized.FindProperty("legacySkin").objectReferenceValue = legacySkin;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
