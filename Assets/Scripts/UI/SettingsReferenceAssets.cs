using UnityEngine;

namespace SkinnyToBeast.UI
{
    internal enum UiSoundId
    {
        Open,
        Close,
        Back,
        Confirm,
        ToggleOn,
        ToggleOff
    }

    internal static class SettingsReferenceAssets
    {
        private const string PanelResourcePath = "UI/Settings/settings_ref";

        public static Sprite CreatePanelSprite()
        {
            Texture2D texture = Resources.Load<Texture2D>(PanelResourcePath);
            if (texture == null)
            {
                Debug.LogError(
                    "Settings reference image is missing. Expected: " +
                    "Assets/Resources/UI/Settings/settings_ref.jpg"
                );
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect
            );
            sprite.name = "SettingsReferencePanelSprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        public static AudioClip LoadSound(UiSoundId id)
        {
            string path = id switch
            {
                UiSoundId.Open => "Audio/UI/Open",
                UiSoundId.Close => "Audio/UI/Close",
                UiSoundId.Back => "Audio/UI/Back",
                UiSoundId.Confirm => "Audio/UI/Confirm",
                UiSoundId.ToggleOn => "Audio/UI/ToggleOn",
                UiSoundId.ToggleOff => "Audio/UI/ToggleOff",
                _ => null
            };

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"UI sound is missing at Resources/{path}");
            }

            return clip;
        }
    }
}
