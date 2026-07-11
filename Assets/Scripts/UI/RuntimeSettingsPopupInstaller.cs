using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DefaultExecutionOrder(1000)]
    public sealed class RuntimeSettingsPopupInstaller : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private static Sprite roundedSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            roundedSprite = null;
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

            RuntimeSettingsPopupInstaller existing = Object.FindFirstObjectByType<RuntimeSettingsPopupInstaller>();
            if (existing != null)
            {
                return;
            }

            GameObject host = new GameObject("RuntimeSettingsUI");
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
                Debug.LogError("Settings UI could not be created because the MainMenu Canvas was not found.");
                return;
            }

            RemoveLegacySettingsPopup();
            roundedSprite ??= CreateRoundedSprite();

            GameObject popupRoot = CreateFullScreenObject(canvas.transform, "SettingsPopup");
            Image dim = popupRoot.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            GameObject panel = CreatePanel(
                popupRoot.transform,
                "Panel",
                new Vector2(0f, -35f),
                new Vector2(930f, 1280f),
                new Color(0.025f, 0.05f, 0.085f, 0.985f)
            );
            AddOutline(panel, new Color(1f, 0.62f, 0.08f, 0.92f), 3f);

            CreateText(panel.transform, "Title", "SETTINGS", 74, new Vector2(0f, 555f), new Vector2(680f, 90f), TextAlignmentOptions.Center, new Color(1f, 0.78f, 0.12f));
            Button closeButton = CreateButton(panel.transform, "CloseButton", "X", new Vector2(386f, 550f), new Vector2(86f, 86f), 42, new Color(0.10f, 0.16f, 0.24f), Color.white);

            GameObject audioSection = CreateSection(panel.transform, "AudioSection", "AUDIO", new Vector2(0f, 270f), new Vector2(840f, 360f));
            GameObject gameplaySection = CreateSection(panel.transform, "GameplaySection", "GAMEPLAY", new Vector2(0f, -105f), new Vector2(840f, 310f));
            GameObject accountSection = CreateSection(panel.transform, "AccountSection", "ACCOUNT", new Vector2(0f, -405f), new Vector2(840f, 205f));

            BuildAudioRow(audioSection.transform, "MusicRow", "MUSIC", new Vector2(0f, 82f), out Slider musicSlider, out Toggle musicToggle, out TMP_Text musicState);
            BuildAudioRow(audioSection.transform, "SfxRow", "SFX", new Vector2(0f, -5f), out Slider sfxSlider, out Toggle sfxToggle, out TMP_Text sfxState);
            BuildAudioRow(audioSection.transform, "VoiceRow", "VOICE", new Vector2(0f, -92f), out Slider voiceSlider, out Toggle voiceToggle, out TMP_Text voiceState);

            BuildToggleRow(gameplaySection.transform, "VibrationRow", "VIBRATION", new Vector2(0f, 64f), out Toggle vibrationToggle, out TMP_Text vibrationState);
            BuildLanguageRow(gameplaySection.transform, new Vector2(0f, -22f), out Button languageButton, out TMP_Text languageValue);
            BuildToggleRow(gameplaySection.transform, "NotificationsRow", "NOTIFICATIONS", new Vector2(0f, -108f), out Toggle notificationsToggle, out TMP_Text notificationsState);

            Button restorePurchasesButton = CreateAccountRow(accountSection.transform, "RestorePurchasesButton", "RESTORE PURCHASES", new Vector2(0f, 25f));
            Button privacyPolicyButton = CreateAccountRow(accountSection.transform, "PrivacyPolicyButton", "PRIVACY POLICY", new Vector2(0f, -55f));

            Button backButton = CreateButton(panel.transform, "BackButton", "BACK", new Vector2(-210f, -575f), new Vector2(350f, 112f), 50, new Color(0.10f, 0.35f, 0.92f), Color.white);
            Button applyButton = CreateButton(panel.transform, "ApplyButton", "APPLY", new Vector2(210f, -575f), new Vector2(350f, 112f), 50, new Color(1f, 0.64f, 0.08f), new Color(0.10f, 0.045f, 0.01f));

            SettingsMenuController controller = GetComponent<SettingsMenuController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<SettingsMenuController>();
            }

            controller.Configure(
                popupRoot,
                closeButton,
                backButton,
                applyButton,
                musicSlider,
                musicToggle,
                musicState,
                sfxSlider,
                sfxToggle,
                sfxState,
                voiceSlider,
                voiceToggle,
                voiceState,
                vibrationToggle,
                vibrationState,
                languageButton,
                languageValue,
                notificationsToggle,
                notificationsState,
                restorePurchasesButton,
                privacyPolicyButton
            );

            Button settingsButton = FindSettingsButton();
            if (settingsButton == null)
            {
                Debug.LogError("Settings UI was created, but SettingsHotspot/SettingsButton was not found.");
                return;
            }

            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(controller.Open);
            Debug.Log("Compact clickable Settings UI installed and connected automatically.");
        }

        private static void RemoveLegacySettingsPopup()
        {
            GameObject existing = GameObject.Find("SettingsPopup");
            if (existing == null)
            {
                return;
            }

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

        private static GameObject CreateSection(Transform parent, string name, string title, Vector2 position, Vector2 size)
        {
            GameObject section = CreatePanel(parent, name, position, size, new Color(0.035f, 0.075f, 0.12f, 0.98f));
            AddOutline(section, new Color(0.16f, 0.54f, 0.98f, 0.7f), 2f);
            CreateText(section.transform, "Header", title, 39, new Vector2(-320f, size.y * 0.5f - 45f), new Vector2(260f, 55f), TextAlignmentOptions.Left, new Color(0.27f, 0.69f, 1f));
            return section;
        }

        private static void BuildAudioRow(
            Transform parent,
            string name,
            string label,
            Vector2 position,
            out Slider slider,
            out Toggle toggle,
            out TMP_Text toggleText)
        {
            GameObject row = CreateRow(parent, name, position);
            CreateText(row.transform, "Label", label, 31, new Vector2(-285f, 0f), new Vector2(200f, 55f), TextAlignmentOptions.Left, Color.white);
            slider = CreateSlider(row.transform, new Vector2(55f, 0f));
            toggle = CreateToggle(row.transform, new Vector2(310f, 0f), out toggleText);
        }

        private static void BuildToggleRow(
            Transform parent,
            string name,
            string label,
            Vector2 position,
            out Toggle toggle,
            out TMP_Text toggleText)
        {
            GameObject row = CreateRow(parent, name, position);
            CreateText(row.transform, "Label", label, 31, new Vector2(-270f, 0f), new Vector2(300f, 55f), TextAlignmentOptions.Left, Color.white);
            toggle = CreateToggle(row.transform, new Vector2(305f, 0f), out toggleText);
        }

        private static void BuildLanguageRow(Transform parent, Vector2 position, out Button button, out TMP_Text valueText)
        {
            GameObject row = CreateRow(parent, "LanguageRow", position);
            CreateText(row.transform, "Label", "LANGUAGE", 31, new Vector2(-270f, 0f), new Vector2(300f, 55f), TextAlignmentOptions.Left, Color.white);

            button = CreateButton(row.transform, "LanguageButton", string.Empty, new Vector2(190f, 0f), new Vector2(330f, 62f), 29, new Color(0.08f, 0.14f, 0.22f), Color.white);
            valueText = CreateText(button.transform, "LanguageValue", "ENGLISH", 28, new Vector2(-25f, 0f), new Vector2(230f, 48f), TextAlignmentOptions.Center, Color.white);
            CreateText(button.transform, "Arrow", ">", 34, new Vector2(125f, 0f), new Vector2(40f, 45f), TextAlignmentOptions.Center, new Color(0.35f, 0.74f, 1f));
        }

        private static Button CreateAccountRow(Transform parent, string name, string label, Vector2 position)
        {
            Button button = CreateButton(parent, name, string.Empty, position, new Vector2(760f, 66f), 28, new Color(0.025f, 0.045f, 0.075f), Color.white);
            CreateText(button.transform, "Label", label, 29, new Vector2(-155f, 0f), new Vector2(500f, 50f), TextAlignmentOptions.Left, Color.white);
            CreateText(button.transform, "Arrow", ">", 37, new Vector2(335f, 0f), new Vector2(35f, 48f), TextAlignmentOptions.Center, new Color(0.55f, 0.79f, 1f));
            return button;
        }

        private static GameObject CreateRow(Transform parent, string name, Vector2 position)
        {
            GameObject row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(780f, 72f);
            return row;
        }

        private static Slider CreateSlider(Transform parent, Vector2 position)
        {
            GameObject root = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = position;
            rootRect.sizeDelta = new Vector2(330f, 28f);

            Image background = CreateImage(root.transform, "Background", Vector2.zero, new Vector2(330f, 22f), new Color(0.015f, 0.025f, 0.045f, 1f));
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            Stretch(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(0f, 3f);
            fillAreaRect.offsetMax = new Vector2(-18f, -3f);

            Image fill = CreateImage(fillArea.transform, "Fill", Vector2.zero, Vector2.zero, new Color(0.10f, 0.55f, 1f, 1f));
            RectTransform fillRect = fill.rectTransform;
            Stretch(fillRect);

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            Stretch(handleAreaRect);

            Image handle = CreateImage(handleArea.transform, "Handle", Vector2.zero, new Vector2(32f, 32f), new Color(0.92f, 0.96f, 1f, 1f));
            AddOutline(handle.gameObject, new Color(0.25f, 0.55f, 0.9f, 1f), 2f);

            Slider slider = root.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            slider.fillRect = fillRect;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            background.raycastTarget = true;
            return slider;
        }

        private static Toggle CreateToggle(Transform parent, Vector2 position, out TMP_Text valueText)
        {
            GameObject root = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(120f, 54f);

            Image background = root.GetComponent<Image>();
            ConfigureRoundedImage(background, new Color(0.05f, 0.10f, 0.17f, 1f));

            Image activeFill = CreateImage(root.transform, "ActiveFill", Vector2.zero, new Vector2(116f, 50f), new Color(0.08f, 0.43f, 0.94f, 1f));
            valueText = CreateText(root.transform, "Value", "ON", 25, Vector2.zero, new Vector2(105f, 44f), TextAlignmentOptions.Center, Color.white);

            Toggle toggle = root.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = activeFill;
            toggle.isOn = true;
            return toggle;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 position,
            Vector2 size,
            int fontSize,
            Color backgroundColor,
            Color textColor)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = root.GetComponent<Image>();
            ConfigureRoundedImage(image, backgroundColor);

            Button button = root.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            button.colors = colors;

            if (!string.IsNullOrWhiteSpace(label))
            {
                CreateText(root.transform, "Text", label, fontSize, Vector2.zero, size - new Vector2(20f, 12f), TextAlignmentOptions.Center, textColor);
            }

            return button;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            ConfigureRoundedImage(panel.GetComponent<Image>(), color);
            return panel;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = imageObject.GetComponent<Image>();
            ConfigureRoundedImage(image, color);
            return image;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            Vector2 position,
            Vector2 size,
            TextAlignmentOptions alignment,
            Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(18, fontSize - 8);
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateFullScreenObject(Transform parent, string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            Stretch(root.GetComponent<RectTransform>());
            return root;
        }

        private static void ConfigureRoundedImage(Image image, Color color)
        {
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
        }

        private static void AddOutline(GameObject target, Color color, float size)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(size, -size);
            outline.useGraphicAlpha = true;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite CreateRoundedSprite()
        {
            const int textureSize = 64;
            const float radius = 14f;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                name = "RuntimeRoundedRect",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float dx = Mathf.Max(radius - x, 0f, x - (textureSize - 1 - radius));
                    float dy = Mathf.Max(radius - y, 0f, y - (textureSize - 1 - radius));
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, textureSize, textureSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius)
            );
            sprite.name = "RuntimeRoundedRectSprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
