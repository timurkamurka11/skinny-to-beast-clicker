using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    public static class Patch4GeneratedPrefabAudit
    {
        public static bool ValidateGeneratedPrefab(out string error)
        {
            List<string> failures = new();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                Patch4PrefabBuilder.PrefabPath);
            if (prefab == null)
            {
                error = "Generated FatMan_Patch4 prefab is missing.";
                return false;
            }

            ValidateComponents(prefab, failures);
            ValidateAnimator(prefab, failures);
            ValidateSkeleton(prefab, failures);
            ValidatePresentation(prefab, failures);
            ValidateAnimationBindings(prefab, failures);
            error = string.Join("\n", failures);
            return failures.Count == 0;
        }

        public static void ValidateOrThrow()
        {
            if (!ValidateGeneratedPrefab(out string error))
            {
                throw new InvalidOperationException(
                    "Patch 4 generated-prefab audit failed:\n" + error);
            }
        }

        private static void ValidateComponents(GameObject prefab, List<string> failures)
        {
            RequireExactlyOne<Patch4CharacterRigController>(prefab, failures);
            RequireExactlyOne<Patch4CharacterStateMachine>(prefab, failures);
            RequireExactlyOne<Patch4FaceController>(prefab, failures);
            RequireExactlyOne<Patch4CanvasPresentation>(prefab, failures);
            RequireExactlyOne<Patch4V21HybridPuppetController>(prefab, failures);
            RequireExactlyOne<Patch4V21FaceSwapBridge>(prefab, failures);
            RequireExactlyOne<Patch4V23FullFramePresentation>(prefab, failures);
            RequireExactlyOne<Patch4CharacterVisibilityGuard>(prefab, failures);
            RequireExactlyOne<Patch4LegacySignalBridge>(prefab, failures);
            RequireExactlyOne<Patch4SecondaryMotionController>(prefab, failures);

            if (prefab.GetComponent<Patch4V21FootPlantController>() != null)
                failures.Add("Obsolete Patch4V21FootPlantController is attached.");
            if (prefab.GetComponent<Patch4StableBodySkinController>() != null)
                failures.Add("Obsolete Patch4StableBodySkinController is attached.");
            if (prefab.GetComponent<Patch4CutoutPuppetController>() != null)
                failures.Add("Obsolete Patch4CutoutPuppetController is attached.");
        }

        private static void ValidateAnimator(GameObject prefab, List<string> failures)
        {
            Animator[] animators = prefab.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1 || animators[0].gameObject != prefab)
            {
                failures.Add("Prefab must have exactly one authoritative root Animator.");
                return;
            }
            Animator animator = animators[0];
            if (!animator.enabled) failures.Add("Root Animator is disabled.");
            if (animator.applyRootMotion)
                failures.Add("Root Animator must not own gameplay-root travel.");
            if (animator.updateMode != AnimatorUpdateMode.UnscaledTime ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                failures.Add("Root Animator does not use the canonical update/culling mode.");
            }
            AnimatorController controller =
                animator.runtimeAnimatorController as AnimatorController;
            if (controller == null)
            {
                failures.Add("Root Animator has no persisted AnimatorController.");
                return;
            }
            if (!string.Equals(controller.name,
                    Patch4CharacterStateMachine.ExpectedControllerName,
                    StringComparison.Ordinal))
            {
                failures.Add("Root Animator uses the wrong controller: " + controller.name + ".");
            }
            if (controller.layers.Length != 1 ||
                controller.layers[0].name != Patch4CharacterStateMachine.AnimatorLayerName)
            {
                failures.Add("Animator does not have the single canonical Base Layer.");
                return;
            }

            var requiredParameters = new Dictionary<string, AnimatorControllerParameterType>
            {
                ["Speed"] = AnimatorControllerParameterType.Float,
                ["Look"] = AnimatorControllerParameterType.Bool,
                ["Shift"] = AnimatorControllerParameterType.Bool,
                ["Turn"] = AnimatorControllerParameterType.Bool,
                ["Sit"] = AnimatorControllerParameterType.Bool,
                ["TapVariant"] = AnimatorControllerParameterType.Int,
                ["Tap"] = AnimatorControllerParameterType.Trigger,
                ["Blink"] = AnimatorControllerParameterType.Trigger,
                ["Upgrade"] = AnimatorControllerParameterType.Trigger
            };
            foreach (var required in requiredParameters)
            {
                if (!controller.parameters.Any(parameter =>
                        parameter.name == required.Key && parameter.type == required.Value))
                {
                    failures.Add("Animator parameter is missing or mistyped: " + required.Key + ".");
                }
            }

            HashSet<string> stateNames = new(
                controller.layers[0].stateMachine.states
                    .Select(child => child.state != null ? child.state.name : string.Empty),
                StringComparer.Ordinal);
            foreach (string state in Patch4RigContract.RequiredClipNames)
            {
                if (!stateNames.Contains(state)) failures.Add("Animator state is missing: " + state + ".");
            }
        }

        private static void ValidateSkeleton(GameObject prefab, List<string> failures)
        {
            Patch4CharacterRigController rig =
                prefab.GetComponent<Patch4CharacterRigController>();
            Transform root = rig != null ? rig.RigRoot : null;
            if (root == null)
            {
                failures.Add("RigRoot is not serialized.");
                return;
            }
            Dictionary<string, List<Transform>> byName = new(StringComparer.Ordinal);
            foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
            {
                if (!byName.TryGetValue(bone.name, out List<Transform> matches))
                {
                    matches = new List<Transform>();
                    byName.Add(bone.name, matches);
                }
                matches.Add(bone);
            }
            foreach (string name in Patch4RigContract.RequiredBoneNames)
            {
                if (!byName.TryGetValue(name, out List<Transform> matches) || matches.Count != 1)
                {
                    failures.Add("Required bone is missing or duplicated: " + name + ".");
                    continue;
                }
                Transform bone = matches[0];
                if (Patch4RigContract.TryGetRequiredParent(name, out string parent) &&
                    (bone.parent == null || bone.parent.name != parent))
                {
                    failures.Add("Required bone has wrong parent: " + name + ".");
                }
                if (!Finite(bone.localPosition) || !Finite(bone.localRotation) ||
                    !Finite(bone.localScale) ||
                    Mathf.Abs(bone.localScale.x) < 0.0001f ||
                    Mathf.Abs(bone.localScale.y) < 0.0001f ||
                    Mathf.Abs(bone.localScale.z) < 0.0001f ||
                    Mathf.Abs(bone.localScale.x) > 4f ||
                    Mathf.Abs(bone.localScale.y) > 4f ||
                    Mathf.Abs(bone.localScale.z) > 4f)
                {
                    failures.Add("Required bone has an invalid transform: " + name + ".");
                }
            }
        }

        private static void ValidatePresentation(GameObject prefab, List<string> failures)
        {
            Transform visual = prefab.transform.Find("Patch4VisualRoot");
            if (visual == null)
            {
                failures.Add("Patch4VisualRoot is missing.");
                return;
            }
            if (visual.gameObject.activeSelf)
                failures.Add("Generated Patch4VisualRoot must remain hidden.");

            Image[] images = visual.GetComponentsInChildren<Image>(true);
            if (images.Length != Patch4RigContract.RequiredLayerPaths.Count)
                failures.Add("Generated Canvas must contain exactly 40 required Images.");
            if (images.Any(image => image == null || image.sprite == null || image.useSpriteMesh))
                failures.Add("Generated Canvas contains an invalid painted Image.");

            RawImage[] references = visual.GetComponentsInChildren<RawImage>(true);
            if (references.Length != 1 || references[0].enabled)
                failures.Add("V23 must remain one disabled QA/reference RawImage.");
            if (visual.GetComponentsInChildren<SpriteRenderer>(true)
                .Any(renderer => renderer.enabled))
            {
                failures.Add("A fallback SpriteRenderer can compete with the Canvas rig.");
            }

            Patch4V21HybridPuppetController hybrid =
                prefab.GetComponent<Patch4V21HybridPuppetController>();
            if (hybrid != null)
            {
                SerializedObject serialized = new(hybrid);
                foreach (string propertyName in new[]
                {
                    "rigController", "canvasPresentation", "torsoSprite",
                    "armLSprite", "armRSprite", "legLSprite", "legRSprite"
                })
                {
                    SerializedProperty property = serialized.FindProperty(propertyName);
                    if (property == null || property.objectReferenceValue == null)
                        failures.Add("V21 hybrid reference is missing: " + propertyName + ".");
                }
            }
            Patch4V21FaceSwapBridge face = prefab.GetComponent<Patch4V21FaceSwapBridge>();
            if (face != null)
            {
                SerializedObject serialized = new(face);
                foreach (string propertyName in new[] { "faceController", "canvasPresentation" })
                {
                    SerializedProperty property = serialized.FindProperty(propertyName);
                    if (property == null || property.objectReferenceValue == null)
                        failures.Add("V21 face reference is missing: " + propertyName + ".");
                }
            }
        }

        private static void ValidateAnimationBindings(GameObject prefab, List<string> failures)
        {
            Animator animator = prefab.GetComponent<Animator>();
            RuntimeAnimatorController controller =
                animator != null ? animator.runtimeAnimatorController : null;
            if (controller == null) return;
            Dictionary<string, AnimationClip> clips = controller.animationClips
                .Where(clip => clip != null)
                .GroupBy(clip => clip.name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            foreach (string required in Patch4RigContract.RequiredClipNames)
            {
                if (!clips.TryGetValue(required, out AnimationClip clip))
                {
                    failures.Add("Animator clip is missing: " + required + ".");
                    continue;
                }
                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (!BindingResolves(prefab.transform, binding.path))
                        failures.Add(required + " has an unresolved binding: " + binding.path + ".");
                    if (binding.type == typeof(Transform) &&
                        string.IsNullOrEmpty(binding.path))
                    {
                        failures.Add(required +
                            " writes the prefab root Transform instead of leaving " +
                            "gameplay travel to CharacterRoutineController.");
                    }
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null) continue;
                    foreach (Keyframe key in curve.keys)
                    {
                        if (float.IsNaN(key.value) || float.IsInfinity(key.value) ||
                            ((binding.propertyName.Contains("Scale") ||
                              binding.propertyName.Contains("Position")) &&
                             Mathf.Abs(key.value) > 4096f))
                        {
                            failures.Add(required + " has an invalid transform curve: " +
                                binding.path + "/" + binding.propertyName + ".");
                            break;
                        }
                    }
                }
                foreach (EditorCurveBinding binding in
                         AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    if (!BindingResolves(prefab.transform, binding.path))
                        failures.Add(required + " has an unresolved object binding: " + binding.path + ".");
                }
            }
        }

        private static bool BindingResolves(Transform root, string path) =>
            string.IsNullOrEmpty(path) || root.Find(path) != null;

        private static void RequireExactlyOne<T>(GameObject prefab, List<string> failures)
            where T : Component
        {
            int count = prefab.GetComponentsInChildren<T>(true).Length;
            if (count != 1) failures.Add(typeof(T).Name + " count is " + count + ", expected 1.");
        }

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        private static bool Finite(Quaternion value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z) &&
            !float.IsNaN(value.w) && !float.IsInfinity(value.w);
    }
}
