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
            const float duration = 0.96f;
            AnimationClip clip = Prepare(
                "FatMan_Walk_InRoom",
                true,
                duration);
            SetCyclePosition(
                clip,
                Visual,
                "m_LocalPosition.y",
                duration,
                0.045f,
                0.045f);
            SetAlternatingRotation(
                clip,
                Pelvis,
                duration,
                -0.6f,
                0.6f);
            SetAlternatingRotation(
                clip,
                SpineLower,
                duration,
                0.3f,
                -0.3f);

            // Keep every shoulder and hip at its authored bind anchor. Moving
            // those anchors translated whole texture regions and produced the
            // rubber/vacuum motion seen in the v16 human review. Locomotion is
            // instead a mirror-correct gait. The left and right bind chains
            // point in opposite X directions, so matching raw rotation signs
            // create opposing endpoint motion on screen. Giving the leading
            // limb the larger bend makes each planted/lifted step unambiguous.
            SetFourPhaseRotation(
                clip,
                ThighL,
                duration,
                -24f,
                14f);
            SetFourPhaseRotation(
                clip,
                ThighR,
                duration,
                -14f,
                24f);
            SetFourPhaseRotation(
                clip,
                ShinL,
                duration,
                34f,
                -6f);
            SetFourPhaseRotation(
                clip,
                ShinR,
                duration,
                6f,
                -34f);
            SetFourPhaseRotation(
                clip,
                FootL,
                duration,
                -16f,
                6f);
            SetFourPhaseRotation(
                clip,
                FootR,
                duration,
                -6f,
                16f);
            SetFourPhaseRotation(
                clip,
                UpperArmL,
                duration,
                24f,
                -16f);
            SetFourPhaseRotation(
                clip,
                UpperArmR,
                duration,
                16f,
                -24f);
            SetFourPhaseRotation(
                clip,
                ForearmL,
                duration,
                -16f,
                8f);
            SetFourPhaseRotation(
                clip,
                ForearmR,
                duration,
                -8f,
                16f);
            SetFourPhaseRotation(
                clip,
                HandL,
                duration,
                6f,
                -4f);
            SetFourPhaseRotation(
                clip,
                HandR,
                duration,
                4f,
                -6f);
            SetAlternatingRotation(
                clip,
                SpineUpper,
                duration,
                1.5f,
                -1.5f);
            SetAlternatingRotation(
                clip,
                Head,
                duration,
                -0.8f,
                0.8f);
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
            controller.AddParameter("Turn", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Sit", AnimatorControllerParameterType.Bool);
            controller.AddParameter("TapVariant", AnimatorControllerParameterType.Int);
            controller.AddParameter("Tap", AnimatorControllerParameterType.Trigger);
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
            // Random blink timing is driven by Patch4FaceController. Keep the
            // authored clip referenced by the controller for contract review.
            AddState(machine, clips["FatMan_Blink_Random"]);
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
                0.15f);
            AddFloatReturn(walk, idle, "Speed", 0.1f);
            AddBoolTransition(idle, look, "Look", true, 0.12f);
            AddBoolTransition(look, idle, "Look", false, 0.12f);
            AddBoolTransition(idle, sit, "Sit", true, 0.16f);
            AddBoolTransition(sit, idle, "Sit", false, 0.16f);

            AnimatorStateTransition shiftTransition = idle.AddTransition(shift);
            shiftTransition.hasExitTime = true;
            shiftTransition.exitTime = 1f;
            shiftTransition.duration = 0.2f;
            AnimatorStateTransition shiftReturn = shift.AddTransition(idle);
            shiftReturn.hasExitTime = true;
            shiftReturn.exitTime = 1f;
            shiftReturn.duration = 0.2f;

            AddAnyBool(machine, turn, "Turn");
            AddAnyTap(machine, tap1, 1);
            AddAnyTap(machine, tap2, 2);
            AddAnyTrigger(machine, upgrade, "Upgrade");
            AddExitToIdle(turn, idle, 0.12f);
            AddExitToIdle(tap1, idle, 0.1f);
            AddExitToIdle(tap2, idle, 0.1f);
            AddExitToIdle(upgrade, idle, 0.12f);

            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState AddState(
            AnimatorStateMachine machine,
            AnimationClip clip)
        {
            AnimatorState state = machine.AddState(clip.name);
            state.motion = clip;
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
            float threshold)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.15f;
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
            transition.duration = 0.08f;
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
            transition.duration = 0.05f;
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
            string trigger)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(state);
            transition.hasExitTime = false;
            transition.duration = 0.05f;
            transition.canTransitionToSelf = true;
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        private static void AddExitToIdle(
            AnimatorState from,
            AnimatorState idle,
            float duration)
        {
            AnimatorStateTransition transition = from.AddTransition(idle);
            transition.hasExitTime = true;
            transition.exitTime = 0.95f;
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
