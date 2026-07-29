using SkinnyToBeast.Editor.Patch4;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Ordered production commands for Patch 4. No command in this class approves
    /// art or enables the new character automatically.
    /// </summary>
    public static class Patch4ProductionPipeline
    {
        [MenuItem("Tools/GameWork/Patch 4.0/Pipeline/1. Download Adobe Sources")]
        public static void DownloadSources()
        {
            Patch4AdobeMaskDownloader.DownloadAdobeSources();
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Pipeline/2. Bake Draft Layers")]
        public static void BakeDraftLayers()
        {
            Patch4MaskDrivenLayerBaker.BakeDraftLayerPack();
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Pipeline/3. Validate Draft Layers")]
        public static void ValidateDraftLayers()
        {
            Patch4DraftLayerValidator.ValidateAndWriteReport();
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Pipeline/4. Rebuild Runtime Assets")]
        public static void RebuildRuntimeAssets()
        {
            Patch4LayerCatalogBuilder.RebuildCatalog();
            Patch4AnimationLibraryBuilder.RebuildLibrary();
            Patch4AnimatorControllerSanitizer.RepairController();
            Patch4PrefabBuilder.RebuildPrefab();
            Patch4PrefabReadinessBinder.BindReadinessGate();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                Patch4PrefabBuilder.PrefabPath);
            Selection.activeObject = prefab;

            Debug.Log(
                "Patch 4 runtime assets rebuilt in locked rollback mode. " +
                "The production-art gate remains false until manual review.",
                prefab);
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Pipeline/5. Run Safety Validation")]
        public static void RunSafetyValidation()
        {
            Patch4DraftLayerValidator.ValidateAndWriteReport();
            Patch4NeutralPoseValidator.ValidateAndWriteReport();
            Patch4RigContractValidator.VerifyProtectedPaths();
            Patch4CompilationMonitor.WriteCompilationSnapshot();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                Patch4PrefabBuilder.PrefabPath);
            if (prefab == null)
            {
                Debug.LogError(
                    "Patch 4 safety validation requires the generated prefab.");
                Patch4EditorSmokeValidator.ValidateAndWriteReport();
                return;
            }

            Selection.activeObject = prefab;
            Patch4RigContractValidator.ValidateSelectedRig();
            Patch4EditorSmokeValidator.ValidateAndWriteReport();

            Patch4CharacterRigController rig =
                prefab.GetComponent<Patch4CharacterRigController>();
            if (rig == null)
            {
                Debug.LogError("Patch 4 prefab has no rig controller.", prefab);
            }
            else if (rig.IsArtApproved)
            {
                Debug.LogWarning(
                    "Patch 4 art readiness is APPROVED. Confirm that the " +
                    "pixel report, hidden overlaps, facial poses, EditMode tests " +
                    "and Play Mode tests were reviewed intentionally before " +
                    "enabling it.",
                    prefab);
            }
            else
            {
                Debug.Log(
                    "Patch 4 readiness gate is correctly locked. Patch 3.5 " +
                    "remains the active character.",
                    prefab);
            }
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Pipeline/6. Run Editor Smoke Report")]
        public static void RunEditorSmokeReport()
        {
            Patch4NeutralPoseValidator.ValidateAndWriteReport();
            Patch4CompilationMonitor.WriteCompilationSnapshot();
            Patch4EditorSmokeValidator.ValidateAndWriteReport();
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Pipeline/7. Open Unity Test Runner")]
        public static void OpenUnityTestRunner()
        {
            bool opened = EditorApplication.ExecuteMenuItem(
                "Window/General/Test Runner");
            if (!opened)
            {
                Debug.LogWarning(
                    "Unity Test Runner window could not be opened automatically. " +
                    "Open Window → General → Test Runner manually.");
            }
        }
    }
}
