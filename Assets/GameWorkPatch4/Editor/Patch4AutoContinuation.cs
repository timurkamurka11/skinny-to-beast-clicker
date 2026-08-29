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
        private const string RunId =
            "single-owner-sync-neutral-bind-v38";
        private const string SessionKeyPrefix =
            "SkinnyToBeast.GameWorkPatch4.AutoContinuation.v38.gameplay.";

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
                    "Patch 4 V38 single-owner rig repair started. Stage 4 " +
                    "selection now latches one actionable readiness failure " +
                    "instead of reapplying the same skin every refresh. The " +
                    "Animator exclusively owns all six Walk leg rotations; " +
                    "secondary motion no longer competes for SpineUpper. The " +
                    "actual-room review pauses legacy signals, resets the " +
                    "Animator to neutral, rebinds the hybrid mesh from that " +
                    "pose, and moves the authoritative gameplay root along a " +
                    "shallow perspective-controlled route with exact restore. " +
                    "Unity will rebuild all local layers and runtime assets, " +
                    "run strict safety plus EditMode/PlayMode tests, then " +
                    "capture a fresh actual-room review. No validator is " +
                    "weakened; readiness remains locked and Patch 3.5 remains " +
                    "the rollback owner.");
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
                Patch4WalkV18Finalizer.Apply();
                Patch4V21PoseResetFinalizer.Apply();
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
                    "Patch 4 automatic V38 continuation failed: " + exception);
            }
        }
    }
}
