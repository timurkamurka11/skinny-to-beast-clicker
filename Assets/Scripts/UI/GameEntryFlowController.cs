using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using SkinnyToBeast.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DisallowMultipleComponent]
    public sealed class GameEntryFlowController : MonoBehaviour
    {
        private const string EntryBackgroundPath =
            "UI/Gameplay/Living/game_entry_door";
        private const string CharacterRigPrefabPath =
            "UI/Gameplay/Living/CharacterRig2D";
        private const string StrengthKey = "game.player.strength";
        private const string GameEntrySceneName = "GameEntry";
        private const string MainMenuSceneName = "MainMenu";
        private const float EntryStartScale = 0.84f;
        private const float EntryEndScale = 0.38f;

        private static GameEntryFlowController instance;

        private CanvasGroup rootGroup;
        private RectTransform backgroundRect;
        private CanvasGroup entryCharacterGroup;
        private RectTransform entryCharacterRoot;
        private CharacterRigController entryRig;
        private CharacterSkinController entrySkin;
        private CharacterRigValidator entryValidator;
        private CharacterVisibilityGate entryVisibilityGate;
        private GameplayAudioController entryAudio;
        private RectTransform characterShadow;
        private Image characterShadowImage;
        private Image doorwayGlow;
        private Image transitionCurtain;
        private TMP_Text statusText;
        private TMP_Text dotsText;
        private readonly List<ResourceRequest> gameplayPreloads = new();
        private Func<bool> openGameplay;
        private Action<bool> completion;
        private AsyncOperation sceneLoadOperation;
        private bool loadDedicatedScene;
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

            if (owner == null || gameplayOpener == null)
            {
                return false;
            }

            GameObject root = new GameObject(
                "GameEntryScreen",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup),
                typeof(Image));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);

            Canvas entryCanvas = root.GetComponent<Canvas>();
            entryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            entryCanvas.overrideSorting = true;
            entryCanvas.sortingOrder = 16000;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image blocker = root.GetComponent<Image>();
            blocker.color = Color.black;
            blocker.raycastTarget = true;

            GameEntryFlowController controller =
                root.AddComponent<GameEntryFlowController>();
            controller.openGameplay = gameplayOpener;
            controller.completion = onComplete;
            controller.loadDedicatedScene =
                SceneManager.GetActiveScene().name != GameEntrySceneName &&
                Application.CanStreamedLevelBeLoaded(GameEntrySceneName);
            if (controller.loadDedicatedScene)
            {
                UnityEngine.Object.DontDestroyOnLoad(root);
            }

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
            BeginGameplayPreload();
            BuildEntryCharacter(root, artIndex);
            entryAudio =
                GetOrAddComponent<GameplayAudioController>(
                    gameObject);
            entryAudio.Configure(false);

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
                "CHARACTER ENTERING",
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

        private void BuildEntryCharacter(
            RectTransform parent,
            int artIndex)
        {
            GameObject prefab =
                Resources.Load<GameObject>(CharacterRigPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "CharacterRig2D.prefab is required for the entry flow.");
            }

            GameObject instance =
                Instantiate(prefab, parent, false);
            entryCharacterRoot =
                instance.GetComponent<RectTransform>();
            if (entryCharacterRoot == null)
            {
                Destroy(instance);
                throw new InvalidOperationException(
                    "CharacterRig2D.prefab has no RectTransform root.");
            }

            entryCharacterRoot.name = "EntryCharacterRoot";
            entryCharacterRoot.anchorMin =
                new Vector2(0.5f, 0.5f);
            entryCharacterRoot.anchorMax =
                new Vector2(0.5f, 0.5f);
            entryCharacterRoot.pivot =
                new Vector2(0.5f, 0.5f);
            entryCharacterRoot.anchoredPosition =
                new Vector2(0f, -260f);
            entryCharacterRoot.sizeDelta =
                new Vector2(720f, 1280f);
            entryCharacterRoot.localScale =
                Vector3.one * EntryStartScale;
            entryCharacterRoot.localRotation =
                Quaternion.identity;

            entryCharacterGroup =
                GetOrAddComponent<CanvasGroup>(
                    entryCharacterRoot.gameObject);
            entryCharacterGroup.alpha = 1f;
            entryCharacterGroup.interactable = false;
            entryCharacterGroup.blocksRaycasts = false;

            CharacterFaceController face =
                GetOrAddComponent<CharacterFaceController>(
                    entryCharacterRoot.gameObject);
            entryRig =
                GetOrAddComponent<CharacterRigController>(
                    entryCharacterRoot.gameObject);
            entryRig.Build(entryCharacterRoot, face);

            entrySkin =
                GetOrAddComponent<CharacterSkinController>(
                    entryCharacterRoot.gameObject);
            entrySkin.Configure(
                entryRig,
                entryCharacterGroup,
                4);
            entrySkin.ApplySkin(
                Mathf.Clamp(artIndex, 0, 3),
                false);
            entryRig.StopLocomotion(CharacterFacing.Back);

            entryValidator =
                GetOrAddComponent<CharacterRigValidator>(
                    entryCharacterRoot.gameObject);
            entryValidator.Configure(entryRig, entrySkin);

            entryVisibilityGate =
                GetOrAddComponent<CharacterVisibilityGate>(
                    entryCharacterRoot.gameObject);
            entryVisibilityGate.Configure(
                entryCharacterRoot,
                entryRig,
                entrySkin,
                entryValidator,
                0.22f,
                0.42f);
        }

        private IEnumerator EntryRoutine()
        {
            opening = true;
            yield return FadeGroup(0f, 1f, 0.22f);

            float rigDeadline = Time.unscaledTime + 4f;
            while (entryVisibilityGate != null &&
                   !entryVisibilityGate.IsReady &&
                   Time.unscaledTime < rigDeadline)
            {
                yield return null;
            }

            if (entryVisibilityGate == null ||
                !entryVisibilityGate.IsReady)
            {
                statusText.text = "CHARACTER RIG ERROR";
                dotsText.text = "●  ○  ○";
                Debug.LogError(
                    "Entry character did not become visible: " +
                    (entryVisibilityGate != null
                        ? entryVisibilityGate.LastError
                        : "visibility gate is missing."),
                    this);
                yield return new WaitForSecondsRealtime(0.8f);
                InvokeCompletion(false);
                yield return FadeGroup(1f, 0f, 0.2f);
                Destroy(gameObject);
                yield break;
            }

            Debug.Log(
                $"Entry character render ready: " +
                $"{entryRig.GetVisibleGraphicCount()} skeletal parts.",
                this);
            entryAudio?.PlayEntryStart();
            statusText.text = "CHARACTER ENTERING";
            yield return new WaitForSecondsRealtime(0.22f);

            Vector2 characterStart = new Vector2(0f, -260f);
            Vector2 characterEnd = new Vector2(0f, 18f);
            Vector2 shadowStart = new Vector2(0f, -560f);
            Vector2 shadowEnd = new Vector2(0f, -115f);
            float elapsed = 0f;
            const float walkDuration = 1.75f;
            int shownPhase = -1;
            int stepIndex = 0;
            float nextStepAt = 0.04f;
            float entryStepSpeed = Mathf.Clamp(
                Vector2.Distance(
                    characterStart,
                    characterEnd) /
                walkDuration /
                420f,
                0.65f,
                1.75f);
            entryRig.PlayEntryWalk(entryStepSpeed);
            while (elapsed < walkDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / walkDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                int phase = Mathf.FloorToInt(elapsed / 0.15f) & 1;
                if (phase != shownPhase)
                {
                    shownPhase = phase;
                    dotsText.text = phase == 0 ? "●  ○  ○" : "●  ●  ○";
                }

                if (elapsed >= nextStepAt)
                {
                    entryAudio?.PlayFootstep(stepIndex++);
                    nextStepAt += 0.25f;
                }

                entryCharacterRoot.anchoredPosition =
                    Vector2.Lerp(characterStart, characterEnd, t);
                characterShadow.anchoredPosition =
                    Vector2.Lerp(shadowStart, shadowEnd, t);
                float scale =
                    Mathf.Lerp(EntryStartScale, EntryEndScale, eased);
                entryCharacterRoot.localScale =
                    Vector3.one * scale;
                characterShadow.localScale = Vector3.one *
                                             Mathf.Lerp(1f, 0.34f, eased);
                yield return null;
            }

            entryRig.StopLocomotion(CharacterFacing.Back);
            dotsText.text = "●  ●  ●";
            statusText.text = "PREPARING THE ROOM";
            entryAudio?.PlayDoorOpen();
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
                entryCharacterGroup.alpha = 1f - eased;
                SetAlpha(characterShadowImage, 0.38f * (1f - eased));
                yield return null;
            }

            statusText.text = "LOADING SAVED BODY";
            yield return WaitForGameplayPreload();
            BeginSceneLoad();
            if (sceneLoadOperation != null)
            {
                statusText.text = "LOADING THE ROOM";
                while (sceneLoadOperation.progress < 0.9f)
                {
                    yield return null;
                }
            }

            // Cover the entry scene before building the room. This hides any
            // one-frame layout/import work and replaces the old cropped-door
            // trick that could expose a solid blue rectangle.
            yield return FadeGraphic(
                transitionCurtain,
                0f,
                1f,
                0.2f);

            if (sceneLoadOperation != null)
            {
                sceneLoadOperation.allowSceneActivation = true;
                while (!sceneLoadOperation.isDone)
                {
                    yield return null;
                }

                // Let the new scene create its root objects before constructing
                // the gameplay canvas above it.
                yield return null;
            }

            bool opened = false;
            try
            {
                opened = openGameplay != null && openGameplay();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not finish game entry: {exception}");
            }

            if (opened)
            {
                float roomDeadline = Time.unscaledTime + 6f;
                while (!GameplayWindowController.IsCharacterReady &&
                       Time.unscaledTime < roomDeadline)
                {
                    yield return null;
                }

                if (!GameplayWindowController.IsCharacterReady)
                {
                    opened = false;
                    Debug.LogError(
                        "The room remained covered because its character " +
                        "did not pass CharacterVisibilityGate: " +
                        GameplayWindowController.CharacterReadinessError,
                        this);
                }
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
                InvokeCompletion(false);
                yield return FadeGroup(1f, 0f, 0.2f);
                if (SceneManager.GetActiveScene().name == GameEntrySceneName &&
                    Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
                {
                    SceneManager.LoadScene(MainMenuSceneName);
                }

                Destroy(gameObject);
                yield break;
            }

            // Give the newly-created room one complete UI layout/render cycle,
            // then reveal it through the black transition curtain.
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            entryAudio?.PlayRoomReveal();
            yield return FadeGroup(1f, 0f, 0.42f);
            opening = false;
            InvokeCompletion(true);
            Destroy(gameObject);
        }

        private void BeginSceneLoad()
        {
            if (!loadDedicatedScene || sceneLoadOperation != null)
            {
                return;
            }

            sceneLoadOperation = SceneManager.LoadSceneAsync(
                GameEntrySceneName,
                LoadSceneMode.Single);
            if (sceneLoadOperation != null)
            {
                sceneLoadOperation.allowSceneActivation = false;
            }
            else
            {
                loadDedicatedScene = false;
            }
        }

        private void BeginGameplayPreload()
        {
            gameplayPreloads.Clear();
            string[] paths =
            {
                "UI/Gameplay/Living/room_stage_01",
                "UI/Gameplay/Living/room_stage_02",
                CharacterRigPrefabPath,
                CharacterAnimationDriver.ControllerResourcePath,
                "UI/Gameplay/Living/dumbbell_stage_01",
                "UI/Gameplay/Living/dumbbell_stage_02",
                "UI/Gameplay/Living/dumbbell_stage_03",
                "UI/Gameplay/Living/prop_protein",
                "UI/Gameplay/Living/prop_coach"
            };

            for (int i = 0; i < paths.Length; i++)
            {
                gameplayPreloads.Add(
                    Resources.LoadAsync<UnityEngine.Object>(paths[i]));
            }
        }

        private IEnumerator WaitForGameplayPreload()
        {
            for (int i = 0; i < gameplayPreloads.Count; i++)
            {
                ResourceRequest request = gameplayPreloads[i];
                if (request == null)
                {
                    continue;
                }

                while (!request.isDone)
                {
                    yield return null;
                }
            }
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

        private void InvokeCompletion(bool success)
        {
            if (completion == null)
            {
                return;
            }

            object target = completion.Target;
            if (target is UnityEngine.Object unityTarget &&
                unityTarget == null)
            {
                return;
            }

            completion.Invoke(success);
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

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : target.AddComponent<T>();
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
