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
        private static readonly int TurnHash = Animator.StringToHash("Turn");
        private static readonly int SitHash = Animator.StringToHash("Sit");
        private static readonly int TapVariantHash = Animator.StringToHash("TapVariant");
        private static readonly int TapHash = Animator.StringToHash("Tap");
        private static readonly int UpgradeHash = Animator.StringToHash("Upgrade");

        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private Animator animator;

        public bool IsReady =>
            rigController != null &&
            rigController.Patch4Enabled &&
            animator != null &&
            animator.runtimeAnimatorController != null;

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

        public void PlayUpgradeReaction()
        {
            if (IsReady)
            {
                animator.SetTrigger(UpgradeHash);
            }
        }
    }
}
