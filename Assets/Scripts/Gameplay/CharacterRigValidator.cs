using System.Text;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CharacterRigValidator : MonoBehaviour
    {
        private static readonly string[] RequiredBones =
        {
            "Bone.Pelvis",
            "Bone.Spine",
            "Bone.Chest",
            "Bone.Neck",
            "Bone.Head",
            "Bone.UpperArm.L",
            "Bone.Forearm.L",
            "Bone.Hand.L",
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
            else if (skinController.ActiveBaseSkinCount > 1)
            {
                errors.AppendLine(
                    $"More than one base skin is active ({skinController.ActiveBaseSkinCount}).");
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
    }
}
