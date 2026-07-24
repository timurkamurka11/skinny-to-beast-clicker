using System.Collections;
using SkinnyToBeast.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayVisualStageController : MonoBehaviour
    {
        private const string ResourceRoot = "UI/Gameplay/Living/";
        private static readonly Vector2 DumbbellScenePosition = new Vector2(0f, 330f);

        private readonly Sprite[] characterSprites = new Sprite[4];
        private readonly Sprite[] dumbbellSprites = new Sprite[3];

        private Sprite roomStageOne;
        private Sprite roomStageTwo;
        private Sprite proteinSprite;
        private Sprite coachSprite;

        private Image roomBaseImage;
        private Image roomUpgradeImage;
        private CanvasGroup roomUpgradeGroup;
        private Image characterImage;
        private Image characterGhostImage;
        private CanvasGroup characterGroup;
        private CanvasGroup characterGhostGroup;
        private Image bellyImage;
        private Image dumbbellImage;
        private Image dumbbellGhostImage;
        private CanvasGroup dumbbellGroup;
        private CanvasGroup dumbbellGhostGroup;
        private Image proteinImage;
        private Image coachImage;
        private CanvasGroup proteinGroup;
        private CanvasGroup coachGroup;

        private GameplayAnimationController characterAnimator;
        private DumbbellTapAnimator dumbbellAnimator;
        private AmbientAnimationController ambientAnimator;

        private Coroutine characterTransition;
        private Coroutine dumbbellTransition;
        private Coroutine roomTransition;
        private Coroutine proteinTransition;
        private Coroutine coachTransition;

        private int currentCharacterArt = -1;
        private int currentDumbbellArt = -1;
        private int currentProteinLevel = -1;
        private int currentCoachLevel = -1;
        private int currentRoomLevel = -1;
        private bool built;

        public Vector2 DumbbellPosition => DumbbellScenePosition;
        internal GameplayAnimationController CharacterAnimator => characterAnimator;
        internal DumbbellTapAnimator DumbbellAnimator => dumbbellAnimator;

        public void Build()
        {
            if (built)
            {
                return;
            }

            LoadSprites();
            BuildRooms();

            RectTransform ambientLayer =
                LivingGameplayVisualFactory.CreateStretchRect(transform, "AmbientLayer");
            ambientAnimator = ambientLayer.gameObject.AddComponent<AmbientAnimationController>();
            ambientAnimator.Build(ambientLayer);

            BuildProps();
            BuildCharacter();
            BuildDumbbell();
            built = true;
        }

        public void Sync(int bodyStageIndex, UpgradeManager upgradeManager, bool animate)
        {
            if (!built)
            {
                return;
            }

            int characterArt = ResolveCharacterArt(bodyStageIndex);
            int dumbbellLevel = GetUpgradeLevel(upgradeManager, "dumbbells");
            int proteinLevel = GetUpgradeLevel(upgradeManager, "protein");
            int coachLevel = GetUpgradeLevel(upgradeManager, "coach");
            int roomLevel = GetUpgradeLevel(upgradeManager, "better_gym");
            int dumbbellArt = ResolveDumbbellArt(dumbbellLevel);

            if (characterArt != currentCharacterArt)
            {
                ApplyCharacter(characterArt, animate && currentCharacterArt >= 0);
            }

            if (dumbbellArt != currentDumbbellArt)
            {
                ApplyDumbbell(dumbbellArt, animate && currentDumbbellArt >= 0);
            }

            if (proteinLevel != currentProteinLevel)
            {
                ApplyProp(
                    proteinGroup,
                    proteinLevel > 0,
                    animate && currentProteinLevel >= 0,
                    ref proteinTransition);
                currentProteinLevel = proteinLevel;
            }

            if (coachLevel != currentCoachLevel)
            {
                ApplyProp(
                    coachGroup,
                    coachLevel > 0,
                    animate && currentCoachLevel >= 0,
                    ref coachTransition);
                currentCoachLevel = coachLevel;
            }

            if (roomLevel != currentRoomLevel)
            {
                ApplyRoom(roomLevel > 0, animate && currentRoomLevel >= 0);
                currentRoomLevel = roomLevel;
            }
        }

        public void PlayTap()
        {
            characterAnimator?.TriggerTap();
            dumbbellAnimator?.TriggerTap();
            ambientAnimator?.PulseFromTap();
        }

        public void PlayUpgrade()
        {
            characterAnimator?.TriggerUpgrade();
            ambientAnimator?.PulseFromTap();
        }

        public void PlayStageChange()
        {
            characterAnimator?.TriggerStageChange();
            ambientAnimator?.PulseFromTap();
        }

        private void LoadSprites()
        {
            roomStageOne = LivingGameplayVisualFactory.LoadSprite(ResourceRoot + "room_stage_01");
            roomStageTwo = LivingGameplayVisualFactory.LoadSprite(ResourceRoot + "room_stage_02");
            if (roomStageOne == null)
            {
                roomStageOne =
                    LivingGameplayVisualFactory.LoadSprite("UI/Gameplay/starter_home_gym");
            }

            if (roomStageTwo == null)
            {
                roomStageTwo = roomStageOne;
            }

            for (int i = 0; i < characterSprites.Length; i++)
            {
                characterSprites[i] = LivingGameplayVisualFactory.LoadSprite(
                    ResourceRoot + $"character_stage_{i + 1:00}");
            }

            for (int i = 0; i < dumbbellSprites.Length; i++)
            {
                dumbbellSprites[i] = LivingGameplayVisualFactory.LoadSprite(
                    ResourceRoot + $"dumbbell_stage_{i + 1:00}");
            }

            proteinSprite =
                LivingGameplayVisualFactory.LoadSprite(ResourceRoot + "prop_protein");
            coachSprite =
                LivingGameplayVisualFactory.LoadSprite(ResourceRoot + "prop_coach");
        }

        private void BuildRooms()
        {
            roomBaseImage = CreateRoomImage("RoomStage01", roomStageOne);
            roomUpgradeImage = CreateRoomImage("RoomStage02", roomStageTwo);
            roomUpgradeGroup = roomUpgradeImage.gameObject.AddComponent<CanvasGroup>();
            roomUpgradeGroup.alpha = 0f;
        }

        private Image CreateRoomImage(string name, Sprite sprite)
        {
            Image image = LivingGameplayVisualFactory.CreateStretchImage(
                transform,
                name,
                sprite,
                Color.white);
            image.preserveAspect = false;

            AspectRatioFitter fitter = image.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 9f / 16f;
            return image;
        }

        private void BuildProps()
        {
            RectTransform propLayer =
                LivingGameplayVisualFactory.CreateStretchRect(transform, "UpgradeProps");

            proteinImage = LivingGameplayVisualFactory.CreateImage(
                propLayer,
                "ProteinUpgradeProp",
                new Vector2(0.5f, 0f),
                new Vector2(372f, 690f),
                new Vector2(230f, 230f),
                proteinSprite,
                Color.white);
            proteinGroup = proteinImage.gameObject.AddComponent<CanvasGroup>();
            proteinGroup.alpha = 0f;

            coachImage = LivingGameplayVisualFactory.CreateImage(
                propLayer,
                "CoachUpgradeProp",
                new Vector2(0.5f, 0f),
                new Vector2(-370f, 985f),
                new Vector2(255f, 255f),
                coachSprite,
                Color.white);
            coachGroup = coachImage.gameObject.AddComponent<CanvasGroup>();
            coachGroup.alpha = 0f;
        }

        private void BuildCharacter()
        {
            RectTransform characterRoot = LivingGameplayVisualFactory.CreateRect(
                transform,
                "CharacterRoot",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 935f),
                new Vector2(720f, 1280f));

            RectTransform animatorLayer =
                LivingGameplayVisualFactory.CreateStretchRect(characterRoot, "AnimatorLayer");
            animatorLayer.gameObject.AddComponent<CanvasGroup>();
            animatorLayer.gameObject.AddComponent<Animator>();

            characterImage = LivingGameplayVisualFactory.CreateStretchImage(
                animatorLayer,
                "Character",
                characterSprites[0],
                Color.white);
            characterImage.preserveAspect = true;
            characterGroup = characterImage.gameObject.AddComponent<CanvasGroup>();

            characterGhostImage = LivingGameplayVisualFactory.CreateStretchImage(
                animatorLayer,
                "CharacterTransitionGhost",
                characterSprites[0],
                Color.white);
            characterGhostImage.preserveAspect = true;
            characterGhostGroup =
                characterGhostImage.gameObject.AddComponent<CanvasGroup>();
            characterGhostGroup.alpha = 0f;

            RectTransform bellyMask = LivingGameplayVisualFactory.CreateRect(
                animatorLayer,
                "BellyJiggleMask",
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 95f),
                new Vector2(525f, 355f));
            bellyMask.gameObject.AddComponent<RectMask2D>();

            bellyImage = LivingGameplayVisualFactory.CreateImage(
                bellyMask,
                "BellyJiggleLayer",
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -95f),
                new Vector2(720f, 1280f),
                characterSprites[0],
                Color.white);

            Color eyelidColor = new Color(0.18f, 0.105f, 0.075f, 0f);
            Image leftEyelid = LivingGameplayVisualFactory.CreateImage(
                animatorLayer,
                "LeftEyelid",
                new Vector2(0.5f, 0.5f),
                new Vector2(-36f, 430f),
                new Vector2(44f, 12f),
                LivingGameplayVisualFactory.GetRoundedSprite(),
                eyelidColor);
            leftEyelid.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -6f);

            Image rightEyelid = LivingGameplayVisualFactory.CreateImage(
                animatorLayer,
                "RightEyelid",
                new Vector2(0.5f, 0.5f),
                new Vector2(36f, 430f),
                new Vector2(44f, 12f),
                LivingGameplayVisualFactory.GetRoundedSprite(),
                eyelidColor);
            rightEyelid.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 6f);

            RandomIdleScheduler scheduler =
                characterRoot.gameObject.AddComponent<RandomIdleScheduler>();
            characterAnimator =
                characterRoot.gameObject.AddComponent<GameplayAnimationController>();
            characterAnimator.Configure(
                characterRoot,
                animatorLayer,
                bellyImage.rectTransform,
                leftEyelid,
                rightEyelid,
                scheduler);
        }

        private void BuildDumbbell()
        {
            RectTransform dumbbellRoot = LivingGameplayVisualFactory.CreateRect(
                transform,
                "DumbbellRoot",
                new Vector2(0.5f, 0f),
                DumbbellScenePosition,
                new Vector2(790f, 445f));

            Image outerRing = LivingGameplayVisualFactory.CreateImage(
                dumbbellRoot,
                "DumbbellAuraOuter",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(900f, 350f),
                LivingGameplayVisualFactory.GetSoftCircleSprite(),
                new Color(0.03f, 0.47f, 1f, 0.12f));

            Image innerRing = LivingGameplayVisualFactory.CreateImage(
                dumbbellRoot,
                "DumbbellAuraInner",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(760f, 300f),
                LivingGameplayVisualFactory.GetSoftCircleSprite(),
                new Color(1f, 0.46f, 0.04f, 0.22f));

            Image shadow = LivingGameplayVisualFactory.CreateImage(
                dumbbellRoot,
                "DumbbellShadow",
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -104f),
                new Vector2(580f, 105f),
                LivingGameplayVisualFactory.GetSoftCircleSprite(),
                new Color(0f, 0f, 0f, 0.36f));

            dumbbellImage = LivingGameplayVisualFactory.CreateImage(
                dumbbellRoot,
                "Dumbbell",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(745f, 420f),
                dumbbellSprites[0],
                Color.white);
            dumbbellGroup = dumbbellImage.gameObject.AddComponent<CanvasGroup>();

            dumbbellGhostImage = LivingGameplayVisualFactory.CreateImage(
                dumbbellRoot,
                "DumbbellTransitionGhost",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(745f, 420f),
                dumbbellSprites[0],
                Color.white);
            dumbbellGhostGroup =
                dumbbellGhostImage.gameObject.AddComponent<CanvasGroup>();
            dumbbellGhostGroup.alpha = 0f;

            dumbbellAnimator =
                dumbbellRoot.gameObject.AddComponent<DumbbellTapAnimator>();
            dumbbellAnimator.Configure(
                dumbbellRoot,
                dumbbellImage,
                innerRing,
                outerRing,
                shadow.rectTransform);
        }

        private void ApplyCharacter(int artIndex, bool animate)
        {
            int safeIndex = Mathf.Clamp(artIndex, 0, characterSprites.Length - 1);
            Sprite next = characterSprites[safeIndex];
            if (next == null)
            {
                return;
            }

            if (characterTransition != null)
            {
                StopCoroutine(characterTransition);
            }

            if (!animate || characterImage.sprite == null)
            {
                characterImage.sprite = next;
                characterGroup.alpha = 1f;
                characterGhostGroup.alpha = 0f;
                bellyImage.sprite = next;
            }
            else
            {
                characterTransition = StartCoroutine(CharacterTransitionRoutine(next));
            }

            currentCharacterArt = safeIndex;
        }

        private IEnumerator CharacterTransitionRoutine(Sprite next)
        {
            characterGhostImage.sprite = characterImage.sprite;
            characterGhostGroup.alpha = 1f;
            characterImage.sprite = next;
            characterGroup.alpha = 0f;
            bellyImage.sprite = next;
            characterAnimator?.TriggerStageChange();

            float elapsed = 0f;
            const float duration = 0.62f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                characterGhostGroup.alpha = 1f - eased;
                characterGroup.alpha = eased;
                yield return null;
            }

            characterGhostGroup.alpha = 0f;
            characterGroup.alpha = 1f;
            characterTransition = null;
        }

        private void ApplyDumbbell(int artIndex, bool animate)
        {
            int safeIndex = Mathf.Clamp(artIndex, 0, dumbbellSprites.Length - 1);
            Sprite next = dumbbellSprites[safeIndex];
            if (next == null)
            {
                return;
            }

            if (dumbbellTransition != null)
            {
                StopCoroutine(dumbbellTransition);
            }

            if (!animate || dumbbellImage.sprite == null)
            {
                dumbbellImage.sprite = next;
                dumbbellGroup.alpha = 1f;
                dumbbellGhostGroup.alpha = 0f;
            }
            else
            {
                dumbbellTransition = StartCoroutine(DumbbellTransitionRoutine(next));
            }

            currentDumbbellArt = safeIndex;
        }

        private IEnumerator DumbbellTransitionRoutine(Sprite next)
        {
            dumbbellGhostImage.sprite = dumbbellImage.sprite;
            dumbbellGhostGroup.alpha = 1f;
            dumbbellImage.sprite = next;
            dumbbellGroup.alpha = 0f;

            float elapsed = 0f;
            const float duration = 0.42f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                dumbbellGhostGroup.alpha = 1f - t;
                dumbbellGroup.alpha = t;
                yield return null;
            }

            dumbbellGhostGroup.alpha = 0f;
            dumbbellGroup.alpha = 1f;
            dumbbellTransition = null;
        }

        private void ApplyRoom(bool improved, bool animate)
        {
            if (roomTransition != null)
            {
                StopCoroutine(roomTransition);
            }

            ambientAnimator?.SetImprovedRoom(improved);
            float target = improved ? 1f : 0f;
            if (!animate)
            {
                roomUpgradeGroup.alpha = target;
                return;
            }

            roomTransition = StartCoroutine(
                FadeCanvasGroup(roomUpgradeGroup, target, 0.72f));
        }

        private void ApplyProp(
            CanvasGroup group,
            bool visible,
            bool animate,
            ref Coroutine routine)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }

            float target = visible ? 1f : 0f;
            if (!animate)
            {
                group.alpha = target;
                group.transform.localScale = Vector3.one;
                routine = null;
                return;
            }

            routine = StartCoroutine(PopCanvasGroup(group, target));
        }

        private IEnumerator FadeCanvasGroup(
            CanvasGroup group,
            float target,
            float duration)
        {
            float from = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                group.alpha = Mathf.Lerp(from, target, t);
                yield return null;
            }

            group.alpha = target;
            roomTransition = null;
        }

        private IEnumerator PopCanvasGroup(CanvasGroup group, float target)
        {
            float from = group.alpha;
            float elapsed = 0f;
            const float duration = 0.42f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                group.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t));
                float pop = target > from
                    ? Mathf.Lerp(0.68f, 1f, 1f - Mathf.Pow(1f - t, 3f))
                    : Mathf.Lerp(1f, 0.76f, t);
                group.transform.localScale = Vector3.one * pop;
                yield return null;
            }

            group.alpha = target;
            group.transform.localScale = Vector3.one;
        }

        private static int GetUpgradeLevel(UpgradeManager manager, string id)
        {
            UpgradeData upgrade = manager != null ? manager.GetUpgrade(id) : null;
            return upgrade != null ? Mathf.Max(0, upgrade.level) : 0;
        }

        private static int ResolveCharacterArt(int bodyStageIndex)
        {
            if (bodyStageIndex <= 0)
            {
                return 0;
            }

            if (bodyStageIndex == 1)
            {
                return 1;
            }

            if (bodyStageIndex <= 3)
            {
                return 2;
            }

            return 3;
        }

        private static int ResolveDumbbellArt(int upgradeLevel)
        {
            if (upgradeLevel <= 0)
            {
                return 0;
            }

            return upgradeLevel < 4 ? 1 : 2;
        }
    }
}
