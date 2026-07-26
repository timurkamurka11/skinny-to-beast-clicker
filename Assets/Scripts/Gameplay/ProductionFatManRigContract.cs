using System;
using System.Text;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Contract for the real authored fat-man character.
    ///
    /// This component belongs on a prefab created with Unity 2D Animation from
    /// a layered PSB/PSD source. It deliberately does not reuse
    /// CharacterRigController bones or any runtime-cut PNG pieces.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProductionFatManRigContract : MonoBehaviour
    {
        public const string ResourcePath =
            "Characters/FatManProduction/FatManRig";

        [Header("Core")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform skeletonRoot;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteSkin[] spriteSkins;
        [SerializeField] private SpriteRenderer[] spriteRenderers;

        [Header("Views")]
        [SerializeField] private GameObject frontView;
        [SerializeField] private GameObject sideView;
        [SerializeField] private GameObject backView;

        [Header("Required authored bones")]
        [SerializeField] private Transform pelvis;
        [SerializeField] private Transform spine;
        [SerializeField] private Transform chest;
        [SerializeField] private Transform belly;
        [SerializeField] private Transform shirtHem;
        [SerializeField] private Transform neck;
        [SerializeField] private Transform head;
        [SerializeField] private Transform chinSoft;
        [SerializeField] private Transform upperArmLeft;
        [SerializeField] private Transform forearmLeft;
        [SerializeField] private Transform handLeft;
        [SerializeField] private Transform upperArmRight;
        [SerializeField] private Transform forearmRight;
        [SerializeField] private Transform handRight;
        [SerializeField] private Transform thighLeft;
        [SerializeField] private Transform shinLeft;
        [SerializeField] private Transform footLeft;
        [SerializeField] private Transform thighRight;
        [SerializeField] private Transform shinRight;
        [SerializeField] private Transform footRight;

        private static readonly int FacingHash =
            Animator.StringToHash("Facing");
        private static readonly int StageHash =
            Animator.StringToHash("Stage");
        private static readonly int SpeedHash =
            Animator.StringToHash("Speed");
        private static readonly int WalkingHash =
            Animator.StringToHash("Walking");
        private static readonly int TapHash =
            Animator.StringToHash("Tap");
        private static readonly int YawnHash =
            Animator.StringToHash("Yawn");
        private static readonly int ScratchHash =
            Animator.StringToHash("Scratch");
        private static readonly int StretchHash =
            Animator.StringToHash("Stretch");
        private static readonly int FlexHash =
            Animator.StringToHash("Flex");
        private static readonly int SitHash =
            Animator.StringToHash("Sit");
        private static readonly int StandHash =
            Animator.StringToHash("Stand");
        private static readonly int UpgradeHash =
            Animator.StringToHash("Upgrade");

        public Animator Animator => animator;
        public Transform SkeletonRoot => skeletonRoot;
        public Transform VisualRoot => visualRoot;
        public SpriteSkin[] SpriteSkins => spriteSkins;
        public SpriteRenderer[] SpriteRenderers => spriteRenderers;

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>(true);
            skeletonRoot = FindByName(transform, "Skeleton");
            visualRoot = FindByName(transform, "Visual");
            spriteSkins = GetComponentsInChildren<SpriteSkin>(true);
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

            frontView = FindObjectByName(transform, "FrontView");
            sideView = FindObjectByName(transform, "SideView");
            backView = FindObjectByName(transform, "BackView");

            pelvis = FindByName(transform, "Pelvis");
            spine = FindByName(transform, "Spine");
            chest = FindByName(transform, "Chest");
            belly = FindByName(transform, "Belly");
            shirtHem = FindByName(transform, "ShirtHem");
            neck = FindByName(transform, "Neck");
            head = FindByName(transform, "Head");
            chinSoft = FindByName(transform, "ChinSoft");
            upperArmLeft = FindByName(transform, "UpperArm_L");
            forearmLeft = FindByName(transform, "Forearm_L");
            handLeft = FindByName(transform, "Hand_L");
            upperArmRight = FindByName(transform, "UpperArm_R");
            forearmRight = FindByName(transform, "Forearm_R");
            handRight = FindByName(transform, "Hand_R");
            thighLeft = FindByName(transform, "Thigh_L");
            shinLeft = FindByName(transform, "Shin_L");
            footLeft = FindByName(transform, "Foot_L");
            thighRight = FindByName(transform, "Thigh_R");
            shinRight = FindByName(transform, "Shin_R");
            footRight = FindByName(transform, "Foot_R");
        }

        public bool Validate(out string error)
        {
            StringBuilder issues = new StringBuilder();

            Require(animator, "Animator", issues);
            Require(skeletonRoot, "Skeleton root", issues);
            Require(visualRoot, "Visual root", issues);
            Require(frontView, "FrontView", issues);
            Require(sideView, "SideView", issues);
            Require(backView, "BackView", issues);

            Require(pelvis, "Pelvis", issues);
            Require(spine, "Spine", issues);
            Require(chest, "Chest", issues);
            Require(belly, "Belly", issues);
            Require(shirtHem, "ShirtHem", issues);
            Require(neck, "Neck", issues);
            Require(head, "Head", issues);
            Require(chinSoft, "ChinSoft", issues);
            Require(upperArmLeft, "UpperArm_L", issues);
            Require(forearmLeft, "Forearm_L", issues);
            Require(handLeft, "Hand_L", issues);
            Require(upperArmRight, "UpperArm_R", issues);
            Require(forearmRight, "Forearm_R", issues);
            Require(handRight, "Hand_R", issues);
            Require(thighLeft, "Thigh_L", issues);
            Require(shinLeft, "Shin_L", issues);
            Require(footLeft, "Foot_L", issues);
            Require(thighRight, "Thigh_R", issues);
            Require(shinRight, "Shin_R", issues);
            Require(footRight, "Foot_R", issues);

            if (animator != null && animator.runtimeAnimatorController == null)
            {
                issues.AppendLine("Animator has no RuntimeAnimatorController.");
            }

            if (spriteSkins == null || spriteSkins.Length == 0)
            {
                issues.AppendLine("No SpriteSkin components were assigned.");
            }
            else
            {
                for (int i = 0; i < spriteSkins.Length; i++)
                {
                    SpriteSkin skin = spriteSkins[i];
                    if (skin == null)
                    {
                        issues.AppendLine($"SpriteSkin[{i}] is null.");
                        continue;
                    }

                    if (skin.boneTransforms == null ||
                        skin.boneTransforms.Length == 0)
                    {
                        issues.AppendLine(
                            $"SpriteSkin '{skin.name}' has no bound bones.");
                    }
                }
            }

            if (spriteRenderers == null || spriteRenderers.Length < 8)
            {
                issues.AppendLine(
                    "Fewer than eight authored SpriteRenderer layers were assigned.");
            }

            error = issues.ToString().Trim();
            return error.Length == 0;
        }

        public void SetFacing(CharacterFacing facing)
        {
            int value = facing == CharacterFacing.Back
                ? 2
                : facing == CharacterFacing.SideLeft ||
                  facing == CharacterFacing.SideRight
                    ? 1
                    : 0;

            SetViewActive(frontView, value == 0);
            SetViewActive(sideView, value == 1);
            SetViewActive(backView, value == 2);

            if (sideView != null)
            {
                Vector3 scale = sideView.transform.localScale;
                scale.x = Mathf.Abs(scale.x) *
                          (facing == CharacterFacing.SideLeft ? -1f : 1f);
                sideView.transform.localScale = scale;
            }

            SetIntegerIfPresent(FacingHash, value);
        }

        public void SetStage(int stage)
        {
            SetIntegerIfPresent(StageHash, Mathf.Clamp(stage, 0, 3));
        }

        public void SetLocomotion(bool walking, float speed)
        {
            SetBoolIfPresent(WalkingHash, walking);
            SetFloatIfPresent(SpeedHash, Mathf.Max(0f, speed));
        }

        public void FireAction(CharacterRoutineAction action)
        {
            int trigger = action switch
            {
                CharacterRoutineAction.Yawn => YawnHash,
                CharacterRoutineAction.Scratch => ScratchHash,
                CharacterRoutineAction.Stretch => StretchHash,
                CharacterRoutineAction.Flex => FlexHash,
                CharacterRoutineAction.Sit => SitHash,
                CharacterRoutineAction.Stand => StandHash,
                _ => 0
            };

            if (trigger != 0)
            {
                SetTriggerIfPresent(trigger);
            }
        }

        public void FireTap()
        {
            SetTriggerIfPresent(TapHash);
        }

        public void FireUpgrade()
        {
            SetTriggerIfPresent(UpgradeHash);
        }

        private void SetIntegerIfPresent(int hash, int value)
        {
            if (HasParameter(hash, AnimatorControllerParameterType.Int))
            {
                animator.SetInteger(hash, value);
            }
        }

        private void SetFloatIfPresent(int hash, float value)
        {
            if (HasParameter(hash, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(hash, value);
            }
        }

        private void SetBoolIfPresent(int hash, bool value)
        {
            if (HasParameter(hash, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(hash, value);
            }
        }

        private void SetTriggerIfPresent(int hash)
        {
            if (HasParameter(hash, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(hash);
            }
        }

        private bool HasParameter(
            int hash,
            AnimatorControllerParameterType type)
        {
            if (animator == null)
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == hash &&
                    parameters[i].type == type)
                {
                    return true;
                }
            }
            return false;
        }

        private static void SetViewActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static void Require(
            UnityEngine.Object target,
            string label,
            StringBuilder issues)
        {
            if (target == null)
            {
                issues.AppendLine(label + " is missing.");
            }
        }

        private static Transform FindByName(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (string.Equals(all[i].name, name,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return all[i];
                }
            }
            return null;
        }

        private static GameObject FindObjectByName(
            Transform root,
            string name)
        {
            Transform found = FindByName(root, name);
            return found != null ? found.gameObject : null;
        }
    }
}
