using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SkinnyToBeast.Editor
{
    [InitializeOnLoad]
    internal static class LivingGameplayAnimatorAssetBuilder
    {
        private const string SessionKey =
            "SkinnyToBeast.LivingAnimatorBuilt.Patch3.AssetTransactionV3";
        private const string RootFolder =
            "Assets/Resources/UI/Gameplay/Living/Animations";
        private const string ControllerPath =
            RootFolder + "/LivingCharacter.controller";
        private const ImportAssetOptions FolderSyncOptions =
            ImportAssetOptions.ForceSynchronousImport |
            ImportAssetOptions.ForceUpdate;

        private const string Root =
            "VisualRoot/Skeleton/Bone.Root";
        private const string Pelvis =
            Root + "/Bone.Pelvis";
        private const string Spine =
            Pelvis + "/Bone.Spine";
        private const string Chest =
            Spine + "/Bone.Chest";
        private const string Neck =
            Chest + "/Bone.Neck";
        private const string Head =
            Neck + "/Bone.Head";

        private const string ShoulderL =
            Chest + "/Bone.Shoulder.L";
        private const string UpperArmL =
            ShoulderL + "/Bone.UpperArm.L";
        private const string ForearmL =
            UpperArmL + "/Bone.Forearm.L";
        private const string HandL =
            ForearmL + "/Bone.Hand.L";
        private const string ShoulderR =
            Chest + "/Bone.Shoulder.R";
        private const string UpperArmR =
            ShoulderR + "/Bone.UpperArm.R";
        private const string ForearmR =
            UpperArmR + "/Bone.Forearm.R";
        private const string HandR =
            ForearmR + "/Bone.Hand.R";

        private const string ThighL =
            Pelvis + "/Bone.Thigh.L";
        private const string ShinL =
            ThighL + "/Bone.Shin.L";
        private const string FootL =
            ShinL + "/Bone.Foot.L";
        private const string ThighR =
            Pelvis + "/Bone.Thigh.R";
        private const string ShinR =
            ThighR + "/Bone.Shin.R";
        private const string FootR =
            ShinR + "/Bone.Foot.R";

        private const string EyelidL =
            Head + "/FaceRig/Eyelid.L";
        private const string EyelidR =
            Head + "/FaceRig/Eyelid.R";
        private const string PupilL =
            Head + "/FaceRig/Eye.L/Pupil.L";
        private const string PupilR =
            Head + "/FaceRig/Eye.R/Pupil.R";
        private const string Mouth =
            Head + "/FaceRig/Mouth.Open";

        private static readonly string[] NeutralBones =
        {
            Root,
            Pelvis,
            Spine,
            Chest,
            Neck,
            Head,
            ShoulderL,
            UpperArmL,
            ForearmL,
            HandL,
            ShoulderR,
            UpperArmR,
            ForearmR,
            HandR,
            ThighL,
            ShinL,
            FootL,
            ThighR,
            ShinR,
            FootR
        };

        private static readonly string[] RequiredMotionClips =
        {
            "Idle_Breathe",
            "Idle_ShiftWeight",
            "Idle_LookAround",
            "Idle_Scratch",
            "Idle_Yawn",
            "Idle_Stretch",
            "Idle_Flex",
            "Idle_AdjustClothes",
            "Idle_WarmShoulders",
            "Walk_Front",
            "Walk_Side",
            "Walk_Back",
            "SitDown",
            "SitLoop",
            "StandUp",
            "TapLift_A",
            "TapLift_B",
            "TapLift_C",
            "StageChange",
            "Entry_WalkToDoor",
            "Face_Blink",
            "Face_Look",
            "Face_Expression"
        };

        private static readonly string[] NeutralClips =
        {
            "UpperBody_Idle",
            "Face_Idle",
            "FullBody_Idle"
        };

        private static bool isBuildingAssets;

        static LivingGameplayAnimatorAssetBuilder()
        {
            EditorApplication.delayCall -= EnsureAssetsOnce;
            EditorApplication.delayCall += EnsureAssetsOnce;
        }

        [MenuItem("Tools/Skinny to Beast/Rebuild Patch 3 Skeletal Animator")]
        public static void RebuildFromMenu()
        {
            RebuildAssets();
        }

        public static void EnsureCurrentAssets()
        {
            if (isBuildingAssets)
            {
                return;
            }

            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);
            if (!NeedsPatchThreeRebuild(controller))
            {
                return;
            }

            RebuildAssets();
        }

        private static void RebuildAssets()
        {
            if (isBuildingAssets)
            {
                return;
            }

            isBuildingAssets = true;
            try
            {
                EnsureFolder(RootFolder);
                BuildAssets();
            }
            finally
            {
                isBuildingAssets = false;
            }
        }

        [InitializeOnEnterPlayMode]
        private static void EnsureBeforePlayMode(
            EnterPlayModeOptions options)
        {
            EnsureCurrentAssets();
        }

        private static void EnsureAssetsOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
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
                EnsureCurrentAssets();
            }
            catch (Exception exception)
            {
                SessionState.SetBool(SessionKey, false);
                Debug.LogError(
                    "Could not generate Patch 3 skeletal Animator: " +
                    exception);
            }
        }

        private static bool NeedsPatchThreeRebuild(
            AnimatorController controller)
        {
            if (controller == null ||
                controller.layers.Length != 4)
            {
                return true;
            }

            string[] layers =
            {
                "Base",
                "UpperBody",
                "Face",
                "FullBodyAction"
            };
            for (int i = 0; i < layers.Length; i++)
            {
                if (controller.layers[i].name != layers[i])
                {
                    return true;
                }
            }

            if (!ContainsExactlyOneState(
                    controller.layers[0].stateMachine,
                    "Idle_Breathe",
                    "Idle_ShiftWeight",
                    "Walk_Front",
                    "Walk_Side",
                    "Walk_Back",
                    "SitLoop",
                    "Entry_WalkToDoor") ||
                !ContainsExactlyOneState(
                    controller.layers[1].stateMachine,
                    "UpperBody_Idle",
                    "Idle_LookAround",
                    "Idle_Scratch",
                    "Idle_Yawn",
                    "Idle_Stretch",
                    "Idle_Flex",
                    "Idle_AdjustClothes",
                    "Idle_WarmShoulders") ||
                !ContainsExactlyOneState(
                    controller.layers[2].stateMachine,
                    "Face_Idle",
                    "Face_Blink",
                    "Face_Look",
                    "Face_Expression") ||
                !ContainsExactlyOneState(
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
                    "Sitting"))
            {
                return true;
            }

            for (int i = 0; i < RequiredMotionClips.Length; i++)
            {
                AnimationClip clip =
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        $"{RootFolder}/{RequiredMotionClips[i]}.anim");
                if (!HasRealMotionCurves(clip))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsExactlyOneState(
            AnimatorStateMachine machine,
            params string[] names)
        {
            if (machine == null)
            {
                return false;
            }

            ChildAnimatorState[] states = machine.states;
            for (int required = 0; required < names.Length; required++)
            {
                int matches = 0;
                for (int stateIndex = 0;
                     stateIndex < states.Length;
                     stateIndex++)
                {
                    if (states[stateIndex].state != null &&
                        states[stateIndex].state.name == names[required])
                    {
                        if (states[stateIndex].state.motion == null)
                        {
                            return false;
                        }

                        matches++;
                    }
                }

                if (matches != 1)
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
            AnimatorControllerParameter[] parameters =
                controller.parameters;
            for (int required = 0; required < names.Length; required++)
            {
                int matches = 0;
                for (int parameterIndex = 0;
                     parameterIndex < parameters.Length;
                     parameterIndex++)
                {
                    if (parameters[parameterIndex].name == names[required])
                    {
                        matches++;
                    }
                }

                if (matches != 1)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasRealMotionCurves(
            AnimationClip clip)
        {
            if (clip == null)
            {
                return false;
            }

            EditorCurveBinding[] bindings =
                AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length == 0)
            {
                return false;
            }

            bool realTarget = false;
            bool markerOnly = true;
            for (int i = 0; i < bindings.Length; i++)
            {
                realTarget |=
                    bindings[i].path.Contains(
                        "Bone.",
                        StringComparison.Ordinal) ||
                    bindings[i].path.Contains(
                        "FaceRig",
                        StringComparison.Ordinal);
                markerOnly &=
                    bindings[i].propertyName ==
                    "localPosition.z";
            }

            return realTarget && !markerOnly;
        }

        private static void BuildAssets()
        {
            EnsureFolder(RootFolder);

            Dictionary<string, AnimationClip> clips = new();
            Add(clips, CreateBaseClip("Idle_Breathe", 2.4f, true));
            Add(clips, CreateBaseClip("Idle_ShiftWeight", 1.8f, true));
            Add(clips, CreateBaseClip("Walk_Front", 0.78f, true));
            Add(clips, CreateBaseClip("Walk_Side", 0.78f, true));
            Add(clips, CreateBaseClip("Walk_Back", 0.78f, true));
            Add(clips, CreateBaseClip("SitLoop", 1.7f, true));

            Add(clips, CreateUpperClip("Idle_LookAround", 1.4f));
            Add(clips, CreateUpperClip("Idle_Scratch", 1.4f));
            Add(clips, CreateUpperClip("Idle_Yawn", 1.8f));
            Add(clips, CreateUpperClip("Idle_Stretch", 1.55f));
            Add(clips, CreateUpperClip("Idle_Flex", 1.35f));
            Add(clips, CreateUpperClip("Idle_AdjustClothes", 1.25f));
            Add(clips, CreateUpperClip("Idle_WarmShoulders", 1.35f));

            Add(clips, CreateFullBodyClip("SitDown", 0.72f));
            Add(clips, CreateFullBodyClip("StandUp", 0.68f));
            Add(clips, CreateFullBodyClip("TapLift_A", 0.52f));
            Add(clips, CreateFullBodyClip("TapLift_B", 0.52f));
            Add(clips, CreateFullBodyClip("TapLift_C", 0.52f));
            Add(clips, CreateFullBodyClip("StageChange", 0.82f));
            Add(clips, CreateBaseClip("Entry_WalkToDoor", 0.78f, true));

            Add(clips, CreateFaceClip("Face_Blink", 0.13f));
            Add(clips, CreateFaceClip("Face_Look", 0.8f));
            Add(clips, CreateFaceClip("Face_Expression", 0.5f));
            Add(clips, CreateNeutralClip("UpperBody_Idle", 1f));
            Add(clips, CreateNeutralClip("Face_Idle", 1f));
            Add(clips, CreateNeutralClip("FullBody_Idle", 1f));
            int expectedClipCount =
                RequiredMotionClips.Length + NeutralClips.Length;
            if (clips.Count != expectedClipCount)
            {
                throw new InvalidOperationException(
                    $"Expected {expectedClipCount} generated clips, " +
                    $"created {clips.Count}.");
            }

            EnsureFolder(RootFolder);
            ClearGeneratedAssetPath(ControllerPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(
                    ControllerPath);
            if (controller == null)
            {
                throw new InvalidOperationException(
                    "Unity did not create the Patch 3 Animator Controller " +
                    $"at '{ControllerPath}'.");
            }

            controller.AddParameter(
                "Speed",
                AnimatorControllerParameterType.Float);
            controller.AddParameter(
                "Facing",
                AnimatorControllerParameterType.Int);
            controller.AddParameter(
                "Sitting",
                AnimatorControllerParameterType.Bool);

            AnimatorControllerLayer[] initialLayers =
                controller.layers;
            initialLayers[0].name = "Base";
            initialLayers[0].defaultWeight = 1f;
            controller.layers = initialLayers;

            AnimatorStateMachine baseMachine =
                controller.layers[0].stateMachine;
            AddStates(
                baseMachine,
                clips,
                "Idle_Breathe",
                "Idle_ShiftWeight",
                "Walk_Front",
                "Walk_Side",
                "Walk_Back",
                "SitLoop",
                "Entry_WalkToDoor");
            baseMachine.defaultState =
                FindState(baseMachine, "Idle_Breathe");

            AnimatorStateMachine upperMachine =
                AddLayer(controller, "UpperBody", 0f);
            AddStates(
                upperMachine,
                clips,
                "UpperBody_Idle",
                "Idle_LookAround",
                "Idle_Scratch",
                "Idle_Yawn",
                "Idle_Stretch",
                "Idle_Flex",
                "Idle_AdjustClothes",
                "Idle_WarmShoulders");
            upperMachine.defaultState =
                FindState(upperMachine, "UpperBody_Idle");

            AnimatorStateMachine faceMachine =
                AddLayer(controller, "Face", 0f);
            AddStates(
                faceMachine,
                clips,
                "Face_Idle",
                "Face_Blink",
                "Face_Look",
                "Face_Expression");
            faceMachine.defaultState =
                FindState(faceMachine, "Face_Idle");

            AnimatorStateMachine fullMachine =
                AddLayer(controller, "FullBodyAction", 0f);
            AddStates(
                fullMachine,
                clips,
                "FullBody_Idle",
                "SitDown",
                "StandUp",
                "TapLift_A",
                "TapLift_B",
                "TapLift_C",
                "StageChange");
            fullMachine.defaultState =
                FindState(fullMachine, "FullBody_Idle");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport);
            AnimatorController persistedController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);
            ValidatePersistedAssets(persistedController);

            Debug.Log(
                "Patch 3 Animator generated: four layers and real curves " +
                "for 20 skeletal bones. No marker-only clips were created.");
        }

        private static AnimationClip CreateBaseClip(
            string name,
            float duration,
            bool loop)
        {
            AnimationClip clip =
                CreateClip(name, duration, loop);
            AddNeutralPose(clip, duration);

            switch (name)
            {
                case "Idle_Breathe":
                    SetScaleY(
                        clip,
                        Chest,
                        Curve(
                            0f, 1f,
                            0.6f, 1.035f,
                            1.2f, 1f,
                            1.8f, 0.988f,
                            2.4f, 1f));
                    SetPositionY(
                        clip,
                        Root,
                        Curve(
                            0f, 0f,
                            0.6f, 4f,
                            1.2f, 0f,
                            1.8f, -2f,
                            2.4f, 0f));
                    SetRotation(
                        clip,
                        Head,
                        Curve(0f, -0.8f, 1.2f, 0.8f, 2.4f, -0.8f));
                    break;
                case "Idle_ShiftWeight":
                    SetPositionX(
                        clip,
                        Root,
                        Curve(0f, -12f, 0.9f, 12f, 1.8f, -12f));
                    SetRotation(
                        clip,
                        Pelvis,
                        Curve(0f, -3f, 0.9f, 3f, 1.8f, -3f));
                    SetRotation(
                        clip,
                        Chest,
                        Curve(0f, 1.5f, 0.9f, -1.5f, 1.8f, 1.5f));
                    break;
                case "Walk_Side":
                    AddWalkCurves(clip, duration, 25f, 18f, 7f);
                    break;
                case "Walk_Back":
                    AddWalkCurves(clip, duration, 19f, 15f, 6f);
                    SetRotation(
                        clip,
                        Chest,
                        Curve(0f, -2f, duration * 0.5f, 2f, duration, -2f));
                    break;
                case "SitLoop":
                    SetRotation(
                        clip,
                        ThighL,
                        Curve(0f, -68f, duration, -68f));
                    SetRotation(
                        clip,
                        ThighR,
                        Curve(0f, 68f, duration, 68f));
                    SetRotation(
                        clip,
                        ShinL,
                        Curve(0f, 84f, duration, 84f));
                    SetRotation(
                        clip,
                        ShinR,
                        Curve(0f, -84f, duration, -84f));
                    SetPositionY(
                        clip,
                        Root,
                        Curve(0f, -112f, duration * 0.5f, -108f, duration, -112f));
                    break;
                default:
                    AddWalkCurves(clip, duration, 21f, 16f, 8f);
                    break;
            }

            return clip;
        }

        private static void AddWalkCurves(
            AnimationClip clip,
            float duration,
            float legAngle,
            float armAngle,
            float bob)
        {
            float half = duration * 0.5f;
            SetRotation(
                clip,
                ThighL,
                Curve(0f, -legAngle, half, legAngle, duration, -legAngle));
            SetRotation(
                clip,
                ThighR,
                Curve(0f, legAngle, half, -legAngle, duration, legAngle));
            SetRotation(
                clip,
                ShinL,
                Curve(0f, 7f, half * 0.5f, 30f, half, 5f, duration, 7f));
            SetRotation(
                clip,
                ShinR,
                Curve(0f, -5f, half, -7f, half * 1.5f, -30f, duration, -5f));
            SetRotation(
                clip,
                UpperArmL,
                Curve(0f, armAngle, half, -armAngle, duration, armAngle));
            SetRotation(
                clip,
                UpperArmR,
                Curve(0f, -armAngle, half, armAngle, duration, -armAngle));
            SetPositionY(
                clip,
                Root,
                Curve(0f, 0f, duration * 0.25f, bob, half, 0f, duration * 0.75f, bob, duration, 0f));
            SetRotation(
                clip,
                Pelvis,
                Curve(0f, -3f, half, 3f, duration, -3f));
        }

        private static AnimationClip CreateUpperClip(
            string name,
            float duration)
        {
            AnimationClip clip =
                CreateClip(name, duration, false);
            float inTime = duration * 0.22f;
            float holdTime = duration * 0.72f;

            switch (name)
            {
                case "Idle_LookAround":
                    SetRotation(
                        clip,
                        Head,
                        Curve(0f, 0f, inTime, -12f, duration * 0.5f, 12f, holdTime, -7f, duration, 0f));
                    SetRotation(
                        clip,
                        Neck,
                        Curve(0f, 0f, duration * 0.5f, 4f, duration, 0f));
                    break;
                case "Idle_Scratch":
                    ActionCurve(clip, UpperArmR, 0f, 82f, duration);
                    ActionCurve(clip, ForearmR, 0f, 118f, duration);
                    ActionCurve(clip, HandR, 0f, -28f, duration);
                    ActionCurve(clip, Head, 0f, -5f, duration);
                    break;
                case "Idle_Yawn":
                    ActionCurve(clip, UpperArmL, 0f, -142f, duration);
                    ActionCurve(clip, UpperArmR, 0f, 142f, duration);
                    ActionCurve(clip, ForearmL, 0f, -18f, duration);
                    ActionCurve(clip, ForearmR, 0f, 18f, duration);
                    break;
                case "Idle_Stretch":
                    ActionCurve(clip, UpperArmL, 0f, -158f, duration);
                    ActionCurve(clip, UpperArmR, 0f, 158f, duration);
                    ActionCurve(clip, Spine, 0f, 5f, duration);
                    break;
                case "Idle_Flex":
                    ActionCurve(clip, UpperArmL, 0f, -62f, duration);
                    ActionCurve(clip, UpperArmR, 0f, 62f, duration);
                    ActionCurve(clip, ForearmL, 0f, -112f, duration);
                    ActionCurve(clip, ForearmR, 0f, 112f, duration);
                    break;
                case "Idle_AdjustClothes":
                    ActionCurve(clip, UpperArmL, 0f, 42f, duration);
                    ActionCurve(clip, UpperArmR, 0f, -42f, duration);
                    ActionCurve(clip, ForearmL, 0f, -88f, duration);
                    ActionCurve(clip, ForearmR, 0f, 88f, duration);
                    break;
                default:
                    SetRotation(
                        clip,
                        ShoulderL,
                        Curve(0f, 0f, duration * 0.25f, -13f, duration * 0.5f, 13f, duration * 0.75f, -8f, duration, 0f));
                    SetRotation(
                        clip,
                        ShoulderR,
                        Curve(0f, 0f, duration * 0.25f, 13f, duration * 0.5f, -13f, duration * 0.75f, 8f, duration, 0f));
                    break;
            }

            return clip;
        }

        private static AnimationClip CreateFullBodyClip(
            string name,
            float duration)
        {
            AnimationClip clip =
                CreateClip(name, duration, false);
            if (name == "StageChange")
            {
                SetScaleX(
                    clip,
                    Root,
                    Curve(0f, 1f, duration * 0.35f, 0.90f, duration * 0.62f, 1.12f, duration, 1f));
                SetScaleY(
                    clip,
                    Root,
                    Curve(0f, 1f, duration * 0.35f, 0.92f, duration * 0.62f, 1.10f, duration, 1f));
                SetPositionY(
                    clip,
                    Root,
                    Curve(0f, 0f, duration * 0.35f, -12f, duration * 0.62f, 28f, duration, 0f));
                return clip;
            }

            if (name == "SitDown" || name == "StandUp")
            {
                bool standingUp = name == "StandUp";
                float start = standingUp ? 1f : 0f;
                float end = standingUp ? 0f : 1f;
                SetPositionY(
                    clip,
                    Root,
                    Curve(0f, Mathf.Lerp(0f, -112f, start), duration, Mathf.Lerp(0f, -112f, end)));
                SetRotation(
                    clip,
                    ThighL,
                    Curve(0f, Mathf.Lerp(0f, -68f, start), duration, Mathf.Lerp(0f, -68f, end)));
                SetRotation(
                    clip,
                    ThighR,
                    Curve(0f, Mathf.Lerp(0f, 68f, start), duration, Mathf.Lerp(0f, 68f, end)));
                SetRotation(
                    clip,
                    ShinL,
                    Curve(0f, Mathf.Lerp(0f, 84f, start), duration, Mathf.Lerp(0f, 84f, end)));
                SetRotation(
                    clip,
                    ShinR,
                    Curve(0f, Mathf.Lerp(0f, -84f, start), duration, Mathf.Lerp(0f, -84f, end)));
                return clip;
            }

            float arm = name == "TapLift_B" ? 72f : 58f;
            float forearm = name == "TapLift_C" ? 122f : 102f;
            ActionCurve(clip, UpperArmL, 0f, arm, duration);
            ActionCurve(clip, UpperArmR, 0f, -arm, duration);
            ActionCurve(clip, ForearmL, 0f, -forearm, duration);
            ActionCurve(clip, ForearmR, 0f, forearm, duration);
            SetPositionY(
                clip,
                Root,
                Curve(0f, 0f, duration * 0.28f, 17f, duration * 0.62f, -3f, duration, 0f));
            SetRotation(
                clip,
                Chest,
                Curve(0f, 0f, duration * 0.35f, name == "TapLift_B" ? 4f : -3f, duration, 0f));
            return clip;
        }

        private static AnimationClip CreateFaceClip(
            string name,
            float duration)
        {
            AnimationClip clip =
                CreateClip(name, duration, false);
            switch (name)
            {
                case "Face_Blink":
                    SetScaleY(
                        clip,
                        EyelidL,
                        Curve(0f, 0.035f, duration * 0.5f, 1f, duration, 0.035f));
                    SetScaleY(
                        clip,
                        EyelidR,
                        Curve(0f, 0.035f, duration * 0.5f, 1f, duration, 0.035f));
                    break;
                case "Face_Look":
                    SetPositionX(
                        clip,
                        PupilL,
                        Curve(0f, 0f, duration * 0.4f, -6f, duration * 0.7f, 6f, duration, 0f));
                    SetPositionX(
                        clip,
                        PupilR,
                        Curve(0f, 0f, duration * 0.4f, -6f, duration * 0.7f, 6f, duration, 0f));
                    break;
                default:
                    SetScaleY(
                        clip,
                        Mouth,
                        Curve(0f, 1f, duration * 0.5f, 1.8f, duration, 1f));
                    break;
            }

            return clip;
        }

        private static AnimationClip CreateNeutralClip(
            string name,
            float duration)
        {
            AnimationClip clip =
                CreateClip(name, duration, true);
            SetRotation(
                clip,
                Root,
                Curve(0f, 0f, duration, 0f));
            return clip;
        }

        private static void AddNeutralPose(
            AnimationClip clip,
            float duration)
        {
            for (int i = 0; i < NeutralBones.Length; i++)
            {
                SetRotation(
                    clip,
                    NeutralBones[i],
                    Curve(0f, 0f, duration, 0f));
            }

            SetPositionX(
                clip,
                Root,
                Curve(0f, 0f, duration, 0f));
            SetPositionY(
                clip,
                Root,
                Curve(0f, 0f, duration, 0f));
            SetScaleX(
                clip,
                Root,
                Curve(0f, 1f, duration, 1f));
            SetScaleY(
                clip,
                Root,
                Curve(0f, 1f, duration, 1f));
            SetScaleX(
                clip,
                Chest,
                Curve(0f, 1f, duration, 1f));
            SetScaleY(
                clip,
                Chest,
                Curve(0f, 1f, duration, 1f));
        }

        private static void ActionCurve(
            AnimationClip clip,
            string path,
            float rest,
            float action,
            float duration)
        {
            SetRotation(
                clip,
                path,
                Curve(
                    0f, rest,
                    duration * 0.22f, action,
                    duration * 0.72f, action,
                    duration, rest));
        }

        private static void Add(
            Dictionary<string, AnimationClip> clips,
            AnimationClip clip)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            if (clips.ContainsKey(clip.name))
            {
                throw new InvalidOperationException(
                    $"Duplicate generated animation clip: {clip.name}");
            }

            AnimationClip persistedClip =
                PersistClipAsset(clip);
            clips.Add(persistedClip.name, persistedClip);
        }

        private static void AddStates(
            AnimatorStateMachine machine,
            Dictionary<string, AnimationClip> clips,
            params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                AnimatorState state = machine.AddState(
                    names[i],
                    new Vector3(
                        120f + (i % 4) * 210f,
                        70f + (i / 4) * 130f));
                state.motion = clips[names[i]];
                state.writeDefaultValues = false;
            }
        }

        private static AnimatorState FindState(
            AnimatorStateMachine machine,
            string name)
        {
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null &&
                    states[i].state.name == name)
                {
                    return states[i].state;
                }
            }

            return null;
        }

        private static AnimatorStateMachine AddLayer(
            AnimatorController controller,
            string name,
            float weight)
        {
            controller.AddLayer(name);
            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorControllerLayer layer =
                layers[layers.Length - 1];
            layer.defaultWeight = weight;
            layers[layers.Length - 1] = layer;
            controller.layers = layers;
            return layer.stateMachine;
        }

        private static AnimationClip CreateClip(
            string name,
            float duration,
            bool loop)
        {
            if (duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration));
            }

            AnimationClip clip = new()
            {
                name = name,
                frameRate = 60f
            };
            AnimationClipSettings settings =
                AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(
                clip,
                settings);
            return clip;
        }

        private static AnimationClip PersistClipAsset(
            AnimationClip clip)
        {
            EnsureFolder(RootFolder);
            string path = $"{RootFolder}/{clip.name}.anim";
            string absolutePath = ToAbsoluteAssetPath(path);
            AnimationClip persistedClip =
                LoadGeneratedClipForUpdate(path, absolutePath);
            if (persistedClip != null)
            {
                // Generated clips are updated in place. Reusing the existing
                // native asset keeps its GUID stable and avoids depending on
                // DeleteAsset becoming physically visible in the same Unity
                // editor callback on Windows.
                EditorUtility.CopySerialized(
                    clip,
                    persistedClip);
                persistedClip.name = clip.name;
                EditorUtility.SetDirty(persistedClip);
                AssetDatabase.SaveAssetIfDirty(persistedClip);
                string persistedPath =
                    (AssetDatabase.GetAssetPath(persistedClip) ??
                     string.Empty)
                    .Replace('\\', '/');
                if (string.Equals(
                        persistedPath,
                        path,
                        StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(absolutePath))
                {
                    UnityEngine.Object.DestroyImmediate(clip);
                    return persistedClip;
                }

                // A cached object can outlive its deleted native file. If
                // saving it did not restore the file, discard that stale
                // association and create the already completed source clip
                // as a new native asset below.
                ClearGeneratedAssetPath(path);
            }

            // A non-AnimationClip main object, an orphaned .meta, or a
            // partially written native file cannot be updated safely. These
            // paths are generated exclusively by this builder, so clear only
            // this exact target before creating the replacement.
            ClearGeneratedAssetPath(path);

            // Build every curve before CreateAsset. LoadAssetAtPath only
            // exposes objects that are already visible in the Project view,
            // which is not guaranteed in the same callback that creates the
            // native asset. GetAssetPath checks the new object's association
            // immediately; the full load/curve validation happens after the
            // single synchronous save-and-import at the end of BuildAssets.
            AssetDatabase.CreateAsset(clip, path);
            string createdPath =
                (AssetDatabase.GetAssetPath(clip) ?? string.Empty)
                .Replace('\\', '/');
            if (!string.Equals(
                    createdPath,
                    path,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Unity did not associate animation clip '{clip.name}' " +
                    $"with '{path}'. Actual path: '{createdPath}'.");
            }

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            return clip;
        }

        private static AnimationClip LoadGeneratedClipForUpdate(
            string path,
            string absolutePath)
        {
            UnityEngine.Object mainAsset =
                AssetDatabase.LoadMainAssetAtPath(path);
            if (mainAsset is AnimationClip animationClip)
            {
                return animationClip;
            }

            // A previous interrupted CreateAsset can leave a valid .anim on
            // disk before it appears in the Project view. Import that exact
            // generated path synchronously and reuse it when possible.
            if (mainAsset == null && File.Exists(absolutePath))
            {
                AssetDatabase.ImportAsset(
                    path,
                    FolderSyncOptions);
                mainAsset =
                    AssetDatabase.LoadMainAssetAtPath(path);
                if (mainAsset is AnimationClip importedClip)
                {
                    return importedClip;
                }
            }

            return null;
        }

        private static void ValidatePersistedAssets(
            AnimatorController controller)
        {
            List<string> failures = new();
            if (controller == null)
            {
                failures.Add("missing LivingCharacter.controller");
            }

            for (int i = 0; i < RequiredMotionClips.Length; i++)
            {
                string name = RequiredMotionClips[i];
                AnimationClip clip =
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        $"{RootFolder}/{name}.anim");
                if (clip == null)
                {
                    failures.Add($"missing {name}.anim");
                }
                else if (!HasRealMotionCurves(clip))
                {
                    failures.Add($"{name}.anim has no skeletal curves");
                }
            }

            for (int i = 0; i < NeutralClips.Length; i++)
            {
                string name = NeutralClips[i];
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        $"{RootFolder}/{name}.anim") == null)
                {
                    failures.Add($"missing {name}.anim");
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Patch 3 Animator persistence validation failed: " +
                    string.Join(", ", failures));
            }

            if (NeedsPatchThreeRebuild(controller))
            {
                throw new InvalidOperationException(
                    "Patch 3 Animator assets persisted, but the controller " +
                    "layers, parameters, states, or motion links are invalid.");
            }
        }

        private static void SetRotation(
            AnimationClip clip,
            string path,
            AnimationCurve curve)
        {
            SetCurve(
                clip,
                path,
                "localEulerAnglesRaw.z",
                curve);
        }

        private static void SetPositionX(
            AnimationClip clip,
            string path,
            AnimationCurve curve)
        {
            SetCurve(clip, path, "localPosition.x", curve);
        }

        private static void SetPositionY(
            AnimationClip clip,
            string path,
            AnimationCurve curve)
        {
            SetCurve(clip, path, "localPosition.y", curve);
        }

        private static void SetScaleX(
            AnimationClip clip,
            string path,
            AnimationCurve curve)
        {
            SetCurve(clip, path, "localScale.x", curve);
        }

        private static void SetScaleY(
            AnimationClip clip,
            string path,
            AnimationCurve curve)
        {
            SetCurve(clip, path, "localScale.y", curve);
        }

        private static void SetCurve(
            AnimationClip clip,
            string path,
            string property,
            AnimationCurve curve)
        {
            clip.SetCurve(
                path,
                typeof(Transform),
                property,
                curve);
            EditorUtility.SetDirty(clip);
        }

        private static AnimationCurve Curve(
            params float[] timeValuePairs)
        {
            if (timeValuePairs == null ||
                timeValuePairs.Length < 4 ||
                timeValuePairs.Length % 2 != 0)
            {
                throw new ArgumentException(
                    "Animation curves require time/value pairs.");
            }

            Keyframe[] keys =
                new Keyframe[timeValuePairs.Length / 2];
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = new Keyframe(
                    timeValuePairs[i * 2],
                    timeValuePairs[i * 2 + 1]);
            }

            AnimationCurve curve = new(keys);
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
            if (string.IsNullOrWhiteSpace(path) ||
                !path.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Unity asset folder must be below Assets: '{path}'.",
                    nameof(path));
            }

            string normalizedPath =
                path.TrimEnd('/').Replace('\\', '/');
            string absolutePath =
                ToAbsoluteAssetPath(normalizedPath);
            bool existsOnDisk = Directory.Exists(absolutePath);
            bool existsInAssetDatabase =
                AssetDatabase.IsValidFolder(normalizedPath);
            if (existsOnDisk && existsInAssetDatabase)
            {
                return;
            }

            // AssetDatabase can temporarily report a deleted folder as valid
            // on Windows. The physical directory is therefore the source of
            // truth during recovery. Creating the final path in one operation
            // also restores every missing parent before Unity imports it.
            if (!existsOnDisk)
            {
                Directory.CreateDirectory(absolutePath);
            }

            if (!Directory.Exists(absolutePath))
            {
                throw new DirectoryNotFoundException(
                    "Could not create required Unity asset folder on disk: " +
                    normalizedPath);
            }

            // Refresh whenever either view was stale. This covers both
            // states: a folder known only to AssetDatabase and a folder known
            // only to the filesystem.
            AssetDatabase.Refresh(FolderSyncOptions);
            if (!AssetDatabase.IsValidFolder(normalizedPath))
            {
                throw new DirectoryNotFoundException(
                    "Unity did not register required asset folder after " +
                    $"synchronous recovery: {normalizedPath}. " +
                    $"Disk exists: {Directory.Exists(absolutePath)}.");
            }
        }

        private static string ToAbsoluteAssetPath(string assetPath)
        {
            DirectoryInfo projectDirectory =
                Directory.GetParent(Application.dataPath);
            if (projectDirectory == null)
            {
                throw new DirectoryNotFoundException(
                    "Could not resolve the Unity project root.");
            }

            return Path.Combine(
                projectDirectory.FullName,
                assetPath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
        }

        private static void ClearGeneratedAssetPath(string path)
        {
            string absolutePath = ToAbsoluteAssetPath(path);
            string metaPath = absolutePath + ".meta";
            bool knownToAssetDatabase =
                AssetDatabase.LoadMainAssetAtPath(path) != null;
            if (!knownToAssetDatabase &&
                !File.Exists(absolutePath) &&
                !File.Exists(metaPath))
            {
                return;
            }

            if (knownToAssetDatabase)
            {
                AssetDatabase.DeleteAsset(path);
            }

            // DeleteAsset can report success before the filesystem view is
            // synchronized. Always release Unity's cached handles and verify
            // the exact generated file and .meta physically as well.
            AssetDatabase.ReleaseCachedFileHandles();
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }

            AssetDatabase.Refresh(FolderSyncOptions);
            if (AssetDatabase.LoadMainAssetAtPath(path) != null ||
                File.Exists(absolutePath) ||
                File.Exists(metaPath))
            {
                throw new IOException(
                    $"Unity could not clear generated asset '{path}'.");
            }
        }
    }
}
