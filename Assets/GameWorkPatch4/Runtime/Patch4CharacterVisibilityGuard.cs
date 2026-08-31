using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Final runtime safety net that prevents Patch 4 and Patch 3.5 bodies
    /// from being visible at the same time.
    /// </summary>
    [DefaultExecutionOrder(1200)]
    [DisallowMultipleComponent]
    public sealed class Patch4CharacterVisibilityGuard : MonoBehaviour
    {
        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private GameObject patch4VisualRoot;
        [SerializeField] private GameObject patch35RollbackRoot;
        [SerializeField] private bool logRepairs = true;

        private bool repairLogged;

        public bool HasRollbackBinding =>
            patch35RollbackRoot != null &&
            rigController != null &&
            rigController.HasRollbackBinding;

        private void Reset()
        {
            rigController = GetComponent<Patch4CharacterRigController>();
        }

        public void BindRollbackRoot(GameObject rollbackRoot)
        {
            if (rigController == null)
            {
                rigController =
                    GetComponent<Patch4CharacterRigController>();
            }

            patch35RollbackRoot = rollbackRoot;
            ApplyExpectedState();
        }

        private void LateUpdate()
        {
            bool repaired = ApplyExpectedState();

            if (repaired && logRepairs && !repairLogged)
            {
                repairLogged = true;
                Debug.LogWarning(
                    "Patch 4 visibility guard repaired a conflicting body " +
                    "state. Only one character system is now visible.",
                    this);
            }
            else if (!repaired)
            {
                repairLogged = false;
            }
        }

        private bool ApplyExpectedState()
        {
            return rigController != null &&
                rigController.SynchronizeVisualState();
        }
    }
}
