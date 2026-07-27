using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Repairs parameter-type-sensitive transitions after the generated
    /// controller is imported. Unity accepts the serialized controller before
    /// it validates every condition, so this pass keeps Speed as a float rule.
    /// </summary>
    public sealed class Patch4AnimatorControllerSanitizer : AssetPostprocessor
    {
        private static bool repairing;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            for (int i = 0; i < importedAssets.Length; i++)
            {
                if (string.Equals(
                        importedAssets[i],
                        Patch4AnimationLibraryBuilder.ControllerPath,
                        StringComparison.Ordinal))
                {
                    EditorApplication.delayCall += RepairController;
                    break;
                }
            }
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Animation/Sanitize Controller")]
        public static void RepairController()
        {
            if (repairing)
            {
                return;
            }

            repairing = true;
            try
            {
                AnimatorController controller =
                    AssetDatabase.LoadAssetAtPath<AnimatorController>(
                        Patch4AnimationLibraryBuilder.ControllerPath);
                if (controller == null || controller.layers.Length == 0)
                {
                    return;
                }

                AnimatorStateMachine machine = controller.layers[0].stateMachine;
                AnimatorState idle = FindState(machine, "FatMan_Idle_Breathe");
                AnimatorState walk = FindState(machine, "FatMan_Walk_InRoom");
                if (idle == null || walk == null)
                {
                    return;
                }

                AnimatorStateTransition[] transitions = idle.transitions;
                for (int i = 0; i < transitions.Length; i++)
                {
                    AnimatorStateTransition transition = transitions[i];
                    if (transition == null || transition.destinationState != walk)
                    {
                        continue;
                    }

                    transition.conditions = Array.Empty<AnimatorCondition>();
                    transition.AddCondition(
                        AnimatorConditionMode.Greater,
                        0.1f,
                        "Speed");
                    transition.hasExitTime = false;
                    transition.duration = 0.15f;
                    EditorUtility.SetDirty(transition);
                }

                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                repairing = false;
            }
        }

        private static AnimatorState FindState(
            AnimatorStateMachine machine,
            string name)
        {
            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == name)
                {
                    return states[i].state;
                }
            }

            return null;
        }
    }
}
