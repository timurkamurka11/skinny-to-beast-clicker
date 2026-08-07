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

            EditorUtility.SetDirty(walk);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Patch 4 v18 walk finalized: neutral loop seam + neutral " +
                "half-cycle seam retained around explicit opposing gait peaks.");
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
