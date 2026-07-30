using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Reassembles the neutral painted pose from the canonical full-canvas
    /// layers and compares it with the locked quality master. The report is
    /// diagnostic only: it always requires human review and can never approve
    /// or activate Patch 4.
    /// </summary>
    public static class Patch4NeutralPoseValidator
    {
        [Serializable]
        private sealed class NeutralPoseReport
        {
            public int schemaVersion = 1;
            public string generatedUtc = string.Empty;
            public bool passedTechnicalChecks;
            public bool technicalCompositeCreated;
            public bool humanReviewRequired = true;
            public bool activationAllowed;
            public string masterPath = string.Empty;
            public string compositePath = string.Empty;
            public string differencePath = string.Empty;
            public string contactSheetPath = string.Empty;
            public string facePoseContactSheetPath = string.Empty;
            public int requiredLayerCount;
            public int neutralLayerCount;
            public int loadedNeutralLayerCount;
            public int independentFacePoseCount;
            public int masterVisiblePixelCount;
            public int compositeVisiblePixelCount;
            public float masterCoverage;
            public float alphaLeakage;
            public float silhouetteIntersectionOverUnion;
            public float meanColorError;
            public float closeColorMatchRatio;
            public bool facePosePreviewCreated;
            public string[] neutralLayers = Array.Empty<string>();
            public string[] errors = Array.Empty<string>();
            public string[] warnings = Array.Empty<string>();
        }

        private sealed class ImageData
        {
            public int width;
            public int height;
            public Color32[] pixels;
        }

        private const int ExpectedWidth = 1024;
        private const int ExpectedHeight = 1536;
        private const byte VisibleThreshold = 8;
        private const int CloseColorThreshold = 12;
        private const int ExcludedComparisonLayerCount = 7;
        private const int FaceCropX = 352;
        private const int FaceCropTop = 112;
        private const int FaceCropWidth = 320;
        private const int FaceCropHeight = 360;
        private const int FacePoseCount = 4;

        public static string ReportPath => Path.Combine(
            Patch4CompilationMonitor.ReportDirectory,
            "patch4-neutral-pose-report.json");

        public static string CompositePath => Path.Combine(
            Patch4CompilationMonitor.ReportDirectory,
            "patch4-neutral-pose-composite.png");

        public static string DifferencePath => Path.Combine(
            Patch4CompilationMonitor.ReportDirectory,
            "patch4-neutral-pose-difference.png");

        public static string ContactSheetPath => Path.Combine(
            Patch4CompilationMonitor.ReportDirectory,
            "patch4-neutral-pose-review.png");

        public static string FacePoseContactSheetPath => Path.Combine(
            Patch4CompilationMonitor.ReportDirectory,
            "patch4-face-pose-review.png");

        [MenuItem(
            "Tools/GameWork/Patch 4.0/Validation/Rebuild Neutral Pose QA")]
        public static bool ValidateAndWriteReport()
        {
            List<string> errors = new();
            List<string> warnings = new();
            List<string> neutralPaths = BuildNeutralLayerPaths();
            NeutralPoseReport report = new()
            {
                generatedUtc = DateTime.UtcNow.ToString("O"),
                masterPath = Patch4MaskDrivenLayerBaker.MasterPath,
                compositePath = NormalizePath(CompositePath),
                differencePath = NormalizePath(DifferencePath),
                contactSheetPath = NormalizePath(ContactSheetPath),
                facePoseContactSheetPath =
                    NormalizePath(FacePoseContactSheetPath),
                requiredLayerCount =
                    Patch4RigContract.RequiredLayerPaths.Count,
                neutralLayerCount = neutralPaths.Count,
                independentFacePoseCount = FacePoseCount,
                neutralLayers = neutralPaths.ToArray()
            };

            if (report.neutralLayerCount !=
                report.requiredLayerCount - ExcludedComparisonLayerCount)
            {
                errors.Add(
                    "Neutral-pose layer count is " +
                    report.neutralLayerCount + "; expected " +
                    (report.requiredLayerCount -
                     ExcludedComparisonLayerCount) + ".");
            }

            DeletePreviousPreview(CompositePath, errors);
            DeletePreviousPreview(DifferencePath, errors);
            DeletePreviousPreview(ContactSheetPath, errors);
            DeletePreviousPreview(FacePoseContactSheetPath, errors);

            ImageData master = LoadImage(
                Patch4MaskDrivenLayerBaker.MasterPath,
                errors);
            if (master != null &&
                (master.width != ExpectedWidth ||
                 master.height != ExpectedHeight))
            {
                errors.Add(
                    "Approved master is " + master.width + "x" +
                    master.height + "; expected " + ExpectedWidth + "x" +
                    ExpectedHeight + ".");
            }

            Color32[] composite =
                master != null &&
                master.width == ExpectedWidth &&
                master.height == ExpectedHeight
                    ? new Color32[
                        ExpectedWidth * ExpectedHeight]
                    : null;
            int loadedNeutralLayerCount = 0;
            for (int i = 0; i < neutralPaths.Count; i++)
            {
                string contractPath = neutralPaths[i];
                ImageData layer = LoadImage(
                    LayerPath(contractPath),
                    errors);
                if (layer == null)
                {
                    continue;
                }

                if (layer.width != ExpectedWidth ||
                    layer.height != ExpectedHeight)
                {
                    errors.Add(
                        contractPath + " is " + layer.width + "x" +
                        layer.height + "; expected " + ExpectedWidth + "x" +
                        ExpectedHeight + ".");
                    continue;
                }

                loadedNeutralLayerCount++;
                if (composite != null)
                {
                    CompositeLayer(composite, layer.pixels);
                }
            }

            report.loadedNeutralLayerCount =
                loadedNeutralLayerCount;
            if (master != null &&
                master.width == ExpectedWidth &&
                master.height == ExpectedHeight &&
                composite != null &&
                loadedNeutralLayerCount == neutralPaths.Count)
            {
                Color32[] difference = BuildDifference(
                    master.pixels,
                    composite);
                Color32[] contactSheet = BuildContactSheet(
                    master.pixels,
                    composite,
                    difference);
                Color32[] facePoseContactSheet =
                    BuildFacePoseContactSheet(
                        composite,
                        errors);

                MeasureComparison(
                    master.pixels,
                    composite,
                    report);

                try
                {
                    Directory.CreateDirectory(
                        Patch4CompilationMonitor.ReportDirectory);
                    SavePng(
                        CompositePath,
                        ExpectedWidth,
                        ExpectedHeight,
                        composite);
                    SavePng(
                        DifferencePath,
                        ExpectedWidth,
                        ExpectedHeight,
                        difference);
                    SavePng(
                        ContactSheetPath,
                        ExpectedWidth * 3,
                        ExpectedHeight,
                        contactSheet);
                    if (facePoseContactSheet != null)
                    {
                        SavePng(
                            FacePoseContactSheetPath,
                            FaceCropWidth * FacePoseCount,
                            FaceCropHeight,
                            facePoseContactSheet);
                        report.facePosePreviewCreated = true;
                    }
                    report.technicalCompositeCreated = true;
                }
                catch (Exception exception)
                {
                    errors.Add(
                        "Could not write neutral-pose previews: " +
                        exception.Message);
                }

                AddMetricWarnings(report, warnings);
            }

            warnings.Add(
                "Automated pixel comparison cannot replace human review of " +
                "hidden joints, face poses or animation deformation.");

            report.passedTechnicalChecks =
                errors.Count == 0 &&
                report.technicalCompositeCreated &&
                report.facePosePreviewCreated &&
                report.loadedNeutralLayerCount == report.neutralLayerCount;
            report.activationAllowed = false;
            report.errors = errors.ToArray();
            report.warnings = warnings.ToArray();
            WriteReport(report);

            if (report.passedTechnicalChecks)
            {
                Debug.Log(
                    "Patch 4 neutral pose QA prepared " +
                    report.loadedNeutralLayerCount +
                    " neutral layers for locked human review. Coverage: " +
                    FormatPercent(report.masterCoverage) +
                    "; silhouette IoU: " +
                    FormatPercent(
                        report.silhouetteIntersectionOverUnion) +
                    "; four independent face poses prepared. Production " +
                    "activation remains locked. Report: " +
                    ReportPath);
            }
            else
            {
                Debug.LogWarning(
                    "Patch 4 neutral pose QA could not prepare a complete " +
                    "review image. Production activation remains locked. " +
                    "Report: " + ReportPath);
            }

            return report.passedTechnicalChecks;
        }

        private static List<string> BuildNeutralLayerPaths()
        {
            IReadOnlyList<string> required =
                Patch4RigContract.RequiredLayerPaths;
            List<int> indices = new(required.Count);
            for (int i = 0; i < required.Count; i++)
            {
                if (IsNeutralLayer(required[i]))
                {
                    indices.Add(i);
                }
            }

            indices.Sort((left, right) =>
            {
                int order = Patch4LayerPlacement.ResolveSortingOrder(
                        required[left])
                    .CompareTo(
                        Patch4LayerPlacement.ResolveSortingOrder(
                            required[right]));
                return order != 0 ? order : left.CompareTo(right);
            });

            List<string> result = new(indices.Count);
            for (int i = 0; i < indices.Count; i++)
            {
                result.Add(required[indices[i]]);
            }

            return result;
        }

        private static bool IsNeutralLayer(string contractPath)
        {
            return !string.Equals(
                       contractPath,
                       "Face/LidL",
                       StringComparison.Ordinal) &&
                   !string.Equals(
                       contractPath,
                       "Face/LidR",
                       StringComparison.Ordinal) &&
                   !string.Equals(
                       contractPath,
                       "Face/MouthOpen",
                       StringComparison.Ordinal) &&
                   !string.Equals(
                       contractPath,
                       "Face/MouthSmile",
                       StringComparison.Ordinal) &&
                   !string.Equals(
                       contractPath,
                       "FX/Sweat",
                       StringComparison.Ordinal) &&
                   !string.Equals(
                       contractPath,
                       "FX/ImpactFold",
                       StringComparison.Ordinal) &&
                   !string.Equals(
                       contractPath,
                       "FX/Shadow",
                       StringComparison.Ordinal);
        }

        private static void CompositeLayer(
            Color32[] destination,
            Color32[] source)
        {
            for (int pixelIndex = 0;
                 pixelIndex < destination.Length;
                 pixelIndex++)
            {
                if (source[pixelIndex].a == 0)
                {
                    continue;
                }

                destination[pixelIndex] = BlendOver(
                    destination[pixelIndex],
                    source[pixelIndex]);
            }
        }

        private static Color32 BlendOver(
            Color32 destination,
            Color32 source)
        {
            int sourceAlpha = source.a;
            if (sourceAlpha <= 0)
            {
                return destination;
            }

            if (sourceAlpha >= 255)
            {
                return source;
            }

            int destinationAlpha = destination.a;
            int inverseSourceAlpha = 255 - sourceAlpha;
            int outputAlpha =
                sourceAlpha +
                (destinationAlpha * inverseSourceAlpha + 127) / 255;
            if (outputAlpha <= 0)
            {
                return default;
            }

            int redPremultiplied =
                source.r * sourceAlpha +
                (destination.r * destinationAlpha *
                 inverseSourceAlpha + 127) / 255;
            int greenPremultiplied =
                source.g * sourceAlpha +
                (destination.g * destinationAlpha *
                 inverseSourceAlpha + 127) / 255;
            int bluePremultiplied =
                source.b * sourceAlpha +
                (destination.b * destinationAlpha *
                 inverseSourceAlpha + 127) / 255;

            return new Color32(
                ClampByte(
                    (redPremultiplied + outputAlpha / 2) /
                    outputAlpha),
                ClampByte(
                    (greenPremultiplied + outputAlpha / 2) /
                    outputAlpha),
                ClampByte(
                    (bluePremultiplied + outputAlpha / 2) /
                    outputAlpha),
                ClampByte(outputAlpha));
        }

        private static Color32[] BuildDifference(
            Color32[] master,
            Color32[] composite)
        {
            Color32[] result = new Color32[master.Length];
            for (int i = 0; i < result.Length; i++)
            {
                Color32 source = master[i];
                Color32 assembled = composite[i];
                bool sourceVisible = source.a > VisibleThreshold;
                bool assembledVisible =
                    assembled.a > VisibleThreshold;

                if (!sourceVisible && !assembledVisible)
                {
                    result[i] = default;
                    continue;
                }

                if (sourceVisible && !assembledVisible)
                {
                    result[i] = new Color32(0, 210, 255, 230);
                    continue;
                }

                if (!sourceVisible && assembledVisible)
                {
                    result[i] = new Color32(255, 0, 220, 230);
                    continue;
                }

                int difference = Mathf.Max(
                    Mathf.Abs(source.r - assembled.r),
                    Mathf.Max(
                        Mathf.Abs(source.g - assembled.g),
                        Mathf.Max(
                            Mathf.Abs(source.b - assembled.b),
                            Mathf.Abs(source.a - assembled.a))));
                if (difference <= 2)
                {
                    result[i] = default;
                    continue;
                }

                int heat = Mathf.Clamp(difference * 3, 0, 255);
                result[i] = new Color32(
                    255,
                    ClampByte(255 - heat),
                    0,
                    230);
            }

            return result;
        }

        private static Color32[] BuildContactSheet(
            Color32[] master,
            Color32[] composite,
            Color32[] difference)
        {
            int panelSize = ExpectedWidth * ExpectedHeight;
            Color32[] result = new Color32[panelSize * 3];
            for (int y = 0; y < ExpectedHeight; y++)
            {
                for (int x = 0; x < ExpectedWidth; x++)
                {
                    int sourceIndex = y * ExpectedWidth + x;
                    Color32 background =
                        ((x / 32 + y / 32) & 1) == 0
                            ? new Color32(42, 45, 52, 255)
                            : new Color32(62, 66, 75, 255);

                    SetContactPixel(
                        result,
                        0,
                        x,
                        y,
                        BlendOver(background, master[sourceIndex]));
                    SetContactPixel(
                        result,
                        1,
                        x,
                        y,
                        BlendOver(background, composite[sourceIndex]));
                    SetContactPixel(
                        result,
                        2,
                        x,
                        y,
                        BlendOver(background, difference[sourceIndex]));
                }
            }

            Color32[] borders =
            {
                new(70, 150, 255, 255),
                new(65, 210, 120, 255),
                new(255, 90, 90, 255)
            };
            for (int panel = 0; panel < 3; panel++)
            {
                for (int y = ExpectedHeight - 6;
                     y < ExpectedHeight;
                     y++)
                {
                    for (int x = 0; x < ExpectedWidth; x++)
                    {
                        SetContactPixel(
                            result,
                            panel,
                            x,
                            y,
                            borders[panel]);
                    }
                }
            }

            return result;
        }

        private static void SetContactPixel(
            Color32[] target,
            int panel,
            int x,
            int y,
            Color32 color)
        {
            int contactWidth = ExpectedWidth * 3;
            target[
                y * contactWidth +
                panel * ExpectedWidth +
                x] = color;
        }

        private static Color32[] BuildFacePoseContactSheet(
            Color32[] neutral,
            ICollection<string> errors)
        {
            Color32[][] poses =
            {
                (Color32[])neutral.Clone(),
                (Color32[])neutral.Clone(),
                (Color32[])neutral.Clone(),
                (Color32[])neutral.Clone()
            };

            bool complete = true;
            complete &= CompositePoseLayer(
                poses[1],
                "Face/LidL",
                errors);
            complete &= CompositePoseLayer(
                poses[1],
                "Face/LidR",
                errors);
            complete &= CompositePoseLayer(
                poses[2],
                "Face/MouthOpen",
                errors);
            complete &= CompositePoseLayer(
                poses[3],
                "Face/MouthSmile",
                errors);
            if (!complete)
            {
                return null;
            }

            int sheetWidth = FaceCropWidth * FacePoseCount;
            Color32[] result =
                new Color32[sheetWidth * FaceCropHeight];
            int sourceBottom =
                ExpectedHeight - FaceCropTop - FaceCropHeight;

            Color32[] accents =
            {
                new(65, 210, 120, 255),
                new(113, 168, 255, 255),
                new(255, 174, 75, 255),
                new(238, 105, 163, 255)
            };

            for (int pose = 0; pose < FacePoseCount; pose++)
            {
                for (int y = 0; y < FaceCropHeight; y++)
                {
                    int sourceY = sourceBottom + y;
                    for (int x = 0; x < FaceCropWidth; x++)
                    {
                        int sourceX = FaceCropX + x;
                        int sourceIndex =
                            sourceY * ExpectedWidth + sourceX;
                        Color32 checker =
                            ((x / 16 + y / 16) & 1) == 0
                                ? new Color32(42, 45, 52, 255)
                                : new Color32(62, 66, 75, 255);
                        Color32 color = BlendOver(
                            checker,
                            poses[pose][sourceIndex]);
                        if (y >= FaceCropHeight - 5)
                        {
                            color = accents[pose];
                        }

                        result[
                            y * sheetWidth +
                            pose * FaceCropWidth +
                            x] = color;
                    }
                }
            }

            return result;
        }

        private static bool CompositePoseLayer(
            Color32[] destination,
            string contractPath,
            ICollection<string> errors)
        {
            ImageData layer = LoadImage(
                LayerPath(contractPath),
                errors);
            if (layer == null)
            {
                return false;
            }

            if (layer.width != ExpectedWidth ||
                layer.height != ExpectedHeight)
            {
                errors.Add(
                    contractPath + " pose layer is " +
                    layer.width + "x" + layer.height +
                    "; expected " + ExpectedWidth + "x" +
                    ExpectedHeight + ".");
                return false;
            }

            CompositeLayer(destination, layer.pixels);
            return true;
        }

        private static void MeasureComparison(
            Color32[] master,
            Color32[] composite,
            NeutralPoseReport report)
        {
            int masterVisible = 0;
            int compositeVisible = 0;
            int intersection = 0;
            int union = 0;
            int leaked = 0;
            int masterTransparent = 0;
            int closeColorMatches = 0;
            double accumulatedColorError = 0d;

            for (int i = 0; i < master.Length; i++)
            {
                Color32 source = master[i];
                Color32 assembled = composite[i];
                bool sourceVisible = source.a > VisibleThreshold;
                bool assembledVisible =
                    assembled.a > VisibleThreshold;

                if (sourceVisible)
                {
                    masterVisible++;
                }
                else
                {
                    masterTransparent++;
                }

                if (assembledVisible)
                {
                    compositeVisible++;
                }

                if (sourceVisible || assembledVisible)
                {
                    union++;
                }

                if (sourceVisible && assembledVisible)
                {
                    intersection++;
                    int redDifference =
                        Mathf.Abs(source.r - assembled.r);
                    int greenDifference =
                        Mathf.Abs(source.g - assembled.g);
                    int blueDifference =
                        Mathf.Abs(source.b - assembled.b);
                    int alphaDifference =
                        Mathf.Abs(source.a - assembled.a);

                    accumulatedColorError +=
                        (redDifference +
                         greenDifference +
                         blueDifference) /
                        (3d * 255d);

                    if (Mathf.Max(
                            redDifference,
                            Mathf.Max(
                                greenDifference,
                                blueDifference)) <=
                        CloseColorThreshold &&
                        alphaDifference <= CloseColorThreshold)
                    {
                        closeColorMatches++;
                    }
                }
                else if (!sourceVisible && assembledVisible)
                {
                    leaked++;
                }
            }

            report.masterVisiblePixelCount = masterVisible;
            report.compositeVisiblePixelCount = compositeVisible;
            report.masterCoverage = SafeRatio(
                intersection,
                masterVisible);
            report.alphaLeakage = SafeRatio(
                leaked,
                masterTransparent);
            report.silhouetteIntersectionOverUnion =
                SafeRatio(intersection, union);
            report.meanColorError =
                intersection == 0
                    ? 1f
                    : (float)(
                        accumulatedColorError / intersection);
            report.closeColorMatchRatio = SafeRatio(
                closeColorMatches,
                masterVisible);
        }

        private static void AddMetricWarnings(
            NeutralPoseReport report,
            ICollection<string> warnings)
        {
            if (report.masterCoverage < 0.965f)
            {
                warnings.Add(
                    "Neutral composite covers only " +
                    FormatPercent(report.masterCoverage) +
                    " of the locked quality-master silhouette.");
            }

            if (report.alphaLeakage > 0.0025f)
            {
                warnings.Add(
                    "Neutral composite alpha leakage is " +
                    FormatPercent(report.alphaLeakage) + ".");
            }

            if (report.closeColorMatchRatio < 0.90f)
            {
                warnings.Add(
                    "Only " +
                    FormatPercent(report.closeColorMatchRatio) +
                    " of master pixels are a close color match. Review the " +
                    "difference panel before any art approval.");
            }
        }

        private static ImageData LoadImage(
            string assetPath,
            ICollection<string> errors)
        {
            string absolutePath = ToAbsolutePath(assetPath);
            if (!File.Exists(absolutePath))
            {
                errors.Add("Missing image: " + assetPath);
                return null;
            }

            Texture2D texture = null;
            try
            {
                texture = new Texture2D(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false,
                    false);
                if (!texture.LoadImage(
                        File.ReadAllBytes(absolutePath),
                        false))
                {
                    errors.Add("Unreadable image: " + assetPath);
                    return null;
                }

                return new ImageData
                {
                    width = texture.width,
                    height = texture.height,
                    pixels = texture.GetPixels32()
                };
            }
            catch (Exception exception)
            {
                errors.Add(
                    "Could not read " + assetPath + ": " +
                    exception.Message);
                return null;
            }
            finally
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        private static void SavePng(
            string path,
            int width,
            int height,
            Color32[] pixels)
        {
            Texture2D texture = new(
                width,
                height,
                TextureFormat.RGBA32,
                false,
                false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string LayerPath(string contractPath)
        {
            return Patch4MaskDrivenLayerBaker.LayerRoot + "/" +
                   contractPath.Replace('/', '_') + ".png";
        }

        private static void WriteReport(NeutralPoseReport report)
        {
            Directory.CreateDirectory(
                Patch4CompilationMonitor.ReportDirectory);
            File.WriteAllText(
                ReportPath,
                JsonUtility.ToJson(report, true));
        }

        private static void DeletePreviousPreview(
            string path,
            ICollection<string> errors)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception)
            {
                errors.Add(
                    "Could not replace stale neutral-pose preview " +
                    path + ": " + exception.Message);
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                Application.dataPath;
            return Path.Combine(
                projectRoot,
                assetPath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path).Replace('\\', '/');
        }

        private static float SafeRatio(int numerator, int denominator)
        {
            return denominator <= 0
                ? 0f
                : (float)numerator / denominator;
        }

        private static string FormatPercent(float value)
        {
            return (value * 100f).ToString(
                       "0.00",
                       CultureInfo.InvariantCulture) + "%";
        }

        private static byte ClampByte(int value)
        {
            return (byte)Mathf.Clamp(value, 0, 255);
        }
    }
}
