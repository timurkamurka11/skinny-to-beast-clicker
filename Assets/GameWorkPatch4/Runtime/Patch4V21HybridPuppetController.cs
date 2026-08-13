using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// v21 visible presentation for the existing Canvas-hosted room.
    /// The torso has its own localized soft mesh, while each arm and leg is one
    /// continuous painted sprite with narrow internal joint blending. The old
    /// broad full-body deformation and rigid internal cutouts are never rendered.
    /// </summary>
    [DefaultExecutionOrder(1245)]
    [DisallowMultipleComponent]
    public sealed class Patch4V21HybridPuppetController : MonoBehaviour
    {
        private const string VisualRootName = "Patch4VisualRoot";
        private const string GeneratedRootName = "GeneratedCanvasLayers";
        private const string LayerPrefix = "Layer.";

        private static readonly string[] AlwaysHiddenLayers =
        {
            "Body/TorsoBase",
            "Body/BellyFront",
            "Body/ChestSoft",
            "Head/EarL",
            "Head/EarR",
            "Face/BrowL",
            "Face/BrowR",
            "Face/EyeWhiteL",
            "Face/EyeWhiteR",
            "Face/IrisL",
            "Face/IrisR",
            "Face/Nose",
            "Face/MouthClosed",
            "Face/CheekL",
            "Face/CheekR",
            "ArmL/Forearm",
            "ArmL/Hand",
            "ArmR/Forearm",
            "ArmR/Hand",
            "LegL/Shin",
            "LegL/Foot",
            "LegR/Shin",
            "LegR/Foot",
            "Clothes/ShirtBellyOverlay",
            "Clothes/Bottoms",
            "Clothes/Shoes",
            "FX/ImpactFold"
        };

        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private Sprite torsoSprite;
        [SerializeField] private Sprite armLSprite;
        [SerializeField] private Sprite armRSprite;
        [SerializeField] private Sprite legLSprite;
        [SerializeField] private Sprite legRSprite;

        private Transform visualRoot;
        private Transform generatedRoot;
        private int appliedGeneratedRootId;
        private float facingSign = 1f;
        private Vector3 visualRootBindScale = Vector3.one;

        public bool IsApplied =>
            generatedRoot != null &&
            appliedGeneratedRootId == generatedRoot.GetInstanceID();

        public void SetFacingSign(int sign)
        {
            facingSign = sign < 0 ? -1f : 1f;
            ApplyFacing();
        }

        private void Reset()
        {
            rigController = GetComponent<Patch4CharacterRigController>();
        }

        private void Awake()
        {
            ResolveReferences();
            TryApplyWhenVisible();
        }

        private void OnEnable()
        {
            ResolveReferences();
            TryApplyWhenVisible();
        }

        private void Update()
        {
            TryApplyWhenVisible();
            ApplyFacing();
        }

        private void ResolveReferences()
        {
            if (rigController == null)
            {
                rigController = GetComponent<Patch4CharacterRigController>();
            }

            if (visualRoot == null)
            {
                visualRoot = transform.Find(VisualRootName);
            }

            Transform candidate = visualRoot != null
                ? visualRoot.Find(GeneratedRootName)
                : null;
            if (candidate != generatedRoot)
            {
                generatedRoot = candidate;
                appliedGeneratedRootId = 0;
                visualRootBindScale = visualRoot != null
                    ? visualRoot.localScale
                    : Vector3.one;
            }
        }

        private void ApplyFacing()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localScale = new Vector3(
                Mathf.Abs(visualRootBindScale.x) * facingSign,
                visualRootBindScale.y,
                visualRootBindScale.z);
        }

        private void TryApplyWhenVisible()
        {
            ResolveReferences();
            if (rigController == null ||
                visualRoot == null ||
                generatedRoot == null ||
                !visualRoot.gameObject.activeInHierarchy ||
                torsoSprite == null ||
                armLSprite == null ||
                armRSprite == null ||
                legLSprite == null ||
                legRSprite == null)
            {
                return;
            }

            int rootId = generatedRoot.GetInstanceID();
            if (appliedGeneratedRootId == rootId)
            {
                return;
            }

            Dictionary<string, Image> images = BuildImageMap();
            if (!images.TryGetValue("Clothes/ShirtBase", out Image torso) ||
                !images.TryGetValue("ArmL/Upper", out Image armL) ||
                !images.TryGetValue("ArmR/Upper", out Image armR) ||
                !images.TryGetValue("LegL/Thigh", out Image legL) ||
                !images.TryGetValue("LegR/Thigh", out Image legR) ||
                !images.TryGetValue("Body/Neck", out Image neck) ||
                !images.TryGetValue("Head/HeadBase", out Image head))
            {
                Debug.LogError(
                    "Patch 4 v21 hybrid puppet cannot find the required Canvas anchors.",
                    this);
                return;
            }

            for (int i = 0; i < AlwaysHiddenLayers.Length; i++)
            {
                SetActiveIfPresent(images, AlwaysHiddenLayers[i], false);
            }

            ConfigureTorsoLayer(torso, torsoSprite);
            ConfigureRigidLayer(neck, neck.sprite, "Neck", "V21/Neck");
            ConfigureRigidLayer(head, head.sprite, "Head", "V21/Head");

            ConfigureLimb(
                armL,
                armLSprite,
                Patch4HybridLimbDeformer.LimbProfile.LeftArm);
            ConfigureLimb(
                armR,
                armRSprite,
                Patch4HybridLimbDeformer.LimbProfile.RightArm);
            ConfigureLimb(
                legL,
                legLSprite,
                Patch4HybridLimbDeformer.LimbProfile.LeftLeg);
            ConfigureLimb(
                legR,
                legRSprite,
                Patch4HybridLimbDeformer.LimbProfile.RightLeg);

            // Proximal limb artwork lives behind the torso. The enlarged hidden
            // shoulder/hip overlap is concealed in neutral pose and only revealed
            // as needed during articulation instead of exposing a straight cut.
            MoveBefore(legL.transform.parent, torso.transform.parent);
            MoveBefore(legR.transform.parent, torso.transform.parent);
            MoveBefore(armL.transform.parent, torso.transform.parent);
            MoveBefore(armR.transform.parent, torso.transform.parent);

            SetActiveIfPresent(images, "FX/Shadow", true);

            appliedGeneratedRootId = rootId;
            Debug.Log(
                "Patch 4 v21 hybrid puppet applied: four continuous limbs use " +
                "localized three-bone deformation; the torso uses only its own " +
                "Spine/Pelvis/Belly mesh instead of whole-body weights; v20 rigid " +
                "internal segment cuts are not rendered.",
                this);
        }

        private void ConfigureTorsoLayer(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            image.gameObject.SetActive(true);
            DisableRigidCutout(image);
            DisableHybridLimb(image);

            Patch4CanvasSkinDeformer skin =
                image.GetComponent<Patch4CanvasSkinDeformer>();
            if (skin == null)
            {
                skin = image.gameObject.AddComponent<Patch4CanvasSkinDeformer>();
            }
            skin.enabled = true;
            skin.Configure(
                "V21/TorsoCore",
                sprite,
                rigController,
                new[]
                {
                    "SpineLower",
                    "SpineUpper",
                    "Pelvis",
                    "BellyBase",
                    "BellyTip"
                },
                24,
                36);
            skin.CaptureBindPose();
            image.SetVerticesDirty();
        }

        private void ConfigureRigidLayer(
            Image image,
            Sprite sprite,
            string boneName,
            string contractPath)
        {
            image.sprite = sprite;
            image.gameObject.SetActive(true);
            DisableRigidCutout(image);
            DisableHybridLimb(image);

            Patch4CanvasSkinDeformer skin =
                image.GetComponent<Patch4CanvasSkinDeformer>();
            if (skin == null)
            {
                skin = image.gameObject.AddComponent<Patch4CanvasSkinDeformer>();
            }
            skin.enabled = true;
            skin.Configure(
                contractPath,
                sprite,
                rigController,
                new[] { boneName },
                1,
                1);
            skin.CaptureBindPose();
            image.SetVerticesDirty();
        }

        private void ConfigureLimb(
            Image image,
            Sprite sprite,
            Patch4HybridLimbDeformer.LimbProfile profile)
        {
            image.sprite = sprite;
            image.gameObject.SetActive(true);

            Patch4CanvasSkinDeformer broad =
                image.GetComponent<Patch4CanvasSkinDeformer>();
            if (broad != null)
            {
                broad.enabled = false;
            }
            DisableRigidCutout(image);

            Patch4HybridLimbDeformer hybrid =
                image.GetComponent<Patch4HybridLimbDeformer>();
            if (hybrid == null)
            {
                hybrid = image.gameObject.AddComponent<Patch4HybridLimbDeformer>();
            }
            hybrid.enabled = true;
            hybrid.Configure(rigController, profile, sprite, 40, 80);
            image.SetVerticesDirty();
        }

        private static void DisableRigidCutout(Image image)
        {
            Patch4RigidCutoutDeformer rigid =
                image.GetComponent<Patch4RigidCutoutDeformer>();
            if (rigid != null)
            {
                rigid.enabled = false;
            }
        }

        private static void DisableHybridLimb(Image image)
        {
            Patch4HybridLimbDeformer hybrid =
                image.GetComponent<Patch4HybridLimbDeformer>();
            if (hybrid != null)
            {
                hybrid.enabled = false;
            }
        }

        private Dictionary<string, Image> BuildImageMap()
        {
            Dictionary<string, Image> result = new(StringComparer.Ordinal);
            Image[] images = generatedRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null ||
                    !image.gameObject.name.StartsWith(LayerPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                string dotted = image.gameObject.name.Substring(LayerPrefix.Length);
                result[dotted.Replace('.', '/')] = image;
            }
            return result;
        }

        private static void SetActiveIfPresent(
            IReadOnlyDictionary<string, Image> images,
            string path,
            bool active)
        {
            if (images.TryGetValue(path, out Image image) && image != null)
            {
                image.gameObject.SetActive(active);
            }
        }

        private static void MoveBefore(Transform moving, Transform reference)
        {
            if (moving == null || reference == null || moving.parent != reference.parent)
            {
                return;
            }
            int referenceIndex = reference.GetSiblingIndex();
            moving.SetSiblingIndex(Mathf.Max(0, referenceIndex));
        }
    }
}
