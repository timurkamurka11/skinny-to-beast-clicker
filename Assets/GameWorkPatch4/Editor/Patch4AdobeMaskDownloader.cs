using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Restores the approved Patch 4 neutral master and deterministic draft masks
    /// from repository-owned data. The menu name is preserved for compatibility,
    /// but no network or expiring Adobe URL is used anymore.
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
        public const string RepositoryMasterPath =
            ArtRoot + "/FatMan_NeutralFront_Master.png";
        public const string ExpectedMasterSha256 =
            "7b151f1ded93f3852bc8a7218ab26f94298b7f822094304bbcea9c076cad72a3";

        private const int MasterWidth = 1024;
        private const int MasterHeight = 1536;

        private readonly struct MaskSpec
        {
            public readonly string fileName;
            public readonly Rect[] regions;

            public MaskSpec(string fileName, params Rect[] regions)
            {
                this.fileName = fileName;
                this.regions = regions ?? Array.Empty<Rect>();
            }
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Art/Download Adobe Sources")]
        public static void DownloadAdobeSources()
        {
            RestoreRepositorySources();
        }

        public static bool RestoreRepositorySources()
        {
            EnsureFolder(SourceRoot);
            EnsureFolder(ReferenceRoot);
            EnsureFolder(DownloadedMaskRoot);

            Texture2D master = null;
            try
            {
                EditorUtility.DisplayProgressBar(
                    "GameWork Patch 4.0",
                    "Restoring repository-owned character master",
                    0.1f);

                byte[] masterBytes = ReadAndValidateRepositoryMaster();
                master = new Texture2D(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false,
                    false)
                {
                    name = "FatMan_NeutralFront_Master",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                if (!master.LoadImage(masterBytes, false))
                {
                    throw new InvalidDataException(
                        "Repository master is not a readable PNG.");
                }

                if (master.width != MasterWidth ||
                    master.height != MasterHeight)
                {
                    throw new InvalidDataException(
                        "Repository master is " + master.width + "x" +
                        master.height + "; expected " + MasterWidth + "x" +
                        MasterHeight + ".");
                }

                WriteBytes(
                    masterBytes,
                    SourceRoot + "/FatMan_NeutralFront_Master.png");
                WriteBytes(
                    masterBytes,
                    ReferenceRoot + "/FatMan_Rigging_Reference.png");

                MaskSpec[] masks = BuildMaskSpecs();
                for (int i = 0; i < masks.Length; i++)
                {
                    EditorUtility.DisplayProgressBar(
                        "GameWork Patch 4.0",
                        "Creating local mask " + masks[i].fileName,
                        0.15f + 0.75f * ((float)i / Mathf.Max(1, masks.Length)));
                    WriteMask(master, masks[i]);
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                Debug.Log(
                    "Patch 4 restored the neutral master, rigging reference and " +
                    masks.Length + " deterministic masks from GitHub-owned data. " +
                    "No Adobe download is required. Run Art/Bake Draft Layer Pack next.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Patch 4 could not restore repository-owned art sources: " +
                    exception);
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (master != null)
                {
                    UnityEngine.Object.DestroyImmediate(master);
                }
            }
        }

        private static MaskSpec[] BuildMaskSpecs()
        {
            return new[]
            {
                new MaskSpec("Mask_Hair.png", R(.38f, .075f, .24f, .105f)),
                new MaskSpec("Mask_FaceBase.png", R(.375f, .085f, .25f, .18f)),
                new MaskSpec("Mask_Eyebrows.png", R(.415f, .142f, .17f, .045f)),
                new MaskSpec("Mask_Nose.png", R(.455f, .155f, .09f, .075f)),
                new MaskSpec(
                    "Mask_Ears.png",
                    R(.365f, .145f, .075f, .10f),
                    R(.56f, .145f, .075f, .10f)),
                new MaskSpec("Mask_Neck.png", R(.39f, .19f, .22f, .115f)),
                new MaskSpec("Mask_UpperClothes.png", R(.19f, .22f, .62f, .315f)),
                new MaskSpec("Mask_LowerClothes.png", R(.255f, .455f, .49f, .315f)),
                new MaskSpec(
                    "Mask_Hands.png",
                    R(.155f, .43f, .22f, .15f),
                    R(.625f, .43f, .22f, .15f)),
                new MaskSpec(
                    "Mask_Shoes.png",
                    R(.225f, .70f, .30f, .13f),
                    R(.465f, .70f, .30f, .13f))
            };
        }

        private static Rect R(float x, float y, float width, float height)
        {
            return new Rect(x, y, width, height);
        }

        private static void WriteMask(Texture2D master, MaskSpec spec)
        {
            Color32[] sourcePixels = master.GetPixels32();
            Color32[] maskPixels = new Color32[sourcePixels.Length];
            int width = master.width;
            int height = master.height;

            for (int y = 0; y < height; y++)
            {
                float topY = 1f - ((y + 0.5f) / height);
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    int index = row + x;
                    if (sourcePixels[index].a < 8)
                    {
                        continue;
                    }

                    float nx = (x + 0.5f) / width;
                    if (ContainsAny(spec.regions, nx, topY))
                    {
                        maskPixels[index] = new Color32(255, 255, 255, 255);
                    }
                }
            }

            Texture2D mask = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false,
                false)
            {
                name = Path.GetFileNameWithoutExtension(spec.fileName),
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            try
            {
                mask.SetPixels32(maskPixels);
                mask.Apply(false, false);
                WriteTexture(mask, DownloadedMaskRoot + "/" + spec.fileName);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mask);
            }
        }

        private static bool ContainsAny(Rect[] regions, float x, float topY)
        {
            for (int i = 0; i < regions.Length; i++)
            {
                if (regions[i].Contains(new Vector2(x, topY)))
                {
                    return true;
                }
            }

            return false;
        }

        private static byte[] ReadAndValidateRepositoryMaster()
        {
            string absolutePath = ToAbsolutePath(RepositoryMasterPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(
                    "Repository master is missing.",
                    absolutePath);
            }

            byte[] bytes = File.ReadAllBytes(absolutePath);
            string actualSha;
            using (SHA256 sha256 = SHA256.Create())
            {
                actualSha = BitConverter.ToString(
                        sha256.ComputeHash(bytes))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }

            if (!string.Equals(
                    actualSha,
                    ExpectedMasterSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Repository master SHA-256 is " + actualSha +
                    "; expected " + ExpectedMasterSha256 + ".");
            }

            return bytes;
        }

        private static void WriteBytes(byte[] bytes, string assetPath)
        {
            string absolutePath = ToAbsolutePath(assetPath);
            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(absolutePath, bytes);
        }

        private static void WriteTexture(Texture2D texture, string assetPath)
        {
            string absolutePath = ToAbsolutePath(assetPath);
            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(
                projectRoot,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
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
