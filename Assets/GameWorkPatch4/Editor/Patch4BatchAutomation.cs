using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Batch-mode entry point used by RUN_PATCH4_VERIFY.ps1.
    /// It never approves production art and never enables Patch 4.
    /// </summary>
    public static class Patch4BatchAutomation
    {
        [Serializable]
        private sealed class BatchSummary
        {
            public string generatedUtc = string.Empty;
            public string unityVersion = string.Empty;
            public bool draftTechnicalChecksPassed;
            public bool editorSmokePassed;
            public bool productionArtStillLocked;
            public int exitCode;
            public string message = string.Empty;
        }

        public static void PrepareAndValidate()
        {
            int exitCode = 1;
            BatchSummary summary = new()
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                productionArtStillLocked = true
            };

            try
            {
                Debug.Log("Patch 4 batch: baking draft layer pack.");
                Patch4MaskDrivenLayerBaker.BakeDraftLayerPack();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Debug.Log("Patch 4 batch: validating draft layer pack.");
                Patch4DraftLayerValidator.ValidateAndWriteReport();
                summary.draftTechnicalChecksPassed = ReadDraftPassState();

                Debug.Log("Patch 4 batch: rebuilding locked runtime assets.");
                Patch4ProductionPipeline.RebuildRuntimeAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Debug.Log("Patch 4 batch: running protected-path and rig validation.");
                Patch4ProductionPipeline.RunSafetyValidation();

                Debug.Log("Patch 4 batch: running Editor smoke validation.");
                summary.editorSmokePassed =
                    Patch4EditorSmokeValidator.ValidateAndWriteReport();

                Patch4ArtReadinessAsset readiness =
                    Patch4ArtReadinessAssetBuilder.EnsureAsset();
                summary.productionArtStillLocked =
                    readiness == null || !readiness.ProductionArtApproved;

                if (!summary.productionArtStillLocked)
                {
                    throw new InvalidOperationException(
                        "Batch automation found production art already approved. " +
                        "Automated preparation must remain locked until human review.");
                }

                // A draft pixel report may legitimately fail until hidden joints and
                // face poses are painted. The prefab smoke contract must still pass.
                exitCode = summary.editorSmokePassed ? 0 : 2;
                summary.message = summary.editorSmokePassed
                    ? summary.draftTechnicalChecksPassed
                        ? "Batch preparation and technical draft validation passed. Art approval remains locked."
                        : "Batch preparation passed. Draft pixel/joint validation still requires manual art work. Art approval remains locked."
                    : "Batch preparation completed, but Editor smoke validation failed. Inspect the generated reports.";
            }
            catch (Exception exception)
            {
                exitCode = 1;
                summary.message = exception.GetType().Name + ": " + exception.Message;
                Debug.LogException(exception);
            }
            finally
            {
                summary.exitCode = exitCode;
                WriteSummary(summary);
                AssetDatabase.SaveAssets();
                Debug.Log("Patch 4 batch finished with exit code " + exitCode + ".");
                EditorApplication.Exit(exitCode);
            }
        }

        private static bool ReadDraftPassState()
        {
            string absolute = ToAbsolutePath(Patch4DraftLayerValidator.ReportPath);
            if (!File.Exists(absolute))
            {
                return false;
            }

            string json = File.ReadAllText(absolute);
            return json.IndexOf(
                       "\"passedTechnicalChecks\": true",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void WriteSummary(BatchSummary summary)
        {
            Directory.CreateDirectory(Patch4CompilationMonitor.ReportDirectory);
            string path = Path.Combine(
                Patch4CompilationMonitor.ReportDirectory,
                "patch4-batch-summary.json");
            File.WriteAllText(path, JsonUtility.ToJson(summary, true));
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new DirectoryNotFoundException(
                    "Could not resolve the Unity project root.");
            }

            return Path.Combine(
                projectRoot,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
