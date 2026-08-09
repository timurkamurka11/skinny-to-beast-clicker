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
            "locked-interactive-gameplay-preview-v27";
        private const string SessionKeyPrefix =
            "SkinnyToBeast.GameWorkPatch4.AutoContinuation.v27.gameplay.";

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
                    "Patch 4 V27 locked gameplay preview pass started: " +
                    "restore the approved master, rebuild the retained v21.1 " +
                    "rollback candidates, verify neutral reconstruction, rebuild " +
                    "the locked prefab and bind six complete-frame RGBA sheets " +
                    "covering all ten required clips, including the corrected " +
                    "complete-body upgrade sheet. During every clip the entire " +
                    "experimental Canvas stack is hidden, so " +
                    "no sliced joint, detached face or vacuum-stretched body can " +
                    "leak behind the complete painted frames. The Walk uses eight " +
                    "strict screen-right profile phases, and blink/look/tap/upgrade " +
                    "use independent painted facial expressions. Every frame is " +
                    "aligned to one shoe line and each pose family uses a fixed " +
                    "body scale. Animator state speeds now match the visible " +
                    "whole-frame cadence. After 4/4, Unity will first route " +
                    "idle, shift, blink, look, both taps, walking, turning, " +
                    "sitting and upgrade through the same gameplay API used at " +
                    "runtime, and play two uninterrupted passes in the actual " +
                    "room. Before Test Runner enters PlayMode, Patch 4 now " +
                    "validates the generated Patch 3 Animator, cancels any " +
                    "stale non-test Play resume and clears stale room-review " +
                    "ownership so no Editor callback can stop the player. " +
                    "It will then enter every full Animator state, " +
                    "sample all ten states and eight Walk frames with silent " +
                    "monotonic travel, " +
                    "measure both visible arm silhouettes, both visible leg " +
                    "silhouettes, facial differences and the weakest adjacent-" +
                    "frame difference, " +
                    "reject duplicated poses, missing travel, weak blink motion, " +
                    "collapse, over-stretch or Console errors, restore Patch 3.5 " +
                    "and verify fresh read-only review artifacts. After that " +
                    "technical pass, Unity will automatically enter a separate " +
                    "Editor-only normal-game preview and leave Play Mode on. " +
                    "The user can tap the dumbbell, buy upgrades and watch the " +
                    "existing bounded room routine drive Patch 4 without " +
                    "unlocking production readiness.");
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
                    "Patch 4 automatic V27 continuation failed: " + exception);
            }
        }
    }
}
