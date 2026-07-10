#if UNITY_EDITOR
using SkinnyToBeast.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SkinnyToBeast.EditorTools
{
    public static class MainMenuSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
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
            CreateEventSystem();

            Selection.activeGameObject = controllerObject;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Skinny To Beast video main menu created: {ScenePath}. Video path expected: {VideoPath}");
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

            AspectRatioFitter fitter = backgroundObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 9f / 16f;

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
            AssignString(menuController, "gameplaySceneName", "Main");

            player.source = VideoSource.VideoClip;
            player.clip = clip;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = renderTexture;
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
            CreateText(panel.transform, "HintText", "VIDEO NOT FOUND\nPut MP4 here:\nAssets/Videos/MainMenuLoop.mp4\nThen run Tools → Skinny To Beast → Create Video Main Menu Scene", 38, Vector2.zero, Color.white, TextAlignmentOptions.Center, new Vector2(850, 240));
        }

        private static void CreateClickableHotspots(Transform parent, MainMenuController menuController)
        {
            Button startButton = CreateHotspot(parent, "StartHotspot", new Vector2(0, -520), new Vector2(800, 180));
            Button settingsButton = CreateHotspot(parent, "SettingsHotspot", new Vector2(-235, -720), new Vector2(380, 130));
            Button shopButton = CreateHotspot(parent, "ShopHotspot", new Vector2(235, -720), new Vector2(380, 130));
            Button trainTab = CreateHotspot(parent, "TrainTabHotspot", new Vector2(-390, -900), new Vector2(240, 120));
            Button upgradeTab = CreateHotspot(parent, "UpgradeTabHotspot", new Vector2(-130, -900), new Vector2(260, 120));
            Button earnTab = CreateHotspot(parent, "EarnTabHotspot", new Vector2(130, -900), new Vector2(240, 120));
            Button achieveTab = CreateHotspot(parent, "AchieveTabHotspot", new Vector2(390, -900), new Vector2(260, 120));

            UnityEventTools.AddPersistentListener(startButton.onClick, menuController.StartGame);
            UnityEventTools.AddPersistentListener(settingsButton.onClick, menuController.OpenSettings);
            UnityEventTools.AddPersistentListener(shopButton.onClick, menuController.OpenShop);
            UnityEventTools.AddPersistentListener(trainTab.onClick, menuController.StartGame);
            UnityEventTools.AddPersistentListener(upgradeTab.onClick, menuController.OpenSettings);
            UnityEventTools.AddPersistentListener(earnTab.onClick, menuController.OpenShop);
            UnityEventTools.AddPersistentListener(achieveTab.onClick, menuController.OpenSettings);
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
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.06f);
            colors.pressedColor = new Color(1f, 0.78f, 0f, 0.12f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.001f);
            colors.disabledColor = new Color(0f, 0f, 0f, 0f);
            button.colors = colors;

            return button;
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
            eventSystemObject.AddComponent<StandaloneInputModule>();
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
