using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DefaultExecutionOrder(5000)]
    internal sealed class ExactSettingsOverlayVisualCleanup : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private static ExactSettingsOverlayVisualCleanup instance;

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
            if (scene.name != MainMenuSceneName) return;
            if (instance != null) return;

            GameObject host = new GameObject("ExactSettingsOverlayVisualCleanup");
            instance = host.AddComponent<ExactSettingsOverlayVisualCleanup>();
        }

        private IEnumerator Start()
        {
            // The settings UI is created at runtime by another installer.
            // Re-apply cleanup for a few seconds so no older generated visuals survive.
            for (int i = 0; i < 240; i++)
            {
                ApplyCleanup();
                yield return null;
            }
        }

        private static void ApplyCleanup()
        {
            RawImage exactBackground = FindExactReferenceBackground();
            if (exactBackground == null) return;

            Transform exactRoot = FindPopupRoot(exactBackground.transform);
            if (exactRoot == null) return;

            HideInteractiveOverlayVisuals(exactRoot, exactBackground);
            DisableOldSettingsRoots(exactRoot);
        }

        private static RawImage FindExactReferenceBackground()
        {
            RawImage[] rawImages = Resources.FindObjectsOfTypeAll<RawImage>();
            foreach (RawImage rawImage in rawImages)
            {
                if (rawImage == null || !rawImage.gameObject.scene.IsValid()) continue;

                string objectName = rawImage.gameObject.name.ToLowerInvariant();
                string textureName = rawImage.texture != null
                    ? rawImage.texture.name.ToLowerInvariant()
                    : string.Empty;

                bool looksExact = objectName.Contains("exact") ||
                                  objectName.Contains("reference") ||
                                  textureName.Contains("settings_exact") ||
                                  textureName.Contains("exact_settings") ||
                                  textureName.Contains("settings_reference");

                if (looksExact) return rawImage;
            }

            return null;
        }

        private static Transform FindPopupRoot(Transform background)
        {
            Transform current = background;
            Transform best = background;

            while (current != null)
            {
                string lowerName = current.name.ToLowerInvariant();
                if (lowerName.Contains("exactsettings") ||
                    lowerName.Contains("exact_settings") ||
                    lowerName.Contains("settingspopup") ||
                    lowerName.Contains("settings_popup"))
                {
                    best = current;
                }

                if (current.GetComponent<Canvas>() != null) break;
                current = current.parent;
            }

            return best;
        }

        private static void HideInteractiveOverlayVisuals(Transform exactRoot, RawImage exactBackground)
        {
            // The reference PNG already contains every icon, slider, knob, label and toggle.
            // Real controls must remain clickable but visually fully transparent.
            Selectable[] controls = exactRoot.GetComponentsInChildren<Selectable>(true);
            foreach (Selectable control in controls)
            {
                if (control == null) continue;

                Graphic[] graphics = control.GetComponentsInChildren<Graphic>(true);
                foreach (Graphic graphic in graphics)
                {
                    if (graphic == null || graphic == exactBackground) continue;
                    MakeInvisible(graphic);
                }

                Shadow[] shadows = control.GetComponentsInChildren<Shadow>(true);
                foreach (Shadow shadow in shadows)
                {
                    if (shadow != null) shadow.enabled = false;
                }
            }

            // Remove any runtime text placed over the baked reference image.
            TMP_Text[] texts = exactRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                if (text == null) continue;
                Color color = text.color;
                color.a = 0f;
                text.color = color;
                text.raycastTarget = false;
            }

            // Catch loose graphics that are not nested directly under a Selectable.
            Graphic[] allGraphics = exactRoot.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in allGraphics)
            {
                if (graphic == null || graphic == exactBackground) continue;

                bool belongsToControl = graphic.GetComponentInParent<Selectable>() != null;
                string lowerName = graphic.gameObject.name.ToLowerInvariant();
                bool looksLikeOldVisual = lowerName.Contains("icon") ||
                                          lowerName.Contains("handle") ||
                                          lowerName.Contains("fill") ||
                                          lowerName.Contains("track") ||
                                          lowerName.Contains("checkmark") ||
                                          lowerName.Contains("arrow") ||
                                          lowerName.Contains("label");

                if (belongsToControl || looksLikeOldVisual)
                {
                    MakeInvisible(graphic);
                }
            }
        }

        private static void MakeInvisible(Graphic graphic)
        {
            Color color = graphic.color;
            color.a = 0.001f;
            graphic.color = color;

            // Keep raycasts working for invisible buttons, sliders and toggles.
            if (graphic.GetComponentInParent<Selectable>() != null)
            {
                graphic.raycastTarget = true;
            }
        }

        private static void DisableOldSettingsRoots(Transform exactRoot)
        {
            Selectable[] allControls = Resources.FindObjectsOfTypeAll<Selectable>();
            foreach (Selectable control in allControls)
            {
                if (control == null || !control.gameObject.scene.IsValid()) continue;
                if (control.transform.IsChildOf(exactRoot)) continue;

                Transform candidateRoot = FindNearestSettingsRoot(control.transform);
                if (candidateRoot == null || candidateRoot == exactRoot) continue;

                string lowerName = candidateRoot.name.ToLowerInvariant();
                if (lowerName.Contains("hotspot") || lowerName.Contains("settingsbutton")) continue;

                // Only disable old popup panels, never the main menu settings button.
                bool hasPopupControls = candidateRoot.GetComponentInChildren<Slider>(true) != null ||
                                        candidateRoot.GetComponentInChildren<Toggle>(true) != null;
                if (hasPopupControls)
                {
                    candidateRoot.gameObject.SetActive(false);
                }
            }
        }

        private static Transform FindNearestSettingsRoot(Transform source)
        {
            Transform current = source;
            while (current != null)
            {
                string lowerName = current.name.ToLowerInvariant();
                if (lowerName.Contains("settingspopup") ||
                    lowerName.Contains("settings_popup") ||
                    lowerName.Contains("referencepanel") ||
                    lowerName.Contains("settingspanel"))
                {
                    return current;
                }

                if (current.GetComponent<Canvas>() != null) break;
                current = current.parent;
            }

            return null;
        }
    }
}
