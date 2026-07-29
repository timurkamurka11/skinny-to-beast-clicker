using System;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Runs the current Patch 4 continuation automatically after a Git pull
    /// triggers Unity script recompilation. Each run id executes only once per
    /// Editor session, so generated reports cannot cause an import loop.
    /// </summary>
    [InitializeOnLoad]
    public static class Patch4AutoContinuation
    {
        private const string RunId = "draft-bake-scaffolds-v1";
        private const string SessionKeyPrefix =
            "SkinnyToBeast.GameWorkPatch4.AutoContinuation.";

        private static int idleFrames;

        static Patch4AutoContinuation()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            EditorApplication.update += TryRunWhenIdle;
        }

        private static void TryRunWhenIdle()
        {
            string sessionKey = SessionKeyPrefix + RunId;
            if (SessionState.GetBool(sessionKey, false))
            {
                EditorApplication.update -= TryRunWhenIdle;
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                idleFrames = 0;
                return;
            }

            idleFrames++;
            if (idleFrames < 2)
            {
                return;
            }

            EditorApplication.update -= TryRunWhenIdle;
            SessionState.SetBool(sessionKey, true);

            try
            {
                Debug.Log(
                    "Patch 4 automatic continuation started: restoring sources, " +
                    "baking layers and validating them. No Dashboard click is required.");
                Patch4ProductionPipeline.DownloadSources();
                Patch4ProductionPipeline.BakeDraftLayers();

                TextAsset report = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    Patch4DraftLayerValidator.ReportPath);
                bool draftPassed =
                    report != null &&
                    report.text.Contains("\"passedTechnicalChecks\": true");

                if (draftPassed)
                {
                    Debug.Log(
                        "Patch 4 draft validation passed. Continuing with locked " +
                        "runtime rebuild, safety validation and smoke reports.");
                    Patch4ProductionPipeline.RebuildRuntimeAssets();
                    Patch4ProductionPipeline.RunSafetyValidation();
                    Patch4ProductionPipeline.RunEditorSmokeReport();
                }
                else
                {
                    Debug.LogWarning(
                        "Patch 4 automatic continuation stopped after draft " +
                        "validation. Production activation remains locked.");
                }

                Debug.Log(
                    "Patch 4 automatic continuation finished. Review the " +
                    "validator messages in Console.");
                EditorApplication.ExecuteMenuItem(
                    "Window/General/Console");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Patch 4 automatic continuation failed: " + exception);
            }
        }
    }
}
