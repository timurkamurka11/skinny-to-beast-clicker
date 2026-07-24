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
        Sit
    }

    [DisallowMultipleComponent]
    public sealed class CharacterRigController : MonoBehaviour
    {
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
        }

        private readonly List<RigPartGraphic> frontRenderers = new();
        private readonly Dictionary<string, RectTransform> namedBones = new();

        private RectTransform characterRoot;
        private RectTransform skeletonRoot;
        private CanvasGroup skeletonGroup;
        private RawImage directionalImage;
        private CharacterFaceController faceController;

        private RectTransform pelvisBone;
        private RectTransform spineBone;
        private RectTransform chestBone;
        private RectTransform neckBone;
        private RectTransform headBone;
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
        private int tapVariant;
        private bool moving;
        private bool built;

        public int BoneCount => namedBones.Count;
        public bool IsMoving => moving;
        public CharacterFacing Facing => ResolveFacing();

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

            skeletonRoot = LivingGameplayVisualFactory.CreateRect(
                root,
                "Skeleton.Root",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                fullSize);
            skeletonGroup = skeletonRoot.gameObject.AddComponent<CanvasGroup>();

            pelvisBone = CreateBone(skeletonRoot, "Bone.Pelvis");
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
            chestBone = CreateBone(spineBone, "Bone.Chest");
            torsoPart = CreatePart(chestBone, "Part.Torso");

            leftUpperArmBone = CreateBone(chestBone, "Bone.UpperArm.L");
            leftUpperArmPart = CreatePart(leftUpperArmBone, "Part.UpperArm.L");
            leftForearmBone = CreateBone(leftUpperArmBone, "Bone.Forearm.L");
            leftForearmPart = CreatePart(leftForearmBone, "Part.Forearm.L");
            leftHandBone = CreateBone(leftForearmBone, "Bone.Hand.L");
            leftHandPart = CreatePart(leftHandBone, "Part.Hand.L");

            rightUpperArmBone = CreateBone(chestBone, "Bone.UpperArm.R");
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
            built = true;
        }

        public void ApplySkin(CharacterSkinDefinition definition)
        {
            if (!built || definition == null || !definition.IsValid)
            {
                return;
            }

            Sprite sprite = definition.FrontSprite;
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
        }

        public void SetLocomotion(Vector2 direction, float speed)
        {
            moveDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector2.zero;
            moving = speed > 0.01f && moveDirection.sqrMagnitude > 0.001f;
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
        }

        public void SetRestingFacing(CharacterFacing facing)
        {
            restingFacing = facing;
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
                _ => CharacterExpression.Neutral
            };
            faceController?.SetExpression(expression, actionDuration);
        }

        public void CancelAction()
        {
            activeAction = CharacterRoutineAction.None;
            actionAge = actionDuration;
            faceController?.ResetExpression();
        }

        public void TriggerTap()
        {
            tapVariant++;
            tapAge = 0f;
            activeAction = CharacterRoutineAction.None;
            faceController?.LookAt(new Vector2(0f, -1f), 0.75f);
            faceController?.SetExpression(CharacterExpression.Strain, 0.46f);
        }

        public void TriggerUpgrade()
        {
            upgradeAge = 0f;
            PlayAction(CharacterRoutineAction.Flex, 0.95f);
            faceController?.SetExpression(CharacterExpression.Happy, 1.15f);
        }

        public void TriggerStageChange()
        {
            stageChangeAge = 0f;
            faceController?.SetExpression(CharacterExpression.Happy, 1.15f);
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
                walkCycle += delta * 5.2f;
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
                chestScale = new Vector2(1f - breath * 0.004f, 1f + breath * 0.012f)
            };

            if (walkBlend > 0.001f && !ShouldUseDirectionalRenderer())
            {
                float phase = walkCycle * Mathf.PI * 2f;
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
                float envelope = activeAction == CharacterRoutineAction.Sit
                    ? CalculateHeldEnvelope(t)
                    : Mathf.Sin(t * Mathf.PI);
                float repeat = Mathf.Sin(t * Mathf.PI * 4f);
                ApplyActionPose(ref pose, activeAction, envelope, repeat);
            }
            else if (actionAge >= actionDuration)
            {
                activeAction = CharacterRoutineAction.None;
            }

            if (tapAge < 0.48f)
            {
                float t = tapAge / 0.48f;
                float punch = Mathf.Sin(t * Mathf.PI);
                bool left = tapVariant % 2 == 0;
                pose.pelvisY += punch * 11f;
                pose.spine += (left ? -1f : 1f) * punch * 4f;
                pose.chest += (left ? 1f : -1f) * punch * 6f;
                if (left)
                {
                    pose.leftUpperArm -= punch * 38f;
                    pose.leftForearm -= punch * 92f;
                    pose.leftHand += punch * 20f;
                }
                else
                {
                    pose.rightUpperArm += punch * 38f;
                    pose.rightForearm += punch * 92f;
                    pose.rightHand -= punch * 20f;
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
                case CharacterRoutineAction.Sit:
                    pose.pelvisY -= 105f * envelope;
                    pose.spine -= 5f * envelope;
                    pose.leftThigh -= 58f * envelope;
                    pose.rightThigh += 58f * envelope;
                    pose.leftShin += 78f * envelope;
                    pose.rightShin -= 78f * envelope;
                    pose.leftUpperArm += 10f * envelope;
                    pose.rightUpperArm -= 10f * envelope;
                    break;
            }
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
            SetBonePosition(pelvisBone, next.pelvis, new Vector2(0.5f, 0.5f));
            SetBonePosition(spineBone, next.spine, next.pelvis);
            SetBonePosition(chestBone, next.chest, next.spine);
            SetBonePosition(neckBone, next.neck, next.chest);
            SetBonePosition(headBone, next.head, next.neck);

            SetBonePosition(leftUpperArmBone, next.leftShoulder, next.chest);
            SetBonePosition(leftForearmBone, next.leftElbow, next.leftShoulder);
            SetBonePosition(leftHandBone, next.leftWrist, next.leftElbow);
            SetBonePosition(rightUpperArmBone, next.rightShoulder, next.chest);
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
    }
}
