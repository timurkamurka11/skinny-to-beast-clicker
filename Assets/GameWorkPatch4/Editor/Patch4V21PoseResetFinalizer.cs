using System;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Patch 4 uses writeDefaultValues=false. Any bone omitted by the next clip
    /// would otherwise keep the rotation left by the previous state; this was
    /// especially destructive after Walk when a reaction clip did not key legs.
    /// v21 makes every clip self-contained by adding neutral curves only where a
    /// clip has no authored curve of its own.
    /// </summary>
    public static class Patch4V21PoseResetFinalizer
    {
        private const string AnimationRoot = "Assets/GameWorkPatch4/Animations/";
        private const string Visual = "Patch4VisualRoot/Root/CharacterRoot";
        private const string Pelvis = Visual + "/Pelvis";
        private const string SpineLower = Pelvis + "/SpineLower";
        private const string SpineUpper = SpineLower + "/SpineUpper";
        private const string BellyTip = SpineLower + "/BellyBase/BellyTip";
        private const string Head = SpineUpper + "/Neck/Head";
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

        private static readonly string[] ClipNames =
        {
            "FatMan_Idle_Breathe",
            "FatMan_Idle_ShiftWeight",
            "FatMan_Blink_Random",
            "FatMan_LookAround",
            "FatMan_TapReact_01",
            "FatMan_TapReact_02",
            "FatMan_Walk_InRoom",
            "FatMan_Turn",
            "FatMan_SitOrLean",
            "FatMan_UpgradeReact"
        };

        private static readonly string[] RotationPaths =
        {
            Pelvis,
            SpineLower,
            SpineUpper,
            Head,
            ClavicleL,
            UpperArmL,
            ForearmL,
            HandL,
            ClavicleR,
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

        public static void Apply()
        {
            for (int clipIndex = 0; clipIndex < ClipNames.Length; clipIndex++)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    AnimationRoot + ClipNames[clipIndex] + ".anim");
                if (clip == null)
                {
                    throw new InvalidOperationException(
                        "Patch 4 v21 pose reset could not load " +
                        ClipNames[clipIndex]);
                }

                float duration = Mathf.Max(.0333f, clip.length);
                for (int i = 0; i < RotationPaths.Length; i++)
                {
                    EnsureNeutralCurve(
                        clip,
                        RotationPaths[i],
                        "localEulerAnglesRaw.z",
                        0f,
                        duration);
                }

                EnsureNeutralCurve(
                    clip,
                    Visual,
                    "m_LocalPosition.x",
                    0f,
                    duration);
                EnsureNeutralCurve(
                    clip,
                    Visual,
                    "m_LocalPosition.y",
                    0f,
                    duration);

                // Belly scale is the only allowed soft-body scale channel. It
                // must still explicitly return to neutral in clips that do not
                // author breathing/impact secondary motion.
                EnsureNeutralCurve(
                    clip,
                    BellyTip,
                    "m_LocalScale.x",
                    1f,
                    duration);
                EnsureNeutralCurve(
                    clip,
                    BellyTip,
                    "m_LocalScale.y",
                    1f,
                    duration);

                EditorUtility.SetDirty(clip);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Patch 4 v21 pose reset finalized: every animation state now " +
                "keys neutral values for omitted core bones/root channels, so " +
                "Walk leg/arm poses cannot leak into Turn, React or Upgrade.");
        }

        private static void EnsureNeutralCurve(
            AnimationClip clip,
            string path,
            string property,
            float value,
            float duration)
        {
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                path,
                typeof(Transform),
                property);
            AnimationCurve existing = AnimationUtility.GetEditorCurve(clip, binding);
            if (existing != null && existing.length > 0)
            {
                return;
            }

            AnimationCurve curve = AnimationCurve.Constant(0f, duration, value);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
    }
}
