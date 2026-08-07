using System;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// One-shot continuation for the v19 visible-rig rebuild. It intentionally
    /// lives beside the older continuation so pulling this commit into an open
    /// Unity session always starts a fresh pass even when v18b already ran.
    /// </summary>
    [InitializeOnLoad]
    public static class Patch4V19AutoContinuation
    {
        private const string SessionKey =
            "SkinnyToBeast.GameWorkPatch4.AutoContinuation.v19.stable-body";

        private static int idleFrames;

        static Patch4V19AutoContinuation()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            EditorApplication.update += TryRunWhenIdle;
        }

        private static void TryRunWhenIdle()
        {
            if (SessionState.GetBool(SessionKey, false))
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
            SessionState.SetBool(SessionKey, true);

            try
            {
                Debug.Log(
                    "Patch 4 automatic v19 stable-rig review started: " +
                    "rebuild art/runtime, replace the visible broad linear " +
                    "blend body with a volume-stable rigid-torso articulated " +
                    "head/arm/leg grid, keep contract bindings intact, finalize " +
                    "the eight-phase gait, then rerun validation and the full " +
                    "room review. This pass specifically targets the visible " +
                    "squashing/collapse reported in v18b.");

                if (!Patch4AdobeMaskDownloader.RestoreRepositorySources())
                {
                    throw new InvalidOperationException(
                        "The exact repository master could not be restored.");
                }

                Patch4ProductionPipeline.BakeDraftLayers();
                Patch4ProductionPipeline.RebuildRuntimeAssets();
                Patch4V19StableSkinInstaller.Apply();
                Patch4WalkV18Finalizer.Apply();
                Patch4ProductionPipeline.RunSafetyValidation();
                Patch4AutomatedTestRunner.RunAll();
                EditorApplication.ExecuteMenuItem(
                    "Window/General/Console");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Patch 4 automatic v19 continuation failed: " + exception);
            }
        }
    }
}
