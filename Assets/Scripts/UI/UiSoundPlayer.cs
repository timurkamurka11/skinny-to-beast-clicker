using System;
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

        private static UiSoundPlayer instance;

        private readonly Dictionary<UiSoundId, AudioClip> clips = new();
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
            if (scene.name == MainMenuSceneName)
            {
                EnsureInstance();
            }
        }

        private static UiSoundPlayer EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            UiSoundPlayer existing = UnityEngine.Object.FindFirstObjectByType<UiSoundPlayer>();
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
            source.priority = 32;

            LoadAllSounds();
        }

        private void LoadAllSounds()
        {
            clips.Clear();

            foreach (UiSoundId id in Enum.GetValues(typeof(UiSoundId)))
            {
                AudioClip clip = SettingsReferenceAssets.LoadSound(id);
                if (clip != null)
                {
                    clips[id] = clip;
                }
            }
        }

        public static void PlayOpen() => Play(UiSoundId.Open);
        public static void PlayClose() => Play(UiSoundId.Close);
        public static void PlayBack() => Play(UiSoundId.Back);
        public static void PlayConfirm() => Play(UiSoundId.Confirm);
        public static void PlayToggleOn(bool force = false) => Play(UiSoundId.ToggleOn, force);
        public static void PlayToggleOff(bool force = false) => Play(UiSoundId.ToggleOff, force);

        private static void Play(UiSoundId id, bool force = false)
        {
            UiSoundPlayer player = EnsureInstance();
            player?.PlayInternal(id, force);
        }

        private void PlayInternal(UiSoundId id, bool force)
        {
            if (!force && PlayerPrefs.GetInt(SfxEnabledKey, 1) == 0)
            {
                return;
            }

            if (!clips.TryGetValue(id, out AudioClip clip) || clip == null)
            {
                clip = SettingsReferenceAssets.LoadSound(id);
                if (clip == null)
                {
                    return;
                }

                clips[id] = clip;
            }

            float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f));
            source.PlayOneShot(clip, volume);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
