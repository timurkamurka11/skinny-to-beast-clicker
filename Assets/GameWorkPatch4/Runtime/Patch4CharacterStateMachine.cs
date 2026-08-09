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
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int LookHash = Animator.StringToHash("Look");
        private static readonly int ShiftHash = Animator.StringToHash("Shift");
        private static readonly int TurnHash = Animator.StringToHash("Turn");
        private static readonly int SitHash = Animator.StringToHash("Sit");
        private static readonly int TapVariantHash = Animator.StringToHash("TapVariant");
        private static readonly int TapHash = Animator.StringToHash("Tap");
        private static readonly int BlinkHash = Animator.StringToHash("Blink");
        private static readonly int UpgradeHash = Animator.StringToHash("Upgrade");

        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private Animator animator;

#if UNITY_EDITOR
        private bool lockedReviewActive;
#endif

        public bool IsReady =>
            rigController != null &&
            (rigController.Patch4Enabled || IsLockedReviewActive) &&
            animator != null &&
            animator.runtimeAnimatorController != null;

        private bool IsLockedReviewActive
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
            animator = GetComponentInChildren<Animator>(true);
        }

        public void SetWalkSpeed(float normalizedSpeed)
        {
            if (!IsReady)
            {
                return;
            }

            animator.SetFloat(SpeedHash, Mathf.Clamp01(normalizedSpeed));
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
