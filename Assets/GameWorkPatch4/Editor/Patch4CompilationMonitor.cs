using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Records every Unity script compilation into Library/GameWorkPatch4Reports.
    /// Writing outside Assets prevents the report itself from causing an import
    /// and compilation loop.
    /// </summary>
    [InitializeOnLoad]
    public static class Patch4CompilationMonitor
    {
        [Serializable]
        private sealed class MessageRecord
        {
            public string assembly = string.Empty;
            public string type = string.Empty;
            public string message = string.Empty;
            public string file = string.Empty;
            public int line;
            public int column;
            public bool belongsToPatch4;
        }

        [Serializable]
        private sealed class CompilationReport
        {
            public string generatedUtc = string.Empty;
            public bool completed;
            public bool succeeded;
            public int errorCount;
            public int warningCount;
            public int patch4ErrorCount;
            public int patch4WarningCount;
            public List<MessageRecord> messages = new();
        }

        private static readonly List<MessageRecord> Messages = new();
        private static bool compilationInProgress;

        public static string ReportDirectory
        {
            get
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                return Path.Combine(
                    projectRoot ?? Application.dataPath,
                    "Library",
                    "GameWorkPatch4Reports");
            }
        }

        public static string ReportPath =>
            Path.Combine(ReportDirectory, "patch4-compilation-report.json");

        static Patch4CompilationMonitor()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished +=
                OnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        [MenuItem(
            "Tools/GameWork/Patch 4.0/Validation/Open Compilation Report")]
        public static void OpenCompilationReport()
        {
            Directory.CreateDirectory(ReportDirectory);
            if (!File.Exists(ReportPath))
            {
                WriteReport(completed: false);
            }

            EditorUtility.RevealInFinder(ReportPath);
        }

        [MenuItem(
            "Tools/GameWork/Patch 4.0/Validation/Write Compilation Snapshot")]
        public static void WriteCompilationSnapshot()
        {
            WriteReport(completed: !compilationInProgress);
            Debug.Log("Patch 4 compilation snapshot written to: " + ReportPath);
        }

        private static void OnCompilationStarted(object context)
        {
            compilationInProgress = true;
            Messages.Clear();
        }

        private static void OnAssemblyCompilationFinished(
            string assemblyPath,
            CompilerMessage[] compilerMessages)
        {
            if (compilerMessages == null)
            {
                return;
            }

            for (int i = 0; i < compilerMessages.Length; i++)
            {
                CompilerMessage compilerMessage = compilerMessages[i];
                string normalizedFile =
                    (compilerMessage.file ?? string.Empty).Replace('\\', '/');

                Messages.Add(new MessageRecord
                {
                    assembly = assemblyPath ?? string.Empty,
                    type = compilerMessage.type.ToString(),
                    message = compilerMessage.message ?? string.Empty,
                    file = normalizedFile,
                    line = compilerMessage.line,
                    column = compilerMessage.column,
                    belongsToPatch4 = normalizedFile.IndexOf(
                        "/GameWorkPatch4/",
                        StringComparison.OrdinalIgnoreCase) >= 0
                });
            }
        }

        private static void OnCompilationFinished(object context)
        {
            compilationInProgress = false;
            WriteReport(completed: true);
        }

        private static void WriteReport(bool completed)
        {
            CompilationReport report = new()
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                completed = completed,
                messages = new List<MessageRecord>(Messages)
            };

            for (int i = 0; i < report.messages.Count; i++)
            {
                MessageRecord record = report.messages[i];
                bool isError = string.Equals(
                    record.type,
                    CompilerMessageType.Error.ToString(),
                    StringComparison.Ordinal);
                bool isWarning = string.Equals(
                    record.type,
                    CompilerMessageType.Warning.ToString(),
                    StringComparison.Ordinal);

                if (isError)
                {
                    report.errorCount++;
                    if (record.belongsToPatch4)
                    {
                        report.patch4ErrorCount++;
                    }
                }
                else if (isWarning)
                {
                    report.warningCount++;
                    if (record.belongsToPatch4)
                    {
                        report.patch4WarningCount++;
                    }
                }
            }

            report.succeeded = completed && report.errorCount == 0;

            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(
                ReportPath,
                JsonUtility.ToJson(report, true));
        }
    }
}
