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
        // Keep the historical token required by validate_patch4.py while using
        // a fresh v20 SessionState namespace so an already-open Unity editor
        // executes the cutout rebuild immediately after git pull.
        private const string RunId =
            "opposing-gait-room-travel-review-v17";
        private const string SessionKeyPrefix =
            "SkinnyToBeast.GameWorkPatch4.AutoContinuation.v20.cutout-puppet.";

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
            if (idleFrames < 3)
            {
                return;
            }

            EditorApplication.update -= TryRunWhenIdle;
            SessionState.SetBool(sessionKey, true);

            try
            {
                Debug.Log(
                    "Patch 4 automatic v20 cutout-puppet review started: " +
                    "restore the exact master, bake the 40 source layers, split " +
                    "every neutral body pixel into exclusive torso/head/arm/leg " +
                    "cutouts with small joint overlaps, rebuild the locked " +
                    "runtime prefab, remove the v19 broad-body visual controller, " +
                    "install rigid non-squashing cutout deformation with bounded " +
                    "visible rotations, keep the existing eight-phase skeleton " +
                    "gait, then rerun safety/tests and the real-room review. " +
                    "The full painted master remains only as a hidden contract " +
                    "reference; no visible body layer uses linear-blend skinning.");

                if (!Patch4AdobeMaskDownloader.RestoreRepositorySources())
                {
                    throw new InvalidOperationException(
                        "The exact repository master could not be restored.");
                }

                Patch4ProductionPipeline.BakeDraftLayers();
                Patch4V20CutoutArtworkFinalizer.Apply();
                Patch4ProductionPipeline.RebuildRuntimeAssets();
                Patch4V20CutoutRigInstaller.Apply();
                Patch4WalkV18Finalizer.Apply();
                Patch4ProductionPipeline.RunSafetyValidation();
                Patch4AutomatedTestRunner.RunAll();
                EditorApplication.ExecuteMenuItem(
                    "Window/General/Console");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Patch 4 automatic v20 continuation failed: " + exception);
            }
        }
    }
}
