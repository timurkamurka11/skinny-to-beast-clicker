using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    public static class Patch4AnimationLibraryBuilder
    {
        public const string AnimationRoot = "Assets/GameWorkPatch4/Animations";
        public const string ControllerPath =
            AnimationRoot + "/FatMan_Patch4.controller";

        private const string Visual = "Patch4VisualRoot/Root/CharacterRoot";
        private const string Pelvis = Visual + "/Pelvis";
        private const string SpineLower = Pelvis + "/SpineLower";
        private const string SpineUpper = SpineLower + "/SpineUpper";
        private const string Neck = SpineUpper + "/Neck";
        private const string Head = Neck + "/Head";
        private const string BellyBase = SpineLower + "/BellyBase";
        private const string BellyTip = BellyBase + "/BellyTip";
        private const string ClavicleL = SpineUpper + "/ClavicleL";
        private const string UpperArmL = ClavicleL + "/UpperArmL";
        private const string ForearmL = UpperArmL + "/ForearmL";
        private const string HandL = ForearmL + "/HandL";
        private const string ClavicleR = SpineUpper + "/ClavicleR";
        private const string UpperArmR = ClavicleR + "/UpperArmR";
        private const string ForearmR = UpperArmR + "/ForearmR";
        private const string HandR = ForearmR + "/HandR";
        private const string ThighL = Pelvis + "/ThighL";
        private const string ShinL = ThighL + "/ShinL";
        private const string FootL = ShinL + "/FootL";
        private const string ThighR = Pelvis + "/ThighR";
        private const string ShinR = ThighR + "/ShinR";
        private const string FootR = ShinR + "/FootR";
        [MenuItem("Tools/GameWork/Patch 4.0/Animation/Rebuild Library")]
        public static void RebuildLibrary()
        {
            EnsureFolder(AnimationRoot);

            Dictionary<string, AnimationClip> clips = new();
            clips.Add("FatMan_Idle_Breathe", BuildIdleBreathe());
            clips.Add("FatMan_Idle_ShiftWeight", BuildShiftWeight());
            clips.Add("FatMan_Blink_Random", BuildBlink());
            clips.Add("FatMan_LookAround", BuildLookAround());
            clips.Add("FatMan_TapReact_01", BuildTapReact01());
            clips.Add("FatMan_TapReact_02", BuildTapReact02());
            clips.Add("FatMan_Walk_InRoom", BuildWalk());
            clips.Add("FatMan_Turn", BuildTurn());
            clips.Add("FatMan_SitOrLean", BuildSitOrLean());
            clips.Add("FatMan_UpgradeReact", BuildUpgrade());

            BuildController(clips);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Debug.Log("Patch 4 animation library rebuilt: 10 clips + controller.");
        }

        private static AnimationClip BuildIdleBreathe()
        {
            AnimationClip clip = Prepare("FatMan_Idle_Breathe", true, 3.2f);
            SetScale(clip, SpineUpper, 'y', 3.2f, 1f, 1.035f, 1f);
            SetScale(clip, BellyTip, 'x', 3.2f, 1f, 1.05f, 1f);
            SetScale(clip, BellyTip, 'y', 3.2f, 1f, 1.028f, 1f);
            SetCurve(
                clip,
                Visual,
                "m_LocalPosition.y",
                new Keyframe(0f, 0f),
                new Keyframe(1.6f, 0.035f),
                new Keyframe(3.2f, 0f));
            SetRotation(clip, Head, 3.2f, 0f, 1.5f, 0f);
            SetRotation(clip, ClavicleL, 3.2f, 0f, -1.8f, 0f);
            SetRotation(clip, ClavicleR, 3.2f, 0f, 1.8f, 0f);
            return clip;
        }

        private static AnimationClip BuildShiftWeight()
        {
            const float duration = 3.2f;
            AnimationClip clip = Prepare(
                "FatMan_Idle_ShiftWeight",
                true,
                duration);
            SetCurve(
                clip,
                Visual,
                "m_LocalPosition.x",
                new Keyframe(0f, 0f),
                new Keyframe(0.8f, -0.12f),
                new Keyframe(1.6f, 0f),
                new Keyframe(2.4f, 0.12f),
                new Keyframe(duration, 0f));
            SetCurve(
                clip,
                Visual,
                "m_LocalPosition.y",
                new Keyframe(0f, 0f),
                new Keyframe(0.8f, -0.025f),
                new Keyframe(1.6f, 0f),
                new Keyframe(2.4f, -0.025f),
                new Keyframe(duration, 0f));
            SetAlternatingRotation(
                clip,
                Pelvis,
                duration,
                -5.5f,
                5.5f);
            SetAlternatingRotation(
                clip,
                SpineLower,
                duration,
                3.8f,
                -3.8f);
            SetAlternatingRotation(
                clip,
                ThighL,
                duration,
                3.5f,
                -3.5f);
            SetAlternatingRotation(
                clip,
                ThighR,
                duration,
                -3.5f,
                3.5f);
            SetAlternatingRotation(
                clip,
                UpperArmL,
                duration,
                -4f,
                4f);
            SetAlternatingRotation(
                clip,
                UpperArmR,
                duration,
                4f,
                -4f);
            return clip;
        }

        private static AnimationClip BuildBlink()
        {
            // Patch4FaceController performs the painted open-eye/lid swap.
            // Scaling Eye bones as well applies the blink twice and tears the
            // full-canvas face away from the head during room review.
            return Prepare("FatMan_Blink_Random", false, 0.18f);
        }

        private static AnimationClip BuildLookAround()
        {
            const float duration = 3f;
            AnimationClip clip = Prepare(
                "FatMan_LookAround",
                true,
                duration);
            SetAlternatingRotation(
                clip,
                Head,
                duration,
                -10f,
                10f);
            SetAlternatingRotation(
                clip,
                SpineUpper,
                duration,
                3f,
                -3f);
            return clip;
        }

        private static AnimationClip BuildTapReact01()
        {
            const float duration = 0.65f;
            AnimationClip clip = Prepare(
                "FatMan_TapReact_01",
                false,
                duration);
            SetCurve(
                clip,
                Visual,
                "m_LocalPosition.y",
                new Keyframe(0f, 0f),
                new Keyframe(0.14f, -0.1f),
                new Keyframe(0.34f, 0.04f),
                new Keyframe(duration, 0f));
            SetCurve(
                clip,
                SpineLower,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(0.14f, -9f),
                new Keyframe(0.34f, 4f),
                new Keyframe(duration, 0f));
            SetCurve(
                clip,
                BellyTip,
                "m_LocalScale.x",
                new Keyframe(0f, 1f),
                new Keyframe(0.12f, 1.018f),
                new Keyframe(0.32f, 0.992f),
                new Keyframe(duration, 1f));
            SetCurve(
                clip,
                BellyTip,
                "m_LocalScale.y",
                new Keyframe(0f, 1f),
                new Keyframe(0.12f, 0.982f),
                new Keyframe(0.32f, 1.012f),
                new Keyframe(duration, 1f));
            SetReactionRotation(
                clip,
                UpperArmL,
                duration,
                -14f,
                5f);
            SetReactionRotation(
                clip,
                UpperArmR,
                duration,
                14f,
                -5f);
            SetReactionRotation(
                clip,
                ForearmL,
                duration,
                18f,
                -6f);
            SetReactionRotation(
                clip,
                ForearmR,
                duration,
                -18f,
                6f);
            SetReactionRotation(
                clip,
                HandL,
                duration,
                -7f,
                3f);
            SetReactionRotation(
                clip,
                HandR,
                duration,
                7f,
                -3f);
            SetReactionRotation(
                clip,
                Head,
                duration,
                5f,
                -2f);
            return clip;
        }

        private static AnimationClip BuildTapReact02()
        {
            const float duration = 0.72f;
            AnimationClip clip = Prepare(
                "FatMan_TapReact_02",
                false,
                duration);
            SetCurve(
                clip,
                Visual,
                "m_LocalPosition.x",
                new Keyframe(0f, 0f),
                new Keyframe(0.16f, 0.07f),
                new Keyframe(0.38f, -0.03f),
                new Keyframe(duration, 0f));
            SetCurve(
                clip,
                Head,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(0.16f, 12f),
                new Keyframe(0.38f, -5f),
                new Keyframe(duration, 0f));
            SetReactionRotation(
                clip,
                UpperArmL,
                duration,
                -18f,
                6f);
            SetReactionRotation(
                clip,
                UpperArmR,
                duration,
                18f,
                -6f);
            SetReactionRotation(
                clip,
                SpineUpper,
                duration,
                -7f,
                3f);
            SetReactionRotation(
                clip,
                ForearmL,
                duration,
                24f,
                -8f);
            SetReactionRotation(
                clip,
                ForearmR,
                duration,
                -24f,
                8f);
            SetReactionRotation(
                clip,
                HandL,
                duration,
                -9f,
                3f);
            SetReactionRotation(
                clip,
                HandR,
                duration,
                9f,
                -3f);
            return clip;
        }

        private static AnimationClip BuildWalk()
        {
            // v18: do not synthesize a walk from two peaks plus interpolation.
            // Author all eight classical gait phases explicitly so the heavy
            // front-facing character has contact, loading, passing and lift on
            // each side. Hip/shoulder bind anchors remain fixed; only articulated
            // joints rotate, preventing the rubber/vacuum deformation seen in
            // earlier reviews.
            const float duration = 1.6f;
            AnimationClip clip = Prepare(
                "FatMan_Walk_InRoom",
                true,
                duration);

            SetEightPhasePosition(
                clip,
                Visual,
                "m_LocalPosition.y",
                duration,
                0f,
                -0.010f,
                0.030f,
                0.012f,
                0f,
                -0.010f,
                0.030f,
                0.012f);

            // Balance stays deliberately small. The gait must read from the
            // limbs, not from rocking the entire painted body.
            SetEightPhaseRotation(
                clip,
                Pelvis,
                duration,
                0f, -0.25f, -0.5f, -0.25f,
                0f, 0.25f, 0.5f, 0.25f);
            SetEightPhaseRotation(
                clip,
                SpineLower,
                duration,
                0f, 0.12f, 0.25f, 0.12f,
                0f, -0.12f, -0.25f, -0.12f);
            SetEightPhaseRotation(
                clip,
                SpineUpper,
                duration,
                0f, -0.45f, -1.1f, -0.55f,
                0f, 0.45f, 1.1f, 0.55f);
            SetEightPhaseRotation(
                clip,
                Head,
                duration,
                0f, 0.2f, 0.45f, 0.2f,
                0f, -0.2f, -0.45f, -0.2f);

            // Left step: contact -> down -> passing -> up.
            SetEightPhaseRotation(
                clip,
                ThighL,
                duration,
                -10f, -15f, -22f, -13f,
                5f, 9f, 13f, 3f);
            SetEightPhaseRotation(
                clip,
                ShinL,
                duration,
                8f, 18f, 30f, 21f,
                3f, -1f, -6f, 1f);
            SetEightPhaseRotation(
                clip,
                FootL,
                duration,
                -3f, -7f, -13f, -7f,
                2f, 4f, 6f, 2f);

            // Right side is phase-shifted by half a cycle. Matching raw thigh
            // signs are intentional because the authored right chain is mirrored.
            SetEightPhaseRotation(
                clip,
                ThighR,
                duration,
                -5f, -8f, -11f, 3f,
                10f, 15f, 22f, 13f);
            SetEightPhaseRotation(
                clip,
                ShinR,
                duration,
                -6f, -1f, 4f, 1f,
                8f, 18f, -30f, -20f);
            SetEightPhaseRotation(
                clip,
                FootR,
                duration,
                -2f, -3f, -5f, -1f,
                3f, 7f, 13f, 7f);

            // Arms counter-swing against their same-side legs. Elbow and hand
            // follow-through prevents the old rigid pendulum look.
            SetEightPhaseRotation(
                clip,
                UpperArmL,
                duration,
                6f, 12f, 18f, 11f,
                0f, -7f, -14f, -6f);
            SetEightPhaseRotation(
                clip,
                ForearmL,
                duration,
                -4f, -8f, -13f, -8f,
                0f, 4f, 8f, 3f);
            SetEightPhaseRotation(
                clip,
                HandL,
                duration,
                1f, 3f, 5f, 3f,
                0f, -2f, -4f, -2f);

            SetEightPhaseRotation(
                clip,
                UpperArmR,
                duration,
                3f, 7f, 14f, 7f,
                0f, -12f, -18f, -11f);
            SetEightPhaseRotation(
                clip,
                ForearmR,
                duration,
                -2f, -4f, -7f, -3f,
                0f, 8f, 13f, 8f);
            SetEightPhaseRotation(
                clip,
                HandR,
                duration,
                1f, 2f, 4f, 2f,
                0f, -3f, -5f, -3f);

            return clip;
        }

        private static AnimationClip BuildTurn()
        {
            const float duration = 0.72f;
            AnimationClip clip = Prepare(
                "FatMan_Turn",
                false,
                duration);
            SetCurve(
                clip,
                Visual,
                "m_LocalScale.x",
                new Keyframe(0f, 1f),
                new Keyframe(0.2f, 0.94f),
                new Keyframe(0.42f, 1.02f),
                new Keyframe(duration, 1f));
            SetCurve(
                clip,
                Visual,
                "m_LocalScale.y",
                new Keyframe(0f, 1f),
                new Keyframe(0.2f, 1.015f),
                new Keyframe(0.42f, 0.99f),
                new Keyframe(duration, 1f));
            SetCurve(
                clip,
                Visual,
                "m_LocalPosition.x",
                new Keyframe(0f, 0f),
                new Keyframe(0.2f, -0.08f),
                new Keyframe(0.42f, 0.12f),
                new Keyframe(duration, 0f));
            SetReactionRotation(
                clip,
                Pelvis,
                duration,
                -9f,
                8f);
            SetReactionRotation(
                clip,
                SpineUpper,
                duration,
                7f,
                -6f);
            SetReactionRotation(
                clip,
                Head,
                duration,
                14f,
                -10f);
            SetReactionRotation(
                clip,
                UpperArmL,
                duration,
                -10f,
                7f);
            SetReactionRotation(
                clip,
                UpperArmR,
                duration,
                10f,
                -7f);
            return clip;
        }

        private static AnimationClip BuildSitOrLean()
        {
            const float duration = 1.15f;
            AnimationClip clip = Prepare(
                "FatMan_SitOrLean",
                false,
                duration);
            SetCurve(
                clip,
                Visual,
                "m_LocalPosition.y",
                new Keyframe(0f, 0f),
                new Keyframe(0.58f, -0.32f),
                new Keyframe(duration, -0.32f));
            SetCurve(
                clip,
                Pelvis,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(0.58f, -10f),
                new Keyframe(duration, -10f));
            SetCurve(
                clip,
                SpineLower,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(0.58f, 14f),
                new Keyframe(duration, 14f));
            SetCurve(
                clip,
                ThighL,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(0.58f, -25f),
                new Keyframe(duration, -25f));
            SetCurve(
                clip,
                ThighR,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(0.58f, 25f),
                new Keyframe(duration, 25f));
            SetCurve(
                clip,
                ShinL,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(0.58f, 17f),
                new Keyframe(duration, 17f));
            SetCurve(
                clip,
                ShinR,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(0.58f, -17f),
                new Keyframe(duration, -17f));
            return clip;
        }

        private static AnimationClip BuildUpgrade()
        {
            const float duration = 1.05f;
            AnimationClip clip = Prepare(
                "FatMan_UpgradeReact",
                false,
                duration);
            SetCurve(
                clip,
                Visual,
                "m_LocalPosition.y",
                new Keyframe(0f, 0f),
                new Keyframe(0.2f, 0.18f),
                new Keyframe(0.48f, -0.06f),
                new Keyframe(duration, 0f));
            SetCurve(
                clip,
                Visual,
                "m_LocalScale.x",
                new Keyframe(0f, 1f),
                new Keyframe(0.2f, 1.015f),
                new Keyframe(0.48f, 0.992f),
                new Keyframe(duration, 1f));
            SetCurve(
                clip,
                Visual,
                "m_LocalScale.y",
                new Keyframe(0f, 1f),
                new Keyframe(0.2f, 1.015f),
                new Keyframe(0.48f, 0.992f),
                new Keyframe(duration, 1f));
            SetReactionRotation(
                clip,
                UpperArmL,
                duration,
                -22f,
                7f);
            SetReactionRotation(
                clip,
                UpperArmR,
                duration,
                22f,
                -7f);
            SetReactionRotation(
                clip,
                ForearmL,
                duration,
                -30f,
                10f);
            SetReactionRotation(
                clip,
                ForearmR,
                duration,
                30f,
                -10f);
            SetReactionRotation(
                clip,
                HandL,
                duration,
                12f,
                -4f);
            SetReactionRotation(
                clip,
                HandR,
                duration,
                -12f,
                4f);
            SetReactionRotation(
                clip,
                SpineUpper,
                duration,
                -7f,
                3f);
            SetReactionRotation(
                clip,
                Head,
                duration,
                -8f,
                4f);
            SetCurve(
                clip,
                BellyTip,
                "m_LocalScale.x",
                new Keyframe(0f, 1f),
                new Keyframe(0.2f, 1.045f),
                new Keyframe(0.48f, 0.97f),
                new Keyframe(duration, 1f));
            return clip;
        }

        private static void BuildController(
            IReadOnlyDictionary<string, AnimationClip> clips)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Look", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Shift", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Turn", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Sit", AnimatorControllerParameterType.Bool);
            controller.AddParameter("TapVariant", AnimatorControllerParameterType.Int);
            controller.AddParameter("Tap", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Blink", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Upgrade", AnimatorControllerParameterType.Trigger);

            AnimatorControllerLayer layer = controller.layers[0];
            AnimatorStateMachine machine = layer.stateMachine;
            // Animator full-path hashes start with the top-level state-machine
            // name. Keep it identical to the owning layer so runtime paths such
            // as "Base Layer.FatMan_Walk_InRoom" resolve through HasState and
            // Animator.Play.
            machine.name = layer.name;
            EditorUtility.SetDirty(machine);

            AnimatorState idle = AddState(machine, clips["FatMan_Idle_Breathe"]);
            AnimatorState shift = AddState(machine, clips["FatMan_Idle_ShiftWeight"]);
            AnimatorState blink = AddState(machine, clips["FatMan_Blink_Random"]);
            AnimatorState look = AddState(machine, clips["FatMan_LookAround"]);
            AnimatorState tap1 = AddState(machine, clips["FatMan_TapReact_01"]);
            AnimatorState tap2 = AddState(machine, clips["FatMan_TapReact_02"]);
            AnimatorState walk = AddState(machine, clips["FatMan_Walk_InRoom"]);
            AnimatorState turn = AddState(machine, clips["FatMan_Turn"]);
            AnimatorState sit = AddState(machine, clips["FatMan_SitOrLean"]);
            AnimatorState upgrade = AddState(machine, clips["FatMan_UpgradeReact"]);
            machine.defaultState = idle;

            AddFloatTransition(
                idle,
                walk,
                "Speed",
                0.1f,
                AnimatorConditionMode.Greater,
                0.18f);
            AddFloatReturn(walk, idle, "Speed", 0.1f, 0.18f);
            AddBoolTransition(idle, look, "Look", true, 0.12f);
            AddBoolTransition(look, idle, "Look", false, 0.12f);
            AddBoolTransition(idle, shift, "Shift", true, 0.12f);
            AddBoolTransition(shift, idle, "Shift", false, 0.12f);
            AddBoolTransition(idle, sit, "Sit", true, 0.16f);
            AddBoolTransition(sit, idle, "Sit", false, 0.16f);

            // One-shot gameplay reactions take priority over a facing pulse.
            // Blink is deliberately last because the runtime only schedules it
            // during free idle and a simultaneous tap/upgrade must win.
            AddAnyTrigger(machine, upgrade, "Upgrade");
            AddAnyTap(machine, tap1, 1);
            AddAnyTap(machine, tap2, 2);
            AddAnyBool(machine, turn, "Turn");
            AddAnyTrigger(machine, blink, "Blink", false);
            AddExitToContext(turn, idle, walk, shift, look, sit);
            AddExitToContext(blink, idle, walk, shift, look, sit);
            AddExitToContext(tap1, idle, walk, shift, look, sit);
            AddExitToContext(tap2, idle, walk, shift, look, sit);
            AddExitToContext(upgrade, idle, walk, shift, look, sit);

            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState AddState(
            AnimatorStateMachine machine,
            AnimationClip clip)
        {
            AnimatorState state = machine.AddState(clip.name);
            state.motion = clip;
            state.speed = Mathf.Max(
                0.01f,
                clip.length /
                Mathf.Max(
                    0.05f,
                    Patch4V23FullFramePresentation
                        .ResolvePlaybackDuration(clip.name)));
            state.writeDefaultValues = false;
            return state;
        }

        private static void AddBoolTransition(
            AnimatorState from,
            AnimatorState to,
            string parameter,
            bool value,
            float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.AddCondition(
                value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                parameter);
        }

        private static void AddFloatReturn(
            AnimatorState from,
            AnimatorState to,
            string parameter,
            float threshold,
            float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.AddCondition(
                AnimatorConditionMode.Less,
                threshold,
                parameter);
        }

        private static void AddFloatTransition(
            AnimatorState from,
            AnimatorState to,
            string parameter,
            float threshold,
            AnimatorConditionMode condition,
            float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.AddCondition(
                condition,
                threshold,
                parameter);
        }

        private static void AddAnyBool(
            AnimatorStateMachine machine,
            AnimatorState state,
            string parameter)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.12f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        }

        private static void AddAnyTap(
            AnimatorStateMachine machine,
            AnimatorState state,
            int variant)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.10f;
            transition.canTransitionToSelf = true;
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Tap");
            transition.AddCondition(
                AnimatorConditionMode.Equals,
                variant,
                "TapVariant");
        }

        private static void AddAnyTrigger(
            AnimatorStateMachine machine,
            AnimatorState state,
            string trigger,
            bool canTransitionToSelf = true)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.10f;
            transition.canTransitionToSelf = canTransitionToSelf;
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void AddExitToContext(
            AnimatorState from,
            AnimatorState idle,
            AnimatorState walk,
            AnimatorState shift,
            AnimatorState look,
            AnimatorState sit)
        {
            AddConditionalExit(
                from,
                walk,
                "Speed",
                AnimatorConditionMode.Greater,
                0.15f);
            AddConditionalExit(
                from,
                sit,
                "Sit",
                AnimatorConditionMode.If,
                0f);
            AddConditionalExit(
                from,
                look,
                "Look",
                AnimatorConditionMode.If,
                0f);
            AddConditionalExit(
                from,
                shift,
                "Shift",
                AnimatorConditionMode.If,
                0f);
            AddExitToIdle(from, idle, 0.12f);
        }

        private static void AddConditionalExit(
            AnimatorState from,
            AnimatorState destination,
            string parameter,
            AnimatorConditionMode mode,
            float threshold)
        {
            AnimatorStateTransition transition =
                from.AddTransition(destination);
            transition.hasExitTime = true;
            transition.exitTime = 0.94f;
            transition.hasFixedDuration = true;
            transition.duration = 0.12f;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void AddExitToIdle(
            AnimatorState from,
            AnimatorState idle,
            float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(idle);
            transition.hasExitTime = true;
            transition.exitTime = 0.94f;
            transition.hasFixedDuration = true;
            transition.duration = duration;
        }

        private static AnimationClip Prepare(
            string name,
            bool loop,
            float duration)
        {
            string path = AnimationRoot + "/" + name + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = name, frameRate = 30f };
                AssetDatabase.CreateAsset(clip, path);
            }

            foreach (EditorCurveBinding binding in
                     AnimationUtility.GetCurveBindings(clip))
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }

            SetLoop(clip, loop);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    string.Empty,
                    typeof(Transform),
                    "m_LocalScale.z"),
                AnimationCurve.Constant(0f, duration, 1f));
            return clip;
        }

        private static void SetScale(
            AnimationClip clip,
            string path,
            char axis,
            float duration,
            float start,
            float middle,
            float end)
        {
            SetCurve(
                clip,
                path,
                "m_LocalScale." + axis,
                new Keyframe(0f, start),
                new Keyframe(duration * 0.5f, middle),
                new Keyframe(duration, end));
        }

        private static void SetRotation(
            AnimationClip clip,
            string path,
            float duration,
            float start,
            float middle,
            float end)
        {
            SetCurve(
                clip,
                path,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, start),
                new Keyframe(duration * 0.5f, middle),
                new Keyframe(duration, end));
        }

        private static void SetAlternatingRotation(
            AnimationClip clip,
            string path,
            float duration,
            float first,
            float second)
        {
            SetCurve(
                clip,
                path,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(duration * 0.25f, first),
                new Keyframe(duration * 0.5f, 0f),
                new Keyframe(duration * 0.75f, second),
                new Keyframe(duration, 0f));
        }

        private static void SetCyclePosition(
            AnimationClip clip,
            string path,
            string property,
            float duration,
            float first,
            float second)
        {
            SetCurve(
                clip,
                path,
                property,
                new Keyframe(0f, 0f),
                new Keyframe(duration * 0.25f, first),
                new Keyframe(duration * 0.5f, 0f),
                new Keyframe(duration * 0.75f, second),
                new Keyframe(duration, 0f));
        }

        private static void SetFourPhaseRotation(
            AnimationClip clip,
            string path,
            float duration,
            float first,
            float second)
        {
            SetAlternatingRotation(
                clip,
                path,
                duration,
                first,
                second);
        }

        private static void SetEightPhaseRotation(
            AnimationClip clip,
            string path,
            float duration,
            float phase0,
            float phase1,
            float phase2,
            float phase3,
            float phase4,
            float phase5,
            float phase6,
            float phase7)
        {
            SetEightPhasePosition(
                clip,
                path,
                "localEulerAnglesRaw.z",
                duration,
                phase0,
                phase1,
                phase2,
                phase3,
                phase4,
                phase5,
                phase6,
                phase7);
        }

        private static void SetEightPhasePosition(
            AnimationClip clip,
            string path,
            string property,
            float duration,
            float phase0,
            float phase1,
            float phase2,
            float phase3,
            float phase4,
            float phase5,
            float phase6,
            float phase7)
        {
            float step = duration / 8f;
            SetCurve(
                clip,
                path,
                property,
                new Keyframe(0f, phase0),
                new Keyframe(step, phase1),
                new Keyframe(step * 2f, phase2),
                new Keyframe(step * 3f, phase3),
                new Keyframe(step * 4f, phase4),
                new Keyframe(step * 5f, phase5),
                new Keyframe(step * 6f, phase6),
                new Keyframe(step * 7f, phase7),
                new Keyframe(duration, phase0));
        }

        private static void SetReactionRotation(
            AnimationClip clip,
            string path,
            float duration,
            float impact,
            float rebound)
        {
            SetCurve(
                clip,
                path,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(duration * 0.22f, impact),
                new Keyframe(duration * 0.52f, rebound),
                new Keyframe(duration, 0f));
        }

        private static void SetCurve(
            AnimationClip clip,
            string path,
            string property,
            params Keyframe[] keys)
        {
            AnimationCurve curve = new(keys);
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve,
                    i,
                    AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(
                    curve,
                    i,
                    AnimationUtility.TangentMode.ClampedAuto);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    property),
                curve);
            EditorUtility.SetDirty(clip);
        }

        private static void SetLoop(AnimationClip clip, bool loop)
        {
            SerializedObject serialized = new(clip);
            SerializedProperty settings =
                serialized.FindProperty("m_AnimationClipSettings");
            SerializedProperty loopProperty =
                settings?.FindPropertyRelative("m_LoopTime");
            if (loopProperty != null)
            {
                loopProperty.boolValue = loop;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
