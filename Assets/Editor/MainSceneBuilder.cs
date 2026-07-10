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

            CreateMainCamera();
            CreateLight();

            Canvas canvas = CreateCanvas();
            GameObject gameManagerObject = CreateGameManagerObject();

            PlayerStats playerStats = gameManagerObject.GetComponent<PlayerStats>();
            TapTrainingController tapTrainingController = gameManagerObject.GetComponent<TapTrainingController>();
            UpgradeManager upgradeManager = gameManagerObject.GetComponent<UpgradeManager>();
            MainHudController hudController = gameManagerObject.GetComponent<MainHudController>();
            GameManager gameManager = gameManagerObject.GetComponent<GameManager>();

            Image background = CreateStretchImage("Background", canvas.transform, new Color(0.055f, 0.07f, 0.095f));
            background.raycastTarget = false;

            GameObject topHud = CreatePanel("TopHud", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(980, 330));
            Image topHudBg = topHud.AddComponent<Image>();
            topHudBg.color = new Color(0.09f, 0.11f, 0.15f, 0.92f);

            TMP_Text titleText = CreateText("TitleText", topHud.transform, "SKINNY TO BEAST", 58, new Vector2(0, 115), new Vector2(920, 70), Color.white);
            titleText.fontStyle = FontStyles.Bold;

            TMP_Text coinsText = CreateText("CoinsText", topHud.transform, "Coins: 0", 38, new Vector2(-255, 35), new Vector2(430, 55), Color.white);
            TMP_Text strengthText = CreateText("StrengthText", topHud.transform, "Strength: 0", 38, new Vector2(255, 35), new Vector2(430, 55), Color.white);
            TMP_Text repsText = CreateText("RepsText", topHud.transform, "Reps: 0", 34, new Vector2(-255, -35), new Vector2(430, 50), Color.white);
            TMP_Text bodyStageText = CreateText("BodyStageText", topHud.transform, "Body: Skinny", 34, new Vector2(255, -35), new Vector2(430, 50), Color.white);
            CreateText("TipText", topHud.transform, "Tap fast. Buy upgrades. Become huge.", 28, new Vector2(0, -105), new Vector2(900, 45), new Color(0.72f, 0.82f, 1f));

            GameObject characterArea = CreatePanel("CharacterArea", canvas.transform, new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.54f), Vector2.zero, new Vector2(900, 760));
            Image characterPanel = characterArea.AddComponent<Image>();
            characterPanel.color = new Color(0.08f, 0.09f, 0.12f, 0.55f);

            Image characterImage = CreateImage("CharacterImage", characterArea.transform, new Color(0.96f, 0.82f, 0.55f), new Vector2(0, 115), new Vector2(310, 410));
            TMP_Text characterLabel = CreateText("CharacterLabel", characterImage.transform, "SKINNY", 46, Vector2.zero, new Vector2(280, 95), new Color(0.08f, 0.07f, 0.05f));
            characterLabel.fontStyle = FontStyles.Bold;
            characterLabel.transform.SetAsLastSibling();

            Button tapButton = CreateButton("TapButton", characterArea.transform, "TAP TO TRAIN", new Vector2(0, -205), new Vector2(650, 130), new Color(0.12f, 0.58f, 0.24f), 44);
            UnityEventTools.AddPersistentListener(tapButton.onClick, tapTrainingController.TrainTap);

            GameObject upgradePanel = CreatePanel("UpgradePanel", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 255), new Vector2(980, 500));
            Image upgradePanelBg = upgradePanel.AddComponent<Image>();
            upgradePanelBg.color = new Color(0.09f, 0.11f, 0.15f, 0.92f);

            TMP_Text dumbbellsText;
            TMP_Text proteinText;
            TMP_Text coachText;
            TMP_Text betterGymText;

            CreateText("UpgradeTitle", upgradePanel.transform, "UPGRADES", 42, new Vector2(0, 195), new Vector2(900, 55), Color.white).fontStyle = FontStyles.Bold;
            Button dumbbellsButton = CreateUpgradeButton("DumbbellsButton", upgradePanel.transform, "Dumbbells\nLv.0 — 10 coins", new Vector2(-240, 70), out dumbbellsText);
            Button proteinButton = CreateUpgradeButton("ProteinButton", upgradePanel.transform, "Protein\nLv.0 — 25 coins", new Vector2(240, 70), out proteinText);
            Button coachButton = CreateUpgradeButton("CoachButton", upgradePanel.transform, "Coach\nLv.0 — 100 coins", new Vector2(-240, -115), out coachText);
            Button betterGymButton = CreateUpgradeButton("BetterGymButton", upgradePanel.transform, "Better Gym\nLv.0 — 500 coins", new Vector2(240, -115), out betterGymText);

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
            camera.backgroundColor = new Color(0.055f, 0.07f, 0.095f);
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

        private static Image CreateStretchImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);
            RectTransform rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
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

        private static TMP_Text CreateText(string name, Transform parent, string value, int fontSize, Vector2 anchoredPosition, Vector2 size, Color color)
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
            text.enableAutoSizing = true;
            text.fontSizeMin = 16;
            text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableWordWrapping = false;
            text.transform.SetAsLastSibling();
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, Vector2 size, Color color, int fontSize)
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
            image.color = color;

            Button button = buttonObject.AddComponent<Button>();
            TMP_Text text = CreateText("Text", buttonObject.transform, label, fontSize, Vector2.zero, new Vector2(size.x - 30, size.y - 20), Color.white);
            text.fontStyle = FontStyles.Bold;
            text.transform.SetAsLastSibling();
            return button;
        }

        private static Button CreateUpgradeButton(string name, Transform parent, string label, Vector2 anchoredPosition, out TMP_Text labelText)
        {
            Button button = CreateButton(name, parent, label, anchoredPosition, new Vector2(420, 140), new Color(0.13f, 0.46f, 0.23f), 31);
            labelText = button.GetComponentInChildren<TMP_Text>();
            labelText.enableWordWrapping = true;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
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
