using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Self-contained entry and gameplay soundscape. The clips are generated
    /// once at runtime, so a fresh checkout has audible feedback even before
    /// optional authored audio assets are added.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-850)]
    public sealed class GameplayAudioController : MonoBehaviour
    {
        private const string SfxEnabledKey = "settings.sfx";
        private const string SfxVolumeKey = "settings.sfx.volume";
        private const string PersistentListenerName =
            "SkinnyToBeast.PersistentAudioListener";
        private const int SampleRate = 44100;

        private static AudioListener persistentListener;

        private readonly List<AudioClip> ownedClips = new();
        private AudioSource oneShotSource;
        private AudioSource ambientSource;
        private AudioClip footstepClip;
        private AudioClip entryClip;
        private AudioClip doorClip;
        private AudioClip roomRevealClip;
        private AudioClip tapClip;
        private AudioClip upgradeClip;
        private AudioClip stageClip;
        private AudioClip ambienceClip;
        private bool configured;
        private bool readyLogged;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            persistentListener = null;
        }

        public void Configure(bool startRoomAmbience)
        {
            EnsurePersistentListener();
            EnsureSources();
            EnsureClips();
            configured = true;
            if (!readyLogged)
            {
                readyLogged = true;
                Debug.Log(
                    $"Gameplay audio ready: {ownedClips.Count} generated " +
                    "clips and one persistent listener.",
                    this);
            }

            if (startRoomAmbience)
            {
                StartRoomAmbience();
            }
        }

        public void PlayEntryStart()
        {
            Play(entryClip, 0.62f, 1f);
        }

        public void PlayFootstep(int stepIndex)
        {
            float pitch = (stepIndex & 1) == 0 ? 0.94f : 1.06f;
            Play(footstepClip, 0.72f, pitch);
        }

        public void PlayDoorOpen()
        {
            Play(doorClip, 0.78f, 1f);
        }

        public void PlayRoomReveal()
        {
            Play(roomRevealClip, 0.68f, 1f);
        }

        public void PlayTap(int chain)
        {
            float pitch = Mathf.Clamp(
                0.94f + Mathf.Min(8, Mathf.Max(0, chain)) * 0.018f,
                0.94f,
                1.10f);
            Play(tapClip, 0.82f, pitch);
        }

        public void PlayUpgrade()
        {
            Play(upgradeClip, 0.76f, 1f);
        }

        public void PlayStageChange()
        {
            Play(stageClip, 0.84f, 1f);
        }

        public void StartRoomAmbience()
        {
            if (!configured)
            {
                Configure(false);
            }

            if (ambientSource == null || ambienceClip == null)
            {
                return;
            }

            ambientSource.clip = ambienceClip;
            ambientSource.loop = true;
            ambientSource.volume = ResolveSfxVolume() * 0.12f;
            ambientSource.mute = !SfxEnabled();
            if (!ambientSource.mute && !ambientSource.isPlaying)
            {
                ambientSource.Play();
            }
        }

        private void Awake()
        {
            Configure(false);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Update()
        {
            if (ambientSource == null)
            {
                return;
            }

            bool enabled = SfxEnabled();
            ambientSource.mute = !enabled;
            ambientSource.volume = ResolveSfxVolume() * 0.12f;
            if (enabled &&
                ambientSource.clip != null &&
                !ambientSource.isPlaying)
            {
                ambientSource.Play();
            }
        }

        private void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            EnsurePersistentListener();
        }

        private void EnsureSources()
        {
            if (oneShotSource == null)
            {
                oneShotSource = gameObject.AddComponent<AudioSource>();
                ConfigureSource(oneShotSource, 8);
            }

            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ConfigureSource(ambientSource, 64);
                ambientSource.loop = true;
            }
        }

        private static void ConfigureSource(
            AudioSource source,
            int priority)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.priority = priority;
            source.volume = 1f;
            source.ignoreListenerPause = true;
            source.bypassEffects = true;
            source.bypassListenerEffects = true;
            source.bypassReverbZones = true;
        }

        private void EnsureClips()
        {
            if (footstepClip != null)
            {
                return;
            }

            footstepClip = CreateClip(
                "Gameplay_Footstep",
                0.16f,
                SampleFootstep);
            entryClip = CreateClip(
                "Gameplay_EntryLatch",
                0.28f,
                SampleEntryLatch);
            doorClip = CreateClip(
                "Gameplay_DoorOpen",
                0.78f,
                SampleDoor);
            roomRevealClip = CreateClip(
                "Gameplay_RoomReveal",
                0.48f,
                SampleRoomReveal);
            tapClip = CreateClip(
                "Gameplay_WeightImpact",
                0.13f,
                SampleTap);
            upgradeClip = CreateClip(
                "Gameplay_Upgrade",
                0.66f,
                SampleUpgrade);
            stageClip = CreateClip(
                "Gameplay_StageChange",
                0.92f,
                SampleStage);
            ambienceClip = CreateClip(
                "Gameplay_RoomAmbience",
                4f,
                SampleAmbience);
        }

        private AudioClip CreateClip(
            string clipName,
            float duration,
            Func<float, int, float> sampler)
        {
            int sampleCount =
                Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                samples[i] = Mathf.Clamp(
                    sampler(t, i),
                    -0.98f,
                    0.98f);
            }

            AudioClip clip = AudioClip.Create(
                clipName,
                sampleCount,
                1,
                SampleRate,
                false);
            clip.SetData(samples, 0);
            ownedClips.Add(clip);
            return clip;
        }

        private void Play(
            AudioClip clip,
            float volume,
            float pitch)
        {
            if (!configured)
            {
                Configure(false);
            }

            if (!SfxEnabled() ||
                oneShotSource == null ||
                clip == null)
            {
                return;
            }

            EnsurePersistentListener();
            oneShotSource.pitch = Mathf.Clamp(pitch, 0.6f, 1.5f);
            oneShotSource.PlayOneShot(
                clip,
                Mathf.Clamp01(volume) * ResolveSfxVolume());
        }

        private static bool SfxEnabled()
        {
            return PlayerPrefs.GetInt(SfxEnabledKey, 1) != 0;
        }

        private static float ResolveSfxVolume()
        {
            return Mathf.Clamp01(
                PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
        }

        private static void EnsurePersistentListener()
        {
            AudioListener[] listeners =
                UnityEngine.Object.FindObjectsByType<AudioListener>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (persistentListener == null)
            {
                for (int i = 0; i < listeners.Length; i++)
                {
                    AudioListener candidate = listeners[i];
                    if (candidate != null &&
                        candidate.gameObject.name ==
                        PersistentListenerName)
                    {
                        persistentListener = candidate;
                        break;
                    }
                }
            }

            if (persistentListener == null)
            {
                GameObject listenerHost =
                    new GameObject(PersistentListenerName);
                persistentListener =
                    listenerHost.AddComponent<AudioListener>();
                UnityEngine.Object.DontDestroyOnLoad(listenerHost);
            }

            persistentListener.enabled = true;
            AudioListener.volume = 1f;
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener candidate = listeners[i];
                if (candidate != null &&
                    candidate != persistentListener &&
                    candidate.enabled)
                {
                    candidate.enabled = false;
                }
            }
        }

        private static float SampleFootstep(float t, int index)
        {
            float envelope = Mathf.Exp(-t * 24f);
            float thump =
                Mathf.Sin(2f * Mathf.PI * 76f * t) * 0.72f +
                Mathf.Sin(2f * Mathf.PI * 132f * t) * 0.20f;
            float grit = HashNoise(index, 17) * 0.16f;
            return (thump + grit) * envelope;
        }

        private static float SampleEntryLatch(float t, int index)
        {
            float strike = Mathf.Exp(-t * 30f) *
                           Mathf.Sin(2f * Mathf.PI * 410f * t) *
                           0.54f;
            float metal = Mathf.Exp(-t * 8f) *
                          Mathf.Sin(2f * Mathf.PI * 930f * t) *
                          0.22f;
            return strike + metal +
                   HashNoise(index, 29) * Mathf.Exp(-t * 18f) * 0.06f;
        }

        private static float SampleDoor(float t, int index)
        {
            float progress = Mathf.Clamp01(t / 0.78f);
            float envelope = Mathf.Sin(progress * Mathf.PI);
            float frequency =
                58f + Mathf.Sin(t * 17f) * 13f;
            float creak =
                Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.46f;
            float wood =
                HashNoise(index, 43) * 0.17f;
            float closeThump =
                Mathf.Exp(-Mathf.Abs(t - 0.69f) * 75f) *
                Mathf.Sin(2f * Mathf.PI * 82f * t) *
                0.52f;
            return (creak + wood) * envelope + closeThump;
        }

        private static float SampleRoomReveal(float t, int index)
        {
            float progress = Mathf.Clamp01(t / 0.48f);
            float envelope =
                Mathf.Sin(progress * Mathf.PI) * 0.48f;
            float sweepFrequency = Mathf.Lerp(180f, 760f, progress);
            float sweep =
                Mathf.Sin(2f * Mathf.PI * sweepFrequency * t);
            float air = HashNoise(index, 71) * 0.20f;
            return (sweep * 0.68f + air) * envelope;
        }

        private static float SampleTap(float t, int index)
        {
            float envelope = Mathf.Exp(-t * 28f);
            float body =
                Mathf.Sin(2f * Mathf.PI * 88f * t) * 0.78f +
                Mathf.Sin(2f * Mathf.PI * 176f * t) * 0.22f;
            float click =
                HashNoise(index, 97) *
                Mathf.Exp(-t * 68f) *
                0.24f;
            return body * envelope + click;
        }

        private static float SampleUpgrade(float t, int index)
        {
            float first = Bell(t, 0f, 392f, 0.28f);
            float second = Bell(t, 0.14f, 523.25f, 0.30f);
            float third = Bell(t, 0.30f, 659.25f, 0.32f);
            return first * 0.44f +
                   second * 0.52f +
                   third * 0.58f;
        }

        private static float SampleStage(float t, int index)
        {
            float rise = Mathf.Clamp01(t / 0.92f);
            float envelope =
                Mathf.Sin(rise * Mathf.PI) * 0.54f;
            float frequency = Mathf.Lerp(110f, 620f, rise * rise);
            float core =
                Mathf.Sin(2f * Mathf.PI * frequency * t);
            float octave =
                Mathf.Sin(2f * Mathf.PI * frequency * 2f * t) *
                0.24f;
            return (core + octave) * envelope +
                   Bell(t, 0.56f, 880f, 0.32f) * 0.38f;
        }

        private static float SampleAmbience(float t, int index)
        {
            float hum =
                Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.34f +
                Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.11f;
            float roomTone =
                Mathf.Sin(2f * Mathf.PI * 2f * t) * 0.05f;
            return hum + roomTone;
        }

        private static float Bell(
            float time,
            float start,
            float frequency,
            float duration)
        {
            float local = time - start;
            if (local < 0f || local > duration)
            {
                return 0f;
            }

            float envelope = Mathf.Exp(-local * 7.5f) *
                             Mathf.Sin(
                                 Mathf.Clamp01(local / duration) *
                                 Mathf.PI);
            return Mathf.Sin(
                       2f * Mathf.PI * frequency * local) *
                   envelope;
        }

        private static float HashNoise(int index, int seed)
        {
            uint value = unchecked(
                (uint)index +
                (uint)seed * 374761393u);
            value = (value ^ (value >> 13)) * 1274126177u;
            value ^= value >> 16;
            return (value / (float)uint.MaxValue) * 2f - 1f;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            for (int i = 0; i < ownedClips.Count; i++)
            {
                if (ownedClips[i] != null)
                {
                    Destroy(ownedClips[i]);
                }
            }

            ownedClips.Clear();
        }
    }
}
