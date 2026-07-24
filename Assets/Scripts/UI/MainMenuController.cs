using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinnyToBeast.UI
{
    public class MainMenuController : MonoBehaviour
    {
        private const string MusicKey = "settings.music";
        private const string SfxKey = "settings.sfx";
        private const string VibrationKey = "settings.vibration";

        [Header("Scene Names")]
        [SerializeField] private string gameplaySceneName = "Main";

        [Header("Animated Popups")]
        [SerializeField] private PopupPanelAnimator settingsPanel;
        [SerializeField] private PopupPanelAnimator shopPanel;
        [SerializeField] private PopupPanelAnimator messagePanel;

        [Header("Settings Labels")]
        [SerializeField] private TMP_Text musicValueText;
        [SerializeField] private TMP_Text sfxValueText;
        [SerializeField] private TMP_Text vibrationValueText;

        [Header("Message Content")]
        [SerializeField] private TMP_Text messageTitleText;
        [SerializeField] private TMP_Text messageBodyText;

        private bool musicEnabled;
        private bool sfxEnabled;
        private bool vibrationEnabled;

        private void Awake()
        {
            musicEnabled = PlayerPrefs.GetInt(MusicKey, 1) == 1;
            sfxEnabled = PlayerPrefs.GetInt(SfxKey, 1) == 1;
            vibrationEnabled = PlayerPrefs.GetInt(VibrationKey, 1) == 1;

            HidePanelImmediate(settingsPanel);
            HidePanelImmediate(shopPanel);
            HidePanelImmediate(messagePanel);
            RefreshSettingsLabels();
        }

        public void StartGame()
        {
            CloseAllPanels();

            if (GameplayWindowController.Show())
            {
                return;
            }

            if (Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
                return;
            }

            ShowMessage(
                "GAMEPLAY SCENE MISSING",
                $"Scene '{gameplaySceneName}' is not available in Build Settings. Rebuild Main and MainMenu through the Tools menu."
            );
        }

        public void OpenSettings()
        {
            OpenSinglePanel(settingsPanel);
        }

        public void OpenShop()
        {
            OpenSinglePanel(shopPanel);
        }

        public void CloseSettings()
        {
            HidePanel(settingsPanel);
        }

        public void CloseShop()
        {
            HidePanel(shopPanel);
        }

        public void CloseMessage()
        {
            HidePanel(messagePanel);
        }

        public void CloseAllPanels()
        {
            HidePanel(settingsPanel);
            HidePanel(shopPanel);
            HidePanel(messagePanel);
        }

        public void ToggleMusic()
        {
            musicEnabled = !musicEnabled;
            SaveSetting(MusicKey, musicEnabled);
            RefreshSettingsLabels();
        }

        public void ToggleSfx()
        {
            sfxEnabled = !sfxEnabled;
            SaveSetting(SfxKey, sfxEnabled);
            RefreshSettingsLabels();
        }

        public void ToggleVibration()
        {
            vibrationEnabled = !vibrationEnabled;
            SaveSetting(VibrationKey, vibrationEnabled);
            RefreshSettingsLabels();
        }

        public void BuyStarterPack()
        {
            ShowMessage("STARTER PACK", "Store integration will be connected after the core gameplay loop is ready.");
        }

        public void BuyNoAds()
        {
            ShowMessage("REMOVE ADS", "This purchase will become available together with Google Play Billing.");
        }

        public void BuyProteinPack()
        {
            ShowMessage("PROTEIN PACK", "Premium boosters are planned for a later monetization milestone.");
        }

        public void ClaimDailyReward()
        {
            ShowMessage("DAILY REWARD", "Daily rewards are programmed as a menu action and will be connected to player saves next.");
        }

        public void OpenLeaderboard()
        {
            ShowMessage("LEADERBOARD", "Global rankings will be connected after player profiles and cloud saves.");
        }

        public void SelectTrainTab()
        {
            StartGame();
        }

        public void SelectUpgradeTab()
        {
            ShowMessage("UPGRADES", "The full upgrade screen will open here after the first playable training scene is finished.");
        }

        public void SelectEarnTab()
        {
            OpenShop();
        }

        public void SelectAchieveTab()
        {
            ShowMessage("ACHIEVEMENTS", "Achievements are planned for the next progression milestone.");
        }

        private void ShowMessage(string title, string body)
        {
            if (messageTitleText != null)
            {
                messageTitleText.text = title;
            }

            if (messageBodyText != null)
            {
                messageBodyText.text = body;
            }

            OpenSinglePanel(messagePanel);
        }

        private void OpenSinglePanel(PopupPanelAnimator panel)
        {
            if (panel == null)
            {
                return;
            }

            if (settingsPanel != panel)
            {
                HidePanel(settingsPanel);
            }

            if (shopPanel != panel)
            {
                HidePanel(shopPanel);
            }

            if (messagePanel != panel)
            {
                HidePanel(messagePanel);
            }

            panel.Show();
        }

        private static void HidePanel(PopupPanelAnimator panel)
        {
            // Unity keeps a managed reference after the native object is destroyed.
            // A null-conditional call (panel?.Hide()) only checks the managed
            // reference and therefore throws MissingReferenceException. The
            // explicit Unity null comparison correctly treats it as destroyed.
            if (panel != null)
            {
                panel.Hide();
            }
        }

        private static void HidePanelImmediate(PopupPanelAnimator panel)
        {
            if (panel != null)
            {
                panel.HideImmediate();
            }
        }

        private void RefreshSettingsLabels()
        {
            SetToggleLabel(musicValueText, "MUSIC", musicEnabled);
            SetToggleLabel(sfxValueText, "SFX", sfxEnabled);
            SetToggleLabel(vibrationValueText, "VIBRATION", vibrationEnabled);
        }

        private static void SetToggleLabel(TMP_Text target, string label, bool enabled)
        {
            if (target != null)
            {
                target.text = $"{label}   {(enabled ? "ON" : "OFF")}";
            }
        }

        private static void SaveSetting(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
