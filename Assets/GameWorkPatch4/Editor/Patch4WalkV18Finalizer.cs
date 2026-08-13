using System;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Final normalization pass for the continuous V33 walk clip. Only the loop
    /// seam returns to its authored first pose. The old half-cycle reset forced
    /// every bone to neutral exactly between left and right support, producing
    /// the visible hitch that remained even with smooth curve tangents.
    /// </summary>
    public static class Patch4WalkV18Finalizer
    {
        private const string WalkPath =
            "Assets/GameWorkPatch4/Animations/FatMan_Walk_InRoom.anim";
        private const string Visual =
            "Patch4VisualRoot/Root/CharacterRoot";
        private const string Pelvis = Visual + "/Pelvis";
        private const string SpineLower = Pelvis + "/SpineLower";
        private const string SpineUpper = SpineLower + "/SpineUpper";
        private const string Head = SpineUpper + "/Neck/Head";
        private const string ThighL = Pelvis + "/ThighL";
        private const string ShinL = ThighL + "/ShinL";
        private const string FootL = ShinL + "/FootL";
        private const string ThighR = Pelvis + "/ThighR";
        private const string ShinR = ThighR + "/ShinR";
        private const string FootR = ShinR + "/FootR";
        private const string UpperArmL =
            SpineUpper + "/ClavicleL/UpperArmL";
        private const string ForearmL = UpperArmL + "/ForearmL";
        private const string HandL = ForearmL + "/HandL";
        private const string UpperArmR =
            SpineUpper + "/ClavicleR/UpperArmR";
        private const string ForearmR = UpperArmR + "/ForearmR";
        private const string HandR = ForearmR + "/HandR";

        public static void Apply()
        {
            AnimationClip walk =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(WalkPath);
            if (walk == null)
            {
                throw new InvalidOperationException(
                    "Patch 4 v18 walk finalizer could not load " + WalkPath);
            }

            string[] rotationPaths =
            {
                Pelvis,
                SpineLower,
                SpineUpper,
                Head,
                ThighL,
                ShinL,
                FootL,
                ThighR,
                ShinR,
                FootR,
                UpperArmL,
                ForearmL,
                HandL,
                UpperArmR,
                ForearmR,
                HandR
            };

            for (int i = 0; i < rotationPaths.Length; i++)
            {
                NormalizeEightPhaseCurve(
                    walk,
                    rotationPaths[i],
                    "localEulerAnglesRaw.z");
            }

            NormalizeEightPhaseCurve(
                walk,
                Visual,
                "m_LocalPosition.y");

            // Balance the two arm chains at their opposing support peaks. The
            // phase-four values authored by BuildWalk remain intact so motion
            // continues through the complete cycle instead of snapping neutral.
            SetOpposingPeakValues(walk, UpperArmL, 18f, -14f);
            SetOpposingPeakValues(walk, ForearmL, -13f, 8f);
            SetOpposingPeakValues(walk, HandL, 5f, -4f);
            SetOpposingPeakValues(walk, UpperArmR, 14f, -18f);
            SetOpposingPeakValues(walk, ForearmR, -7f, 13f);
            SetOpposingPeakValues(walk, HandR, 4f, -5f);

            EditorUtility.SetDirty(walk);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Patch 4 V33 walk finalized: clamped-auto curves, a loop-only seam " +
                "and restrained opposing arm peaks applied for a heavy smooth gait.");
        }

        private static void NormalizeEightPhaseCurve(
            AnimationClip clip,
            string path,
            string property)
        {
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                path,
                typeof(Transform),
                property);
            AnimationCurve curve =
                AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null || curve.length < 9)
            {
                throw new InvalidOperationException(
                    "Patch 4 v18 walk curve is missing or not eight-phase: " +
                    path + " / " + property);
            }

            Keyframe[] keys = curve.keys;
            keys[keys.Length - 1].value = keys[0].value;
            WriteCurve(clip, binding, keys);
        }

        private static void SetOpposingPeakValues(
            AnimationClip clip,
            string path,
            float firstPeak,
            float oppositePeak)
        {
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                path,
                typeof(Transform),
                "localEulerAnglesRaw.z");
            AnimationCurve curve =
                AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null || curve.length < 9)
            {
                throw new InvalidOperationException(
                    "Patch 4 v18b arm curve is missing or not eight-phase: " +
                    path);
            }

            Keyframe[] keys = curve.keys;
            keys[2].value = firstPeak;
            keys[6].value = oppositePeak;
            WriteCurve(clip, binding, keys);
        }

        private static void WriteCurve(
            AnimationClip clip,
            EditorCurveBinding binding,
            Keyframe[] keys)
        {
            AnimationCurve normalized = new(keys);
            for (int i = 0; i < normalized.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    normalized,
                    i,
                    AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(
                    normalized,
                    i,
                    AnimationUtility.TangentMode.ClampedAuto);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                binding,
                normalized);
        }
    }
}
