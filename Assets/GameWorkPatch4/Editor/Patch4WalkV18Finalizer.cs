using System;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Final normalization pass for the v18 walk clip. The authored eight-phase
    /// gait uses strong contact/lift poses, but the loop seam and half-cycle seam
    /// must remain neutral so Animator sampling begins from an undistorted bind
    /// pose and the next half-cycle can reverse cleanly.
    ///
    /// v18b also balances the arm peaks. The first v18 runtime review measured
    /// the right hand at only ~0.63 units from its neutral shoulder-relative pose
    /// while the contract requires 0.68. We correct the actual gait amplitude,
    /// not the test threshold, and keep both arms visually symmetric.
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

            // Balance the two arm chains at the two sampled gait peaks. This is
            // intentionally done after seam normalization so phase 0 / phase 4
            // stay neutral while phase 2 / phase 6 carry the readable arm swing.
            SetOpposingPeakValues(walk, UpperArmL, 30f, -28f);
            SetOpposingPeakValues(walk, ForearmL, -20f, 18f);
            SetOpposingPeakValues(walk, HandL, 7f, -7f);
            SetOpposingPeakValues(walk, UpperArmR, 28f, -30f);
            SetOpposingPeakValues(walk, ForearmR, -18f, 20f);
            SetOpposingPeakValues(walk, HandR, 7f, -7f);

            EditorUtility.SetDirty(walk);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Patch 4 v18b walk finalized: neutral seams retained and " +
                "balanced opposing arm peaks applied for full hand articulation.");
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
            keys[0].value = 0f;
            keys[4].value = 0f;
            keys[keys.Length - 1].value = 0f;
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
