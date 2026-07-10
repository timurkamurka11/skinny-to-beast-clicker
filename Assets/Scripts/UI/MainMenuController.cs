using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinnyToBeast.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string gameplaySceneName = "Main";

        public void StartGame()
        {
            if (Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
                return;
            }

            Debug.LogWarning($"Gameplay scene '{gameplaySceneName}' is not added to Build Settings yet. Open File > Build Settings and add MainMenu + Main scenes.");
        }

        public void OpenSettings()
        {
            Debug.Log("Settings screen is not implemented yet.");
        }

        public void OpenShop()
        {
            Debug.Log("Shop screen is not implemented yet.");
        }
    }
}
