using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    /// <summary>
    /// Keeps the previously approved reference-image settings UI authoritative.
    /// Patch 3.6 creates a high-order gameplay Canvas; without this bridge an
    /// authored fallback settings panel can open above the room while the exact
    /// reference popup remains behind it. The bridge raises the exact popup,
    /// disables obsolete settings panels and reconnects every settings button.
    /// </summary>
    [DefaultExecutionOrder(31000)]
    internal sealed class Patch36SettingsBridge : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string HostName = "Patch36.SettingsBridge";
        private const int PopupSortingOrder = 32000;

        private static readonly FieldInfo PopupRootField =
            typeof(SettingsMenuController).GetField(
                "popupRoot",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static Patch36SettingsBridge instance;
        private SettingsMenuController exactController;
        private GameObject exactRoot;

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
            if (scene.name != MainMenuSceneName || instance != null)
            {
                return;
            }

            GameObject host = new GameObject(HostName);
            instance = host.AddComponent<Patch36SettingsBridge>();
        }

        private IEnumerator Start()
        {
            // RuntimeSettingsPopupInstaller builds after one frame. Keep
            // reconnecting for a few seconds because gameplay and menu buttons
            // are also instantiated at runtime.
            for (int frame = 0; frame < 600; frame++)
            {
                Repair();
                yield return null;
            }
        }

        private void LateUpdate()
        {
            Repair();
        }

        private void Repair()
        {
            if (exactController == null)
            {
                SettingsMenuController[] controllers =
                    Resources.FindObjectsOfTypeAll<SettingsMenuController>();
                for (int i = 0; i < controllers.Length; i++)
                {
                    SettingsMenuController candidate = controllers[i];
                    if (candidate != null &&
                        candidate.gameObject.scene.IsValid())
                    {
                        exactController = candidate;
                        break;
                    }
                }
            }

            if (exactController == null)
            {
                return;
            }

            if (exactRoot == null && PopupRootField != null)
            {
                exactRoot = PopupRootField.GetValue(exactController)
                    as GameObject;
            }

            if (exactRoot != null)
            {
                RaiseExactPopup(exactRoot);
                DisableLegacySettingsPanels(exactRoot.transform);
            }

            ReconnectSettingsButtons();
        }

        private static void RaiseExactPopup(GameObject popup)
        {
            Canvas canvas = popup.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = popup.AddComponent<Canvas>();
            }
            canvas.overrideSorting = true;
            canvas.sortingOrder = PopupSortingOrder;
            canvas.enabled = true;

            if (popup.GetComponent<GraphicRaycaster>() == null)
            {
                popup.AddComponent<GraphicRaycaster>();
            }
        }

        private void ReconnectSettingsButtons()
        {
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null ||
                    !button.gameObject.scene.IsValid() ||
                    (exactRoot != null &&
                     button.transform.IsChildOf(exactRoot.transform)))
                {
                    continue;
                }

                string lowerName = button.gameObject.name.ToLowerInvariant();
                if (!lowerName.Contains("settings"))
                {
                    continue;
                }

                // Persistent authored listeners may still call
                // MainMenuController.OpenSettings. That method is also updated
                // to delegate to the exact controller. This runtime listener
                // covers dynamically created settings buttons.
                button.onClick.RemoveListener(OpenExactSettings);
                button.onClick.AddListener(OpenExactSettings);
            }
        }

        private void OpenExactSettings()
        {
            exactController?.Open();
        }

        private static void DisableLegacySettingsPanels(Transform exactPopup)
        {
            PopupPanelAnimator[] panels =
                Resources.FindObjectsOfTypeAll<PopupPanelAnimator>();
            for (int i = 0; i < panels.Length; i++)
            {
                PopupPanelAnimator panel = panels[i];
                if (panel == null ||
                    !panel.gameObject.scene.IsValid() ||
                    panel.transform.IsChildOf(exactPopup))
                {
                    continue;
                }

                if (!LooksLikeLegacySettingsPanel(panel.transform))
                {
                    continue;
                }

                panel.HideImmediate();
                panel.gameObject.SetActive(false);
            }
        }

        private static bool LooksLikeLegacySettingsPanel(Transform root)
        {
            string lowerName = root.gameObject.name.ToLowerInvariant();
            if (lowerName.Contains("settings"))
            {
                return true;
            }

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            int matches = 0;
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                {
                    continue;
                }

                string value = text.text != null
                    ? text.text.ToUpperInvariant()
                    : string.Empty;
                if (value.Contains("MUSIC") ||
                    value.Contains("SFX") ||
                    value.Contains("VIBRATION") ||
                    value == "SETTINGS")
                {
                    matches++;
                }
            }
            return matches >= 2;
        }
    }
}
