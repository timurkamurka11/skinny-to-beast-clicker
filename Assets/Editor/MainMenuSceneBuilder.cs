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

        [MenuItem("Tools/Skinny To Beast/Create Video Main Menu Scene")]
        public static void CreateVideoMainMenuScene()
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

            CreateVideoMissingHint(canvas.transform);
            CreateClickableHotspots(canvas.transform, menuController);

            GameObject settingsPanel = CreateSettingsPanel(canvas.transform, menuController);
            GameObject shopPanel = CreateShopPanel(canvas.transform, menuController);
            GameObject messagePanel = CreateMessagePanel(canvas.transform, menuController);

            AssignReference(menuController, "settingsPanel", settingsPanel);
            AssignReference(menuController, "shopPanel", shopPanel);
            AssignReference(menuController, "messagePanel", messagePanel);

            CreateEventSystem();

            Selection.activeGameObject = controllerObject;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Skinny To Beast video main menu created with functional UI hotspots and popups: {ScenePath}. Video path expected: {VideoPath}");
        }

        private static void CreateMainCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.01f, 0.015f);
            camera.orthographic = true;
            camera.orthographicSize = 5;
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

        private static RenderTexture CreateOrLoadRenderTexture()
        {
            RenderTexture renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (renderTexture != null)
            {
                renderTexture.width = 1080;
                renderTexture.height = 1920;
                renderTexture.depth = 0;
                renderTexture.format = RenderTextureFormat.ARGB32;
                EditorUtility.SetDirty(renderTexture);
                return renderTexture;
            }

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
            AssetDatabase.SaveAssets();
            return renderTexture;
        }

        private static RawImage CreateVideoBackground(Transform parent, RenderTexture renderTexture)
        {
            GameObject backgroundObject = new GameObject("VideoBackground");
            backgroundObject.transform.SetParent(parent, false);
            backgroundObject.transform.SetAsFirstSibling();

            RectTransform rect = backgroundObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

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

            if (clip == null)
            {
                Debug.LogWarning($"Video clip not found at {VideoPath}. Put your MP4 there, then run this builder again.");
            }

            return controllerObject;
        }

        private static void CreateVideoMissingHint(Transform parent)
        {
            VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
            if (clip != null)
            {
                return;
            }

            GameObject panel = CreatePanel(parent, "MissingVideoHint", new Vector2(0, 0), new Vector2(900, 260), new Color(0f, 0f, 0f, 0.72f), true);
            CreateText(panel.transform, "HintText", "VIDEO NOT FOUND\nPut MP4 here:\nAssets/Videos/MainMenuLoop.mp4\nThen run Tools -> Skinny To Beast -> Create Video Main Menu Scene", 38, Vector2.zero, Color.white, TextAlignmentOptions.Center, new Vector2(850, 240));
        }

        private static void CreateClickableHotspots(Transform parent, MainMenuController menuController)
        {
            GameObject hotspotRoot = new GameObject("ProgrammedUI_Hotspots");
            hotspotRoot.transform.SetParent(parent, false);

            Button startButton = CreateHotspot(hotspotRoot.transform, "StartHotspot_Clickable", new Vector2(0, -520), new Vector2(830, 210));
            Button settingsButton = CreateHotspot(hotspotRoot.transform, "SettingsHotspot_Clickable", new Vector2(-260, -735), new Vector2(390, 140));
            Button shopButton = CreateHotspot(hotspotRoot.transform, "ShopHotspot_Clickable", new Vector2(260, -735), new Vector2(390, 140));
            Button leaderboardButton = CreateHotspot(hotspotRoot.transform, "LeaderboardHotspot_Clickable", new Vector2(390, 830), new Vector2(250, 130));
            Button coinPlusButton = CreateHotspot(hotspotRoot.transform, "CoinPlusHotspot_Clickable", new Vector2(-365, 835), new Vector2(350, 110));
            Button trainTab = CreateHotspot(hotspotRoot.transform, "TrainTabHotspot_Clickable", new Vector2(-390, -900), new Vector2(245, 135));
            Button upgradeTab = CreateHotspot(hotspotRoot.transform, "UpgradeTabHotspot_Clickable", new Vector2(-130, -900), new Vector2(265, 135));
            Button earnTab = CreateHotspot(hotspotRoot.transform, "EarnTabHotspot_Clickable", new Vector2(130, -900), new Vector2(245, 135));
            Button achieveTab = CreateHotspot(hotspotRoot.transform, "AchieveTabHotspot_Clickable", new Vector2(390, -900), new Vector2(265, 135));

            UnityEventTools.AddPersistentListener(startButton.onClick, menuController.StartGame);
            UnityEventTools.AddPersistentListener(settingsButton.onClick, menuController.OpenSettings);
            UnityEventTools.AddPersistentListener(shopButton.onClick, menuController.OpenShop);
            UnityEventTools.AddPersistentListener(leaderboardButton.onClick, menuController.SelectAchieveTab);
            UnityEventTools.AddPersistentListener(coinPlusButton.onClick, menuController.OpenShop);
            UnityEventTools.AddPersistentListener(trainTab.onClick, menuController.SelectTrainTab);
            UnityEventTools.AddPersistentListener(upgradeTab.onClick, menuController.SelectUpgradeTab);
            UnityEventTools.AddPersistentListener(earnTab.onClick, menuController.SelectEarnTab);
            UnityEventTools.AddPersistentListener(achieveTab.onClick, menuController.SelectAchieveTab);
        }

        private static Button CreateHotspot(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
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
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.001f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.05f);
            colors.pressedColor = new Color(1f, 0.78f, 0f, 0.12f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.001f);
            colors.disabledColor = new Color(0f, 0f, 0f, 0f);
            button.colors = colors;

            return button;
        }

        private static GameObject CreateSettingsPanel(Transform parent, MainMenuController menuController)
        {
            GameObject panel = CreatePopupBase(parent, "SettingsPanel");
            CreateText(panel.transform, "Title", "SETTINGS", 56, new Vector2(0, 235), new Color(1f, 0.82f, 0.1f), TextAlignmentOptions.Center, new Vector2(760, 80));
            CreateText(panel.transform, "Body", "Music: OFF\nSFX: ON\nVideo Loop: ON\nLanguage: EN", 38, new Vector2(0, 40), Color.white, TextAlignmentOptions.Center, new Vector2(760, 260));
            Button closeButton = CreateVisibleButton(panel.transform, "CloseButton", "CLOSE", new Vector2(0, -235), new Vector2(430, 105), 40, new Color(0.18f, 0.32f, 0.95f), Color.white);
            UnityEventTools.AddPersistentListener(closeButton.onClick, menuController.CloseSettings);
            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateShopPanel(Transform parent, MainMenuController menuController)
        {
            GameObject panel = CreatePopupBase(parent, "ShopPanel");
            CreateText(panel.transform, "Title", "SHOP", 60, new Vector2(0, 235), new Color(1f, 0.82f, 0.1f), TextAlignmentOptions.Center, new Vector2(760, 80));
            CreateText(panel.transform, "Body", "Starter Pack\nNo Ads\nPremium Protein\nLegend Skin\n\nComing soon", 36, new Vector2(0, 30), Color.white, TextAlignmentOptions.Center, new Vector2(780, 330));
            Button closeButton = CreateVisibleButton(panel.transform, "CloseButton", "CLOSE", new Vector2(0, -235), new Vector2(430, 105), 40, new Color(0.18f, 0.32f, 0.95f), Color.white);
            UnityEventTools.AddPersistentListener(closeButton.onClick, menuController.CloseShop);
            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreateMessagePanel(Transform parent, MainMenuController menuController)
        {
            GameObject panel = CreatePopupBase(parent, "MessagePanel");
            CreateText(panel.transform, "Title", "NOT READY YET", 50, new Vector2(0, 205), new Color(1f, 0.82f, 0.1f), TextAlignmentOptions.Center, new Vector2(760, 80));
            CreateText(panel.transform, "Body", "This button is already programmed.\nThe next step is to finish the gameplay scene and add it to Build Settings.", 34, new Vector2(0, 20), Color.white, TextAlignmentOptions.Center, new Vector2(760, 260));
            Button closeButton = CreateVisibleButton(panel.transform, "CloseButton", "CLOSE", new Vector2(0, -220), new Vector2(430, 105), 40, new Color(0.18f, 0.32f, 0.95f), Color.white);
            UnityEventTools.AddPersistentListener(closeButton.onClick, menuController.CloseMessage);
            panel.SetActive(false);
            return panel;
        }

        private static GameObject CreatePopupBase(Transform parent, string name)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.58f);
            dim.raycastTarget = true;

            GameObject card = CreatePanel(root.transform, "Card", new Vector2(0, 0), new Vector2(850, 650), new Color(0.04f, 0.06f, 0.1f, 0.96f), true);
            Outline outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.7f, 0.08f, 0.9f);
            outline.effectDistance = new Vector2(4, -4);

            return card;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, bool raycastTarget)
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
            image.color = color;
            image.raycastTarget = raycastTarget;

            return panel;
        }

        private static Button CreateVisibleButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, int fontSize, Color buttonColor, Color textColor)
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
            image.color = buttonColor;

            Button button = buttonObject.AddComponent<Button>();
            TMP_Text text = CreateText(buttonObject.transform, "Text", label, fontSize, Vector2.zero, textColor, TextAlignmentOptions.Center, size);
            text.fontStyle = FontStyles.Bold;

            return button;
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
            text.alignment = alignment;
            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18;
            text.fontSizeMax = fontSize;

            return text;
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
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
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
