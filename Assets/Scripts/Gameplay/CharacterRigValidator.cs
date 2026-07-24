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

        public void Configure(
            CharacterRigController rig,
            CharacterSkinController skin)
        {
            rigController = rig;
            skinController = skin;
            ValidateNow(false);
        }

        public bool ValidateNow()
        {
            return ValidateNow(true);
        }

        [ContextMenu("Validate Character Rig")]
        private void ValidateFromContextMenu()
        {
            ValidateNow(true);
        }

        public bool ValidateNow(bool logSuccess)
        {
            StringBuilder errors = new StringBuilder();
            if (rigController == null)
            {
                errors.AppendLine("Rig controller is missing.");
            }
            else
            {
                foreach (string bone in RequiredBones)
                {
                    if (!rigController.HasBone(bone))
                    {
                        errors.AppendLine($"Missing bone: {bone}");
                    }
                }

                int textureCount = rigController.GetDistinctFrontTextureCount();
                if (textureCount > 1)
                {
                    errors.AppendLine(
                        $"More than one front skin texture is visible ({textureCount}).");
                }
            }

            if (skinController == null)
            {
                errors.AppendLine("Skin controller is missing.");
            }
            else
            {
                if (skinController.ActiveBaseSkinCount > 1)
                {
                    errors.AppendLine(
                        $"More than one base skin is active ({skinController.ActiveBaseSkinCount}).");
                }

                if (!skinController.ValidateSlotExclusivity(
                        out string slotError))
                {
                    errors.AppendLine(slotError);
                }

                foreach (CharacterSkinSlot slot in
                         System.Enum.GetValues(typeof(CharacterSkinSlot)))
                {
                    int active = skinController.GetActiveCount(slot);
                    if (active > 1)
                    {
                        errors.AppendLine(
                            $"Slot {slot} contains {active} active items.");
                    }
                }
            }

            if (errors.Length > 0)
            {
                if (!hasLoggedFailure)
                {
                    Debug.LogError($"Character rig validation failed:\n{errors}", this);
                    hasLoggedFailure = true;
                }

                return false;
            }

            hasLoggedFailure = false;
            if (logSuccess)
            {
                Debug.Log(
                    $"Character rig is valid: {rigController.BoneCount} bones, " +
                    "one active skin texture.",
                    this);
            }

            return true;
        }

        [ContextMenu("Run 50 Skin Swaps")]
        private void RunFiftySkinSwaps()
        {
            if (skinController == null || skinController.DefinitionCount <= 0)
            {
                Debug.LogError("Cannot run skin swap test: no skins configured.", this);
                return;
            }

            int original = Mathf.Max(0, skinController.CurrentArtIndex);
            for (int i = 0; i < 50; i++)
            {
                skinController.ApplySkin(
                    (original + i + 1) % skinController.DefinitionCount,
                    false);
                if (!ValidateNow(false))
                {
                    Debug.LogError(
                        $"Skin swap stress test failed on iteration {i + 1}.",
                        this);
                    skinController.ApplySkin(original, false);
                    return;
                }
            }

            skinController.ApplySkin(original, false);
            Debug.Log(
                "Skin swap stress test passed: 50 swaps, one Body item " +
                "and no duplicate visual slot remained active.",
                this);
        }
    }
}
