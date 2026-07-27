using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Applies deterministic import settings to hand-painted Patch 4 layers.
    /// It only runs inside the isolated Patch 4 art directory.
    /// </summary>
    public sealed class Patch4LayerImportPostprocessor : AssetPostprocessor
    {
        public const string LayerRoot =
            "Assets/GameWorkPatch4/Art/Character/FatMan/Layers/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(
                    LayerRoot,
                    System.StringComparison.Ordinal))
            {
                return;
            }

            string contractPath =
                Patch4LayerPlacement.ContractPathFromAssetPath(assetPath);
            Vector2 pivot =
                Patch4LayerPlacement.ResolvePivotNormalized(contractPath);

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteAlignment = (int)SpriteAlignment.Custom;
            importer.spritePivot = pivot;
        }
    }
}
