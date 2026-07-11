using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DefaultExecutionOrder(1200)]
    internal sealed class ReferenceSettingsPanelSpriteFix : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string PanelObjectName = "ReferencePanel";
        private const string BackgroundObjectName = "ReferencePanelBackground";
        private const string SurfaceObjectName = "ProceduralReferenceSurface";
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
            if (Object.FindFirstObjectByType<ReferenceSettingsPanelSpriteFix>() != null) return;
            new GameObject("ReferenceSettingsPanelSpriteFix").AddComponent<ReferenceSettingsPanelSpriteFix>();
        }

        private IEnumerator Start()
        {
            float timeout = 10f;
            while (timeout > 0f)
            {
                if (TryApply()) yield break;
                timeout -= 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
            }

            Debug.LogError("Reference settings panel could not be found in the loaded MainMenu scene.");
        }

        private void Update()
        {
            if (Time.frameCount % 45 == 0)
            {
                TryApply();
            }
        }

        private static bool TryApply()
        {
            GameObject panel = FindInactivePanel();
            if (panel == null) return false;

            Image panelBlocker = panel.GetComponent<Image>();
            if (panelBlocker == null) panelBlocker = panel.AddComponent<Image>();
            panelBlocker.sprite = null;
            panelBlocker.color = new Color(1f, 1f, 1f, 0.001f);
            panelBlocker.raycastTarget = true;

            Transform backgroundRoot = panel.transform.Find(BackgroundObjectName);
            if (backgroundRoot == null)
            {
                GameObject root = new GameObject(BackgroundObjectName, typeof(RectTransform));
                root.transform.SetParent(panel.transform, false);
                backgroundRoot = root.transform;
                Stretch(root.GetComponent<RectTransform>());
            }

            backgroundRoot.SetAsFirstSibling();

            RawImage obsoleteRawImage = backgroundRoot.GetComponent<RawImage>();
            if (obsoleteRawImage != null)
            {
                obsoleteRawImage.texture = null;
                obsoleteRawImage.color = Color.clear;
                obsoleteRawImage.raycastTarget = false;
                obsoleteRawImage.enabled = false;
            }

            Transform surface = backgroundRoot.Find(SurfaceObjectName);
            if (surface == null)
            {
                BuildProceduralReference(backgroundRoot, panel.GetComponent<RectTransform>());
            }
            else
            {
                surface.SetAsFirstSibling();
            }

            return true;
        }

        private static void BuildProceduralReference(Transform parent, RectTransform panelRect)
        {
            roundedSprite ??= CreateRoundedSprite(64, 13f);

            GameObject surfaceObject = new GameObject(SurfaceObjectName, typeof(RectTransform), typeof(Image));
            surfaceObject.transform.SetParent(parent, false);
            surfaceObject.transform.SetAsFirstSibling();
            RectTransform surfaceRect = surfaceObject.GetComponent<RectTransform>();
            Stretch(surfaceRect);

            Image surfaceImage = surfaceObject.GetComponent<Image>();
            surfaceImage.sprite = roundedSprite;
            surfaceImage.type = Image.Type.Sliced;
            surfaceImage.color = new Color(0.025f, 0.055f, 0.078f, 0.985f);
            surfaceImage.raycastTarget = false;

            Outline outerBorder = surfaceObject.AddComponent<Outline>();
            outerBorder.effectColor = new Color(0.19f, 0.39f, 0.50f, 0.95f);
            outerBorder.effectDistance = new Vector2(2.5f, -2.5f);
            outerBorder.useGraphicAlpha = false;

            BuildSection(surfaceRect, "AudioSection", "AUDIO", 2f, 2f, 279f, 105f);
            BuildSection(surfaceRect, "GameplaySection", "GAMEPLAY", 2f, 108f, 279f, 108f);
            BuildSection(surfaceRect, "AccountSection", "ACCOUNT", 2f, 218f, 279f, 81f);

            BuildRow(surfaceRect, "MusicRowVisual", "♪", "Music", 7f, 30f, 270f, 25f, false);
            BuildRow(surfaceRect, "SfxRowVisual", "◖", "SFX", 7f, 56f, 270f, 25f, false);
            BuildRow(surfaceRect, "VoiceRowVisual", "◆", "Voice", 7f, 82f, 270f, 25f, false);

            BuildRow(surfaceRect, "VibrationRowVisual", "▣", "Vibration", 7f, 134f, 270f, 27f, false);
            BuildRow(surfaceRect, "LanguageRowVisual", "◎", "Language", 7f, 161f, 270f, 30f, false);
            BuildRow(surfaceRect, "NotificationsRowVisual", "●", "Notifications", 7f, 191f, 270f, 27f, false);

            BuildRow(surfaceRect, "RestoreRowVisual", "↻", "Restore Purchases", 7f, 242f, 270f, 27f, true);
            BuildRow(surfaceRect, "PrivacyRowVisual", "◆", "Privacy Policy", 7f, 269f, 270f, 27f, true);
        }

        private static void BuildSection(
            RectTransform parent,
            string name,
            string title,
            float x,
            float y,
            float width,
            float height)
        {
            RectTransform sectionRect = CreateSourceRect(parent, name, x, y, width, height);
            Image sectionImage = sectionRect.gameObject.AddComponent<Image>();
            sectionImage.sprite = roundedSprite;
            sectionImage.type = Image.Type.Sliced;
            sectionImage.color = new Color(0.035f, 0.082f, 0.115f, 0.96f);
            sectionImage.raycastTarget = false;

            Outline border = sectionRect.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(0.12f, 0.31f, 0.40f, 0.90f);
            border.effectDistance = new Vector2(1.4f, -1.4f);
            border.useGraphicAlpha = false;

            RectTransform header = CreateSourceRect(sectionRect, "Header", 0f, 0f, width, 25f, true);
            Image headerImage = header.gameObject.AddComponent<Image>();
            headerImage.color = new Color(0.025f, 0.060f, 0.086f, 1f);
            headerImage.raycastTarget = false;

            CreateSourceText(
                header,
                "HeaderText",
                title,
                8.5f,
                8f,
                1f,
                width - 16f,
                22f,
                new Color(0.12f, 0.68f, 0.96f, 1f),
                TextAlignmentOptions.Left,
                FontStyles.Bold);
        }

        private static void BuildRow(
            RectTransform parent,
            string name,
            string icon,
            string label,
            float x,
            float y,
            float width,
            float height,
            bool showArrow)
        {
            RectTransform row = CreateSourceRect(parent, name, x, y, width, height);
            Image rowImage = row.gameObject.AddComponent<Image>();
            rowImage.color = new Color(0.025f, 0.065f, 0.092f, 0.92f);
            rowImage.raycastTarget = false;

            CreateSourceText(
                row,
                "Icon",
                icon,
                8.5f,
                6f,
                1f,
                20f,
                height - 2f,
                new Color(0.15f, 0.67f, 0.94f, 1f),
                TextAlignmentOptions.Center,
                FontStyles.Bold);

            CreateSourceText(
                row,
                "Label",
                label,
                8.2f,
                27f,
                1f,
                showArrow ? width - 55f : 73f,
                height - 2f,
                new Color(0.92f, 0.96f, 0.98f, 1f),
                TextAlignmentOptions.Left,
                FontStyles.Bold);

            if (showArrow)
            {
                CreateSourceText(
                    row,
                    "Arrow",
                    "›",
                    13f,
                    width - 29f,
                    0f,
                    22f,
                    height,
                    new Color(0.82f, 0.91f, 0.95f, 1f),
                    TextAlignmentOptions.Center,
                    FontStyles.Bold);
            }
        }

        private static RectTransform CreateSourceRect(
            RectTransform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            bool coordinatesRelativeToParent = false)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            float parentSourceWidth = coordinatesRelativeToParent
                ? Mathf.Max(1f, width)
                : SourceWidth;
            float parentSourceHeight = coordinatesRelativeToParent
                ? Mathf.Max(1f, parent.rect.height / Mathf.Max(0.001f, parent.rect.width) * width)
                : SourceHeight;

            float scaleX = parent.rect.width / parentSourceWidth;
            float scaleY = parent.rect.height / parentSourceHeight;
            rect.sizeDelta = new Vector2(width * scaleX, height * scaleY);

            float centerX = x + width * 0.5f;
            float centerY = y + height * 0.5f;
            rect.anchoredPosition = new Vector2(
                (centerX - parentSourceWidth * 0.5f) * scaleX,
                (parentSourceHeight * 0.5f - centerY) * scaleY);

            return rect;
        }

        private static TMP_Text CreateSourceText(
            RectTransform parent,
            string name,
            string value,
            float sourceFontSize,
            float x,
            float y,
            float width,
            float height,
            Color color,
            TextAlignmentOptions alignment,
            FontStyles style)
        {
            RectTransform rect = CreateSourceRect(parent, name, x, y, width, height, true);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            float scale = Mathf.Max(1f, parent.rect.width / Mathf.Max(1f, width));
            text.text = value;
            text.fontSize = Mathf.Clamp(sourceFontSize * scale * 0.29f, 18f, 38f);
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = 15f;
            text.fontSizeMax = 38f;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static GameObject FindInactivePanel()
        {
            RectTransform[] allRects = Resources.FindObjectsOfTypeAll<RectTransform>();
            foreach (RectTransform rect in allRects)
            {
                if (rect == null || rect.name != PanelObjectName) continue;
                GameObject candidate = rect.gameObject;
                if (!candidate.scene.IsValid() || !candidate.scene.isLoaded) continue;
                return candidate;
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
                name = "SettingsReferenceRoundedRect",
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
