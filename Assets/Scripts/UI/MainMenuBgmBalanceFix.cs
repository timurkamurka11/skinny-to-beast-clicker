using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinnyToBeast.UI
{
    [DefaultExecutionOrder(-850)]
    internal sealed class MainMenuBgmBalanceFix : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string MusicEnabledKey = "settings.music";
        private const string MusicVolumeKey = "settings.music.volume";
        private const float DefaultMenuBgmVolume = 0.28f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInitial()
        {
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (scene.name != MainMenuSceneName) return;
            if (Object.FindFirstObjectByType<MainMenuBgmBalanceFix>() != null) return;
            new GameObject("MainMenuBgmBalanceFix").AddComponent<MainMenuBgmBalanceFix>();
        }

        private void Start()
        {
            if (!PlayerPrefs.HasKey(MusicVolumeKey))
            {
                PlayerPrefs.SetFloat(MusicVolumeKey, DefaultMenuBgmVolume);
            }
            else
            {
                float current = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMenuBgmVolume);
                if (current > 0.40f)
                {
                    PlayerPrefs.SetFloat(MusicVolumeKey, DefaultMenuBgmVolume);
                }
            }

            if (!PlayerPrefs.HasKey(MusicEnabledKey))
            {
                PlayerPrefs.SetInt(MusicEnabledKey, 1);
            }

            PlayerPrefs.Save();
            ApplyToCurrentSource();
        }

        private static void ApplyToCurrentSource()
        {
            AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (AudioSource source in sources)
            {
                if (source == null || source.clip == null || !source.loop) continue;
                source.volume = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1
                    ? Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMenuBgmVolume))
                    : 0f;
                source.mute = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 0;
                break;
            }
        }
    }
}
