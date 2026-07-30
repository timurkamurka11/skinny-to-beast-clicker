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

            // Unity 6 stores the default maximum size in platform settings.
            TextureImporterPlatformSettings defaultSettings =
                importer.GetDefaultPlatformTextureSettings();
            defaultSettings.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(defaultSettings);

            // Unity 6000.3 no longer exposes spriteAlignment directly on
            // TextureImporter. Read and write the sprite-specific fields through
            // TextureImporterSettings instead.
            TextureImporterSettings spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            spriteSettings.spriteMode = (int)SpriteImportMode.Single;
            spriteSettings.spritePixelsPerUnit = 100f;
            spriteSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            spriteSettings.spritePivot = pivot;
            // Every Patch 4 PNG keeps the full 1024 x 1536 canvas. A Tight
            // sprite mesh changes its outer UV rectangle to the opaque crop;
            // feeding that crop to the subdivided Canvas grid stretches one
            // body fragment over the whole character rectangle.
            spriteSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(spriteSettings);
        }
    }
}
