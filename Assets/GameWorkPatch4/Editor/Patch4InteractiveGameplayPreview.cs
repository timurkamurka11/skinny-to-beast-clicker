using System;
using SkinnyToBeast.Editor;
using SkinnyToBeast.Gameplay;
using SkinnyToBeast.UI;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Starts a separate, user-controlled Play Mode session after a fresh
    /// technical room pass. The real gameplay room remains interactive until
    /// the user exits Play Mode; Patch 4 stays readiness-locked throughout.
    /// </summary>
    [InitializeOnLoad]
    public static class Patch4InteractiveGameplayPreview
    {
        private const string InProgressKey =
            "SkinnyToBeast.GameWorkPatch4.InteractivePreview.InProgress";
        private const string StageKey =
            "SkinnyToBeast.GameWorkPatch4.InteractivePreview.Stage";
        private const string BoundKey =
            "SkinnyToBeast.GameWorkPatch4.InteractivePreview.Bound";
        private const string LegacyAnimatorResumePlayKey =
            "SkinnyToBeast.LivingAnimatorBuilt.Patch3.ResumePlayV4";

        private const string WaitingStage = "waiting-for-editor-quiescence";
        private const string EnteringStage = "entering-play-mode";
        private const string RunningStage = "running-interactive-preview";

        private static double quiescenceStartedAt;
        private static double bindingDeadline;
        private static int quiescentUpdateCount;
        private static bool gameplayWindowRequested;

        public static bool IsInProgress =>
            SessionState.GetBool(InProgressKey, false);

        static Patch4InteractiveGameplayPreview()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (!IsInProgress)
            {
                return;
            }

            string stage = SessionState.GetString(StageKey, string.Empty);
            if (string.Equals(stage, WaitingStage, StringComparison.Ordinal) &&
                !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                BeginEditorQuiescence();
            }
            else if (string.Equals(
                         stage,
                         EnteringStage,
                         StringComparison.Ordinal) &&
                     !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += EnterPlayModeWhenReady;
            }
            else if (string.Equals(
                         stage,
                         RunningStage,
                         StringComparison.Ordinal) &&
                     EditorApplication.isPlaying)
            {
                ScheduleRoomBinding();
            }
        }

        public static bool StartAfterFreshReview()
        {
            if (Application.isBatchMode ||
                Patch4AutomatedTestRunner.IsRunInProgress ||
                IsInProgress ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return false;
            }

            SessionState.SetBool(InProgressKey, true);
            SessionState.SetBool(BoundKey, false);
            SessionState.SetString(StageKey, WaitingStage);
            BeginEditorQuiescence();
            return true;
        }

        public static void PrepareForAutomatedTests()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Patch 4 tests require stable Edit Mode before clearing " +
                    "interactive-preview ownership.");
            }

            ClearOwnership();
        }

        private static void BeginEditorQuiescence()
        {
            if (!IsInProgress)
            {
                return;
            }

            SessionState.SetString(StageKey, WaitingStage);
            quiescenceStartedAt = EditorApplication.timeSinceStartup;
            quiescentUpdateCount = 0;
            EditorApplication.update -= WaitForEditorQuiescence;
            EditorApplication.update += WaitForEditorQuiescence;
        }

        private static void WaitForEditorQuiescence()
        {
            if (!IsInProgress ||
                !string.Equals(
                    SessionState.GetString(StageKey, string.Empty),
                    WaitingStage,
                    StringComparison.Ordinal))
            {
                EditorApplication.update -= WaitForEditorQuiescence;
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                Patch4AutomatedTestRunner.IsRunInProgress ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                quiescenceStartedAt = EditorApplication.timeSinceStartup;
                quiescentUpdateCount = 0;
                return;
            }

            quiescentUpdateCount++;
            double stableSeconds =
                EditorApplication.timeSinceStartup - quiescenceStartedAt;
            if (quiescentUpdateCount < 30 || stableSeconds < 1.25d)
            {
                return;
            }

            EditorApplication.update -= WaitForEditorQuiescence;
            SessionState.SetString(StageKey, EnteringStage);
            EditorApplication.delayCall += EnterPlayModeWhenReady;
        }

        private static void EnterPlayModeWhenReady()
        {
            EditorApplication.delayCall -= EnterPlayModeWhenReady;
            if (!IsInProgress ||
                !string.Equals(
                    SessionState.GetString(StageKey, string.Empty),
                    EnteringStage,
                    StringComparison.Ordinal))
            {
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                Patch4AutomatedTestRunner.IsRunInProgress ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += EnterPlayModeWhenReady;
                return;
            }

            try
            {
                SessionState.SetBool(LegacyAnimatorResumePlayKey, false);
                LivingGameplayAnimatorAssetBuilder.EnsureCurrentAssets();
                SessionState.SetBool(LegacyAnimatorResumePlayKey, false);
            }
            catch (Exception exception)
            {
                FailBeforePlay(
                    "Legacy gameplay Animator preflight failed: " +
                    exception.Message);
                return;
            }

            Debug.Log(
                "Patch 4 LOCKED GAMEPLAY PREVIEW is entering the real room. " +
                "Unity will leave Play Mode running so taps, purchases and " +
                "the existing room routine can be inspected interactively. " +
                "This Editor-only preview does not approve or activate " +
                "Patch 4 production art.");
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!IsInProgress)
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (!string.Equals(
                        SessionState.GetString(StageKey, string.Empty),
                        EnteringStage,
                        StringComparison.Ordinal))
                {
                    return;
                }

                SessionState.SetString(StageKey, RunningStage);
                ScheduleRoomBinding();
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            bool wasBound = SessionState.GetBool(BoundKey, false);
            ClearOwnership();
            if (wasBound)
            {
                Debug.Log(
                    "Patch 4 locked gameplay preview ended. Patch 3.5 " +
                    "remains the active production character.");
            }
            else
            {
                Debug.LogError(
                    "Patch 4 locked gameplay preview ended before the real " +
                    "gameplay character was bound.");
            }

            EditorApplication.delayCall += OpenDeferredReviewArtifacts;
        }

        private static void ScheduleRoomBinding()
        {
            gameplayWindowRequested = false;
            bindingDeadline = EditorApplication.timeSinceStartup + 20d;
            EditorApplication.update -= TryBindRealRoom;
            EditorApplication.update += TryBindRealRoom;
        }

        private static void TryBindRealRoom()
        {
            if (!IsInProgress || !EditorApplication.isPlaying)
            {
                EditorApplication.update -= TryBindRealRoom;
                return;
            }

            Patch4InteractiveGameplayPreviewDriver existing =
                UnityEngine.Object.FindFirstObjectByType<
                    Patch4InteractiveGameplayPreviewDriver>();
            if (existing != null && existing.IsActive)
            {
                SessionState.SetBool(BoundKey, true);
                EditorApplication.update -= TryBindRealRoom;
                EditorApplication.ExecuteMenuItem("Window/General/Game");
                return;
            }

            if (EditorApplication.timeSinceStartup > bindingDeadline)
            {
                FailAndExitPlayMode(
                    "The real LivingGameplayScene did not become ready in time.");
                return;
            }

            if (!gameplayWindowRequested)
            {
                gameplayWindowRequested = true;
                if (!GameplayWindowController.Show())
                {
                    FailAndExitPlayMode(
                        "GameplayWindowController could not create the real room.");
                    return;
                }
            }

            Patch4RuntimeInstaller.InstallAvailableGameplayRigs();
            if (!GameplayWindowController.IsCharacterReady)
            {
                return;
            }

            Patch4CharacterRigController patchRig = FindInstalledPatchRig();
            if (patchRig == null)
            {
                return;
            }

            Transform legacyRoot = patchRig.transform.parent;
            CharacterRigController legacyRig =
                legacyRoot != null
                    ? legacyRoot.GetComponent<CharacterRigController>()
                    : null;
            GameObject patchVisual = patchRig.transform
                .Find("Patch4VisualRoot")?.gameObject;
            GameObject rollbackVisual =
                legacyRig != null && legacyRig.VisualRoot != null
                    ? legacyRig.VisualRoot.gameObject
                    : null;

            Patch4CharacterStateMachine stateMachine =
                patchRig.GetComponent<Patch4CharacterStateMachine>();
            Patch4CharacterVisibilityGuard visibility =
                patchRig.GetComponent<Patch4CharacterVisibilityGuard>();
            Patch4V23FullFramePresentation presentation =
                patchRig.GetComponent<Patch4V23FullFramePresentation>();
            Animator animator = patchRig.GetComponent<Animator>();
            Patch4LegacySignalBridge signalBridge =
                patchRig.GetComponent<Patch4LegacySignalBridge>();

            if (legacyRig == null ||
                patchVisual == null ||
                rollbackVisual == null ||
                stateMachine == null ||
                visibility == null ||
                presentation == null ||
                !presentation.IsReady ||
                animator == null ||
                signalBridge == null ||
                !signalBridge.enabled)
            {
                FailAndExitPlayMode(
                    "The locked interactive Patch 4 room binding is incomplete.");
                return;
            }

            GameObject host = new("Patch4LockedGameplayPreview");
            host.hideFlags = HideFlags.HideInHierarchy;
            UnityEngine.Object.DontDestroyOnLoad(host);
            Patch4InteractiveGameplayPreviewDriver driver =
                host.AddComponent<Patch4InteractiveGameplayPreviewDriver>();
            if (!driver.Begin(
                    patchRig,
                    stateMachine,
                    visibility,
                    presentation,
                    animator,
                    patchVisual,
                    rollbackVisual))
            {
                UnityEngine.Object.Destroy(host);
                FailAndExitPlayMode(
                    "The locked interactive Patch 4 visual override failed.");
                return;
            }

            SessionState.SetBool(BoundKey, true);
            EditorApplication.update -= TryBindRealRoom;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            Debug.Log(
                "Patch 4 LOCKED GAMEPLAY PREVIEW READY. The normal gameplay " +
                "room is live: use the dumbbell and upgrade controls while " +
                "the existing character routine drives walking and standing " +
                "actions. Play Mode will remain on until you stop it. " +
                "Readiness is still locked and Patch 3.5 will be restored on exit.");
        }

        private static Patch4CharacterRigController FindInstalledPatchRig()
        {
            Patch4CharacterRigController[] rigs =
                UnityEngine.Object.FindObjectsByType<
                    Patch4CharacterRigController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int i = 0; i < rigs.Length; i++)
            {
                Patch4CharacterRigController rig = rigs[i];
                if (rig != null &&
                    string.Equals(
                        rig.gameObject.name,
                        Patch4RuntimeInstaller.InstanceName,
                        StringComparison.Ordinal))
                {
                    return rig;
                }
            }

            return null;
        }

        private static void FailBeforePlay(string message)
        {
            Debug.LogError("Patch 4 locked gameplay preview: " + message);
            ClearOwnership();
            EditorApplication.delayCall += OpenDeferredReviewArtifacts;
        }

        private static void FailAndExitPlayMode(string message)
        {
            EditorApplication.update -= TryBindRealRoom;
            Debug.LogError("Patch 4 locked gameplay preview: " + message);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static void OpenDeferredReviewArtifacts()
        {
            EditorApplication.delayCall -= OpenDeferredReviewArtifacts;
            EditorApplication.ExecuteMenuItem("Window/General/Console");
            Patch4NeutralPoseReviewWindow.Open();
            Patch4FacePoseReviewWindow.Open();
            Patch4AnimationRoomReviewWindow.Open();
        }

        private static void ClearOwnership()
        {
            EditorApplication.delayCall -= EnterPlayModeWhenReady;
            EditorApplication.delayCall -= OpenDeferredReviewArtifacts;
            EditorApplication.update -= WaitForEditorQuiescence;
            EditorApplication.update -= TryBindRealRoom;

            SessionState.SetBool(InProgressKey, false);
            SessionState.SetBool(BoundKey, false);
            SessionState.SetString(StageKey, string.Empty);
            gameplayWindowRequested = false;
            quiescentUpdateCount = 0;
            quiescenceStartedAt = 0d;
            bindingDeadline = 0d;
        }
    }
}
