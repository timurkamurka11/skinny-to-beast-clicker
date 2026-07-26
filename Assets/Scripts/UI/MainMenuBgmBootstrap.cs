using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinnyToBeast.UI
{
    /// <summary>
    /// Guarantees an audible looping theme on MainMenu.
    /// An authored Resources clip is preferred. A small original procedural
    /// ambient loop is generated only when the repository has no music asset.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1200)]
    internal sealed class MainMenuBgmBootstrap : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string HostName = "MainMenuBGM";
        private const string MusicEnabledKey = "settings.music";
        private const string MusicVolumeKey = "settings.music.volume";
        private const string AuthoredClipPath = "Audio/Music/MainMenuTheme";
        private const int SampleRate = 44100;
        private const float DefaultVolume = 0.12f;
        private const float LoopDuration = 8f;

        private static MainMenuBgmBootstrap instance;
        private AudioSource source;
        private AudioClip generatedClip;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
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
            if (scene.name != MainMenuSceneName)
            {
                return;
            }

            if (instance != null)
            {
                return;
            }

            MainMenuBgmBootstrap existing =
                UnityEngine.Object.FindFirstObjectByType<
                    MainMenuBgmBootstrap>();
            if (existing != null)
            {
                instance = existing;
                return;
            }

            GameObject named = GameObject.Find(HostName);
            GameObject host = named != null
                ? named
                : new GameObject(HostName);
            instance = host.GetComponent<MainMenuBgmBootstrap>();
            if (instance == null)
            {
                instance = host.AddComponent<MainMenuBgmBootstrap>();
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            source = GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.priority = 32;
            source.ignoreListenerPause = true;
            source.bypassEffects = true;
            source.bypassListenerEffects = true;
            source.bypassReverbZones = true;

            AudioClip authored =
                Resources.Load<AudioClip>(AuthoredClipPath);
            source.clip = authored != null
                ? authored
                : CreateProceduralTheme();
            ApplySettings(forcePlay: true);
        }

        private void Update()
        {
            ApplySettings(forcePlay: false);
        }

        private void ApplySettings(bool forcePlay)
        {
            if (source == null)
            {
                return;
            }

            AudioListener.pause = false;
            AudioListener.volume = 1f;

            bool enabled =
                PlayerPrefs.GetInt(MusicEnabledKey, 1) != 0;
            float volume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume));
            if (enabled && volume <= 0.001f)
            {
                volume = DefaultVolume;
                PlayerPrefs.SetFloat(MusicVolumeKey, volume);
                PlayerPrefs.Save();
            }

            source.mute = !enabled;
            source.volume = enabled ? volume : 0f;
            if (enabled &&
                source.clip != null &&
                (forcePlay || !source.isPlaying))
            {
                source.Play();
            }
            else if (!enabled && source.isPlaying)
            {
                source.Pause();
            }
        }

        private AudioClip CreateProceduralTheme()
        {
            int sampleCount =
                Mathf.CeilToInt(SampleRate * LoopDuration);
            float[] samples = new float[sampleCount * 2];
            for (int sample = 0; sample < sampleCount; sample++)
            {
                float t = sample / (float)SampleRate;
                float phase = t / LoopDuration;
                float pulse =
                    0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * phase * 2f);
                float pad =
                    Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.18f +
                    Mathf.Sin(2f * Mathf.PI * 138.625f * t) * 0.14f +
                    Mathf.Sin(2f * Mathf.PI * 164.75f * t) * 0.11f +
                    Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.05f;
                float shimmer =
                    Mathf.Sin(
                        2f * Mathf.PI * 440f * t +
                        Mathf.Sin(2f * Mathf.PI * phase) * 0.8f) *
                    0.025f;
                float sampleValue =
                    Mathf.Clamp((pad * pulse + shimmer) * 0.42f, -0.7f, 0.7f);
                float pan = 0.12f * Mathf.Sin(2f * Mathf.PI * phase);
                samples[sample * 2] = sampleValue * (1f - pan);
                samples[sample * 2 + 1] = sampleValue * (1f + pan);
            }

            generatedClip = AudioClip.Create(
                "MainMenuTheme_ProceduralFallback",
                sampleCount,
                2,
                SampleRate,
                false);
            generatedClip.SetData(samples, 0);
            return generatedClip;
        }

        private void OnDestroy()
        {
            if (generatedClip != null)
            {
                Destroy(generatedClip);
                generatedClip = null;
            }
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
