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

        private CharacterSkeletonDefinition skeletonDefinition;
        private CharacterSkinDefinition currentDefinition;
        private CharacterFaceController faceController;
        private CharacterAnimationDriver animationDriver;
        private CharacterViewController viewController;
        private CharacterIKController ikController;
        private Animator animator;

        private RectTransform characterRoot;
        private RectTransform visualRoot;
        private RectTransform skeletonRoot;
        private RectTransform rootBone;
        private RectTransform pelvisBone;
        private RectTransform spineBone;
        private RectTransform chestBone;
        private RectTransform neckBone;
        private RectTransform headBone;
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
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Bottom,
                new Vector2(104f, 224f),
                new Vector2(0f, 20f),
                new Vector2(0.5f, 1f));
            CreatePart(
                rightThighBone,
                "Part.Thigh.R",
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Bottom,
                new Vector2(104f, 224f),
                new Vector2(0f, 20f),
                new Vector2(0.5f, 1f));

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
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Skin,
                new Vector2(75f, 201f),
                new Vector2(0f, 18f),
                new Vector2(0.5f, 1f));
            CreatePart(
                rightShinBone,
                "Part.Shin.R",
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Skin,
                new Vector2(75f, 201f),
                new Vector2(0f, 18f),
                new Vector2(0.5f, 1f));

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
                CharacterMeshShape.Shoe,
                CharacterVisualRole.Shoe,
                new Vector2(132f, 67f),
                new Vector2(-18f, -31f),
                new Vector2(0.5f, 0.5f));
            leftShoe.rectTransform.localScale =
                new Vector3(-1f, 1f, 1f);
            CreatePart(
                rightFootBone,
                "Part.Foot.R",
                CharacterMeshShape.Shoe,
                CharacterVisualRole.Shoe,
                new Vector2(132f, 67f),
                new Vector2(18f, -31f),
                new Vector2(0.5f, 0.5f));
        }

        private void BuildTorsoAndArms()
        {
            CreatePart(
                pelvisBone,
                "Part.Pelvis",
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Bottom,
                new Vector2(245f, 151f),
                new Vector2(0f, -20f),
                new Vector2(0.5f, 0.5f));

            spineBone = CreateBone(
                pelvisBone,
                "Bone.Spine",
                skeletonDefinition.spine);
            CreatePart(
                spineBone,
                "Part.Abdomen",
                CharacterMeshShape.Torso,
                CharacterVisualRole.Top,
                new Vector2(244f, 194f),
                new Vector2(0f, 29f),
                new Vector2(0.5f, 0.5f),
                0.88f,
                1.04f);

            chestBone = CreateBone(
                spineBone,
                "Bone.Chest",
                skeletonDefinition.chest);
            CreatePart(
                chestBone,
                "Part.Chest",
                CharacterMeshShape.Torso,
                CharacterVisualRole.Top,
                new Vector2(316f, 244f),
                Vector2.zero,
                new Vector2(0.5f, 0.5f),
                1.05f,
                0.76f);
            CreatePart(
                chestBone,
                "Part.ChestAccent",
                CharacterMeshShape.Ellipse,
                CharacterVisualRole.Accent,
                new Vector2(48f, 48f),
                new Vector2(0f, 28f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                2f);

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
                CharacterMeshShape.Ellipse,
                CharacterVisualRole.Skin,
                new Vector2(92f, 92f),
                Vector2.zero,
                new Vector2(0.5f, 0.5f));
            CreatePart(
                rightShoulderBone,
                "Part.Shoulder.R",
                CharacterMeshShape.Ellipse,
                CharacterVisualRole.Skin,
                new Vector2(92f, 92f),
                Vector2.zero,
                new Vector2(0.5f, 0.5f));

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
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Skin,
                new Vector2(80f, 181f),
                new Vector2(0f, 20f),
                new Vector2(0.5f, 1f));
            CreatePart(
                rightUpperArmBone,
                "Part.UpperArm.R",
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Skin,
                new Vector2(80f, 181f),
                new Vector2(0f, 20f),
                new Vector2(0.5f, 1f));

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
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Skin,
                new Vector2(69f, 165f),
                new Vector2(0f, 18f),
                new Vector2(0.5f, 1f));
            CreatePart(
                rightForearmBone,
                "Part.Forearm.R",
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Skin,
                new Vector2(69f, 165f),
                new Vector2(0f, 18f),
                new Vector2(0.5f, 1f));
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
                2f);
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
                2f);

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
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Skin,
                new Vector2(76f, 92f),
                new Vector2(0f, -34f),
                new Vector2(0.5f, 0.5f));
            CreatePart(
                rightHandBone,
                "Part.Hand.R",
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Skin,
                new Vector2(76f, 92f),
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
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Skin,
                new Vector2(76f, 104f),
                new Vector2(0f, 31f),
                new Vector2(0.5f, 0.5f));

            headBone = CreateBone(
                neckBone,
                "Bone.Head",
                skeletonDefinition.head);
            CreatePart(
                headBone,
                "Part.Head",
                CharacterMeshShape.Ellipse,
                CharacterVisualRole.Skin,
                new Vector2(214f, 242f),
                new Vector2(0f, 87f),
                new Vector2(0.5f, 0.5f));
            CreatePart(
                headBone,
                "Part.HairBack",
                CharacterMeshShape.Hair,
                CharacterVisualRole.Hair,
                new Vector2(222f, 132f),
                new Vector2(0f, 162f),
                new Vector2(0.5f, 0.5f),
                1f,
                1f,
                4f);

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
        }

        public void StopLocomotion(CharacterFacing facing)
        {
            moving = false;
            moveDirection = Vector2.zero;
            restingFacing = facing;
            viewController?.SetFacing(facing);
            animationDriver?.SetLocomotion(facing, 0f, false);
            ikController?.SetLocomotion(false, facing);
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
            faceController?.LookAt(new Vector2(0f, -1f), 0.42f);
            faceController?.SetExpression(
                CharacterExpression.Strain,
                0.46f);
        }

        public void TriggerUpgrade()
        {
            animationDriver?.TriggerUpgrade();
            faceController?.SetExpression(
                CharacterExpression.Happy,
                0.9f);
        }

        public void TriggerStageChange()
        {
            animationDriver?.TriggerStageChange();
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
            leftShoulderBone.anchoredPosition =
                new Vector2(
                    -shoulderOffset,
                    skeletonDefinition.leftShoulder.y);
            rightShoulderBone.anchoredPosition =
                new Vector2(
                    shoulderOffset,
                    skeletonDefinition.rightShoulder.y);

            SetPartSize(
                "Part.Chest",
                new Vector2(
                    316f * appearance.chestWidth,
                    244f));
            SetPartWidthProfile(
                "Part.Chest",
                1.05f,
                Mathf.Lerp(0.70f, 0.86f, appearance.bellyWidth - 0.9f));
            SetPartSize(
                "Part.Abdomen",
                new Vector2(
                    244f * appearance.bellyWidth,
                    194f));
            SetPartSize(
                "Part.Pelvis",
                new Vector2(
                    245f * appearance.hipWidth,
                    151f));

            SetPairSize(
                "Part.Shoulder",
                new Vector2(
                    92f * appearance.armWidth,
                    92f * appearance.armWidth));
            SetPairSize(
                "Part.UpperArm",
                new Vector2(
                    80f * appearance.armWidth,
                    181f));
            SetPairSize(
                "Part.Forearm",
                new Vector2(
                    69f * appearance.armWidth,
                    165f));
            SetPairSize(
                "Part.Hand",
                new Vector2(
                    76f * appearance.armWidth,
                    92f * appearance.armWidth));
            SetPairSize(
                "Part.Thigh",
                new Vector2(
                    104f * appearance.legWidth,
                    224f));
            SetPairSize(
                "Part.Shin",
                new Vector2(
                    75f * appearance.legWidth,
                    201f));
            SetPairSize(
                "Part.Foot",
                new Vector2(
                    132f * appearance.legWidth,
                    67f * appearance.legWidth));
            SetPartSize(
                "Part.Head",
                new Vector2(
                    214f * appearance.headScale,
                    242f * appearance.headScale));
            SetPartSize(
                "Part.HairBack",
                new Vector2(
                    222f * appearance.headScale,
                    132f * appearance.headScale));
        }

        private static Color ResolveRoleColor(
            CharacterVisualRole role,
            CharacterAppearance appearance)
        {
            return role switch
            {
                CharacterVisualRole.Skin => appearance.skin,
                CharacterVisualRole.Hair => appearance.hair,
                CharacterVisualRole.Top => appearance.top,
                CharacterVisualRole.Bottom => appearance.bottom,
                CharacterVisualRole.Shoe => appearance.shoes,
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
            float outlineWidth = 5f)
        {
            RectTransform rect = CreateRect(
                parent,
                name,
                position,
                size);
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
            return graphic;
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
