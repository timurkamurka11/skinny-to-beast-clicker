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
        // Keep the historical validation token but use a fresh v21 namespace so
        // an already-open Editor runs the architecture migration after git pull.
        private const string RunId =
            "opposing-gait-room-travel-review-v17";
        private const string SessionKeyPrefix =
            "SkinnyToBeast.GameWorkPatch4.AutoContinuation.v21.hybrid-rig.";

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
                    "Patch 4 automatic v21 hybrid-rig rebuild started: restore " +
                    "the approved master, bake non-exclusive source layers, merge " +
                    "each complete arm and leg into one continuous painted surface, " +
                    "verify neutral reconstruction before motion, keep enlarged " +
                    "shoulder/hip artwork hidden behind a stable torso, rebuild the " +
                    "runtime prefab, remove the v20 rigid paper-doll controller, " +
                    "install localized whole-limb joint deformation plus planted-" +
                    "foot gait correction, remove whole-body/core-bone scale " +
                    "animation, retain the tested opposing arm peaks, run v21 " +
                    "structure gates, then execute safety/tests and the real-room " +
                    "ten-clip review. v20 exclusive pixel ownership is not run.");

                if (!Patch4AdobeMaskDownloader.RestoreRepositorySources())
                {
                    throw new InvalidOperationException(
                        "The exact repository master could not be restored.");
                }

                Patch4ProductionPipeline.BakeDraftLayers();
                Patch4V21HybridArtworkBuilder.Build();
                Patch4V21NeutralReconstructionGate.ValidateOrThrow();
                Patch4ProductionPipeline.RebuildRuntimeAssets();
                Patch4V21AnimationFinalizer.Apply();
                // Preserve the previously verified opposing hand peaks for the
                // legacy structural PlayMode contract. During real playback the
                // v21 planted-foot solver replaces the old direct leg solution.
                Patch4WalkV18Finalizer.Apply();
                Patch4V21HybridRigInstaller.Apply();
                Patch4V21HybridValidator.ValidateOrThrow();
                Patch4ProductionPipeline.RunSafetyValidation();
                Patch4AutomatedTestRunner.RunAll();
                EditorApplication.ExecuteMenuItem(
                    "Window/General/Console");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Patch 4 automatic v21 continuation failed: " + exception);
            }
        }
    }
}
