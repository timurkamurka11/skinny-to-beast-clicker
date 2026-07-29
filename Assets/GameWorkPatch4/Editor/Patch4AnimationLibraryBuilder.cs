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
        private const string ClavicleR = SpineUpper + "/ClavicleR";
        private const string UpperArmR = ClavicleR + "/UpperArmR";
        private const string ForearmR = UpperArmR + "/ForearmR";
        private const string ThighL = Pelvis + "/ThighL";
        private const string ShinL = ThighL + "/ShinL";
        private const string ThighR = Pelvis + "/ThighR";
        private const string ShinR = ThighR + "/ShinR";
        private const string EyeL = Head + "/EyeL";
        private const string EyeR = Head + "/EyeR";

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
            SetScale(clip, SpineUpper, 'y', 3.2f, 1f, 1.018f, 1f);
            SetScale(clip, BellyTip, 'x', 3.2f, 1f, 1.025f, 1f);
            SetScale(clip, BellyTip, 'y', 3.2f, 1f, 1.018f, 1f);
            SetRotation(clip, Head, 3.2f, -0.6f, 0.8f, -0.6f);
            return clip;
        }

        private static AnimationClip BuildShiftWeight()
        {
            AnimationClip clip = Prepare("FatMan_Idle_ShiftWeight", true, 4f);
            SetRotation(clip, Pelvis, 4f, -2.4f, 2.4f, -2.4f);
            SetRotation(clip, SpineLower, 4f, 1.6f, -1.6f, 1.6f);
            SetRotation(clip, ThighL, 4f, 1.2f, -1.2f, 1.2f);
            SetRotation(clip, ThighR, 4f, -1.2f, 1.2f, -1.2f);
            return clip;
        }

        private static AnimationClip BuildBlink()
        {
            AnimationClip clip = Prepare("FatMan_Blink_Random", false, 0.18f);
            SetCurve(
                clip,
                EyeL,
                "m_LocalScale.y",
                new Keyframe(0f, 1f),
                new Keyframe(0.065f, 0.05f),
                new Keyframe(0.11f, 0.05f),
                new Keyframe(0.18f, 1f));
            SetCurve(
                clip,
                EyeR,
                "m_LocalScale.y",
                new Keyframe(0f, 1f),
                new Keyframe(0.065f, 0.05f),
                new Keyframe(0.11f, 0.05f),
                new Keyframe(0.18f, 1f));
            return clip;
        }

        private static AnimationClip BuildLookAround()
        {
            AnimationClip clip = Prepare("FatMan_LookAround", true, 2.4f);
            SetRotation(clip, Head, 2.4f, -5f, 5f, -5f);
            SetRotation(clip, EyeL, 2.4f, -3f, 3f, -3f);
            SetRotation(clip, EyeR, 2.4f, -3f, 3f, -3f);
            return clip;
        }

        private static AnimationClip BuildTapReact01()
        {
            AnimationClip clip = Prepare("FatMan_TapReact_01", false, 0.54f);
            SetCurve(
                clip,
                SpineLower,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(0.12f, -5.5f),
                new Keyframe(0.3f, 2.2f),
                new Keyframe(0.54f, 0f));
            SetCurve(
                clip,
                BellyTip,
                "m_LocalScale.x",
                new Keyframe(0f, 1f),
                new Keyframe(0.1f, 1.09f),
                new Keyframe(0.28f, 0.98f),
                new Keyframe(0.54f, 1f));
            SetCurve(
                clip,
                BellyTip,
                "m_LocalScale.y",
                new Keyframe(0f, 1f),
                new Keyframe(0.1f, 0.94f),
                new Keyframe(0.28f, 1.03f),
                new Keyframe(0.54f, 1f));
            return clip;
        }

        private static AnimationClip BuildTapReact02()
        {
            AnimationClip clip = Prepare("FatMan_TapReact_02", false, 0.62f);
            SetCurve(
                clip,
                Head,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(0.12f, 7f),
                new Keyframe(0.34f, -3f),
                new Keyframe(0.62f, 0f));
            SetCurve(
                clip,
                UpperArmL,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(0.14f, -10f),
                new Keyframe(0.62f, 0f));
            SetCurve(
                clip,
                UpperArmR,
                "localEulerAnglesRaw.z",
                new Keyframe(0f, 0f),
                new Keyframe(0.14f, 10f),
                new Keyframe(0.62f, 0f));
            return clip;
        }

        private static AnimationClip BuildWalk()
        {
            AnimationClip clip = Prepare("FatMan_Walk_InRoom", true, 0.9f);
            SetRotation(clip, ThighL, 0.9f, -14f, 14f, -14f);
            SetRotation(clip, ThighR, 0.9f, 14f, -14f, 14f);
            SetRotation(clip, ShinL, 0.9f, 8f, -12f, 8f);
            SetRotation(clip, ShinR, 0.9f, -12f, 8f, -12f);
            SetRotation(clip, UpperArmL, 0.9f, 8f, -8f, 8f);
            SetRotation(clip, UpperArmR, 0.9f, -8f, 8f, -8f);
            SetRotation(clip, SpineUpper, 0.9f, -1.5f, 1.5f, -1.5f);
            return clip;
        }

        private static AnimationClip BuildTurn()
        {
            AnimationClip clip = Prepare("FatMan_Turn", false, 0.38f);
            SetCurve(
                clip,
                Visual,
                "m_LocalScale.x",
                new Keyframe(0f, 1f),
                new Keyframe(0.18f, 0.12f),
                new Keyframe(0.38f, 1f));
            SetRotation(clip, Head, 0.38f, 0f, 6f, 0f);
            return clip;
        }

        private static AnimationClip BuildSitOrLean()
        {
            AnimationClip clip = Prepare("FatMan_SitOrLean", true, 1.6f);
            SetRotation(clip, Pelvis, 1.6f, -5f, -7f, -5f);
            SetRotation(clip, SpineLower, 1.6f, 7f, 10f, 7f);
            SetRotation(clip, ThighL, 1.6f, -18f, -21f, -18f);
            SetRotation(clip, ThighR, 1.6f, 18f, 21f, 18f);
            return clip;
        }

        private static AnimationClip BuildUpgrade()
        {
            AnimationClip clip = Prepare("FatMan_UpgradeReact", false, 0.95f);
            SetCurve(
                clip,
                Visual,
                "m_LocalScale.x",
                new Keyframe(0f, 1f),
                new Keyframe(0.18f, 1.1f),
                new Keyframe(0.4f, 0.97f),
                new Keyframe(0.95f, 1f));
            SetCurve(
                clip,
                Visual,
                "m_LocalScale.y",
                new Keyframe(0f, 1f),
                new Keyframe(0.18f, 1.1f),
                new Keyframe(0.4f, 0.97f),
                new Keyframe(0.95f, 1f));
            SetRotation(clip, UpperArmL, 0.95f, 0f, -28f, 0f);
            SetRotation(clip, UpperArmR, 0.95f, 0f, 28f, 0f);
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

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            machine.name = "Patch 4 Locomotion";

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

            AddBoolTransition(idle, walk, "Speed", true, 0.15f);
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
