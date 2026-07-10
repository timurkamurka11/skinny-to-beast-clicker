using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinnyToBeast.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string gameplaySceneName = "Main";

        [Header("Popup Panels")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject messagePanel;

        private void Awake()
        {
            CloseAllPanels();
        }

        public void StartGame()
        {
            CloseAllPanels();

            if (Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
                return;
            }

            Debug.LogWarning($"Gameplay scene '{gameplaySceneName}' is not added to Build Settings yet. Open File > Build Settings and add MainMenu + Main scenes.");
            ShowMessagePanel();
        }

        public void OpenSettings()
        {
            OpenSinglePanel(settingsPanel);
            Debug.Log("Settings panel opened.");
        }

        public void OpenShop()
        {
            OpenSinglePanel(shopPanel);
            Debug.Log("Shop panel opened.");
        }

        public void CloseSettings()
        {
            SetPanel(settingsPanel, false);
        }

        public void CloseShop()
        {
            SetPanel(shopPanel, false);
        }

        public void CloseMessage()
        {
            SetPanel(messagePanel, false);
        }

        public void CloseAllPanels()
        {
            SetPanel(settingsPanel, false);
            SetPanel(shopPanel, false);
            SetPanel(messagePanel, false);
        }

        public void SelectTrainTab()
        {
            StartGame();
        }

        public void SelectUpgradeTab()
        {
            OpenSettings();
        }

        public void SelectEarnTab()
        {
            OpenShop();
        }

        public void SelectAchieveTab()
        {
            OpenSinglePanel(messagePanel);
            Debug.Log("Achievements panel placeholder opened.");
        }

        private void ShowMessagePanel()
        {
            OpenSinglePanel(messagePanel);
        }

        private void OpenSinglePanel(GameObject panel)
        {
            CloseAllPanels();
            SetPanel(panel, true);
        }

        private static void SetPanel(GameObject panel, bool isActive)
        {
            if (panel != null)
            {
                panel.SetActive(isActive);
            }
        }
    }
}
