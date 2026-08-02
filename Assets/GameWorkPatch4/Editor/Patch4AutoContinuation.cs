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
            "articulated-gait-silhouette-review-v14";
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
                    "Patch 4 automatic fresh motion-review run started: " +
                    "restoring the repository master, rebaking all 40 layers as " +
                    "one exact intact visible body plus feathered expression " +
                    "replacements, rebuilding the locked runtime prefab with a " +
                    "96 x 144 silhouette-constrained anatomical deformation " +
                    "grid with torso-excluding arm weights and separated leg " +
                    "ownership, explicit hip lift, knee bend, foot plant and " +
                    "counter-swinging shoulder, elbow and hand phases, " +
                    "uncropped Canvas UVs and " +
                    "frozen bind anchors, then " +
                    "running the corrected ten-clip library. " +
                    "After 4/4, Unity will review all ten clips in the real room, " +
                    "block chopped reference layers, whole-body sway disguised " +
                    "as motion, weak silhouette change in any individual arm " +
                    "or leg, weak hand-to-shoulder or foot-to-pelvis motion, " +
                    "weak focused blink motion, collapsed or over-stretched " +
                    "silhouettes or Console errors, " +
                    "wait until Test Runner has fully returned to Edit " +
                    "Mode, suppress the legacy robot footstep only during the " +
                    "fresh review, restore Patch 3.5 and open only current " +
                    "read-only review artifacts.");
                if (!Patch4AdobeMaskDownloader.RestoreRepositorySources())
                {
                    throw new InvalidOperationException(
                        "The exact repository master could not be restored.");
                }

                Patch4ProductionPipeline.BakeDraftLayers();
                Patch4ProductionPipeline.RebuildRuntimeAssets();
                Patch4ProductionPipeline.RunSafetyValidation();
                Patch4AutomatedTestRunner.RunAll();
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
