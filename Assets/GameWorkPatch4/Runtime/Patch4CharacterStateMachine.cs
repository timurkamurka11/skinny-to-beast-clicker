using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Small gameplay-to-Animator bridge for the Patch 4 animation set.
    /// It does not replace existing menu or gameplay systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Patch4CharacterStateMachine : MonoBehaviour
    {
        public const string AnimatorLayerName = "Base Layer";

        private readonly struct AnimatorParameterContract
        {
            public readonly string name;
            public readonly AnimatorControllerParameterType type;

            public AnimatorParameterContract(
                string name,
                AnimatorControllerParameterType type)
            {
                this.name = name;
                this.type = type;
            }
        }

        private const float WalkThreshold = 0.1f;
        private const float LocomotionTransitionSeconds = 0.18f;

        private static readonly AnimatorParameterContract[]
            RequiredParameters =
            {
                new("Speed", AnimatorControllerParameterType.Float),
                new("Look", AnimatorControllerParameterType.Bool),
                new("Shift", AnimatorControllerParameterType.Bool),
                new("Turn", AnimatorControllerParameterType.Bool),
                new("Sit", AnimatorControllerParameterType.Bool),
                new("TapVariant", AnimatorControllerParameterType.Int),
                new("Tap", AnimatorControllerParameterType.Trigger),
                new("Blink", AnimatorControllerParameterType.Trigger),
                new("Upgrade", AnimatorControllerParameterType.Trigger)
            };

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int LookHash = Animator.StringToHash("Look");
        private static readonly int ShiftHash = Animator.StringToHash("Shift");
        private static readonly int TurnHash = Animator.StringToHash("Turn");
        private static readonly int SitHash = Animator.StringToHash("Sit");
        private static readonly int TapVariantHash = Animator.StringToHash("TapVariant");
        private static readonly int TapHash = Animator.StringToHash("Tap");
        private static readonly int BlinkHash = Animator.StringToHash("Blink");
        private static readonly int UpgradeHash = Animator.StringToHash("Upgrade");
        private static readonly int IdleStateHash = Animator.StringToHash(
            "Base Layer.FatMan_Idle_Breathe");
        private static readonly int WalkStateHash = Animator.StringToHash(
            "Base Layer.FatMan_Walk_InRoom");

        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private Animator animator;

#if UNITY_EDITOR
        private bool lockedReviewActive;
#endif

        public bool IsConfigured =>
            rigController != null &&
            ValidateAnimatorContract(animator, out _);
        public string AnimatorReadinessError
        {
            get
            {
                ValidateAnimatorContract(animator, out string error);
                return error;
            }
        }
        public bool IsReady =>
            IsConfigured &&
            (rigController.Patch4Enabled || IsLockedReviewActive) &&
            animator != null;

        public bool IsLockedReviewActive
        {
            get
            {
#if UNITY_EDITOR
                return lockedReviewActive;
#else
                return false;
#endif
            }
        }

        private void Reset()
        {
            rigController = GetComponent<Patch4CharacterRigController>();
            animator = GetComponent<Animator>();
        }

        public bool BindRuntimeDependencies(
            Patch4CharacterRigController patchRig,
            Animator rootAnimator)
        {
            if (patchRig == null ||
                rootAnimator == null ||
                rootAnimator.gameObject != gameObject)
            {
                return false;
            }

            rigController = patchRig;
            animator = rootAnimator;
            return IsConfigured;
        }

        public static bool ValidateAnimatorContract(
            Animator animator,
            out string error)
        {
            if (animator == null)
            {
                error = "Patch 4 root Animator is missing.";
                return false;
            }

            Animator[] animatorOwners =
                animator.GetComponentsInChildren<Animator>(true);
            if (animator.GetComponent<Patch4CharacterRigController>() == null ||
                animatorOwners.Length != 1 ||
                animatorOwners[0] != animator)
            {
                error =
                    "Patch 4 must have exactly one authoritative root " +
                    "Animator.";
                return false;
            }

            if (!animator.enabled)
            {
                error = "Patch 4 root Animator is disabled.";
                return false;
            }

            if (animator.runtimeAnimatorController == null)
            {
                error =
                    "Patch 4 root Animator has no RuntimeAnimatorController.";
                return false;
            }

            if (animator.layerCount != 1 ||
                !string.Equals(
                    animator.GetLayerName(0),
                    AnimatorLayerName,
                    System.StringComparison.Ordinal))
            {
                error =
                    "Patch 4 Animator must expose exactly one Base Layer.";
                return false;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int requiredIndex = 0;
                 requiredIndex < RequiredParameters.Length;
                 requiredIndex++)
            {
                AnimatorParameterContract required =
                    RequiredParameters[requiredIndex];
                bool found = false;
                for (int parameterIndex = 0;
                     parameterIndex < parameters.Length;
                     parameterIndex++)
                {
                    AnimatorControllerParameter parameter =
                        parameters[parameterIndex];
                    if (string.Equals(
                            parameter.name,
                            required.name,
                            System.StringComparison.Ordinal) &&
                        parameter.type == required.type)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    error =
                        "Patch 4 Animator is missing required parameter " +
                        required.name + ".";
                    return false;
                }
            }

            for (int stateIndex = 0;
                 stateIndex < Patch4RigContract.RequiredClipNames.Count;
                 stateIndex++)
            {
                string statePath =
                    AnimatorLayerName + "." +
                    Patch4RigContract.RequiredClipNames[stateIndex];
                if (!animator.HasState(
                        0,
                        Animator.StringToHash(statePath)))
                {
                    error =
                        "Patch 4 Animator is missing required state " +
                        statePath + ".";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public void SetWalkSpeed(float normalizedSpeed)
        {
            if (!IsReady)
            {
                return;
            }

            float speed = Mathf.Clamp01(normalizedSpeed);
            bool nextWalkRequested = speed > WalkThreshold;
            animator.SetFloat(SpeedHash, speed);

            if (nextWalkRequested)
            {
                // The real room can keep the controller in Idle even after
                // accepting Speed = 1. Route the locomotion edge explicitly,
                // just like the established gameplay animation driver does,
                // instead of depending on a serialized float transition.
                // Repeated movement ticks do not restart the walk cycle, and
                // a one-shot reaction already in progress remains free to use
                // its conditional exit back to Walk.
                if (IsCurrentState(IdleStateHash))
                {
                    CrossFadePersistentState(WalkStateHash);
                }

                return;
            }

            if (IsCurrentOrTransitioningTo(WalkStateHash))
            {
                CrossFadePersistentState(IdleStateHash);
            }
        }

        private bool IsCurrentState(int stateHash)
        {
            return animator != null &&
                   !animator.IsInTransition(0) &&
                   animator.GetCurrentAnimatorStateInfo(0).fullPathHash ==
                       stateHash;
        }

        private bool IsCurrentOrTransitioningTo(int stateHash)
        {
            if (animator == null)
            {
                return false;
            }

            if (animator.IsInTransition(0))
            {
                return animator.GetNextAnimatorStateInfo(0).fullPathHash ==
                    stateHash;
            }

            return animator.GetCurrentAnimatorStateInfo(0).fullPathHash ==
                stateHash;
        }

        private void CrossFadePersistentState(int stateHash)
        {
            if (animator == null ||
                !animator.HasState(0, stateHash) ||
                IsCurrentOrTransitioningTo(stateHash))
            {
                return;
            }

            animator.CrossFadeInFixedTime(
                stateHash,
                LocomotionTransitionSeconds,
                0,
                0f);
        }

        public void SetLooking(bool active)
        {
            if (IsReady)
            {
                animator.SetBool(LookHash, active);
            }
        }

        public void SetShiftingWeight(bool active)
        {
            if (IsReady)
            {
                animator.SetBool(ShiftHash, active);
            }
        }

        public void SetTurning(bool active)
        {
            if (IsReady)
            {
                animator.SetBool(TurnHash, active);
            }
        }

        public void SetSittingOrLeaning(bool active)
        {
            if (IsReady)
            {
                animator.SetBool(SitHash, active);
            }
        }

        public void PlayTapReaction(int variant)
        {
            if (!IsReady)
            {
                return;
            }

            animator.SetInteger(TapVariantHash, variant <= 1 ? 1 : 2);
            animator.SetTrigger(TapHash);
        }

        public void PlayBlink()
        {
            if (IsReady)
            {
                animator.SetTrigger(BlinkHash);
            }
        }

        public void PlayUpgradeReaction()
        {
            if (IsReady)
            {
                animator.SetTrigger(UpgradeHash);
            }
        }

        /// <summary>
        /// Lets the editor-only locked room review exercise the same public
        /// gameplay API without opening the production readiness gate.
        /// Player builds always ignore this override.
        /// </summary>
        public void SetLockedReviewActive(bool active)
        {
#if UNITY_EDITOR
            lockedReviewActive = active;
#endif
        }
    }
}
