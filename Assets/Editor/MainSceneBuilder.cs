#if UNITY_EDITOR
using SkinnyToBeast.Core;
using SkinnyToBeast.Economy;
using SkinnyToBeast.Player;
using SkinnyToBeast.Training;
using SkinnyToBeast.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinnyToBeast.EditorTools
{
    public static class MainSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Tools/Skinny To Beast/Create MVP Main Scene")]
        public static void CreateMvpMainScene()
        {
            EnsureFolder("Assets", "Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Main";

            Camera mainCamera = CreateMainCamera();
            CreateLight();

            Canvas canvas = CreateCanvas();
            GameObject gameManagerObject = CreateGameManagerObject();

            PlayerStats playerStats = gameManagerObject.GetComponent<PlayerStats>();
            TapTrainingController tapTrainingController = gameManagerObject.GetComponent<TapTrainingController>();
            UpgradeManager upgradeManager = gameManagerObject.GetComponent<UpgradeManager>();
            MainHudController hudController = gameManagerObject.GetComponent<MainHudController>();
            GameManager gameManager = gameManagerObject.GetComponent<GameManager>();

            GameObject topHud = CreatePanel("TopHud", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -140), new Vector2(900, 220));
            TMP_Text coinsText = CreateText("CoinsText", topHud.transform, "Coins: 0", 42, new Vector2(0, 70));
            TMP_Text strengthText = CreateText("StrengthText", topHud.transform, "Strength: 0", 42, new Vector2(0, 10));
            TMP_Text repsText = CreateText("RepsText", topHud.transform, "Reps: 0", 34, new Vector2(0, -45));
            TMP_Text bodyStageText = CreateText("BodyStageText", topHud.transform, "Body: Skinny", 38, new Vector2(0, -95));

            GameObject characterArea = CreatePanel("CharacterArea", canvas.transform, new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(820, 760));
            Image characterImage = CreateImage("CharacterImage", characterArea.transform, new Color(0.95f, 0.86f, 0.65f), new Vector2(0, 100), new Vector2(360, 420));
            CreateText("CharacterLabel", characterImage.transform, "SKINNY", 42, Vector2.zero);
            Button tapButton = CreateButton("TapButton", characterArea.transform, "TAP TO TRAIN", new Vector2(0, -245), new Vector2(560, 130));
            UnityEventTools.AddPersistentListener(tapButton.onClick, tapTrainingController.TrainTap);

            GameObject upgradePanel = CreatePanel("UpgradePanel", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 225), new Vector2(900, 430));
            TMP_Text dumbbellsText;
            TMP_Text proteinText;
            TMP_Text coachText;
            TMP_Text betterGymText;

            Button dumbbellsButton = CreateUpgradeButton("DumbbellsButton", upgradePanel.transform, "Dumbbells", new Vector2(-230, 110), out dumbbellsText);
            Button proteinButton = CreateUpgradeButton("ProteinButton", upgradePanel.transform, "Protein", new Vector2(230, 110), out proteinText);
            Button coachButton = CreateUpgradeButton("CoachButton", upgradePanel.transform, "Coach", new Vector2(-230, -80), out coachText);
            Button betterGymButton = CreateUpgradeButton("BetterGymButton", upgradePanel.transform, "Better Gym", new Vector2(230, -80), out betterGymText);

            UnityEventTools.AddPersistentListener(dumbbellsButton.onClick, hudController.PurchaseDumbbells);
            UnityEventTools.AddPersistentListener(proteinButton.onClick, hudController.PurchaseProtein);
            UnityEventTools.AddPersistentListener(coachButton.onClick, hudController.PurchaseCoach);
            UnityEventTools.AddPersistentListener(betterGymButton.onClick, hudController.PurchaseBetterGym);

            CreateEventSystem();

            AssignReference(tapTrainingController, "playerStats", playerStats);
            AssignReference(upgradeManager, "playerStats", playerStats);
            AssignReference(upgradeManager, "tapTrainingController", tapTrainingController);
            AssignReference(hudController, "playerStats", playerStats);
            AssignReference(hudController, "upgradeManager", upgradeManager);
            AssignReference(hudController, "coinsText", coinsText);
            AssignReference(hudController, "strengthText", strengthText);
            AssignReference(hudController, "repsText", repsText);
            AssignReference(hudController, "bodyStageText", bodyStageText);
            AssignReference(hudController, "dumbbellsText", dumbbellsText);
            AssignReference(hudController, "proteinText", proteinText);
            AssignReference(hudController, "coachText", coachText);
            AssignReference(hudController, "betterGymText", betterGymText);
            AssignReference(gameManager, "playerStats", playerStats);
            AssignReference(gameManager, "tapTrainingController", tapTrainingController);
            AssignReference(gameManager, "upgradeManager", upgradeManager);
            AssignReference(gameManager, "hudController", hudController);

            Selection.activeGameObject = gameManagerObject;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Skinny To Beast MVP scene created: {ScenePath}");
        }

        private static Camera CreateMainCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.13f);
            camera.orthographic = true;
            camera.orthographicSize = 5;
            return camera;
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("Global Light 2D Placeholder");
            lightObject.transform.position = new Vector3(0, 0, -10);
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

        private static GameObject CreateGameManagerObject()
        {
            GameObject gameManagerObject = new GameObject("GameManager");
            gameManagerObject.AddComponent<PlayerStats>();
            gameManagerObject.AddComponent<TapTrainingController>();
            gameManagerObject.AddComponent<UpgradeManager>();
            gameManagerObject.AddComponent<MainHudController>();
            gameManagerObject.AddComponent<GameManager>();
            return gameManagerObject;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return panel;
        }

        private static Image CreateImage(string name, Transform parent, Color color, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, int fontSize, Vector2 anchoredPosition)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(860, 70);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size)
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
            image.color = new Color(0.16f, 0.52f, 0.24f);

            Button button = buttonObject.AddComponent<Button>();
            TMP_Text text = CreateText("Text", buttonObject.transform, label, 42, Vector2.zero);
            text.rectTransform.sizeDelta = size;
            return button;
        }

        private static Button CreateUpgradeButton(string name, Transform parent, string label, Vector2 anchoredPosition, out TMP_Text labelText)
        {
            Button button = CreateButton(name, parent, label, anchoredPosition, new Vector2(400, 145));
            labelText = button.GetComponentInChildren<TMP_Text>();
            labelText.fontSize = 30;
            return button;
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
