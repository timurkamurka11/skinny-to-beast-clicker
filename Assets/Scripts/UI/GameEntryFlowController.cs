using System;
using System.Collections;
using System.Globalization;
using SkinnyToBeast.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DisallowMultipleComponent]
    public sealed class GameEntryFlowController : MonoBehaviour
    {
        private const string EntryBackgroundPath =
            "UI/Gameplay/Living/game_entry_door";
        private const string WalkSheetRoot =
            "UI/Gameplay/Living/Rig/walk_stage_";
        private const string StrengthKey = "game.player.strength";
        private const float EntryCharacterSize = 720f;
        private const float DirectionalReferenceSize = 1280f;

        private static GameEntryFlowController instance;

        private CanvasGroup rootGroup;
        private RectTransform backgroundRect;
        private RawImage walkingCharacter;
        private RectTransform walkingCharacterRect;
        private RectTransform characterShadow;
        private Image characterShadowImage;
        private Image doorwayGlow;
        private Image transitionCurtain;
        private TMP_Text statusText;
        private TMP_Text dotsText;
        private CharacterDirectionalFrame[] entryDirectionalFrames;
        private Func<bool> openGameplay;
        private Action<bool> completion;
        private bool opening;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        public static bool Show(
            MonoBehaviour owner,
            Func<bool> gameplayOpener,
            Action<bool> onComplete)
        {
            if (instance != null)
            {
                return true;
            }

            Canvas canvas = owner != null
                ? owner.GetComponentInParent<Canvas>()
                : null;
            if (canvas == null)
            {
                canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            }

            if (canvas == null || gameplayOpener == null)
            {
                return false;
            }

            GameObject root = new GameObject(
                "GameEntryScreen",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup),
                typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);

            Canvas entryCanvas = root.GetComponent<Canvas>();
            entryCanvas.overrideSorting = true;
            entryCanvas.sortingOrder = 16000;

            Image blocker = root.GetComponent<Image>();
            blocker.color = Color.black;
            blocker.raycastTarget = true;

            GameEntryFlowController controller =
                root.AddComponent<GameEntryFlowController>();
            controller.openGameplay = gameplayOpener;
            controller.completion = onComplete;
            instance = controller;

            try
            {
                controller.Build(rootRect);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Could not build game entry screen: {exception}",
                    controller);
                instance = null;
                UnityEngine.Object.Destroy(root);
                return false;
            }
        }

        private void Build(RectTransform root)
        {
            rootGroup = GetComponent<CanvasGroup>();
            rootGroup.alpha = 0f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;

            Sprite backgroundSprite =
                Resources.Load<Sprite>(EntryBackgroundPath);
            if (backgroundSprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>(EntryBackgroundPath);
                if (texture != null)
                {
                    backgroundSprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }
            }

            Image background = CreateStretchImage(
                root,
                "EntryBackground",
                backgroundSprite,
                backgroundSprite != null ? Color.white : Color.black);
            backgroundRect = background.rectTransform;
            AspectRatioFitter backgroundFitter =
                background.gameObject.AddComponent<AspectRatioFitter>();
            backgroundFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundFitter.aspectRatio = 9f / 16f;

            Image vignette = CreateStretchImage(
                root,
                "EntryVignette",
                null,
                new Color(0f, 0f, 0f, 0.13f));
            vignette.raycastTarget = false;

            doorwayGlow = CreateImage(
                root,
                "DoorGlow",
                new Vector2(0f, 90f),
                new Vector2(620f, 1120f),
                LivingGameplayVisualFactory.GetSoftCircleSprite(),
                new Color(0.12f, 0.55f, 1f, 0f));

            characterShadow = CreateRect(
                root,
                "EntryCharacterShadow",
                new Vector2(0f, -560f),
                new Vector2(330f, 92f));
            characterShadowImage =
                characterShadow.gameObject.AddComponent<Image>();
            characterShadowImage.sprite =
                LivingGameplayVisualFactory.GetSoftCircleSprite();
            characterShadowImage.color =
                new Color(0f, 0f, 0f, 0.38f);
            characterShadowImage.raycastTarget = false;

            int artIndex = ResolveSavedArtIndex();
            string walkPath = WalkSheetRoot + $"{artIndex + 1:00}";
            Texture2D walkSheet = Resources.Load<Texture2D>(walkPath);
            if (walkSheet == null)
            {
                Sprite walkSprite = Resources.Load<Sprite>(walkPath);
                walkSheet = walkSprite != null ? walkSprite.texture : null;
            }
            entryDirectionalFrames =
                CharacterDirectionalFrame.CreateForStage(artIndex);
            walkingCharacterRect = CreateRect(
                root,
                "EntryCharacter",
                new Vector2(0f, -260f),
                new Vector2(EntryCharacterSize, EntryCharacterSize));
            walkingCharacter = walkingCharacterRect.gameObject.AddComponent<RawImage>();
            walkingCharacter.texture = walkSheet;
            walkingCharacter.color = Color.white;
            walkingCharacter.raycastTarget = false;
            walkingCharacter.enabled = walkSheet != null;
            ApplyEntryWalkFrame(0, 1.05f, new Vector2(0f, -260f));

            RectTransform statusPanel = CreateRect(
                root,
                "EntryStatus",
                new Vector2(0f, -790f),
                new Vector2(850f, 116f));
            Image statusBackground = statusPanel.gameObject.AddComponent<Image>();
            statusBackground.sprite = LivingGameplayVisualFactory.GetRoundedSprite();
            statusBackground.type = Image.Type.Sliced;
            statusBackground.color = new Color(0.015f, 0.028f, 0.05f, 0.83f);
            statusBackground.raycastTarget = false;

            statusText = CreateText(
                statusPanel,
                "EntryStatusText",
                "ENTERING THE GYM",
                34f,
                new Vector2(0f, 17f),
                new Vector2(760f, 54f),
                Color.white);
            dotsText = CreateText(
                statusPanel,
                "EntryDots",
                "●  ○  ○",
                25f,
                new Vector2(0f, -27f),
                new Vector2(360f, 40f),
                new Color(0.15f, 0.61f, 1f, 1f));

            transitionCurtain = CreateStretchImage(
                root,
                "RoomTransitionCurtain",
                null,
                new Color(0f, 0f, 0f, 0f));
            transitionCurtain.transform.SetAsLastSibling();

            StartCoroutine(EntryRoutine());
        }

        private IEnumerator EntryRoutine()
        {
            opening = true;
            yield return FadeGroup(0f, 1f, 0.22f);

            Vector2 characterStart = new Vector2(0f, -260f);
            Vector2 characterEnd = new Vector2(0f, 18f);
            Vector2 shadowStart = new Vector2(0f, -560f);
            Vector2 shadowEnd = new Vector2(0f, -115f);
            float elapsed = 0f;
            const float walkDuration = 1.35f;
            int shownFrame = -1;
            while (elapsed < walkDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / walkDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                int frame = Mathf.FloorToInt(elapsed / 0.15f) & 1;
                if (frame != shownFrame)
                {
                    shownFrame = frame;
                    dotsText.text = frame == 0 ? "●  ○  ○" : "●  ●  ○";
                }

                Vector2 characterPosition =
                    Vector2.Lerp(characterStart, characterEnd, eased);
                characterShadow.anchoredPosition =
                    Vector2.Lerp(shadowStart, shadowEnd, eased);
                float scale = Mathf.Lerp(1.05f, 0.38f, eased);
                ApplyEntryWalkFrame(frame, scale, characterPosition);
                characterShadow.localScale = Vector3.one *
                                             Mathf.Lerp(1f, 0.34f, eased);
                yield return null;
            }

            dotsText.text = "●  ●  ●";
            statusText.text = "PREPARING THE ROOM";
            yield return new WaitForSecondsRealtime(0.1f);

            elapsed = 0f;
            const float approachDuration = 0.42f;
            while (elapsed < approachDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / approachDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);

                SetAlpha(doorwayGlow, eased * 0.32f);
                backgroundRect.localScale =
                    Vector3.one * Mathf.Lerp(1f, 1.045f, eased);
                walkingCharacter.color = WithAlpha(
                    walkingCharacter.color,
                    1f - eased);
                SetAlpha(characterShadowImage, 0.38f * (1f - eased));
                yield return null;
            }

            // Cover the entry scene before building the room. This hides any
            // one-frame layout/import work and replaces the old cropped-door
            // trick that could expose a solid blue rectangle.
            yield return FadeGraphic(
                transitionCurtain,
                0f,
                1f,
                0.2f);

            bool opened = false;
            try
            {
                opened = openGameplay != null && openGameplay();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not finish game entry: {exception}");
            }

            if (!opened)
            {
                yield return FadeGraphic(
                    transitionCurtain,
                    1f,
                    0f,
                    0.2f);
                statusText.text = "COULD NOT ENTER";
                dotsText.text = "●  ○  ○";
                yield return new WaitForSecondsRealtime(0.55f);
                completion?.Invoke(false);
                yield return FadeGroup(1f, 0f, 0.2f);
                Destroy(gameObject);
                yield break;
            }

            // Give the newly-created room one complete UI layout/render cycle,
            // then reveal it through the black transition curtain.
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            yield return FadeGroup(1f, 0f, 0.42f);
            opening = false;
            completion?.Invoke(true);
            Destroy(gameObject);
        }

        private void ApplyEntryWalkFrame(
            int frame,
            float depthScale,
            Vector2 pathPosition)
        {
            int safeFrame = Mathf.Abs(frame) % 2;
            CharacterDirectionalFrame calibration =
                entryDirectionalFrames != null &&
                entryDirectionalFrames.Length >= 4
                    ? entryDirectionalFrames[2 + safeFrame]
                    : CharacterDirectionalFrame.Default;
            walkingCharacter.uvRect = new Rect(
                safeFrame == 0 ? 0f : 0.5f,
                0f,
                0.5f,
                0.5f);

            float safeDepth = Mathf.Max(0.01f, depthScale);
            Vector2 calibratedOffset = calibration.Offset *
                                       (EntryCharacterSize /
                                        DirectionalReferenceSize) *
                                       safeDepth;
            walkingCharacterRect.anchoredPosition =
                pathPosition + calibratedOffset;
            walkingCharacterRect.localScale =
                Vector3.one * (safeDepth * calibration.Scale);
        }

        private IEnumerator FadeGroup(float from, float to, float duration)
        {
            float elapsed = 0f;
            rootGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                rootGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            rootGroup.alpha = to;
        }

        private static IEnumerator FadeGraphic(
            Graphic graphic,
            float from,
            float to,
            float duration)
        {
            if (graphic == null)
            {
                yield break;
            }

            float elapsed = 0f;
            SetAlpha(graphic, from);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(
                    elapsed / Mathf.Max(0.01f, duration));
                SetAlpha(
                    graphic,
                    Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }

            SetAlpha(graphic, to);
        }

        private static int ResolveSavedArtIndex()
        {
            string raw = PlayerPrefs.GetString(StrengthKey, string.Empty);
            double strength = 0d;
            if (!string.IsNullOrEmpty(raw))
            {
                double.TryParse(
                    raw,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out strength);
            }

            return CharacterSkinDefinition.ResolveArtIndexForStrength(
                Math.Max(0d, strength));
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            if (opening)
            {
                opening = false;
            }
        }

        private static RectTransform CreateRect(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image CreateStretchImage(
            Transform parent,
            string name,
            Sprite sprite,
            Color color)
        {
            GameObject target = new GameObject(name, typeof(RectTransform), typeof(Image));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            Stretch(rect);
            Image image = target.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateImage(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size,
            Sprite sprite,
            Color color)
        {
            RectTransform rect = CreateRect(parent, name, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            RectTransform rect = CreateRect(parent, name, position, size);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = fontSize;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic != null)
            {
                graphic.color = WithAlpha(graphic.color, alpha);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }
    }
}
