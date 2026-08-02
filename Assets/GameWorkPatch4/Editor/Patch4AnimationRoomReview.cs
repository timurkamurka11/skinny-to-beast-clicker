using System;
using System.IO;
using SkinnyToBeast.Gameplay;
using SkinnyToBeast.UI;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Opens the real generated gameplay room after the automatic tests, runs
    /// the ten clips through an Editor-only locked driver and returns to Edit
    /// Mode before opening the read-only contact sheet.
    /// </summary>
    [InitializeOnLoad]
    public static class Patch4AnimationRoomReview
    {
        [Serializable]
        private sealed class ReviewArtifactStatus
        {
            public string runToken = string.Empty;
            public bool completed;
        }

        private const string InProgressKey =
            "SkinnyToBeast.GameWorkPatch4.AnimationReview.InProgress";
        private const string StageKey =
            "SkinnyToBeast.GameWorkPatch4.AnimationReview.Stage";
        private const string ResultKey =
            "SkinnyToBeast.GameWorkPatch4.AnimationReview.Result";
        private const string MessageKey =
            "SkinnyToBeast.GameWorkPatch4.AnimationReview.Message";
        private const string RunTokenKey =
            "SkinnyToBeast.GameWorkPatch4.AnimationReview.RunToken";

        private const string WaitingForEditModeStage =
            "waiting-for-test-play-mode-exit";
        private const string WaitingForEditorQuiescenceStage =
            "waiting-for-editor-quiescence";
        private const string EnteringStage = "entering-play-mode";
        private const string RunningStage = "running-room-review";
        private const string ExitingStage = "exiting-play-mode";

        private static double startDeadline;
        private static double quiescenceStartedAt;
        private static int quiescentUpdateCount;
        private static bool gameplayWindowRequested;
        private static bool driverStarted;

        public static string ReportPath => Path.Combine(
            Patch4CompilationMonitor.ReportDirectory,
            Patch4AnimationRoomReviewDriver.ReportFileName);

        public static string ContactSheetPath => Path.Combine(
            Patch4CompilationMonitor.ReportDirectory,
            Patch4AnimationRoomReviewDriver.ContactSheetFileName);

        public static string WalkCyclePath => Path.Combine(
            Patch4CompilationMonitor.ReportDirectory,
            Patch4AnimationRoomReviewDriver.WalkCycleFileName);

        public static string CurrentRunToken =>
            SessionState.GetString(RunTokenKey, string.Empty);

        static Patch4AnimationRoomReview()
        {
            EditorApplication.playModeStateChanged -=
                OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged +=
                OnPlayModeStateChanged;

            if (!SessionState.GetBool(InProgressKey, false))
            {
                return;
            }

            string stage =
                SessionState.GetString(StageKey, string.Empty);
            if (string.Equals(
                    stage,
                    WaitingForEditModeStage,
                    StringComparison.Ordinal) &&
                !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                BeginEditorQuiescence();
            }
            else if (string.Equals(
                         stage,
                         WaitingForEditorQuiescenceStage,
                         StringComparison.Ordinal) &&
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
            else if (string.Equals(
                         stage,
                         ExitingStage,
                         StringComparison.Ordinal) &&
                     !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += CompleteAfterExit;
            }
        }

        public static bool StartAfterTests()
        {
            if (Application.isBatchMode ||
                SessionState.GetBool(InProgressKey, false))
            {
                return false;
            }

            SessionState.SetBool(InProgressKey, true);
            SessionState.SetBool(ResultKey, false);
            SessionState.SetString(MessageKey, string.Empty);
            SessionState.SetString(
                RunTokenKey,
                Guid.NewGuid().ToString("N"));
            ClearPreviousReviewArtifacts();

            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isPlaying)
            {
                SessionState.SetString(
                    StageKey,
                    WaitingForEditModeStage);
                Debug.Log(
                    "Patch 4 room review is queued until the Test Runner " +
                    "has fully returned to Edit Mode.");
            }
            else
            {
                BeginEditorQuiescence();
            }

            return true;
        }

        private static void BeginEditorQuiescence()
        {
            if (!SessionState.GetBool(InProgressKey, false))
            {
                return;
            }

            SessionState.SetString(
                StageKey,
                WaitingForEditorQuiescenceStage);
            quiescenceStartedAt = EditorApplication.timeSinceStartup;
            quiescentUpdateCount = 0;
            EditorApplication.update -= WaitForEditorQuiescence;
            EditorApplication.update += WaitForEditorQuiescence;
        }

        private static void WaitForEditorQuiescence()
        {
            if (!SessionState.GetBool(InProgressKey, false) ||
                !string.Equals(
                    SessionState.GetString(StageKey, string.Empty),
                    WaitingForEditorQuiescenceStage,
                    StringComparison.Ordinal))
            {
                EditorApplication.update -= WaitForEditorQuiescence;
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isPlaying)
            {
                quiescenceStartedAt =
                    EditorApplication.timeSinceStartup;
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
            QueueEnterPlayMode();
        }

        private static void QueueEnterPlayMode()
        {
            SessionState.SetString(StageKey, EnteringStage);
            EditorApplication.delayCall -= EnterPlayModeWhenReady;
            EditorApplication.delayCall += EnterPlayModeWhenReady;
        }

        private static void EnterPlayModeWhenReady()
        {
            EditorApplication.delayCall -= EnterPlayModeWhenReady;
            if (!SessionState.GetBool(InProgressKey, false) ||
                !string.Equals(
                    SessionState.GetString(StageKey, string.Empty),
                    EnteringStage,
                    StringComparison.Ordinal))
            {
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += EnterPlayModeWhenReady;
                return;
            }

            Debug.Log(
                "Patch 4 locked animation-room review started. Unity will " +
                "open the real LivingGameplayScene, review all ten clips with " +
                "the intact continuous Canvas body, restore Patch 3.5 and " +
                "return to Edit Mode.");
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (!SessionState.GetBool(InProgressKey, false))
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                string enteredStage =
                    SessionState.GetString(StageKey, string.Empty);
                if (!string.Equals(
                        enteredStage,
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

            string stage =
                SessionState.GetString(StageKey, string.Empty);
            if (string.Equals(
                    stage,
                    WaitingForEditModeStage,
                    StringComparison.Ordinal) ||
                string.Equals(
                    stage,
                    WaitingForEditorQuiescenceStage,
                    StringComparison.Ordinal) ||
                string.Equals(
                    stage,
                    EnteringStage,
                    StringComparison.Ordinal))
            {
                BeginEditorQuiescence();
                return;
            }

            if (!string.Equals(
                stage,
                ExitingStage,
                StringComparison.Ordinal))
            {
                SessionState.SetBool(ResultKey, false);
                SessionState.SetString(
                    MessageKey,
                    "Play Mode ended before the locked room review completed.");
                SessionState.SetString(StageKey, ExitingStage);
            }

            EditorApplication.delayCall += CompleteAfterExit;
        }

        private static void ScheduleRoomBinding()
        {
            gameplayWindowRequested = false;
            driverStarted = false;
            startDeadline = EditorApplication.timeSinceStartup + 20d;
            EditorApplication.update -= TryBindRealRoom;
            EditorApplication.update += TryBindRealRoom;
        }

        private static void TryBindRealRoom()
        {
            if (!SessionState.GetBool(InProgressKey, false) ||
                !EditorApplication.isPlaying)
            {
                EditorApplication.update -= TryBindRealRoom;
                return;
            }

            if (driverStarted)
            {
                EditorApplication.update -= TryBindRealRoom;
                return;
            }

            if (EditorApplication.timeSinceStartup > startDeadline)
            {
                FailAndExit(
                    "The real LivingGameplayScene did not become ready in time.");
                return;
            }

            if (!gameplayWindowRequested)
            {
                gameplayWindowRequested = true;
                if (!GameplayWindowController.Show())
                {
                    FailAndExit(
                        "GameplayWindowController could not create the real room.");
                    return;
                }
            }

            Patch4RuntimeInstaller.InstallAvailableGameplayRigs();
            if (!GameplayWindowController.IsCharacterReady)
            {
                return;
            }

            Patch4CharacterRigController patchRig =
                FindInstalledPatchRig();
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

            Patch4CharacterVisibilityGuard visibility =
                patchRig.GetComponent<Patch4CharacterVisibilityGuard>();
            Patch4CanvasPresentation presentation =
                patchRig.GetComponent<Patch4CanvasPresentation>();
            Patch4FaceController face =
                patchRig.GetComponent<Patch4FaceController>();
            Patch4SecondaryMotionController motion =
                patchRig.GetComponent<Patch4SecondaryMotionController>();
            Animator animator = patchRig.GetComponent<Animator>();

            if (legacyRig == null ||
                patchVisual == null ||
                rollbackVisual == null ||
                visibility == null ||
                presentation == null ||
                face == null ||
                motion == null ||
                animator == null)
            {
                FailAndExit(
                    "The real-room Patch 4 review binding is incomplete.");
                return;
            }

            driverStarted = true;
            EditorApplication.update -= TryBindRealRoom;
            EditorApplication.ExecuteMenuItem("Window/General/Game");

            GameObject host = new("Patch4LockedAnimationRoomReview");
            host.hideFlags = HideFlags.HideInHierarchy;
            UnityEngine.Object.DontDestroyOnLoad(host);
            Patch4AnimationRoomReviewDriver driver =
                host.AddComponent<Patch4AnimationRoomReviewDriver>();
            driver.ReviewFinished += OnDriverFinished;
            driver.Begin(
                patchRig,
                visibility,
                presentation,
                face,
                motion,
                animator,
                patchVisual,
                rollbackVisual,
                Patch4CompilationMonitor.ReportDirectory,
                CurrentRunToken);
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

        private static void OnDriverFinished(
            bool passed,
            string message)
        {
            SessionState.SetBool(ResultKey, passed);
            SessionState.SetString(
                MessageKey,
                message ?? string.Empty);
            SessionState.SetString(StageKey, ExitingStage);

            if (passed)
            {
                Debug.Log(
                    "Patch 4 locked animation-room technical review PASSED: " +
                    "all ten clips were sampled in LivingGameplayScene with " +
                    "one intact body, frozen Canvas bind anchors, retained " +
                    "full silhouettes, " +
                    "measurable motion, an eight-phase opposing-limb walk " +
                    "advancing through the room, no legacy robot " +
                    "footsteps and zero review errors. " +
                    "Human motion review is still required and activation " +
                    "remains locked.");
            }
            else
            {
                Debug.LogError(
                    "Patch 4 locked animation-room review failed: " +
                    message);
            }

            EditorApplication.delayCall += ExitPlayMode;
        }

        private static void FailAndExit(string message)
        {
            EditorApplication.update -= TryBindRealRoom;
            SessionState.SetBool(ResultKey, false);
            SessionState.SetString(MessageKey, message);
            SessionState.SetString(StageKey, ExitingStage);
            Debug.LogError("Patch 4 animation-room review: " + message);
            EditorApplication.delayCall += ExitPlayMode;
        }

        private static void ExitPlayMode()
        {
            EditorApplication.delayCall -= ExitPlayMode;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
            }
            else
            {
                EditorApplication.delayCall += CompleteAfterExit;
            }
        }

        private static void CompleteAfterExit()
        {
            EditorApplication.delayCall -= CompleteAfterExit;
            EditorApplication.update -= TryBindRealRoom;
            EditorApplication.update -= WaitForEditorQuiescence;
            if (!SessionState.GetBool(InProgressKey, false) ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            bool passed = SessionState.GetBool(ResultKey, false);
            string message =
                SessionState.GetString(MessageKey, string.Empty);
            SessionState.SetBool(InProgressKey, false);
            SessionState.SetString(StageKey, string.Empty);

            EditorApplication.ExecuteMenuItem("Window/General/Console");
            Patch4NeutralPoseReviewWindow.Open();
            Patch4FacePoseReviewWindow.Open();
            bool hasFreshRoomArtifacts = HasFreshRoomArtifacts();
            if (hasFreshRoomArtifacts)
            {
                Patch4AnimationRoomReviewWindow.Open();
            }

            if (!passed && !string.IsNullOrWhiteSpace(message))
            {
                Debug.LogError(
                    "Patch 4 animation-room review did not complete: " +
                    message);
            }

            if (!hasFreshRoomArtifacts)
            {
                Debug.LogError(
                    "Patch 4 did not produce a fresh animation-room report " +
                    "and contact sheet. No previous contact sheet was opened.");
            }
        }

        private static void ClearPreviousReviewArtifacts()
        {
            DeleteReviewArtifact(ReportPath);
            DeleteReviewArtifact(ContactSheetPath);
            DeleteReviewArtifact(WalkCyclePath);
        }

        private static bool HasFreshRoomArtifacts()
        {
            if (!File.Exists(ReportPath) ||
                !File.Exists(ContactSheetPath) ||
                !File.Exists(WalkCyclePath) ||
                string.IsNullOrWhiteSpace(CurrentRunToken))
            {
                return false;
            }

            try
            {
                ReviewArtifactStatus status =
                    JsonUtility.FromJson<ReviewArtifactStatus>(
                        File.ReadAllText(ReportPath));
                return status != null &&
                    status.completed &&
                    string.Equals(
                        status.runToken,
                        CurrentRunToken,
                        StringComparison.Ordinal);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Patch 4 could not verify review artifact freshness: " +
                    exception.Message);
                return false;
            }
        }

        private static void DeleteReviewArtifact(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            try
            {
                File.Delete(path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Patch 4 could not clear stale review artifact " +
                    path + ": " + exception.Message);
            }
        }
    }
}
