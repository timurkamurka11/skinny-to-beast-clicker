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

        private void Reset()
        {
            rigController = GetComponent<Patch4CharacterRigController>();
        }

        private void LateUpdate()
        {
            bool shouldShowPatch4 =
                rigController != null &&
                rigController.Patch4Enabled;
            bool repaired = false;

            if (patch4VisualRoot != null &&
                patch4VisualRoot.activeSelf != shouldShowPatch4)
            {
                patch4VisualRoot.SetActive(shouldShowPatch4);
                repaired = true;
            }

            if (patch35RollbackRoot != null &&
                patch35RollbackRoot.activeSelf == shouldShowPatch4)
            {
                patch35RollbackRoot.SetActive(!shouldShowPatch4);
                repaired = true;
            }

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
    }
}
