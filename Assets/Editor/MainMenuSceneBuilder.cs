#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SkinnyToBeast.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;
using UnityEngine.Video;

namespace SkinnyToBeast.EditorTools
{
    public static class MainMenuSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameplayScenePath = "Assets/Scenes/Main.unity";
        private const string VideoPath = "Assets/Videos/MainMenuLoop.mp4";
        private const string RenderTexturePath = "Assets/Videos/MainMenuRenderTexture.renderTexture";
        private const string RoundedSpritePath = "Assets/GeneratedUI/RoundedRect.png";

        private static readonly Color Navy = new Color(0.025f, 0.045f, 0.085f, 0.94f);
        private static readonly Color NavySoft = new Color(0.045f, 0.085f, 0.16f, 0.94f);
        private static readonly Color Blue = new Color(0.08f, 0.33f, 0.93f, 1f);
        private static readonly Color Gold = new Color(1f, 0.61f, 0.03f, 1f);
        private static readonly Color GoldLight = new Color(1f, 0.82f, 0.1f, 1f);
        private static readonly Color WhiteSoft = new Color(0.88f, 0.93f, 1f, 1f);

        [MenuItem("Tools/Skinny To Beast/Create Animated Main Menu Scene")]
        [MenuItem("Tools/Skinny To Beast/Create Video Main Menu Scene")]
        public static void CreateAnimatedMainMenuScene()
        {
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Videos");
            EnsureFolder("Assets", "GeneratedUI");

            Sprite roundedSprite = CreateOrLoadRoundedSprite();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            CreateMainCamera();
            Canvas canvas = CreateCanvas();
            RenderTexture renderTexture = CreateOrLoadRenderTexture();
            RawImage videoImage = CreateVideoBackground(canvas.transform, renderTexture);
            CreateStretchImage(canvas.transform, "VideoDarkener", new Color(0.005f, 0.012f, 0.025f, 0.24f), false);
            CreateTopShade(canvas.transform);
            CreateBottomShade(canvas.transform);

            GameObject controllerObject = CreateController(videoImage, renderTexture);
            MainMenuController menuController = controllerObject.GetComponent<MainMenuController>();

            RectTransform safeArea = CreateSafeArea(canvas.transform);
            RectTransform uiRoot = CreateUiRoot(safeArea);

            CreateTopBar(uiRoot, menuController, roundedSprite);
            CreateHeroCaption(uiRoot, roundedSprite);
            CreateActionDeck(uiRoot, menuController, roundedSprite);

            PopupPanelAnimator settingsPopup = CreateSettingsPopup(
                safeArea,
                menuController,
                roundedSprite,
                out TMP_Text musicValue,
                out TMP_Text sfxValue,
                out TMP_Text vibrationValue
            );

            PopupPanelAnimator shopPopup = CreateShopPopup(safeArea, menuController, roundedSprite);
            PopupPanelAnimator messagePopup = CreateMessagePopup(
                safeArea,
                menuController,
                roundedSprite,
                out TMP_Text messageTitle,
                out TMP_Text messageBody
            );

            AssignReference(menuController, "settingsPanel", settingsPopup);
            AssignReference(menuController, "shopPanel", shopPopup);
            AssignReference(menuController, "messagePanel", messagePopup);
            AssignReference(menuController, "musicValueText", musicValue);
            AssignReference(menuController, "sfxValueText", sfxValue);
            AssignReference(menuController, "vibrationValueText", vibrationValue);
            AssignReference(menuController, "messageTitleText", messageTitle);
            AssignReference(menuController, "messageBodyText", messageBody);

            CreateVideoMissingHint(safeArea, roundedSprite);
            CreateEventSystem();

            Selection.activeGameObject = controllerObject;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Animated Skinny To Beast main menu created: {ScenePath}");
        }

