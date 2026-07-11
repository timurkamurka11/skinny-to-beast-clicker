using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinnyToBeast.Audio
{
    public sealed class MainMenuBgmBootstrap : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string MusicResourcePath = "Audio/MainMenuBGM";
        private const string MusicSettingKey = "settings.music";

        private static MainMenuBgmBootstrap instance;

        private AudioSource audioSource;
        private int cachedMusicSetting = -1;
        private float nextSettingCheckTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != MainMenuSceneName)
            {
                return;
            }

            if (instance != null)
            {
                return;
            }

            GameObject musicObject = new GameObject("MainMenuBGM");
            instance = musicObject.AddComponent<MainMenuBgmBootstrap>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            AudioClip clip = Resources.Load<AudioClip>(MusicResourcePath);
            if (clip == null)
            {
                Debug.LogWarning(
                    "Main menu BGM is missing. Put the loop file at " +
                    "Assets/Resources/Audio/MainMenuBGM.ogg"
                );
                return;
            }

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = 0.55f;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;

            ApplyMusicSetting(force: true);
            audioSource.Play();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextSettingCheckTime)
            {
                return;
            }

            nextSettingCheckTime = Time.unscaledTime + 0.25f;
            ApplyMusicSetting(force: false);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void ApplyMusicSetting(bool force)
        {
            if (audioSource == null)
            {
                return;
            }

            int currentSetting = PlayerPrefs.GetInt(MusicSettingKey, 1);
            if (!force && currentSetting == cachedMusicSetting)
            {
                return;
            }

            cachedMusicSetting = currentSetting;
            audioSource.mute = currentSetting == 0;
        }
    }
}
