using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DefaultExecutionOrder(2000)]
    internal sealed class ReferenceSettingsPanelHardFix : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string PanelName = "ReferencePanel";
        private const string HardBackgroundName = "SettingsReferenceHardBackground";
        private const float SourceWidth = 283f;
        private const float SourceHeight = 301f;

        private static Sprite roundedSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            roundedSprite = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInitial()
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
            if (Object.FindFirstObjectByType<ReferenceSettingsPanelHardFix>() != null) return;
            new GameObject("ReferenceSettingsPanelHardFix").AddComponent<ReferenceSettingsPanelHardFix>();
        }

        private IEnumerator Start()
        {
            float timeout = 12f;
            while (timeout > 0f)
            {
                if (ApplyHardFix()) yield break;
                timeout -= 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
            }

            Debug.LogError("Hard settings UI fix: ReferencePanel was not created.");
        }

        private void LateUpdate()
        {
            if (Time.frameCount % 60 == 0)
            {
                ApplyHardFix();
            }
        }

        private static bool ApplyHardFix()
        {
            RectTransform panel = FindInactivePanel();
            if (panel == null) return false;

            Image panelImage = panel.GetComponent<Image>();
            if (panelImage == null) panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = null;
            panelImage.color = new Color(1f, 1f, 1f, 0.001f);
            panelImage.raycastTarget = true;

            Transform obsoleteBackground = panel.Find("ReferencePanelBackground");
            if (obsoleteBackground != null)
            {
                RawImage[] oldRawImages = obsoleteBackground.GetComponentsInChildren<RawImage>(true);
                foreach (RawImage rawImage in oldRawImages)
                {
                    if (rawImage == null) continue;
                    rawImage.texture = null;
                    rawImage.color = Color.clear;
                    rawImage.enabled = false;
                    rawImage.raycastTarget = false;
                }

                obsoleteBackground.gameObject.SetActive(false);
            }

            Transform existing = panel.Find(HardBackgroundName);
            if (existing == null)
            {
                BuildBackground(panel);
            }
            else
            {
                existing.gameObject.SetActive(true);
                existing.SetAsFirstSibling();
            }

            return true;
        }

        private static void BuildBackground(RectTransform panel)
        {
            roundedSprite ??= CreateRoundedSprite(64, 12f);

            GameObject rootObject = new GameObject(HardBackgroundName, typeof(RectTransform), typeof(Image));
            rootObject.transform.SetParent(panel, false);
            rootObject.transform.SetAsFirstSibling();

            RectTransform root = rootObject.GetComponent<RectTransform>();
            Stretch(root);

            Image rootImage = rootObject.GetComponent<Image>();
            rootImage.sprite = roundedSprite;
            rootImage.type = Image.Type.Sliced;
            rootImage.color = new Color(0.022f, 0.050f, 0.071f, 0.995f);
            rootImage.raycastTarget = false;

            Outline outerOutline = rootObject.AddComponent<Outline>();
            outerOutline.effectColor = new Color(0.17f, 0.36f, 0.47f, 1f);
            outerOutline.effectDistance = new Vector2(2.2f, -2.2f);
            outerOutline.useGraphicAlpha = false;

            AddSection(root, "Audio", 2f, 2f, 279f, 105f);
            AddSection(root, "Gameplay", 2f, 109f, 279f, 107f);
            AddSection(root, "Account", 2f, 218f, 279f, 81f);

            AddHeader(root, "AUDIO", 2f, 2f, 279f, 25f);
            AddHeader(root, "GAMEPLAY", 2f, 109f, 279f, 25f);
            AddHeader(root, "ACCOUNT", 2f, 218f, 279f, 25f);

            AddRow(root, 7f, 30f, 270f, 25f);
            AddRow(root, 7f, 56f, 270f, 25f);
            AddRow(root, 7f, 82f, 270f, 25f);
            AddRow(root, 7f, 134f, 270f, 27f);
            AddRow(root, 7f, 162f, 270f, 29f);
            AddRow(root, 7f, 191f, 270f, 27f);
            AddRow(root, 7f, 243f, 270f, 26f);
            AddRow(root, 7f, 270f, 270f, 26f);

            AddIconAndLabel(root, "♪", "Music", 8f, 30f, 25f);
            AddIconAndLabel(root, "◖", "SFX", 8f, 56f, 25f);
            AddIconAndLabel(root, "◆", "Voice", 8f, 82f, 25f);
            AddIconAndLabel(root, "▣", "Vibration", 8f, 134f, 27f);
            AddIconAndLabel(root, "◎", "Language", 8f, 162f, 29f);
            AddIconAndLabel(root, "●", "Notifications", 8f, 191f, 27f);
            AddIconAndLabel(root, "↻", "Restore Purchases", 8f, 243f, 26f);
            AddIconAndLabel(root, "◆", "Privacy Policy", 8f, 270f, 26f);

            AddText(root, "RestoreArrow", "›", 13f, 254f, 243f, 20f, 26f,
                new Color(0.82f, 0.91f, 0.95f, 1f), TextAlignmentOptions.Center);
            AddText(root, "PrivacyArrow", "›", 13f, 254f, 270f, 20f, 26f,
                new Color(0.82f, 0.91f, 0.95f, 1f), TextAlignmentOptions.Center);

            Debug.Log("Hard settings UI fix applied: procedural reference panel created.");
        }

        private static void AddSection(RectTransform parent, string name, float x, float y, float width, float height)
        {
            RectTransform rect = AddRect(parent, name + "Section", x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.029f, 0.074f, 0.106f, 0.98f);
            image.raycastTarget = false;

            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.10f, 0.30f, 0.40f, 0.95f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            outline.useGraphicAlpha = false;
        }

        private static void AddHeader(RectTransform parent, string title, float x, float y, float width, float height)
        {
            RectTransform rect = AddRect(parent, title + "HeaderBar", x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.020f, 0.052f, 0.075f, 1f);
            image.raycastTarget = false;

            AddText(parent, title + "HeaderText", title, 9f, x + 7f, y + 1f, width - 14f, height - 2f,
                new Color(0.12f, 0.68f, 0.96f, 1f), TextAlignmentOptions.Left);
        }

        private static void AddRow(RectTransform parent, float x, float y, float width, float height)
        {
            RectTransform rect = AddRect(parent, "Row_" + y, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.022f, 0.061f, 0.087f, 0.95f);
            image.raycastTarget = false;
        }

        private static void AddIconAndLabel(RectTransform parent, string icon, string label, float x, float y, float height)
        {
            AddText(parent, label + "Icon", icon, 8.5f, x, y, 20f, height,
                new Color(0.15f, 0.67f, 0.94f, 1f), TextAlignmentOptions.Center);
            AddText(parent, label + "Label", label, 8.2f, x + 23f, y, 105f, height,
                new Color(0.92f, 0.96f, 0.98f, 1f), TextAlignmentOptions.Left);
        }

        private static TMP_Text AddText(
            RectTransform parent,
            string name,
            string value,
            float sourceFontSize,
            float x,
            float y,
            float width,
            float height,
            Color color,
            TextAlignmentOptions alignment)
        {
            RectTransform rect = AddRect(parent, name, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;

            float scale = parent.rect.width / SourceWidth;
            text.text = value;
            text.fontSize = sourceFontSize * scale;
            text.fontStyle = FontStyles.Bold;
            text.alignment = alignment;
            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(14f, sourceFontSize * scale * 0.72f);
            text.fontSizeMax = sourceFontSize * scale;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static RectTransform AddRect(
            RectTransform parent,
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

            float scaleX = parent.rect.width / SourceWidth;
            float scaleY = parent.rect.height / SourceHeight;
            rect.sizeDelta = new Vector2(width * scaleX, height * scaleY);

            float centerX = x + width * 0.5f;
            float centerY = y + height * 0.5f;
            rect.anchoredPosition = new Vector2(
                (centerX - SourceWidth * 0.5f) * scaleX,
                (SourceHeight * 0.5f - centerY) * scaleY);
            return rect;
        }

        private static RectTransform FindInactivePanel()
        {
            RectTransform[] allRects = Resources.FindObjectsOfTypeAll<RectTransform>();
            foreach (RectTransform rect in allRects)
            {
                if (rect == null || rect.name != PanelName) continue;
                if (!rect.gameObject.scene.IsValid() || !rect.gameObject.scene.isLoaded) continue;
                return rect;
            }

            return null;
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
                name = "SettingsHardFixRoundedRect",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float dx = Mathf.Max(Mathf.Max(radius - px, 0f), px - (size - 1 - radius));
                    float dy = Mathf.Max(Mathf.Max(radius - py, 0f), py - (size - 1 - radius));
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                    pixels[py * size + px] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
