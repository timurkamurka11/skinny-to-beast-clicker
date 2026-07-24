using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using SkinnyToBeast.Economy;
using SkinnyToBeast.Gameplay;
using SkinnyToBeast.Player;
using SkinnyToBeast.Training;
using SkinnyToBeast.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SkinnyToBeast.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(7000)]
    public sealed class GameplayWindowController : MonoBehaviour
    {
        private const string RootName = "GameplayWindow";
        private const string MainMenuSceneName = "MainMenu";

        private static readonly Color DeepBlue = new Color(0.035f, 0.13f, 0.23f, 0.96f);
        private static readonly Color Orange = new Color(1f, 0.53f, 0.035f, 1f);
        private static readonly Color Gold = new Color(1f, 0.78f, 0.12f, 1f);
        private static readonly Color Muted = new Color(0.66f, 0.73f, 0.82f, 1f);

        private static GameplayWindowController instance;
        private static Sprite roundedSprite;

        private readonly Dictionary<string, TMP_Text> upgradeLabels = new();
        private readonly Dictionary<string, Button> upgradeButtons = new();
        private readonly List<VideoPlayer> pausedVideoPlayers = new();

        private PlayerStats playerStats;
        private TapTrainingController tapTrainingController;
        private UpgradeManager upgradeManager;

        private TMP_Text stageText;
        private TMP_Text strengthText;
        private TMP_Text coinsText;
        private TMP_Text repsText;
        private TMP_Text nextStageText;
        private TMP_Text tapHintText;
        private TMP_Text tapGainText;
        private TMP_Text toastText;
        private RectTransform progressFillRect;
        private GameObject upgradeSheet;
        private CanvasGroup toastGroup;
        private AudioSource tapAudioSource;
        private AudioClip tapAudioClip;
        private Coroutine toastRoutine;
        private GameplayVisualStageController visualStageController;
        private TapFeedbackController tapFeedbackController;

        private float tapPunch;
        private float lastTapTime = -10f;
        private int tapChain;
        private int lastVisualBodyStage = -1;
        private bool isClosing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            roundedSprite = null;
        }

        public static bool Show()
        {
            try
            {
                if (instance != null)
                {
                    instance.gameObject.SetActive(true);
                    instance.transform.SetAsLastSibling();
                    return true;
                }

                Canvas parentCanvas = FindOrCreateCanvas();
                if (parentCanvas == null)
                {
                    return false;
                }

                GameObject root = new GameObject(
                    RootName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(GraphicRaycaster),
                    typeof(CanvasGroup));
                root.transform.SetParent(parentCanvas.transform, false);

                RectTransform rootRect = root.GetComponent<RectTransform>();
                Stretch(rootRect);

                Canvas overlayCanvas = root.GetComponent<Canvas>();
                overlayCanvas.overrideSorting = true;
                overlayCanvas.sortingOrder = 15000;

                instance = root.AddComponent<GameplayWindowController>();
                root.transform.SetAsLastSibling();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not open gameplay window: {exception}");
                return false;
            }
        }

        private static Canvas FindOrCreateCanvas()
        {
            Canvas found = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (found != null)
            {
                return found;
            }

            GameObject canvasObject = new GameObject(
                "RuntimeGameplayCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject(
                "RuntimeGameplayEventSystem",
                typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
            UnityEngine.Object.DontDestroyOnLoad(eventSystemObject);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            EnsureEventSystem();
            PauseMenuVideo();
            CreateGameState();
            BuildWindow();

            playerStats.StatsChanged += Refresh;
            upgradeManager.UpgradesChanged += Refresh;

            CreateTapAudio();
            Refresh();
        }

        private void CreateGameState()
        {
            GameObject state = new GameObject("RuntimeGameState");
            state.transform.SetParent(transform, false);

            playerStats = state.AddComponent<PlayerStats>();
            tapTrainingController = state.AddComponent<TapTrainingController>();
            upgradeManager = state.AddComponent<UpgradeManager>();

            tapTrainingController.SetPlayerStats(playerStats);
            upgradeManager.SetReferences(playerStats, tapTrainingController);
        }

        private void BuildWindow()
        {
            Image blocker = CreateStretchImage(transform, "InputBlocker", Color.black);
            blocker.raycastTarget = true;

            RectTransform livingScene =
                CreateStretchRect(transform, "LivingGameplayScene");
            visualStageController =
                livingScene.gameObject.AddComponent<GameplayVisualStageController>();
            visualStageController.Build();

            RectTransform effectsRect = CreateStretchRect(transform, "TapEffects");
            tapFeedbackController =
                effectsRect.gameObject.AddComponent<TapFeedbackController>();
            tapFeedbackController.Build(
                effectsRect,
                visualStageController.DumbbellPosition);

            RectTransform safeRoot = CreateStretchRect(transform, "SafeArea");
            safeRoot.gameObject.AddComponent<SafeAreaFitter>();

            BuildTopHud(safeRoot);
            BuildTapArea(safeRoot);
            BuildBottomNavigation(safeRoot);
            BuildToast(safeRoot);
            BuildUpgradeSheet(safeRoot);
        }

        private void BuildTopHud(RectTransform safeRoot)
        {
            Image header = CreateRoundedPanel(
                safeRoot,
                "TopHud",
                new Vector2(0.5f, 1f),
                new Vector2(0f, -94f),
                new Vector2(1000f, 154f),
                new Color(0.018f, 0.032f, 0.055f, 0.90f));

            Button backButton = CreateButton(
                header.transform,
                "BackButton",
                "<",
                new Vector2(-444f, 0f),
                new Vector2(78f, 78f),
                DeepBlue,
                46f);
            backButton.onClick.AddListener(CloseToMenu);

            CreateText(
                header.transform,
                "StageLabel",
                "BODY STAGE",
                17f,
                new Vector2(-294f, 30f),
                new Vector2(220f, 30f),
                Muted);
            stageText = CreateText(
                header.transform,
                "StageValue",
                "STAGE 1",
                31f,
                new Vector2(-294f, -10f),
                new Vector2(230f, 55f),
                Color.white);

            CreateText(
                header.transform,
                "PowerLabel",
                "POWER",
                17f,
                new Vector2(0f, 34f),
                new Vector2(230f, 28f),
                Muted);
            strengthText = CreateText(
                header.transform,
                "PowerValue",
                "0",
                45f,
                new Vector2(0f, -11f),
                new Vector2(280f, 64f),
                Orange);

            CreateText(
                header.transform,
                "CoinsLabel",
                "COINS",
                17f,
                new Vector2(309f, 34f),
                new Vector2(210f, 28f),
                Muted);
            coinsText = CreateText(
                header.transform,
                "CoinsValue",
                "0",
                40f,
                new Vector2(309f, -11f),
                new Vector2(230f, 60f),
                Gold);

            Image progressBackground = CreateRoundedPanel(
                safeRoot,
                "StageProgressBackground",
                new Vector2(0.5f, 1f),
                new Vector2(0f, -196f),
                new Vector2(900f, 24f),
                new Color(0.02f, 0.055f, 0.095f, 0.94f));

            GameObject fillObject = new GameObject(
                "StageProgressFill",
                typeof(RectTransform),
                typeof(Image));
            fillObject.transform.SetParent(progressBackground.transform, false);
            progressFillRect = fillObject.GetComponent<RectTransform>();
            progressFillRect.anchorMin = new Vector2(0f, 0f);
            progressFillRect.anchorMax = new Vector2(0f, 1f);
            progressFillRect.pivot = new Vector2(0f, 0.5f);
            progressFillRect.offsetMin = new Vector2(4f, 4f);
            progressFillRect.offsetMax = new Vector2(4f, -4f);

            Image fillImage = fillObject.GetComponent<Image>();
            fillImage.sprite = GetRoundedSprite();
            fillImage.type = Image.Type.Sliced;
            fillImage.color = Orange;
            fillImage.raycastTarget = false;

            nextStageText = CreateText(
                safeRoot,
                "NextStageText",
                "NEXT: BEGINNER",
                20f,
                new Vector2(0f, -232f),
                new Vector2(900f, 34f),
                new Color(0.87f, 0.91f, 0.96f, 0.96f),
                new Vector2(0.5f, 1f));
        }

        private void BuildTapArea(RectTransform safeRoot)
        {
            tapHintText = CreateText(
                safeRoot,
                "TapHint",
                "TAP THE DUMBBELL",
                43f,
                new Vector2(0f, 600f),
                new Vector2(820f, 62f),
                Color.white,
                new Vector2(0.5f, 0f));
            tapHintText.outlineColor = new Color32(8, 13, 24, 255);
            tapHintText.outlineWidth = 0.22f;

            tapGainText = CreateText(
                safeRoot,
                "TapGain",
                "+1 POWER PER TAP",
                23f,
                new Vector2(0f, 552f),
                new Vector2(700f, 42f),
                Gold,
                new Vector2(0.5f, 0f));

            RectTransform tapZoneRect = CreateRect(
                safeRoot,
                "DumbbellTapZone",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 330f),
                new Vector2(930f, 490f));
            Image tapZoneImage = tapZoneRect.gameObject.AddComponent<Image>();
            tapZoneImage.color = new Color(1f, 1f, 1f, 0.001f);
            tapZoneImage.raycastTarget = true;

            Button tapButton = tapZoneRect.gameObject.AddComponent<Button>();
            tapButton.transition = Selectable.Transition.None;
            tapButton.onClick.AddListener(HandleTrainingTap);
        }

        private void BuildBottomNavigation(RectTransform safeRoot)
        {
            Image nav = CreateRoundedPanel(
                safeRoot,
                "BottomNavigation",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 72f),
                new Vector2(1000f, 118f),
                new Color(0.018f, 0.032f, 0.055f, 0.93f));

            Button trainButton = CreateButton(
                nav.transform,
                "TrainTab",
                "TRAIN",
                new Vector2(-320f, 0f),
                new Vector2(282f, 82f),
                Orange,
                29f);
            trainButton.onClick.AddListener(CloseUpgradeSheet);

            Button upgradesButton = CreateButton(
                nav.transform,
                "UpgradesTab",
                "UPGRADES",
                Vector2.zero,
                new Vector2(300f, 82f),
                DeepBlue,
                27f);
            upgradesButton.onClick.AddListener(OpenUpgradeSheet);

            Button goalsButton = CreateButton(
                nav.transform,
                "GoalsTab",
                "GOALS  SOON",
                new Vector2(320f, 0f),
                new Vector2(282f, 82f),
                new Color(0.12f, 0.15f, 0.20f, 0.96f),
                24f);
            goalsButton.onClick.AddListener(() => ShowToast("GOALS UNLOCK IN THE NEXT UPDATE"));
        }

        private void BuildUpgradeSheet(RectTransform safeRoot)
        {
            RectTransform sheetRect = CreateStretchRect(safeRoot, "UpgradeSheet");
            upgradeSheet = sheetRect.gameObject;

            Image dim = upgradeSheet.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.68f);
            dim.raycastTarget = true;

            Button backdropButton = upgradeSheet.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.onClick.AddListener(CloseUpgradeSheet);

            Image card = CreateRoundedPanel(
                upgradeSheet.transform,
                "UpgradeCard",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 390f),
                new Vector2(1030f, 760f),
                new Color(0.022f, 0.045f, 0.078f, 0.99f));
            AddOutline(card.gameObject, Orange, new Vector2(5f, -5f));

            CreateText(
                card.transform,
                "UpgradeTitle",
                "UPGRADES",
                47f,
                new Vector2(0f, 314f),
                new Vector2(700f, 70f),
                Color.white);
            CreateText(
                card.transform,
                "UpgradeSubtitle",
                "SPEND COINS TO TRAIN FASTER",
                19f,
                new Vector2(0f, 264f),
                new Vector2(760f, 34f),
                Muted);

            Button closeButton = CreateButton(
                card.transform,
                "CloseUpgradesButton",
                "X",
                new Vector2(438f, 310f),
                new Vector2(72f, 72f),
                new Color(0.16f, 0.19f, 0.25f, 1f),
                30f);
            closeButton.onClick.AddListener(CloseUpgradeSheet);

            CreateUpgradeButton(
                card.transform,
                "dumbbells",
                "DUMBBELLS",
                "TAP POWER +25%",
                new Vector2(-245f, 115f));
            CreateUpgradeButton(
                card.transform,
                "protein",
                "PROTEIN",
                "COINS +15%",
                new Vector2(245f, 115f));
            CreateUpgradeButton(
                card.transform,
                "coach",
                "COACH",
                "AUTO +1 REP/SEC",
                new Vector2(-245f, -135f));
            CreateUpgradeButton(
                card.transform,
                "better_gym",
                "BETTER GYM",
                "BOOST ALL GAINS",
                new Vector2(245f, -135f));

            repsText = CreateText(
                card.transform,
                "TotalRepsText",
                "TOTAL REPS  0",
                24f,
                new Vector2(0f, -307f),
                new Vector2(700f, 44f),
                new Color(0.75f, 0.84f, 0.96f, 1f));

            upgradeSheet.SetActive(false);
        }

        private void CreateUpgradeButton(
            Transform parent,
            string id,
            string title,
            string description,
            Vector2 position)
        {
            Button button = CreateButton(
                parent,
                $"{title.Replace(" ", string.Empty)}Button",
                string.Empty,
                position,
                new Vector2(450f, 215f),
                DeepBlue,
                25f);

            TMP_Text label = CreateText(
                button.transform,
                "Label",
                $"{title}\n{description}",
                27f,
                Vector2.zero,
                new Vector2(410f, 180f),
                Color.white);
            label.enableWordWrapping = true;
            label.richText = true;

            button.onClick.AddListener(() => TryPurchaseUpgrade(id));
            upgradeButtons[id] = button;
            upgradeLabels[id] = label;
        }

        private void BuildToast(RectTransform safeRoot)
        {
            Image toast = CreateRoundedPanel(
                safeRoot,
                "Toast",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 770f),
                new Vector2(820f, 96f),
                new Color(0.025f, 0.055f, 0.09f, 0.97f));
            AddOutline(toast.gameObject, Orange, new Vector2(3f, -3f));

            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastGroup.blocksRaycasts = false;
            toastGroup.interactable = false;

            toastText = CreateText(
                toast.transform,
                "ToastText",
                string.Empty,
                27f,
                Vector2.zero,
                new Vector2(760f, 72f),
                Color.white);
        }

        private void HandleTrainingTap()
        {
            if (isClosing || upgradeSheet.activeSelf)
            {
                return;
            }

            int previousStage = playerStats.BodyStageIndex;
            double gainedStrength = tapTrainingController.StrengthGainedPerTap;
            tapTrainingController.TrainTap();

            float now = Time.unscaledTime;
            tapChain = now - lastTapTime <= 0.68f ? tapChain + 1 : 1;
            lastTapTime = now;
            tapPunch = 1f;

            string gain = $"+{FormatGain(gainedStrength)} POWER";
            visualStageController?.PlayTap();
            tapFeedbackController?.EmitTap(gain, tapChain);
            PlayTapSound();
            HapticsService.Tap();

            if (playerStats.BodyStageIndex > previousStage)
            {
                ShowToast($"NEW BODY STAGE: {playerStats.BodyStageName.ToUpperInvariant()}");
            }
        }

        private void OpenUpgradeSheet()
        {
            if (upgradeSheet.activeSelf)
            {
                return;
            }

            upgradeSheet.SetActive(true);
            upgradeSheet.transform.SetAsLastSibling();
            RefreshUpgradeCards();
            UiSoundPlayer.PlayOpen();
        }

        private void CloseUpgradeSheet()
        {
            if (!upgradeSheet.activeSelf)
            {
                return;
            }

            upgradeSheet.SetActive(false);
            UiSoundPlayer.PlayClose();
        }

        private void TryPurchaseUpgrade(string id)
        {
            UpgradeData upgrade = upgradeManager.GetUpgrade(id);
            if (upgrade == null)
            {
                return;
            }

            if (!upgradeManager.Purchase(id))
            {
                double missing = Math.Max(0d, Math.Ceiling(upgrade.CurrentCost - playerStats.Coins));
                ShowToast($"NEED {NumberFormatter.Format(missing)} MORE COINS");
                UiSoundPlayer.PlayBack();
                return;
            }

            ShowToast($"{upgrade.displayName.ToUpperInvariant()} UPGRADED TO LV.{upgrade.level}");
            UiSoundPlayer.PlayConfirm();
            visualStageController?.PlayUpgrade();
            tapFeedbackController?.EmitUpgrade(GetUpgradeEffectPosition(id));
            HapticsService.Upgrade();
            Refresh();
        }

        private void Refresh()
        {
            if (playerStats == null)
            {
                return;
            }

            stageText.text = $"STAGE {playerStats.BodyStageIndex + 1}";
            strengthText.text = NumberFormatter.Format(playerStats.Strength);
            coinsText.text = NumberFormatter.Format(playerStats.Coins);

            if (repsText != null)
            {
                repsText.text = $"TOTAL REPS  {NumberFormatter.Format(playerStats.TotalReps)}";
            }

            float progress = playerStats.BodyStageProgress;
            progressFillRect.anchorMax = new Vector2(Mathf.Max(0.012f, progress), 1f);

            bool finalStage = playerStats.BodyStageIndex >= playerStats.BodyStageCount - 1;
            nextStageText.text = finalStage
                ? "MAXIMUM BODY STAGE"
                : $"NEXT: {playerStats.NextBodyStageName.ToUpperInvariant()}  •  " +
                  $"{NumberFormatter.Format(playerStats.Strength)} / " +
                  $"{NumberFormatter.Format(playerStats.NextBodyStageStrength)} POWER";

            tapGainText.text =
                $"+{FormatGain(tapTrainingController.StrengthGainedPerTap)} POWER PER TAP";

            bool animateVisualChange = lastVisualBodyStage >= 0;
            visualStageController?.Sync(
                playerStats.BodyStageIndex,
                upgradeManager,
                animateVisualChange);

            if (lastVisualBodyStage >= 0 &&
                playerStats.BodyStageIndex > lastVisualBodyStage)
            {
                tapFeedbackController?.EmitStageChange();
                HapticsService.StageChange();
            }

            lastVisualBodyStage = playerStats.BodyStageIndex;
            RefreshUpgradeCards();
        }

        private void RefreshUpgradeCards()
        {
            if (upgradeManager == null)
            {
                return;
            }

            RefreshUpgradeCard("dumbbells", "DUMBBELLS", "TAP POWER +25%");
            RefreshUpgradeCard("protein", "PROTEIN", "COINS +15%");
            RefreshUpgradeCard("coach", "COACH", "AUTO +1 REP/SEC");
            RefreshUpgradeCard("better_gym", "BETTER GYM", "BOOST ALL GAINS");
        }

        private void RefreshUpgradeCard(string id, string title, string description)
        {
            if (!upgradeLabels.TryGetValue(id, out TMP_Text label) ||
                !upgradeButtons.TryGetValue(id, out Button button))
            {
                return;
            }

            UpgradeData upgrade = upgradeManager.GetUpgrade(id);
            if (upgrade == null)
            {
                return;
            }

            double shownCost = Math.Ceiling(upgrade.CurrentCost);
            label.text =
                $"{title}\n" +
                $"<size=20><color=#AFC6E6>LV.{upgrade.level}  •  {description}</color></size>\n" +
                $"<size=25><color=#FFD65A>{NumberFormatter.Format(shownCost)} COINS</color></size>";

            bool affordable = playerStats.CanSpend(upgrade.CurrentCost);
            Image image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = affordable
                    ? new Color(0.04f, 0.25f, 0.36f, 0.98f)
                    : new Color(0.10f, 0.13f, 0.18f, 0.98f);
            }
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            tapPunch = Mathf.MoveTowards(tapPunch, 0f, Time.unscaledDeltaTime * 5.8f);

            if (tapHintText != null)
            {
                if (tapChain >= 3 && time - lastTapTime <= 1.05f)
                {
                    tapHintText.text = $"TAP STREAK  x{tapChain}";
                }
                else
                {
                    tapHintText.text = "TAP THE DUMBBELL";
                    if (time - lastTapTime > 1.05f)
                    {
                        tapChain = 0;
                    }
                }

                float hintScale = 1f + tapPunch * 0.08f;
                tapHintText.rectTransform.localScale = Vector3.one * hintScale;
            }

            if (BackPressedThisFrame())
            {
                if (upgradeSheet != null && upgradeSheet.activeSelf)
                {
                    CloseUpgradeSheet();
                }
                else
                {
                    CloseToMenu();
                }
            }
        }

        private static bool BackPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        private static string FormatGain(double value)
        {
            if (value < 1000d)
            {
                return value.ToString("0.##", CultureInfo.InvariantCulture);
            }

            return NumberFormatter.Format(value);
        }

        private void ShowToast(string message)
        {
            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
            }

            toastGroup.transform.SetAsLastSibling();
            toastText.text = message;
            toastRoutine = StartCoroutine(ToastRoutine());
        }

        private IEnumerator ToastRoutine()
        {
            toastGroup.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < 0.16f)
            {
                elapsed += Time.unscaledDeltaTime;
                toastGroup.alpha = Mathf.Clamp01(elapsed / 0.16f);
                yield return null;
            }

            toastGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(1.25f);

            elapsed = 0f;
            while (elapsed < 0.22f)
            {
                elapsed += Time.unscaledDeltaTime;
                toastGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.22f);
                yield return null;
            }

            toastGroup.alpha = 0f;
            toastRoutine = null;
        }

        private void CreateTapAudio()
        {
            tapAudioSource = gameObject.AddComponent<AudioSource>();
            tapAudioSource.playOnAwake = false;
            tapAudioSource.loop = false;
            tapAudioSource.spatialBlend = 0f;
            tapAudioSource.priority = 16;

            const int sampleRate = 44100;
            const float duration = 0.055f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Pow(1f - i / (float)sampleCount, 3f);
                float tone = Mathf.Sin(2f * Mathf.PI * 145f * t) * 0.68f;
                tone += Mathf.Sin(2f * Mathf.PI * 290f * t) * 0.22f;
                samples[i] = tone * envelope;
            }

            tapAudioClip = AudioClip.Create(
                "GameplayTap",
                sampleCount,
                1,
                sampleRate,
                false);
            tapAudioClip.SetData(samples, 0);
        }

        private void PlayTapSound()
        {
            if (PlayerPrefs.GetInt("settings.sfx", 1) == 0 ||
                tapAudioSource == null ||
                tapAudioClip == null)
            {
                return;
            }

            float settingVolume = Mathf.Clamp01(
                PlayerPrefs.GetFloat("settings.sfx.volume", 0.8f));
            tapAudioSource.pitch = UnityEngine.Random.Range(0.94f, 1.08f);
            tapAudioSource.PlayOneShot(tapAudioClip, 0.24f * settingVolume);
        }

        private static Vector2 GetUpgradeEffectPosition(string id)
        {
            return id switch
            {
                "dumbbells" => new Vector2(0f, 330f),
                "protein" => new Vector2(372f, 690f),
                "coach" => new Vector2(-370f, 985f),
                "better_gym" => new Vector2(0f, 1030f),
                _ => new Vector2(0f, 760f)
            };
        }

        private void PauseMenuVideo()
        {
            VideoPlayer[] players = UnityEngine.Object.FindObjectsByType<VideoPlayer>(
                FindObjectsSortMode.None);
            foreach (VideoPlayer player in players)
            {
                if (player == null || !player.isPlaying)
                {
                    continue;
                }

                player.Pause();
                pausedVideoPlayers.Add(player);
            }
        }

        private void ResumeMenuVideo()
        {
            foreach (VideoPlayer player in pausedVideoPlayers)
            {
                if (player != null)
                {
                    player.Play();
                }
            }

            pausedVideoPlayers.Clear();
        }

        private void CloseToMenu()
        {
            if (isClosing)
            {
                return;
            }

            isClosing = true;
            playerStats?.SaveNow();
            UiSoundPlayer.PlayBack();
            ResumeMenuVideo();

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != MainMenuSceneName &&
                Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
            {
                SceneManager.LoadScene(MainMenuSceneName);
                return;
            }

            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (playerStats != null)
            {
                playerStats.StatsChanged -= Refresh;
                playerStats.SaveNow();
            }

            if (upgradeManager != null)
            {
                upgradeManager.UpgradesChanged -= Refresh;
            }

            ResumeMenuVideo();

            if (tapAudioClip != null)
            {
                Destroy(tapAudioClip);
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        private static RectTransform CreateStretchRect(Transform parent, string name)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            Stretch(rect);
            return rect;
        }

        private static RectTransform CreateRect(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image CreateStretchImage(Transform parent, string name, Color color)
        {
            RectTransform rect = CreateStretchRect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Image CreateRoundedPanel(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            RectTransform rect = CreateRect(parent, name, anchor, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 position,
            Vector2 size,
            Color color,
            float fontSize)
        {
            Image image = CreateRoundedPanel(
                parent,
                name,
                new Vector2(0.5f, 0.5f),
                position,
                size,
                color);
            image.raycastTarget = true;

            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.75f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.06f;
            button.colors = colors;

            if (!string.IsNullOrEmpty(label))
            {
                CreateText(
                    image.transform,
                    "Label",
                    label,
                    fontSize,
                    Vector2.zero,
                    size - new Vector2(22f, 16f),
                    Color.white);
            }

            return button;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            string value,
            float fontSize,
            Vector2 position,
            Vector2 size,
            Color color,
            Vector2? anchor = null)
        {
            Vector2 resolvedAnchor = anchor ?? new Vector2(0.5f, 0.5f);
            RectTransform rect = CreateRect(parent, name, resolvedAnchor, position, size);

            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(12f, fontSize - 9f);
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableWordWrapping = false;
            text.outlineColor = new Color32(8, 12, 20, 225);
            text.outlineWidth = 0.10f;
            return text;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static Sprite GetRoundedSprite()
        {
            if (roundedSprite != null)
            {
                return roundedSprite;
            }

            const int size = 64;
            const float radius = 18f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "GameplayRoundedRect",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float straight = center - radius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - center) - straight, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - center) - straight, 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.75f - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            roundedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(20f, 20f, 20f, 20f));
            roundedSprite.name = "GameplayRoundedSprite";
            return roundedSprite;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
