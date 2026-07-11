using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DisallowMultipleComponent]
    public sealed class SettingsMenuController : MonoBehaviour
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

        [Header("Popup")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button applyButton;

        [Header("Audio")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private TMP_Text musicToggleText;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private TMP_Text sfxToggleText;
        [SerializeField] private Slider voiceSlider;
        [SerializeField] private Toggle voiceToggle;
        [SerializeField] private TMP_Text voiceToggleText;

        [Header("Gameplay")]
        [SerializeField] private Toggle vibrationToggle;
        [SerializeField] private TMP_Text vibrationToggleText;
        [SerializeField] private Button languageButton;
        [SerializeField] private TMP_Text languageValueText;
        [SerializeField] private Toggle notificationsToggle;
        [SerializeField] private TMP_Text notificationsToggleText;

        [Header("Account")]
        [SerializeField] private Button restorePurchasesButton;
        [SerializeField] private Button privacyPolicyButton;

        private bool isConfigured;
        private int selectedLanguage;

        public void Configure(
            GameObject root,
            Button close,
            Button back,
            Button apply,
            Slider musicVolume,
            Toggle musicEnabled,
            TMP_Text musicState,
            Slider sfxVolume,
            Toggle sfxEnabled,
            TMP_Text sfxState,
            Slider voiceVolume,
            Toggle voiceEnabled,
            TMP_Text voiceState,
            Toggle vibration,
            TMP_Text vibrationState,
            Button language,
            TMP_Text languageValue,
            Toggle notifications,
            TMP_Text notificationsState,
            Button restorePurchases,
            Button privacyPolicy)
        {
            popupRoot = root;
            closeButton = close;
            backButton = back;
            applyButton = apply;

            musicSlider = musicVolume;
            musicToggle = musicEnabled;
            musicToggleText = musicState;
            sfxSlider = sfxVolume;
            sfxToggle = sfxEnabled;
            sfxToggleText = sfxState;
            voiceSlider = voiceVolume;
            voiceToggle = voiceEnabled;
            voiceToggleText = voiceState;

            vibrationToggle = vibration;
            vibrationToggleText = vibrationState;
            languageButton = language;
            languageValueText = languageValue;
            notificationsToggle = notifications;
            notificationsToggleText = notificationsState;

            restorePurchasesButton = restorePurchases;
            privacyPolicyButton = privacyPolicy;

            BindEvents();
            LoadSavedSettings();
            isConfigured = true;

            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        public void Open()
        {
            if (!isConfigured || popupRoot == null)
            {
                return;
            }

            LoadSavedSettings();
            popupRoot.SetActive(true);
        }

        public void CloseWithoutSaving()
        {
            LoadSavedSettings();
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        public void Apply()
        {
            PlayerPrefs.SetInt(MusicEnabledKey, musicToggle != null && musicToggle.isOn ? 1 : 0);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicSlider != null ? musicSlider.value : 0.7f);
            PlayerPrefs.SetInt(SfxEnabledKey, sfxToggle != null && sfxToggle.isOn ? 1 : 0);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxSlider != null ? sfxSlider.value : 0.8f);
            PlayerPrefs.SetInt(VoiceEnabledKey, voiceToggle != null && voiceToggle.isOn ? 1 : 0);
            PlayerPrefs.SetFloat(VoiceVolumeKey, voiceSlider != null ? voiceSlider.value : 0.8f);
            PlayerPrefs.SetInt(VibrationKey, vibrationToggle != null && vibrationToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt(LanguageKey, selectedLanguage);
            PlayerPrefs.SetInt(NotificationsKey, notificationsToggle != null && notificationsToggle.isOn ? 1 : 0);
            PlayerPrefs.Save();

            ApplyMusicPreview();

            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        private void BindEvents()
        {
            closeButton?.onClick.AddListener(CloseWithoutSaving);
            backButton?.onClick.AddListener(CloseWithoutSaving);
            applyButton?.onClick.AddListener(Apply);
            languageButton?.onClick.AddListener(CycleLanguage);
            restorePurchasesButton?.onClick.AddListener(RestorePurchases);
            privacyPolicyButton?.onClick.AddListener(OpenPrivacyPolicy);

            musicSlider?.onValueChanged.AddListener(_ => ApplyMusicPreview());
            musicToggle?.onValueChanged.AddListener(_ =>
            {
                RefreshToggleLabels();
                ApplyMusicPreview();
            });
            sfxToggle?.onValueChanged.AddListener(_ => RefreshToggleLabels());
            voiceToggle?.onValueChanged.AddListener(_ => RefreshToggleLabels());
            vibrationToggle?.onValueChanged.AddListener(_ => RefreshToggleLabels());
            notificationsToggle?.onValueChanged.AddListener(_ => RefreshToggleLabels());
        }

        private void LoadSavedSettings()
        {
            if (musicToggle != null) musicToggle.isOn = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
            if (musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f);
            if (sfxToggle != null) sfxToggle.isOn = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
            if (sfxSlider != null) sfxSlider.value = PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);
            if (voiceToggle != null) voiceToggle.isOn = PlayerPrefs.GetInt(VoiceEnabledKey, 1) == 1;
            if (voiceSlider != null) voiceSlider.value = PlayerPrefs.GetFloat(VoiceVolumeKey, 0.8f);
            if (vibrationToggle != null) vibrationToggle.isOn = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            if (notificationsToggle != null) notificationsToggle.isOn = PlayerPrefs.GetInt(NotificationsKey, 1) == 1;

            selectedLanguage = Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, 1);
            RefreshLanguageLabel();
            RefreshToggleLabels();
            ApplyMusicPreview();
        }

        private void CycleLanguage()
        {
            selectedLanguage = selectedLanguage == 0 ? 1 : 0;
            RefreshLanguageLabel();
        }

        private void RefreshLanguageLabel()
        {
            if (languageValueText != null)
            {
                languageValueText.text = selectedLanguage == 0 ? "ENGLISH" : "RUSSIAN";
            }
        }

        private void RefreshToggleLabels()
        {
            SetToggleLabel(musicToggleText, musicToggle);
            SetToggleLabel(sfxToggleText, sfxToggle);
            SetToggleLabel(voiceToggleText, voiceToggle);
            SetToggleLabel(vibrationToggleText, vibrationToggle);
            SetToggleLabel(notificationsToggleText, notificationsToggle);
        }

        private static void SetToggleLabel(TMP_Text label, Toggle toggle)
        {
            if (label != null && toggle != null)
            {
                label.text = toggle.isOn ? "ON" : "OFF";
            }
        }

        private void ApplyMusicPreview()
        {
            AudioSource source = FindMainMenuMusicSource();
            if (source == null)
            {
                return;
            }

            bool enabled = musicToggle == null || musicToggle.isOn;
            source.mute = !enabled;
            source.volume = musicSlider != null ? musicSlider.value : 0.7f;

            if (enabled && source.clip != null && !source.isPlaying)
            {
                source.Play();
            }
        }

        private static AudioSource FindMainMenuMusicSource()
        {
            GameObject musicObject = GameObject.Find("MainMenuBGM");
            if (musicObject != null)
            {
                AudioSource namedSource = musicObject.GetComponent<AudioSource>();
                if (namedSource != null)
                {
                    return namedSource;
                }
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

        private static void RestorePurchases()
        {
            Debug.Log("Restore Purchases will be connected with store billing later.");
        }

        private static void OpenPrivacyPolicy()
        {
            Debug.Log("Privacy Policy button clicked. Add the final policy URL before release.");
        }
    }
}
