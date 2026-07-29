using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Repository-owned fallback for the approved Patch 4 neutral character.
    /// The compact indexed preview is expanded locally by Unity, so the art
    /// pipeline no longer depends on expiring Adobe download URLs.
    /// </summary>
    internal static class Patch4EmbeddedArtSource
    {
        private const string Encoded =
            "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";

        public static Texture2D CreateTexture()
        {
            byte[] blob = Convert.FromBase64String(Encoded);
            if (blob.Length < 265 || blob[0] != (byte)'F' || blob[1] != (byte)'4' ||
                blob[2] != (byte)'G' || blob[3] != (byte)'1')
            {
                throw new InvalidDataException("Patch 4 embedded art header is invalid.");
            }

            int width = BitConverter.ToUInt16(blob, 4);
            int height = BitConverter.ToUInt16(blob, 6);
            int paletteCount = blob[8];
            int paletteOffset = 9;
            int dataOffset = paletteOffset + paletteCount * 4;

            Color32[] palette = new Color32[paletteCount];
            for (int i = 0; i < paletteCount; i++)
            {
                int offset = paletteOffset + i * 4;
                palette[i] = new Color32(
                    blob[offset], blob[offset + 1], blob[offset + 2], blob[offset + 3]);
            }

            byte[] indices;
            using (MemoryStream input = new MemoryStream(blob, dataOffset, blob.Length - dataOffset))
            using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
            using (MemoryStream output = new MemoryStream(width * height))
            {
                gzip.CopyTo(output);
                indices = output.ToArray();
            }

            if (indices.Length != width * height)
            {
                throw new InvalidDataException("Patch 4 embedded art pixel count is invalid.");
            }

            Color32[] pixels = new Color32[indices.Length];
            for (int topY = 0; topY < height; topY++)
            {
                int sourceRow = topY * width;
                int destinationRow = (height - 1 - topY) * width;
                for (int x = 0; x < width; x++)
                {
                    int paletteIndex = indices[sourceRow + x];
                    pixels[destinationRow + x] =
                        paletteIndex < palette.Length ? palette[paletteIndex] : default;
                }
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                name = "FatMan_EmbeddedRepositorySource",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }
    }
}
