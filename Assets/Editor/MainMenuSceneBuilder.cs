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

        [MenuItem("Tools/Skinny To Beast/Create Video Main Menu Scene")]
        public static void CreateVideoMainMenuScene()
        {
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Videos");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            CreateMainCamera();
            Canvas canvas = CreateCanvas();

            RawImage videoImage = CreateVideoBackground(canvas.transform);
            GameObject controllerObject = CreateController(videoImage);
            MainMenuController menuController = controllerObject.GetComponent<MainMenuController>();

            CreateTopBar(canvas.transform);
            CreateTitle(canvas.transform);
            CreateRightBadge(canvas.transform);

            Button startButton = CreateButton(canvas.transform, "StartButton", "START", new Vector2(0, -520), new Vector2(760, 160), 72, new Color(1f, 0.73f, 0.06f), new Color(0.22f, 0.11f, 0.02f));
            Button settingsButton = CreateButton(canvas.transform, "SettingsButton", "SETTINGS", new Vector2(-235, -720), new Vector2(360, 110), 42, new Color(0.17f, 0.32f, 0.95f), Color.white);
            Button shopButton = CreateButton(canvas.transform, "ShopButton", "SHOP", new Vector2(235, -720), new Vector2(360, 110), 42, new Color(0.17f, 0.32f, 0.95f), Color.white);

            UnityEventTools.AddPersistentListener(startButton.onClick, menuController.StartGame);
            UnityEventTools.AddPersistentListener(settingsButton.onClick, menuController.OpenSettings);
            UnityEventTools.AddPersistentListener(shopButton.onClick, menuController.OpenShop);

            CreateBottomTabs(canvas.transform);
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
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
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

        private static RawImage CreateVideoBackground(Transform parent)
        {
            GameObject backgroundObject = new GameObject("VideoBackground");
            backgroundObject.transform.SetParent(parent, false);

            RectTransform rect = backgroundObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RawImage rawImage = backgroundObject.AddComponent<RawImage>();
            rawImage.color = Color.white;

            AspectRatioFitter fitter = backgroundObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 1280f / 720f;

            return rawImage;
        }

        private static GameObject CreateController(RawImage videoImage)
        {
            GameObject controllerObject = new GameObject("MainMenuController");
            MainMenuVideoController videoController = controllerObject.AddComponent<MainMenuVideoController>();
            controllerObject.AddComponent<MainMenuController>();
            VideoPlayer player = controllerObject.GetComponent<VideoPlayer>();
            VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);

            AssignReference(videoController, "targetImage", videoImage);
            AssignReference(videoController, "menuLoopClip", clip);

            player.isLooping = true;
            player.playOnAwake = false;

            if (clip == null)
            {
                Debug.LogWarning($"Video clip not found at {VideoPath}. Put your MP4 there, then run this builder again.");
            }

            return controllerObject;
        }

        private static void CreateTopBar(Transform parent)
        {
            GameObject coinPanel = CreatePanel(parent, "CoinPanel", new Vector2(-330, 830), new Vector2(360, 90), new Color(0.03f, 0.06f, 0.12f, 0.78f));
            CreateText(coinPanel.transform, "CoinText", "💪 10.2K   +", 42, Vector2.zero, Color.white, TextAlignmentOptions.Center, new Vector2(330, 80));

            GameObject leaderboard = CreatePanel(parent, "LeaderboardPanel", new Vector2(355, 825), new Vector2(240, 110), new Color(0.03f, 0.06f, 0.12f, 0.78f));
            CreateText(leaderboard.transform, "LeaderboardText", "🏆\nLEADERS", 30, Vector2.zero, Color.white, TextAlignmentOptions.Center, new Vector2(220, 95));
        }

        private static void CreateTitle(Transform parent)
        {
            GameObject titlePanel = CreatePanel(parent, "TitlePanel", new Vector2(0, 605), new Vector2(980, 260), new Color(0.02f, 0.02f, 0.03f, 0.45f));
            CreateText(titlePanel.transform, "TitleText", "SKINNY\nTO BEAST", 92, new Vector2(0, 20), new Color(1f, 0.81f, 0.08f), TextAlignmentOptions.Center, new Vector2(940, 180));
            CreateText(titlePanel.transform, "SubtitleText", "Idle Gym Clicker", 38, new Vector2(0, -100), Color.white, TextAlignmentOptions.Center, new Vector2(700, 70));
        }

        private static void CreateRightBadge(Transform parent)
        {
            GameObject badge = CreatePanel(parent, "MotivationBadge", new Vector2(330, -260), new Vector2(300, 160), new Color(0.02f, 0.02f, 0.03f, 0.72f));
            CreateText(badge.transform, "BadgeText", "TRANSFORM\nYOUR BODY\nBUILD LEGACY", 28, Vector2.zero, new Color(1f, 0.86f, 0.08f), TextAlignmentOptions.Center, new Vector2(280, 145));
        }

        private static void CreateBottomTabs(Transform parent)
        {
            GameObject bottomBar = CreatePanel(parent, "BottomTabs", new Vector2(0, -900), new Vector2(1040, 120), new Color(0.02f, 0.04f, 0.08f, 0.86f));
            CreateText(bottomBar.transform, "TabsText", "TRAIN        UPGRADE        EARN        ACHIEVE", 28, Vector2.zero, Color.white, TextAlignmentOptions.Center, new Vector2(1000, 85));
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
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

            return panel;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, int fontSize, Color buttonColor, Color textColor)
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
