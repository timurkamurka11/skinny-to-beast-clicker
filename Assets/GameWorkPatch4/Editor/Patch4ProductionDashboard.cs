using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Read-only project status plus explicit production buttons. The window does
    /// not approve art or enable Patch 4.
    /// </summary>
    public sealed class Patch4ProductionDashboard : EditorWindow
    {
        private Vector2 scroll;

        [MenuItem("Tools/GameWork/Patch 4.0/Open Production Dashboard")]
        public static void Open()
        {
            GetWindow<Patch4ProductionDashboard>("Patch 4.0");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField(
                "GameWork Patch 4.0 — Production Dashboard",
                EditorStyles.boldLabel);
            EditorGUILayout.Space(6f);

            DrawStatus(
                "Repository mask manifest",
                AssetDatabase.LoadAssetAtPath<TextAsset>(
                    Patch4AdobeMaskDownloader.ManifestPath) != null,
                Patch4AdobeMaskDownloader.ManifestPath);
            DrawStatus(
                "Exact neutral master restored",
                FileExists(Patch4MaskDrivenLayerBaker.MasterPath),
                Patch4MaskDrivenLayerBaker.MasterPath);

            int maskCount = CountPng(Patch4AdobeMaskDownloader.DownloadedMaskRoot);
            DrawStatus(
                "Repository masks restored",
                maskCount >= 10,
                maskCount + " PNG file(s); target is 10 local masks");

            int layerCount = CountPng(Patch4MaskDrivenLayerBaker.LayerRoot);
            DrawStatus(
                "Canonical draft layer pack",
                layerCount >= Patch4RigContract.RequiredLayerPaths.Count,
                layerCount + " / " + Patch4RigContract.RequiredLayerPaths.Count);

            TextAsset report = AssetDatabase.LoadAssetAtPath<TextAsset>(
                Patch4DraftLayerValidator.ReportPath);
            bool reportPassed =
                report != null && report.text.Contains(
                    "\"passedTechnicalChecks\": true");
            DrawStatus(
                "Pixel and joint report",
                reportPassed,
                report == null
                    ? "report not generated"
                    : Patch4DraftLayerValidator.ReportPath);

            Patch4ArtReadinessAsset readiness =
                Patch4ArtReadinessAssetBuilder.EnsureAsset();
            DrawStatus(
                "Human production-art approval",
                readiness != null && readiness.ProductionArtApproved,
                readiness == null
                    ? "readiness asset unavailable"
                    : readiness.ProductionArtApproved
                        ? "approved — verify intentionally"
                        : "locked — correct until manual review passes");

            DrawStatus(
                "Animator Controller",
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    Patch4AnimationLibraryBuilder.ControllerPath) != null,
                Patch4AnimationLibraryBuilder.ControllerPath);
            DrawStatus(
                "Patch 4 prefab",
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    Patch4PrefabBuilder.PrefabPath) != null,
                Patch4PrefabBuilder.PrefabPath);

            DrawStatus(
                "Unity compilation report",
                JsonReportContains(
                    Patch4CompilationMonitor.ReportPath,
                    "\"succeeded\": true"),
                File.Exists(Patch4CompilationMonitor.ReportPath)
                    ? Patch4CompilationMonitor.ReportPath
                    : "compile report not generated yet");
            DrawStatus(
                "Editor prefab smoke report",
                JsonReportContains(
                    Patch4EditorSmokeValidator.ReportPath,
                    "\"passed\": true"),
                File.Exists(Patch4EditorSmokeValidator.ReportPath)
                    ? Patch4EditorSmokeValidator.ReportPath
                    : "smoke report not generated yet");

            EditorGUILayout.Space(12f);
            EditorGUILayout.HelpBox(
                "Required order: restore repository sources → bake draft layers → " +
                "manually repaint hidden joints and facial poses → validate → " +
                "rebuild runtime assets → run compile/smoke reports → run " +
                "EditMode and PlayMode tests → review in the room → approve " +
                "readiness. Draft generation never approves or enables Patch 4.",
                MessageType.Info);

            if (GUILayout.Button("1. Restore Repository Sources"))
            {
                Patch4ProductionPipeline.DownloadSources();
            }

            if (GUILayout.Button("2. Bake Draft Layer Pack"))
            {
                Patch4ProductionPipeline.BakeDraftLayers();
            }

            if (GUILayout.Button("3. Validate Draft Layers"))
            {
                Patch4ProductionPipeline.ValidateDraftLayers();
            }

            if (GUILayout.Button("4. Rebuild Locked Runtime Assets"))
            {
                Patch4ProductionPipeline.RebuildRuntimeAssets();
            }

            if (GUILayout.Button("5. Run Safety Validation"))
            {
                Patch4ProductionPipeline.RunSafetyValidation();
            }

            if (GUILayout.Button("6. Run Compilation + Editor Smoke Reports"))
            {
                Patch4ProductionPipeline.RunEditorSmokeReport();
            }

            if (GUILayout.Button("7. Open Unity Test Runner"))
            {
                Patch4ProductionPipeline.OpenUnityTestRunner();
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Open Compilation Report"))
            {
                Patch4CompilationMonitor.OpenCompilationReport();
            }

            if (GUILayout.Button("Open Editor Smoke Report"))
            {
                Patch4EditorSmokeValidator.OpenReport();
            }

            if (GUILayout.Button("Select Art Readiness Asset"))
            {
                Selection.activeObject = readiness;
            }

            if (GUILayout.Button("Select Pixel Validation Report"))
            {
                Selection.activeObject = report;
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.HelpBox(
                "Protected scope: MainMenuLoop.mp4, menu scenes/prefabs, music, " +
                "audio mixers and settings must remain unchanged.",
                MessageType.Warning);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawStatus(string label, bool passed, string detail)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(passed ? "PASS" : "WAIT", GUILayout.Width(46f));
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(detail ?? string.Empty, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private static bool JsonReportContains(string path, string marker)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                return File.ReadAllText(path).Contains(marker);
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static int CountPng(string assetDirectory)
        {
            string absolute = ToAbsolutePath(assetDirectory);
            return Directory.Exists(absolute)
                ? Directory.GetFiles(absolute, "*.png", SearchOption.TopDirectoryOnly).Length
                : 0;
        }

        private static bool FileExists(string assetPath)
        {
            return File.Exists(ToAbsolutePath(assetPath));
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(root, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
