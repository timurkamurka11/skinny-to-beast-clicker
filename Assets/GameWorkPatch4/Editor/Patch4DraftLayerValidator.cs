using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Pixel-level validation for the full-canvas Patch 4 candidate layer pack.
    /// Existence alone is not enough: the validator checks dimensions, alpha
    /// coverage, leakage, meaningful content, exclusive live-pixel ownership
    /// and the small intentional overlap at moving joints.
    /// </summary>
    public static class Patch4DraftLayerValidator
    {
        public const string ReportPath =
            "Assets/GameWorkPatch4/Art/Character/FatMan/layer-bake-report.json";

        private const int ExpectedWidth = 1024;
        private const int ExpectedHeight = 1536;
        private const byte VisibleThreshold = 8;

        private sealed class ImageData
        {
            public int width;
            public int height;
            public Color32[] pixels;
        }

        private readonly struct JointCheck
        {
            public readonly string name;
            public readonly string first;
            public readonly string second;
            public readonly Vector2 normalizedTopPoint;
            public readonly float radiusPixels;
            public readonly int minimumOverlapPixels;

            public JointCheck(
                string name,
                string first,
                string second,
                float x,
                float y,
                float radiusPixels = 46f,
                int minimumOverlapPixels = 180)
            {
                this.name = name;
                this.first = first;
                this.second = second;
                normalizedTopPoint = new Vector2(x, y);
                this.radiusPixels = radiusPixels;
                this.minimumOverlapPixels = minimumOverlapPixels;
            }
        }

        private readonly struct FaceReplacementCheck
        {
            public readonly string path;
            public readonly Rect normalizedTopRegion;
            public readonly float maximumVisibleRatio;

            public FaceReplacementCheck(
                string path,
                Rect normalizedTopRegion,
                float maximumVisibleRatio = .48f)
            {
                this.path = path;
                this.normalizedTopRegion = normalizedTopRegion;
                this.maximumVisibleRatio = maximumVisibleRatio;
            }
        }

        private readonly struct FaceTransitionCheck
        {
            public readonly string path;
            public readonly Rect normalizedTopRegion;

            public FaceTransitionCheck(
                string path,
                Rect normalizedTopRegion)
            {
                this.path = path;
                this.normalizedTopRegion = normalizedTopRegion;
            }
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Validate/Draft Layer Pack")]
        public static void ValidateAndWriteReport()
        {
            List<string> errors = new();
            List<string> warnings = new();
            Dictionary<string, ImageData> layers = new(StringComparer.Ordinal);

            ImageData master = LoadImage(Patch4MaskDrivenLayerBaker.MasterPath);
            if (master == null)
            {
                errors.Add("Locked quality master is missing or unreadable.");
                WriteReport(false, 0f, 0f, errors, warnings, Array.Empty<string>());
                AssetDatabase.Refresh();
                return;
            }

            if (master.width != ExpectedWidth || master.height != ExpectedHeight)
            {
                errors.Add(
                    $"Master is {master.width}×{master.height}; expected " +
                    $"{ExpectedWidth}×{ExpectedHeight}.");
            }

            foreach (string contractPath in Patch4RigContract.RequiredLayerPaths)
            {
                string path = LayerPath(contractPath);
                ImageData layer = LoadImage(path);
                if (layer == null)
                {
                    errors.Add("Missing layer: " + contractPath);
                    continue;
                }

                if (layer.width != ExpectedWidth || layer.height != ExpectedHeight)
                {
                    errors.Add(
                        $"Layer {contractPath} is {layer.width}×{layer.height}; " +
                        $"expected {ExpectedWidth}×{ExpectedHeight}.");
                    continue;
                }

                int visiblePixels = CountVisible(layer.pixels);
                int minimum = ResolveMinimumVisiblePixels(contractPath);
                if (visiblePixels < minimum)
                {
                    errors.Add(
                        $"Layer {contractPath} contains only {visiblePixels} " +
                        $"visible pixels; minimum candidate threshold is {minimum}.");
                }

                layers[contractPath] = layer;
            }

            ValidateFaceReplacementLayers(layers, errors);
            ValidateFaceTransitionLayers(layers, errors);
            ValidateExclusiveRuntimeOwnership(layers, errors);

            float coverage = 0f;
            float leakage = 0f;
            if (master.width == ExpectedWidth && master.height == ExpectedHeight)
            {
                List<ImageData> characterLayers = new();
                IReadOnlyList<string> neutralPaths =
                    Patch4RigContract.RuntimeNeutralLayerPaths;
                for (int i = 0; i < neutralPaths.Count; i++)
                {
                    if (layers.TryGetValue(
                        neutralPaths[i],
                        out ImageData layer))
                    {
                        characterLayers.Add(layer);
                    }
                }

                MeasureCoverage(
                    master,
                    characterLayers,
                    out coverage,
                    out leakage);
                if (coverage < 0.965f)
                {
                    errors.Add(
                        "Character layer union covers only " +
                        FormatPercent(coverage) +
                        " of the locked quality-master alpha. Minimum is 96.5%." );
                }

                if (leakage > 0.0025f)
                {
                    errors.Add(
                        "Character layer union leaks " +
                        FormatPercent(leakage) +
                        " outside the locked quality-master alpha. Maximum is 0.25%." );
                }
            }

            List<string> jointResults = new();
            JointCheck[] checks = BuildJointChecks();
            for (int i = 0; i < checks.Length; i++)
            {
                JointCheck check = checks[i];
                if (!layers.TryGetValue(check.first, out ImageData first) ||
                    !layers.TryGetValue(check.second, out ImageData second))
                {
                    jointResults.Add(check.name + ": unavailable");
                    continue;
                }

                int overlap = CountLocalOverlap(first, second, check);
                jointResults.Add(check.name + ": " + overlap + " px");
                if (overlap < check.minimumOverlapPixels)
                {
                    errors.Add(
                        $"Joint {check.name} has only {overlap} overlapping " +
                        $"pixels near its pivot; minimum is " +
                        $"{check.minimumOverlapPixels}. The hidden continuation " +
                        "is not production-review ready.");
                }
            }

            string draftStatusAbsolute = ToAbsolutePath(
                Patch4MaskDrivenLayerBaker.DraftMetadataPath);
            if (!File.Exists(draftStatusAbsolute))
            {
                warnings.Add("Draft metadata file is missing.");
            }
            else
            {
                string draftStatus = File.ReadAllText(draftStatusAbsolute);
                if (!draftStatus.Contains("\"activationAllowed\": false"))
                {
                    errors.Add(
                        "Draft metadata does not explicitly block activation.");
                }
            }

            bool passed = errors.Count == 0;
            WriteReport(passed, coverage, leakage, errors, warnings, jointResults);
            AssetDatabase.Refresh();

            if (passed)
            {
                Debug.Log(
                    "Patch 4 candidate layer validation passed technical checks. " +
                    "Human joint, face and motion review is still required " +
                    "before activation.");
            }
            else
            {
                for (int i = 0; i < errors.Count; i++)
                {
                    Debug.LogWarning(
                    "Patch 4 candidate blocker " + (i + 1) + "/" + errors.Count +
                        ": " + errors[i]);
                }

                Debug.LogWarning(
                    "Patch 4 candidate layer validation found " + errors.Count +
                    " blocking issue(s). See " + ReportPath + ".");
            }
        }

        private static JointCheck[] BuildJointChecks()
        {
            return new[]
            {
                new JointCheck("neck-head", "Body/Neck", "Head/HeadBase", 0.5f, 0.225f),
                new JointCheck("left-shoulder", "Clothes/ShirtBase", "ArmL/Upper", 0.34f, 0.285f),
                new JointCheck("right-shoulder", "Clothes/ShirtBase", "ArmR/Upper", 0.66f, 0.285f),
                new JointCheck("left-elbow", "ArmL/Upper", "ArmL/Forearm", 0.285f, 0.405f),
                new JointCheck("right-elbow", "ArmR/Upper", "ArmR/Forearm", 0.715f, 0.405f),
                new JointCheck("left-wrist", "ArmL/Forearm", "ArmL/Hand", 0.255f, 0.495f),
                new JointCheck("right-wrist", "ArmR/Forearm", "ArmR/Hand", 0.745f, 0.495f),
                new JointCheck("left-hip", "Clothes/ShirtBase", "LegL/Thigh", 0.42f, 0.505f),
                new JointCheck("right-hip", "Clothes/ShirtBase", "LegR/Thigh", 0.58f, 0.505f),
                new JointCheck("left-knee", "LegL/Thigh", "LegL/Shin", 0.40f, 0.625f),
                new JointCheck("right-knee", "LegR/Thigh", "LegR/Shin", 0.60f, 0.625f),
                new JointCheck("left-ankle", "LegL/Shin", "LegL/Foot", 0.385f, 0.735f),
                new JointCheck("right-ankle", "LegR/Shin", "LegR/Foot", 0.615f, 0.735f)
            };
        }

        private static void ValidateExclusiveRuntimeOwnership(
            IReadOnlyDictionary<string, ImageData> layers,
            ICollection<string> errors)
        {
            IReadOnlyList<string> paths =
                Patch4RigContract.RuntimeExclusiveArtworkLayerPaths;
            JointCheck[] joints = BuildJointChecks();
            int uncontrolledOverlapPixels = 0;

            for (int y = 0; y < ExpectedHeight; y++)
            {
                for (int x = 0; x < ExpectedWidth; x++)
                {
                    int index = y * ExpectedWidth + x;
                    int ownerCount = 0;
                    for (int pathIndex = 0;
                         pathIndex < paths.Count;
                         pathIndex++)
                    {
                        if (layers.TryGetValue(
                                paths[pathIndex],
                                out ImageData layer) &&
                            layer.pixels[index].a > VisibleThreshold)
                        {
                            ownerCount++;
                        }
                    }

                    if (ownerCount > 1 &&
                        !IsInsideAuthorizedJoint(x, y, joints))
                    {
                        uncontrolledOverlapPixels++;
                    }
                }
            }

            if (uncontrolledOverlapPixels > 0)
            {
                errors.Add(
                    "Runtime neutral artwork has " +
                    uncontrolledOverlapPixels +
                    " multiply-owned pixels outside authorized joints. " +
                    "Those duplicate pixels will split into extra limbs or " +
                    "a detached face when the rig moves.");
            }
        }

        private static bool IsInsideAuthorizedJoint(
            int x,
            int y,
            IReadOnlyList<JointCheck> joints)
        {
            for (int i = 0; i < joints.Count; i++)
            {
                JointCheck joint = joints[i];
                float centerX =
                    joint.normalizedTopPoint.x * ExpectedWidth;
                float centerY =
                    ExpectedHeight -
                    joint.normalizedTopPoint.y * ExpectedHeight;
                float dx = x - centerX;
                float dy = y - centerY;
                if (dx * dx + dy * dy <=
                    joint.radiusPixels * joint.radiusPixels)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateFaceReplacementLayers(
            IReadOnlyDictionary<string, ImageData> layers,
            ICollection<string> errors)
        {
            FaceReplacementCheck[] checks =
            {
                new(
                    "Face/EyeWhiteL",
                    new Rect(.438f, .164f, .063f, .045f)),
                new(
                    "Face/EyeWhiteR",
                    new Rect(.499f, .164f, .063f, .045f)),
                new(
                    "Face/IrisL",
                    new Rect(.438f, .164f, .063f, .045f)),
                new(
                    "Face/IrisR",
                    new Rect(.499f, .164f, .063f, .045f)),
                new(
                    "Face/LidL",
                    new Rect(.438f, .164f, .063f, .045f)),
                new(
                    "Face/LidR",
                    new Rect(.499f, .164f, .063f, .045f)),
                new(
                    "Face/MouthClosed",
                    new Rect(.452f, .198f, .096f, .052f)),
                new(
                    "Face/MouthOpen",
                    new Rect(.452f, .198f, .096f, .052f)),
                new(
                    "Face/MouthSmile",
                    new Rect(.452f, .198f, .096f, .052f))
            };

            for (int i = 0; i < checks.Length; i++)
            {
                FaceReplacementCheck check = checks[i];
                if (!layers.TryGetValue(check.path, out ImageData layer))
                {
                    continue;
                }

                GetPixelBounds(
                    check.normalizedTopRegion,
                    out int minX,
                    out int maxX,
                    out int minY,
                    out int maxY);
                int regionPixelCount =
                    (maxX - minX + 1) *
                    (maxY - minY + 1);
                int visiblePixels = CountVisible(layer.pixels);
                int visibleInside = 0;
                int visibleBorder = 0;
                const int borderWidth = 3;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (layer.pixels[y * ExpectedWidth + x].a <=
                            VisibleThreshold)
                        {
                            continue;
                        }

                        visibleInside++;
                        if (x - minX < borderWidth ||
                            maxX - x < borderWidth ||
                            y - minY < borderWidth ||
                            maxY - y < borderWidth)
                        {
                            visibleBorder++;
                        }
                    }
                }

                float visibleRatio =
                    regionPixelCount == 0
                        ? 1f
                        : (float)visibleInside / regionPixelCount;
                int outside = visiblePixels - visibleInside;
                if (outside > 0 ||
                    visibleBorder > 0 ||
                    visibleRatio > check.maximumVisibleRatio)
                {
                    errors.Add(
                        $"Layer {check.path} still behaves like a backing " +
                        $"rectangle: {visibleRatio:P2} region fill, " +
                        $"{visibleBorder} border pixels and {outside} pixels " +
                        "outside its face region. Replacement poses must " +
                        "contain only their painted feature.");
                }
            }
        }

        private static void ValidateFaceTransitionLayers(
            IReadOnlyDictionary<string, ImageData> layers,
            ICollection<string> errors)
        {
            FaceTransitionCheck[] checks =
            {
                new(
                    "Face/CheekL",
                    new Rect(.438f, .164f, .063f, .045f)),
                new(
                    "Face/CheekR",
                    new Rect(.499f, .164f, .063f, .045f)),
                new(
                    "Face/CheekL",
                    new Rect(.452f, .198f, .096f, .052f)),
                new(
                    "Face/CheekR",
                    new Rect(.452f, .198f, .096f, .052f))
            };

            const int maximumHardCutPixels = 6;
            for (int i = 0; i < checks.Length; i++)
            {
                FaceTransitionCheck check = checks[i];
                if (!layers.TryGetValue(
                        check.path,
                        out ImageData layer))
                {
                    continue;
                }

                int hardCutPixels = CountHardAlphaCutPixels(
                    layer,
                    check.normalizedTopRegion);
                if (hardCutPixels <= maximumHardCutPixels)
                {
                    continue;
                }

                errors.Add(
                    $"Layer {check.path} has {hardCutPixels} abrupt " +
                    "transparent-to-opaque pixels on a face patch border. " +
                    "Neutral feature removal must use an elliptical feather, " +
                    "never a rectangular cut.");
            }
        }

        private static int CountHardAlphaCutPixels(
            ImageData layer,
            Rect region)
        {
            GetPixelBounds(
                region,
                out int minX,
                out int maxX,
                out int minY,
                out int maxY);
            int count = 0;

            if (minY > 0)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    count += IsHardAlphaCut(
                        layer.pixels[minY * ExpectedWidth + x].a,
                        layer.pixels[
                            (minY - 1) * ExpectedWidth + x].a)
                        ? 1
                        : 0;
                }
            }

            if (maxY < ExpectedHeight - 1)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    count += IsHardAlphaCut(
                        layer.pixels[maxY * ExpectedWidth + x].a,
                        layer.pixels[
                            (maxY + 1) * ExpectedWidth + x].a)
                        ? 1
                        : 0;
                }
            }

            if (minX > 0)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    count += IsHardAlphaCut(
                        layer.pixels[y * ExpectedWidth + minX].a,
                        layer.pixels[
                            y * ExpectedWidth + minX - 1].a)
                        ? 1
                        : 0;
                }
            }

            if (maxX < ExpectedWidth - 1)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    count += IsHardAlphaCut(
                        layer.pixels[y * ExpectedWidth + maxX].a,
                        layer.pixels[
                            y * ExpectedWidth + maxX + 1].a)
                        ? 1
                        : 0;
                }
            }

            return count;
        }

        private static bool IsHardAlphaCut(
            byte insideAlpha,
            byte outsideAlpha)
        {
            return insideAlpha <= VisibleThreshold &&
                   outsideAlpha >= 64;
        }

        private static int CountLocalOverlap(
            ImageData first,
            ImageData second,
            JointCheck check)
        {
            float centerX = check.normalizedTopPoint.x * ExpectedWidth;
            float centerYFromTop = check.normalizedTopPoint.y * ExpectedHeight;
            float centerY = ExpectedHeight - centerYFromTop;
            float radiusSquared = check.radiusPixels * check.radiusPixels;
            int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - check.radiusPixels));
            int maxX = Mathf.Min(ExpectedWidth - 1, Mathf.CeilToInt(centerX + check.radiusPixels));
            int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - check.radiusPixels));
            int maxY = Mathf.Min(ExpectedHeight - 1, Mathf.CeilToInt(centerY + check.radiusPixels));
            int count = 0;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    if (dx * dx + dy * dy > radiusSquared)
                    {
                        continue;
                    }

                    int index = y * ExpectedWidth + x;
                    if (first.pixels[index].a > VisibleThreshold &&
                        second.pixels[index].a > VisibleThreshold)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static void MeasureCoverage(
            ImageData master,
            IEnumerable<ImageData> layers,
            out float coverage,
            out float leakage)
        {
            bool[] union = new bool[master.pixels.Length];
            foreach (ImageData layer in layers)
            {
                for (int i = 0; i < union.Length; i++)
                {
                    union[i] |= layer.pixels[i].a > VisibleThreshold;
                }
            }

            int masterVisible = 0;
            int covered = 0;
            int leaked = 0;
            int masterTransparent = 0;
            for (int i = 0; i < union.Length; i++)
            {
                bool sourceVisible = master.pixels[i].a > VisibleThreshold;
                if (sourceVisible)
                {
                    masterVisible++;
                    if (union[i])
                    {
                        covered++;
                    }
                }
                else
                {
                    masterTransparent++;
                    if (union[i])
                    {
                        leaked++;
                    }
                }
            }

            coverage = masterVisible == 0 ? 0f : (float)covered / masterVisible;
            leakage = masterTransparent == 0 ? 0f : (float)leaked / masterTransparent;
        }

        private static int ResolveMinimumVisiblePixels(string contractPath)
        {
            if (contractPath.StartsWith("FX/", StringComparison.Ordinal))
            {
                return 20;
            }

            if (contractPath.StartsWith("Face/", StringComparison.Ordinal))
            {
                return contractPath.Contains("Iris", StringComparison.Ordinal) ? 15 : 40;
            }

            if (contractPath.Contains("Ear", StringComparison.Ordinal) ||
                contractPath.Contains("Hand", StringComparison.Ordinal))
            {
                return 150;
            }

            return 500;
        }

        private static int CountVisible(IReadOnlyList<Color32> pixels)
        {
            int count = 0;
            for (int i = 0; i < pixels.Count; i++)
            {
                if (pixels[i].a > VisibleThreshold)
                {
                    count++;
                }
            }

            return count;
        }

        private static void GetPixelBounds(
            Rect region,
            out int minX,
            out int maxX,
            out int minY,
            out int maxY)
        {
            minX = Mathf.Clamp(
                Mathf.FloorToInt(region.xMin * ExpectedWidth),
                0,
                ExpectedWidth - 1);
            maxX = Mathf.Clamp(
                Mathf.CeilToInt(region.xMax * ExpectedWidth) - 1,
                0,
                ExpectedWidth - 1);
            minY = Mathf.Clamp(
                Mathf.FloorToInt(
                    ExpectedHeight -
                    region.yMax * ExpectedHeight),
                0,
                ExpectedHeight - 1);
            maxY = Mathf.Clamp(
                Mathf.CeilToInt(
                    ExpectedHeight -
                    region.yMin * ExpectedHeight) - 1,
                0,
                ExpectedHeight - 1);
        }

        private static ImageData LoadImage(string assetPath)
        {
            string absolutePath = ToAbsolutePath(assetPath);
            if (!File.Exists(absolutePath))
            {
                return null;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(absolutePath);
                Texture2D texture = new(2, 2, TextureFormat.RGBA32, false, false);
                if (!texture.LoadImage(bytes, false))
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                    return null;
                }

                ImageData data = new()
                {
                    width = texture.width,
                    height = texture.height,
                    pixels = texture.GetPixels32()
                };
                UnityEngine.Object.DestroyImmediate(texture);
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Patch 4 validator could not read " + assetPath + ": " +
                    exception.Message);
                return null;
            }
        }

        private static string LayerPath(string contractPath)
        {
            return Patch4MaskDrivenLayerBaker.LayerRoot + "/" +
                   contractPath.Replace('/', '_') + ".png";
        }

        private static void WriteReport(
            bool passed,
            float coverage,
            float leakage,
            IReadOnlyCollection<string> errors,
            IReadOnlyCollection<string> warnings,
            IReadOnlyCollection<string> jointResults)
        {
            string ArrayJson(IEnumerable<string> values)
            {
                List<string> escaped = new();
                foreach (string value in values)
                {
                    escaped.Add("\"" + Escape(value) + "\"");
                }

                return "[\n    " + string.Join(",\n    ", escaped) + "\n  ]";
            }

            string json =
                "{\n" +
                "  \"schemaVersion\": 1,\n" +
                "  \"passedTechnicalChecks\": " + passed.ToString().ToLowerInvariant() + ",\n" +
                "  \"humanArtApprovalStillRequired\": true,\n" +
                "  \"activationAllowed\": false,\n" +
                "  \"masterCoverage\": " + coverage.ToString("0.000000", CultureInfo.InvariantCulture) + ",\n" +
                "  \"alphaLeakage\": " + leakage.ToString("0.000000", CultureInfo.InvariantCulture) + ",\n" +
                "  \"jointOverlap\": " + ArrayJson(jointResults) + ",\n" +
                "  \"errors\": " + ArrayJson(errors) + ",\n" +
                "  \"warnings\": " + ArrayJson(warnings) + "\n" +
                "}\n";

            string absolute = ToAbsolutePath(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, json);
        }

        private static string FormatPercent(float value)
        {
            return (value * 100f).ToString("0.00", CultureInfo.InvariantCulture) + "%";
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
