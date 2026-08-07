using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// v20 visible-body presentation. The contract's intact master remains in
    /// the hierarchy for readiness/rollback checks, but when Patch 4 is actually
    /// visible this controller hides that deforming master and presents the
    /// exclusive anatomical cutouts as rigid pieces.
    /// </summary>
    [DefaultExecutionOrder(1240)]
    [DisallowMultipleComponent]
    public sealed class Patch4CutoutPuppetController : MonoBehaviour
    {
        private const string VisualRootName = "Patch4VisualRoot";
        private const string GeneratedRootName = "GeneratedCanvasLayers";
        private const string LayerPrefix = "Layer.";

        [Serializable]
        private sealed class PieceSpec
        {
            public string contractPath;
            public string boneName;
            public float rotationMultiplier;
            public float translationMultiplier;
            public float rotationLimit;

            public PieceSpec(
                string path,
                string bone,
                float rotation,
                float translation,
                float limit)
            {
                contractPath = path;
                boneName = bone;
                rotationMultiplier = rotation;
                translationMultiplier = translation;
                rotationLimit = limit;
            }
        }

        private static readonly PieceSpec[] PuppetPieces =
        {
            new("Clothes/ShirtBase", "SpineLower", .25f, 1f, 5f),
            new("Body/Neck", "Neck", .45f, 1f, 8f),
            new("Head/HeadBase", "Head", .60f, 1f, 10f),

            new("ArmL/Upper", "UpperArmL", .48f, 1f, 14f),
            new("ArmL/Forearm", "ForearmL", .45f, 1f, 16f),
            new("ArmL/Hand", "HandL", .60f, 1f, 12f),
            new("ArmR/Upper", "UpperArmR", .48f, 1f, 14f),
            new("ArmR/Forearm", "ForearmR", .45f, 1f, 16f),
            new("ArmR/Hand", "HandR", .60f, 1f, 12f),

            new("LegL/Thigh", "ThighL", .38f, 1f, 12f),
            new("LegL/Shin", "ShinL", .42f, 1f, 16f),
            new("LegL/Foot", "FootL", .50f, 1f, 14f),
            new("LegR/Thigh", "ThighR", .38f, 1f, 12f),
            new("LegR/Shin", "ShinR", .42f, 1f, 16f),
            new("LegR/Foot", "FootR", .50f, 1f, 14f)
        };

        private static readonly string[] NeutralFaceLayers =
        {
            "Face/BrowL",
            "Face/BrowR",
            "Face/EyeWhiteL",
            "Face/EyeWhiteR",
            "Face/IrisL",
            "Face/IrisR",
            "Face/Nose",
            "Face/MouthClosed"
        };

        [SerializeField] private Patch4CharacterRigController rigController;

        private Transform visualRoot;
        private Transform generatedRoot;
        private int appliedGeneratedRootId;

        public bool IsCutoutPuppetApplied =>
            generatedRoot != null &&
            appliedGeneratedRootId == generatedRoot.GetInstanceID();

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
            }
        }

        private void TryApplyWhenVisible()
        {
            ResolveReferences();
            if (rigController == null ||
                visualRoot == null ||
                !visualRoot.gameObject.activeInHierarchy ||
                generatedRoot == null)
            {
                return;
            }

            int rootId = generatedRoot.GetInstanceID();
            if (appliedGeneratedRootId == rootId)
            {
                return;
            }

            Dictionary<string, Image> images = BuildImageMap();
            if (!images.TryGetValue("Body/TorsoBase", out Image intactMaster))
            {
                return;
            }

            // The full master is the source of the old rubber/squash artefact.
            // Keep its contract component intact but never render it in v20.
            intactMaster.gameObject.SetActive(false);

            Patch4StableBodyCanvasDeformer stable =
                intactMaster.GetComponent<Patch4StableBodyCanvasDeformer>();
            if (stable != null)
            {
                stable.enabled = false;
            }

            for (int i = 0; i < PuppetPieces.Length; i++)
            {
                PieceSpec spec = PuppetPieces[i];
                if (!images.TryGetValue(spec.contractPath, out Image image))
                {
                    Debug.LogError(
                        "Patch 4 v20 cutout puppet is missing " +
                        spec.contractPath + ".",
                        this);
                    return;
                }

                image.gameObject.SetActive(true);
                Patch4CanvasSkinDeformer broadSkin =
                    image.GetComponent<Patch4CanvasSkinDeformer>();
                if (broadSkin != null)
                {
                    broadSkin.enabled = false;
                }

                Patch4RigidCutoutDeformer rigid =
                    image.GetComponent<Patch4RigidCutoutDeformer>();
                if (rigid == null)
                {
                    rigid = image.gameObject.AddComponent<
                        Patch4RigidCutoutDeformer>();
                }

                rigid.enabled = true;
                rigid.Configure(
                    rigController,
                    spec.boneName,
                    spec.rotationMultiplier,
                    spec.translationMultiplier,
                    spec.rotationLimit);
            }

            for (int i = 0; i < NeutralFaceLayers.Length; i++)
            {
                if (images.TryGetValue(NeutralFaceLayers[i], out Image face))
                {
                    face.gameObject.SetActive(true);
                }
            }

            if (images.TryGetValue("FX/Shadow", out Image shadow))
            {
                shadow.gameObject.SetActive(true);
            }

            // Reference-only pieces stay hidden so no body pixel is drawn twice.
            HideIfPresent(images, "Body/BellyFront");
            HideIfPresent(images, "Body/ChestSoft");
            HideIfPresent(images, "Head/EarL");
            HideIfPresent(images, "Head/EarR");
            HideIfPresent(images, "Face/CheekL");
            HideIfPresent(images, "Face/CheekR");
            HideIfPresent(images, "Clothes/ShirtBellyOverlay");
            HideIfPresent(images, "Clothes/Bottoms");
            HideIfPresent(images, "Clothes/Shoes");
            HideIfPresent(images, "FX/ImpactFold");

            appliedGeneratedRootId = rootId;
            Debug.Log(
                "Patch 4 v20 cutout puppet applied: intact master hidden; " +
                "torso/head/arms/legs are rigid pieces with bounded rotation. " +
                "No visible layer uses broad linear-blend skinning.",
                this);
        }

        private Dictionary<string, Image> BuildImageMap()
        {
            Dictionary<string, Image> result =
                new(StringComparer.Ordinal);
            Image[] images = generatedRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null ||
                    !image.gameObject.name.StartsWith(
                        LayerPrefix,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                string dotted = image.gameObject.name.Substring(LayerPrefix.Length);
                string contractPath = dotted.Replace('.', '/');
                result[contractPath] = image;
            }

            return result;
        }

        private static void HideIfPresent(
            IReadOnlyDictionary<string, Image> images,
            string path)
        {
            if (images.TryGetValue(path, out Image image) && image != null)
            {
                image.gameObject.SetActive(false);
            }
        }
    }
}
