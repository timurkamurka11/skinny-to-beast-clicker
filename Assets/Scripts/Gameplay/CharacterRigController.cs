using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        private const string AnimatorResourcePath =
            "UI/Gameplay/Living/Animations/LivingCharacter";

        private sealed class RigPart
        {
            public RigPartGraphic Graphic;
            public RectTransform Bone;
        }

        private struct PoseFrame
        {
            public float pelvis;
            public float spine;
            public float chest;
            public float neck;
            public float head;
            public float leftShoulder;
            public float rightShoulder;
            public float leftUpperArm;
            public float leftForearm;
            public float leftHand;
            public float rightUpperArm;
            public float rightForearm;
            public float rightHand;
            public float leftThigh;
            public float leftShin;
            public float leftFoot;
            public float rightThigh;
            public float rightShin;
            public float rightFoot;
            public float pelvisY;
            public float pelvisX;
            public Vector2 chestScale;
            public Vector2 abdomenScale;
        }

        private readonly List<RigPartGraphic> frontRenderers = new();
        private readonly Dictionary<string, RectTransform> namedBones = new();

        private RectTransform characterRoot;
        private RectTransform skeletonRoot;
        private CanvasGroup skeletonGroup;
        private RawImage directionalImage;
        private CharacterFaceController faceController;
        private Animator stateAnimator;

        private RectTransform rootBone;
        private RectTransform pelvisBone;
        private RectTransform spineBone;
        private RectTransform chestBone;
        private RectTransform neckBone;
        private RectTransform headBone;
        private RectTransform leftShoulderBone;
        private RectTransform rightShoulderBone;
        private RectTransform leftUpperArmBone;
        private RectTransform leftForearmBone;
        private RectTransform leftHandBone;
        private RectTransform rightUpperArmBone;
        private RectTransform rightForearmBone;
        private RectTransform rightHandBone;
        private RectTransform leftThighBone;
        private RectTransform leftShinBone;
        private RectTransform leftFootBone;
        private RectTransform rightThighBone;
        private RectTransform rightShinBone;
        private RectTransform rightFootBone;

        private RigPart torsoPart;
        private RigPart abdomenPart;
        private RigPart pelvisPart;
        private RigPart headPart;
        private RigPart leftUpperArmPart;
        private RigPart leftForearmPart;
        private RigPart leftHandPart;
        private RigPart rightUpperArmPart;
        private RigPart rightForearmPart;
        private RigPart rightHandPart;
        private RigPart leftThighPart;
        private RigPart leftShinPart;
        private RigPart leftFootPart;
        private RigPart rightThighPart;
        private RigPart rightShinPart;
        private RigPart rightFootPart;

        private CharacterRigProfile profile;
        private CharacterSkinDefinition currentDefinition;
        private Texture currentFrontTexture;
        private Rect sourceUv = new Rect(0f, 0f, 1f, 1f);
        private Vector2 fullSize = new Vector2(720f, 1280f);
        private Vector2 basePelvisPosition;
        private PoseFrame currentPose;
        private CharacterFacing restingFacing;
        private CharacterRoutineAction activeAction;
        private Vector2 moveDirection;
        private float actionAge = 10f;
        private float actionDuration = 1f;
        private float walkBlend;
        private float walkCycle;
        private float tapAge = 10f;
        private float upgradeAge = 10f;
        private float stageChangeAge = 10f;
        private int tapVariant = -1;
        private Coroutine tapFaceRoutine;
        private string currentBaseAnimatorState;
        private bool animatorBound;
        private bool moving;
        private bool built;

        public int BoneCount => namedBones.Count;
        public bool IsMoving => moving;
        public CharacterFacing Facing => ResolveFacing();
        public bool HasAppliedSkin =>
            built &&
            currentDefinition != null &&
            currentFrontTexture != null;
        public bool HasVisibleSkin
        {
            get
            {
                if (!HasAppliedSkin)
                {
                    return false;
                }

                if (directionalImage != null && directionalImage.enabled)
                {
                    return directionalImage.texture != null &&
                           directionalImage.color.a > 0.001f;
                }

                if (skeletonGroup == null || skeletonGroup.alpha <= 0.001f)
                {
                    return false;
                }

                foreach (RigPartGraphic renderer in frontRenderers)
                {
                    if (renderer != null &&
                        renderer.isActiveAndEnabled &&
                        renderer.SourceTexture == currentFrontTexture &&
                        renderer.color.a > 0.001f)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        public CharacterRoutineAction ActiveAction => activeAction;
        public float ActiveActionRemaining =>
            activeAction == CharacterRoutineAction.None
                ? 0f
                : Mathf.Max(0f, actionDuration - actionAge);
        public int ActiveTapVariant => Mathf.Max(0, tapVariant);
        public bool IsTapReacting => tapAge < 0.52f;

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
            stateAnimator = root.GetComponent<Animator>();
            if (stateAnimator == null)
            {
                stateAnimator = root.gameObject.AddComponent<Animator>();
            }
            TryBindStateAnimator();

            skeletonRoot = LivingGameplayVisualFactory.CreateRect(
                root,
                "Skeleton.Root",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                fullSize);
            skeletonGroup = skeletonRoot.gameObject.AddComponent<CanvasGroup>();

            rootBone = CreateBone(skeletonRoot, "Bone.Root");
            pelvisBone = CreateBone(rootBone, "Bone.Pelvis");
            leftThighBone = CreateBone(pelvisBone, "Bone.Thigh.L");
            leftThighPart = CreatePart(leftThighBone, "Part.Thigh.L");
            leftShinBone = CreateBone(leftThighBone, "Bone.Shin.L");
            leftShinPart = CreatePart(leftShinBone, "Part.Shin.L");
            leftFootBone = CreateBone(leftShinBone, "Bone.Foot.L");
            leftFootPart = CreatePart(leftFootBone, "Part.Foot.L");

            rightThighBone = CreateBone(pelvisBone, "Bone.Thigh.R");
            rightThighPart = CreatePart(rightThighBone, "Part.Thigh.R");
            rightShinBone = CreateBone(rightThighBone, "Bone.Shin.R");
            rightShinPart = CreatePart(rightShinBone, "Part.Shin.R");
            rightFootBone = CreateBone(rightShinBone, "Bone.Foot.R");
            rightFootPart = CreatePart(rightFootBone, "Part.Foot.R");

            spineBone = CreateBone(pelvisBone, "Bone.Spine");
            abdomenPart = CreatePart(spineBone, "Part.Abdomen");
            chestBone = CreateBone(spineBone, "Bone.Chest");
            torsoPart = CreatePart(chestBone, "Part.Torso");

            leftShoulderBone = CreateBone(chestBone, "Bone.Shoulder.L");
            leftUpperArmBone = CreateBone(
                leftShoulderBone,
                "Bone.UpperArm.L");
            leftUpperArmPart = CreatePart(leftUpperArmBone, "Part.UpperArm.L");
            leftForearmBone = CreateBone(leftUpperArmBone, "Bone.Forearm.L");
            leftForearmPart = CreatePart(leftForearmBone, "Part.Forearm.L");
            leftHandBone = CreateBone(leftForearmBone, "Bone.Hand.L");
            leftHandPart = CreatePart(leftHandBone, "Part.Hand.L");

            rightShoulderBone = CreateBone(chestBone, "Bone.Shoulder.R");
            rightUpperArmBone = CreateBone(
                rightShoulderBone,
                "Bone.UpperArm.R");
            rightUpperArmPart = CreatePart(rightUpperArmBone, "Part.UpperArm.R");
            rightForearmBone = CreateBone(rightUpperArmBone, "Bone.Forearm.R");
            rightForearmPart = CreatePart(rightForearmBone, "Part.Forearm.R");
            rightHandBone = CreateBone(rightForearmBone, "Bone.Hand.R");
            rightHandPart = CreatePart(rightHandBone, "Part.Hand.R");

            neckBone = CreateBone(chestBone, "Bone.Neck");
            headBone = CreateBone(neckBone, "Bone.Head");
            headPart = CreatePart(headBone, "Part.Head");
            faceController?.Build(headBone);

            pelvisPart = CreatePart(pelvisBone, "Part.Pelvis");

            RectTransform directionalRect = LivingGameplayVisualFactory.CreateRect(
                root,
                "DirectionalWalkRenderer",
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -18f),
                new Vector2(1280f, 1280f));
            directionalImage = directionalRect.gameObject.AddComponent<RawImage>();
            directionalImage.color = Color.white;
            directionalImage.raycastTarget = false;
            directionalImage.enabled = false;

            currentPose.chestScale = Vector2.one;
            currentPose.abdomenScale = Vector2.one;
            built = true;
        }

        public void ApplySkin(CharacterSkinDefinition definition)
        {
            if (!built || definition == null || !definition.IsValid)
            {
                return;
            }

            Sprite sprite = definition.FrontSprite;
            TryBindStateAnimator();
            Texture texture = sprite.texture;
            currentFrontTexture = texture;
            sourceUv = new Rect(
                sprite.rect.x / texture.width,
                sprite.rect.y / texture.height,
                sprite.rect.width / texture.width,
                sprite.rect.height / texture.height);

            foreach (RigPartGraphic renderer in frontRenderers)
            {
                renderer.enabled = true;
                renderer.color = Color.white;
            }

            directionalImage.texture = definition.DirectionalWalkSheet;
            currentDefinition = definition;
            profile = definition.RigProfile;
            fullSize = new Vector2(profile.visualWidth, profile.visualHeight);
            skeletonRoot.sizeDelta = fullSize;
            ApplyProfile(profile);
            faceController?.ApplyStyle(definition.FaceStyle, profile);
            SetDirectionalVisible(false);
            EnsureSkinVisible();
        }

        public void ClearSkin()
        {
            if (tapFaceRoutine != null)
            {
                StopCoroutine(tapFaceRoutine);
                tapFaceRoutine = null;
            }

            foreach (RigPartGraphic renderer in frontRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            currentFrontTexture = null;
            currentDefinition = null;
            profile = null;
            if (directionalImage != null)
            {
                directionalImage.enabled = false;
                directionalImage.texture = null;
            }

            if (skeletonGroup != null)
            {
                skeletonGroup.alpha = 0f;
            }

            faceController?.SetVisible(false);
        }

        public bool EnsureSkinVisible()
        {
            if (!HasAppliedSkin)
            {
                return false;
            }

            bool hasFrontPart = false;
            foreach (RigPartGraphic renderer in frontRenderers)
            {
                if (renderer == null ||
                    renderer.SourceTexture != currentFrontTexture)
                {
                    continue;
                }

                renderer.enabled = true;
                Color color = renderer.color;
                color.a = 1f;
                renderer.color = color;
                renderer.SetVerticesDirty();
                renderer.SetMaterialDirty();
                hasFrontPart = true;
            }

            bool directional = ShouldUseDirectionalRenderer();
            SetDirectionalVisible(directional);
            if (directional)
            {
                UpdateDirectionalRenderer();
                return directionalImage != null &&
                       directionalImage.enabled &&
                       directionalImage.texture != null;
            }

            return hasFrontPart &&
                   skeletonGroup != null &&
                   skeletonGroup.alpha > 0.001f;
        }

        public void SynchronizeAnimationState()
        {
            activeAction = CharacterRoutineAction.None;
            actionAge = actionDuration;
            tapAge = 10f;
            upgradeAge = 10f;
            stageChangeAge = 10f;
            currentPose = default;
            currentPose.chestScale = Vector2.one;
            currentPose.abdomenScale = Vector2.one;
            if (rootBone != null)
            {
                rootBone.localRotation = Quaternion.identity;
                rootBone.localScale = Vector3.one;
            }

            if (skeletonRoot != null)
            {
                skeletonRoot.localScale = Vector3.one;
            }

            faceController?.ResetExpression();
            CrossFadeState("Idle_Breathe", 0, 0f);
        }

        public void SetLocomotion(Vector2 direction, float speed)
        {
            bool wasMoving = moving;
            moveDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector2.zero;
            moving = speed > 0.01f && moveDirection.sqrMagnitude > 0.001f;
            if (moving && !wasMoving)
            {
                walkCycle = 0f;
            }

            UpdateBaseAnimatorState();
            if (!moving)
            {
                walkBlend = Mathf.MoveTowards(
                    walkBlend,
                    0f,
                    Time.unscaledDeltaTime * 4f);
            }
        }

        public void StopLocomotion(CharacterFacing facing)
        {
            moving = false;
            moveDirection = Vector2.zero;
            restingFacing = facing;
            UpdateBaseAnimatorState();
        }

        public void SetRestingFacing(CharacterFacing facing)
        {
            restingFacing = facing;
            UpdateBaseAnimatorState();
        }

        public void PlayAction(CharacterRoutineAction action, float duration)
        {
            activeAction = action;
            actionAge = 0f;
            actionDuration = Mathf.Max(0.2f, duration);

            CharacterExpression expression = action switch
            {
                CharacterRoutineAction.Yawn => CharacterExpression.Yawn,
                CharacterRoutineAction.Flex => CharacterExpression.Happy,
                CharacterRoutineAction.Stretch => CharacterExpression.Focused,
                CharacterRoutineAction.WarmShoulders => CharacterExpression.Focused,
                CharacterRoutineAction.AdjustClothes => CharacterExpression.Neutral,
                _ => CharacterExpression.Neutral
            };
            faceController?.SetExpression(expression, actionDuration);
            PlayAnimatorAction(action);
        }

        public void CancelAction()
        {
            activeAction = CharacterRoutineAction.None;
            actionAge = actionDuration;
            faceController?.ResetExpression();
            CrossFadeState("UpperBody_Idle", 1, 0.1f);
            CrossFadeState("FullBody_Idle", 3, 0.1f);
        }

        public void TriggerTap()
        {
            tapVariant = (tapVariant + 1) % 3;
            tapAge = 0f;
            activeAction = CharacterRoutineAction.None;
            faceController?.LookAt(new Vector2(0f, -1f), 0.75f);
            faceController?.SetExpression(CharacterExpression.Strain, 0.52f);
            CrossFadeState(
                $"TapLift_{(char)('A' + tapVariant)}",
                3,
                0.08f);
            if (tapFaceRoutine != null)
            {
                StopCoroutine(tapFaceRoutine);
            }

            tapFaceRoutine = StartCoroutine(TapFaceSequence());
        }

        private IEnumerator TapFaceSequence()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            faceController?.SetExpression(CharacterExpression.Happy, 0.34f);
            yield return new WaitForSecondsRealtime(0.34f);
            tapFaceRoutine = null;
        }

        public void TriggerUpgrade()
        {
            upgradeAge = 0f;
            PlayAction(CharacterRoutineAction.Flex, 0.95f);
            faceController?.SetExpression(CharacterExpression.Happy, 1.15f);
            CrossFadeState("Idle_Flex", 1, 0.1f);
        }

        public void TriggerStageChange()
        {
            stageChangeAge = 0f;
            faceController?.SetExpression(CharacterExpression.Happy, 1.15f);
            CrossFadeState("StageChange", 3, 0.08f);
        }

        public bool HasBone(string boneName)
        {
            return !string.IsNullOrEmpty(boneName) && namedBones.ContainsKey(boneName);
        }

        public int GetDistinctFrontTextureCount()
        {
            HashSet<Texture> textures = new();
            foreach (RigPartGraphic renderer in frontRenderers)
            {
                if (renderer != null &&
                    renderer.enabled &&
                    renderer.SourceTexture != null)
                {
                    textures.Add(renderer.SourceTexture);
                }
            }

            return textures.Count;
        }

        private void Update()
        {
            if (!built || profile == null)
            {
                return;
            }

            float delta = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;
            actionAge += delta;
            tapAge += delta;
            upgradeAge += delta;
            stageChangeAge += delta;

            walkBlend = Mathf.MoveTowards(
                walkBlend,
                moving ? 1f : 0f,
                delta * (moving ? 6f : 4f));
            if (moving)
            {
                // One complete two-contact gait lasts about 0.77 seconds.
                // Keeping the procedural legs and two-frame directional sheets
                // on the same clock prevents foot cadence from doubling.
                walkCycle += delta * 2.6f;
            }

            if (animatorBound)
            {
                stateAnimator.SetFloat("Speed", moving ? 1f : 0f);
                stateAnimator.SetInteger("Facing", (int)ResolveFacing());
            }

            bool directional = ShouldUseDirectionalRenderer();
            SetDirectionalVisible(directional);
            if (directional)
            {
                UpdateDirectionalRenderer();
            }

            PoseFrame target = CalculatePose(now);
            float poseBlend = 1f - Mathf.Exp(-delta * 12f);
            SmoothPose(ref currentPose, target, poseBlend);
            ApplyPose(currentPose);
        }

        private PoseFrame CalculatePose(float now)
        {
            float breath = Mathf.Sin(now * Mathf.PI * 2f / 2.6f);
            float slowSway = Mathf.Sin(now * 0.72f);
            PoseFrame pose = new PoseFrame
            {
                pelvis = slowSway * 0.55f,
                spine = -slowSway * 0.7f,
                chest = slowSway * 0.45f,
                neck = -slowSway * 0.18f,
                head = slowSway * 0.72f,
                leftShoulder = breath * 0.45f,
                rightShoulder = -breath * 0.45f,
                leftUpperArm = slowSway * 1.6f,
                rightUpperArm = -slowSway * 1.6f,
                leftForearm = 0f,
                rightForearm = 0f,
                leftHand = slowSway * 0.5f,
                rightHand = -slowSway * 0.5f,
                leftThigh = slowSway * 0.35f,
                rightThigh = -slowSway * 0.35f,
                leftShin = 0f,
                rightShin = 0f,
                leftFoot = 0f,
                rightFoot = 0f,
                pelvisY = breath * 2.5f,
                pelvisX = slowSway * 1.6f,
                chestScale = new Vector2(
                    1f - breath * 0.004f,
                    1f + breath * 0.012f),
                abdomenScale = new Vector2(
                    1f + breath * 0.009f,
                    1f + breath * 0.005f)
            };

            if (walkBlend > 0.001f && !ShouldUseDirectionalRenderer())
            {
                float phase = walkCycle * Mathf.PI;
                float stride = Mathf.Sin(phase) * 17f * walkBlend;
                float liftLeft = Mathf.Max(0f, Mathf.Sin(phase)) * 19f * walkBlend;
                float liftRight = Mathf.Max(0f, -Mathf.Sin(phase)) * 19f * walkBlend;
                pose.leftThigh += stride;
                pose.rightThigh -= stride;
                pose.leftShin -= liftLeft;
                pose.rightShin += liftRight;
                pose.leftFoot += liftLeft * 0.35f;
                pose.rightFoot -= liftRight * 0.35f;
                pose.leftUpperArm -= stride * 0.72f;
                pose.rightUpperArm += stride * 0.72f;
                pose.pelvisY += Mathf.Abs(Mathf.Sin(phase)) * 8f * walkBlend;
                pose.pelvis = Mathf.Sin(phase * 2f) * 1.1f * walkBlend;
                pose.chest = -pose.pelvis * 0.55f;
            }

            if (activeAction != CharacterRoutineAction.None &&
                actionAge < actionDuration)
            {
                float t = Mathf.Clamp01(actionAge / actionDuration);
                float envelope = activeAction switch
                {
                    CharacterRoutineAction.Sit => CalculateHeldEnvelope(t),
                    CharacterRoutineAction.SitDown =>
                        Mathf.SmoothStep(0f, 1f, t),
                    CharacterRoutineAction.SitLoop => 1f,
                    CharacterRoutineAction.StandUp =>
                        Mathf.SmoothStep(1f, 0f, t),
                    _ => Mathf.Sin(t * Mathf.PI)
                };
                float repeat = Mathf.Sin(t * Mathf.PI * 4f);
                ApplyActionPose(ref pose, activeAction, envelope, repeat);
            }
            else if (actionAge >= actionDuration)
            {
                activeAction = CharacterRoutineAction.None;
            }

            if (tapAge < 0.52f)
            {
                float t = tapAge / 0.52f;
                float punch = Mathf.Sin(t * Mathf.PI);
                pose.pelvisY += punch * 11f;
                switch (tapVariant)
                {
                    case 0:
                        pose.spine -= punch * 4f;
                        pose.chest += punch * 6f;
                        pose.leftShoulder -= punch * 8f;
                        pose.leftUpperArm -= punch * 38f;
                        pose.leftForearm -= punch * 92f;
                        pose.leftHand += punch * 20f;
                        break;
                    case 1:
                        pose.spine += punch * 4f;
                        pose.chest -= punch * 6f;
                        pose.rightShoulder += punch * 8f;
                        pose.rightUpperArm += punch * 38f;
                        pose.rightForearm += punch * 92f;
                        pose.rightHand -= punch * 20f;
                        break;
                    default:
                        pose.pelvisY -= punch * 8f;
                        pose.spine += Mathf.Sin(t * Mathf.PI * 2f) * 3f;
                        pose.leftShoulder -= punch * 11f;
                        pose.rightShoulder += punch * 11f;
                        pose.leftUpperArm -= punch * 52f;
                        pose.rightUpperArm += punch * 52f;
                        pose.leftForearm -= punch * 106f;
                        pose.rightForearm += punch * 106f;
                        pose.leftHand += punch * 18f;
                        pose.rightHand -= punch * 18f;
                        break;
                }

                pose.chestScale += new Vector2(punch * 0.025f, -punch * 0.018f);
            }

            if (upgradeAge < 0.95f)
            {
                float pulse = Mathf.Sin(Mathf.Clamp01(upgradeAge / 0.95f) * Mathf.PI);
                pose.leftUpperArm -= pulse * 48f;
                pose.rightUpperArm += pulse * 48f;
                pose.leftForearm -= pulse * 78f;
                pose.rightForearm += pulse * 78f;
                pose.chestScale += Vector2.one * pulse * 0.035f;
            }

            if (!moving &&
                activeAction != CharacterRoutineAction.SitDown &&
                activeAction != CharacterRoutineAction.SitLoop &&
                activeAction != CharacterRoutineAction.StandUp &&
                activeAction != CharacterRoutineAction.Sit)
            {
                // Keep the global foot angle planted while the pelvis and legs
                // perform breathing and weight-shift poses.
                pose.leftFoot =
                    -(pose.pelvis + pose.leftThigh + pose.leftShin);
                pose.rightFoot =
                    -(pose.pelvis + pose.rightThigh + pose.rightShin);
            }

            return pose;
        }

        private static void ApplyActionPose(
            ref PoseFrame pose,
            CharacterRoutineAction action,
            float envelope,
            float repeat)
        {
            switch (action)
            {
                case CharacterRoutineAction.ShiftWeight:
                    pose.pelvis += repeat * 2.5f * envelope;
                    pose.pelvisX += repeat * 8f * envelope;
                    pose.head -= repeat * 1.5f * envelope;
                    break;
                case CharacterRoutineAction.LookAround:
                    pose.head += repeat * 11f * envelope;
                    pose.neck += repeat * 3f * envelope;
                    break;
                case CharacterRoutineAction.Scratch:
                    pose.rightUpperArm += 84f * envelope;
                    pose.rightForearm += 112f * envelope + repeat * 8f;
                    pose.rightHand -= 25f * envelope;
                    pose.head -= 5f * envelope;
                    break;
                case CharacterRoutineAction.Yawn:
                    pose.leftUpperArm -= 118f * envelope;
                    pose.rightUpperArm += 118f * envelope;
                    pose.leftForearm -= 18f * envelope;
                    pose.rightForearm += 18f * envelope;
                    pose.chestScale += new Vector2(0.02f, 0.035f) * envelope;
                    break;
                case CharacterRoutineAction.Stretch:
                    pose.leftUpperArm -= 142f * envelope;
                    pose.rightUpperArm += 142f * envelope;
                    pose.leftForearm -= 12f * envelope;
                    pose.rightForearm += 12f * envelope;
                    pose.spine += repeat * 2f * envelope;
                    pose.pelvisY += 12f * envelope;
                    break;
                case CharacterRoutineAction.Flex:
                    pose.leftUpperArm -= 54f * envelope;
                    pose.rightUpperArm += 54f * envelope;
                    pose.leftForearm -= 108f * envelope;
                    pose.rightForearm += 108f * envelope;
                    pose.leftHand += 17f * envelope;
                    pose.rightHand -= 17f * envelope;
                    pose.chestScale += Vector2.one * 0.025f * envelope;
                    break;
                case CharacterRoutineAction.AdjustClothes:
                    pose.leftUpperArm += 38f * envelope;
                    pose.rightUpperArm -= 38f * envelope;
                    pose.leftForearm -= 84f * envelope + repeat * 5f;
                    pose.rightForearm += 84f * envelope - repeat * 5f;
                    pose.leftHand += 13f * envelope;
                    pose.rightHand -= 13f * envelope;
                    pose.head += repeat * 1.2f * envelope;
                    break;
                case CharacterRoutineAction.WarmShoulders:
                    pose.leftShoulder += repeat * 13f * envelope;
                    pose.rightShoulder -= repeat * 13f * envelope;
                    pose.leftUpperArm -= repeat * 8f * envelope;
                    pose.rightUpperArm += repeat * 8f * envelope;
                    pose.chest -= repeat * 2.2f * envelope;
                    break;
                case CharacterRoutineAction.SitDown:
                    ApplySitPose(ref pose, envelope);
                    break;
                case CharacterRoutineAction.SitLoop:
                    ApplySitPose(ref pose, 1f);
                    pose.spine += repeat * 1.4f;
                    pose.head -= repeat * 1.1f;
                    break;
                case CharacterRoutineAction.StandUp:
                    ApplySitPose(ref pose, envelope);
                    break;
                case CharacterRoutineAction.Sit:
                    ApplySitPose(ref pose, envelope);
                    break;
            }
        }

        private static void ApplySitPose(ref PoseFrame pose, float amount)
        {
            float held = Mathf.Clamp01(amount);
            pose.pelvisY -= 105f * held;
            pose.spine -= 5f * held;
            pose.leftThigh -= 58f * held;
            pose.rightThigh += 58f * held;
            pose.leftShin += 78f * held;
            pose.rightShin -= 78f * held;
            pose.leftUpperArm += 10f * held;
            pose.rightUpperArm -= 10f * held;
        }

        private void ApplyPose(PoseFrame pose)
        {
            pelvisBone.anchoredPosition =
                basePelvisPosition + new Vector2(pose.pelvisX, pose.pelvisY);
            SetRotation(pelvisBone, pose.pelvis);
            SetRotation(spineBone, pose.spine);
            SetRotation(chestBone, pose.chest);
            SetRotation(neckBone, pose.neck);
            SetRotation(headBone, pose.head);
            SetRotation(leftShoulderBone, pose.leftShoulder);
            SetRotation(rightShoulderBone, pose.rightShoulder);
            SetRotation(leftUpperArmBone, pose.leftUpperArm);
            SetRotation(leftForearmBone, pose.leftForearm);
            SetRotation(leftHandBone, pose.leftHand);
            SetRotation(rightUpperArmBone, pose.rightUpperArm);
            SetRotation(rightForearmBone, pose.rightForearm);
            SetRotation(rightHandBone, pose.rightHand);
            SetRotation(leftThighBone, pose.leftThigh);
            SetRotation(leftShinBone, pose.leftShin);
            SetRotation(leftFootBone, pose.leftFoot);
            SetRotation(rightThighBone, pose.rightThigh);
            SetRotation(rightShinBone, pose.rightShin);
            SetRotation(rightFootBone, pose.rightFoot);
            chestBone.localScale = new Vector3(
                Mathf.Max(0.85f, pose.chestScale.x),
                Mathf.Max(0.85f, pose.chestScale.y),
                1f);
            abdomenPart.Graphic.rectTransform.localScale = new Vector3(
                Mathf.Max(0.85f, pose.abdomenScale.x),
                Mathf.Max(0.85f, pose.abdomenScale.y),
                1f);

            float stagePulse = 0f;
            if (stageChangeAge < 0.78f)
            {
                float t = stageChangeAge / 0.78f;
                stagePulse = Mathf.Sin(t * Mathf.PI) * 0.08f;
            }

            skeletonRoot.localScale = Vector3.one * (1f + stagePulse);
        }

        private bool ShouldUseDirectionalRenderer()
        {
            if (directionalImage == null || directionalImage.texture == null)
            {
                return false;
            }

            if (!moving)
            {
                return restingFacing != CharacterFacing.Front;
            }

            bool side = Mathf.Abs(moveDirection.x) >
                        Mathf.Abs(moveDirection.y) * 0.65f;
            bool back = !side && moveDirection.y > 0f;
            return side || back;
        }

        private CharacterFacing ResolveFacing()
        {
            if (!moving)
            {
                return restingFacing;
            }

            if (Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.y) * 0.65f)
            {
                return moveDirection.x < 0f
                    ? CharacterFacing.SideLeft
                    : CharacterFacing.SideRight;
            }

            return moveDirection.y > 0f
                ? CharacterFacing.Back
                : CharacterFacing.Front;
        }

        private void UpdateDirectionalRenderer()
        {
            CharacterFacing facing = ResolveFacing();
            int frame = moving ? Mathf.FloorToInt(walkCycle) & 1 : 0;
            bool back = facing == CharacterFacing.Back;
            CharacterDirectionalFrame calibration = currentDefinition != null
                ? currentDefinition.GetDirectionalFrame(back, frame)
                : CharacterDirectionalFrame.Default;
            directionalImage.uvRect = new Rect(
                frame == 0 ? 0f : 0.5f,
                back ? 0f : 0.5f,
                0.5f,
                0.5f);

            Vector3 scale = Vector3.one * calibration.Scale;
            Vector2 offset = calibration.Offset;
            if (facing == CharacterFacing.SideRight)
            {
                scale.x *= -1f;
                offset.x *= -1f;
            }

            float bob = moving ? Mathf.Abs(Mathf.Sin(walkCycle * Mathf.PI)) * 7f : 0f;
            offset.y += bob;
            directionalImage.rectTransform.anchoredPosition = offset;
            directionalImage.rectTransform.localScale = scale;
        }

        private void SetDirectionalVisible(bool directional)
        {
            if (directionalImage == null)
            {
                return;
            }

            directionalImage.enabled = directional;
            if (skeletonGroup != null)
            {
                skeletonGroup.alpha = directional ? 0f : 1f;
            }

            faceController?.SetVisible(!directional);
        }

        private void ApplyProfile(CharacterRigProfile next)
        {
            SetBonePosition(rootBone, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            SetBonePosition(pelvisBone, next.pelvis, new Vector2(0.5f, 0.5f));
            SetBonePosition(spineBone, next.spine, next.pelvis);
            SetBonePosition(chestBone, next.chest, next.spine);
            SetBonePosition(neckBone, next.neck, next.chest);
            SetBonePosition(headBone, next.head, next.neck);

            SetBonePosition(leftShoulderBone, next.leftShoulder, next.chest);
            SetBonePosition(leftUpperArmBone, next.leftShoulder, next.leftShoulder);
            SetBonePosition(leftForearmBone, next.leftElbow, next.leftShoulder);
            SetBonePosition(leftHandBone, next.leftWrist, next.leftElbow);
            SetBonePosition(rightShoulderBone, next.rightShoulder, next.chest);
            SetBonePosition(rightUpperArmBone, next.rightShoulder, next.rightShoulder);
            SetBonePosition(rightForearmBone, next.rightElbow, next.rightShoulder);
            SetBonePosition(rightHandBone, next.rightWrist, next.rightElbow);

            SetBonePosition(leftThighBone, next.leftHip, next.pelvis);
            SetBonePosition(leftShinBone, next.leftKnee, next.leftHip);
            SetBonePosition(leftFootBone, next.leftAnkle, next.leftKnee);
            SetBonePosition(rightThighBone, next.rightHip, next.pelvis);
            SetBonePosition(rightShinBone, next.rightKnee, next.rightHip);
            SetBonePosition(rightFootBone, next.rightAnkle, next.rightKnee);
            basePelvisPosition = pelvisBone.anchoredPosition;

            ApplyPart(torsoPart, next.torso);
            ApplyPart(abdomenPart, next.abdomen);
            ApplyPart(pelvisPart, next.pelvisArt);
            ApplyPart(headPart, next.headArt);
            ApplyPart(leftUpperArmPart, next.leftUpperArm);
            ApplyPart(leftForearmPart, next.leftForearm);
            ApplyPart(leftHandPart, next.leftHand);
            ApplyPart(rightUpperArmPart, next.rightUpperArm);
            ApplyPart(rightForearmPart, next.rightForearm);
            ApplyPart(rightHandPart, next.rightHand);
            ApplyPart(leftThighPart, next.leftThigh);
            ApplyPart(leftShinPart, next.leftShin);
            ApplyPart(leftFootPart, next.leftFoot);
            ApplyPart(rightThighPart, next.rightThigh);
            ApplyPart(rightShinPart, next.rightShin);
            ApplyPart(rightFootPart, next.rightFoot);
        }

        private RectTransform CreateBone(Transform parent, string name)
        {
            RectTransform bone = LivingGameplayVisualFactory.CreateRect(
                parent,
                name,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            namedBones[name] = bone;
            return bone;
        }

        private RigPart CreatePart(RectTransform bone, string name)
        {
            RectTransform rect = LivingGameplayVisualFactory.CreateRect(
                bone,
                name,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(16f, 16f));
            RigPartGraphic graphic =
                rect.gameObject.AddComponent<RigPartGraphic>();
            graphic.color = Color.white;
            graphic.raycastTarget = false;
            frontRenderers.Add(graphic);
            return new RigPart
            {
                Bone = bone,
                Graphic = graphic
            };
        }

        private void SetBonePosition(
            RectTransform bone,
            Vector2 joint,
            Vector2 parentJoint)
        {
            bone.anchoredPosition = Vector2.Scale(joint - parentJoint, fullSize);
            bone.localRotation = Quaternion.identity;
            bone.localScale = Vector3.one;
        }

        private void ApplyPart(RigPart part, CharacterRigCrop crop)
        {
            part.Graphic.Configure(
                currentFrontTexture,
                sourceUv,
                crop,
                fullSize);
        }

        private static void SmoothPose(
            ref PoseFrame current,
            PoseFrame target,
            float blend)
        {
            current.pelvis = Mathf.LerpAngle(current.pelvis, target.pelvis, blend);
            current.spine = Mathf.LerpAngle(current.spine, target.spine, blend);
            current.chest = Mathf.LerpAngle(current.chest, target.chest, blend);
            current.neck = Mathf.LerpAngle(current.neck, target.neck, blend);
            current.head = Mathf.LerpAngle(current.head, target.head, blend);
            current.leftShoulder = Mathf.LerpAngle(current.leftShoulder, target.leftShoulder, blend);
            current.rightShoulder = Mathf.LerpAngle(current.rightShoulder, target.rightShoulder, blend);
            current.leftUpperArm = Mathf.LerpAngle(current.leftUpperArm, target.leftUpperArm, blend);
            current.leftForearm = Mathf.LerpAngle(current.leftForearm, target.leftForearm, blend);
            current.leftHand = Mathf.LerpAngle(current.leftHand, target.leftHand, blend);
            current.rightUpperArm = Mathf.LerpAngle(current.rightUpperArm, target.rightUpperArm, blend);
            current.rightForearm = Mathf.LerpAngle(current.rightForearm, target.rightForearm, blend);
            current.rightHand = Mathf.LerpAngle(current.rightHand, target.rightHand, blend);
            current.leftThigh = Mathf.LerpAngle(current.leftThigh, target.leftThigh, blend);
            current.leftShin = Mathf.LerpAngle(current.leftShin, target.leftShin, blend);
            current.leftFoot = Mathf.LerpAngle(current.leftFoot, target.leftFoot, blend);
            current.rightThigh = Mathf.LerpAngle(current.rightThigh, target.rightThigh, blend);
            current.rightShin = Mathf.LerpAngle(current.rightShin, target.rightShin, blend);
            current.rightFoot = Mathf.LerpAngle(current.rightFoot, target.rightFoot, blend);
            current.pelvisY = Mathf.Lerp(current.pelvisY, target.pelvisY, blend);
            current.pelvisX = Mathf.Lerp(current.pelvisX, target.pelvisX, blend);
            current.chestScale = Vector2.Lerp(current.chestScale, target.chestScale, blend);
            current.abdomenScale = Vector2.Lerp(
                current.abdomenScale,
                target.abdomenScale,
                blend);
        }

        private static float CalculateHeldEnvelope(float normalizedTime)
        {
            const float transition = 0.18f;
            float t = Mathf.Clamp01(normalizedTime);
            if (t < transition)
            {
                return Mathf.SmoothStep(0f, 1f, t / transition);
            }

            if (t > 1f - transition)
            {
                return Mathf.SmoothStep(
                    1f,
                    0f,
                    (t - (1f - transition)) / transition);
            }

            return 1f;
        }

        private static void SetRotation(RectTransform rect, float degrees)
        {
            rect.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }

        private void TryBindStateAnimator()
        {
            if (stateAnimator == null || animatorBound)
            {
                return;
            }

            RuntimeAnimatorController controller =
                Resources.Load<RuntimeAnimatorController>(
                    AnimatorResourcePath);
            if (controller == null)
            {
                return;
            }

            stateAnimator.runtimeAnimatorController = controller;
            stateAnimator.applyRootMotion = false;
            stateAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            stateAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animatorBound = true;
            currentBaseAnimatorState = string.Empty;
        }

        private void UpdateBaseAnimatorState()
        {
            if (!animatorBound)
            {
                TryBindStateAnimator();
            }

            string next;
            if (!moving)
            {
                next = "Idle_Breathe";
            }
            else
            {
                CharacterFacing facing = ResolveFacing();
                next = facing switch
                {
                    CharacterFacing.Back => "Walk_Back",
                    CharacterFacing.SideLeft => "Walk_Side",
                    CharacterFacing.SideRight => "Walk_Side",
                    _ => "Walk_Front"
                };
            }

            if (next == currentBaseAnimatorState)
            {
                return;
            }

            currentBaseAnimatorState = next;
            CrossFadeState(next, 0, 0.12f);
        }

        private void PlayAnimatorAction(CharacterRoutineAction action)
        {
            switch (action)
            {
                case CharacterRoutineAction.ShiftWeight:
                    CrossFadeState("Idle_ShiftWeight", 0, 0.12f);
                    break;
                case CharacterRoutineAction.LookAround:
                    CrossFadeState("Face_Look", 2, 0.1f);
                    break;
                case CharacterRoutineAction.Scratch:
                    CrossFadeState("Idle_Scratch", 1, 0.12f);
                    break;
                case CharacterRoutineAction.Yawn:
                    CrossFadeState("Idle_Yawn", 1, 0.12f);
                    break;
                case CharacterRoutineAction.Stretch:
                    CrossFadeState("Idle_Stretch", 1, 0.12f);
                    break;
                case CharacterRoutineAction.Flex:
                    CrossFadeState("Idle_Flex", 1, 0.12f);
                    break;
                case CharacterRoutineAction.AdjustClothes:
                    CrossFadeState("Idle_AdjustClothes", 1, 0.12f);
                    break;
                case CharacterRoutineAction.WarmShoulders:
                    CrossFadeState("Idle_WarmShoulders", 1, 0.12f);
                    break;
                case CharacterRoutineAction.SitDown:
                    CrossFadeState("SitDown", 3, 0.12f);
                    break;
                case CharacterRoutineAction.SitLoop:
                    CrossFadeState("SitLoop", 0, 0.12f);
                    break;
                case CharacterRoutineAction.StandUp:
                    CrossFadeState("StandUp", 3, 0.12f);
                    break;
            }
        }

        private void CrossFadeState(
            string stateName,
            int layer,
            float duration)
        {
            if (!animatorBound || stateAnimator == null ||
                stateAnimator.runtimeAnimatorController == null ||
                layer < 0 ||
                layer >= stateAnimator.layerCount)
            {
                return;
            }

            int hash = Animator.StringToHash(stateName);
            if (!stateAnimator.HasState(layer, hash))
            {
                hash = Animator.StringToHash(
                    $"{stateAnimator.GetLayerName(layer)}.{stateName}");
            }

            if (stateAnimator.HasState(layer, hash))
            {
                stateAnimator.CrossFade(hash, duration, layer);
            }
        }
    }
}
