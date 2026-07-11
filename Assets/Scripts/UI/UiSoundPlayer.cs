using System.Collections;
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
        private const string BalanceMigrationKey = "settings.audio.balance.v3";
        private const float SampleBoost = 3.6f;

        private static UiSoundPlayer instance;
        private readonly Dictionary<string, AudioClip> clips = new();
        private AudioSource source;
        private Coroutine duckRoutine;
        private int duckGeneration;

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
            ApplyBalanceMigration();
            AudioListener.volume = 1f;

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.priority = 0;
            source.volume = 1f;
            source.ignoreListenerPause = true;
            source.bypassEffects = true;
            source.bypassListenerEffects = true;
            source.bypassReverbZones = true;

            LoadAll();
        }

        private static void ApplyBalanceMigration()
        {
            if (PlayerPrefs.GetInt(BalanceMigrationKey, 0) == 1) return;

            PlayerPrefs.SetInt(SfxEnabledKey, 1);
            PlayerPrefs.SetFloat(SfxVolumeKey, 1f);
            PlayerPrefs.SetFloat("settings.music.volume", 0.12f);
            PlayerPrefs.SetInt(BalanceMigrationKey, 1);
            PlayerPrefs.Save();
        }

        private void LoadAll()
        {
            LoadClip("Open");
            LoadClip("Close");
            LoadClip("Back");
            LoadClip("Confirm");
            LoadClip("ToggleOn");
            LoadClip("ToggleOff");
        }

        private void LoadClip(string id)
        {
            AudioClip original = Resources.Load<AudioClip>($"Audio/UI/{id}");
            if (original == null)
            {
                Debug.LogWarning($"UI sound missing: Audio/UI/{id}");
                return;
            }

            AudioClip amplified = CreateAmplifiedCopy(original, SampleBoost);
            clips[id] = amplified != null ? amplified : original;
        }

        private static AudioClip CreateAmplifiedCopy(AudioClip original, float boost)
        {
            if (original == null) return null;

            try
            {
                original.LoadAudioData();
                float[] samples = new float[original.samples * original.channels];
                if (!original.GetData(samples, 0))
                {
                    return null;
                }

                for (int i = 0; i < samples.Length; i++)
                {
                    // System.Math.Tanh is available in Unity's C# runtime; Mathf has no Tanh method.
                    samples[i] = (float)System.Math.Tanh(samples[i] * boost);
                }

                AudioClip amplified = AudioClip.Create(
                    original.name + "_Amplified",
                    original.samples,
                    original.channels,
                    original.frequency,
                    false);

                amplified.SetData(samples, 0);
                return amplified;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Could not amplify UI sound '{original.name}': {exception.Message}");
                return null;
            }
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
            player?.PlayInternal(id, force);
        }

        private void PlayInternal(string id, bool force)
        {
            if (!force && PlayerPrefs.GetInt(SfxEnabledKey, 1) == 0) return;

            if (!clips.TryGetValue(id, out AudioClip clip) || clip == null)
            {
                LoadClip(id);
                if (!clips.TryGetValue(id, out clip) || clip == null) return;
            }

            float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
            source.PlayOneShot(clip, volume);
            DuckMenuBgm(Mathf.Clamp(clip.length + 0.10f, 0.25f, 1.4f));
        }

        private void DuckMenuBgm(float duration)
        {
            AudioSource bgm = FindMenuBgm();
            if (bgm == null) return;

            duckGeneration++;
            if (duckRoutine != null)
            {
                StopCoroutine(duckRoutine);
            }

            duckRoutine = StartCoroutine(DuckRoutine(bgm, duration, duckGeneration));
        }

        private IEnumerator DuckRoutine(AudioSource bgm, float duration, int generation)
        {
            float targetVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("settings.music.volume", 0.12f));
            bgm.volume = targetVolume * 0.08f;

            yield return new WaitForSecondsRealtime(duration);

            if (generation == duckGeneration && bgm != null)
            {
                bgm.volume = targetVolume;
            }

            duckRoutine = null;
        }

        private static AudioSource FindMenuBgm()
        {
            GameObject named = GameObject.Find("MainMenuBGM");
            if (named != null && named.TryGetComponent(out AudioSource source))
            {
                return source;
            }

            AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (AudioSource candidate in sources)
            {
                if (candidate != null && candidate.loop && candidate.clip != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            foreach (AudioClip clip in clips.Values)
            {
                if (clip != null && clip.name.EndsWith("_Amplified"))
                {
                    Destroy(clip);
                }
            }

            clips.Clear();
            if (instance == this) instance = null;
        }
    }
}
