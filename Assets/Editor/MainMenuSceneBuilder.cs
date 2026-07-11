#if UNITY_EDITOR
using System.Collections.Generic;
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

        private static readonly Color PopupBackground = new Color(0.015f, 0.03f, 0.065f, 0.98f);
        private static readonly Color Blue = new Color(0.08f, 0.33f, 0.93f, 1f);
        private static readonly Color Gold = new Color(1f, 0.62f, 0.04f, 1f);

        [MenuItem("Tools/Skinny To Beast/Create Video Hotspot Main Menu")]
        [MenuItem("Tools/Skinny To Beast/Create Animated Main Menu Scene")]
        [MenuItem("Tools/Skinny To Beast/Create Video Main Menu Scene")]
        public static void CreateVideoHotspotMainMenu()
        {
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Videos");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            CreateMainCamera();
            Canvas canvas = CreateCanvas();
            RenderTexture renderTexture = CreateOrLoadRenderTexture();
            RawImage videoImage = CreateVideoBackground(canvas.transform, renderTexture);

            GameObject controllerObject = CreateController(videoImage, renderTexture);
            MainMenuController menuController = controllerObject.GetComponent<MainMenuController>();

            RectTransform safeArea = CreateSafeArea(canvas.transform);
            CreateInvisibleHotspots(safeArea, menuController);

            PopupPanelAnimator settingsPopup = CreateSettingsPopup(
                safeArea,
                menuController,
                out TMP_Text musicValue,
                out TMP_Text sfxValue,
                out TMP_Text vibrationValue
            );

            PopupPanelAnimator shopPopup = CreateShopPopup(safeArea, menuController);
            PopupPanelAnimator messagePopup = CreateMessagePopup(
                safeArea,
                menuController,
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

            CreateVideoMissingHint(safeArea);
            CreateEventSystem();

            Selection.activeGameObject = controllerObject;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Main menu rebuilt: baked video UI is visible, Unity uses transparent clickable hotspots only.");
        }

        private static void CreateMainCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
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
            scaler.referenceResolution = new Vector2(1080f, 1920f);
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

        private static void CreateInvisibleHotspots(Transform parent, MainMenuController controller)
        {
            GameObject root = new GameObject("InvisibleClickableHotspots");
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            Stretch(rootRect);

            // Coordinates match the UI that is already baked into the menu video.
            Button power = CreateHotspot(root.transform, "PowerHotspot", new Vector2(-355f, 835f), new Vector2(350f, 130f));
            Button leaderboard = CreateHotspot(root.transform, "LeaderboardHotspot", new Vector2(65f, 830f), new Vector2(220f, 150f));
            Button settings = CreateHotspot(root.transform, "SettingsHotspot", new Vector2(385f, 835f), new Vector2(285f, 135f));

            Button shop = CreateHotspot(root.transform, "ShopHotspot", new Vector2(455f, 285f), new Vector2(190f, 180f));
            Button reward = CreateHotspot(root.transform, "RewardHotspot", new Vector2(455f, 80f), new Vector2(190f, 180f));
            Button start = CreateHotspot(root.transform, "StartHotspot", new Vector2(330f, -430f), new Vector2(430f, 210f));

            Button train = CreateHotspot(root.transform, "TrainTabHotspot", new Vector2(-390f, -875f), new Vector2(250f, 145f));
            Button upgrade = CreateHotspot(root.transform, "UpgradeTabHotspot", new Vector2(-130f, -875f), new Vector2(260f, 145f));
            Button earn = CreateHotspot(root.transform, "EarnTabHotspot", new Vector2(130f, -875f), new Vector2(250f, 145f));
            Button achieve = CreateHotspot(root.transform, "AchieveTabHotspot", new Vector2(390f, -875f), new Vector2(260f, 145f));

            UnityEventTools.AddPersistentListener(power.onClick, controller.OpenShop);
            UnityEventTools.AddPersistentListener(leaderboard.onClick, controller.OpenLeaderboard);
            UnityEventTools.AddPersistentListener(settings.onClick, controller.OpenSettings);
            UnityEventTools.AddPersistentListener(shop.onClick, controller.OpenShop);
            UnityEventTools.AddPersistentListener(reward.onClick, controller.ClaimDailyReward);
            UnityEventTools.AddPersistentListener(start.onClick, controller.StartGame);
            UnityEventTools.AddPersistentListener(train.onClick, controller.SelectTrainTab);
            UnityEventTools.AddPersistentListener(upgrade.onClick, controller.SelectUpgradeTab);
            UnityEventTools.AddPersistentListener(earn.onClick, controller.SelectEarnTab);
            UnityEventTools.AddPersistentListener(achieve.onClick, controller.SelectAchieveTab);
        }

        private static Button CreateHotspot(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject hotspotObject = new GameObject(name);
            hotspotObject.transform.SetParent(parent, false);

            RectTransform rect = hotspotObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = hotspotObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;

            Button button = hotspotObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            return button;
        }

        private static PopupPanelAnimator CreateSettingsPopup(
            Transform parent,
            MainMenuController controller,
            out TMP_Text musicValue,
            out TMP_Text sfxValue,
            out TMP_Text vibrationValue)
        {
            PopupPanelAnimator popup = CreatePopupBase(parent, "SettingsPopup", "SETTINGS", out RectTransform card);

            musicValue = CreateText(card, "MusicValue", "MUSIC   ON", 38, new Vector2(0f, 120f), Color.white, new Vector2(650f, 70f));
            sfxValue = CreateText(card, "SfxValue", "SFX   ON", 38, new Vector2(0f, 25f), Color.white, new Vector2(650f, 70f));
            vibrationValue = CreateText(card, "VibrationValue", "VIBRATION   ON", 38, new Vector2(0f, -70f), Color.white, new Vector2(650f, 70f));

            Button musicButton = CreateVisibleButton(card, "MusicButton", new Vector2(0f, 120f), new Vector2(690f, 80f), new Color(0f, 0f, 0f, 0.001f));
            Button sfxButton = CreateVisibleButton(card, "SfxButton", new Vector2(0f, 25f), new Vector2(690f, 80f), new Color(0f, 0f, 0f, 0.001f));
            Button vibrationButton = CreateVisibleButton(card, "VibrationButton", new Vector2(0f, -70f), new Vector2(690f, 80f), new Color(0f, 0f, 0f, 0.001f));
            Button closeButton = CreateLabeledButton(card, "CloseButton", "CLOSE", new Vector2(0f, -220f), new Vector2(430f, 105f), Blue);

            UnityEventTools.AddPersistentListener(musicButton.onClick, controller.ToggleMusic);
            UnityEventTools.AddPersistentListener(sfxButton.onClick, controller.ToggleSfx);
            UnityEventTools.AddPersistentListener(vibrationButton.onClick, controller.ToggleVibration);
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.CloseSettings);
            return popup;
        }

        private static PopupPanelAnimator CreateShopPopup(Transform parent, MainMenuController controller)
        {
            PopupPanelAnimator popup = CreatePopupBase(parent, "ShopPopup", "SHOP", out RectTransform card);

            Button starter = CreateLabeledButton(card, "StarterPackButton", "STARTER PACK", new Vector2(0f, 120f), new Vector2(650f, 100f), Gold);
            Button noAds = CreateLabeledButton(card, "NoAdsButton", "REMOVE ADS", new Vector2(0f, 5f), new Vector2(650f, 100f), Blue);
            Button protein = CreateLabeledButton(card, "ProteinButton", "PROTEIN PACK", new Vector2(0f, -110f), new Vector2(650f, 100f), new Color(0.12f, 0.58f, 0.24f));
            Button close = CreateLabeledButton(card, "CloseButton", "CLOSE", new Vector2(0f, -240f), new Vector2(430f, 100f), new Color(0.13f, 0.18f, 0.3f));

            UnityEventTools.AddPersistentListener(starter.onClick, controller.BuyStarterPack);
            UnityEventTools.AddPersistentListener(noAds.onClick, controller.BuyNoAds);
            UnityEventTools.AddPersistentListener(protein.onClick, controller.BuyProteinPack);
            UnityEventTools.AddPersistentListener(close.onClick, controller.CloseShop);
            return popup;
        }

        private static PopupPanelAnimator CreateMessagePopup(
            Transform parent,
            MainMenuController controller,
            out TMP_Text title,
            out TMP_Text body)
        {
            PopupPanelAnimator popup = CreatePopupBase(parent, "MessagePopup", "INFO", out RectTransform card);
            title = card.Find("Title").GetComponent<TMP_Text>();
            body = CreateText(card, "MessageBody", "COMING SOON", 32, new Vector2(0f, 15f), Color.white, new Vector2(700f, 270f));
            body.enableWordWrapping = true;
            Button close = CreateLabeledButton(card, "CloseButton", "CLOSE", new Vector2(0f, -220f), new Vector2(430f, 105f), Blue);
            UnityEventTools.AddPersistentListener(close.onClick, controller.CloseMessage);
            return popup;
        }

        private static PopupPanelAnimator CreatePopupBase(Transform parent, string name, string titleText, out RectTransform cardRect)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            Stretch(rootRect);

            Image dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.68f);
            dim.raycastTarget = true;

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            GameObject card = new GameObject("Card");
            card.transform.SetParent(root.transform, false);
            cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(850f, 650f);
            cardRect.localScale = Vector3.one * 0.88f;

            Image cardImage = card.AddComponent<Image>();
            cardImage.color = PopupBackground;
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.63f, 0.04f, 0.95f);
            outline.effectDistance = new Vector2(4f, -4f);

            TMP_Text title = CreateText(card.transform, "Title", titleText, 54, new Vector2(0f, 240f), Gold, new Vector2(720f, 80f));
            title.fontStyle = FontStyles.Bold;

            PopupPanelAnimator animator = root.AddComponent<PopupPanelAnimator>();
            AssignReference(animator, "canvasGroup", canvasGroup);
            AssignReference(animator, "card", cardRect);
            return animator;
        }

        private static Button CreateVisibleButton(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = buttonObject.AddComponent<Image>();
            image.color = color;
            Button button = buttonObject.AddComponent<Button>();
            return button;
        }

        private static Button CreateLabeledButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color color)
        {
            Button button = CreateVisibleButton(parent, name, position, size, color);
            TMP_Text text = CreateText(button.transform, "Text", label, 36, Vector2.zero, Color.white, size);
            text.fontStyle = FontStyles.Bold;
            return button;
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, int fontSize, Vector2 position, Color color, Vector2 size)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateVideoMissingHint(Transform parent)
        {
            if (AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath) != null)
            {
                return;
            }

            TMP_Text hint = CreateText(
                parent,
                "MissingVideoHint",
                "VIDEO NOT FOUND\nAssets/Videos/MainMenuLoop.mp4",
                42,
                Vector2.zero,
                Color.white,
                new Vector2(900f, 240f)
            );
            hint.enableWordWrapping = true;
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

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
