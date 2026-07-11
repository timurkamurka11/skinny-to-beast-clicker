using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
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

        private readonly Dictionary<EmbeddedUiSoundId, AudioClip> clips = new();
        private readonly Queue<(EmbeddedUiSoundId id, bool force)> pending = new();

        private AudioSource source;
        private bool loading;

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
            if (scene.name != MainMenuSceneName)
            {
                return;
            }

            EnsureInstance();
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

            if (!loading)
            {
                StartCoroutine(LoadAll());
            }
        }

        private IEnumerator LoadAll()
        {
            loading = true;

            foreach (EmbeddedUiSoundId id in Enum.GetValues(typeof(EmbeddedUiSoundId)))
            {
                byte[] bytes = EmbeddedSettingsAssets.GetSoundBytes(id);
                if (bytes == null || bytes.Length == 0)
                {
                    continue;
                }

                string path = Path.Combine(Application.temporaryCachePath, $"skinny_to_beast_ui_{id}.mp3");
                bool needsWrite = !File.Exists(path) || new FileInfo(path).Length != bytes.Length;
                if (needsWrite)
                {
                    File.WriteAllBytes(path, bytes);
                }

                string uri = new Uri(path).AbsoluteUri;
                using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG);
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"UI sound '{id}' failed to load: {request.error}");
                    continue;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip != null)
                {
                    clip.name = $"UI_{id}";
                    clips[id] = clip;
                }
            }

            loading = false;
            FlushPending();
        }

        private void FlushPending()
        {
            int count = pending.Count;
            for (int i = 0; i < count; i++)
            {
                (EmbeddedUiSoundId id, bool force) item = pending.Dequeue();
                PlayInternal(item.id, item.force);
            }
        }

        public static void PlayOpen() => Play(EmbeddedUiSoundId.Open);
        public static void PlayClose() => Play(EmbeddedUiSoundId.Close);
        public static void PlayBack() => Play(EmbeddedUiSoundId.Back);
        public static void PlayConfirm() => Play(EmbeddedUiSoundId.Confirm);
        public static void PlayToggleOn(bool force = false) => Play(EmbeddedUiSoundId.ToggleOn, force);
        public static void PlayToggleOff(bool force = false) => Play(EmbeddedUiSoundId.ToggleOff, force);

        private static void Play(EmbeddedUiSoundId id, bool force = false)
        {
            UiSoundPlayer player = EnsureInstance();
            if (player == null)
            {
                return;
            }

            player.PlayInternal(id, force);
        }

        private void PlayInternal(EmbeddedUiSoundId id, bool force)
        {
            if (!force && PlayerPrefs.GetInt(SfxEnabledKey, 1) == 0)
            {
                return;
            }

            if (!clips.TryGetValue(id, out AudioClip clip) || clip == null)
            {
                if (loading)
                {
                    pending.Enqueue((id, force));
                }

                return;
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

            foreach (AudioClip clip in clips.Values)
            {
                if (clip != null)
                {
                    Destroy(clip);
                }
            }

            clips.Clear();
        }
    }
}
