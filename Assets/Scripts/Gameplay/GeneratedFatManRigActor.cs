using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Original code-authored 2D character. It owns its bone transforms,
    /// skinned meshes, facial states and Front/Side/Back view rigs.
    /// No visible surface is cut from a PNG and no old mannequin transform is
    /// included in a SkinnedMeshRenderer.bones array.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GeneratedFatManRigActor : MonoBehaviour
    {
        private static readonly Color Skin =
            new Color(1f, 0.61f, 0.46f, 1f);
        private static readonly Color SkinLight =
            new Color(1f, 0.73f, 0.59f, 1f);
        private static readonly Color SkinShadow =
            new Color(0.78f, 0.31f, 0.23f, 1f);
        private static readonly Color Hair =
            new Color(0.16f, 0.075f, 0.045f, 1f);
        private static readonly Color Shirt =
            new Color(0.58f, 0.055f, 0.055f, 1f);
        private static readonly Color Stripe =
            new Color(0.78f, 0.14f, 0.11f, 1f);
        private static readonly Color ShirtShadow =
            new Color(0.34f, 0.025f, 0.035f, 1f);
        private static readonly Color Shorts =
            new Color(0.17f, 0.18f, 0.19f, 1f);
        private static readonly Color ShortsLight =
            new Color(0.27f, 0.28f, 0.29f, 1f);
        private static readonly Color Shoe =
            new Color(0.055f, 0.15f, 0.22f, 1f);
        private static readonly Color Sole =
            new Color(0.62f, 0.60f, 0.54f, 1f);
        private static readonly Color Dark =
            new Color(0.095f, 0.04f, 0.025f, 1f);
        private static readonly Color Badge =
            new Color(1f, 0.71f, 0.06f, 1f);
        private static readonly Color Sweat =
            new Color(0.78f, 0.92f, 1f, 0.82f);

        private readonly List<ViewRig> views = new();
        private GeneratedFatManAssetScope assets;
        private ViewRig active;
        private CharacterFacing facing = CharacterFacing.Front;
        private CharacterRoutineAction action;
        private CharacterRoutineAction previousAction;
        private int stage;
        private int tapVariant;
        private bool moving;
        private bool tapReacting;
        private bool previousTap;
        private float tapStartedAt = -10f;
        private float actionStartedAt;
        private float nextBlinkAt;
        private float blinkStartedAt = -10f;
        private float bellySpring;
        private float bellyVelocity;
        private float shirtSpring;
        private float shirtVelocity;
        private float chinSpring;
        private float chinVelocity;
        private bool built;

        public bool IsReady => built && active != null;
        public int ViewCount => views.Count;
        public int BoneCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < views.Count; i++)
                    count += views[i].Bones.Count;
                return count;
            }
        }
        public int SkinnedSurfaceCount =>
            GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;

        public void Build()
        {
            if (built) return;

            assets = new GeneratedFatManAssetScope();
            views.Add(BuildView(ViewKind.Front));
            views.Add(BuildView(ViewKind.Side));
            views.Add(BuildView(ViewKind.Back));
            SetFacing(CharacterFacing.Front, true);
            for (int i = 0; i < views.Count; i++)
                views[i].SetFace(false, MouthState.Neutral);
            ScheduleBlink();

            built = views.Count == 3 &&
                    BoneCount >= 45 &&
                    SkinnedSurfaceCount >= 45;
        }

        public void SetSignals(
            CharacterFacing newFacing,
            int newStage,
            bool isMoving,
            bool isTapReacting,
            int newTapVariant,
            CharacterRoutineAction newAction)
        {
            if (!built) return;

            SetFacing(newFacing, false);
            stage = Mathf.Clamp(newStage, 0, 3);
            moving = isMoving;
            tapReacting = isTapReacting;
            tapVariant = Mathf.Abs(newTapVariant) % 3;

            if (tapReacting && !previousTap)
                tapStartedAt = Time.unscaledTime;
            previousTap = tapReacting;

            if (newAction != previousAction)
            {
                actionStartedAt = Time.unscaledTime;
                previousAction = newAction;
            }
            action = newAction;
            Animate(Time.unscaledTime);
        }

        public Bounds CalculateVisibleBounds()
        {
            Renderer[] renderers =
                GetComponentsInChildren<Renderer>(false);
            Bounds result = new Bounds(transform.position, Vector3.zero);
            bool found = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled) continue;
                if (!found)
                {
                    result = renderer.bounds;
                    found = true;
                }
                else
                {
                    result.Encapsulate(renderer.bounds);
                }
            }
            return found ? result : new Bounds(transform.position, Vector3.one);
        }

        private ViewRig BuildView(ViewKind kind)
        {
            ViewRig view = BuildSkeleton(kind);
            BuildLegs(view);
            BuildArms(view);
            BuildTorso(view);
            BuildHead(view);
            if (kind != ViewKind.Back)
                BuildFace(view);
            return view;
        }

        private ViewRig BuildSkeleton(ViewKind kind)
        {
            GameObject rootObject = new GameObject(kind + ".View");
            rootObject.transform.SetParent(transform, false);
            ViewRig v = new ViewRig(
                rootObject.transform,
                kind,
                kind == ViewKind.Back ? -1f : 1f);

            v.RigRoot = v.AddBone(v.Root, "RigRoot", Vector2.zero);
            v.Pelvis = v.AddBone(v.RigRoot.T, "Pelvis",
                new Vector2(0f, -1.12f));
            v.Spine = v.AddBone(v.Pelvis.T, "Spine.01",
                new Vector2(0f, 1.18f));
            v.Chest = v.AddBone(v.Spine.T, "Chest",
                new Vector2(0f, 1.02f));
            v.Neck = v.AddBone(v.Chest.T, "Neck",
                new Vector2(0f, 0.91f));
            v.Head = v.AddBone(v.Neck.T, "Head",
                new Vector2(kind == ViewKind.Side ? 0.12f : 0f, 0.66f));
            v.Chin = v.AddBone(v.Head.T, "ChinSoft",
                new Vector2(0f, -0.50f));
            v.Belly = v.AddBone(v.Pelvis.T, "Belly",
                new Vector2(kind == ViewKind.Side ? 0.34f : 0f, 1.05f));
            v.ShirtHem = v.AddBone(v.Pelvis.T, "ShirtHem",
                new Vector2(kind == ViewKind.Side ? 0.24f : 0f, 0.52f));

            float shoulderL = kind == ViewKind.Side ? -0.10f : -1.12f;
            float shoulderR = kind == ViewKind.Side ? 0.28f : 1.12f;
            v.ClavicleL = v.AddBone(v.Chest.T, "Clavicle.L",
                new Vector2(shoulderL, 0.23f));
            v.UpperArmL = v.AddBone(v.ClavicleL.T, "UpperArm.L",
                Vector2.zero);
            v.ForearmL = v.AddBone(v.UpperArmL.T, "Forearm.L",
                new Vector2(0f, -1.16f));
            v.HandL = v.AddBone(v.ForearmL.T, "Hand.L",
                new Vector2(0f, -0.94f));
            v.ClavicleR = v.AddBone(v.Chest.T, "Clavicle.R",
                new Vector2(shoulderR, 0.23f));
            v.UpperArmR = v.AddBone(v.ClavicleR.T, "UpperArm.R",
                Vector2.zero);
            v.ForearmR = v.AddBone(v.UpperArmR.T, "Forearm.R",
                new Vector2(0f, -1.16f));
            v.HandR = v.AddBone(v.ForearmR.T, "Hand.R",
                new Vector2(0f, -0.94f));

            float hip = kind == ViewKind.Side ? 0.18f : 0.60f;
            v.ThighL = v.AddBone(v.Pelvis.T, "Thigh.L",
                new Vector2(-hip, -0.18f));
            v.ShinL = v.AddBone(v.ThighL.T, "Shin.L",
                new Vector2(0f, -1.42f));
            v.FootL = v.AddBone(v.ShinL.T, "Foot.L",
                new Vector2(0f, -1.08f));
            v.ThighR = v.AddBone(v.Pelvis.T, "Thigh.R",
                new Vector2(hip, -0.18f));
            v.ShinR = v.AddBone(v.ThighR.T, "Shin.R",
                new Vector2(0f, -1.42f));
            v.FootR = v.AddBone(v.ShinR.T, "Foot.R",
                new Vector2(0f, -1.08f));
            return v;
        }

        private void BuildLegs(ViewRig v)
        {
            bool side = v.Kind == ViewKind.Side;
            CreateLimb(v, v.ThighL, "Thigh.L",
                new Vector2(0f, -0.64f),
                side ? new Vector2(0.82f, 1.62f) :
                       new Vector2(0.92f, 1.65f),
                side ? SkinShadow : Skin, side ? 7 : 10);
            CreateLimb(v, v.ShinL, "Shin.L",
                new Vector2(0f, -0.52f),
                side ? new Vector2(0.68f, 1.36f) :
                       new Vector2(0.72f, 1.40f),
                side ? SkinShadow : Skin, side ? 8 : 11);
            CreateLimb(v, v.FootL, "Foot.L",
                new Vector2(side ? 0.28f : -0.10f, -0.25f),
                new Vector2(1.12f, 0.53f),
                Shoe, side ? 9 : 12);
            CreateLimb(v, v.FootL, "Sole.L",
                new Vector2(side ? 0.28f : -0.10f, -0.43f),
                new Vector2(1.08f, 0.16f),
                Sole, side ? 10 : 13, 0.025f);

            CreateLimb(v, v.ThighR, "Thigh.R",
                new Vector2(0f, -0.64f),
                new Vector2(0.93f, 1.68f),
                Skin, 28);
            CreateLimb(v, v.ShinR, "Shin.R",
                new Vector2(0f, -0.52f),
                new Vector2(0.73f, 1.43f),
                Skin, 29);
            CreateLimb(v, v.FootR, "Foot.R",
                new Vector2(side ? 0.36f : 0.10f, -0.25f),
                new Vector2(1.20f, 0.55f),
                Shoe, 30);
            CreateLimb(v, v.FootR, "Sole.R",
                new Vector2(side ? 0.36f : 0.10f, -0.43f),
                new Vector2(1.16f, 0.16f),
                Sole, 31, 0.025f);
        }

        private void BuildArms(ViewRig v)
        {
            bool side = v.Kind == ViewKind.Side;
            CreateLimb(v, v.UpperArmL, "UpperArm.L",
                new Vector2(side ? 0f : -0.07f, -0.57f),
                new Vector2(side ? 0.58f : 0.66f, 1.44f),
                side ? SkinShadow : Skin, side ? 6 : 34);
            CreateLimb(v, v.ForearmL, "Forearm.L",
                new Vector2(side ? 0f : -0.02f, -0.47f),
                new Vector2(side ? 0.53f : 0.58f, 1.18f),
                side ? SkinShadow : Skin, side ? 7 : 35);
            CreateLimb(v, v.HandL, "Hand.L",
                new Vector2(0f, -0.22f),
                new Vector2(side ? 0.58f : 0.64f, 0.64f),
                side ? SkinShadow : SkinLight, side ? 8 : 36);

            CreateLimb(v, v.UpperArmR, "UpperArm.R",
                new Vector2(side ? 0.03f : 0.07f, -0.57f),
                new Vector2(side ? 0.70f : 0.66f, 1.48f),
                Skin, 36);
            CreateLimb(v, v.ForearmR, "Forearm.R",
                new Vector2(side ? 0.04f : 0.02f, -0.47f),
                new Vector2(side ? 0.61f : 0.58f, 1.22f),
                Skin, 37);
            CreateLimb(v, v.HandR, "Hand.R",
                new Vector2(side ? 0.05f : 0f, -0.22f),
                new Vector2(0.67f, 0.68f),
                SkinLight, 38);
        }

        private void BuildTorso(ViewRig v)
        {
            bool side = v.Kind == ViewKind.Side;
            bool back = v.Kind == ViewKind.Back;
            Transform[] bellyBones =
            {
                v.Pelvis.T, v.Spine.T, v.Belly.T
            };
            Transform[] shirtBones =
            {
                v.Spine.T, v.Chest.T, v.Belly.T
            };
            Transform[] shortsBones =
            {
                v.Pelvis.T, v.ThighL.T, v.ThighR.T
            };

            Vector2 bellyCenter = side
                ? new Vector2(0.52f, -0.03f)
                : new Vector2(0f, -0.02f);
            Soft(v, "Body.Belly", bellyCenter,
                side ? new Vector2(2.46f, 2.44f) :
                       new Vector2(2.70f, 2.45f),
                bellyBones,
                side ? BellyWeightsSide : BellyWeights,
                Skin, 18);

            Vector2 shirtCenter = side
                ? new Vector2(0.34f, 0.86f)
                : new Vector2(0f, 0.88f);
            Soft(v, back ? "Back.Shirt" : "Shirt.Main", shirtCenter,
                side ? new Vector2(2.18f, 2.50f) :
                       new Vector2(2.78f, 2.52f),
                shirtBones,
                side ? ShirtWeightsSide : ShirtWeights,
                Shirt, 21);

            Soft(v, "Shirt.Stripe.Upper",
                shirtCenter + new Vector2(side ? 0.06f : 0f, 0.34f),
                side ? new Vector2(2.00f, 0.20f) :
                       new Vector2(2.55f, 0.22f),
                shirtBones,
                side ? ShirtWeightsSide : ShirtWeights,
                Stripe, 23, 0.02f);
            Soft(v, "Shirt.Stripe.Lower",
                shirtCenter + new Vector2(side ? 0.14f : 0f, -0.25f),
                side ? new Vector2(2.12f, 0.20f) :
                       new Vector2(2.64f, 0.22f),
                shirtBones,
                side ? ShirtWeightsSide : ShirtWeights,
                Stripe, 23, 0.02f);

            Soft(v, "Shirt.Hem",
                side ? new Vector2(0.51f, -0.04f) :
                       new Vector2(0f, -0.02f),
                side ? new Vector2(2.18f, 0.27f) :
                       new Vector2(2.67f, 0.28f),
                new[] { v.Pelvis.T, v.ShirtHem.T, v.Belly.T },
                HemWeights, ShirtShadow, 24, 0.025f);

            Soft(v, "Shorts.Pelvis",
                side ? new Vector2(0.12f, -1.30f) :
                       new Vector2(0f, -1.30f),
                side ? new Vector2(2.18f, 1.24f) :
                       new Vector2(2.52f, 1.22f),
                shortsBones, PelvisWeights, Shorts, 25);
            Soft(v, "Shorts.Highlight",
                side ? new Vector2(0.12f, -1.10f) :
                       new Vector2(0f, -1.10f),
                side ? new Vector2(1.90f, 0.14f) :
                       new Vector2(2.25f, 0.15f),
                shortsBones, PelvisWeights, ShortsLight, 26, 0.02f);

            if (!back)
            {
                CreateLimb(v, v.Chest, "Badge",
                    new Vector2(side ? 0.55f : 0f, 0.33f),
                    new Vector2(0.29f, 0.29f),
                    Badge, 32, 0.03f);
            }
        }

        private void BuildHead(ViewRig v)
        {
            bool side = v.Kind == ViewKind.Side;
            bool back = v.Kind == ViewKind.Back;
            Vector2 head = Point(v, v.Head);
            CreateLimb(v, v.Neck, "Neck",
                new Vector2(side ? 0.12f : 0f, 0.22f),
                new Vector2(side ? 0.72f : 0.82f, 1.02f),
                SkinShadow, 19);
            CreateLimb(v, v.Head, "Head",
                new Vector2(side ? 0.12f : 0f, 0f),
                new Vector2(side ? 1.42f : 1.66f, 1.50f),
                back ? Skin : SkinLight, 40);
            if (!side)
            {
                CreateLimb(v, v.Head, "Ear.L",
                    new Vector2(-0.82f, -0.03f),
                    new Vector2(0.34f, 0.50f), Skin, 39);
                CreateLimb(v, v.Head, "Ear.R",
                    new Vector2(0.82f, -0.03f),
                    new Vector2(0.34f, 0.50f), Skin, 39);
            }
            else
            {
                CreateLimb(v, v.Head, "Ear",
                    new Vector2(-0.55f, -0.02f),
                    new Vector2(0.34f, 0.50f), Skin, 41);
                CreateLimb(v, v.Head, "Nose",
                    new Vector2(0.78f, 0.03f),
                    new Vector2(0.35f, 0.32f), SkinLight, 43, 0.035f);
            }
            CreateLimb(v, v.Chin, "Chin",
                new Vector2(side ? 0.28f : 0f, -0.08f),
                new Vector2(side ? 0.96f : 1.10f, 0.48f),
                Skin, 42, 0.04f);

            CreateLimb(v, v.Head, "Hair.Base",
                new Vector2(side ? 0.06f : 0f, 0.49f),
                new Vector2(side ? 1.45f : 1.72f, 0.84f),
                Hair, 46, 0.04f);
            int spikes = side ? 6 : 8;
            for (int i = 0; i < spikes; i++)
            {
                float t = i / (float)(spikes - 1);
                float x = Mathf.Lerp(
                    side ? -0.65f : -0.72f,
                    side ? 0.50f : 0.72f, t);
                float y = 0.78f + Mathf.Sin(t * Mathf.PI) * 0.17f;
                Vector2[] points =
                {
                    head + new Vector2(x - 0.17f, y - 0.20f),
                    head + new Vector2(x, y + 0.30f +
                        (i % 2 == 0 ? 0.08f : 0f)),
                    head + new Vector2(x + 0.19f, y - 0.18f)
                };
                GeneratedFatManMeshFactory.CreateOutlinedPolygon(
                    v.Root, "Hair.Spike." + i, points, v.Head.T,
                    Hair, 47, assets, 1.08f);
            }

            if (!back)
            {
                CreateLimb(v, v.Head, "Sweat.Head",
                    new Vector2(side ? 0.63f : 0.68f, 0.25f),
                    new Vector2(0.10f, 0.19f),
                    Sweat, 58, 0.012f);
            }
        }

        private void BuildFace(ViewRig v)
        {
            bool side = v.Kind == ViewKind.Side;
            Vector2 eyeL = side
                ? new Vector2(0.48f, 0.10f)
                : new Vector2(-0.30f, 0.10f);
            Vector2 eyeR = new Vector2(0.30f, 0.10f);

            v.EyesOpen.Add(Face(v, "Eye.L.Open", eyeL,
                new Vector2(0.16f, 0.23f), Dark, 52));
            v.EyesClosed.Add(Face(v, "Eye.L.Closed",
                eyeL + new Vector2(0f, -0.01f),
                new Vector2(0.27f, 0.055f), Dark, 53));
            if (!side)
            {
                v.EyesOpen.Add(Face(v, "Eye.R.Open", eyeR,
                    new Vector2(0.16f, 0.23f), Dark, 52));
                v.EyesClosed.Add(Face(v, "Eye.R.Closed",
                    eyeR + new Vector2(0f, -0.01f),
                    new Vector2(0.27f, 0.055f), Dark, 53));
                Face(v, "Brow.L", new Vector2(-0.31f, 0.34f),
                    new Vector2(0.36f, 0.07f), Hair, 54);
                Face(v, "Brow.R", new Vector2(0.31f, 0.34f),
                    new Vector2(0.36f, 0.07f), Hair, 54);
            }
            else
            {
                Face(v, "Brow", new Vector2(0.45f, 0.34f),
                    new Vector2(0.34f, 0.07f), Hair, 54);
            }

            Vector2 mouth = side
                ? new Vector2(0.58f, -0.26f)
                : new Vector2(0f, -0.27f);
            float scale = side ? 0.85f : 1f;
            v.MouthNeutral = Face(v, "Mouth.Neutral", mouth,
                new Vector2(0.42f, 0.075f) * scale, Dark, 55);
            v.MouthOpen = Face(v, "Mouth.Open", mouth,
                new Vector2(0.38f, 0.24f) * scale, Dark, 55);
            v.MouthStrain = Face(v, "Mouth.Strain", mouth,
                new Vector2(0.52f, 0.095f) * scale, Dark, 55);
            v.MouthYawn = Face(v, "Mouth.Yawn",
                mouth + new Vector2(0f, -0.04f),
                new Vector2(0.48f, 0.53f) * scale, Dark, 55);
        }

        private void CreateLimb(
            ViewRig v,
            BonePose bone,
            string name,
            Vector2 offset,
            Vector2 size,
            Color color,
            int order,
            float outline = 0.055f)
        {
            Vector2 center = Point(v, bone) + offset;
            GeneratedFatManMeshFactory.CreateOutlinedEllipse(
                v.Root, name, center, size, new[] { bone.T },
                _ => GeneratedFatManMeshFactory.RigidWeight(),
                color, order, assets, 26, 4, outline);
        }

        private void Soft(
            ViewRig v,
            string name,
            Vector2 center,
            Vector2 size,
            Transform[] bones,
            GeneratedFatManMeshFactory.WeightProvider weights,
            Color color,
            int order,
            float outline = 0.055f)
        {
            GeneratedFatManMeshFactory.CreateOutlinedEllipse(
                v.Root, name, center, size, bones, weights,
                color, order, assets, 30, 5, outline);
        }

        private GameObject Face(
            ViewRig v,
            string name,
            Vector2 offset,
            Vector2 size,
            Color color,
            int order)
        {
            Vector2 center = Point(v, v.Head) + offset;
            return GeneratedFatManMeshFactory.CreateEllipse(
                v.Root, name, center, size, new[] { v.Head.T },
                _ => GeneratedFatManMeshFactory.RigidWeight(),
                color, order, assets, 20, 3).gameObject;
        }

        private void Animate(float time)
        {
            if (active == null) return;
            active.ResetPose();
            ApplyStage(active);
            ApplyIdle(active, time);
            if (moving) ApplyWalk(active, time);
            if (tapReacting ||
                time - tapStartedAt < TapDuration(tapVariant))
                ApplyTap(active, time);
            ApplyAction(active, time);
            ApplySecondary(active, time);
            ApplyFace(active, time);
        }

        private void ApplyStage(ViewRig v)
        {
            float bellyX = new[] { 1f, 0.94f, 0.85f, 0.76f }[stage];
            float bellyY = new[] { 1f, 0.96f, 0.92f, 0.88f }[stage];
            float shoulder = new[] { 1f, 1.02f, 1.07f, 1.13f }[stage];
            float posture = new[] { 0f, 1.8f, 3.6f, 5.2f }[stage];
            float arm = new[] { 1f, 1.015f, 1.04f, 1.075f }[stage];
            v.Belly.Scale(new Vector3(bellyX, bellyY, 1f));
            v.ShirtHem.Scale(new Vector3(
                Mathf.Lerp(1f, bellyX, 0.75f), bellyY, 1f));
            v.Chest.Scale(new Vector3(shoulder, 1f, 1f));
            v.Spine.Rotate(posture * v.PostureSign);
            v.UpperArmL.Scale(Vector3.one * arm);
            v.UpperArmR.Scale(Vector3.one * arm);
        }

        private static void ApplyIdle(ViewRig v, float time)
        {
            float cycle = time * Mathf.PI * 0.5f;
            float breath = Mathf.Sin(cycle);
            float delayed = Mathf.Sin(cycle - 0.42f);
            float sway = Mathf.Sin(time * 0.72f);
            v.Pelvis.Move(new Vector3(sway * 0.018f, breath * 0.026f, 0f));
            v.Pelvis.Rotate(sway * 0.7f);
            v.Spine.Rotate(-sway * 0.55f);
            v.Chest.Rotate(sway * 0.42f);
            v.Chest.Scale(new Vector3(
                1f + breath * 0.006f, 1f + breath * 0.018f, 1f));
            v.Belly.Move(new Vector3(0f, delayed * 0.018f, 0f));
            v.Belly.Scale(new Vector3(
                1f + delayed * 0.017f, 1f + delayed * 0.024f, 1f));
            v.ShirtHem.Rotate(delayed * 0.85f);
            v.Head.Rotate(-sway * 0.4f);
            v.UpperArmL.Rotate(sway * 0.65f);
            v.UpperArmR.Rotate(-sway * 0.65f);
        }

        private static void ApplyWalk(ViewRig v, float time)
        {
            float phase = time / 0.80f * Mathf.PI * 2f;
            float stride = Mathf.Sin(phase);
            float liftL = Mathf.Max(0f, -Mathf.Cos(phase));
            float liftR = Mathf.Max(0f, Mathf.Cos(phase));
            float leg = v.Kind == ViewKind.Side ? 25f : 18f;
            float arm = v.Kind == ViewKind.Side ? 20f : 14f;

            v.Pelvis.Move(new Vector3(
                0f, Mathf.Abs(Mathf.Cos(phase)) * 0.075f, 0f));
            v.Pelvis.Rotate(stride * 1.8f);
            v.Chest.Rotate(-stride * 1.4f);
            v.ThighL.Rotate(stride * leg);
            v.ThighR.Rotate(-stride * leg);
            v.ShinL.Rotate(liftL * 30f);
            v.ShinR.Rotate(liftR * 30f);
            v.FootL.Rotate(-stride * 9f - liftL * 8f);
            v.FootR.Rotate(stride * 9f - liftR * 8f);
            v.UpperArmL.Rotate(-stride * arm);
            v.UpperArmR.Rotate(stride * arm);
            v.ForearmL.Rotate(8f + Mathf.Max(0f, stride) * 14f);
            v.ForearmR.Rotate(-8f - Mathf.Max(0f, -stride) * 14f);
            v.Belly.Rotate(Mathf.Sin(phase - 0.68f) * 2.8f);
            v.ShirtHem.Rotate(Mathf.Sin(phase - 0.92f) * 3.4f);
        }

        private void ApplyTap(ViewRig v, float time)
        {
            float progress = Mathf.Clamp01(
                (time - tapStartedAt) / TapDuration(tapVariant));
            if (progress >= 1f) return;
            float punch = Mathf.Sin(progress * Mathf.PI);
            float recoil = Mathf.Sin(progress * Mathf.PI * 2f) *
                           (1f - progress);
            if (tapVariant == 1)
            {
                v.Pelvis.Move(new Vector3(0f, -punch * 0.12f, 0f));
                v.Belly.Scale(new Vector3(
                    1f + punch * 0.065f, 1f - punch * 0.025f, 1f));
                v.ShirtHem.Rotate(recoil * 6f);
            }
            else if (tapVariant == 2)
            {
                v.Head.Rotate(v.PostureSign * punch * 8f);
                v.Chest.Rotate(-v.PostureSign * punch * 4f);
                v.UpperArmR.Rotate(v.PostureSign * punch * 13f);
            }
            else
            {
                v.Pelvis.Move(new Vector3(0f, punch * 0.065f, 0f));
                v.Chest.Rotate(recoil * v.PostureSign * 4f);
                v.Head.Rotate(-recoil * v.PostureSign * 5f);
                v.Belly.Scale(new Vector3(
                    1f + punch * 0.035f, 1f + punch * 0.02f, 1f));
            }
        }

        private void ApplyAction(ViewRig v, float time)
        {
            if (action == CharacterRoutineAction.None) return;
            float p = Mathf.Clamp01(
                (time - actionStartedAt) / ActionDuration(action));
            float bell = Mathf.Sin(p * Mathf.PI);
            float wave = Mathf.Sin(p * Mathf.PI * 2f);

            switch (action)
            {
                case CharacterRoutineAction.ShiftWeight:
                    v.Pelvis.Move(new Vector3(wave * 0.16f, 0f, 0f));
                    v.Pelvis.Rotate(wave * 3.2f);
                    v.Chest.Rotate(-wave * 2f);
                    break;
                case CharacterRoutineAction.LookAround:
                    v.Head.Rotate(wave * 9f);
                    v.Chest.Rotate(wave * 2.2f);
                    break;
                case CharacterRoutineAction.Scratch:
                    v.UpperArmR.Rotate(bell * 132f);
                    v.ForearmR.Rotate(-bell * 112f);
                    v.HandR.Rotate(Mathf.Sin(p * Mathf.PI * 8f) * bell * 8f);
                    break;
                case CharacterRoutineAction.Yawn:
                    v.Head.Rotate(-bell * 5f);
                    v.Chest.Scale(new Vector3(
                        1f + bell * 0.015f, 1f + bell * 0.035f, 1f));
                    break;
                case CharacterRoutineAction.Stretch:
                    v.UpperArmL.Rotate(-bell * 154f);
                    v.UpperArmR.Rotate(bell * 154f);
                    v.ForearmL.Rotate(bell * 12f);
                    v.ForearmR.Rotate(-bell * 12f);
                    v.Belly.Scale(new Vector3(
                        1f - bell * 0.025f, 1f + bell * 0.055f, 1f));
                    break;
                case CharacterRoutineAction.Flex:
                    v.UpperArmL.Rotate(-bell * 73f);
                    v.UpperArmR.Rotate(bell * 73f);
                    v.ForearmL.Rotate(bell * 105f);
                    v.ForearmR.Rotate(-bell * 105f);
                    v.Chest.Scale(new Vector3(
                        1f + bell * 0.055f, 1f + bell * 0.02f, 1f));
                    break;
                case CharacterRoutineAction.AdjustClothes:
                    v.UpperArmL.Rotate(-bell * 35f);
                    v.UpperArmR.Rotate(bell * 35f);
                    v.ForearmL.Rotate(bell * 52f);
                    v.ForearmR.Rotate(-bell * 52f);
                    v.ShirtHem.Move(new Vector3(0f, bell * 0.075f, 0f));
                    break;
                case CharacterRoutineAction.WarmShoulders:
                    float shoulder = Mathf.Sin(p * Mathf.PI * 6f) * bell;
                    v.UpperArmL.Rotate(shoulder * 9f);
                    v.UpperArmR.Rotate(-shoulder * 9f);
                    v.Chest.Rotate(shoulder * 1.8f);
                    break;
                case CharacterRoutineAction.SitDown:
                    ApplySit(v, Mathf.SmoothStep(0f, 1f, p));
                    break;
                case CharacterRoutineAction.SitLoop:
                case CharacterRoutineAction.Sit:
                    ApplySit(v, 1f);
                    break;
                case CharacterRoutineAction.StandUp:
                    ApplySit(v, 1f - Mathf.SmoothStep(0f, 1f, p));
                    break;
            }
        }

        private static void ApplySit(ViewRig v, float amount)
        {
            v.Pelvis.Move(new Vector3(0f, -amount * 0.78f, 0f));
            v.Spine.Rotate(v.PostureSign * amount * 10f);
            v.ThighL.Rotate(-v.PostureSign * amount * 72f);
            v.ThighR.Rotate(v.PostureSign * amount * 72f);
            v.ShinL.Rotate(amount * 83f);
            v.ShinR.Rotate(-amount * 83f);
            v.Belly.Scale(new Vector3(
                1f + amount * 0.08f, 1f - amount * 0.055f, 1f));
        }

        private void ApplySecondary(ViewRig v, float time)
        {
            float target = moving
                ? Mathf.Sin(time / 0.8f * Mathf.PI * 2f - 0.65f)
                : Mathf.Sin(time * Mathf.PI * 0.5f - 0.42f) * 0.35f;
            if (tapReacting) target += 0.55f;

            bellySpring = Mathf.SmoothDamp(
                bellySpring, target, ref bellyVelocity,
                moving ? 0.07f : 0.14f, Mathf.Infinity,
                Time.unscaledDeltaTime);
            shirtSpring = Mathf.SmoothDamp(
                shirtSpring, bellySpring, ref shirtVelocity,
                0.09f, Mathf.Infinity, Time.unscaledDeltaTime);
            chinSpring = Mathf.SmoothDamp(
                chinSpring, -v.Head.Angle * 0.035f, ref chinVelocity,
                0.11f, Mathf.Infinity, Time.unscaledDeltaTime);

            float damping = Mathf.Lerp(1f, 0.55f, stage / 3f);
            v.Belly.Rotate(bellySpring * 2.1f * damping);
            v.ShirtHem.Rotate(shirtSpring * 3.2f * damping);
            v.Chin.Rotate(chinSpring * 1.4f * damping);
        }

        private void ApplyFace(ViewRig v, float time)
        {
            bool blink = IsBlinking(time);
            MouthState mouth = MouthState.Neutral;
            if (tapReacting ||
                time - tapStartedAt < TapDuration(tapVariant))
                mouth = tapVariant == 2 ? MouthState.Strain : MouthState.Open;

            if (action == CharacterRoutineAction.Yawn)
            {
                float p = Mathf.Clamp01(
                    (time - actionStartedAt) / ActionDuration(action));
                if (p > 0.18f && p < 0.84f)
                {
                    mouth = MouthState.Yawn;
                    blink = p > 0.28f && p < 0.72f;
                }
            }
            else if (action == CharacterRoutineAction.Flex ||
                     action == CharacterRoutineAction.Stretch)
            {
                mouth = MouthState.Strain;
            }
            v.SetFace(blink, mouth);
        }

        private bool IsBlinking(float time)
        {
            if (time >= nextBlinkAt && blinkStartedAt < 0f)
                blinkStartedAt = time;
            if (blinkStartedAt < 0f) return false;
            float elapsed = time - blinkStartedAt;
            if (elapsed >= 0.14f)
            {
                blinkStartedAt = -10f;
                ScheduleBlink();
                return false;
            }
            return elapsed >= 0.035f && elapsed <= 0.115f;
        }

        private void ScheduleBlink()
        {
            nextBlinkAt = Time.unscaledTime +
                          UnityEngine.Random.Range(2.2f, 5.4f);
        }

        private void SetFacing(CharacterFacing value, bool force)
        {
            if (!force && value == facing) return;
            facing = value;
            ViewKind target = value == CharacterFacing.Back
                ? ViewKind.Back
                : value == CharacterFacing.SideLeft ||
                  value == CharacterFacing.SideRight
                    ? ViewKind.Side
                    : ViewKind.Front;
            bool mirror = value == CharacterFacing.SideRight;
            for (int i = 0; i < views.Count; i++)
            {
                bool enabled = views[i].Kind == target;
                views[i].Root.gameObject.SetActive(enabled);
                if (enabled)
                {
                    active = views[i];
                    active.Root.localScale =
                        mirror ? new Vector3(-1f, 1f, 1f) : Vector3.one;
                }
            }
        }

        private static Vector2 Point(ViewRig v, BonePose bone)
        {
            Vector3 point = v.Root.InverseTransformPoint(bone.T.position);
            return new Vector2(point.x, point.y);
        }

        private static BoneWeight BellyWeights(Vector2 p)
        {
            float top = Mathf.Clamp01((p.y + 1f) * 0.5f);
            return GeneratedFatManMeshFactory.Blend(
                0, (1f - top) * 0.45f,
                1, top * 0.35f,
                2, 0.45f * (1f - Mathf.Abs(p.x) * 0.25f));
        }

        private static BoneWeight BellyWeightsSide(Vector2 p)
        {
            float top = Mathf.Clamp01((p.y + 1f) * 0.5f);
            float front = Mathf.Clamp01((p.x + 1f) * 0.5f);
            return GeneratedFatManMeshFactory.Blend(
                0, (1f - top) * 0.40f,
                1, top * 0.32f,
                2, 0.40f + front * 0.25f);
        }

        private static BoneWeight ShirtWeights(Vector2 p)
        {
            float top = Mathf.Clamp01((p.y + 1f) * 0.5f);
            return GeneratedFatManMeshFactory.Blend(
                0, (1f - top) * 0.22f,
                1, top * 0.62f,
                2, (1f - top) * 0.36f);
        }

        private static BoneWeight ShirtWeightsSide(Vector2 p)
        {
            float top = Mathf.Clamp01((p.y + 1f) * 0.5f);
            float front = Mathf.Clamp01((p.x + 1f) * 0.5f);
            return GeneratedFatManMeshFactory.Blend(
                0, (1f - top) * 0.18f,
                1, top * 0.55f,
                2, (1f - top) * 0.32f + front * 0.12f);
        }

        private static BoneWeight HemWeights(Vector2 p)
        {
            return GeneratedFatManMeshFactory.Blend(
                0, 0.20f, 1, 0.58f + Mathf.Abs(p.x) * 0.12f, 2, 0.22f);
        }

        private static BoneWeight PelvisWeights(Vector2 p)
        {
            return GeneratedFatManMeshFactory.Blend(
                0, 0.72f,
                1, Mathf.Clamp01(-p.x) * 0.28f,
                2, Mathf.Clamp01(p.x) * 0.28f);
        }

        private static float TapDuration(int variant)
        {
            return variant == 1 ? 0.42f :
                   variant == 2 ? 0.46f : 0.34f;
        }

        private static float ActionDuration(CharacterRoutineAction value)
        {
            switch (value)
            {
                case CharacterRoutineAction.ShiftWeight: return 2.6f;
                case CharacterRoutineAction.LookAround: return 2.4f;
                case CharacterRoutineAction.Scratch: return 2.2f;
                case CharacterRoutineAction.Yawn: return 3.6f;
                case CharacterRoutineAction.Stretch: return 3.0f;
                case CharacterRoutineAction.Flex: return 2.3f;
                case CharacterRoutineAction.AdjustClothes: return 1.9f;
                case CharacterRoutineAction.WarmShoulders: return 2.5f;
                case CharacterRoutineAction.SitDown:
                case CharacterRoutineAction.StandUp: return 0.9f;
                default: return 3.2f;
            }
        }

        private void OnDestroy()
        {
            assets?.Dispose();
            assets = null;
        }

        private enum ViewKind { Front, Side, Back }
        private enum MouthState { Neutral, Open, Strain, Yawn }

        private sealed class ViewRig
        {
            public readonly Transform Root;
            public readonly ViewKind Kind;
            public readonly float PostureSign;
            public readonly List<BonePose> Bones = new();
            public readonly List<GameObject> EyesOpen = new();
            public readonly List<GameObject> EyesClosed = new();

            public BonePose RigRoot, Pelvis, Spine, Chest, Neck, Head;
            public BonePose Chin, Belly, ShirtHem;
            public BonePose ClavicleL, ClavicleR;
            public BonePose UpperArmL, UpperArmR, ForearmL, ForearmR;
            public BonePose HandL, HandR, ThighL, ThighR;
            public BonePose ShinL, ShinR, FootL, FootR;
            public GameObject MouthNeutral, MouthOpen, MouthStrain, MouthYawn;

            public ViewRig(Transform root, ViewKind kind, float postureSign)
            {
                Root = root;
                Kind = kind;
                PostureSign = postureSign;
            }

            public BonePose AddBone(
                Transform parent, string name, Vector2 position)
            {
                GameObject value = new GameObject(name);
                value.transform.SetParent(parent, false);
                value.transform.localPosition =
                    new Vector3(position.x, position.y, 0f);
                BonePose pose = new BonePose(value.transform);
                Bones.Add(pose);
                return pose;
            }

            public void ResetPose()
            {
                for (int i = 0; i < Bones.Count; i++) Bones[i].Reset();
            }

            public void SetFace(bool closed, MouthState mouth)
            {
                for (int i = 0; i < EyesOpen.Count; i++)
                    EyesOpen[i]?.SetActive(!closed);
                for (int i = 0; i < EyesClosed.Count; i++)
                    EyesClosed[i]?.SetActive(closed);
                MouthNeutral?.SetActive(mouth == MouthState.Neutral);
                MouthOpen?.SetActive(mouth == MouthState.Open);
                MouthStrain?.SetActive(mouth == MouthState.Strain);
                MouthYawn?.SetActive(mouth == MouthState.Yawn);
            }
        }

        private sealed class BonePose
        {
            public readonly Transform T;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public float Angle
            {
                get
                {
                    float value = T.localEulerAngles.z;
                    return value > 180f ? value - 360f : value;
                }
            }

            public BonePose(Transform transform)
            {
                T = transform;
                position = T.localPosition;
                rotation = T.localRotation;
                scale = T.localScale;
            }

            public void Reset()
            {
                T.localPosition = position;
                T.localRotation = rotation;
                T.localScale = scale;
            }

            public void Move(Vector3 delta) => T.localPosition += delta;
            public void Rotate(float degrees) =>
                T.localRotation *= Quaternion.Euler(0f, 0f, degrees);
            public void Scale(Vector3 factor) =>
                T.localScale = Vector3.Scale(T.localScale, factor);
        }
    }
}
