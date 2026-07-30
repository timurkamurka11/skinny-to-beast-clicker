using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Runs the isolated Patch 4 EditMode and PlayMode test assemblies in order.
    /// SessionState keeps the workflow alive across the domain reloads caused by
    /// PlayMode, while durable JSON and NUnit XML reports stay outside Assets.
    /// </summary>
    [InitializeOnLoad]
    public static class Patch4AutomatedTestRunner
    {
        [Serializable]
        private sealed class ModeReport
        {
            public bool completed;
            public bool passed;
            public string resultState = string.Empty;
            public int passCount;
            public int failCount;
            public int skipCount;
            public int inconclusiveCount;
            public string message = string.Empty;
            public string stackTrace = string.Empty;
            public string xmlPath = string.Empty;
        }

        [Serializable]
        private sealed class TestReport
        {
            public string generatedUtc = string.Empty;
            public bool completed;
            public bool passed;
            public ModeReport editMode = new();
            public ModeReport playMode = new();
        }

        private sealed class TestRunCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                HandleRunFinished(result);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }

        private const string EditModeAssembly =
            "SkinnyToBeast.GameWorkPatch4.EditModeTests";
        private const string PlayModeAssembly =
            "SkinnyToBeast.GameWorkPatch4.PlayModeTests";

        private const string InProgressKey =
            "SkinnyToBeast.GameWorkPatch4.AutomatedTests.InProgress";
        private const string StageKey =
            "SkinnyToBeast.GameWorkPatch4.AutomatedTests.Stage";
        private const string JobKey =
            "SkinnyToBeast.GameWorkPatch4.AutomatedTests.Job";

        private const string EditModeStage = "edit-mode";
        private const string PlayModePendingStage = "play-mode-pending";
        private const string PlayModeStage = "play-mode";

        private const string EditModeXmlRelativePath =
            "Library/GameWorkPatch4Reports/patch4-editmode-results.xml";
        private const string PlayModeXmlRelativePath =
            "Library/GameWorkPatch4Reports/patch4-playmode-results.xml";

        private static TestRunCallbacks callbacks;
        private static TestRunnerApi activeRunner;

        public static string ReportPath => Path.Combine(
            Patch4CompilationMonitor.ReportDirectory,
            "patch4-test-report.json");

        static Patch4AutomatedTestRunner()
        {
            if (Application.isBatchMode ||
                !SessionState.GetBool(InProgressKey, false))
            {
                return;
            }

            EnsureCallbacksRegistered();
            if (string.Equals(
                SessionState.GetString(StageKey, string.Empty),
                PlayModePendingStage,
                StringComparison.Ordinal))
            {
                EditorApplication.delayCall += StartPendingPlayModeRun;
            }
        }

        public static void RunAll()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            if (SessionState.GetBool(InProgressKey, false))
            {
                Debug.LogWarning(
                    "Patch 4 automated tests are already running.");
                return;
            }

            Directory.CreateDirectory(
                Patch4CompilationMonitor.ReportDirectory);
            WriteReport(new TestReport());

            SessionState.SetBool(InProgressKey, true);
            SessionState.SetString(StageKey, EditModeStage);
            SessionState.SetString(JobKey, string.Empty);
            EnsureCallbacksRegistered();

            Debug.Log(
                "Patch 4 automated verification started: EditMode tests.");

            try
            {
                StartRun(TestMode.EditMode, EditModeAssembly);
            }
            catch (Exception exception)
            {
                RecordStartFailure(EditModeStage, exception);
                QueuePlayModeRun();
            }
        }

        private static void StartRun(TestMode mode, string assemblyName)
        {
            CleanupActiveRunner();

            Filter filter = new()
            {
                testMode = mode,
                assemblyNames = new[] { assemblyName }
            };

            activeRunner = ScriptableObject.CreateInstance<TestRunnerApi>();
            activeRunner.hideFlags = HideFlags.HideAndDontSave;
            string jobId = activeRunner.Execute(
                new ExecutionSettings(filter));
            SessionState.SetString(JobKey, jobId ?? string.Empty);
        }

        private static void HandleRunFinished(ITestResultAdaptor result)
        {
            if (!SessionState.GetBool(InProgressKey, false))
            {
                return;
            }

            string stage = SessionState.GetString(StageKey, string.Empty);
            if (string.Equals(stage, EditModeStage, StringComparison.Ordinal))
            {
                SaveModeResult(
                    result,
                    isEditMode: true,
                    EditModeXmlRelativePath);
                QueuePlayModeRun();
                return;
            }

            if (!string.Equals(stage, PlayModeStage, StringComparison.Ordinal))
            {
                return;
            }

            SaveModeResult(
                result,
                isEditMode: false,
                PlayModeXmlRelativePath);
            CompleteWorkflow();
        }

        private static void QueuePlayModeRun()
        {
            if (!SessionState.GetBool(InProgressKey, false))
            {
                return;
            }

            SessionState.SetString(StageKey, PlayModePendingStage);
            SessionState.SetString(JobKey, string.Empty);
            CleanupActiveRunner();
            EditorApplication.delayCall += StartPendingPlayModeRun;
        }

        private static void StartPendingPlayModeRun()
        {
            EditorApplication.delayCall -= StartPendingPlayModeRun;

            if (!SessionState.GetBool(InProgressKey, false) ||
                !string.Equals(
                    SessionState.GetString(StageKey, string.Empty),
                    PlayModePendingStage,
                    StringComparison.Ordinal))
            {
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += StartPendingPlayModeRun;
                return;
            }

            SessionState.SetString(StageKey, PlayModeStage);
            Debug.Log(
                "Patch 4 EditMode tests finished. Starting PlayMode tests " +
                "automatically.");

            try
            {
                StartRun(TestMode.PlayMode, PlayModeAssembly);
            }
            catch (Exception exception)
            {
                RecordStartFailure(PlayModeStage, exception);
                CompleteWorkflow();
            }
        }

        private static void SaveModeResult(
            ITestResultAdaptor result,
            bool isEditMode,
            string xmlRelativePath)
        {
            ModeReport modeReport = BuildModeReport(result, xmlRelativePath);

            try
            {
                TestRunnerApi.SaveResultToFile(result, xmlRelativePath);
            }
            catch (Exception exception)
            {
                modeReport.message = AppendMessage(
                    modeReport.message,
                    "Could not save NUnit XML: " + exception.Message);
            }

            TestReport report = ReadReport();
            if (isEditMode)
            {
                report.editMode = modeReport;
            }
            else
            {
                report.playMode = modeReport;
            }

            WriteReport(report);
        }

        private static ModeReport BuildModeReport(
            ITestResultAdaptor result,
            string xmlRelativePath)
        {
            if (result == null)
            {
                return new ModeReport
                {
                    completed = true,
                    passed = false,
                    resultState = "Failed:Error",
                    failCount = 1,
                    message = "Unity Test Framework returned no result.",
                    xmlPath = GetAbsoluteReportPath(xmlRelativePath)
                };
            }

            string resultState = result.ResultState ?? string.Empty;
            return new ModeReport
            {
                completed = true,
                passed = result.FailCount == 0 &&
                    result.InconclusiveCount == 0 &&
                    result.PassCount > 0 &&
                    resultState.StartsWith(
                        "Passed",
                        StringComparison.OrdinalIgnoreCase),
                resultState = resultState,
                passCount = result.PassCount,
                failCount = result.FailCount,
                skipCount = result.SkipCount,
                inconclusiveCount = result.InconclusiveCount,
                message = result.Message ?? string.Empty,
                stackTrace = result.StackTrace ?? string.Empty,
                xmlPath = GetAbsoluteReportPath(xmlRelativePath)
            };
        }

        private static void RecordStartFailure(
            string stage,
            Exception exception)
        {
            ModeReport failedMode = new()
            {
                completed = true,
                passed = false,
                resultState = "Failed:Error",
                failCount = 1,
                message = exception.Message,
                stackTrace = exception.ToString(),
                xmlPath = GetAbsoluteReportPath(
                    string.Equals(
                        stage,
                        EditModeStage,
                        StringComparison.Ordinal)
                        ? EditModeXmlRelativePath
                        : PlayModeXmlRelativePath)
            };

            TestReport report = ReadReport();
            if (string.Equals(stage, EditModeStage, StringComparison.Ordinal))
            {
                report.editMode = failedMode;
            }
            else
            {
                report.playMode = failedMode;
            }

            WriteReport(report);
            Debug.LogError(
                "Patch 4 automated test run could not start for " + stage +
                ": " + exception);
        }

        private static void CompleteWorkflow()
        {
            TestReport report = ReadReport();
            report.completed = report.editMode.completed &&
                report.playMode.completed;
            report.passed = report.completed &&
                report.editMode.passed &&
                report.playMode.passed;
            WriteReport(report);

            SessionState.SetBool(InProgressKey, false);
            SessionState.SetString(StageKey, string.Empty);
            SessionState.SetString(JobKey, string.Empty);
            CleanupActiveRunner();
            RemoveCallbacks();

            if (report.passed)
            {
                Debug.Log(
                    "Patch 4 automated verification PASSED. EditMode: " +
                    report.editMode.passCount + " passed; PlayMode: " +
                    report.playMode.passCount + " passed. Report: " +
                    ReportPath);
            }
            else
            {
                Debug.LogError(
                    "Patch 4 automated verification FAILED. EditMode state: " +
                    report.editMode.resultState + "; PlayMode state: " +
                    report.playMode.resultState + ". Report: " + ReportPath);
            }

            EditorApplication.delayCall += OpenResults;
        }

        private static TestReport ReadReport()
        {
            try
            {
                if (File.Exists(ReportPath))
                {
                    TestReport report = JsonUtility.FromJson<TestReport>(
                        File.ReadAllText(ReportPath));
                    if (report != null)
                    {
                        report.editMode ??= new ModeReport();
                        report.playMode ??= new ModeReport();
                        return report;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Patch 4 test report could not be read: " +
                    exception.Message);
            }

            return new TestReport();
        }

        private static void WriteReport(TestReport report)
        {
            report.generatedUtc = DateTime.UtcNow.ToString("O");
            Directory.CreateDirectory(
                Patch4CompilationMonitor.ReportDirectory);
            File.WriteAllText(
                ReportPath,
                JsonUtility.ToJson(report, true));
        }

        private static string GetAbsoluteReportPath(string relativePath)
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                Application.dataPath;
            return Path.GetFullPath(
                Path.Combine(projectRoot, relativePath))
                .Replace('\\', '/');
        }

        private static string AppendMessage(
            string current,
            string addition)
        {
            return string.IsNullOrWhiteSpace(current)
                ? addition
                : current + Environment.NewLine + addition;
        }

        private static void EnsureCallbacksRegistered()
        {
            if (callbacks != null)
            {
                return;
            }

            callbacks = new TestRunCallbacks();
            TestRunnerApi.RegisterTestCallback(callbacks, 100);
        }

        private static void RemoveCallbacks()
        {
            if (callbacks == null)
            {
                return;
            }

            TestRunnerApi.UnregisterTestCallback(callbacks);
            callbacks = null;
        }

        private static void CleanupActiveRunner()
        {
            if (activeRunner == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(activeRunner);
            activeRunner = null;
        }

        private static void OpenResults()
        {
            EditorApplication.delayCall -= OpenResults;
            EditorApplication.ExecuteMenuItem("Window/General/Console");
            Patch4NeutralPoseReviewWindow.Open();
            Patch4FacePoseReviewWindow.Open();
        }
    }
}
