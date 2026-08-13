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
            "continuous-layered-motion-v33";
        private const string SessionKeyPrefix =
            "SkinnyToBeast.GameWorkPatch4.AutoContinuation.v33.gameplay.";

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
                    "Patch 4 V33 continuous layered motion pass started: the " +
                    "corrupt V32 whole-body atlas and every live frame swap are " +
                    "removed. One persistent torso, head, face and four continuous " +
                    "limbs now follow clamped-auto Animator curves every render " +
                    "frame. Walk uses a restrained 1.6-second heavy gait, 0.18-second " +
                    "locomotion blends, loop-only seam normalization, direction-" +
                    "aware planted-foot IK and visible depth travel across a " +
                    "narrow central route. Gameplay signals still select " +
                    "idle, shift, blink, look, taps, walk, turn, sit and upgrade. " +
                    "Unity will rebuild the local hybrid art and locked prefab, " +
                    "then validate 16 continuous walk-time samples by live hand " +
                    "and foot trajectories; body twitch and discrete slides cannot " +
                    "pass. Patch 3.5 remains the active rollback and readiness stays " +
                    "locked.");
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
                    "Patch 4 automatic V33 continuation failed: " + exception);
            }
        }
    }
}
