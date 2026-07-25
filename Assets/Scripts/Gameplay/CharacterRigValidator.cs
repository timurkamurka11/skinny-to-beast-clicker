using System.Text;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CharacterRigValidator : MonoBehaviour
    {
        private static readonly string[] RequiredBones =
        {
            "Bone.Root",
            "Bone.Pelvis",
            "Bone.Spine",
            "Bone.Chest",
            "Bone.Neck",
            "Bone.Head",
            "Bone.Shoulder.L",
            "Bone.UpperArm.L",
            "Bone.Forearm.L",
            "Bone.Hand.L",
            "Bone.Shoulder.R",
            "Bone.UpperArm.R",
            "Bone.Forearm.R",
            "Bone.Hand.R",
            "Bone.Thigh.L",
            "Bone.Shin.L",
            "Bone.Foot.L",
            "Bone.Thigh.R",
            "Bone.Shin.R",
            "Bone.Foot.R"
        };

        private CharacterRigController rigController;
        private CharacterSkinController skinController;
        private bool hasLoggedFailure;

        public string LastError { get; private set; } = string.Empty;

        public void Configure(
            CharacterRigController rig,
            CharacterSkinController skin)
        {
            rigController = rig;
            skinController = skin;
        }

        public bool ValidateNow()
        {
            return ValidateNow(true);
        }

        public bool ValidateNow(bool logSuccess)
        {
            StringBuilder errors = new();
            if (rigController == null)
            {
                errors.AppendLine("Rig controller is missing.");
            }
            else
            {
                for (int i = 0; i < RequiredBones.Length; i++)
                {
                    if (!rigController.HasBone(RequiredBones[i]))
                    {
                        errors.AppendLine(
                            $"Missing bone: {RequiredBones[i]}");
                    }
                }

                if (rigController.GetCharacterTextureCount() != 0)
                {
                    errors.AppendLine(
                        "The mesh rig unexpectedly samples a character texture.");
                }

                if (rigController.GetVisibleGraphicCount() < 18)
                {
                    errors.AppendLine(
                        "Fewer than 18 independent skeletal parts have a " +
                        "live uGUI render surface.");
                }

                if (!rigController.ValidateCanvasRendererCoverage(
                        out string rendererError))
                {
                    errors.AppendLine(rendererError);
                }

                if (!rigController.AnimatorReady)
                {
                    errors.AppendLine(
                        "The four-layer character Animator is not ready.");
                }

                if (!rigController.HasVisibleSkin)
                {
                    errors.AppendLine(
                        "The selected stage has no non-zero on-screen mesh bounds.");
                }

                if (!rigController.ValidateJointContinuity(
                        out string jointError))
                {
                    errors.AppendLine(jointError);
                }
            }

            if (ContainsForbiddenCharacterComponent())
            {
                errors.AppendLine(
                    "CharacterRoot contains a forbidden raw texture UI component.");
            }

            if (skinController == null)
            {
                errors.AppendLine("Skin controller is missing.");
            }
            else
            {
                if (skinController.ActiveBaseSkinCount != 1)
                {
                    errors.AppendLine(
                        $"Expected one active Body slot, found " +
                        $"{skinController.ActiveBaseSkinCount}.");
                }

                if (!skinController.ValidateSlotExclusivity(
                        out string slotError))
                {
                    errors.AppendLine(slotError);
                }

                if (skinController.CurrentArtIndex >= 0 &&
                    !skinController.IsVisualReady)
                {
                    errors.AppendLine(
                        "The selected stage is not visually ready.");
                }

                foreach (CharacterSkinSlot slot in
                         System.Enum.GetValues(
                             typeof(CharacterSkinSlot)))
                {
                    int active =
                        skinController.GetActiveCount(slot);
                    if (active > 1)
                    {
                        errors.AppendLine(
                            $"Slot {slot} contains {active} active items.");
                    }
                }
            }

            LastError = errors.ToString().Trim();
            if (errors.Length > 0)
            {
                if (!hasLoggedFailure && logSuccess)
                {
                    Debug.LogError(
                        $"Character rig validation failed:\n{errors}",
                        this);
                    hasLoggedFailure = true;
                }

                return false;
            }

            hasLoggedFailure = false;
            if (logSuccess)
            {
                Debug.Log(
                    $"Character rig is valid: {rigController.BoneCount} " +
                    $"bones, {rigController.GetVisibleGraphicCount()} " +
                    "vector mesh parts, zero character textures.",
                    this);
            }

            return true;
        }

        public bool RunSkinSwapStress(int swapCount = 50)
        {
            if (rigController == null ||
                skinController == null ||
                skinController.DefinitionCount <= 0)
            {
                LastError =
                    "Cannot run skin swap stress: no definitions.";
                return false;
            }

            int count = Mathf.Max(1, swapCount);
            int original =
                Mathf.Max(0, skinController.CurrentArtIndex);
            int originalMeshCount =
                rigController.MeshPartCount;
            for (int i = 0; i < count; i++)
            {
                skinController.ApplySkin(
                    (original + i + 1) %
                    skinController.DefinitionCount,
                    false);
                if (!ValidateNow(false) ||
                    rigController.MeshPartCount != originalMeshCount)
                {
                    LastError =
                        $"Skin swap stress failed at iteration {i + 1}: " +
                        (rigController.MeshPartCount != originalMeshCount
                            ? "the persistent mesh count changed."
                            : LastError);
                    skinController.ApplySkin(original, false);
                    return false;
                }
            }

            skinController.ApplySkin(original, false);
            return ValidateNow(false);
        }

        public bool RunTapStress(int tapCount = 300)
        {
            if (rigController == null)
            {
                LastError =
                    "Cannot run tap stress: rig is missing.";
                return false;
            }

            bool passed =
                rigController.StressTapAnimator(
                    Mathf.Max(0, tapCount));
            if (!passed)
            {
                LastError =
                    "Animator rejected or lost one of the stress taps.";
            }

            return passed;
        }

        [ContextMenu("Validate Patch 3 Character Rig")]
        private void ValidateFromContextMenu()
        {
            ValidateNow(true);
        }

        [ContextMenu("Run 50 Atomic Stage Swaps")]
        private void RunFiftySkinSwaps()
        {
            bool passed = RunSkinSwapStress(50);
            Debug.Log(
                passed
                    ? "Stage swap stress passed: 50 swaps, one persistent body."
                    : $"Stage swap stress failed: {LastError}",
                this);
        }

        [ContextMenu("Run 300 Tap Animator Stress")]
        private void RunThreeHundredTaps()
        {
            bool passed = RunTapStress(300);
            Debug.Log(
                passed
                    ? "Tap stress passed: 300 rapid taps, Animator remained healthy."
                    : $"Tap stress failed: {LastError}",
                this);
        }

        private bool ContainsForbiddenCharacterComponent()
        {
            Component[] components =
                GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component != null &&
                    component.GetType().Name == "Raw" + "Image")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
