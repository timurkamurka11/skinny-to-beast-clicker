using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SkinnyToBeast.UI
{
    [DisallowMultipleComponent]
    internal sealed class SettingsMenuController : MonoBehaviour
    {
        private const string MusicEnabledKey = "settings.music";
        private const string MusicVolumeKey = "settings.music.volume";
        private const string SfxEnabledKey = "settings.sfx";
        private const string SfxVolumeKey = "settings.sfx.volume";
        private const string VoiceEnabledKey = "settings.voice";
        private const string VoiceVolumeKey = "settings.voice.volume";
        private const string VibrationKey = "settings.vibration";
        private const string LanguageKey = "settings.language";
        private const string NotificationsKey = "settings.notifications";

        private GameObject popupRoot;
        private Button backdropButton;
        private Slider musicSlider;
        private Toggle musicToggle;
        private Slider sfxSlider;
        private Toggle sfxToggle;
        private Slider voiceSlider;
        private Toggle voiceToggle;
        private Toggle vibrationToggle;
        private Button languageButton;
        private TMP_Text languageValueText;
        private Toggle notificationsToggle;
        private Button restorePurchasesButton;
        private Button privacyPolicyButton;

        private bool configured;
        private bool suppressEvents;
        private int languageIndex;

        public void Configure(
            GameObject root,
            Button backdrop,
            Slider musicVolume,
            Toggle musicEnabled,
            Slider sfxVolume,
            Toggle sfxEnabled,
            Slider voiceVolume,
            Toggle voiceEnabled,
            Toggle vibration,
            Button language,
            TMP_Text languageText,
            Toggle notifications,
            Button restorePurchases,
            Button privacyPolicy)
        {
            popupRoot = root;
            backdropButton = backdrop;
            musicSlider = musicVolume;
            musicToggle = musicEnabled;
            sfxSlider = sfxVolume;
            sfxToggle = sfxEnabled;
            voiceSlider = voiceVolume;
            voiceToggle = voiceEnabled;
            vibrationToggle = vibration;
            languageButton = language;
            languageValueText = languageText;
            notificationsToggle = notifications;
            restorePurchasesButton = restorePurchases;
            privacyPolicyButton = privacyPolicy;

            BindEvents();
            LoadSettings();
            configured = true;
            popupRoot.SetActive(false);
        }

        public void Open()
        {
            if (!configured || popupRoot == null)
            {
                return;
            }

            LoadSettings();
            popupRoot.SetActive(true);
            popupRoot.transform.SetAsLastSibling();
            UiSoundPlayer.PlayOpen();
        }

        private void Update()
        {
            if (popupRoot == null || !popupRoot.activeSelf)
            {
                return;
            }

            bool backPressed = false;
#if ENABLE_INPUT_SYSTEM
            backPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            backPressed = Input.GetKeyDown(KeyCode.Escape);
#endif

            if (backPressed)
            {
                CloseWithBackSound();
            }
        }

        private void BindEvents()
        {
            backdropButton?.onClick.AddListener(CloseWithCloseSound);
            languageButton?.onClick.AddListener(CycleLanguage);
            restorePurchasesButton?.onClick.AddListener(OnRestorePurchases);
            privacyPolicyButton?.onClick.AddListener(OnPrivacyPolicy);

            musicSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
            sfxSlider?.onValueChanged.AddListener(OnSfxVolumeChanged);
            voiceSlider?.onValueChanged.AddListener(OnVoiceVolumeChanged);

            musicToggle?.onValueChanged.AddListener(value => OnToggleChanged(MusicEnabledKey, value, false, true));
            sfxToggle?.onValueChanged.AddListener(value => OnToggleChanged(SfxEnabledKey, value, true, false));
            voiceToggle?.onValueChanged.AddListener(value => OnToggleChanged(VoiceEnabledKey, value, false, false));
            vibrationToggle?.onValueChanged.AddListener(value => OnToggleChanged(VibrationKey, value, false, false));
            notificationsToggle?.onValueChanged.AddListener(value => OnToggleChanged(NotificationsKey, value, false, false));
        }

        private void LoadSettings()
        {
            suppressEvents = true;

            if (musicToggle != null) musicToggle.isOn = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
            if (musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f);
            if (sfxToggle != null) sfxToggle.isOn = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
            if (sfxSlider != null) sfxSlider.value = PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);
            if (voiceToggle != null) voiceToggle.isOn = PlayerPrefs.GetInt(VoiceEnabledKey, 1) == 1;
            if (voiceSlider != null) voiceSlider.value = PlayerPrefs.GetFloat(VoiceVolumeKey, 0.8f);
            if (vibrationToggle != null) vibrationToggle.isOn = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            if (notificationsToggle != null) notificationsToggle.isOn = PlayerPrefs.GetInt(NotificationsKey, 1) == 1;

            languageIndex = Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, 1);
            RefreshLanguage();
            suppressEvents = false;
            ApplyMusicPreview();
        }

        private void OnToggleChanged(string key, bool enabled, bool isSfxToggle, bool controlsMusic)
        {
            if (suppressEvents)
            {
                return;
            }

            if (isSfxToggle)
            {
                if (enabled)
                {
                    PlayerPrefs.SetInt(key, 1);
                    PlayerPrefs.Save();
                    UiSoundPlayer.PlayToggleOn(force: true);
                }
                else
                {
                    UiSoundPlayer.PlayToggleOff(force: true);
                    PlayerPrefs.SetInt(key, 0);
                    PlayerPrefs.Save();
                }
            }
            else
            {
                PlayerPrefs.SetInt(key, enabled ? 1 : 0);
                PlayerPrefs.Save();

                if (enabled)
                {
                    UiSoundPlayer.PlayToggleOn();
                }
                else
                {
                    UiSoundPlayer.PlayToggleOff();
                }
            }

            if (controlsMusic)
            {
                ApplyMusicPreview();
            }
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (suppressEvents) return;
            PlayerPrefs.SetFloat(MusicVolumeKey, value);
            PlayerPrefs.Save();
            ApplyMusicPreview();
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (suppressEvents) return;
            PlayerPrefs.SetFloat(SfxVolumeKey, value);
            PlayerPrefs.Save();
        }

        private void OnVoiceVolumeChanged(float value)
        {
            if (suppressEvents) return;
            PlayerPrefs.SetFloat(VoiceVolumeKey, value);
            PlayerPrefs.Save();
        }

        private void CycleLanguage()
        {
            languageIndex = languageIndex == 0 ? 1 : 0;
            PlayerPrefs.SetInt(LanguageKey, languageIndex);
            PlayerPrefs.Save();
            RefreshLanguage();
            UiSoundPlayer.PlayConfirm();
        }

        private void RefreshLanguage()
        {
            if (languageValueText != null)
            {
                languageValueText.text = languageIndex == 0 ? "English" : "Russian";
            }
        }

        private void CloseWithCloseSound()
        {
            UiSoundPlayer.PlayClose();
            popupRoot?.SetActive(false);
        }

        private void CloseWithBackSound()
        {
            UiSoundPlayer.PlayBack();
            popupRoot?.SetActive(false);
        }

        private void OnRestorePurchases()
        {
            UiSoundPlayer.PlayConfirm();
            Debug.Log("Restore Purchases requested.");
        }

        private void OnPrivacyPolicy()
        {
            UiSoundPlayer.PlayConfirm();
            Debug.Log("Privacy Policy requested.");
        }

        private void ApplyMusicPreview()
        {
            AudioSource source = FindMainMenuMusicSource();
            if (source == null) return;

            bool enabled = musicToggle == null || musicToggle.isOn;
            source.mute = !enabled;
            source.volume = musicSlider != null
                ? Mathf.Clamp01(musicSlider.value)
                : PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f);

            if (enabled && source.clip != null && !source.isPlaying)
            {
                source.Play();
            }
        }

        private static AudioSource FindMainMenuMusicSource()
        {
            GameObject named = GameObject.Find("MainMenuBGM");
            if (named != null && named.TryGetComponent(out AudioSource namedSource))
            {
                return namedSource;
            }

            AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (AudioSource source in sources)
            {
                if (source != null && source.loop && source.clip != null)
                {
                    return source;
                }
            }

            return null;
        }
    }
}
