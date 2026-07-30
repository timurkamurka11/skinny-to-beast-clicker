using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Canvas-compatible equivalent of Sprite Skin for the painted Patch 4
    /// layers. UI.Image is the production presentation in LivingGameplayScene,
    /// so the usual SpriteRenderer-only SpriteSkin cannot deform the visible
    /// character. This mesh effect builds a deterministic grid and applies
    /// distance-painted weights from the Patch 4 bones to every vertex.
    /// </summary>
    [DefaultExecutionOrder(1210)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class Patch4CanvasSkinDeformer : BaseMeshEffect
    {
        [Serializable]
        private sealed class BoneBinding
        {
            public string boneName = string.Empty;
            public Transform bone;
            [Min(0.01f)] public float weightBias = 1f;

            [NonSerialized] public Matrix4x4 bindPose;
            [NonSerialized] public Vector2 bindPixel;
        }

        private const float MinimumDimension = 0.001f;
        private const float MinimumDistancePixels = 12f;
        private const float MinimumWeight = 0.000000000001f;

        [SerializeField] private string contractPath = string.Empty;
        [SerializeField] private Sprite sourceSprite;
        [SerializeField, Range(1, 16)] private int gridColumns = 8;
        [SerializeField, Range(1, 24)] private int gridRows = 12;
        [SerializeField, Range(1.25f, 4f)]
        private float weightFalloff = 2.35f;
        [SerializeField] private List<BoneBinding> boneBindings = new();

        [NonSerialized] private bool bindPoseCaptured;
        [NonSerialized] private Matrix4x4[] skinMatrices =
            Array.Empty<Matrix4x4>();

        public string ContractPath => contractPath;
        public int BoneCount => boneBindings.Count;
        public bool HasMultipleBoneWeights => boneBindings.Count > 1;
        public bool IsBound => bindPoseCaptured && boneBindings.Count > 0;
        public int ExpectedVertexCount
        {
            get
            {
                int columns = HasMultipleBoneWeights ? gridColumns : 1;
                int rows = HasMultipleBoneWeights ? gridRows : 1;
                return (columns + 1) * (rows + 1);
            }
        }

        public void Configure(
            string layerContractPath,
            Sprite sprite,
            Patch4CharacterRigController rigController,
            IReadOnlyList<string> influenceBoneNames,
            int columns,
            int rows)
        {
            contractPath = layerContractPath ?? string.Empty;
            sourceSprite = sprite;
            gridColumns = Mathf.Clamp(columns, 1, 16);
            gridRows = Mathf.Clamp(rows, 1, 24);
            boneBindings.Clear();
            bindPoseCaptured = false;
            skinMatrices = Array.Empty<Matrix4x4>();

            if (rigController == null || influenceBoneNames == null)
            {
                return;
            }

            for (int i = 0; i < influenceBoneNames.Count; i++)
            {
                string boneName = influenceBoneNames[i];
                if (string.IsNullOrWhiteSpace(boneName))
                {
                    continue;
                }

                Transform bone = rigController.GetBone(boneName);
                if (bone == null)
                {
                    continue;
                }

                boneBindings.Add(new BoneBinding
                {
                    boneName = boneName,
                    bone = bone,
                    weightBias = i == 0 ? 1.18f : 1f
                });
            }

            skinMatrices = new Matrix4x4[boneBindings.Count];
            SetVerticesDirty();
        }

        public bool CaptureBindPose()
        {
            bindPoseCaptured = false;
            RectTransform imageTransform =
                graphic != null ? graphic.rectTransform : null;
            Sprite sprite = ResolveSprite();
            if (imageTransform == null ||
                sprite == null ||
                boneBindings.Count == 0)
            {
                return false;
            }

            Rect rect = imageTransform.rect;
            if (Mathf.Abs(rect.width) < MinimumDimension ||
                Mathf.Abs(rect.height) < MinimumDimension)
            {
                return false;
            }

            for (int i = 0; i < boneBindings.Count; i++)
            {
                BoneBinding binding = boneBindings[i];
                if (binding == null || binding.bone == null)
                {
                    return false;
                }

                binding.bindPose =
                    binding.bone.worldToLocalMatrix *
                    imageTransform.localToWorldMatrix;
                Vector3 localBone =
                    imageTransform.InverseTransformPoint(
                        binding.bone.position);
                binding.bindPixel = LocalToSpritePixel(
                    localBone,
                    rect,
                    sprite);
            }

            bindPoseCaptured = true;
            SetVerticesDirty();
            return true;
        }

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || vertexHelper == null)
            {
                return;
            }

            Sprite sprite = ResolveSprite();
            RectTransform imageTransform =
                graphic != null ? graphic.rectTransform : null;
            if (sprite == null ||
                imageTransform == null ||
                boneBindings.Count == 0)
            {
                return;
            }

            if (!bindPoseCaptured && !CaptureBindPose())
            {
                return;
            }

            int columns = HasMultipleBoneWeights ? gridColumns : 1;
            int rows = HasMultipleBoneWeights ? gridRows : 1;
            Rect rect = imageTransform.rect;
            Vector4 outerUv = DataUtility.GetOuterUV(sprite);
            Color32 color = graphic.color;

            if (skinMatrices == null ||
                skinMatrices.Length != boneBindings.Count)
            {
                skinMatrices =
                    new Matrix4x4[boneBindings.Count];
            }

            for (int i = 0; i < boneBindings.Count; i++)
            {
                BoneBinding binding = boneBindings[i];
                skinMatrices[i] =
                    imageTransform.worldToLocalMatrix *
                    binding.bone.localToWorldMatrix *
                    binding.bindPose;
            }

            vertexHelper.Clear();
            for (int row = 0; row <= rows; row++)
            {
                float v = row / (float)rows;
                float y = Mathf.Lerp(rect.yMin, rect.yMax, v);
                for (int column = 0; column <= columns; column++)
                {
                    float u = column / (float)columns;
                    float x = Mathf.Lerp(rect.xMin, rect.xMax, u);
                    Vector3 original = new(x, y, 0f);
                    Vector2 spritePixel = new(
                        u * sprite.rect.width,
                        v * sprite.rect.height);
                    Vector3 deformed = DeformVertex(
                        original,
                        spritePixel,
                        skinMatrices);

                    UIVertex vertex = UIVertex.simpleVert;
                    vertex.position = deformed;
                    vertex.color = color;
                    vertex.uv0 = new Vector2(
                        Mathf.Lerp(outerUv.x, outerUv.z, u),
                        Mathf.Lerp(outerUv.y, outerUv.w, v));
                    vertexHelper.AddVert(vertex);
                }
            }

            int stride = columns + 1;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int lowerLeft = row * stride + column;
                    int lowerRight = lowerLeft + 1;
                    int upperLeft = lowerLeft + stride;
                    int upperRight = upperLeft + 1;
                    vertexHelper.AddTriangle(
                        lowerLeft,
                        upperLeft,
                        upperRight);
                    vertexHelper.AddTriangle(
                        lowerLeft,
                        upperRight,
                        lowerRight);
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!bindPoseCaptured)
            {
                CaptureBindPose();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        private void LateUpdate()
        {
            if (bindPoseCaptured &&
                HasMultipleBoneWeights &&
                graphic != null &&
                graphic.gameObject.activeInHierarchy)
            {
                graphic.SetVerticesDirty();
            }
        }

        private Vector3 DeformVertex(
            Vector3 original,
            Vector2 spritePixel,
            IReadOnlyList<Matrix4x4> skinMatrices)
        {
            if (boneBindings.Count == 1)
            {
                return skinMatrices[0].MultiplyPoint3x4(original);
            }

            float totalWeight = 0f;
            Vector3 result = Vector3.zero;
            for (int i = 0; i < boneBindings.Count; i++)
            {
                BoneBinding binding = boneBindings[i];
                float distance = Mathf.Max(
                    MinimumDistancePixels,
                    Vector2.Distance(spritePixel, binding.bindPixel));
                float weight = binding.weightBias /
                    Mathf.Pow(distance, weightFalloff);
                totalWeight += weight;
                result +=
                    skinMatrices[i].MultiplyPoint3x4(original) *
                    weight;
            }

            return totalWeight > MinimumWeight
                ? result / totalWeight
                : original;
        }

        private Sprite ResolveSprite()
        {
            Image image = graphic as Image;
            if (image != null)
            {
                Sprite current =
                    image.overrideSprite != null
                        ? image.overrideSprite
                        : image.sprite;
                if (current != null)
                {
                    return current;
                }
            }

            return sourceSprite;
        }

        private static Vector2 LocalToSpritePixel(
            Vector3 local,
            Rect rect,
            Sprite sprite)
        {
            float u = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
            float v = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);
            return new Vector2(
                u * sprite.rect.width,
                v * sprite.rect.height);
        }

        private void SetVerticesDirty()
        {
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }
    }
}
