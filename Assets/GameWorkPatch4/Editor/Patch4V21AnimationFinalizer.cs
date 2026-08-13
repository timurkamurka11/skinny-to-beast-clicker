using System;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// v21 animation normalization. Strong pose ideas are retained, but the
    /// visible actor is no longer asked to fake weight transfer with whole-body
    /// scale or extreme independent limb arcs. Walk leg motion is corrected at
    /// runtime by Patch4V21FootPlantController's mirrored continuous gait; this
    /// pass keeps the authored arm counter-swing and removes the
    /// silhouette-destroying scale curves.
    /// </summary>
    public static class Patch4V21AnimationFinalizer
    {
        private const string Root = "Assets/GameWorkPatch4/Animations/";
        private const string Visual = "Patch4VisualRoot/Root/CharacterRoot";
        private const string Pelvis = Visual + "/Pelvis";
        private const string SpineLower = Pelvis + "/SpineLower";
        private const string SpineUpper = SpineLower + "/SpineUpper";
        private const string BellyBase = SpineLower + "/BellyBase";
        private const string BellyTip = BellyBase + "/BellyTip";
        private const string Head = SpineUpper + "/Neck/Head";
        private const string UpperArmL = SpineUpper + "/ClavicleL/UpperArmL";
        private const string ForearmL = UpperArmL + "/ForearmL";
        private const string HandL = ForearmL + "/HandL";
        private const string UpperArmR = SpineUpper + "/ClavicleR/UpperArmR";
        private const string ForearmR = UpperArmR + "/ForearmR";
        private const string HandR = ForearmR + "/HandR";
        private const string ThighL = Pelvis + "/ThighL";
        private const string ShinL = ThighL + "/ShinL";
        private const string ThighR = Pelvis + "/ThighR";
        private const string ShinR = ThighR + "/ShinR";

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

        public static void Apply()
        {
            AnimationClip[] clips = new AnimationClip[ClipNames.Length];
            for (int i = 0; i < ClipNames.Length; i++)
            {
                clips[i] = Load(ClipNames[i]);
                RemoveForbiddenScaleCurves(clips[i]);
            }

            StabilizeIdle(clips[0]);
            StabilizeShiftWeight(clips[1]);
            StabilizeLook(clips[3]);
            StabilizeTap01(clips[4]);
            StabilizeTap02(clips[5]);
            StabilizeTurn(clips[7]);
            StabilizeSit(clips[8]);
            StabilizeUpgrade(clips[9]);

            for (int i = 0; i < clips.Length; i++)
            {
                EditorUtility.SetDirty(clips[i]);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Patch 4 v21 animations finalized: whole-body/core-bone scale " +
                "curves removed, reaction silhouettes bounded, arm motion kept " +
                "readable, and leg motion delegated to continuous mirrored IK.");
        }

        private static AnimationClip Load(string name)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                Root + name + ".anim");
            if (clip == null)
            {
                throw new InvalidOperationException(
                    "Patch 4 v21 animation finalizer could not load " + name);
            }
            return clip;
        }

        private static void RemoveForbiddenScaleCurves(AnimationClip clip)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            for (int i = 0; i < bindings.Length; i++)
            {
                EditorCurveBinding binding = bindings[i];
                if (binding.type != typeof(Transform) ||
                    string.IsNullOrEmpty(binding.path) ||
                    !binding.propertyName.StartsWith(
                        "m_LocalScale.",
                        StringComparison.Ordinal) ||
                    string.Equals(binding.path, BellyTip, StringComparison.Ordinal))
                {
                    continue;
                }

                AnimationUtility.SetEditorCurve(clip, binding, null);
            }
        }

        private static void StabilizeIdle(AnimationClip clip)
        {
            float d = 3.2f;
            SetCurve(clip, Visual, "m_LocalPosition.y",
                K(0f, 0f), K(d * .5f, .022f), K(d, 0f));
            SetCurve(clip, BellyTip, "m_LocalScale.x",
                K(0f, 1f), K(d * .5f, 1.025f), K(d, 1f));
            SetCurve(clip, BellyTip, "m_LocalScale.y",
                K(0f, 1f), K(d * .5f, 1.014f), K(d, 1f));
        }

        private static void StabilizeShiftWeight(AnimationClip clip)
        {
            const float d = 3.2f;
            SetCurve(clip, Visual, "m_LocalPosition.x",
                K(0f, 0f), K(.8f, -.07f), K(1.6f, 0f),
                K(2.4f, .07f), K(d, 0f));
            SetCurve(clip, Visual, "m_LocalPosition.y",
                K(0f, 0f), K(.8f, -.014f), K(1.6f, 0f),
                K(2.4f, -.014f), K(d, 0f));
            Alternating(clip, Pelvis, d, -3f, 3f);
            Alternating(clip, SpineLower, d, 2f, -2f);
            Alternating(clip, ThighL, d, 2f, -2f);
            Alternating(clip, ThighR, d, -2f, 2f);
            Alternating(clip, UpperArmL, d, -2.5f, 2.5f);
            Alternating(clip, UpperArmR, d, 2.5f, -2.5f);
        }

        private static void StabilizeLook(AnimationClip clip)
        {
            const float d = 3f;
            Alternating(clip, Head, d, -7f, 7f);
            Alternating(clip, SpineUpper, d, 1.5f, -1.5f);
        }

        private static void StabilizeTap01(AnimationClip clip)
        {
            const float d = .65f;
            SetCurve(clip, Visual, "m_LocalPosition.y",
                K(0f, 0f), K(.14f, -.06f), K(.34f, .025f), K(d, 0f));
            Reaction(clip, SpineLower, d, -5f, 2f);
            Reaction(clip, UpperArmL, d, -10f, 4f);
            Reaction(clip, UpperArmR, d, 10f, -4f);
            Reaction(clip, ForearmL, d, 12f, -4f);
            Reaction(clip, ForearmR, d, -12f, 4f);
            Reaction(clip, HandL, d, -4f, 2f);
            Reaction(clip, HandR, d, 4f, -2f);
            Reaction(clip, Head, d, 3f, -1.5f);
        }

        private static void StabilizeTap02(AnimationClip clip)
        {
            const float d = .72f;
            SetCurve(clip, Visual, "m_LocalPosition.x",
                K(0f, 0f), K(.16f, .045f), K(.38f, -.02f), K(d, 0f));
            Reaction(clip, Head, d, 7f, -3f);
            Reaction(clip, SpineUpper, d, -4f, 2f);
            Reaction(clip, UpperArmL, d, -12f, 4f);
            Reaction(clip, UpperArmR, d, 12f, -4f);
            Reaction(clip, ForearmL, d, 14f, -5f);
            Reaction(clip, ForearmR, d, -14f, 5f);
            Reaction(clip, HandL, d, -5f, 2f);
            Reaction(clip, HandR, d, 5f, -2f);
        }

        private static void StabilizeTurn(AnimationClip clip)
        {
            const float d = .72f;
            SetCurve(clip, Visual, "m_LocalPosition.x",
                K(0f, 0f), K(.2f, -.05f), K(.42f, .07f), K(d, 0f));
            Reaction(clip, Pelvis, d, -6f, 5f);
            Reaction(clip, SpineUpper, d, 4f, -3f);
            Reaction(clip, Head, d, 9f, -6f);
            Reaction(clip, UpperArmL, d, -7f, 4f);
            Reaction(clip, UpperArmR, d, 7f, -4f);
        }

        private static void StabilizeSit(AnimationClip clip)
        {
            const float d = 1.15f;
            SetCurve(clip, Visual, "m_LocalPosition.y",
                K(0f, 0f), K(.58f, -.22f), K(d, -.22f));
            Hold(clip, Pelvis, d, -6f);
            Hold(clip, SpineLower, d, 8f);
            Hold(clip, ThighL, d, -14f);
            Hold(clip, ThighR, d, 14f);
            Hold(clip, ShinL, d, 10f);
            Hold(clip, ShinR, d, -10f);
        }

        private static void StabilizeUpgrade(AnimationClip clip)
        {
            const float d = 1.05f;
            SetCurve(clip, Visual, "m_LocalPosition.y",
                K(0f, 0f), K(.2f, .10f), K(.48f, -.03f), K(d, 0f));
            Reaction(clip, UpperArmL, d, -14f, 5f);
            Reaction(clip, UpperArmR, d, 14f, -5f);
            Reaction(clip, ForearmL, d, -18f, 6f);
            Reaction(clip, ForearmR, d, 18f, -6f);
            Reaction(clip, HandL, d, 6f, -2f);
            Reaction(clip, HandR, d, -6f, 2f);
            Reaction(clip, SpineUpper, d, -4f, 2f);
            Reaction(clip, Head, d, -5f, 3f);
            SetCurve(clip, BellyTip, "m_LocalScale.x",
                K(0f, 1f), K(.2f, 1.025f), K(.48f, .985f), K(d, 1f));
        }

        private static void Alternating(
            AnimationClip clip,
            string path,
            float duration,
            float first,
            float second)
        {
            SetCurve(clip, path, "localEulerAnglesRaw.z",
                K(0f, 0f),
                K(duration * .25f, first),
                K(duration * .5f, 0f),
                K(duration * .75f, second),
                K(duration, 0f));
        }

        private static void Reaction(
            AnimationClip clip,
            string path,
            float duration,
            float impact,
            float rebound)
        {
            SetCurve(clip, path, "localEulerAnglesRaw.z",
                K(0f, 0f),
                K(duration * .22f, impact),
                K(duration * .52f, rebound),
                K(duration, 0f));
        }

        private static void Hold(
            AnimationClip clip,
            string path,
            float duration,
            float value)
        {
            SetCurve(clip, path, "localEulerAnglesRaw.z",
                K(0f, 0f), K(.58f, value), K(duration, value));
        }

        private static Keyframe K(float time, float value)
        {
            return new Keyframe(time, value);
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
                    curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }
    }
}
