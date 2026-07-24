using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SkinnyToBeast.Editor
{
    [InitializeOnLoad]
    internal static class LivingGameplayAnimatorAssetBuilder
    {
        private const string SessionKey =
            "SkinnyToBeast.LivingAnimatorBuilt.Patch2";
        private const string RootFolder = "Assets/Resources/UI/Gameplay/Living/Animations";
        private const string ControllerPath = RootFolder + "/LivingCharacter.controller";

        static LivingGameplayAnimatorAssetBuilder()
        {
            EditorApplication.delayCall -= EnsureAssetsOnce;
            EditorApplication.delayCall += EnsureAssetsOnce;
        }

        [MenuItem("Tools/Skinny to Beast/Rebuild Patch 2 Character Animator")]
        public static void RebuildFromMenu()
        {
            DeleteGeneratedAssets();
            BuildAssets();
        }

        private static void EnsureAssetsOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall -= EnsureAssetsOnce;
                EditorApplication.delayCall += EnsureAssetsOnce;
                return;
            }

            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            try
            {
                AnimatorController controller =
                    AssetDatabase.LoadAssetAtPath<AnimatorController>(
                        ControllerPath);
                if (NeedsPatchTwoRebuild(controller))
                {
                    DeleteGeneratedAssets();
                    BuildAssets();
                }
            }
            catch (Exception exception)
            {
                SessionState.SetBool(SessionKey, false);
                Debug.LogError(
                    $"Could not generate Patch 2 character Animator: {exception}");
            }
        }

        private static bool NeedsPatchTwoRebuild(
            AnimatorController controller)
        {
            if (controller == null || controller.layers.Length != 4)
            {
                return true;
            }

            string[] required =
            {
                "Base",
                "UpperBody",
                "Face",
                "FullBodyAction"
            };
            for (int i = 0; i < required.Length; i++)
            {
                if (controller.layers[i].name != required[i])
                {
                    return true;
                }
            }

            return
                !ContainsStates(
                    controller.layers[0].stateMachine,
                    "Idle_Breathe",
                    "Idle_ShiftWeight",
                    "Walk_Front",
                    "Walk_Side",
                    "Walk_Back",
                    "SitLoop") ||
                !ContainsStates(
                    controller.layers[1].stateMachine,
                    "UpperBody_Idle",
                    "Idle_Scratch",
                    "Idle_Yawn",
                    "Idle_Stretch",
                    "Idle_Flex",
                    "Idle_AdjustClothes",
                    "Idle_WarmShoulders") ||
                !ContainsStates(
                    controller.layers[2].stateMachine,
                    "Face_Idle",
                    "Face_Blink",
                    "Face_Look",
                    "Face_Expression") ||
                !ContainsStates(
                    controller.layers[3].stateMachine,
                    "FullBody_Idle",
                    "SitDown",
                    "StandUp",
                    "TapLift_A",
                    "TapLift_B",
                    "TapLift_C",
                    "StageChange") ||
                !ContainsParameters(
                    controller,
                    "Speed",
                    "Facing",
                    "Sitting");
        }

        private static bool ContainsStates(
            AnimatorStateMachine machine,
            params string[] names)
        {
            if (machine == null)
            {
                return false;
            }

            ChildAnimatorState[] states = machine.states;
            for (int requiredIndex = 0;
                 requiredIndex < names.Length;
                 requiredIndex++)
            {
                bool found = false;
                for (int stateIndex = 0;
                     stateIndex < states.Length;
                     stateIndex++)
                {
                    if (states[stateIndex].state != null &&
                        states[stateIndex].state.name == names[requiredIndex])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsParameters(
            AnimatorController controller,
            params string[] names)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int requiredIndex = 0;
                 requiredIndex < names.Length;
                 requiredIndex++)
            {
                bool found = false;
                for (int parameterIndex = 0;
                     parameterIndex < parameters.Length;
                     parameterIndex++)
                {
                    if (parameters[parameterIndex].name ==
                        names[requiredIndex])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static void BuildAssets()
        {
            EnsureFolder("Assets/Resources/UI/Gameplay/Living");
            EnsureFolder(RootFolder);

            AnimationClip idle = CreateMarkerClip(
                "Idle_Breathe",
                2.4f,
                true);
            AnimationClip tapA = CreateMarkerClip(
                "TapReact_A",
                0.28f,
                false);
            AnimationClip tapB = CreateMarkerClip(
                "TapReact_B",
                0.34f,
                false);
            AnimationClip rareLook = CreateMarkerClip(
                "Idle_LookDown",
                1.15f,
                false);
            AnimationClip rareScratch = CreateMarkerClip(
                "Idle_Scratch",
                1.4f,
                false);
            AnimationClip upgrade = CreateMarkerClip(
                "UpgradeReact",
                0.9f,
                false);
            AnimationClip stageChange = CreateMarkerClip(
                "StageChange",
                1.1f,
                false);
            AnimationClip idleShift = CreateMarkerClip(
                "Idle_ShiftWeight",
                1.8f,
                true);
            AnimationClip idleLook = CreateMarkerClip(
                "Idle_LookAround",
                1.4f,
                false);
            AnimationClip idleYawn = CreateMarkerClip(
                "Idle_Yawn",
                1.8f,
                false);
            AnimationClip idleStretch = CreateMarkerClip(
                "Idle_Stretch",
                1.55f,
                false);
            AnimationClip idleFlex = CreateMarkerClip(
                "Idle_Flex",
                1.35f,
                false);
            AnimationClip adjustClothes = CreateMarkerClip(
                "Idle_AdjustClothes",
                1.25f,
                false);
            AnimationClip warmShoulders = CreateMarkerClip(
                "Idle_WarmShoulders",
                1.35f,
                false);
            AnimationClip walkFront = CreateMarkerClip(
                "Walk_Front",
                0.78f,
                true);
            AnimationClip walkSide = CreateMarkerClip(
                "Walk_Side",
                0.78f,
                true);
            AnimationClip walkBack = CreateMarkerClip(
                "Walk_Back",
                0.78f,
                true);
            AnimationClip sitDown = CreateMarkerClip(
                "SitDown",
                0.72f,
                false);
            AnimationClip sitLoop = CreateMarkerClip(
                "SitLoop",
                1.7f,
                true);
            AnimationClip standUp = CreateMarkerClip(
                "StandUp",
                0.68f,
                false);
            AnimationClip tapLiftA = CreateMarkerClip(
                "TapLift_A",
                0.52f,
                false);
            AnimationClip tapLiftB = CreateMarkerClip(
                "TapLift_B",
                0.52f,
                false);
            AnimationClip tapLiftC = CreateMarkerClip(
                "TapLift_C",
                0.52f,
                false);

            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("TapA", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("TapB", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("RareLook", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("RareScratch", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Upgrade", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("StageChange", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Facing", AnimatorControllerParameterType.Int);
            controller.AddParameter("Sitting", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            RenameBaseLayer(controller);
            machine.name = "Base";
            AnimatorState idleState = AddState(machine, "Idle_Breathe", idle, new Vector3(260f, 70f));
            machine.defaultState = idleState;
            AddState(machine, "Idle_ShiftWeight", idleShift, new Vector3(260f, 190f));
            AddState(machine, "Walk_Front", walkFront, new Vector3(20f, 300f));
            AddState(machine, "Walk_Side", walkSide, new Vector3(260f, 300f));
            AddState(machine, "Walk_Back", walkBack, new Vector3(500f, 300f));
            AddState(machine, "SitLoop", sitLoop, new Vector3(740f, 300f));

            AddTriggeredState(
                machine,
                idleState,
                "TapReact_A",
                tapA,
                "TapA",
                new Vector3(520f, -20f),
                0.02f,
                true);

            AnimatorStateMachine upperBody =
                AddLayer(controller, "UpperBody", 1f);
            AnimatorState upperIdle = AddState(
                upperBody,
                "UpperBody_Idle",
                CreateMarkerClip("UpperBody_Idle", 1f, true),
                new Vector3(250f, 60f));
            upperBody.defaultState = upperIdle;
            AddState(upperBody, "Idle_Scratch", rareScratch, new Vector3(20f, 180f));
            AddState(upperBody, "Idle_Yawn", idleYawn, new Vector3(180f, 180f));
            AddState(upperBody, "Idle_Stretch", idleStretch, new Vector3(340f, 180f));
            AddState(upperBody, "Idle_Flex", idleFlex, new Vector3(500f, 180f));
            AddState(upperBody, "Idle_AdjustClothes", adjustClothes, new Vector3(660f, 180f));
            AddState(upperBody, "Idle_WarmShoulders", warmShoulders, new Vector3(820f, 180f));

            AnimatorStateMachine face =
                AddLayer(controller, "Face", 1f);
            AnimatorState faceIdle = AddState(
                face,
                "Face_Idle",
                CreateMarkerClip("Face_Idle", 1f, true),
                new Vector3(250f, 60f));
            face.defaultState = faceIdle;
            AddState(face, "Face_Blink", CreateMarkerClip("Face_Blink", 0.12f, false), new Vector3(80f, 190f));
            AddState(face, "Face_Look", idleLook, new Vector3(250f, 190f));
            AddState(face, "Face_Expression", CreateMarkerClip("Face_Expression", 0.5f, false), new Vector3(420f, 190f));

            AnimatorStateMachine fullBody =
                AddLayer(controller, "FullBodyAction", 1f);
            AnimatorState fullIdle = AddState(
                fullBody,
                "FullBody_Idle",
                CreateMarkerClip("FullBody_Idle", 1f, true),
                new Vector3(250f, 60f));
            fullBody.defaultState = fullIdle;
            AddState(fullBody, "SitDown", sitDown, new Vector3(20f, 190f));
            AddState(fullBody, "StandUp", standUp, new Vector3(180f, 190f));
            AddState(fullBody, "TapLift_A", tapLiftA, new Vector3(340f, 190f));
            AddState(fullBody, "TapLift_B", tapLiftB, new Vector3(500f, 190f));
            AddState(fullBody, "TapLift_C", tapLiftC, new Vector3(660f, 190f));
            AddState(fullBody, "StageChange", stageChange, new Vector3(820f, 190f));
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
                "Patch 2 character Animator generated with Base, UpperBody, " +
                "Face and FullBodyAction layers.");
        }

        private static AnimationClip CreateMarkerClip(
            string name,
            float duration,
            bool loop)
        {
            AnimationClip clip = CreateClipAsset(name, duration, loop);
            SetCurve(
                clip,
                "localPosition.z",
                Curve(0f, 0f, duration, 0f));
            return clip;
        }

        private static void RenameBaseLayer(AnimatorController controller)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            layers[0].name = "Base";
            layers[0].defaultWeight = 1f;
            controller.layers = layers;
        }

        private static AnimatorStateMachine AddLayer(
            AnimatorController controller,
            string name,
            float weight)
        {
            controller.AddLayer(name);
            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer layer = layers[layers.Length - 1];
            layer.defaultWeight = weight;
            layers[layers.Length - 1] = layer;
            controller.layers = layers;
            return layer.stateMachine;
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

            AssetDatabase.Refresh();
        }
    }
}
