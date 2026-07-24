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
        private static Sprite cachedPanelSprite;
        private static bool missingPanelLogged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeCache()
        {
            cachedPanelSprite = null;
            missingPanelLogged = false;
        }

        public static Sprite CreatePanelSprite()
        {
            if (cachedPanelSprite != null)
            {
                return cachedPanelSprite;
            }

            // Unity may import an image in Resources as either a Sprite or a
            // Texture2D depending on its TextureImporter settings.
            Sprite importedSprite = Resources.Load<Sprite>(PanelResourcePath);
            if (importedSprite != null)
            {
                cachedPanelSprite = importedSprite;
                missingPanelLogged = false;
                return cachedPanelSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(PanelResourcePath);
            if (texture == null)
            {
                // An editor import guard fixes the source importer before play.
                // Keep this warning single-shot so a bad local .meta file can
                // never flood the Console every frame and stall START.
                if (!missingPanelLogged)
                {
                    Debug.LogWarning(
                        "Settings reference image is not available yet. " +
                        "The procedural settings background will be used until " +
                        "Assets/Resources/UI/Settings/settings_ref.jpg finishes importing."
                    );
                    missingPanelLogged = true;
                }

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
            cachedPanelSprite = sprite;
            missingPanelLogged = false;
            return cachedPanelSprite;
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
