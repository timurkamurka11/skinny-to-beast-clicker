using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinnyToBeast.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    internal sealed class UiSoundPlayer : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string SfxEnabledKey = "settings.sfx";
        private const string SfxVolumeKey = "settings.sfx.volume";
        private const float SfxBoost = 1.55f;

        private static UiSoundPlayer instance;
        private readonly Dictionary<string, AudioClip> clips = new();
        private AudioSource source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInitialScene()
        {
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (scene.name != MainMenuSceneName) return;
            EnsureInstance();
        }

        private static UiSoundPlayer EnsureInstance()
        {
            if (instance != null) return instance;
            UiSoundPlayer existing = Object.FindFirstObjectByType<UiSoundPlayer>();
            if (existing != null)
            {
                instance = existing;
                return existing;
            }

            GameObject host = new GameObject("UISoundPlayer");
            instance = host.AddComponent<UiSoundPlayer>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.priority = 16;
            source.volume = 1f;
            LoadAll();
        }

        private void LoadAll()
        {
            clips["Open"] = Resources.Load<AudioClip>("Audio/UI/Open");
            clips["Close"] = Resources.Load<AudioClip>("Audio/UI/Close");
            clips["Back"] = Resources.Load<AudioClip>("Audio/UI/Back");
            clips["Confirm"] = Resources.Load<AudioClip>("Audio/UI/Confirm");
            clips["ToggleOn"] = Resources.Load<AudioClip>("Audio/UI/ToggleOn");
            clips["ToggleOff"] = Resources.Load<AudioClip>("Audio/UI/ToggleOff");
        }

        public static void PlayOpen() => Play("Open");
        public static void PlayClose() => Play("Close");
        public static void PlayBack() => Play("Back");
        public static void PlayConfirm() => Play("Confirm");
        public static void PlayToggleOn(bool force = false) => Play("ToggleOn", force);
        public static void PlayToggleOff(bool force = false) => Play("ToggleOff", force);

        private static void Play(string id, bool force = false)
        {
            UiSoundPlayer player = EnsureInstance();
            if (player == null) return;
            player.PlayInternal(id, force);
        }

        private void PlayInternal(string id, bool force)
        {
            if (!force && PlayerPrefs.GetInt(SfxEnabledKey, 1) == 0) return;
            if (!clips.TryGetValue(id, out AudioClip clip) || clip == null)
            {
                Debug.LogWarning($"UI sound missing: {id}");
                return;
            }

            float volume = Mathf.Clamp(PlayerPrefs.GetFloat(SfxVolumeKey, 0.95f) * SfxBoost, 0f, 1.6f);
            source.PlayOneShot(clip, volume);
        }
    }
}
