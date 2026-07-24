using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SkinnyToBeast.Editor
{
    [InitializeOnLoad]
    internal static class LivingGameplayAnimatorAssetBuilder
    {
        private const string SessionKey = "SkinnyToBeast.LivingAnimatorBuilt.V1";
        private const string RootFolder = "Assets/Resources/UI/Gameplay/Living/Animations";
        private const string ControllerPath = RootFolder + "/LivingCharacter.controller";

        static LivingGameplayAnimatorAssetBuilder()
        {
            EditorApplication.delayCall += EnsureAssetsOnce;
        }

        [MenuItem("Tools/Skinny to Beast/Rebuild Living Gameplay Animator")]
        public static void RebuildFromMenu()
        {
            DeleteGeneratedAssets();
            BuildAssets();
        }

        private static void EnsureAssetsOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) == null)
            {
                BuildAssets();
            }
        }

        private static void BuildAssets()
        {
            EnsureFolder("Assets/Resources/UI/Gameplay/Living");
            EnsureFolder(RootFolder);

            AnimationClip idle = CreateIdleClip();
            AnimationClip tapA = CreateTapAClip();
            AnimationClip tapB = CreateTapBClip();
            AnimationClip rareLook = CreateRareLookClip();
            AnimationClip rareScratch = CreateRareScratchClip();
            AnimationClip upgrade = CreateUpgradeClip();
            AnimationClip stageChange = CreateStageChangeClip();

            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("TapA", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("TapB", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("RareLook", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("RareScratch", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Upgrade", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("StageChange", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            machine.name = "Living Character";
            AnimatorState idleState = AddState(machine, "Idle_Breathe", idle, new Vector3(260f, 70f));
            machine.defaultState = idleState;

            AddTriggeredState(
                machine,
                idleState,
                "TapReact_A",
                tapA,
                "TapA",
                new Vector3(520f, -20f),
                0.02f,
                true);
            AddTriggeredState(
                machine,
                idleState,
                "TapReact_B",
                tapB,
                "TapB",
                new Vector3(520f, 90f),
                0.02f,
                true);
            AddTriggeredState(
                machine,
                idleState,
                "Idle_LookDown",
                rareLook,
                "RareLook",
                new Vector3(20f, -70f),
                0.06f,
                false);
            AddTriggeredState(
                machine,
                idleState,
                "Idle_Scratch",
                rareScratch,
                "RareScratch",
                new Vector3(20f, 170f),
                0.06f,
                false);
            AddTriggeredState(
                machine,
                idleState,
                "UpgradeReact",
                upgrade,
                "Upgrade",
                new Vector3(760f, 40f),
                0.03f,
                true);
            AddTriggeredState(
                machine,
                idleState,
                "StageChange",
                stageChange,
                "StageChange",
                new Vector3(760f, 160f),
                0.02f,
                true);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Living gameplay Animator generated: Idle, Blink scheduler, Tap A/B, " +
                "rare idle, Upgrade and StageChange states are ready.");
        }

        private static AnimationClip CreateIdleClip()
        {
            AnimationClip clip = CreateClipAsset("Idle_Breathe", 2.4f, true);
            SetCurve(clip, "localPosition.y", Curve(
                0f, 0f,
                0.6f, 4.5f,
                1.2f, 0f,
                1.8f, -2.5f,
                2.4f, 0f));
            SetCurve(clip, "localScale.x", Curve(
                0f, 1f,
                0.6f, 0.996f,
                1.2f, 1f,
                1.8f, 1.003f,
                2.4f, 1f));
            SetCurve(clip, "localScale.y", Curve(
                0f, 1f,
                0.6f, 1.013f,
                1.2f, 1f,
                1.8f, 0.994f,
                2.4f, 1f));
            SetCurve(clip, "localEulerAnglesRaw.z", Curve(
                0f, -0.18f,
                1.2f, 0.18f,
                2.4f, -0.18f));
            return clip;
        }

        private static AnimationClip CreateTapAClip()
        {
            AnimationClip clip = CreateClipAsset("TapReact_A", 0.28f, false);
            SetCurve(clip, "localPosition.y", Curve(
                0f, 0f,
                0.07f, 18f,
                0.16f, -3f,
                0.28f, 0f));
            SetCurve(clip, "localScale.x", Curve(
                0f, 1f,
                0.07f, 1.045f,
                0.16f, 0.985f,
                0.28f, 1f));
            SetCurve(clip, "localScale.y", Curve(
                0f, 1f,
                0.07f, 0.95f,
                0.16f, 1.025f,
                0.28f, 1f));
            SetCurve(clip, "localEulerAnglesRaw.z", Curve(
                0f, 0f,
                0.07f, -1.6f,
                0.16f, 0.7f,
                0.28f, 0f));
            return clip;
        }

        private static AnimationClip CreateTapBClip()
        {
            AnimationClip clip = CreateClipAsset("TapReact_B", 0.34f, false);
            SetCurve(clip, "localPosition.x", Curve(
                0f, 0f,
                0.09f, 13f,
                0.2f, -5f,
                0.34f, 0f));
            SetCurve(clip, "localPosition.y", Curve(
                0f, 0f,
                0.09f, 12f,
                0.2f, -2f,
                0.34f, 0f));
            SetCurve(clip, "localScale.x", Curve(
                0f, 1f,
                0.09f, 1.03f,
                0.2f, 0.99f,
                0.34f, 1f));
            SetCurve(clip, "localScale.y", Curve(
                0f, 1f,
                0.09f, 0.965f,
                0.2f, 1.018f,
                0.34f, 1f));
            SetCurve(clip, "localEulerAnglesRaw.z", Curve(
                0f, 0f,
                0.09f, 2.1f,
                0.2f, -0.9f,
                0.34f, 0f));
            return clip;
        }

        private static AnimationClip CreateRareLookClip()
        {
            AnimationClip clip = CreateClipAsset("Idle_LookDown", 1.15f, false);
            SetCurve(clip, "localPosition.y", Curve(
                0f, 0f,
                0.35f, -8f,
                0.8f, -8f,
                1.15f, 0f));
            SetCurve(clip, "localScale.y", Curve(
                0f, 1f,
                0.35f, 0.985f,
                0.8f, 0.985f,
                1.15f, 1f));
            SetCurve(clip, "localEulerAnglesRaw.z", Curve(
                0f, 0f,
                0.35f, 0.7f,
                0.8f, 0.7f,
                1.15f, 0f));
            return clip;
        }

        private static AnimationClip CreateRareScratchClip()
        {
            AnimationClip clip = CreateClipAsset("Idle_Scratch", 1.4f, false);
            SetCurve(clip, "localScale.x", Curve(
                0f, 1f,
                0.25f, 1.012f,
                0.46f, 0.992f,
                0.67f, 1.012f,
                0.88f, 0.992f,
                1.12f, 1.008f,
                1.4f, 1f));
            SetCurve(clip, "localEulerAnglesRaw.z", Curve(
                0f, 0f,
                0.35f, -0.55f,
                0.7f, 0.55f,
                1.05f, -0.35f,
                1.4f, 0f));
            return clip;
        }

        private static AnimationClip CreateUpgradeClip()
        {
            AnimationClip clip = CreateClipAsset("UpgradeReact", 0.9f, false);
            SetCurve(clip, "localPosition.y", Curve(
                0f, 0f,
                0.18f, 26f,
                0.42f, -4f,
                0.66f, 12f,
                0.9f, 0f));
            SetCurve(clip, "localScale.x", Curve(
                0f, 1f,
                0.18f, 1.065f,
                0.42f, 0.985f,
                0.66f, 1.025f,
                0.9f, 1f));
            SetCurve(clip, "localScale.y", Curve(
                0f, 1f,
                0.18f, 0.97f,
                0.42f, 1.035f,
                0.66f, 1.012f,
                0.9f, 1f));
            return clip;
        }

        private static AnimationClip CreateStageChangeClip()
        {
            AnimationClip clip = CreateClipAsset("StageChange", 1.1f, false);
            SetCurve(clip, "localPosition.y", Curve(
                0f, 0f,
                0.28f, -10f,
                0.55f, 34f,
                0.82f, 8f,
                1.1f, 0f));
            SetCurve(clip, "localScale.x", Curve(
                0f, 1f,
                0.28f, 0.88f,
                0.55f, 1.13f,
                0.82f, 1.035f,
                1.1f, 1f));
            SetCurve(clip, "localScale.y", Curve(
                0f, 1f,
                0.28f, 0.9f,
                0.55f, 1.1f,
                0.82f, 1.025f,
                1.1f, 1f));
            return clip;
        }

        private static AnimationClip CreateClipAsset(
            string name,
            float duration,
            bool loop)
        {
            string path = $"{RootFolder}/{name}.anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AnimationClip clip = new AnimationClip
            {
                name = name,
                frameRate = 60f
            };

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static AnimatorState AddState(
            AnimatorStateMachine machine,
            string name,
            Motion motion,
            Vector3 position)
        {
            AnimatorState state = machine.AddState(name, position);
            state.motion = motion;
            state.writeDefaultValues = true;
            return state;
        }

        private static void AddTriggeredState(
            AnimatorStateMachine machine,
            AnimatorState idle,
            string stateName,
            Motion motion,
            string trigger,
            Vector3 position,
            float transitionDuration,
            bool canRestart)
        {
            AnimatorState state = AddState(machine, stateName, motion, position);
            AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.duration = transitionDuration;
            enter.canTransitionToSelf = canRestart;
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);

            AnimatorStateTransition exit = state.AddTransition(idle);
            exit.hasExitTime = true;
            exit.exitTime = 0.96f;
            exit.duration = 0.07f;
        }

        private static void SetCurve(
            AnimationClip clip,
            string property,
            AnimationCurve curve)
        {
            clip.SetCurve(string.Empty, typeof(Transform), property, curve);
            EditorUtility.SetDirty(clip);
        }

        private static AnimationCurve Curve(params float[] timeValuePairs)
        {
            if (timeValuePairs == null || timeValuePairs.Length < 4 ||
                timeValuePairs.Length % 2 != 0)
            {
                throw new ArgumentException(
                    "Animation curves require pairs of time and value.");
            }

            Keyframe[] keys = new Keyframe[timeValuePairs.Length / 2];
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = new Keyframe(
                    timeValuePairs[i * 2],
                    timeValuePairs[i * 2 + 1]);
            }

            AnimationCurve curve = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve,
                    i,
                    AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(
                    curve,
                    i,
                    AnimationUtility.TangentMode.Auto);
            }

            return curve;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void DeleteGeneratedAssets()
        {
            if (AssetDatabase.IsValidFolder(RootFolder))
            {
                AssetDatabase.DeleteAsset(RootFolder);
            }

            SessionState.SetBool(SessionKey, false);
            AssetDatabase.Refresh();
        }
    }
}
