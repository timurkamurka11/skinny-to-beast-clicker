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
            "renderer-owned-preview-suppression-v37";
        private const string SessionKeyPrefix =
            "SkinnyToBeast.GameWorkPatch4.AutoContinuation.v37.gameplay.";

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
                    "Patch 4 V37 renderer-owned preview suppression pass " +
                    "started. V36 deactivated Patch 3.5 VisualRoot and made " +
                    "GameplayVisualStageController reject the selected Stage 4 " +
                    "skin on every refresh. The preview now leaves that complete " +
                    "hierarchy active and asks its renderer owner to suppress only " +
                    "the bounded body and face pixels, so stage validation, room " +
                    "movement and gameplay signals stay alive without drawing a " +
                    "second character. HeadBase " +
                    "owns the exact neutral face; blink, gaze, open mouth and " +
                    "smile are feathered mutually exclusive replacements. Both " +
                    "legs use the same mirrored, phase-shifted IK cycle with a " +
                    "soft state-entry blend. An Editor-only empty camera removes " +
                    "the InitTestScene diagnostic without touching production " +
                    "scenes. Unity will rebuild all local layers and runtime " +
                    "assets, run strict safety/tests, then capture a fresh actual-" +
                    "room review. Readiness remains locked and Patch 3.5 remains " +
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
                    "Patch 4 automatic V37 continuation failed: " + exception);
            }
        }
    }
}
