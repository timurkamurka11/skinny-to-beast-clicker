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

        [MenuItem("Tools/Skinny To Beast/Create Video Hotspot Main Menu")]
        [MenuItem("Tools/Skinny To Beast/Create Video Main Menu Scene")]
        [MenuItem("Tools/Skinny To Beast/Create Animated Main Menu Scene")]
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

            CreateInvisibleHotspots(canvas.transform, menuController);

            PopupPanelAnimator settingsPopup = CreateSettingsPopup(
                canvas.transform,
                menuController,
                out TMP_Text musicValue,
                out TMP_Text sfxValue,
                out TMP_Text vibrationValue
            );

            PopupPanelAnimator messagePopup = CreateMessagePopup(
                canvas.transform,
                menuController,
                out TMP_Text messageTitle,
                out TMP_Text messageBody
            );

            AssignReference(menuController, "settingsPanel", settingsPopup);
            AssignReference(menuController, "messagePanel", messagePopup);
            AssignReference(menuController, "musicValueText", musicValue);
            AssignReference(menuController, "sfxValueText", sfxValue);
            AssignReference(menuController, "vibrationValueText", vibrationValue);
            AssignReference(menuController, "messageTitleText", messageTitle);
            AssignReference(menuController, "messageBodyText", messageBody);

            CreateMissingVideoHint(canvas.transform);
            CreateEventSystem();

            Selection.activeGameObject = controllerObject;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Vertical loop menu created with invisible START and SETTINGS hotspots: {ScenePath}");
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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static RenderTexture CreateOrLoadRenderTexture()
        {
            RenderTexture renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(720, 1280, 0, RenderTextureFormat.ARGB32)
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
                renderTexture.width = 720;
                renderTexture.height = 1280;
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

            AspectRatioFitter fitter = backgroundObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
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
            AssignBool(videoController, "stretchToPortraitScreen", false);
            AssignString(menuController, "gameplaySceneName", "Main");

            player.source = VideoSource.VideoClip;
            player.clip = clip;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = renderTexture;
            player.aspectRatio = VideoAspectRatio.FitInside;
            player.isLooping = true;
            player.playOnAwake = true;
            player.skipOnDrop = true;
            player.audioOutputMode = VideoAudioOutputMode.None;
            return controllerObject;
        }

        private static void CreateInvisibleHotspots(Transform parent, MainMenuController controller)
        {
            GameObject root = new GameObject("InvisibleClickableUI");
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            Stretch(rootRect);

            // Coordinates match the baked UI in the 720x1280 vertical video.
            Button settingsButton = CreateHotspot(
                root.transform,
                "SettingsHotspot",
                new Vector2(442f, 835f),
                new Vector2(250f, 210f)
            );

            Button startButton = CreateHotspot(
                root.transform,
                "StartHotspot",
                new Vector2(0f, -805f),
                new Vector2(610f, 260f)
            );

            UnityEventTools.AddPersistentListener(settingsButton.onClick, controller.OpenSettings);
            UnityEventTools.AddPersistentListener(startButton.onClick, controller.StartGame);
        }

        private static Button CreateHotspot(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject objectRoot = new GameObject(name);
            objectRoot.transform.SetParent(parent, false);

            RectTransform rect = objectRoot.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = objectRoot.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;

            Button button = objectRoot.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static PopupPanelAnimator CreateSettingsPopup(
            Transform parent,
            MainMenuController controller,
            out TMP_Text musicValue,
            out TMP_Text sfxValue,
            out TMP_Text vibrationValue)
        {
            PopupPanelAnimator popup = CreatePopupBase(parent, "SettingsPopup", "SETTINGS", out Transform card);

            Button musicButton = CreatePopupButton(card, "MusicButton", "MUSIC", new Vector2(0f, 120f), out musicValue);
            Button sfxButton = CreatePopupButton(card, "SfxButton", "SFX", new Vector2(0f, 0f), out sfxValue);
            Button vibrationButton = CreatePopupButton(card, "VibrationButton", "VIBRATION", new Vector2(0f, -120f), out vibrationValue);
            Button closeButton = CreatePopupButton(card, "CloseButton", "CLOSE", new Vector2(0f, -260f), out _);

            UnityEventTools.AddPersistentListener(musicButton.onClick, controller.ToggleMusic);
            UnityEventTools.AddPersistentListener(sfxButton.onClick, controller.ToggleSfx);
            UnityEventTools.AddPersistentListener(vibrationButton.onClick, controller.ToggleVibration);
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.CloseSettings);
            popup.HideImmediate();
            return popup;
        }

        private static PopupPanelAnimator CreateMessagePopup(
            Transform parent,
            MainMenuController controller,
            out TMP_Text title,
            out TMP_Text body)
        {
            PopupPanelAnimator popup = CreatePopupBase(parent, "MessagePopup", string.Empty, out Transform card);
            title = CreateText(card, "MessageTitle", "INFO", 54, new Vector2(0f, 180f), new Vector2(700f, 80f));
            body = CreateText(card, "MessageBody", "", 34, new Vector2(0f, 20f), new Vector2(700f, 250f));
            Button closeButton = CreatePopupButton(card, "CloseButton", "CLOSE", new Vector2(0f, -230f), out _);
            UnityEventTools.AddPersistentListener(closeButton.onClick, controller.CloseMessage);
            popup.HideImmediate();
            return popup;
        }

        private static PopupPanelAnimator CreatePopupBase(Transform parent, string name, string title, out Transform card)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            Stretch(rootRect);

            Image dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.68f);
            dim.raycastTarget = true;

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            GameObject cardObject = new GameObject("Card");
            cardObject.transform.SetParent(root.transform, false);
            RectTransform cardRect = cardObject.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(820f, 760f);

            Image cardImage = cardObject.AddComponent<Image>();
            cardImage.color = new Color(0.025f, 0.045f, 0.08f, 0.98f);
            Outline outline = cardObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.63f, 0.05f, 0.95f);
            outline.effectDistance = new Vector2(4f, -4f);

            PopupPanelAnimator animator = root.AddComponent<PopupPanelAnimator>();
            AssignReference(animator, "canvasGroup", canvasGroup);
            AssignReference(animator, "card", cardRect);

            card = cardObject.transform;
            if (!string.IsNullOrEmpty(title))
            {
                TMP_Text titleText = CreateText(card, "Title", title, 58, new Vector2(0f, 285f), new Vector2(700f, 85f));
                titleText.color = new Color(1f, 0.78f, 0.08f);
            }

            return animator;
        }

        private static Button CreatePopupButton(Transform parent, string name, string label, Vector2 position, out TMP_Text labelText)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(620f, 95f);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.28f, 0.8f, 1f);
            Button button = buttonObject.AddComponent<Button>();
            labelText = CreateText(buttonObject.transform, "Text", label, 38, Vector2.zero, rect.sizeDelta);
            labelText.fontStyle = FontStyles.Bold;
            return button;
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size)
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
            text.fontSizeMin = 20f;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateMissingVideoHint(Transform parent)
        {
            if (AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath) != null)
            {
                return;
            }

            GameObject hint = new GameObject("MissingVideoHint");
            hint.transform.SetParent(parent, false);
            RectTransform rect = hint.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900f, 300f);
            Image image = hint.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.85f);
            CreateText(hint.transform, "Text", "VIDEO NOT FOUND\nPut the loop here:\nAssets/Videos/MainMenuLoop.mp4", 38, Vector2.zero, new Vector2(820f, 260f));
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
