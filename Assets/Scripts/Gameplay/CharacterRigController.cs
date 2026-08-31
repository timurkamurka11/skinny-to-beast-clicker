using System.Collections.Generic;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    public enum CharacterFacing
    {
        Front,
        SideLeft,
        SideRight,
        Back
    }

    public enum CharacterRoutineAction
    {
        None,
        ShiftWeight,
        LookAround,
        Scratch,
        Yawn,
        Stretch,
        Flex,
        AdjustClothes,
        WarmShoulders,
        SitDown,
        SitLoop,
        StandUp,
        Sit
    }

    [DisallowMultipleComponent]
    public sealed class CharacterRigController : MonoBehaviour
    {
        private readonly Dictionary<string, RectTransform> namedBones = new();
        private readonly Dictionary<string, CharacterMeshGraphic> namedParts = new();
        private readonly List<CharacterMeshGraphic> meshRenderers = new();
        private readonly List<CharacterArtPart> artParts = new();

        private CharacterSkeletonDefinition skeletonDefinition;
        private CharacterSkinDefinition currentDefinition;
        private CharacterFaceController faceController;
        private CharacterAnimationDriver animationDriver;
        private CharacterViewController viewController;
        private CharacterIKController ikController;
        private CharacterSoftBodyController softBodyController;
        private Animator animator;

        private RectTransform characterRoot;
        private RectTransform visualRoot;
        private RectTransform skeletonRoot;
        private RectTransform rootBone;
        private RectTransform pelvisBone;
        private RectTransform spineBone;
        private RectTransform chestBone;
        private RectTransform bellyBone;
        private RectTransform shirtHemBone;
        private RectTransform chestSoftBone;
        private RectTransform neckBone;
        private RectTransform headBone;
        private RectTransform chinSoftBone;
        private RectTransform leftShoulderBone;
        private RectTransform rightShoulderBone;
        private RectTransform leftUpperArmBone;
        private RectTransform rightUpperArmBone;
        private RectTransform leftForearmBone;
        private RectTransform rightForearmBone;
        private RectTransform leftHandBone;
        private RectTransform rightHandBone;
        private RectTransform leftThighBone;
        private RectTransform rightThighBone;
        private RectTransform leftShinBone;
        private RectTransform rightShinBone;
        private RectTransform leftFootBone;
        private RectTransform rightFootBone;

        private Vector2 moveDirection;
        private CharacterFacing restingFacing = CharacterFacing.Front;
        private CharacterRoutineAction activeAction;
        private float activeActionUntil;
        private int tapVariant;
        private bool moving;
        private bool built;

        public int BoneCount => namedBones.Count;
        public int MeshPartCount => meshRenderers.Count;
        public int ArtPartCount => artParts.Count;
        public int SoftBoneCount =>
            softBodyController != null
                ? softBodyController.SoftBoneCount
                : 0;
        public float SoftBodyMotionMagnitude =>
            softBodyController != null
                ? softBodyController.MotionMagnitude
                : 0f;
        public bool IsMoving => moving;
        public CharacterFacing Facing => ResolveFacing();
        public CharacterRoutineAction ActiveAction => activeAction;
        public float ActiveActionRemaining =>
            Mathf.Max(0f, activeActionUntil - Time.unscaledTime);
        public int ActiveTapVariant => tapVariant;
        public bool IsTapReacting =>
            animationDriver != null &&
            ActiveActionRemaining > 0f;
        public bool HasAppliedSkin => currentDefinition != null;
        public bool AnimatorReady =>
            animationDriver != null && animationDriver.IsReady;
        public string AnimatorReadinessError =>
            animationDriver != null
                ? animationDriver.ReadinessError
                : "Character animation driver is missing.";
        public int AcceptedTapCount =>
            animationDriver != null
                ? animationDriver.AcceptedTapCount
                : 0;
        public int ObservedIdleActionCount =>
            animationDriver != null
                ? animationDriver.ObservedIdleActionCount
                : 0;
        public RectTransform VisualRoot => visualRoot;

        public bool HasVisibleSkin
        {
            get
            {
                if (!built ||
                    currentDefinition == null ||
                    characterRoot == null ||
                    !characterRoot.gameObject.activeInHierarchy ||
                    skeletonRoot == null ||
                    !skeletonRoot.gameObject.activeInHierarchy ||
                    GetVisibleGraphicCount() < 18)
                {
                    return false;
                }

                Bounds bounds = GetWorldGeometryBounds();
                return bounds.size.x > 10f &&
                       bounds.size.y > 10f &&
                       characterRoot.lossyScale.sqrMagnitude > 0.0001f;
            }
        }

        public void Build(
            RectTransform root,
            CharacterFaceController face)
        {
            if (built || root == null)
            {
                return;
            }

            characterRoot = root;
            faceController = face;
            skeletonDefinition =
                CharacterSkeletonDefinition.CreateDefault();

            visualRoot = CreateRect(
                characterRoot,
                "VisualRoot",
                Vector2.zero,
                skeletonDefinition.canvasSize);
            skeletonRoot = CreateRect(
                visualRoot,
                "Skeleton",
                Vector2.zero,
                skeletonDefinition.canvasSize);

            rootBone = CreateBone(
                skeletonRoot,
                "Bone.Root",
                Vector2.zero);
            pelvisBone = CreateBone(
                rootBone,
                "Bone.Pelvis",
                skeletonDefinition.pelvis);

            BuildLegs();
            BuildTorsoAndArms();
            BuildHead();

            softBodyController =
                GetOrAdd<CharacterSoftBodyController>(
                    characterRoot.gameObject);
            softBodyController.Configure(
                characterRoot,
                bellyBone,
                shirtHemBone,
                chestSoftBone,
                chinSoftBone);

            animator = GetOrAdd<Animator>(characterRoot.gameObject);
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            animationDriver =
                GetOrAdd<CharacterAnimationDriver>(
                    characterRoot.gameObject);
            animationDriver.Configure(animator);
            faceController?.ConfigureAnimationDriver(
                animationDriver);

            viewController =
                GetOrAdd<CharacterViewController>(
                    characterRoot.gameObject);
            viewController.Configure(visualRoot, faceController);
            viewController.SetBasePresentationScale(
                skeletonDefinition.presentationScale);

            ikController =
                GetOrAdd<CharacterIKController>(
                    characterRoot.gameObject);
            ikController.Configure(
                leftThighBone,
                leftShinBone,
                leftFootBone,
                rightThighBone,
                rightShinBone,
                rightFootBone,
                animator);

            built = true;
            ClearSkin();
        }

        private void BuildLegs()
        {
            leftThighBone = CreateBone(
                pelvisBone,
                "Bone.Thigh.L",
                skeletonDefinition.leftHip);
            rightThighBone = CreateBone(
                pelvisBone,
                "Bone.Thigh.R",
                skeletonDefinition.rightHip);

            CreatePart(
                leftThighBone,
                "Part.Thigh.L",
                CharacterMeshShape.FatThigh,
                CharacterVisualRole.Bottom,
                new Vector2(122f, 232f),
                new Vector2(0f, 20f),
                new Vector2(0.5f, 1f));
            CreatePart(
                rightThighBone,
                "Part.Thigh.R",
                CharacterMeshShape.FatThigh,
                CharacterVisualRole.Bottom,
                new Vector2(122f, 232f),
                new Vector2(0f, 20f),
                new Vector2(0.5f, 1f));
            CreatePart(
                leftThighBone,
                "Detail.ShortsFold.L",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.BottomShadow,
                new Vector2(64f, 10f),
                new Vector2(-10f, -112f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);
            CreatePart(
                rightThighBone,
                "Detail.ShortsFold.R",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.BottomShadow,
                new Vector2(64f, 10f),
                new Vector2(10f, -112f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);

            leftShinBone = CreateBone(
                leftThighBone,
                "Bone.Shin.L",
                skeletonDefinition.knee);
            rightShinBone = CreateBone(
                rightThighBone,
                "Bone.Shin.R",
                skeletonDefinition.knee);
            CreatePart(
                leftShinBone,
                "Part.Shin.L",
                CharacterMeshShape.FatCalf,
                CharacterVisualRole.Skin,
                new Vector2(91f, 208f),
                new Vector2(0f, 18f),
                new Vector2(0.5f, 1f));
            CreatePart(
                rightShinBone,
                "Part.Shin.R",
                CharacterMeshShape.FatCalf,
                CharacterVisualRole.Skin,
                new Vector2(91f, 208f),
                new Vector2(0f, 18f),
                new Vector2(0.5f, 1f));
            CreatePart(
                leftShinBone,
                "Detail.CalfShade.L",
                CharacterMeshShape.Stain,
                CharacterVisualRole.SkinShadow,
                new Vector2(34f, 101f),
                new Vector2(-21f, -60f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);
            CreatePart(
                rightShinBone,
                "Detail.CalfShade.R",
                CharacterMeshShape.Stain,
                CharacterVisualRole.SkinShadow,
                new Vector2(34f, 101f),
                new Vector2(21f, -60f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);
            CreatePart(
                leftShinBone,
                "Detail.KneeCrease.L",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.SkinShadow,
                new Vector2(54f, 8f),
                new Vector2(5f, 4f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false,
                true,
                true,
                false);
            CreatePart(
                rightShinBone,
                "Detail.KneeCrease.R",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.SkinShadow,
                new Vector2(54f, 8f),
                new Vector2(-5f, 4f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false,
                true,
                true,
                false);

            leftFootBone = CreateBone(
                leftShinBone,
                "Bone.Foot.L",
                skeletonDefinition.ankle);
            rightFootBone = CreateBone(
                rightShinBone,
                "Bone.Foot.R",
                skeletonDefinition.ankle);
            CharacterMeshGraphic leftShoe = CreatePart(
                leftFootBone,
                "Part.Foot.L",
                CharacterMeshShape.WornShoe,
                CharacterVisualRole.Shoe,
                new Vector2(145f, 72f),
                new Vector2(-18f, -31f),
                new Vector2(0.5f, 0.5f));
            leftShoe.rectTransform.localScale =
                new Vector3(-1f, 1f, 1f);
            CreatePart(
                rightFootBone,
                "Part.Foot.R",
                CharacterMeshShape.WornShoe,
                CharacterVisualRole.Shoe,
                new Vector2(145f, 72f),
                new Vector2(18f, -31f),
                new Vector2(0.5f, 0.5f));
            CreatePart(
                leftFootBone,
                "Detail.ShoeSole.L",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.ShoeDetail,
                new Vector2(118f, 11f),
                new Vector2(-14f, -54f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);
            CreatePart(
                rightFootBone,
                "Detail.ShoeSole.R",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.ShoeDetail,
                new Vector2(118f, 11f),
                new Vector2(14f, -54f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);
        }

        private void BuildTorsoAndArms()
        {
            CreatePart(
                pelvisBone,
                "Part.Pelvis",
                CharacterMeshShape.FatPelvis,
                CharacterVisualRole.Bottom,
                new Vector2(270f, 158f),
                new Vector2(0f, -20f),
                new Vector2(0.5f, 0.5f));
            CreatePart(
                pelvisBone,
                "Detail.Waistband",
                CharacterMeshShape.Waistband,
                CharacterVisualRole.BottomShadow,
                new Vector2(246f, 34f),
                new Vector2(0f, 35f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                1f,
                false);
            CreatePart(
                pelvisBone,
                "Detail.Drawstring.Knot",
                CharacterMeshShape.Stain,
                CharacterVisualRole.BottomDetail,
                new Vector2(23f, 18f),
                new Vector2(0f, 18f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                1f,
                false,
                true,
                true,
                false);
            CreatePart(
                pelvisBone,
                "Detail.Drawstring.L",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.BottomDetail,
                new Vector2(8f, 55f),
                new Vector2(-8f, -4f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false,
                true,
                true,
                false);
            CreatePart(
                pelvisBone,
                "Detail.Drawstring.R",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.BottomDetail,
                new Vector2(8f, 55f),
                new Vector2(8f, -4f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false,
                true,
                true,
                false);
            CreatePart(
                pelvisBone,
                "Detail.BackPocket.L",
                CharacterMeshShape.Pocket,
                CharacterVisualRole.BottomDetail,
                new Vector2(82f, 69f),
                new Vector2(-75f, -24f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                2f,
                false,
                false,
                false,
                true);
            CreatePart(
                pelvisBone,
                "Detail.BackPocket.R",
                CharacterMeshShape.Pocket,
                CharacterVisualRole.BottomDetail,
                new Vector2(82f, 69f),
                new Vector2(75f, -24f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                2f,
                false,
                false,
                false,
                true);

            spineBone = CreateBone(
                pelvisBone,
                "Bone.Spine",
                skeletonDefinition.spine);
            bellyBone = CreateBone(
                spineBone,
                "Bone.Belly",
                new Vector2(0f, 18f));
            CreatePart(
                bellyBone,
                "Part.Abdomen",
                CharacterMeshShape.FatBelly,
                CharacterVisualRole.Top,
                new Vector2(270f, 220f),
                new Vector2(0f, 24f),
                new Vector2(0.5f, 0.5f),
                0.90f,
                1.18f);
            CreatePart(
                bellyBone,
                "Detail.BellyBand",
                CharacterMeshShape.BellyBand,
                CharacterVisualRole.Skin,
                new Vector2(258f, 64f),
                new Vector2(0f, -96f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                2f,
                false);
            CreatePart(
                bellyBone,
                "Detail.SideBellyProfile",
                CharacterMeshShape.FatBelly,
                CharacterVisualRole.Top,
                new Vector2(145f, 194f),
                new Vector2(138f, 15f),
                new Vector2(0.5f, 0.5f),
                0.82f,
                1.16f,
                4f,
                false,
                false,
                true,
                false);
            CreatePart(
                bellyBone,
                "Detail.ShirtStain.Belly",
                CharacterMeshShape.Stain,
                CharacterVisualRole.TopStain,
                new Vector2(82f, 49f),
                new Vector2(-48f, 26f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false,
                true,
                true,
                true);
            CreatePart(
                bellyBone,
                "Detail.ShirtFold.Belly.L",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.TopShadow,
                new Vector2(86f, 9f),
                new Vector2(-66f, -43f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);
            CreatePart(
                bellyBone,
                "Detail.ShirtFold.Belly.R",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.TopShadow,
                new Vector2(72f, 8f),
                new Vector2(70f, -24f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);
            shirtHemBone = CreateBone(
                bellyBone,
                "Bone.ShirtHem",
                new Vector2(0f, -73f));
            CreatePart(
                shirtHemBone,
                "Part.ShirtHem",
                CharacterMeshShape.ShirtHem,
                CharacterVisualRole.Top,
                new Vector2(292f, 66f),
                Vector2.zero,
                new Vector2(0.5f, 0.5f));

            chestBone = CreateBone(
                spineBone,
                "Bone.Chest",
                skeletonDefinition.chest);
            chestSoftBone = CreateBone(
                chestBone,
                "Bone.ChestSoft",
                Vector2.zero);
            CreatePart(
                chestSoftBone,
                "Part.Chest",
                CharacterMeshShape.FatChest,
                CharacterVisualRole.Top,
                new Vector2(330f, 250f),
                Vector2.zero,
                new Vector2(0.5f, 0.5f),
                1.02f,
                0.92f);
            CreatePart(
                chestSoftBone,
                "Detail.NecklineSkin",
                CharacterMeshShape.Neckline,
                CharacterVisualRole.Skin,
                new Vector2(184f, 108f),
                new Vector2(0f, 86f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                3f,
                false,
                true,
                true,
                false);
            CreatePart(
                chestSoftBone,
                "Detail.ChestHair.L",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.Hair,
                new Vector2(43f, 5f),
                new Vector2(-18f, 63f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false,
                true,
                false,
                false);
            CreatePart(
                chestSoftBone,
                "Detail.ChestHair.R",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.Hair,
                new Vector2(40f, 5f),
                new Vector2(20f, 55f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false,
                true,
                false,
                false);
            CreatePart(
                chestSoftBone,
                "Detail.ShirtStain.Chest",
                CharacterMeshShape.Stain,
                CharacterVisualRole.TopStain,
                new Vector2(56f, 42f),
                new Vector2(82f, -8f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);
            CreatePart(
                chestSoftBone,
                "Detail.ShirtChestShade",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.TopShadow,
                new Vector2(238f, 15f),
                new Vector2(0f, -72f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);
            CreatePart(
                chestSoftBone,
                "Part.ChestAccent",
                CharacterMeshShape.Ellipse,
                CharacterVisualRole.Accent,
                new Vector2(48f, 48f),
                new Vector2(0f, 28f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                2f,
                false);

            leftShoulderBone = CreateBone(
                chestBone,
                "Bone.Shoulder.L",
                skeletonDefinition.leftShoulder);
            rightShoulderBone = CreateBone(
                chestBone,
                "Bone.Shoulder.R",
                skeletonDefinition.rightShoulder);
            CreatePart(
                leftShoulderBone,
                "Part.Shoulder.L",
                CharacterMeshShape.FatShoulder,
                CharacterVisualRole.Skin,
                new Vector2(104f, 108f),
                Vector2.zero,
                new Vector2(0.5f, 0.5f));
            CreatePart(
                rightShoulderBone,
                "Part.Shoulder.R",
                CharacterMeshShape.FatShoulder,
                CharacterVisualRole.Skin,
                new Vector2(104f, 108f),
                Vector2.zero,
                new Vector2(0.5f, 0.5f));
            CreatePart(
                leftShoulderBone,
                "Detail.ShoulderHighlight.L",
                CharacterMeshShape.Stain,
                CharacterVisualRole.SkinHighlight,
                new Vector2(38f, 46f),
                new Vector2(-14f, 17f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);
            CreatePart(
                rightShoulderBone,
                "Detail.ShoulderHighlight.R",
                CharacterMeshShape.Stain,
                CharacterVisualRole.SkinHighlight,
                new Vector2(38f, 46f),
                new Vector2(14f, 17f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);

            leftUpperArmBone = CreateBone(
                leftShoulderBone,
                "Bone.UpperArm.L",
                Vector2.zero);
            rightUpperArmBone = CreateBone(
                rightShoulderBone,
                "Bone.UpperArm.R",
                Vector2.zero);
            CreatePart(
                leftUpperArmBone,
                "Part.UpperArm.L",
                CharacterMeshShape.FatUpperArm,
                CharacterVisualRole.Skin,
                new Vector2(98f, 190f),
                new Vector2(0f, 20f),
                new Vector2(0.5f, 1f));
            CreatePart(
                rightUpperArmBone,
                "Part.UpperArm.R",
                CharacterMeshShape.FatUpperArm,
                CharacterVisualRole.Skin,
                new Vector2(98f, 190f),
                new Vector2(0f, 20f),
                new Vector2(0.5f, 1f));
            CreatePart(
                leftUpperArmBone,
                "Detail.UpperArmShade.L",
                CharacterMeshShape.Stain,
                CharacterVisualRole.SkinShadow,
                new Vector2(38f, 88f),
                new Vector2(-24f, -58f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);
            CreatePart(
                rightUpperArmBone,
                "Detail.UpperArmShade.R",
                CharacterMeshShape.Stain,
                CharacterVisualRole.SkinShadow,
                new Vector2(38f, 88f),
                new Vector2(24f, -58f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false);

            leftForearmBone = CreateBone(
                leftUpperArmBone,
                "Bone.Forearm.L",
                skeletonDefinition.elbow);
            rightForearmBone = CreateBone(
                rightUpperArmBone,
                "Bone.Forearm.R",
                skeletonDefinition.elbow);
            CreatePart(
                leftForearmBone,
                "Part.Forearm.L",
                CharacterMeshShape.FatForearm,
                CharacterVisualRole.Skin,
                new Vector2(87f, 172f),
                new Vector2(0f, 18f),
                new Vector2(0.5f, 1f));
            CreatePart(
                rightForearmBone,
                "Part.Forearm.R",
                CharacterMeshShape.FatForearm,
                CharacterVisualRole.Skin,
                new Vector2(87f, 172f),
                new Vector2(0f, 18f),
                new Vector2(0.5f, 1f));
            CreatePart(
                leftForearmBone,
                "Detail.ElbowCrease.L",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.SkinShadow,
                new Vector2(53f, 8f),
                new Vector2(9f, 0f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false,
                true,
                true,
                false);
            CreatePart(
                rightForearmBone,
                "Detail.ElbowCrease.R",
                CharacterMeshShape.FabricFold,
                CharacterVisualRole.SkinShadow,
                new Vector2(53f, 8f),
                new Vector2(-9f, 0f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                0f,
                false,
                true,
                true,
                false);
            CreatePart(
                leftForearmBone,
                "Part.WristWrap.L",
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Accent,
                new Vector2(76f, 28f),
                new Vector2(0f, -117f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                2f,
                false);
            CreatePart(
                rightForearmBone,
                "Part.WristWrap.R",
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Accent,
                new Vector2(76f, 28f),
                new Vector2(0f, -117f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                2f,
                false);

            leftHandBone = CreateBone(
                leftForearmBone,
                "Bone.Hand.L",
                skeletonDefinition.wrist);
            rightHandBone = CreateBone(
                rightForearmBone,
                "Bone.Hand.R",
                skeletonDefinition.wrist);
            CreatePart(
                leftHandBone,
                "Part.Hand.L",
                CharacterMeshShape.FatHand,
                CharacterVisualRole.Skin,
                new Vector2(88f, 101f),
                new Vector2(0f, -34f),
                new Vector2(0.5f, 0.5f));
            CreatePart(
                rightHandBone,
                "Part.Hand.R",
                CharacterMeshShape.FatHand,
                CharacterVisualRole.Skin,
                new Vector2(88f, 101f),
                new Vector2(0f, -34f),
                new Vector2(0.5f, 0.5f));
        }

        private void BuildHead()
        {
            neckBone = CreateBone(
                chestBone,
                "Bone.Neck",
                skeletonDefinition.neck);
            CreatePart(
                neckBone,
                "Part.Neck",
                CharacterMeshShape.FatNeck,
                CharacterVisualRole.Skin,
                new Vector2(116f, 110f),
                new Vector2(0f, 31f),
                new Vector2(0.5f, 0.5f));

            headBone = CreateBone(
                neckBone,
                "Bone.Head",
                skeletonDefinition.head);
            CreatePart(
                headBone,
                "Part.Head",
                CharacterMeshShape.FatHead,
                CharacterVisualRole.Skin,
                new Vector2(230f, 254f),
                new Vector2(0f, 87f),
                new Vector2(0.5f, 0.5f));
            CreatePart(
                headBone,
                "Part.HairBack",
                CharacterMeshShape.MessyHair,
                CharacterVisualRole.Hair,
                new Vector2(242f, 148f),
                new Vector2(0f, 168f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                4f);
            CreatePart(
                headBone,
                "Part.Ear.L",
                CharacterMeshShape.Ear,
                CharacterVisualRole.Skin,
                new Vector2(40f, 65f),
                new Vector2(-112f, 91f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                3f,
                false);
            CreatePart(
                headBone,
                "Part.Ear.R",
                CharacterMeshShape.Ear,
                CharacterVisualRole.Skin,
                new Vector2(40f, 65f),
                new Vector2(112f, 91f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                3f,
                false);

            chinSoftBone = CreateBone(
                headBone,
                "Bone.ChinSoft",
                new Vector2(0f, 30f));
            CreatePart(
                chinSoftBone,
                "Part.DoubleChin",
                CharacterMeshShape.DoubleChin,
                CharacterVisualRole.SkinShadow,
                new Vector2(158f, 64f),
                Vector2.zero,
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                2f,
                true,
                true,
                true,
                false);

            faceController?.Build(headBone);
        }

        public void ApplySkin(CharacterSkinDefinition definition)
        {
            if (!built || definition == null || !definition.IsValid)
            {
                return;
            }

            currentDefinition = definition;
            CharacterAppearance appearance = definition.Appearance;
            ApplyAppearance(appearance);
            for (int i = 0; i < meshRenderers.Count; i++)
            {
                CharacterMeshGraphic part = meshRenderers[i];
                part.enabled = true;
                part.gameObject.SetActive(true);
                part.SetOutline(appearance.outline);
                part.SetFill(ResolveRoleColor(part.Role, appearance));
                part.ForceRenderRefresh();
            }

            faceController?.ApplyStyle(definition.FaceStyle);
            faceController?.SetVisible(true);
            skeletonRoot.gameObject.SetActive(true);
            viewController?.SetBasePresentationScale(
                skeletonDefinition.presentationScale *
                appearance.heightScale);
            viewController?.SetFacing(ResolveFacing());
        }

        public void ClearSkin()
        {
            currentDefinition = null;
            for (int i = 0; i < meshRenderers.Count; i++)
            {
                Color clear = meshRenderers[i].color;
                clear.a = 0f;
                meshRenderers[i].SetFill(clear);
            }

            faceController?.SetVisible(false);
        }

        public bool EnsureSkinVisible()
        {
            if (!built || currentDefinition == null)
            {
                return false;
            }

            if (!characterRoot.gameObject.activeSelf)
            {
                characterRoot.gameObject.SetActive(true);
            }

            if (!skeletonRoot.gameObject.activeSelf)
            {
                skeletonRoot.gameObject.SetActive(true);
            }

            ApplySkin(currentDefinition);
            Canvas.ForceUpdateCanvases();
            return HasVisibleSkin;
        }

        public void SynchronizeAnimationState()
        {
            activeAction = CharacterRoutineAction.None;
            activeActionUntil = 0f;
            moving = false;
            moveDirection = Vector2.zero;
            animationDriver?.ResetState();
            viewController?.SetFacing(restingFacing);
            ikController?.SetLocomotion(false, restingFacing);
            softBodyController?.ResetState();
        }

        public void SetLocomotion(Vector2 direction, float speed)
        {
            if (!built)
            {
                return;
            }

            moveDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector2.down;
            moving = speed > 0.001f;
            CharacterFacing nextFacing = ResolveFacing();
            viewController?.SetFacing(nextFacing);
            animationDriver?.SetLocomotion(
                nextFacing,
                Mathf.Max(0f, speed),
                moving);
            ikController?.SetLocomotion(moving, nextFacing);
            softBodyController?.SetLocomotion(
                moving,
                Mathf.Max(0f, speed));
        }

        public void StopLocomotion(CharacterFacing facing)
        {
            moving = false;
            moveDirection = Vector2.zero;
            restingFacing = facing;
            viewController?.SetFacing(facing);
            animationDriver?.SetLocomotion(facing, 0f, false);
            ikController?.SetLocomotion(false, facing);
            softBodyController?.SetLocomotion(false, 0f);
        }

        public void PlayEntryWalk(float speed = 1f)
        {
            moveDirection = Vector2.up;
            moving = true;
            restingFacing = CharacterFacing.Back;
            viewController?.SetFacing(CharacterFacing.Back);
            animationDriver?.PlayEntryWalk(speed);
            ikController?.SetLocomotion(
                true,
                CharacterFacing.Back);
            softBodyController?.SetLocomotion(
                true,
                Mathf.Max(0.2f, speed));
        }

        public void SetRestingFacing(CharacterFacing facing)
        {
            restingFacing = facing;
            if (!moving)
            {
                viewController?.SetFacing(facing);
                animationDriver?.SetLocomotion(facing, 0f, false);
            }
        }

        public void PlayAction(
            CharacterRoutineAction action,
            float duration)
        {
            if (!built || action == CharacterRoutineAction.None)
            {
                return;
            }

            activeAction = action;
            activeActionUntil =
                Time.unscaledTime + Mathf.Max(0.12f, duration);
            animationDriver?.PlayRoutineAction(action, duration);
            if (action == CharacterRoutineAction.SitDown ||
                action == CharacterRoutineAction.StandUp ||
                action == CharacterRoutineAction.Flex ||
                action == CharacterRoutineAction.AdjustClothes)
            {
                softBodyController?.AddImpulse(
                    action == CharacterRoutineAction.Flex
                        ? 0.82f
                        : 0.55f);
            }

            if (action == CharacterRoutineAction.Yawn)
            {
                faceController?.SetExpression(
                    CharacterExpression.Yawn,
                    duration);
            }
            else if (action == CharacterRoutineAction.Flex)
            {
                faceController?.SetExpression(
                    CharacterExpression.Happy,
                    duration);
            }
            else if (action == CharacterRoutineAction.Scratch ||
                     action == CharacterRoutineAction.WarmShoulders)
            {
                faceController?.SetExpression(
                    CharacterExpression.Focused,
                    duration);
            }
        }

        public void CancelAction()
        {
            activeAction = CharacterRoutineAction.None;
            activeActionUntil = 0f;
            animationDriver?.CancelActions();
            faceController?.ResetExpression();
        }

        public void TriggerTap()
        {
            if (!built)
            {
                return;
            }

            tapVariant = animationDriver != null
                ? animationDriver.TriggerTap()
                : (tapVariant + 1) % 3;
            activeActionUntil = Time.unscaledTime + 0.54f;
            softBodyController?.AddImpulse(
                0.92f + tapVariant * 0.12f);
            faceController?.LookAt(new Vector2(0f, -1f), 0.42f);
            faceController?.SetExpression(
                CharacterExpression.Strain,
                0.46f);
        }

        public void TriggerUpgrade()
        {
            animationDriver?.TriggerUpgrade();
            softBodyController?.AddImpulse(1.15f);
            faceController?.SetExpression(
                CharacterExpression.Happy,
                0.9f);
        }

        public void TriggerStageChange()
        {
            animationDriver?.TriggerStageChange();
            softBodyController?.AddImpulse(1.42f);
            faceController?.SetExpression(
                CharacterExpression.Happy,
                0.82f);
        }

        public bool StressTapAnimator(int tapCount)
        {
            return animationDriver != null &&
                   animationDriver.StressTap(tapCount);
        }

        public void ResetObservedIdleActionHistory()
        {
            animationDriver?.ClearObservedIdleActions();
        }

        public void ResetFootPlantDiagnostics()
        {
            ikController?.ResetDiagnostics();
        }

        public float FootPlantError =>
            ikController != null
                ? ikController.LastPlantError
                : float.PositiveInfinity;

        public bool HasBone(string boneName)
        {
            return !string.IsNullOrEmpty(boneName) &&
                   namedBones.ContainsKey(boneName) &&
                   namedBones[boneName] != null;
        }

        public RectTransform GetBone(string boneName)
        {
            namedBones.TryGetValue(boneName, out RectTransform bone);
            return bone;
        }

        public int GetCharacterTextureCount()
        {
            return 0;
        }

        public int GetVisibleGraphicCount()
        {
            int count = 0;
            for (int i = 0; i < meshRenderers.Count; i++)
            {
                if (meshRenderers[i] != null &&
                    meshRenderers[i].HasRenderableGeometry)
                {
                    count++;
                }
            }

            return count;
        }

        public Bounds GetWorldGeometryBounds()
        {
            Bounds bounds = default;
            bool initialized = false;
            Vector3[] corners = new Vector3[4];
            for (int i = 0; i < meshRenderers.Count; i++)
            {
                CharacterMeshGraphic graphic = meshRenderers[i];
                if (graphic == null || !graphic.HasRenderableGeometry)
                {
                    continue;
                }

                graphic.rectTransform.GetWorldCorners(corners);
                for (int corner = 0; corner < corners.Length; corner++)
                {
                    if (!initialized)
                    {
                        bounds = new Bounds(corners[corner], Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(corners[corner]);
                    }
                }
            }

            return bounds;
        }

        public bool ValidateJointContinuity(out string error)
        {
            if (!ValidateJoint(
                    leftShoulderBone,
                    "Part.Shoulder.L",
                    "Part.UpperArm.L") ||
                !ValidateJoint(
                    rightShoulderBone,
                    "Part.Shoulder.R",
                    "Part.UpperArm.R"))
            {
                error =
                    "A shoulder joint is outside one of its overlapping meshes.";
                return false;
            }

            if (!ValidateJoint(
                    leftForearmBone,
                    "Part.UpperArm.L",
                    "Part.Forearm.L") ||
                !ValidateJoint(
                    rightForearmBone,
                    "Part.UpperArm.R",
                    "Part.Forearm.R"))
            {
                error =
                    "An elbow joint is outside one of its overlapping meshes.";
                return false;
            }

            if (!ValidateJoint(
                    leftShinBone,
                    "Part.Thigh.L",
                    "Part.Shin.L") ||
                !ValidateJoint(
                    rightShinBone,
                    "Part.Thigh.R",
                    "Part.Shin.R"))
            {
                error =
                    "A knee joint is outside one of its overlapping meshes.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private void Update()
        {
            animationDriver?.Tick();
            if (activeAction != CharacterRoutineAction.None &&
                Time.unscaledTime >= activeActionUntil)
            {
                activeAction = CharacterRoutineAction.None;
            }
        }

        private CharacterFacing ResolveFacing()
        {
            if (!moving)
            {
                return restingFacing;
            }

            if (Mathf.Abs(moveDirection.x) >
                Mathf.Abs(moveDirection.y) * 0.65f)
            {
                return moveDirection.x < 0f
                    ? CharacterFacing.SideLeft
                    : CharacterFacing.SideRight;
            }

            return moveDirection.y > 0f
                ? CharacterFacing.Back
                : CharacterFacing.Front;
        }

        private void ApplyAppearance(CharacterAppearance appearance)
        {
            float shoulderOffset =
                Mathf.Abs(skeletonDefinition.leftShoulder.x) *
                appearance.shoulderWidth;
            float shoulderDrop = appearance.slouch * 15f;
            leftShoulderBone.anchoredPosition =
                new Vector2(
                    -shoulderOffset,
                    skeletonDefinition.leftShoulder.y -
                    shoulderDrop);
            rightShoulderBone.anchoredPosition =
                new Vector2(
                    shoulderOffset,
                    skeletonDefinition.rightShoulder.y -
                    shoulderDrop);
            chestBone.anchoredPosition =
                skeletonDefinition.chest +
                new Vector2(0f, -appearance.slouch * 10f);
            neckBone.anchoredPosition =
                skeletonDefinition.neck +
                new Vector2(0f, -appearance.slouch * 8f);

            SetPartSize(
                "Part.Chest",
                new Vector2(
                    330f * appearance.chestWidth,
                    250f));
            SetPartWidthProfile(
                "Part.Chest",
                1.02f,
                Mathf.Lerp(
                    0.86f,
                    1.06f,
                    Mathf.InverseLerp(
                        1.12f,
                        1.72f,
                        appearance.bellyWidth)));
            SetPartSize(
                "Part.Abdomen",
                new Vector2(
                    270f * appearance.bellyWidth,
                    220f +
                    appearance.softness * 13f));
            SetPartSize(
                "Part.ShirtHem",
                new Vector2(
                    270f * appearance.bellyWidth,
                    66f +
                    appearance.softness * 8f));
            SetPartSize(
                "Detail.BellyBand",
                new Vector2(
                    245f * appearance.bellyWidth,
                    64f +
                    appearance.softness * 7f));
            SetPartSize(
                "Detail.SideBellyProfile",
                new Vector2(
                    145f * appearance.sideDepth,
                    194f +
                    appearance.softness * 16f));
            SetPartSize(
                "Part.Pelvis",
                new Vector2(
                    270f * appearance.hipWidth,
                    158f));
            SetPartSize(
                "Detail.Waistband",
                new Vector2(
                    246f * appearance.hipWidth,
                    34f));

            SetPairSize(
                "Part.Shoulder",
                new Vector2(
                    104f * appearance.armWidth,
                    108f * appearance.armWidth));
            SetPairSize(
                "Part.UpperArm",
                new Vector2(
                    98f * appearance.armWidth,
                    190f));
            SetPairSize(
                "Part.Forearm",
                new Vector2(
                    87f * appearance.armWidth,
                    172f));
            SetPairSize(
                "Part.Hand",
                new Vector2(
                    88f * appearance.armWidth,
                    101f * appearance.armWidth));
            SetPairSize(
                "Part.Thigh",
                new Vector2(
                    122f * appearance.legWidth,
                    232f));
            SetPairSize(
                "Part.Shin",
                new Vector2(
                    91f * appearance.legWidth,
                    208f));
            SetPairSize(
                "Part.Foot",
                new Vector2(
                    145f * appearance.legWidth,
                    72f * appearance.legWidth));
            SetPartSize(
                "Part.Neck",
                new Vector2(
                    116f * appearance.chinScale,
                    110f));
            SetPartSize(
                "Part.Head",
                new Vector2(
                    230f * appearance.headScale,
                    254f * appearance.headScale));
            SetPartSize(
                "Part.HairBack",
                new Vector2(
                    242f * appearance.headScale,
                    148f * appearance.headScale));
            SetPartSize(
                "Part.DoubleChin",
                new Vector2(
                    158f * appearance.chinScale,
                    64f * appearance.chinScale));
            SetPairSize(
                "Part.Ear",
                new Vector2(
                    40f * appearance.headScale,
                    65f * appearance.headScale));

            softBodyController?.ApplyAppearance(appearance);
        }

        private static Color ResolveRoleColor(
            CharacterVisualRole role,
            CharacterAppearance appearance)
        {
            return role switch
            {
                CharacterVisualRole.Skin => appearance.skin,
                CharacterVisualRole.SkinShadow =>
                    Shade(appearance.skin, 0.72f, 0.56f),
                CharacterVisualRole.SkinHighlight =>
                    Shade(appearance.skin, 1.15f, 0.48f),
                CharacterVisualRole.Hair => appearance.hair,
                CharacterVisualRole.Top => appearance.top,
                CharacterVisualRole.TopShadow =>
                    Shade(appearance.top, 0.58f, 0.64f),
                CharacterVisualRole.TopHighlight =>
                    Shade(appearance.top, 1.18f, 0.46f),
                CharacterVisualRole.TopStain =>
                    new Color(
                        0.19f,
                        0.16f,
                        0.12f,
                        Mathf.Lerp(
                            0.10f,
                            0.48f,
                            appearance.shirtWear)),
                CharacterVisualRole.Bottom => appearance.bottom,
                CharacterVisualRole.BottomShadow =>
                    Shade(appearance.bottom, 0.56f, 0.78f),
                CharacterVisualRole.BottomDetail =>
                    Shade(appearance.bottom, 1.38f, 0.68f),
                CharacterVisualRole.Shoe => appearance.shoes,
                CharacterVisualRole.ShoeDetail =>
                    Shade(appearance.shoes, 0.48f, 0.88f),
                CharacterVisualRole.Accent => appearance.accentVisible
                    ? appearance.accent
                    : new Color(
                        appearance.accent.r,
                        appearance.accent.g,
                        appearance.accent.b,
                        0f),
                _ => Color.white
            };
        }

        private static Color Shade(
            Color source,
            float brightness,
            float alpha)
        {
            return new Color(
                Mathf.Clamp01(source.r * brightness),
                Mathf.Clamp01(source.g * brightness),
                Mathf.Clamp01(source.b * brightness),
                Mathf.Clamp01(source.a * alpha));
        }

        private void SetPairSize(string baseName, Vector2 size)
        {
            SetPartSize($"{baseName}.L", size);
            SetPartSize($"{baseName}.R", size);
        }

        private void SetPartSize(string name, Vector2 size)
        {
            if (namedParts.TryGetValue(
                    name,
                    out CharacterMeshGraphic part))
            {
                part.SetSize(size);
            }
        }

        private void SetPartWidthProfile(
            string name,
            float top,
            float bottom)
        {
            if (namedParts.TryGetValue(
                    name,
                    out CharacterMeshGraphic part))
            {
                part.SetWidthProfile(top, bottom);
            }
        }

        private bool ValidateJoint(
            RectTransform joint,
            string firstPartName,
            string secondPartName)
        {
            if (joint == null ||
                !namedParts.TryGetValue(
                    firstPartName,
                    out CharacterMeshGraphic first) ||
                !namedParts.TryGetValue(
                    secondPartName,
                    out CharacterMeshGraphic second) ||
                first == null ||
                second == null)
            {
                return false;
            }

            Vector3 point = joint.position;
            return ContainsWorldPoint(
                       first.rectTransform,
                       point,
                       8f) &&
                   ContainsWorldPoint(
                       second.rectTransform,
                       point,
                       8f);
        }

        private static bool ContainsWorldPoint(
            RectTransform rect,
            Vector3 point,
            float margin)
        {
            Vector3 local = rect.InverseTransformPoint(point);
            Rect localRect = rect.rect;
            localRect.xMin -= margin;
            localRect.xMax += margin;
            localRect.yMin -= margin;
            localRect.yMax += margin;
            return localRect.Contains(local);
        }

        private RectTransform CreateBone(
            Transform parent,
            string name,
            Vector2 position)
        {
            RectTransform bone = CreateRect(
                parent,
                name,
                position,
                Vector2.zero);
            bone.localRotation = Quaternion.identity;
            bone.localScale = Vector3.one;
            namedBones[name] = bone;
            return bone;
        }

        private CharacterMeshGraphic CreatePart(
            Transform parent,
            string name,
            CharacterMeshShape shape,
            CharacterVisualRole role,
            Vector2 size,
            Vector2 position,
            Vector2 pivot,
            float topWidth = 1f,
            float bottomWidth = 1f,
            float outlineWidth = 5f,
            bool corePart = true,
            bool showFront = true,
            bool showSide = true,
            bool showBack = true)
        {
            RectTransform rect = CreateRect(
                parent,
                name,
                position,
                size);
            CanvasRenderer renderer =
                GetOrAdd<CanvasRenderer>(rect.gameObject);
            renderer.cullTransparentMesh = false;
            CharacterMeshGraphic graphic =
                rect.gameObject.AddComponent<CharacterMeshGraphic>();
            graphic.Configure(
                shape,
                role,
                size,
                pivot,
                Color.clear,
                new Color(0.075f, 0.045f, 0.035f, 1f),
                outlineWidth,
                topWidth,
                bottomWidth);
            meshRenderers.Add(graphic);
            namedParts[name] = graphic;

            CharacterArtPart artPart =
                rect.gameObject.AddComponent<CharacterArtPart>();
            artPart.Configure(
                graphic,
                ResolveArtKind(shape, role),
                ResolveArtSlot(role),
                corePart,
                showFront,
                showSide,
                showBack);
            artParts.Add(artPart);
            return graphic;
        }

        public bool ValidateFatManArtCoverage(out string error)
        {
            string[] requiredParts =
            {
                "Part.Thigh.L",
                "Part.Thigh.R",
                "Part.Shin.L",
                "Part.Shin.R",
                "Part.Foot.L",
                "Part.Foot.R",
                "Part.Pelvis",
                "Part.Abdomen",
                "Part.ShirtHem",
                "Part.Chest",
                "Part.Shoulder.L",
                "Part.Shoulder.R",
                "Part.UpperArm.L",
                "Part.UpperArm.R",
                "Part.Forearm.L",
                "Part.Forearm.R",
                "Part.Hand.L",
                "Part.Hand.R",
                "Part.Neck",
                "Part.Head",
                "Part.HairBack",
                "Part.DoubleChin"
            };

            for (int i = 0; i < requiredParts.Length; i++)
            {
                string partName = requiredParts[i];
                if (!namedParts.TryGetValue(
                        partName,
                        out CharacterMeshGraphic graphic) ||
                    graphic == null)
                {
                    error =
                        $"Required fat-man art part is missing: {partName}.";
                    return false;
                }

                CharacterArtPart artPart =
                    graphic.GetComponent<CharacterArtPart>();
                if (artPart == null ||
                    !artPart.IsConfigured ||
                    !artPart.IsCorePart ||
                    !artPart.UsesFatManSilhouette)
                {
                    error =
                        $"'{partName}' still uses the technical mannequin " +
                        "instead of a fat-man cutout silhouette.";
                    return false;
                }
            }

            if (softBodyController == null ||
                !softBodyController.HasCompleteRig ||
                SoftBoneCount != 4)
            {
                error =
                    "Belly, ShirtHem, ChestSoft and ChinSoft bones are not " +
                    "fully connected.";
                return false;
            }

            if (currentDefinition != null &&
                (currentDefinition.SkinSet == null ||
                 !currentDefinition.SkinSet.IsValid))
            {
                error =
                    "The selected stage is not a valid FatManSkinSet.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static FatManArtPartKind ResolveArtKind(
            CharacterMeshShape shape,
            CharacterVisualRole role)
        {
            return shape switch
            {
                CharacterMeshShape.FatThigh =>
                    FatManArtPartKind.Thigh,
                CharacterMeshShape.FatCalf =>
                    FatManArtPartKind.Calf,
                CharacterMeshShape.WornShoe =>
                    FatManArtPartKind.Foot,
                CharacterMeshShape.FatPelvis =>
                    FatManArtPartKind.Pelvis,
                CharacterMeshShape.FatBelly =>
                    FatManArtPartKind.Belly,
                CharacterMeshShape.ShirtHem =>
                    FatManArtPartKind.ShirtHem,
                CharacterMeshShape.FatChest =>
                    FatManArtPartKind.Chest,
                CharacterMeshShape.FatShoulder =>
                    FatManArtPartKind.Shoulder,
                CharacterMeshShape.FatUpperArm =>
                    FatManArtPartKind.UpperArm,
                CharacterMeshShape.FatForearm =>
                    FatManArtPartKind.Forearm,
                CharacterMeshShape.FatHand =>
                    FatManArtPartKind.Hand,
                CharacterMeshShape.FatNeck =>
                    FatManArtPartKind.Neck,
                CharacterMeshShape.FatHead =>
                    FatManArtPartKind.Head,
                CharacterMeshShape.DoubleChin =>
                    FatManArtPartKind.DoubleChin,
                CharacterMeshShape.MessyHair =>
                    FatManArtPartKind.Hair,
                CharacterMeshShape.Ear =>
                    FatManArtPartKind.Ear,
                _ => role switch
                {
                    CharacterVisualRole.ShoeDetail =>
                        FatManArtPartKind.ShoeDetail,
                    CharacterVisualRole.SkinShadow =>
                        FatManArtPartKind.SkinDetail,
                    CharacterVisualRole.SkinHighlight =>
                        FatManArtPartKind.SkinDetail,
                    _ => FatManArtPartKind.ClothingDetail
                }
            };
        }

        private static CharacterSkinSlot ResolveArtSlot(
            CharacterVisualRole role)
        {
            return role switch
            {
                CharacterVisualRole.Hair =>
                    CharacterSkinSlot.Hair,
                CharacterVisualRole.Top =>
                    CharacterSkinSlot.Top,
                CharacterVisualRole.TopShadow =>
                    CharacterSkinSlot.Top,
                CharacterVisualRole.TopHighlight =>
                    CharacterSkinSlot.Top,
                CharacterVisualRole.TopStain =>
                    CharacterSkinSlot.Top,
                CharacterVisualRole.Bottom =>
                    CharacterSkinSlot.Bottom,
                CharacterVisualRole.BottomShadow =>
                    CharacterSkinSlot.Bottom,
                CharacterVisualRole.BottomDetail =>
                    CharacterSkinSlot.Bottom,
                CharacterVisualRole.Shoe =>
                    CharacterSkinSlot.Shoes,
                CharacterVisualRole.ShoeDetail =>
                    CharacterSkinSlot.Shoes,
                CharacterVisualRole.Accent =>
                    CharacterSkinSlot.Accessory,
                CharacterVisualRole.EyeWhite =>
                    CharacterSkinSlot.Face,
                CharacterVisualRole.Iris =>
                    CharacterSkinSlot.Face,
                CharacterVisualRole.Brow =>
                    CharacterSkinSlot.Face,
                CharacterVisualRole.Mouth =>
                    CharacterSkinSlot.Face,
                CharacterVisualRole.Cheek =>
                    CharacterSkinSlot.Face,
                _ => CharacterSkinSlot.Body
            };
        }

        public bool ValidateCanvasRendererCoverage(
            out string error)
        {
            if (characterRoot == null)
            {
                error =
                    "CharacterRoot is unavailable for renderer validation.";
                return false;
            }

            CharacterMeshGraphic[] graphics =
                characterRoot.GetComponentsInChildren<
                    CharacterMeshGraphic>(true);
            if (graphics.Length < 18)
            {
                error =
                    $"Expected at least 18 skeletal graphics, found " +
                    $"{graphics.Length}.";
                return false;
            }

            for (int i = 0; i < graphics.Length; i++)
            {
                CharacterMeshGraphic graphic = graphics[i];
                if (graphic == null ||
                    !graphic.HasRequiredCanvasRenderer)
                {
                    error =
                        $"Skeletal graphic " +
                        $"'{(graphic != null ? graphic.name : "<missing>")}' " +
                        "has no CanvasRenderer.";
                    return false;
                }
            }

            CharacterSurfaceGraphic[] surfaces =
                characterRoot.GetComponentsInChildren<
                    CharacterSurfaceGraphic>(true);
            if (surfaces.Length < graphics.Length * 2)
            {
                error =
                    $"Expected two stable artistic surfaces per skeletal " +
                    $"graphic, found {surfaces.Length} surfaces for " +
                    $"{graphics.Length} graphics.";
                return false;
            }

            for (int i = 0; i < surfaces.Length; i++)
            {
                CharacterSurfaceGraphic surface = surfaces[i];
                if (surface == null ||
                    surface.GetComponent<CanvasRenderer>() == null)
                {
                    error =
                        "An artistic character surface has no CanvasRenderer.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static RectTransform CreateRect(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            target.layer = parent.gameObject.layer;
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : target.AddComponent<T>();
        }
    }
}
