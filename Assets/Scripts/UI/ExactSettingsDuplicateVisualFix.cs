using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DefaultExecutionOrder(5500)]
    internal sealed class ExactSettingsDuplicateVisualFix : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private static ExactSettingsDuplicateVisualFix instance;
        private static Sprite referenceSprite;

        private static readonly string[] LiveControlNames =
        {
            "MusicSlider",
            "MusicToggle",
            "SfxSlider",
            "SfxToggle",
            "VoiceSlider",
            "VoiceToggle",
            "VibrationToggle",
            "LanguageButton",
            "NotificationsToggle"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            referenceSprite = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCurrentScene()
        {
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (scene.name != MainMenuSceneName || instance != null) return;

            GameObject host = new GameObject("ExactSettingsDuplicateVisualFix");
            instance = host.AddComponent<ExactSettingsDuplicateVisualFix>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 300; i++)
            {
                ApplySingleLayerFix();
                yield return null;
            }
        }

        private void LateUpdate()
        {
            if (Time.frameCount % 30 == 0)
            {
                ApplySingleLayerFix();
            }
        }

        private static void ApplySingleLayerFix()
        {
            DisableLegacyBackgroundBuilders();

            RectTransform panel = FindPanel();
            if (panel == null) return;

            DisableDuplicateSettingsControls(panel);

            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                referenceSprite ??= EmbeddedSettingsAssets.CreatePanelSprite();
                panelImage.sprite = referenceSprite;
                panelImage.preserveAspect = true;
                panelImage.color = Color.white;
                panelImage.raycastTarget = true;
            }

            Transform cleanup = FindChildRecursive(panel, "BakedControlsCleanup");
            if (cleanup != null)
            {
                cleanup.gameObject.SetActive(true);
                cleanup.SetAsFirstSibling();
            }

            // Keep the masks behind the one functional control set.
            foreach (string controlName in LiveControlNames)
            {
                Transform control = FindChildRecursive(panel, controlName);
                if (control != null)
                {
                    control.gameObject.SetActive(true);
                    control.SetAsLastSibling();
                }
            }
        }

        private static void DisableLegacyBackgroundBuilders()
        {
            RectTransform[] allRects = Resources.FindObjectsOfTypeAll<RectTransform>();
            foreach (RectTransform rect in allRects)
            {
                if (rect == null || !rect.gameObject.scene.IsValid()) continue;

                if (rect.name == "SettingsReferenceHardBackground" ||
                    rect.name == "ReferencePanelBackground" ||
                    rect.name == "ProceduralReferenceSurface")
                {
                    rect.gameObject.SetActive(false);
                }
            }

            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null || !behaviour.gameObject.scene.IsValid()) continue;

                string typeName = behaviour.GetType().Name;
                if (typeName == "ReferenceSettingsPanelHardFix" ||
                    typeName == "ReferenceSettingsPanelSpriteFix")
                {
                    behaviour.enabled = false;
                }
            }
        }

        private static void DisableDuplicateSettingsControls(RectTransform canonicalPanel)
        {
            Selectable[] selectables = Resources.FindObjectsOfTypeAll<Selectable>();
            foreach (Selectable selectable in selectables)
            {
                if (selectable == null || !selectable.gameObject.scene.IsValid()) continue;
                if (selectable.transform.IsChildOf(canonicalPanel)) continue;

                bool isSettingsControl =
                    selectable is Slider ||
                    selectable is Toggle ||
                    selectable.name == "LanguageButton";

                if (isSettingsControl && HasSettingsAncestor(selectable.transform))
                {
                    selectable.gameObject.SetActive(false);
                }
            }

            RectTransform[] allRects = Resources.FindObjectsOfTypeAll<RectTransform>();
            foreach (RectTransform rect in allRects)
            {
                if (rect == null || rect == canonicalPanel || rect.name != "ReferencePanel") continue;
                if (!rect.gameObject.scene.IsValid() || !rect.gameObject.scene.isLoaded) continue;
                rect.gameObject.SetActive(false);
            }
        }

        private static bool HasSettingsAncestor(Transform source)
        {
            Transform current = source;
            while (current != null)
            {
                string lowerName = current.name.ToLowerInvariant();
                if (lowerName.Contains("settingspopup") ||
                    lowerName.Contains("settings_popup") ||
                    lowerName.Contains("settingspanel") ||
                    lowerName.Contains("referencepanel"))
                {
                    return true;
                }

                if (current.GetComponent<Canvas>() != null) break;
                current = current.parent;
            }

            return false;
        }

        private static RectTransform FindPanel()
        {
            RectTransform fallback = null;
            RectTransform[] allRects = Resources.FindObjectsOfTypeAll<RectTransform>();
            foreach (RectTransform rect in allRects)
            {
                if (rect == null || rect.name != "ReferencePanel") continue;
                if (!rect.gameObject.scene.IsValid() || !rect.gameObject.scene.isLoaded) continue;

                // Only the panel built by the current installer owns this marker.
                if (FindChildRecursive(rect, "BakedControlsCleanup") != null)
                {
                    return rect;
                }

                if (fallback == null)
                {
                    fallback = rect;
                }
            }

            return fallback;
        }

        private static Transform FindChildRecursive(Transform parent, string exactName)
        {
            if (parent == null) return null;

            foreach (Transform child in parent)
            {
                if (child.name == exactName) return child;

                Transform nested = FindChildRecursive(child, exactName);
                if (nested != null) return nested;
            }

            return null;
        }
    }
}
