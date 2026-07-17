using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DefaultExecutionOrder(5500)]
    internal sealed class ExactSettingsDuplicateVisualFix : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const float HiddenAlpha = 0.001f;
        private static ExactSettingsDuplicateVisualFix instance;

        private static readonly string[] SliderNames =
        {
            "MusicSlider",
            "SfxSlider",
            "VoiceSlider"
        };

        private static readonly string[] ToggleNames =
        {
            "MusicToggle",
            "SfxToggle",
            "VoiceToggle",
            "VibrationToggle",
            "NotificationsToggle"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
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
            // RuntimeSettingsPopupInstaller creates the panel after scene load.
            // Re-apply only to named duplicated control visuals; never disable popup roots.
            for (int i = 0; i < 300; i++)
            {
                ApplyTargetedFix();
                yield return null;
            }
        }

        private void LateUpdate()
        {
            // Unity Selectables and toggle listeners can repaint on the same frame that
            // the popup opens. Keep the input areas alive, but suppress their duplicate
            // graphics before every rendered frame.
            ApplyTargetedFix();
        }

        private static void ApplyTargetedFix()
        {
            DisableOnlyObsoleteFallbackBackground();

            RectTransform panel = FindPanel();
            if (panel == null) return;

            foreach (string sliderName in SliderNames)
            {
                Transform sliderRoot = FindChildRecursive(panel, sliderName);
                if (sliderRoot != null)
                {
                    HideSliderGraphics(sliderRoot);
                }
            }

            foreach (string toggleName in ToggleNames)
            {
                Transform toggleRoot = FindChildRecursive(panel, toggleName);
                if (toggleRoot != null)
                {
                    HideToggleGraphics(toggleRoot);
                }
            }

            Transform languageRoot = FindChildRecursive(panel, "LanguageButton");
            if (languageRoot != null)
            {
                HideLanguageGraphics(languageRoot);
            }

            // Restore Purchases and Privacy Policy hotspots are already invisible and remain functional.
        }

        private static void HideSliderGraphics(Transform sliderRoot)
        {
            Slider slider = sliderRoot.GetComponent<Slider>();
            if (slider == null) return;
            slider.transition = Selectable.Transition.None;

            Image[] images = sliderRoot.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image == null) continue;
                SetTransparentButInteractive(image);
            }

            Outline[] outlines = sliderRoot.GetComponentsInChildren<Outline>(true);
            foreach (Outline outline in outlines)
            {
                if (outline != null) outline.enabled = false;
            }

            // Keep slider interaction active across the complete track area.
            Image track = FindNamedComponent<Image>(sliderRoot, "Track");
            if (track != null)
            {
                track.raycastTarget = true;
                slider.targetGraphic = track;
            }
        }

        private static void HideToggleGraphics(Transform toggleRoot)
        {
            Toggle toggle = toggleRoot.GetComponent<Toggle>();
            if (toggle == null) return;
            toggle.transition = Selectable.Transition.None;

            ReferenceToggleVisual visual = toggleRoot.GetComponent<ReferenceToggleVisual>();
            if (visual != null)
            {
                Object.Destroy(visual);
            }

            Image[] images = toggleRoot.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image == null) continue;
                SetTransparentButInteractive(image);
            }

            TMP_Text[] texts = toggleRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                if (text == null) continue;
                Color color = text.color;
                color.a = 0f;
                text.color = color;
                text.raycastTarget = false;
            }

            Outline[] outlines = toggleRoot.GetComponentsInChildren<Outline>(true);
            foreach (Outline outline in outlines)
            {
                if (outline != null) outline.enabled = false;
            }

            Image rootImage = toggleRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.raycastTarget = true;
                toggle.targetGraphic = rootImage;
            }
        }

        private static void HideLanguageGraphics(Transform languageRoot)
        {
            Button button = languageRoot.GetComponent<Button>();
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
            }

            Image background = languageRoot.GetComponent<Image>();
            if (background != null)
            {
                SetTransparentButInteractive(background);
                if (button != null)
                {
                    button.targetGraphic = background;
                }
            }

            Transform arrowRoot = FindChildRecursive(languageRoot, "Arrow");
            if (arrowRoot != null)
            {
                Graphic[] arrowGraphics = arrowRoot.GetComponentsInChildren<Graphic>(true);
                foreach (Graphic graphic in arrowGraphics)
                {
                    if (graphic != null)
                    {
                        SetTransparentButInteractive(graphic);
                        graphic.raycastTarget = false;
                    }
                }
            }

            Outline[] outlines = languageRoot.GetComponentsInChildren<Outline>(true);
            foreach (Outline outline in outlines)
            {
                if (outline != null) outline.enabled = false;
            }
        }

        private static void DisableOnlyObsoleteFallbackBackground()
        {
            // This old generated background duplicates the baked reference image.
            // Disable only this named fallback, not SettingsPopup, ReferencePanel or controls.
            RectTransform[] allRects = Resources.FindObjectsOfTypeAll<RectTransform>();
            foreach (RectTransform rect in allRects)
            {
                if (rect == null || !rect.gameObject.scene.IsValid()) continue;

                if (rect.name == "SettingsReferenceHardBackground" ||
                    rect.name == "ProceduralReferenceSurface")
                {
                    rect.gameObject.SetActive(false);
                }
            }

            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null || !behaviour.gameObject.scene.IsValid()) continue;

                if (behaviour.GetType().Name == "ReferenceSettingsPanelHardFix")
                {
                    behaviour.enabled = false;
                }
            }
        }

        private static RectTransform FindPanel()
        {
            RectTransform[] allRects = Resources.FindObjectsOfTypeAll<RectTransform>();
            foreach (RectTransform rect in allRects)
            {
                if (rect == null || rect.name != "ReferencePanel") continue;
                if (!rect.gameObject.scene.IsValid() || !rect.gameObject.scene.isLoaded) continue;
                return rect;
            }

            return null;
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

        private static T FindNamedComponent<T>(Transform parent, string exactName) where T : Component
        {
            Transform child = FindChildRecursive(parent, exactName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static void SetTransparentButInteractive(Graphic graphic)
        {
            Color color = graphic.color;
            color.a = HiddenAlpha;
            graphic.color = color;
            graphic.canvasRenderer.SetAlpha(HiddenAlpha);
            graphic.raycastTarget = true;
        }
    }
}
