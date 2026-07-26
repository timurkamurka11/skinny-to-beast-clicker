using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinnyToBeast.UI
{
    /// <summary>
    /// Guarantees a working MainMenuBGM AudioSource.
    /// Uses an authored Resources clip when present and otherwise creates a
    /// lightweight seamless synth/gym ambience loop so the menu is never silent.
    /// </summary>
    [DefaultExecutionOrder(-950)]
    internal sealed class MainMenuBgmBootstrap : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string ObjectName = "MainMenuBGM";
        private const string MusicEnabledKey = "settings.music";
        private const string MusicVolumeKey = "settings.music.volume";
        private const int SampleRate = 44100;
        private const float DefaultVolume = 0.12f;

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
        private static void EnsureCurrentScene()
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
                Object.FindFirstObjectByType<MainMenuBgmBootstrap>();
            if (existing != null)
            {
                instance = existing;
                return;
            }

            GameObject named = GameObject.Find(ObjectName);
            GameObject host = named != null
                ? named
                : new GameObject(ObjectName);
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
            source.priority = 128;
            source.ignoreListenerPause = true;
            source.bypassEffects = false;
            source.bypassListenerEffects = true;
            source.bypassReverbZones = true;

            AudioClip authored = LoadAuthoredClip();
            if (authored != null)
            {
                source.clip = authored;
            }
            else
            {
                generatedClip = CreateFallbackLoop();
                source.clip = generatedClip;
                Debug.LogWarning(
                    "No authored main-menu music was found in Resources. " +
                    "Using the generated fallback loop until a real track is supplied.",
                    this);
            }

            RepairStoredVolume();
            ApplySettings();
        }

        private void Update()
        {
            ApplySettings();
        }

        private static AudioClip LoadAuthoredClip()
        {
            string[] paths =
            {
                "Audio/Music/MainMenu",
                "Audio/MainMenuBGM",
                "Audio/MainMenu"
            };
            for (int i = 0; i < paths.Length; i++)
            {
                AudioClip clip = Resources.Load<AudioClip>(paths[i]);
                if (clip != null)
                {
                    return clip;
                }
            }
            return null;
        }

        private static void RepairStoredVolume()
        {
            bool enabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) != 0;
            float volume = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume);
            if (enabled && volume <= 0.001f)
            {
                PlayerPrefs.SetFloat(MusicVolumeKey, DefaultVolume);
                PlayerPrefs.Save();
            }
        }

        private void ApplySettings()
        {
            if (source == null || source.clip == null)
            {
                return;
            }

            bool enabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) != 0;
            float volume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume));

            AudioListener.pause = false;
            AudioListener.volume = 1f;
            source.mute = !enabled;
            source.volume = enabled
                ? Mathf.Max(0.015f, volume)
                : 0f;

            if (enabled && !source.isPlaying)
            {
                source.Play();
            }
            else if (!enabled && source.isPlaying)
            {
                source.Pause();
            }
        }

        private static AudioClip CreateFallbackLoop()
        {
            const float duration = 8f;
            int sampleCount = Mathf.RoundToInt(duration * SampleRate);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float beat = t * 1.75f;
                float phase = beat - Mathf.Floor(beat);

                float bassEnvelope = Mathf.Exp(-phase * 7.5f);
                float bassNote = Mathf.Sin(2f * Mathf.PI * 55f * t) *
                                 bassEnvelope * 0.16f;

                float pad =
                    Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.035f +
                    Mathf.Sin(2f * Mathf.PI * 164.81f * t) * 0.025f +
                    Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.018f;

                float pulse = phase < 0.04f
                    ? (1f - phase / 0.04f) *
                      Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.028f
                    : 0f;

                float fadeIn = Mathf.Clamp01(t / 0.35f);
                float fadeOut = Mathf.Clamp01((duration - t) / 0.35f);
                float loopEnvelope = Mathf.Min(fadeIn, fadeOut);
                samples[i] = Mathf.Clamp(
                    (bassNote + pad + pulse) * loopEnvelope,
                    -0.75f,
                    0.75f);
            }

            AudioClip clip = AudioClip.Create(
                "MainMenu_GeneratedFallback",
                sampleCount,
                1,
                SampleRate,
                false);
            clip.SetData(samples, 0);
            return clip;
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
