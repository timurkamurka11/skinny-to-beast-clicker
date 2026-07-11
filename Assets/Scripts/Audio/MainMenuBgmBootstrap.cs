using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinnyToBeast.Audio
{
    [DisallowMultipleComponent]
    public sealed class MainMenuBgmBootstrap : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string MusicResourcePath = "Audio/MainMenuBGM";
        private const string MusicSettingKey = "settings.music";
        private const string MusicVolumeKey = "settings.music.volume";

        [Header("Optional explicit references")]
        [SerializeField] private AudioClip clipOverride;
        [SerializeField] private AudioSource audioSource;

        [Header("Playback")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

        private static MainMenuBgmBootstrap instance;
        private int cachedMusicSetting = -1;
        private float cachedVolume = -1f;
        private float nextSettingCheckTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneListener()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureAfterInitialSceneLoad()
        {
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (scene.name != MainMenuSceneName)
            {
                return;
            }

            MainMenuBgmBootstrap existing = Object.FindFirstObjectByType<MainMenuBgmBootstrap>();
            if (existing != null)
            {
                instance = existing;
                existing.SetupAndPlay();
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
            SetupAndPlay();
        }

        private void OnEnable()
        {
            if (SceneManager.GetActiveScene().name == MainMenuSceneName)
            {
                SetupAndPlay();
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextSettingCheckTime)
            {
                return;
            }

            nextSettingCheckTime = Time.unscaledTime + 0.2f;
            ApplyMusicSetting(force: false);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public void Configure(AudioClip clip, AudioSource source)
        {
            clipOverride = clip;
            audioSource = source;
            SetupAndPlay();
        }

        private void SetupAndPlay()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            EnsureAudioListener();

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            AudioClip clip = clipOverride;
            if (clip == null)
            {
                clip = audioSource.clip;
            }

            if (clip == null)
            {
                clip = Resources.Load<AudioClip>(MusicResourcePath);
            }

            if (clip == null)
            {
                Debug.LogError(
                    "Main menu BGM was not found. Expected file: " +
                    "Assets/Resources/Audio/MainMenuBGM.ogg"
                );
                return;
            }

            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.playOnAwake = true;
            audioSource.volume = volume;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;
            audioSource.priority = 0;

            ApplyMusicSetting(force: true);

            if (!audioSource.mute && !audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log($"Main menu BGM started: {clip.name}, volume {audioSource.volume:0.00}");
            }
            else if (audioSource.mute)
            {
                Debug.LogWarning(
                    "Main menu BGM is loaded but MUSIC is OFF. " +
                    "Open SETTINGS and switch MUSIC to ON."
                );
            }
        }

        private void EnsureAudioListener()
        {
            AudioListener existingListener = Object.FindFirstObjectByType<AudioListener>();
            if (existingListener != null)
            {
                if (!existingListener.enabled)
                {
                    existingListener.enabled = true;
                }

                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = Object.FindFirstObjectByType<Camera>();
            }

            GameObject listenerHost = mainCamera != null ? mainCamera.gameObject : gameObject;
            listenerHost.AddComponent<AudioListener>();
            Debug.Log($"AudioListener added automatically to '{listenerHost.name}'.");
        }

        private void ApplyMusicSetting(bool force)
        {
            if (audioSource == null)
            {
                return;
            }

            int currentSetting = PlayerPrefs.GetInt(MusicSettingKey, 1);
            float currentVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, volume));

            if (!force && currentSetting == cachedMusicSetting && Mathf.Approximately(currentVolume, cachedVolume))
            {
                return;
            }

            cachedMusicSetting = currentSetting;
            cachedVolume = currentVolume;
            audioSource.mute = currentSetting == 0;
            audioSource.volume = currentVolume;

            if (!audioSource.mute && audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }
}
