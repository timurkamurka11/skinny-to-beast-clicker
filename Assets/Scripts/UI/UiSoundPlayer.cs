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
        private const string BalanceMigrationKey = "settings.audio.balance.v2";

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
            ApplyOneTimeBalanceMigration();

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

        private static void ApplyOneTimeBalanceMigration()
        {
            if (PlayerPrefs.GetInt(BalanceMigrationKey, 0) == 1) return;

            PlayerPrefs.SetFloat(SfxVolumeKey, 1f);
            PlayerPrefs.SetFloat("settings.music.volume", 0.18f);
            PlayerPrefs.SetInt(BalanceMigrationKey, 1);
            PlayerPrefs.Save();
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
            player?.PlayInternal(id, force);
        }

        private void PlayInternal(string id, bool force)
        {
            if (!force && PlayerPrefs.GetInt(SfxEnabledKey, 1) == 0) return;

            if (!clips.TryGetValue(id, out AudioClip clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>($"Audio/UI/{id}");
                if (clip == null)
                {
                    Debug.LogWarning($"UI sound missing: Audio/UI/{id}");
                    return;
                }

                clips[id] = clip;
            }

            float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
            float boostedVolume = savedVolume <= 0.001f ? 0f : Mathf.Clamp01(savedVolume * 1.8f);
            source.PlayOneShot(clip, boostedVolume);
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
            float targetVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("settings.music.volume", 0.18f));
            bgm.volume = targetVolume * 0.22f;

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
    }
}
