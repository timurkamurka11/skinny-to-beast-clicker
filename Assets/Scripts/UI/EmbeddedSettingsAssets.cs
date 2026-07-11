using UnityEngine;

namespace SkinnyToBeast.UI
{
    // Compatibility bridge for the reference-driven settings installer.
    internal static class EmbeddedSettingsAssets
    {
        public static Sprite CreatePanelSprite()
        {
            return SettingsReferenceAssets.CreatePanelSprite();
        }
    }
}
