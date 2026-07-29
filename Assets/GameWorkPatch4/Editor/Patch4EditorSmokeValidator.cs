using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Performs a non-destructive validation of the generated Patch 4 prefab.
    /// This does not approve art and does not enable the character.
    /// </summary>
    public static class Patch4EditorSmokeValidator
    {
        [Serializable]
        private sealed class Finding
        {
            public string severity = string.Empty;
            public string code = string.Empty;
            public string message = string.Empty;
        }

        [Serializable]
        private sealed class NeutralPoseGateReport
        {
            public bool passedTechnicalChecks;
            public bool humanReviewRequired;
            public bool activationAllowed;
        }

        [Serializable]
        private sealed class SmokeReport
        {
            public string generatedUtc = string.Empty;
            public string unityVersion = string.Empty;
            public string prefabPath = string.Empty;
            public bool passed;
            public int errorCount;
            public int warningCount;
            public int boneCount;
            public int clipCount;
            public int requiredLayerCount;
            public int missingLayerCount;
            public bool artApproved;
            public bool patch4InitiallyHidden;
            public bool runtimeResourceLoadable;
            public bool canvasPresentationPrepared;
            public int canvasLayerCount;
            public int fallbackSpriteRendererCount;
            public bool fallbackSpriteRenderersDisabled;
            public bool neutralPoseTechnicalChecksPassed;
            public bool neutralPoseHumanReviewRequired;
            public bool neutralPoseActivationBlocked;
            public List<Finding> findings = new();
        }

        private const string ExpectedSha =
            "7b151f1ded93f3852bc8a7218ab26f94298b7f822094304bbcea9c076cad72a3";

        public static string ReportPath => Path.Combine(
            Patch4CompilationMonitor.ReportDirectory,
            "patch4-editor-smoke-report.json");

        [MenuItem(
            "Tools/GameWork/Patch 4.0/Validation/Run Editor Smoke Validation")]
        public static bool ValidateAndWriteReport()
        {
            SmokeReport report = new()
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                prefabPath = Patch4PrefabBuilder.PrefabPath,
                requiredLayerCount = Patch4RigContract.RequiredLayerPaths.Count
            };

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                Patch4PrefabBuilder.PrefabPath);
            if (prefab == null)
            {
                AddError(
                    report,
                    "PREFAB_MISSING",
                    "Generated Patch 4 prefab was not found. Run Rebuild Runtime Assets first.");
                Finish(report);
                return false;
            }

            ValidateRuntimeResource(prefab, report);
            ValidateNeutralPoseReport(report);

            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(
                    Patch4PrefabBuilder.PrefabPath);

                ValidateRig(contents, report);
                ValidateAnimator(contents, report);
                ValidateLayerCatalog(contents, report);
                ValidateCanvasPresentation(contents, report);
                ValidateInitialVisibility(contents, report);
            }
            catch (Exception exception)
            {
                AddError(
                    report,
                    "VALIDATOR_EXCEPTION",
                    exception.GetType().Name + ": " + exception.Message);
            }
            finally
            {
                if (contents != null)
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            Finish(report);
            return report.passed;
        }

        private static void ValidateRuntimeResource(
            GameObject prefab,
            SmokeReport report)
        {
            GameObject runtimePrefab = Resources.Load<GameObject>(
                Patch4PrefabBuilder.PrefabResourcePath);
            report.runtimeResourceLoadable = runtimePrefab == prefab;
            if (!report.runtimeResourceLoadable)
            {
                AddError(
                    report,
                    "RUNTIME_RESOURCE_MISSING",
                    "The generated Patch 4 prefab cannot be loaded from " +
                    "Resources at runtime.");
            }
        }

        private static void ValidateNeutralPoseReport(
            SmokeReport report)
        {
            if (!File.Exists(Patch4NeutralPoseValidator.ReportPath))
            {
                Patch4NeutralPoseValidator.ValidateAndWriteReport();
            }

            if (!File.Exists(Patch4NeutralPoseValidator.ReportPath))
            {
                AddError(
                    report,
                    "NEUTRAL_POSE_REPORT_MISSING",
                    "Neutral-pose QA report was not generated.");
                return;
            }

            NeutralPoseGateReport gate;
            try
            {
                gate = JsonUtility.FromJson<NeutralPoseGateReport>(
                    File.ReadAllText(
                        Patch4NeutralPoseValidator.ReportPath));
            }
            catch (Exception exception)
            {
                AddError(
                    report,
                    "NEUTRAL_POSE_REPORT_UNREADABLE",
                    exception.Message);
                return;
            }

            if (gate == null)
            {
                AddError(
                    report,
                    "NEUTRAL_POSE_REPORT_UNREADABLE",
                    "Neutral-pose QA report contains no readable data.");
                return;
            }

            report.neutralPoseTechnicalChecksPassed =
                gate.passedTechnicalChecks;
            report.neutralPoseHumanReviewRequired =
                gate.humanReviewRequired;
            report.neutralPoseActivationBlocked =
                !gate.activationAllowed;

            if (!report.neutralPoseTechnicalChecksPassed ||
                !report.neutralPoseHumanReviewRequired ||
                !report.neutralPoseActivationBlocked)
            {
                AddError(
                    report,
                    "NEUTRAL_POSE_GATE_INVALID",
                    "Neutral-pose QA must produce a complete technical " +
                    "composite while explicitly requiring human review and " +
                    "blocking activation.");
            }
        }

        [MenuItem(
            "Tools/GameWork/Patch 4.0/Validation/Open Editor Smoke Report")]
        public static void OpenReport()
        {
            Directory.CreateDirectory(
                Patch4CompilationMonitor.ReportDirectory);
            if (!File.Exists(ReportPath))
            {
                ValidateAndWriteReport();
            }

            EditorUtility.RevealInFinder(ReportPath);
        }

        private static void ValidateRig(GameObject contents, SmokeReport report)
        {
            Patch4CharacterRigController rig =
                contents.GetComponent<Patch4CharacterRigController>();
            if (rig == null)
            {
                AddError(
                    report,
                    "RIG_CONTROLLER_MISSING",
                    "Patch4CharacterRigController is missing from the prefab root.");
                return;
            }

            bool rigValid = rig.RebuildBoneMap();
            report.boneCount = CountTransforms(rig.RigRoot);
            if (!rigValid)
            {
                AddError(
                    report,
                    "SKELETON_INCOMPLETE",
                    "Missing bones: " + string.Join(", ", rig.MissingBones));
            }

            if (!string.Equals(
                    rig.ExpectedSourceSha256,
                    ExpectedSha,
                    StringComparison.OrdinalIgnoreCase))
            {
                AddError(
                    report,
                    "MASTER_SHA_MISMATCH",
                    "Prefab expects a different master SHA-256.");
            }

            if (rig.ArtReadiness == null)
            {
                AddError(
                    report,
                    "READINESS_ASSET_MISSING",
                    "Patch4ArtReadiness.asset is not bound to the prefab.");
            }

            report.artApproved = rig.IsArtApproved;
            if (report.artApproved)
            {
                AddWarning(
                    report,
                    "ART_ALREADY_APPROVED",
                    "Production art is approved. Confirm that manual joint, face, pixel and Play Mode review was intentional.");
            }
        }

        private static void ValidateAnimator(
            GameObject contents,
            SmokeReport report)
        {
            Animator animator = contents.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                AddError(
                    report,
                    "ANIMATOR_MISSING",
                    "Animator or RuntimeAnimatorController is missing.");
                return;
            }

            HashSet<string> clipNames = new(
                animator.runtimeAnimatorController.animationClips
                    .Where(clip => clip != null)
                    .Select(clip => clip.name),
                StringComparer.Ordinal);
            report.clipCount = clipNames.Count;

            List<string> missing = Patch4RigContract.RequiredClipNames
                .Where(name => !clipNames.Contains(name))
                .ToList();
            if (missing.Count > 0)
            {
                AddError(
                    report,
                    "ANIMATION_SET_INCOMPLETE",
                    "Missing clips: " + string.Join(", ", missing));
            }
        }

        private static void ValidateLayerCatalog(
            GameObject contents,
            SmokeReport report)
        {
            Patch4LayerRenderer renderer =
                contents.GetComponent<Patch4LayerRenderer>();
            if (renderer == null)
            {
                AddError(
                    report,
                    "LAYER_RENDERER_MISSING",
                    "Patch4LayerRenderer is missing from the prefab.");
                return;
            }

            SerializedObject serialized = new(renderer);
            Patch4LayerCatalog catalog = serialized
                .FindProperty("catalog")?.objectReferenceValue as Patch4LayerCatalog;
            if (catalog == null)
            {
                AddError(
                    report,
                    "LAYER_CATALOG_MISSING",
                    "Layer catalog is not assigned to Patch4LayerRenderer.");
                return;
            }

            bool complete = catalog.IsComplete(out List<string> missingLayers);
            report.missingLayerCount = missingLayers.Count;
            if (!complete)
            {
                AddError(
                    report,
                    "PAINTED_LAYERS_INCOMPLETE",
                    "Missing or empty layers: " + string.Join(", ", missingLayers));
            }
        }

        private static void ValidateInitialVisibility(
            GameObject contents,
            SmokeReport report)
        {
            Patch4CharacterRigController rig =
                contents.GetComponent<Patch4CharacterRigController>();
            if (rig == null)
            {
                return;
            }

            SerializedObject serialized = new(rig);
            GameObject patch4Visual = serialized
                .FindProperty("patch4VisualRoot")?.objectReferenceValue as GameObject;
            bool serializedEnabled = serialized
                .FindProperty("patch4Enabled")?.boolValue ?? false;

            report.patch4InitiallyHidden =
                !serializedEnabled &&
                (patch4Visual == null || !patch4Visual.activeSelf);

            if (!report.patch4InitiallyHidden)
            {
                AddError(
                    report,
                    "PATCH4_NOT_LOCKED",
                    "Generated prefab must start disabled with Patch4VisualRoot hidden.");
            }
        }

        private static void ValidateCanvasPresentation(
            GameObject contents,
            SmokeReport report)
        {
            Patch4CanvasPresentation presentation =
                contents.GetComponent<Patch4CanvasPresentation>();
            if (presentation == null)
            {
                AddError(
                    report,
                    "CANVAS_PRESENTATION_MISSING",
                    "Patch4CanvasPresentation is missing from the prefab.");
                return;
            }

            report.canvasPresentationPrepared =
                presentation.RebuildCanvasLayers();
            report.canvasLayerCount = presentation.ImageCount;

            if (!report.canvasPresentationPrepared ||
                report.canvasLayerCount !=
                Patch4RigContract.RequiredLayerPaths.Count)
            {
                AddError(
                    report,
                    "CANVAS_LAYER_SET_INCOMPLETE",
                    "Canvas presentation prepared " +
                    report.canvasLayerCount + " of " +
                    Patch4RigContract.RequiredLayerPaths.Count +
                    " required UI Images. Missing: " +
                    string.Join(", ", presentation.MissingLayers));
            }

            Image[] canvasImages =
                contents.GetComponentsInChildren<Image>(true);
            if (canvasImages.Length != report.canvasLayerCount)
            {
                AddError(
                    report,
                    "CANVAS_IMAGE_COUNT_MISMATCH",
                    "Generated Canvas Image hierarchy does not match the " +
                    "presentation binding count.");
            }

            SpriteRenderer[] fallbackRenderers =
                contents.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer[] paintedFallbacks =
                fallbackRenderers
                    .Where(renderer =>
                        renderer != null &&
                        renderer.gameObject.name.StartsWith(
                            "Layer.",
                            StringComparison.Ordinal))
                    .ToArray();
            report.fallbackSpriteRendererCount =
                paintedFallbacks.Length;
            report.fallbackSpriteRenderersDisabled =
                paintedFallbacks.All(renderer => !renderer.enabled);

            if (report.fallbackSpriteRendererCount !=
                Patch4RigContract.RequiredLayerPaths.Count ||
                !report.fallbackSpriteRenderersDisabled)
            {
                AddError(
                    report,
                    "FALLBACK_SPRITE_SET_INVALID",
                    "All 40 SpriteRenderer fallback layers must exist and " +
                    "stay disabled when the Canvas presentation is prepared.");
            }
        }

        private static int CountTransforms(Transform root)
        {
            if (root == null)
            {
                return 0;
            }

            int count = 1;
            for (int i = 0; i < root.childCount; i++)
            {
                count += CountTransforms(root.GetChild(i));
            }

            return count;
        }

        private static void AddError(
            SmokeReport report,
            string code,
            string message)
        {
            report.errorCount++;
            report.findings.Add(new Finding
            {
                severity = "error",
                code = code,
                message = message
            });
            Debug.LogError("Patch 4 smoke validation: " + code + " — " + message);
        }

        private static void AddWarning(
            SmokeReport report,
            string code,
            string message)
        {
            report.warningCount++;
            report.findings.Add(new Finding
            {
                severity = "warning",
                code = code,
                message = message
            });
            Debug.LogWarning("Patch 4 smoke validation: " + code + " — " + message);
        }

        private static void Finish(SmokeReport report)
        {
            report.passed = report.errorCount == 0;
            Directory.CreateDirectory(
                Patch4CompilationMonitor.ReportDirectory);
            File.WriteAllText(
                ReportPath,
                JsonUtility.ToJson(report, true));

            if (report.passed)
            {
                Debug.Log(
                    "Patch 4 Editor smoke validation PASSED. Report: " +
                    ReportPath);
            }
            else
            {
                Debug.LogError(
                    "Patch 4 Editor smoke validation FAILED with " +
                    report.errorCount + " error(s). Report: " + ReportPath);
            }
        }
    }
}