        private static void CreateMainCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.005f, 0.008f, 0.015f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;
            return canvas;
        }

        private static RectTransform CreateSafeArea(Transform parent)
        {
            GameObject safeAreaObject = new GameObject("SafeArea");
            safeAreaObject.transform.SetParent(parent, false);
            RectTransform rect = safeAreaObject.AddComponent<RectTransform>();
            Stretch(rect);
            safeAreaObject.AddComponent<SafeAreaFitter>();
            return rect;
        }

        private static RectTransform CreateUiRoot(Transform parent)
        {
            GameObject rootObject = new GameObject("AnimatedUI");
            rootObject.transform.SetParent(parent, false);
            RectTransform rect = rootObject.AddComponent<RectTransform>();
            Stretch(rect);
            CanvasGroup group = rootObject.AddComponent<CanvasGroup>();
            MainMenuIntroAnimator animator = rootObject.AddComponent<MainMenuIntroAnimator>();
            AssignReference(animator, "canvasGroup", group);
            AssignReference(animator, "content", rect);
            return rect;
        }

        private static RenderTexture CreateOrLoadRenderTexture()
        {
            RenderTexture renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(1080, 1920, 0, RenderTextureFormat.ARGB32)
                {
                    name = "MainMenuRenderTexture",
                    antiAliasing = 1,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                AssetDatabase.CreateAsset(renderTexture, RenderTexturePath);
            }
            else
            {
                renderTexture.Release();
                renderTexture.width = 1080;
                renderTexture.height = 1920;
                renderTexture.depth = 0;
                renderTexture.format = RenderTextureFormat.ARGB32;
                EditorUtility.SetDirty(renderTexture);
            }

            return renderTexture;
        }

        private static RawImage CreateVideoBackground(Transform parent, RenderTexture renderTexture)
        {
            GameObject backgroundObject = new GameObject("VideoBackground");
            backgroundObject.transform.SetParent(parent, false);
            backgroundObject.transform.SetAsFirstSibling();

            RectTransform rect = backgroundObject.AddComponent<RectTransform>();
            Stretch(rect);

            RawImage rawImage = backgroundObject.AddComponent<RawImage>();
            rawImage.texture = renderTexture;
            rawImage.color = Color.white;
            rawImage.raycastTarget = false;
            rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            return rawImage;
        }

        private static GameObject CreateController(RawImage videoImage, RenderTexture renderTexture)
        {
            GameObject controllerObject = new GameObject("MainMenuController");
            MainMenuVideoController videoController = controllerObject.AddComponent<MainMenuVideoController>();
            MainMenuController menuController = controllerObject.AddComponent<MainMenuController>();
            VideoPlayer player = controllerObject.GetComponent<VideoPlayer>();
            VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);

            AssignReference(videoController, "targetImage", videoImage);
            AssignReference(videoController, "menuLoopClip", clip);
            AssignReference(videoController, "targetTexture", renderTexture);
            AssignBool(videoController, "stretchToPortraitScreen", true);
            AssignString(menuController, "gameplaySceneName", "Main");

            player.source = VideoSource.VideoClip;
            player.clip = clip;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = renderTexture;
            player.aspectRatio = VideoAspectRatio.Stretch;
            player.isLooping = true;
            player.playOnAwake = true;
            player.skipOnDrop = true;
            player.audioOutputMode = VideoAudioOutputMode.None;
            return controllerObject;
        }

        private static void CreateTopShade(Transform parent)
        {
            Image shade = CreateAnchoredImage(
                parent,
                "TopShade",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -170f),
                new Vector2(1080f, 420f),
                new Color(0.005f, 0.012f, 0.03f, 0.58f)
            );
            shade.raycastTarget = false;
        }

        private static void CreateBottomShade(Transform parent)
        {
            Image shade = CreateAnchoredImage(
                parent,
                "BottomShade",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 300f),
                new Vector2(1080f, 820f),
                new Color(0.005f, 0.012f, 0.03f, 0.7f)
            );
            shade.raycastTarget = false;
        }

        private static void CreateTopBar(Transform parent, MainMenuController controller, Sprite roundedSprite)
        {
            GameObject currency = CreatePanel(parent, "CurrencyPanel", new Vector2(-345f, 835f), new Vector2(330f, 100f), Navy, roundedSprite, false);
            AddOutline(currency, Gold, 3f);
            CreateText(currency.transform, "CurrencyText", "POWER   10.2K", 34, Vector2.zero, WhiteSoft, TextAlignmentOptions.Center, new Vector2(300f, 75f));

            Button dailyButton = CreateButton(parent, "DailyButton", "DAILY", new Vector2(145f, 835f), new Vector2(210f, 100f), 32, NavySoft, Color.white, roundedSprite, false);
            Button settingsButton = CreateButton(parent, "SettingsButton", "SETTINGS", new Vector2(380f, 835f), new Vector2(260f, 100f), 31, Blue, Color.white, roundedSprite, false);
            UnityEventTools.AddPersistentListener(dailyButton.onClick, controller.ClaimDailyReward);
            UnityEventTools.AddPersistentListener(settingsButton.onClick, controller.OpenSettings);
        }

        private static void CreateHeroCaption(Transform parent, Sprite roundedSprite)
        {
            GameObject caption = CreatePanel(parent, "HeroCaption", new Vector2(0f, 390f), new Vector2(790f, 105f), new Color(0.02f, 0.05f, 0.11f, 0.76f), roundedSprite, false);
            AddOutline(caption, new Color(0.1f, 0.52f, 1f, 0.75f), 2f);
            TMP_Text captionText = CreateText(caption.transform, "CaptionText", "FROM SKINNY TO GYM LEGEND", 34, Vector2.zero, Color.white, TextAlignmentOptions.Center, new Vector2(740f, 75f));
            captionText.fontStyle = FontStyles.Bold;
        }

        private static void CreateActionDeck(Transform parent, MainMenuController controller, Sprite roundedSprite)
        {
            GameObject deck = CreatePanel(parent, "ActionDeck", new Vector2(0f, -575f), new Vector2(1000f, 700f), Navy, roundedSprite, true);
            AddOutline(deck, new Color(0.11f, 0.43f, 1f, 0.72f), 3f);
            AddShadow(deck, new Color(0f, 0f, 0f, 0.65f), new Vector2(0f, -14f));

            TMP_Text eyebrow = CreateText(deck.transform, "Eyebrow", "YOUR TRANSFORMATION STARTS NOW", 28, new Vector2(0f, 286f), new Color(0.58f, 0.76f, 1f), TextAlignmentOptions.Center, new Vector2(880f, 55f));
            eyebrow.fontStyle = FontStyles.Bold;

            Button startButton = CreateButton(deck.transform, "StartButton", "START TRAINING", new Vector2(0f, 165f), new Vector2(840f, 165f), 58, Gold, new Color(0.12f, 0.055f, 0.005f), roundedSprite, true);
            AddOutline(startButton.gameObject, GoldLight, 4f);
            AddShadow(startButton.gameObject, new Color(1f, 0.35f, 0f, 0.35f), new Vector2(0f, -10f));
            UnityEventTools.AddPersistentListener(startButton.onClick, controller.StartGame);

            Button shopButton = CreateButton(deck.transform, "ShopButton", "SHOP", new Vector2(-225f, -15f), new Vector2(390f, 115f), 36, Blue, Color.white, roundedSprite, false);
            Button rewardButton = CreateButton(deck.transform, "RewardButton", "DAILY REWARD", new Vector2(225f, -15f), new Vector2(390f, 115f), 32, NavySoft, Color.white, roundedSprite, false);
            UnityEventTools.AddPersistentListener(shopButton.onClick, controller.OpenShop);
            UnityEventTools.AddPersistentListener(rewardButton.onClick, controller.ClaimDailyReward);

            CreateDivider(deck.transform, new Vector2(0f, -112f), new Vector2(900f, 3f));
            CreateBottomNavigation(deck.transform, controller, roundedSprite);
            CreateText(deck.transform, "VersionText", "EARLY MVP 0.1", 20, new Vector2(0f, -322f), new Color(0.55f, 0.63f, 0.75f), TextAlignmentOptions.Center, new Vector2(400f, 35f));
        }

        private static void CreateBottomNavigation(Transform parent, MainMenuController controller, Sprite roundedSprite)
        {
            const float y = -215f;
            Button train = CreateButton(parent, "TrainTab", "TRAIN", new Vector2(-345f, y), new Vector2(205f, 105f), 27, new Color(0.11f, 0.45f, 0.24f, 1f), Color.white, roundedSprite, false);
            Button upgrade = CreateButton(parent, "UpgradeTab", "UPGRADE", new Vector2(-115f, y), new Vector2(205f, 105f), 25, NavySoft, Color.white, roundedSprite, false);
            Button earn = CreateButton(parent, "EarnTab", "EARN", new Vector2(115f, y), new Vector2(205f, 105f), 27, NavySoft, Color.white, roundedSprite, false);
            Button achieve = CreateButton(parent, "AchieveTab", "ACHIEVE", new Vector2(345f, y), new Vector2(205f, 105f), 24, NavySoft, Color.white, roundedSprite, false);

            UnityEventTools.AddPersistentListener(train.onClick, controller.SelectTrainTab);
            UnityEventTools.AddPersistentListener(upgrade.onClick, controller.SelectUpgradeTab);
            UnityEventTools.AddPersistentListener(earn.onClick, controller.SelectEarnTab);
            UnityEventTools.AddPersistentListener(achieve.onClick, controller.SelectAchieveTab);
        }

        private static PopupPanelAnimator CreateSettingsPopup(
            Transform parent,
            MainMenuController controller,
            Sprite roundedSprite,
            out TMP_Text musicValue,
            out TMP_Text sfxValue,
            out TMP_Text vibrationValue)
        {
            PopupPanelAnimator popup = CreatePopupBase(parent, "SettingsPopup", roundedSprite, out Transform card);
            CreatePopupTitle(card, "SETTINGS");

            Button musicButton = CreateButton(card, "MusicToggle", "MUSIC   ON", new Vector2(0f, 145f), new Vector2(700f, 105f), 34, NavySoft, Color.white, roundedSprite, false);
            Button sfxButton = CreateButton(card, "SfxToggle", "SFX   ON", new Vector2(0f, 20f), new Vector2(700f, 105f), 34, NavySoft, Color.white, roundedSprite, false);
            Button vibrationButton = CreateButton(card, "VibrationToggle", "VIBRATION   ON", new Vector2(0f, -105f), new Vector2(700f, 105f), 34, NavySoft, Color.white, roundedSprite, false);

            musicValue = musicButton.GetComponentInChildren<TMP_Text>();
            sfxValue = sfxButton.GetComponentInChildren<TMP_Text>();
            vibrationValue = vibrationButton.GetComponentInChildren<TMP_Text>();

            UnityEventTools.AddPersistentListener(musicButton.onClick, controller.ToggleMusic);
            UnityEventTools.AddPersistentListener(sfxButton.onClick, controller.ToggleSfx);
            UnityEventTools.AddPersistentListener(vibrationButton.onClick, controller.ToggleVibration);

            Button closeButton = CreateButton(card, "CloseSettingsButton", "CLOSE", new Vector2(0f, -285f), new Vector2(430f, 105f), 36, Blue, Color.white, roundedSprite, false);
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.CloseSettings);
            return popup;
        }

        private static PopupPanelAnimator CreateShopPopup(Transform parent, MainMenuController controller, Sprite roundedSprite)
        {
            PopupPanelAnimator popup = CreatePopupBase(parent, "ShopPopup", roundedSprite, out Transform card);
            CreatePopupTitle(card, "SHOP");
            CreateText(card, "ShopSubtitle", "BOOST YOUR FIRST TRANSFORMATION", 25, new Vector2(0f, 235f), new Color(0.58f, 0.76f, 1f), TextAlignmentOptions.Center, new Vector2(720f, 45f));

            Button starter = CreateStoreItem(card, "StarterPack", "STARTER PACK", "Coins + early boost", new Vector2(0f, 105f), roundedSprite);
            Button noAds = CreateStoreItem(card, "NoAds", "REMOVE ADS", "Permanent comfort", new Vector2(0f, -40f), roundedSprite);
            Button protein = CreateStoreItem(card, "ProteinPack", "PROTEIN PACK", "Temporary training bonus", new Vector2(0f, -185f), roundedSprite);

            UnityEventTools.AddPersistentListener(starter.onClick, controller.BuyStarterPack);
            UnityEventTools.AddPersistentListener(noAds.onClick, controller.BuyNoAds);
            UnityEventTools.AddPersistentListener(protein.onClick, controller.BuyProteinPack);

            Button closeButton = CreateButton(card, "CloseShopButton", "CLOSE", new Vector2(0f, -330f), new Vector2(430f, 95f), 34, Blue, Color.white, roundedSprite, false);
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.CloseShop);
            return popup;
        }

        private static PopupPanelAnimator CreateMessagePopup(
            Transform parent,
            MainMenuController controller,
            Sprite roundedSprite,
            out TMP_Text title,
            out TMP_Text body)
        {
            PopupPanelAnimator popup = CreatePopupBase(parent, "MessagePopup", roundedSprite, out Transform card);
            title = CreateText(card, "MessageTitle", "COMING SOON", 50, new Vector2(0f, 225f), GoldLight, TextAlignmentOptions.Center, new Vector2(760f, 90f));
            title.fontStyle = FontStyles.Bold;
            body = CreateText(card, "MessageBody", "This feature will be connected in the next milestone.", 32, new Vector2(0f, 20f), Color.white, TextAlignmentOptions.Center, new Vector2(740f, 300f));
            body.enableWordWrapping = true;

            Button closeButton = CreateButton(card, "CloseMessageButton", "OK", new Vector2(0f, -275f), new Vector2(430f, 105f), 38, Gold, new Color(0.12f, 0.055f, 0.005f), roundedSprite, false);
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.CloseMessage);
            return popup;
        }

        private static PopupPanelAnimator CreatePopupBase(Transform parent, string name, Sprite roundedSprite, out Transform card)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            Stretch(rootRect);

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Image dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            GameObject cardObject = CreatePanel(root.transform, "Card", Vector2.zero, new Vector2(900f, 780f), Navy, roundedSprite, true);
            AddOutline(cardObject, new Color(0.12f, 0.5f, 1f, 0.9f), 4f);
            AddShadow(cardObject, new Color(0f, 0f, 0f, 0.72f), new Vector2(0f, -18f));
            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.localScale = Vector3.one * 0.86f;
            card = cardObject.transform;

            PopupPanelAnimator animator = root.AddComponent<PopupPanelAnimator>();
            AssignReference(animator, "canvasGroup", group);
            AssignReference(animator, "card", cardRect);
            AssignBool(animator, "startHidden", true);
            return animator;
        }

        private static void CreatePopupTitle(Transform parent, string value)
        {
            TMP_Text title = CreateText(parent, "Title", value, 56, new Vector2(0f, 315f), GoldLight, TextAlignmentOptions.Center, new Vector2(780f, 80f));
            title.fontStyle = FontStyles.Bold;
        }

        private static Button CreateStoreItem(Transform parent, string name, string title, string subtitle, Vector2 position, Sprite roundedSprite)
        {
            Button button = CreateButton(parent, name, string.Empty, position, new Vector2(720f, 120f), 32, NavySoft, Color.white, roundedSprite, false);
            CreateText(button.transform, "Title", title, 31, new Vector2(-115f, 22f), Color.white, TextAlignmentOptions.Left, new Vector2(430f, 45f)).fontStyle = FontStyles.Bold;
            CreateText(button.transform, "Subtitle", subtitle, 22, new Vector2(-115f, -24f), new Color(0.62f, 0.72f, 0.86f), TextAlignmentOptions.Left, new Vector2(430f, 38f));
            CreateText(button.transform, "Arrow", ">", 42, new Vector2(295f, 0f), GoldLight, TextAlignmentOptions.Center, new Vector2(55f, 70f)).fontStyle = FontStyles.Bold;
            return button;
        }

        private static void CreateVideoMissingHint(Transform parent, Sprite roundedSprite)
        {
            if (AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath) != null)
            {
                return;
            }

            GameObject panel = CreatePanel(parent, "MissingVideoHint", Vector2.zero, new Vector2(900f, 280f), Navy, roundedSprite, true);
            CreateText(panel.transform, "HintText", "VIDEO NOT FOUND\nPut the MP4 at Assets/Videos/MainMenuLoop.mp4\nThen rebuild this scene from Tools.", 34, Vector2.zero, Color.white, TextAlignmentOptions.Center, new Vector2(820f, 240f));
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            Color buttonColor,
            Color textColor,
            Sprite roundedSprite,
            bool idlePulse)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = buttonColor;

            Button button = buttonObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.86f, 0.9f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.65f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            MenuButtonAnimator animator = buttonObject.AddComponent<MenuButtonAnimator>();
            AssignReference(animator, "target", rect);
            AssignReference(animator, "targetGraphic", image);
            AssignBool(animator, "idlePulse", idlePulse);

            if (!string.IsNullOrWhiteSpace(label))
            {
                TMP_Text text = CreateText(buttonObject.transform, "Text", label, fontSize, Vector2.zero, textColor, TextAlignmentOptions.Center, new Vector2(size.x - 28f, size.y - 18f));
                text.fontStyle = FontStyles.Bold;
                text.raycastTarget = false;
            }

            return button;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, Sprite roundedSprite, bool raycastTarget)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = panel.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = raycastTarget;
            return panel;
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, int fontSize, Vector2 anchoredPosition, Color color, TextAlignmentOptions alignment, Vector2 size)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontSizeMin = 16;
            text.fontSizeMax = fontSize;
            text.enableAutoSizing = true;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static Image CreateStretchImage(Transform parent, string name, Color color, bool raycastTarget)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.AddComponent<RectTransform>();
            Stretch(rect);
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static Image CreateAnchoredImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void CreateDivider(Transform parent, Vector2 position, Vector2 size)
        {
            Image divider = CreateAnchoredImage(parent, "Divider", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size, new Color(0.28f, 0.42f, 0.62f, 0.42f));
            divider.raycastTarget = false;
        }

        private static void AddOutline(GameObject target, Color color, float distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Sprite CreateOrLoadRoundedSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 64;
            const int radius = 18;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool insideHorizontal = x >= radius && x < size - radius;
                    bool insideVertical = y >= radius && y < size - radius;
                    bool inside = insideHorizontal || insideVertical;

                    if (!inside)
                    {
                        float cornerX = x < radius ? radius : size - radius - 1;
                        float cornerY = y < radius ? radius : size - radius - 1;
                        float dx = x - cornerX;
                        float dy = y - cornerY;
                        inside = dx * dx + dy * dy <= radius * radius;
                    }

                    pixels[y * size + x] = inside ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            string fullPath = Path.Combine(Application.dataPath, RoundedSpritePath.Substring("Assets/".Length));
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(RoundedSpritePath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(RoundedSpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spriteBorder = new Vector4(20f, 20f, 20f, 20f);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void AddScenesToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            AddBuildSceneIfExists(scenes, ScenePath);
            AddBuildSceneIfExists(scenes, GameplayScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddBuildSceneIfExists(List<EditorBuildSettingsScene> scenes, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }

        private static void AssignReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Property not found: {target.name}.{propertyName}");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void AssignString(Object target, string propertyName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Property not found: {target.name}.{propertyName}");
                return;
            }

            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void AssignBool(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Property not found: {target.name}.{propertyName}");
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
#endif
