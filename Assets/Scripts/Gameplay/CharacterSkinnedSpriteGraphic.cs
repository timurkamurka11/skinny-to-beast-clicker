using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    public enum FatManSkinBone
    {
        Root,
        Pelvis,
        Spine,
        Chest,
        Belly,
        ShirtHem,
        ChestSoft,
        Neck,
        Head,
        ChinSoft,
        ShoulderLeft,
        UpperArmLeft,
        ForearmLeft,
        HandLeft,
        ShoulderRight,
        UpperArmRight,
        ForearmRight,
        HandRight,
        ThighLeft,
        ShinLeft,
        FootLeft,
        ThighRight,
        ShinRight,
        FootRight
    }

    /// <summary>
    /// A textured, weighted 2D mesh driven by the same RectTransform bones as
    /// CharacterRigController. Unlike the 3.2 rectangular cutout attempt, the
    /// painted body remains one continuous surface, so joints cannot duplicate
    /// neighbouring limbs. Unlike the 3.3 flat Image, every weighted region
    /// follows the animated skeleton, including arms, legs, chest and soft body.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CharacterSkinnedSpriteGraphic : MaskableGraphic
    {
        private const int GridColumns = 24;
        private const int GridRows = 38;
        private const int InfluenceCount = 4;

        private readonly Matrix4x4[] bindInverse =
            new Matrix4x4[Enum.GetValues(typeof(FatManSkinBone)).Length];
        private readonly Matrix4x4[] boneDeltas =
            new Matrix4x4[Enum.GetValues(typeof(FatManSkinBone)).Length];

        private Texture2D sourceTexture;
        private Rect normalizedUv;
        private RectTransform referenceRoot;
        private RectTransform[] bones;
        private int[] influenceIndices;
        private float[] influenceWeights;
        private CharacterFacing facing;
        private bool hasBindPose;
        private bool configured;

        public override Texture mainTexture =>
            sourceTexture != null ? sourceTexture : s_WhiteTexture;

        public bool IsReady =>
            configured &&
            sourceTexture != null &&
            referenceRoot != null &&
            bones != null &&
            hasBindPose;

        public float DeformationMagnitude { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            maskable = false;
        }

        public void Configure(
            Texture2D texture,
            RectInt pixelBounds,
            RectTransform root,
            RectTransform[] mappedBones,
            CharacterFacing initialFacing)
        {
            sourceTexture = texture;
            referenceRoot = root;
            bones = mappedBones;
            color = Color.white;
            raycastTarget = false;
            maskable = false;
            facing = initialFacing;
            SetView(pixelBounds, initialFacing);
            BuildInfluenceMap();
            CaptureBindPose();
            configured = sourceTexture != null &&
                         referenceRoot != null &&
                         bones != null;
            SetAllDirty();
        }

        public void SetView(
            RectInt pixelBounds,
            CharacterFacing nextFacing)
        {
            facing = nextFacing;
            if (sourceTexture == null ||
                pixelBounds.width < 2 ||
                pixelBounds.height < 2)
            {
                normalizedUv = new Rect(0f, 0f, 1f, 1f);
                return;
            }

            normalizedUv = new Rect(
                pixelBounds.x / (float)sourceTexture.width,
                pixelBounds.y / (float)sourceTexture.height,
                pixelBounds.width / (float)sourceTexture.width,
                pixelBounds.height / (float)sourceTexture.height);
            BuildInfluenceMap();
            SetVerticesDirty();
            SetMaterialDirty();
        }

        public void CaptureBindPose()
        {
            if (referenceRoot == null || bones == null)
            {
                hasBindPose = false;
                return;
            }

            Matrix4x4 rootWorldToLocal = referenceRoot.worldToLocalMatrix;
            for (int i = 0; i < bindInverse.Length; i++)
            {
                RectTransform bone =
                    i < bones.Length ? bones[i] : null;
                Matrix4x4 bind = bone != null
                    ? rootWorldToLocal * bone.localToWorldMatrix
                    : Matrix4x4.identity;
                bindInverse[i] = bind.inverse;
                boneDeltas[i] = Matrix4x4.identity;
            }

            hasBindPose = true;
            SetVerticesDirty();
        }

        public void RefreshDeformation()
        {
            if (!IsReady)
            {
                return;
            }

            Matrix4x4 rootWorldToLocal = referenceRoot.worldToLocalMatrix;
            for (int i = 0; i < boneDeltas.Length; i++)
            {
                RectTransform bone =
                    i < bones.Length ? bones[i] : null;
                boneDeltas[i] = bone != null
                    ? rootWorldToLocal *
                      bone.localToWorldMatrix *
                      bindInverse[i]
                    : Matrix4x4.identity;
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (!IsReady)
            {
                return;
            }

            int vertexColumns = GridColumns + 1;
            int vertexRows = GridRows + 1;
            int vertexCount = vertexColumns * vertexRows;
            EnsureInfluenceArrays(vertexCount);

            Rect rect = rectTransform.rect;
            Matrix4x4 localToReference =
                referenceRoot.worldToLocalMatrix *
                rectTransform.localToWorldMatrix;
            Matrix4x4 referenceToLocal =
                rectTransform.worldToLocalMatrix *
                referenceRoot.localToWorldMatrix;

            float totalDisplacement = 0f;
            int displacementSamples = 0;
            Color32 vertexColor = color;

            for (int row = 0; row < vertexRows; row++)
            {
                float ny = row / (float)GridRows;
                float localY = Mathf.Lerp(rect.yMin, rect.yMax, ny);
                float uvY = Mathf.Lerp(
                    normalizedUv.yMin,
                    normalizedUv.yMax,
                    ny);

                for (int column = 0; column < vertexColumns; column++)
                {
                    float nx = column / (float)GridColumns;
                    float localX = Mathf.Lerp(rect.xMin, rect.xMax, nx);
                    float uvX = Mathf.Lerp(
                        normalizedUv.xMin,
                        normalizedUv.xMax,
                        nx);

                    int vertexIndex = row * vertexColumns + column;
                    Vector3 baseLocal =
                        new Vector3(localX, localY, 0f);
                    Vector3 baseReference =
                        localToReference.MultiplyPoint3x4(baseLocal);
                    Vector3 deformedReference = Vector3.zero;
                    float accumulatedWeight = 0f;
                    int influenceOffset =
                        vertexIndex * InfluenceCount;

                    for (int influence = 0;
                         influence < InfluenceCount;
                         influence++)
                    {
                        int offset = influenceOffset + influence;
                        float weight = influenceWeights[offset];
                        if (weight <= 0.0001f)
                        {
                            continue;
                        }

                        int boneIndex = Mathf.Clamp(
                            influenceIndices[offset],
                            0,
                            boneDeltas.Length - 1);
                        deformedReference +=
                            boneDeltas[boneIndex].MultiplyPoint3x4(
                                baseReference) * weight;
                        accumulatedWeight += weight;
                    }

                    if (accumulatedWeight <= 0.0001f)
                    {
                        deformedReference = baseReference;
                    }
                    else if (Mathf.Abs(accumulatedWeight - 1f) > 0.0001f)
                    {
                        deformedReference /= accumulatedWeight;
                    }

                    Vector3 deformedLocal =
                        referenceToLocal.MultiplyPoint3x4(
                            deformedReference);
                    totalDisplacement += Vector3.Distance(
                        baseReference,
                        deformedReference);
                    displacementSamples++;

                    vertexHelper.AddVert(
                        deformedLocal,
                        vertexColor,
                        new Vector2(uvX, uvY));
                }
            }

            for (int row = 0; row < GridRows; row++)
            {
                for (int column = 0;
                     column < GridColumns;
                     column++)
                {
                    int bottomLeft =
                        row * vertexColumns + column;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = bottomLeft + vertexColumns;
                    int topRight = topLeft + 1;

                    vertexHelper.AddTriangle(
                        bottomLeft,
                        topLeft,
                        topRight);
                    vertexHelper.AddTriangle(
                        bottomLeft,
                        topRight,
                        bottomRight);
                }
            }

            DeformationMagnitude = displacementSamples > 0
                ? totalDisplacement / displacementSamples
                : 0f;
        }

        private void BuildInfluenceMap()
        {
            int vertexCount =
                (GridColumns + 1) * (GridRows + 1);
            EnsureInfluenceArrays(vertexCount);

            bool side =
                facing == CharacterFacing.SideLeft ||
                facing == CharacterFacing.SideRight;
            int vertexColumns = GridColumns + 1;

            for (int row = 0; row <= GridRows; row++)
            {
                float ny = row / (float)GridRows;
                for (int column = 0;
                     column <= GridColumns;
                     column++)
                {
                    float nx = column / (float)GridColumns;
                    InfluenceAccumulator accumulator = default;
                    if (side)
                    {
                        AddSideProfileInfluences(
                            nx,
                            ny,
                            ref accumulator);
                    }
                    else
                    {
                        AddFrontBackInfluences(
                            nx,
                            ny,
                            ref accumulator);
                    }

                    accumulator.Add(
                        (int)FatManSkinBone.Root,
                        0.015f);
                    accumulator.Normalize();

                    int vertexIndex =
                        row * vertexColumns + column;
                    int offset =
                        vertexIndex * InfluenceCount;
                    accumulator.CopyTo(
                        influenceIndices,
                        influenceWeights,
                        offset);
                }
            }
        }

        private static void AddFrontBackInfluences(
            float x,
            float y,
            ref InfluenceAccumulator weights)
        {
            float core =
                1f - Mathf.SmoothStep(
                    0.23f,
                    0.47f,
                    Mathf.Abs(x - 0.5f));
            float leftArm =
                1f - Mathf.SmoothStep(
                    0.12f,
                    0.31f,
                    Mathf.Abs(x - 0.17f));
            float rightArm =
                1f - Mathf.SmoothStep(
                    0.12f,
                    0.31f,
                    Mathf.Abs(x - 0.83f));
            float leftLeg =
                1f - Mathf.SmoothStep(
                    0.10f,
                    0.25f,
                    Mathf.Abs(x - 0.39f));
            float rightLeg =
                1f - Mathf.SmoothStep(
                    0.10f,
                    0.25f,
                    Mathf.Abs(x - 0.61f));

            AddTorsoInfluences(core, y, ref weights);
            AddArmInfluences(
                leftArm,
                y,
                FatManSkinBone.ShoulderLeft,
                FatManSkinBone.UpperArmLeft,
                FatManSkinBone.ForearmLeft,
                FatManSkinBone.HandLeft,
                ref weights);
            AddArmInfluences(
                rightArm,
                y,
                FatManSkinBone.ShoulderRight,
                FatManSkinBone.UpperArmRight,
                FatManSkinBone.ForearmRight,
                FatManSkinBone.HandRight,
                ref weights);
            AddLegInfluences(
                leftLeg,
                y,
                FatManSkinBone.ThighLeft,
                FatManSkinBone.ShinLeft,
                FatManSkinBone.FootLeft,
                ref weights);
            AddLegInfluences(
                rightLeg,
                y,
                FatManSkinBone.ThighRight,
                FatManSkinBone.ShinRight,
                FatManSkinBone.FootRight,
                ref weights);
        }

        private static void AddSideProfileInfluences(
            float x,
            float y,
            ref InfluenceAccumulator weights)
        {
            float core =
                1f - Mathf.SmoothStep(
                    0.30f,
                    0.51f,
                    Mathf.Abs(x - 0.5f));
            float rearLimb =
                1f - Mathf.SmoothStep(
                    0.13f,
                    0.29f,
                    Mathf.Abs(x - 0.40f));
            float frontLimb =
                1f - Mathf.SmoothStep(
                    0.13f,
                    0.29f,
                    Mathf.Abs(x - 0.60f));

            AddTorsoInfluences(core, y, ref weights);
            AddArmInfluences(
                rearLimb,
                y,
                FatManSkinBone.ShoulderLeft,
                FatManSkinBone.UpperArmLeft,
                FatManSkinBone.ForearmLeft,
                FatManSkinBone.HandLeft,
                ref weights);
            AddArmInfluences(
                frontLimb,
                y,
                FatManSkinBone.ShoulderRight,
                FatManSkinBone.UpperArmRight,
                FatManSkinBone.ForearmRight,
                FatManSkinBone.HandRight,
                ref weights);
            AddLegInfluences(
                rearLimb,
                y,
                FatManSkinBone.ThighLeft,
                FatManSkinBone.ShinLeft,
                FatManSkinBone.FootLeft,
                ref weights);
            AddLegInfluences(
                frontLimb,
                y,
                FatManSkinBone.ThighRight,
                FatManSkinBone.ShinRight,
                FatManSkinBone.FootRight,
                ref weights);
        }

        private static void AddTorsoInfluences(
            float core,
            float y,
            ref InfluenceAccumulator weights)
        {
            weights.Add(
                (int)FatManSkinBone.Pelvis,
                core * Band(y, 0.29f, 0.49f, 0.09f));
            weights.Add(
                (int)FatManSkinBone.Spine,
                core * Band(y, 0.43f, 0.74f, 0.12f));
            weights.Add(
                (int)FatManSkinBone.Belly,
                core * Band(y, 0.45f, 0.68f, 0.10f) * 1.25f);
            weights.Add(
                (int)FatManSkinBone.ShirtHem,
                core * Band(y, 0.38f, 0.54f, 0.07f));
            weights.Add(
                (int)FatManSkinBone.Chest,
                core * Band(y, 0.62f, 0.82f, 0.09f));
            weights.Add(
                (int)FatManSkinBone.ChestSoft,
                core * Band(y, 0.59f, 0.79f, 0.10f) * 1.18f);
            weights.Add(
                (int)FatManSkinBone.Neck,
                core * Band(y, 0.76f, 0.88f, 0.06f));
            weights.Add(
                (int)FatManSkinBone.Head,
                core * Band(y, 0.79f, 1.02f, 0.09f) * 1.20f);
            weights.Add(
                (int)FatManSkinBone.ChinSoft,
                core * Band(y, 0.75f, 0.86f, 0.05f));
        }

        private static void AddArmInfluences(
            float horizontal,
            float y,
            FatManSkinBone shoulder,
            FatManSkinBone upperArm,
            FatManSkinBone forearm,
            FatManSkinBone hand,
            ref InfluenceAccumulator weights)
        {
            weights.Add(
                (int)shoulder,
                horizontal * Band(y, 0.67f, 0.83f, 0.07f));
            weights.Add(
                (int)upperArm,
                horizontal * Band(y, 0.54f, 0.76f, 0.09f) * 1.20f);
            weights.Add(
                (int)forearm,
                horizontal * Band(y, 0.36f, 0.61f, 0.09f) * 1.22f);
            weights.Add(
                (int)hand,
                horizontal * Band(y, 0.27f, 0.45f, 0.07f) * 1.25f);
        }

        private static void AddLegInfluences(
            float horizontal,
            float y,
            FatManSkinBone thigh,
            FatManSkinBone shin,
            FatManSkinBone foot,
            ref InfluenceAccumulator weights)
        {
            weights.Add(
                (int)thigh,
                horizontal * Band(y, 0.20f, 0.46f, 0.10f) * 1.22f);
            weights.Add(
                (int)shin,
                horizontal * Band(y, 0.055f, 0.29f, 0.085f) * 1.25f);
            weights.Add(
                (int)foot,
                horizontal * Band(y, -0.02f, 0.115f, 0.055f) * 1.32f);
        }

        private static float Band(
            float value,
            float minimum,
            float maximum,
            float feather)
        {
            float safeFeather = Mathf.Max(0.001f, feather);
            float enter = Mathf.SmoothStep(
                minimum - safeFeather,
                minimum + safeFeather,
                value);
            float leave = 1f - Mathf.SmoothStep(
                maximum - safeFeather,
                maximum + safeFeather,
                value);
            return Mathf.Clamp01(enter * leave);
        }

        private void EnsureInfluenceArrays(int vertexCount)
        {
            int required = vertexCount * InfluenceCount;
            if (influenceIndices == null ||
                influenceIndices.Length != required)
            {
                influenceIndices = new int[required];
                influenceWeights = new float[required];
            }
        }

        private struct InfluenceAccumulator
        {
            private int index0;
            private int index1;
            private int index2;
            private int index3;
            private float weight0;
            private float weight1;
            private float weight2;
            private float weight3;

            public void Add(int index, float weight)
            {
                if (weight <= 0.0001f)
                {
                    return;
                }

                if (weight > weight0)
                {
                    index3 = index2;
                    weight3 = weight2;
                    index2 = index1;
                    weight2 = weight1;
                    index1 = index0;
                    weight1 = weight0;
                    index0 = index;
                    weight0 = weight;
                }
                else if (weight > weight1)
                {
                    index3 = index2;
                    weight3 = weight2;
                    index2 = index1;
                    weight2 = weight1;
                    index1 = index;
                    weight1 = weight;
                }
                else if (weight > weight2)
                {
                    index3 = index2;
                    weight3 = weight2;
                    index2 = index;
                    weight2 = weight;
                }
                else if (weight > weight3)
                {
                    index3 = index;
                    weight3 = weight;
                }
            }

            public void Normalize()
            {
                float total =
                    weight0 + weight1 + weight2 + weight3;
                if (total <= 0.0001f)
                {
                    index0 = (int)FatManSkinBone.Root;
                    weight0 = 1f;
                    index1 = index2 = index3 = index0;
                    weight1 = weight2 = weight3 = 0f;
                    return;
                }

                float inverse = 1f / total;
                weight0 *= inverse;
                weight1 *= inverse;
                weight2 *= inverse;
                weight3 *= inverse;
            }

            public void CopyTo(
                int[] indices,
                float[] weights,
                int offset)
            {
                indices[offset] = index0;
                indices[offset + 1] = index1;
                indices[offset + 2] = index2;
                indices[offset + 3] = index3;
                weights[offset] = weight0;
                weights[offset + 1] = weight1;
                weights[offset + 2] = weight2;
                weights[offset + 3] = weight3;
            }
        }
    }
}
