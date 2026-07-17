using System;
using System.Collections;
using System.Collections.Generic;
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
        private const float SourceWidth = 283f;
        private const float SourceHeight = 301f;
        private const float BlueBadgeScaleX = 1.22f;
        private const float BlueBadgeScaleY = 1.28f;
        private const float OrangeBadgeScaleX = 1.24f;
        private const float OrangeBadgeScaleY = 1.30f;
        private static ExactSettingsDuplicateVisualFix instance;
        private static Sprite referenceSprite;
        private static readonly Dictionary<int, BadgeCoverLayout> BadgeLayouts =
            new Dictionary<int, BadgeCoverLayout>();
        private static int loggedBadgeCount;

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
            BadgeLayouts.Clear();
            loggedBadgeCount = 0;
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
            // Enforce the calibrated positions after every other UI/layout update.
            ApplySingleLayerFix();
        }

        private static void ApplySingleLayerFix()
        {
            DisableLegacyBackgroundBuilders();

            RectTransform panel = FindPanel();
            if (panel == null)
            {
                CoverVisibleStateBadges();
                return;
            }

            DisableDuplicateSettingsControls(panel);
            ForceVisibleControlLayout();

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

            // The former edge-trim approach is intentionally disabled:
            // live controls are now calibrated directly onto the baked wells.
            Transform edgeCleanup = FindChildRecursive(panel, "ControlEdgeCleanup");
            if (edgeCleanup != null)
            {
                edgeCleanup.gameObject.SetActive(false);
            }

            // Run last: locally installed image-driven patches may rebuild or
            // reposition their controls earlier in the same frame.
            CoverVisibleStateBadges();
        }

        private static void ForceVisibleControlLayout()
        {
            RectTransform[] allRects = Resources.FindObjectsOfTypeAll<RectTransform>();
            foreach (RectTransform rect in allRects)
            {
                if (rect == null || !rect.gameObject.scene.IsValid()) continue;

                switch (rect.name)
                {
                    case "MusicToggle":
                        SetSourceLayout(rect, 239f, 37f, 40f, 18f);
                        break;
                    case "SfxToggle":
                        SetSourceLayout(rect, 239f, 63f, 40f, 18f);
                        break;
                    case "VoiceToggle":
                        SetSourceLayout(rect, 239f, 89f, 40f, 18f);
                        break;
                    case "VibrationToggle":
                        SetSourceLayout(rect, 225f, 144f, 57f, 20f);
                        break;
                    case "NotificationsToggle":
                        SetSourceLayout(rect, 225f, 204f, 57f, 20f);
                        break;
                }
            }
        }

        private static void SetSourceLayout(
            RectTransform rect,
            float x,
            float y,
            float width,
            float height)
        {
            RectTransform panel = FindReferencePanelAncestor(rect);
            if (panel == null) return;

            float scaleX = panel.rect.width / SourceWidth;
            float scaleY = panel.rect.height / SourceHeight;
            rect.sizeDelta = new Vector2(width * scaleX, height * scaleY);

            float centerX = x + width * 0.5f;
            float centerY = y + height * 0.5f;
            rect.anchoredPosition = new Vector2(
                (centerX - SourceWidth * 0.5f) * scaleX,
                (SourceHeight * 0.5f - centerY) * scaleY);
            rect.SetAsLastSibling();
        }

        private static void CoverVisibleStateBadges()
        {
            int matchedCount = 0;

            TMP_Text[] tmpLabels = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (TMP_Text label in tmpLabels)
            {
                if (label == null || !label.gameObject.scene.IsValid()) continue;
                if (!label.gameObject.activeInHierarchy || !IsStateLabel(label.text)) continue;

                RectTransform badge = FindBadgeVisualRoot(label.rectTransform);
                if (badge != null && ApplyBadgeCover(badge))
                {
                    matchedCount++;
                }
            }

            Text[] legacyLabels = Resources.FindObjectsOfTypeAll<Text>();
            foreach (Text label in legacyLabels)
            {
                if (label == null || !label.gameObject.scene.IsValid()) continue;
                if (!label.gameObject.activeInHierarchy || !IsStateLabel(label.text)) continue;

                RectTransform badge = FindBadgeVisualRoot(label.rectTransform);
                if (badge != null && ApplyBadgeCover(badge))
                {
                    matchedCount++;
                }
            }

            if (matchedCount > loggedBadgeCount)
            {
                loggedBadgeCount = matchedCount;
                Debug.Log(
                    $"ExactSettingsDuplicateVisualFix: enlarged {matchedCount} ON/OFF badge visuals only.");
            }
        }

        private static bool ApplyBadgeCover(RectTransform rect)
        {
            int id = rect.GetInstanceID();
            if (!BadgeLayouts.TryGetValue(id, out BadgeCoverLayout layout) ||
                layout.Rect != rect)
            {
                Vector2 baseSize = rect.rect.size;
                float aspect = baseSize.y > 0.01f ? baseSize.x / baseSize.y : 0f;
                bool isOrangeGameplayBadge = aspect >= 2.55f;

                layout = new BadgeCoverLayout(
                    rect,
                    isOrangeGameplayBadge ? OrangeBadgeScaleX : BlueBadgeScaleX,
                    isOrangeGameplayBadge ? OrangeBadgeScaleY : BlueBadgeScaleY);
                BadgeLayouts[id] = layout;
            }

            layout.Apply();
            return true;
        }

        private static RectTransform FindBadgeVisualRoot(RectTransform labelRect)
        {
            Canvas canvas = labelRect.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            float maxWidth = canvasRect != null ? canvasRect.rect.width * 0.32f : 360f;
            float maxHeight = canvasRect != null ? canvasRect.rect.height * 0.12f : 180f;

            Transform current = labelRect.parent;
            for (int depth = 0; current != null && depth < 5; depth++, current = current.parent)
            {
                if (current.GetComponent<Canvas>() != null) break;

                RectTransform candidate = current as RectTransform;
                if (candidate == null) continue;

                float width = Mathf.Abs(candidate.rect.width);
                float height = Mathf.Abs(candidate.rect.height);
                if (height < 0.01f) continue;

                float aspect = width / height;
                bool badgeSized =
                    width >= 16f &&
                    height >= 8f &&
                    width <= maxWidth &&
                    height <= maxHeight &&
                    aspect >= 1.35f &&
                    aspect <= 6f;

                // The actual pill owns its colored Image. Requiring it prevents
                // the full-screen backdrop Button from ever being scaled.
                if (badgeSized && candidate.GetComponent<Image>() != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool IsStateLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            string normalized = value.Trim();
            return string.Equals(normalized, "ON", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(normalized, "OFF", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class BadgeCoverLayout
        {
            internal readonly RectTransform Rect;
            private readonly Vector2 baseAnchoredPosition;
            private readonly Vector2 baseSize;
            private readonly Vector3 baseScale;
            private readonly float scaleX;
            private readonly float scaleY;

            internal BadgeCoverLayout(RectTransform rect, float scaleX, float scaleY)
            {
                Rect = rect;
                baseAnchoredPosition = rect.anchoredPosition;
                baseSize = rect.rect.size;
                baseScale = rect.localScale;
                this.scaleX = scaleX;
                this.scaleY = scaleY;
            }

            internal void Apply()
            {
                if (Rect == null) return;

                Rect.localScale = new Vector3(
                    baseScale.x * scaleX,
                    baseScale.y * scaleY,
                    baseScale.z);

                // Keep the original top-left edge fixed. All added coverage
                // extends toward the protruding baked layer: right and down.
                float shiftX =
                    baseSize.x * baseScale.x * (scaleX - 1f) * Rect.pivot.x;
                float shiftY =
                    baseSize.y * baseScale.y * (scaleY - 1f) * (1f - Rect.pivot.y);
                Rect.anchoredPosition =
                    baseAnchoredPosition + new Vector2(shiftX, -shiftY);
                Rect.SetAsLastSibling();
            }
        }

        private static RectTransform FindReferencePanelAncestor(Transform source)
        {
            Transform current = source;
            while (current != null)
            {
                if (current.name == "ReferencePanel")
                {
                    return current as RectTransform;
                }

                current = current.parent;
            }

            return null;
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
