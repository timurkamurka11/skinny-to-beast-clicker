using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Downloads the approved master, valid Adobe selection masks and the
    /// Firefly parts reference into the isolated Patch 4 art directory.
    /// Invalid whole-subject masks are deliberately skipped.
    /// </summary>
    public static class Patch4AdobeMaskDownloader
    {
        public const string ArtRoot =
            "Assets/GameWorkPatch4/Art/Character/FatMan";
        public const string ManifestPath =
            ArtRoot + "/Masks/adobe-mask-manifest.json";
        public const string DownloadedMaskRoot =
            ArtRoot + "/Masks/Downloaded";
        public const string SourceRoot = ArtRoot + "/Source";
        public const string ReferenceRoot = ArtRoot + "/References";

        [Serializable]
        private sealed class Manifest
        {
            public int schemaVersion;
            public string status;
            public SourceEntry source;
            public ReferenceEntry riggingReference;
            public MaskEntry[] masks;
        }

        [Serializable]
        private sealed class SourceEntry
        {
            public string assetId;
            public string fileName;
            public string url;
            public int width;
            public int height;
        }

        [Serializable]
        private sealed class ReferenceEntry
        {
            public string fileName;
            public string url;
            public string usage;
            public string notes;
        }

        [Serializable]
        private sealed class MaskEntry
        {
            public string id;
            public string fileName;
            public string url;
            public bool valid;
            public string fallback;
            public string notes;
            public float[] bbox;
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Art/Download Adobe Sources")]
        public static async void DownloadAdobeSources()
        {
            TextAsset manifestAsset =
                AssetDatabase.LoadAssetAtPath<TextAsset>(ManifestPath);
            if (manifestAsset == null)
            {
                Debug.LogError(
                    "Patch 4 Adobe mask manifest is missing: " + ManifestPath);
                return;
            }

            Manifest manifest = JsonUtility.FromJson<Manifest>(manifestAsset.text);
            if (manifest == null || manifest.schemaVersion != 1)
            {
                Debug.LogError("Patch 4 Adobe mask manifest is invalid.");
                return;
            }

            EnsureFolder(SourceRoot);
            EnsureFolder(ReferenceRoot);
            EnsureFolder(DownloadedMaskRoot);

            List<DownloadSpec> downloads = new();
            if (manifest.source != null)
            {
                downloads.Add(new DownloadSpec(
                    manifest.source.url,
                    SourceRoot + "/" + manifest.source.fileName,
                    "approved neutral master"));
            }

            if (manifest.riggingReference != null)
            {
                downloads.Add(new DownloadSpec(
                    manifest.riggingReference.url,
                    ReferenceRoot + "/" + manifest.riggingReference.fileName,
                    "Adobe rigging reference"));
            }

            if (manifest.masks != null)
            {
                for (int i = 0; i < manifest.masks.Length; i++)
                {
                    MaskEntry mask = manifest.masks[i];
                    if (mask == null || !mask.valid)
                    {
                        continue;
                    }

                    downloads.Add(new DownloadSpec(
                        mask.url,
                        DownloadedMaskRoot + "/" + mask.fileName,
                        "mask " + mask.id));
                }
            }

            int successCount = 0;
            List<string> failures = new();
            try
            {
                for (int i = 0; i < downloads.Count; i++)
                {
                    DownloadSpec spec = downloads[i];
                    EditorUtility.DisplayProgressBar(
                        "GameWork Patch 4.0",
                        "Downloading " + spec.label,
                        downloads.Count == 0 ? 1f : (float)i / downloads.Count);

                    string error = await DownloadFile(spec.url, spec.assetPath);
                    if (string.IsNullOrEmpty(error))
                    {
                        successCount++;
                    }
                    else
                    {
                        failures.Add(spec.label + ": " + error);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            if (failures.Count == 0)
            {
                Debug.Log(
                    $"Patch 4 downloaded {successCount} Adobe source files. " +
                    "Run Art/Bake Draft Layer Pack next.");
            }
            else
            {
                Debug.LogWarning(
                    $"Patch 4 downloaded {successCount} files, but " +
                    $"{failures.Count} failed:\n" + string.Join("\n", failures));
            }
        }

        private readonly struct DownloadSpec
        {
            public readonly string url;
            public readonly string assetPath;
            public readonly string label;

            public DownloadSpec(string url, string assetPath, string label)
            {
                this.url = url;
                this.assetPath = assetPath;
                this.label = label;
            }
        }

        private static async Task<string> DownloadFile(
            string url,
            string assetPath)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return "URL is empty";
            }

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = 45;
            try
            {
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    return request.error + " (HTTP " + request.responseCode + ")";
                }

                byte[] bytes = request.downloadHandler.data;
                if (bytes == null || bytes.Length < 64)
                {
                    return "download returned no usable image data";
                }

                string absolutePath = ToAbsolutePath(assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
                File.WriteAllBytes(absolutePath, bytes);
                return string.Empty;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
            finally
            {
                request.Dispose();
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
