using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DefaultExecutionOrder(1000)]
    internal sealed class RuntimeSettingsPopupInstaller : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const float SourceWidth = 283f;
        private const float SourceHeight = 301f;
        private const float PanelWidth = 960f;
        private const float PanelHeight = PanelWidth * SourceHeight / SourceWidth;

        private static Sprite panelSprite;
        private static Sprite roundedSprite;
        private static Sprite circleSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            panelSprite = null;
            roundedSprite = null;
            circleSprite = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallSceneListener()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInitialScene()
        {
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (scene.name != MainMenuSceneName)
            {
                return;
            }

            RuntimeSettingsPopupInstaller existing =
                Object.FindFirstObjectByType<RuntimeSettingsPopupInstaller>();

            if (existing != null)
            {
                return;
            }

            GameObject host = new GameObject("ReferenceSettingsUI");
            host.AddComponent<RuntimeSettingsPopupInstaller>();
        }

        private IEnumerator Start()
        {
            yield return null;
            BuildAndConnect();
        }

        private void BuildAndConnect()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Reference Settings UI: MainMenu Canvas was not found.");
                return;
            }

            RemoveLegacySettingsPopup();

            panelSprite ??= EmbeddedSettingsAssets.CreatePanelSprite();
            roundedSprite ??= CreateRoundedSprite(64, 22f);
            circleSprite ??= CreateCircleSprite(64);

            GameObject popupRoot = CreateFullScreenObject(canvas.transform, "SettingsPopup");
            Image dimImage = popupRoot.AddComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.68f);
            dimImage.raycastTarget = true;

            Button backdropButton = popupRoot.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;

            GameObject panelObject = new GameObject("ReferencePanel", typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(popupRoot.transform, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, -30f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.sprite = panelSprite;
            panelImage.preserveAspect = true;
            panelImage.raycastTarget = true;
            panelImage.color = Color.white;

            // The reference JPG contains a decorative preview of the controls.
            // Cover those baked pixels first, then draw one live interactive set above.
            BuildBakedControlCleanupMasks(panelObject.transform);

            BuildAudioControls(
                panelObject.transform,
                out Slider musicSlider,
                out Toggle musicToggle,
                out Slider sfxSlider,
                out Toggle sfxToggle,
                out Slider voiceSlider,
                out Toggle voiceToggle
            );

            BuildGameplayControls(
                panelObject.transform,
                out Toggle vibrationToggle,
                out Button languageButton,
                out TMP_Text languageValue,
                out Toggle notificationsToggle
            );

            Button restorePurchasesButton = CreateInvisibleHotspot(
                panelObject.transform,
                "RestorePurchasesButton",
                8f, 244f, 267f, 27f
            );

            Button privacyPolicyButton = CreateInvisibleHotspot(
                panelObject.transform,
                "PrivacyPolicyButton",
                8f, 271f, 267f, 27f
            );

            SettingsMenuController controller = gameObject.AddComponent<SettingsMenuController>();
            controller.Configure(
                popupRoot,
                backdropButton,
                musicSlider,
                musicToggle,
                sfxSlider,
                sfxToggle,
                voiceSlider,
                voiceToggle,
                vibrationToggle,
                languageButton,
                languageValue,
                notificationsToggle,
                restorePurchasesButton,
                privacyPolicyButton
            );

            Button settingsButton = FindSettingsButton();
            if (settingsButton == null)
            {
                Debug.LogError(
                    "Reference Settings UI was created, but SettingsHotspot/SettingsButton was not found."
                );
                return;
            }

            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(controller.Open);

            Debug.Log(
                "Reference-driven Settings UI installed: baked panel, aligned interactive controls and synchronized UI sounds."
            );
        }

        private static void BuildBakedControlCleanupMasks(Transform panel)
        {
            GameObject rootObject = new GameObject(
                "BakedControlsCleanup",
                typeof(RectTransform)
            );
            rootObject.transform.SetParent(panel, false);
            rootObject.transform.SetAsFirstSibling();

            RectTransform root = rootObject.GetComponent<RectTransform>();
            Stretch(root);

            Color rowColor = new Color(0.018f, 0.050f, 0.072f, 1f);

            // Audio: erase the three preview sliders and blue ON badges.
            CreateSourceMask(root, "MusicControlMask", 88f, 29f, 191f, 27f, rowColor);
            CreateSourceMask(root, "SfxControlMask", 88f, 55f, 191f, 27f, rowColor);
            CreateSourceMask(root, "VoiceControlMask", 88f, 81f, 191f, 27f, rowColor);

            // Gameplay: erase the complete baked wells and their offset shadows.
            CreateSourceMask(root, "VibrationControlMask", 195f, 133f, 84f, 29f, rowColor);
            CreateSourceMask(root, "LanguageControlMask", 112f, 160f, 167f, 32f, rowColor);
            CreateSourceMask(root, "NotificationsControlMask", 195f, 190f, 84f, 29f, rowColor);
        }

        private static void CreateSourceMask(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            Color color)
        {
            RectTransform rect = CreateSourceRect(parent, name, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void BuildControlEdgeCleanup(Transform panel)
        {
            GameObject rootObject = new GameObject(
                "ControlEdgeCleanup",
                typeof(RectTransform)
            );
            rootObject.transform.SetParent(panel, false);
            rootObject.transform.SetAsLastSibling();

            RectTransform root = rootObject.GetComponent<RectTransform>();
            Stretch(root);

            Color rowColor = new Color(0.018f, 0.050f, 0.072f, 1f);

            // Blue audio ON badges: remove the cyan tail on the right
            // and the thin duplicate edge below each badge.
            CreateSourceMask(root, "MusicRightTrim", 276f, 29f, 4f, 27f, rowColor);
            CreateSourceMask(root, "MusicBottomTrim", 234f, 53f, 46f, 3f, rowColor);
            CreateSourceMask(root, "SfxRightTrim", 276f, 55f, 4f, 27f, rowColor);
            CreateSourceMask(root, "SfxBottomTrim", 234f, 79f, 46f, 3f, rowColor);
            CreateSourceMask(root, "VoiceRightTrim", 276f, 81f, 4f, 27f, rowColor);
            CreateSourceMask(root, "VoiceBottomTrim", 234f, 105f, 46f, 3f, rowColor);

            // Orange toggles and Language: remove only their offset
            // right and bottom shadows, leaving the main controls untouched.
            CreateSourceMask(root, "VibrationRightTrim", 276f, 133f, 4f, 30f, rowColor);
            CreateSourceMask(root, "VibrationBottomTrim", 216f, 159f, 64f, 4f, rowColor);
            CreateSourceMask(root, "LanguageRightTrim", 276f, 160f, 4f, 32f, rowColor);
            CreateSourceMask(root, "LanguageBottomTrim", 119f, 187f, 161f, 5f, rowColor);
            CreateSourceMask(root, "NotificationsRightTrim", 276f, 190f, 4f, 32f, rowColor);
            CreateSourceMask(root, "NotificationsBottomTrim", 216f, 219f, 64f, 4f, rowColor);
        }

        private static void BuildAudioControls(
            Transform panel,
            out Slider musicSlider,
            out Toggle musicToggle,
            out Slider sfxSlider,
            out Toggle sfxToggle,
            out Slider voiceSlider,
            out Toggle voiceToggle)
        {
            musicSlider = CreateReferenceSlider(panel, "MusicSlider", 100f, 36f, 122f, 15f);
            sfxSlider = CreateReferenceSlider(panel, "SfxSlider", 100f, 62f, 122f, 15f);
            voiceSlider = CreateReferenceSlider(panel, "VoiceSlider", 100f, 88f, 122f, 15f);

            // Calibrated against the captured device frame so the live badges
            // fully cover the lower/right edge of the reference layer.
            musicToggle = CreateReferenceToggle(panel, "MusicToggle", 246f, 40.5f, 40f, 18f,
                new Color(0.04f, 0.49f, 0.94f, 1f), new Color(0.08f, 0.13f, 0.18f, 1f), false);
            sfxToggle = CreateReferenceToggle(panel, "SfxToggle", 246f, 66.5f, 40f, 18f,
                new Color(0.04f, 0.49f, 0.94f, 1f), new Color(0.08f, 0.13f, 0.18f, 1f), false);
            voiceToggle = CreateReferenceToggle(panel, "VoiceToggle", 246f, 92.5f, 40f, 18f,
                new Color(0.04f, 0.49f, 0.94f, 1f), new Color(0.08f, 0.13f, 0.18f, 1f), false);
        }

        private static void BuildGameplayControls(
            Transform panel,
            out Toggle vibrationToggle,
            out Button languageButton,
            out TMP_Text languageValue,
            out Toggle notificationsToggle)
        {
            // Gameplay controls were visibly up/left from their baked wells.
            // Shift the live layer onto the exact background positions.
            vibrationToggle = CreateReferenceToggle(panel, "VibrationToggle", 235f, 150f, 57f, 20f,
                new Color(1f, 0.55f, 0.05f, 1f), new Color(0.08f, 0.13f, 0.18f, 1f), true);
            languageButton = CreateLanguageControl(panel, 128f, 167f, 151f, 22f, out languageValue);
            notificationsToggle = CreateReferenceToggle(panel, "NotificationsToggle", 235f, 210f, 57f, 20f,
                new Color(1f, 0.55f, 0.05f, 1f), new Color(0.08f, 0.13f, 0.18f, 1f), true);
        }

        private static Slider CreateReferenceSlider(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height)
        {
            RectTransform rect = CreateSourceRect(parent, name, x, y, width, height);
            Slider slider = rect.gameObject.AddComponent<Slider>();

            // Opaque local cover hides the baked slider/handle before the live
            // track is rendered, so a saved volume value cannot reveal two knobs.
            Image bakedSliderCover = rect.gameObject.AddComponent<Image>();
            bakedSliderCover.color = new Color(0.018f, 0.050f, 0.072f, 1f);
            bakedSliderCover.raycastTarget = false;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            slider.direction = Slider.Direction.LeftToRight;

            Image background = CreateAnchoredImage(
                rect, "Track", Vector2.zero,
                new Vector2(rect.sizeDelta.x, Mathf.Max(10f, rect.sizeDelta.y * 0.28f)),
                new Color(0.015f, 0.025f, 0.04f, 1f), roundedSprite);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(rect, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            Stretch(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(0f, rect.sizeDelta.y * 0.32f);
            fillAreaRect.offsetMax = new Vector2(-rect.sizeDelta.y * 0.45f, -rect.sizeDelta.y * 0.32f);

            Image fill = CreateAnchoredImage(
                fillAreaRect, "Fill", Vector2.zero, Vector2.zero,
                new Color(0.02f, 0.55f, 1f, 1f), roundedSprite);
            Stretch(fill.rectTransform);

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(rect, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            Stretch(handleAreaRect);

            float handleSize = rect.sizeDelta.y * 0.86f;
            Image handle = CreateAnchoredImage(
                handleAreaRect, "Handle", Vector2.zero,
                new Vector2(handleSize, handleSize),
                new Color(0.88f, 0.93f, 0.96f, 1f), circleSprite);

            Outline outline = handle.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.42f, 0.56f, 0.66f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            background.raycastTarget = true;
            return slider;
        }

        private static Toggle CreateReferenceToggle(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            Color onColor,
            Color offColor,
            bool showKnob)
        {
            RectTransform rect = CreateSourceRect(parent, name, x, y, width, height);
            Image background = rect.gameObject.AddComponent<Image>();
            background.sprite = roundedSprite;
            background.type = Image.Type.Sliced;
            background.color = onColor;

            Toggle toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.isOn = true;
            toggle.transition = Selectable.Transition.None;

            TMP_Text valueText = CreateCenteredText(
                rect, "Value", "ON",
                Mathf.RoundToInt(rect.sizeDelta.y * 0.43f),
                showKnob ? new Vector2(-rect.sizeDelta.x * 0.16f, 0f) : Vector2.zero,
                showKnob ? new Vector2(rect.sizeDelta.x * 0.62f, rect.sizeDelta.y * 0.82f)
                         : new Vector2(rect.sizeDelta.x * 0.86f, rect.sizeDelta.y * 0.82f),
                Color.white);

            Image knob = null;
            float movement = 0f;
            if (showKnob)
            {
                float size = rect.sizeDelta.y * 0.76f;
                movement = rect.sizeDelta.x * 0.31f;
                knob = CreateAnchoredImage(
                    rect, "Knob", new Vector2(movement, 0f), new Vector2(size, size),
                    new Color(0.92f, 0.94f, 0.95f, 1f), circleSprite);

                Outline knobOutline = knob.gameObject.AddComponent<Outline>();
                knobOutline.effectColor = new Color(0.42f, 0.48f, 0.53f, 1f);
                knobOutline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            ReferenceToggleVisual visual = rect.gameObject.AddComponent<ReferenceToggleVisual>();
            visual.Configure(toggle, background, knob, valueText, onColor, offColor, showKnob, movement);
            return toggle;
        }

        private static Button CreateLanguageControl(
            Transform parent,
            float x,
            float y,
            float width,
            float height,
            out TMP_Text valueText)
        {
            RectTransform rect = CreateSourceRect(parent, "LanguageButton", x, y, width, height);
            Image background = rect.gameObject.AddComponent<Image>();
            background.sprite = roundedSprite;
            background.type = Image.Type.Sliced;
            background.color = new Color(0.065f, 0.105f, 0.145f, 1f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            valueText = CreateCenteredText(
                rect, "LanguageValue", "English",
                Mathf.RoundToInt(rect.sizeDelta.y * 0.44f),
                new Vector2(-rect.sizeDelta.x * 0.08f, 0f),
                new Vector2(rect.sizeDelta.x * 0.76f, rect.sizeDelta.y * 0.86f),
                Color.white);

            CreateCenteredText(
                rect, "Arrow", "⌄",
                Mathf.RoundToInt(rect.sizeDelta.y * 0.48f),
                new Vector2(rect.sizeDelta.x * 0.40f, rect.sizeDelta.y * 0.04f),
                new Vector2(rect.sizeDelta.x * 0.14f, rect.sizeDelta.y * 0.80f),
                new Color(0.34f, 0.74f, 1f, 1f));

            return button;
        }

        private static Button CreateInvisibleHotspot(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height)
        {
            RectTransform rect = CreateSourceRect(parent, name, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 0.85f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            return button;
        }

        private static RectTransform CreateSourceRect(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            float scaleX = PanelWidth / SourceWidth;
            float scaleY = PanelHeight / SourceHeight;
            rect.sizeDelta = new Vector2(width * scaleX, height * scaleY);

            float centerX = x + width * 0.5f;
            float centerY = y + height * 0.5f;
            rect.anchoredPosition = new Vector2(
                (centerX - SourceWidth * 0.5f) * scaleX,
                (SourceHeight * 0.5f - centerY) * scaleY);
            return rect;
        }

        private static Image CreateAnchoredImage(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            Sprite sprite)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(Image));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = target.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private static TMP_Text CreateCenteredText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(15f, fontSize - 8f);
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;
            return text;
        }

        private static void RemoveLegacySettingsPopup()
        {
            GameObject existing = GameObject.Find("SettingsPopup");
            if (existing == null) return;
            existing.name = "LegacySettingsPopup_Removed";
            existing.SetActive(false);
            Object.Destroy(existing);
        }

        private static Button FindSettingsButton()
        {
            GameObject hotspot = GameObject.Find("SettingsHotspot");
            if (hotspot != null && hotspot.TryGetComponent(out Button hotspotButton))
            {
                return hotspotButton;
            }

            GameObject namedButton = GameObject.Find("SettingsButton");
            if (namedButton != null && namedButton.TryGetComponent(out Button settingsButton))
            {
                return settingsButton;
            }

            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (Button button in buttons)
            {
                if (button != null && button.name.ToLowerInvariant().Contains("settings"))
                {
                    return button;
                }
            }

            return null;
        }

        private static GameObject CreateFullScreenObject(Transform parent, string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            return root;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite CreateRoundedSprite(int size, float radius)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ReferenceRoundedRect",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float dx = Mathf.Max(radius - px, 0f, px - (size - 1 - radius));
                    float dy = Mathf.Max(radius - py, 0f, py - (size - 1 - radius));
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    pixels[py * size + px] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ReferenceCircle",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f - 1f;

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float distance = Vector2.Distance(new Vector2(px, py), center);
                    float alpha = Mathf.Clamp01(radius + 0.75f - distance);
                    pixels[py * size + px] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
