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
    /// Patch 3.5 puppet surface. The old rig is used only as an animation signal.
    /// Every signal is remapped onto art-specific anchors, amplitude-limited and
    /// topology-checked before the painted character is submitted to the Canvas.
    /// This prevents the full-matrix skinning explosions from Patch 3.4 while
    /// keeping visible breathing, blinking, body sway and limb motion.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CharacterSkinnedSpriteGraphic : MaskableGraphic
    {
        private const int GridColumns = 30;
        private const int GridRows = 46;
        private const int InfluenceCount = 4;
        private const int FoldRepairPasses = 3;
        private const float MinimumRootWeight = 0.42f;

        private readonly Vector2[] bindPositions =
            new Vector2[Enum.GetValues(typeof(FatManSkinBone)).Length];
        private readonly float[] bindAngles =
            new float[Enum.GetValues(typeof(FatManSkinBone)).Length];
        private readonly Vector2[] bindScales =
            new Vector2[Enum.GetValues(typeof(FatManSkinBone)).Length];
        private readonly DriverPose[] driverPoses =
            new DriverPose[Enum.GetValues(typeof(FatManSkinBone)).Length];

        private Texture2D sourceTexture;
        private Rect normalizedUv;
        private RectTransform referenceRoot;
        private RectTransform[] bones;
        private int[] influenceIndices;
        private float[] influenceWeights;
        private Vector3[] baseVertices;
        private Vector3[] deformedVertices;
        private Vector2[] displacementBuffer;
        private Vector2[] smoothingBuffer;
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
        public int FoldRepairCount { get; private set; }
        public int SafetyClampCount { get; private set; }

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
            for (int i = 0; i < bindPositions.Length; i++)
            {
                RectTransform bone = i < bones.Length ? bones[i] : null;
                Matrix4x4 matrix = bone != null
                    ? rootWorldToLocal * bone.localToWorldMatrix
                    : Matrix4x4.identity;
                bindPositions[i] = ExtractPosition(matrix);
                bindAngles[i] = ExtractAngle(matrix);
                bindScales[i] = ExtractScale(matrix);
                driverPoses[i] = DriverPose.Identity;
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
            for (int i = 0; i < driverPoses.Length; i++)
            {
                RectTransform bone = i < bones.Length ? bones[i] : null;
                if (bone == null)
                {
                    driverPoses[i] = DriverPose.Identity;
                    continue;
                }

                Matrix4x4 matrix =
                    rootWorldToLocal * bone.localToWorldMatrix;
                Vector2 position = ExtractPosition(matrix);
                float angle = ExtractAngle(matrix);
                Vector2 scale = ExtractScale(matrix);
                MotionRule rule = GetMotionRule(
                    (FatManSkinBone)i,
                    facing);

                Vector2 translation =
                    (position - bindPositions[i]) * rule.translationGain;
                translation = Vector2.ClampMagnitude(
                    translation,
                    rule.maximumTranslation);

                float rotation = Mathf.Clamp(
                    Mathf.DeltaAngle(bindAngles[i], angle) *
                    rule.rotationGain,
                    -rule.maximumRotation,
                    rule.maximumRotation);

                Vector2 bindScale = bindScales[i];
                float ratioX = bindScale.x > 0.0001f
                    ? scale.x / bindScale.x
                    : 1f;
                float ratioY = bindScale.y > 0.0001f
                    ? scale.y / bindScale.y
                    : 1f;
                Vector2 scaleDelta = new Vector2(
                    (ratioX - 1f) * rule.scaleGain,
                    (ratioY - 1f) * rule.scaleGain);
                scaleDelta.x = Mathf.Clamp(
                    scaleDelta.x,
                    -rule.maximumScaleDelta,
                    rule.maximumScaleDelta);
                scaleDelta.y = Mathf.Clamp(
                    scaleDelta.y,
                    -rule.maximumScaleDelta,
                    rule.maximumScaleDelta);

                driverPoses[i] = new DriverPose(
                    translation,
                    rotation,
                    Vector2.one + scaleDelta);
            }

            SetVerticesDirty();
        }

        public bool TryGetDrivenPoint(
            Vector2 normalizedPoint,
            FatManSkinBone driver,
            out Vector2 localPosition,
            out float rotation,
            out Vector2 scale)
        {
            localPosition = Vector2.zero;
            rotation = 0f;
            scale = Vector2.one;
            if (!IsReady)
            {
                return false;
            }

            Rect rect = rectTransform.rect;
            Vector2 basePoint = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalizedPoint.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalizedPoint.y));
            DriverPose pose = driverPoses[
                Mathf.Clamp((int)driver, 0, driverPoses.Length - 1)];
            Matrix4x4 referenceToLocal =
                rectTransform.worldToLocalMatrix *
                referenceRoot.localToWorldMatrix;
            Vector2 localTranslation =
                referenceToLocal.MultiplyVector(pose.translation);
            DriverPose localPose = new DriverPose(
                localTranslation,
                pose.rotation,
                pose.scale);
            Vector2 anchor = GetDirectionalAnchor(driver, facing, rect);
            localPosition = ApplyDriver(basePoint, anchor, localPose);
            rotation = localPose.rotation;
            scale = localPose.scale;
            return true;
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
            EnsureArrays(vertexCount);

            Rect rect = rectTransform.rect;
            Matrix4x4 localToReference =
                referenceRoot.worldToLocalMatrix *
                rectTransform.localToWorldMatrix;
            Matrix4x4 referenceToLocal =
                rectTransform.worldToLocalMatrix *
                referenceRoot.localToWorldMatrix;

            for (int row = 0; row < vertexRows; row++)
            {
                float ny = row / (float)GridRows;
                float localY = Mathf.Lerp(rect.yMin, rect.yMax, ny);
                for (int column = 0; column < vertexColumns; column++)
                {
                    float nx = column / (float)GridColumns;
                    float localX = Mathf.Lerp(rect.xMin, rect.xMax, nx);
                    int vertexIndex = row * vertexColumns + column;
                    Vector3 baseLocal = new Vector3(localX, localY, 0f);
                    baseVertices[vertexIndex] = baseLocal;

                    Vector3 baseReference =
                        localToReference.MultiplyPoint3x4(baseLocal);
                    Vector3 blendedReference = Vector3.zero;
                    float accumulatedWeight = 0f;
                    int influenceOffset = vertexIndex * InfluenceCount;

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
                            driverPoses.Length - 1);
                        FatManSkinBone bone = (FatManSkinBone)boneIndex;
                        Vector2 anchorLocal =
                            GetDirectionalAnchor(bone, facing, rect);
                        Vector3 anchorReference =
                            localToReference.MultiplyPoint3x4(anchorLocal);
                        DriverPose pose = driverPoses[boneIndex];
                        Vector3 transformedReference = ApplyDriver(
                            baseReference,
                            anchorReference,
                            pose);
                        blendedReference += transformedReference * weight;
                        accumulatedWeight += weight;
                    }

                    if (accumulatedWeight <= 0.0001f)
                    {
                        blendedReference = baseReference;
                    }
                    else if (Mathf.Abs(accumulatedWeight - 1f) > 0.0001f)
                    {
                        blendedReference /= accumulatedWeight;
                    }

                    Vector3 rawLocal =
                        referenceToLocal.MultiplyPoint3x4(blendedReference);
                    displacementBuffer[vertexIndex] =
                        (Vector2)(rawLocal - baseLocal);
                }
            }

            SmoothDisplacements(vertexColumns, vertexRows);
            ApplySafetyEnvelope(rect, vertexCount);
            RepairFoldedCells(vertexColumns, vertexRows);

            float totalDisplacement = 0f;
            Color32 vertexColor = color;
            for (int row = 0; row < vertexRows; row++)
            {
                float ny = row / (float)GridRows;
                float uvY = Mathf.Lerp(
                    normalizedUv.yMin,
                    normalizedUv.yMax,
                    ny);
                for (int column = 0; column < vertexColumns; column++)
                {
                    float nx = column / (float)GridColumns;
                    float uvX = Mathf.Lerp(
                        normalizedUv.xMin,
                        normalizedUv.xMax,
                        nx);
                    int index = row * vertexColumns + column;
                    totalDisplacement += Vector3.Distance(
                        baseVertices[index],
                        deformedVertices[index]);
                    vertexHelper.AddVert(
                        deformedVertices[index],
                        vertexColor,
                        new Vector2(uvX, uvY));
                }
            }

            for (int row = 0; row < GridRows; row++)
            {
                for (int column = 0; column < GridColumns; column++)
                {
                    int bottomLeft = row * vertexColumns + column;
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

            DeformationMagnitude = vertexCount > 0
                ? totalDisplacement / vertexCount
                : 0f;
        }

        private void SmoothDisplacements(
            int vertexColumns,
            int vertexRows)
        {
            Array.Copy(
                displacementBuffer,
                smoothingBuffer,
                displacementBuffer.Length);

            const int smoothingPasses = 2;
            for (int pass = 0; pass < smoothingPasses; pass++)
            {
                for (int row = 0; row < vertexRows; row++)
                {
                    for (int column = 0;
                         column < vertexColumns;
                         column++)
                    {
                        int index = row * vertexColumns + column;
                        Vector2 sum = smoothingBuffer[index] * 4f;
                        float count = 4f;
                        if (column > 0)
                        {
                            sum += smoothingBuffer[index - 1];
                            count += 1f;
                        }
                        if (column + 1 < vertexColumns)
                        {
                            sum += smoothingBuffer[index + 1];
                            count += 1f;
                        }
                        if (row > 0)
                        {
                            sum += smoothingBuffer[index - vertexColumns];
                            count += 1f;
                        }
                        if (row + 1 < vertexRows)
                        {
                            sum += smoothingBuffer[index + vertexColumns];
                            count += 1f;
                        }

                        displacementBuffer[index] = sum / count;
                    }
                }

                Array.Copy(
                    displacementBuffer,
                    smoothingBuffer,
                    displacementBuffer.Length);
            }
        }

        private void ApplySafetyEnvelope(Rect rect, int vertexCount)
        {
            SafetyClampCount = 0;
            float maximumDisplacement = GetMaximumVertexDisplacement(facing);
            float horizontalMargin = rect.width * 0.035f;
            float verticalMargin = rect.height * 0.025f;
            for (int i = 0; i < vertexCount; i++)
            {
                Vector2 displacement = displacementBuffer[i];
                Vector2 clamped = Vector2.ClampMagnitude(
                    displacement,
                    maximumDisplacement);
                if ((clamped - displacement).sqrMagnitude > 0.0001f)
                {
                    SafetyClampCount++;
                }

                Vector3 point = baseVertices[i] + (Vector3)clamped;
                float x = Mathf.Clamp(
                    point.x,
                    rect.xMin - horizontalMargin,
                    rect.xMax + horizontalMargin);
                float y = Mathf.Clamp(
                    point.y,
                    rect.yMin - verticalMargin,
                    rect.yMax + verticalMargin);
                if (Mathf.Abs(x - point.x) > 0.001f ||
                    Mathf.Abs(y - point.y) > 0.001f)
                {
                    SafetyClampCount++;
                }

                deformedVertices[i] = new Vector3(x, y, 0f);
            }
        }

        private void RepairFoldedCells(
            int vertexColumns,
            int vertexRows)
        {
            FoldRepairCount = 0;
            for (int pass = 0; pass < FoldRepairPasses; pass++)
            {
                bool repaired = false;
                for (int row = 0; row < vertexRows - 1; row++)
                {
                    for (int column = 0;
                         column < vertexColumns - 1;
                         column++)
                    {
                        int bottomLeft = row * vertexColumns + column;
                        int bottomRight = bottomLeft + 1;
                        int topLeft = bottomLeft + vertexColumns;
                        int topRight = topLeft + 1;
                        if (IsValidCell(
                                deformedVertices[bottomLeft],
                                deformedVertices[bottomRight],
                                deformedVertices[topLeft],
                                deformedVertices[topRight]))
                        {
                            continue;
                        }

                        repaired = true;
                        FoldRepairCount++;
                        const float keepDeformation = 0.18f;
                        int[] indices =
                        {
                            bottomLeft,
                            bottomRight,
                            topLeft,
                            topRight
                        };
                        for (int i = 0; i < indices.Length; i++)
                        {
                            int index = indices[i];
                            deformedVertices[index] = Vector3.Lerp(
                                baseVertices[index],
                                deformedVertices[index],
                                keepDeformation);
                        }
                    }
                }

                if (!repaired)
                {
                    break;
                }
            }
        }

        private static bool IsValidCell(
            Vector3 bottomLeft,
            Vector3 bottomRight,
            Vector3 topLeft,
            Vector3 topRight)
        {
            float areaOne = SignedArea(bottomLeft, topLeft, topRight);
            float areaTwo = SignedArea(bottomLeft, topRight, bottomRight);
            return areaOne < -0.01f && areaTwo < -0.01f;
        }

        private static float SignedArea(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector2 ab = b - a;
            Vector2 ac = c - a;
            return ab.x * ac.y - ab.y * ac.x;
        }

        private void BuildInfluenceMap()
        {
            int vertexCount =
                (GridColumns + 1) * (GridRows + 1);
            EnsureArrays(vertexCount);
            bool side = IsSide(facing);
            bool back = facing == CharacterFacing.Back;
            int vertexColumns = GridColumns + 1;

            for (int row = 0; row <= GridRows; row++)
            {
                float y = row / (float)GridRows;
                for (int column = 0; column <= GridColumns; column++)
                {
                    float x = column / (float)GridColumns;
                    InfluenceAccumulator weights = default;
                    weights.Add(
                        (int)FatManSkinBone.Root,
                        MinimumRootWeight);
                    if (side)
                    {
                        AddSideInfluences(x, y, ref weights);
                    }
                    else
                    {
                        AddFrontBackInfluences(
                            x,
                            y,
                            back,
                            ref weights);
                    }

                    weights.Normalize();
                    int index = row * vertexColumns + column;
                    weights.CopyTo(
                        influenceIndices,
                        influenceWeights,
                        index * InfluenceCount);
                }
            }
        }

        private static void AddFrontBackInfluences(
            float x,
            float y,
            bool back,
            ref InfluenceAccumulator weights)
        {
            float centerDistance = Mathf.Abs(x - 0.5f);
            float core = 1f - Mathf.SmoothStep(0.18f, 0.43f, centerDistance);
            float head = core * Band(y, 0.76f, 1.01f, 0.065f);
            float chest = core * Band(y, 0.60f, 0.79f, 0.075f);
            float belly = core * Band(y, 0.42f, 0.66f, 0.08f);
            float pelvis = core * Band(y, 0.29f, 0.47f, 0.07f);

            weights.Add((int)FatManSkinBone.Head, head * 0.42f);
            weights.Add((int)FatManSkinBone.Neck, head * 0.14f);
            weights.Add((int)FatManSkinBone.ChinSoft, head * 0.12f);
            weights.Add((int)FatManSkinBone.Chest, chest * 0.28f);
            weights.Add((int)FatManSkinBone.ChestSoft, chest * 0.24f);
            weights.Add((int)FatManSkinBone.Spine, chest * 0.14f);
            weights.Add((int)FatManSkinBone.Belly, belly * 0.30f);
            weights.Add((int)FatManSkinBone.ShirtHem, belly * 0.17f);
            weights.Add((int)FatManSkinBone.Pelvis, pelvis * 0.28f);

            float leftOuter =
                1f - Mathf.SmoothStep(0.02f, 0.18f, Mathf.Abs(x - 0.14f));
            float rightOuter =
                1f - Mathf.SmoothStep(0.02f, 0.18f, Mathf.Abs(x - 0.86f));
            float armGain = back ? 0.40f : 0.46f;
            AddArmInfluences(
                leftOuter * armGain,
                y,
                FatManSkinBone.UpperArmLeft,
                FatManSkinBone.ForearmLeft,
                FatManSkinBone.HandLeft,
                ref weights);
            AddArmInfluences(
                rightOuter * armGain,
                y,
                FatManSkinBone.UpperArmRight,
                FatManSkinBone.ForearmRight,
                FatManSkinBone.HandRight,
                ref weights);

            float leftLeg =
                1f - Mathf.SmoothStep(0.035f, 0.18f, Mathf.Abs(x - 0.39f));
            float rightLeg =
                1f - Mathf.SmoothStep(0.035f, 0.18f, Mathf.Abs(x - 0.61f));
            AddLegInfluences(
                leftLeg * 0.46f,
                y,
                FatManSkinBone.ThighLeft,
                FatManSkinBone.ShinLeft,
                FatManSkinBone.FootLeft,
                ref weights);
            AddLegInfluences(
                rightLeg * 0.46f,
                y,
                FatManSkinBone.ThighRight,
                FatManSkinBone.ShinRight,
                FatManSkinBone.FootRight,
                ref weights);
        }

        private static void AddSideInfluences(
            float x,
            float y,
            ref InfluenceAccumulator weights)
        {
            float core =
                1f - Mathf.SmoothStep(0.24f, 0.50f, Mathf.Abs(x - 0.5f));
            float head = core * Band(y, 0.76f, 1.01f, 0.065f);
            float chest = core * Band(y, 0.60f, 0.79f, 0.075f);
            float belly = core * Band(y, 0.42f, 0.67f, 0.08f);
            float pelvis = core * Band(y, 0.29f, 0.47f, 0.07f);

            weights.Add((int)FatManSkinBone.Head, head * 0.30f);
            weights.Add((int)FatManSkinBone.Neck, head * 0.10f);
            weights.Add((int)FatManSkinBone.ChinSoft, head * 0.08f);
            weights.Add((int)FatManSkinBone.Chest, chest * 0.20f);
            weights.Add((int)FatManSkinBone.ChestSoft, chest * 0.19f);
            weights.Add((int)FatManSkinBone.Spine, chest * 0.12f);
            weights.Add((int)FatManSkinBone.Belly, belly * 0.24f);
            weights.Add((int)FatManSkinBone.ShirtHem, belly * 0.13f);
            weights.Add((int)FatManSkinBone.Pelvis, pelvis * 0.22f);

            float rear =
                1f - Mathf.SmoothStep(0.05f, 0.20f, Mathf.Abs(x - 0.42f));
            float front =
                1f - Mathf.SmoothStep(0.05f, 0.20f, Mathf.Abs(x - 0.60f));
            AddArmInfluences(
                rear * 0.22f,
                y,
                FatManSkinBone.UpperArmLeft,
                FatManSkinBone.ForearmLeft,
                FatManSkinBone.HandLeft,
                ref weights);
            AddArmInfluences(
                front * 0.32f,
                y,
                FatManSkinBone.UpperArmRight,
                FatManSkinBone.ForearmRight,
                FatManSkinBone.HandRight,
                ref weights);
            AddLegInfluences(
                rear * 0.24f,
                y,
                FatManSkinBone.ThighLeft,
                FatManSkinBone.ShinLeft,
                FatManSkinBone.FootLeft,
                ref weights);
            AddLegInfluences(
                front * 0.34f,
                y,
                FatManSkinBone.ThighRight,
                FatManSkinBone.ShinRight,
                FatManSkinBone.FootRight,
                ref weights);
        }

        private static void AddArmInfluences(
            float horizontal,
            float y,
            FatManSkinBone upperArm,
            FatManSkinBone forearm,
            FatManSkinBone hand,
            ref InfluenceAccumulator weights)
        {
            weights.Add(
                (int)upperArm,
                horizontal * Band(y, 0.55f, 0.75f, 0.055f));
            weights.Add(
                (int)forearm,
                horizontal * Band(y, 0.39f, 0.61f, 0.055f));
            weights.Add(
                (int)hand,
                horizontal * Band(y, 0.28f, 0.44f, 0.045f));
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
                horizontal * Band(y, 0.22f, 0.42f, 0.055f));
            weights.Add(
                (int)shin,
                horizontal * Band(y, 0.075f, 0.27f, 0.050f));
            weights.Add(
                (int)foot,
                horizontal * Band(y, -0.01f, 0.115f, 0.035f));
        }

        private static MotionRule GetMotionRule(
            FatManSkinBone bone,
            CharacterFacing view)
        {
            bool side = IsSide(view);
            bool back = view == CharacterFacing.Back;
            float viewGain = side ? 0.56f : back ? 0.78f : 1f;
            switch (bone)
            {
                case FatManSkinBone.Root:
                    return new MotionRule(0.30f, 10f, 0.08f, 1.5f, 0f, 0f);
                case FatManSkinBone.Pelvis:
                    return new MotionRule(0.24f, 10f, 0.16f, 3.0f, 0.08f, 0.018f);
                case FatManSkinBone.Spine:
                    return new MotionRule(0.18f, 7f, 0.18f, 3.8f, 0.12f, 0.020f);
                case FatManSkinBone.Chest:
                    return new MotionRule(0.16f, 7f, 0.22f, 4.2f, 0.30f, 0.028f);
                case FatManSkinBone.Belly:
                    return new MotionRule(0.20f, 8f, 0.12f, 2.4f, 0.65f, 0.050f);
                case FatManSkinBone.ShirtHem:
                    return new MotionRule(0.22f, 8f, 0.12f, 2.6f, 0.48f, 0.040f);
                case FatManSkinBone.ChestSoft:
                    return new MotionRule(0.18f, 7f, 0.14f, 2.8f, 0.62f, 0.045f);
                case FatManSkinBone.Neck:
                    return new MotionRule(0.10f, 5f, 0.24f, 4.0f, 0.08f, 0.015f);
                case FatManSkinBone.Head:
                    return new MotionRule(0.14f, 7f, 0.28f, 6.5f, 0.05f, 0.012f);
                case FatManSkinBone.ChinSoft:
                    return new MotionRule(0.16f, 6f, 0.18f, 3.2f, 0.50f, 0.035f);
                case FatManSkinBone.UpperArmLeft:
                case FatManSkinBone.UpperArmRight:
                    return new MotionRule(0.07f, 5f, 0.24f * viewGain, 8.5f * viewGain, 0f, 0f);
                case FatManSkinBone.ForearmLeft:
                case FatManSkinBone.ForearmRight:
                    return new MotionRule(0.055f, 4f, 0.20f * viewGain, 10f * viewGain, 0f, 0f);
                case FatManSkinBone.HandLeft:
                case FatManSkinBone.HandRight:
                    return new MotionRule(0.045f, 4f, 0.16f * viewGain, 8f * viewGain, 0f, 0f);
                case FatManSkinBone.ThighLeft:
                case FatManSkinBone.ThighRight:
                    return new MotionRule(0.06f, 5f, 0.20f * viewGain, 7.5f * viewGain, 0f, 0f);
                case FatManSkinBone.ShinLeft:
                case FatManSkinBone.ShinRight:
                    return new MotionRule(0.045f, 4f, 0.17f * viewGain, 8.5f * viewGain, 0f, 0f);
                case FatManSkinBone.FootLeft:
                case FatManSkinBone.FootRight:
                    return new MotionRule(0.035f, 4f, 0.14f * viewGain, 6.5f * viewGain, 0f, 0f);
                default:
                    return new MotionRule(0f, 0f, 0f, 0f, 0f, 0f);
            }
        }

        private static Vector2 GetDirectionalAnchor(
            FatManSkinBone bone,
            CharacterFacing view,
            Rect rect)
        {
            Vector2 normalized = IsSide(view)
                ? GetSideAnchor(bone)
                : GetFrontBackAnchor(bone);
            return new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalized.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalized.y));
        }

        private static Vector2 GetFrontBackAnchor(FatManSkinBone bone)
        {
            switch (bone)
            {
                case FatManSkinBone.Pelvis: return new Vector2(0.50f, 0.37f);
                case FatManSkinBone.Spine: return new Vector2(0.50f, 0.54f);
                case FatManSkinBone.Chest: return new Vector2(0.50f, 0.68f);
                case FatManSkinBone.Belly: return new Vector2(0.50f, 0.52f);
                case FatManSkinBone.ShirtHem: return new Vector2(0.50f, 0.43f);
                case FatManSkinBone.ChestSoft: return new Vector2(0.50f, 0.66f);
                case FatManSkinBone.Neck: return new Vector2(0.50f, 0.78f);
                case FatManSkinBone.Head: return new Vector2(0.50f, 0.86f);
                case FatManSkinBone.ChinSoft: return new Vector2(0.50f, 0.79f);
                case FatManSkinBone.UpperArmLeft: return new Vector2(0.28f, 0.68f);
                case FatManSkinBone.ForearmLeft: return new Vector2(0.20f, 0.53f);
                case FatManSkinBone.HandLeft: return new Vector2(0.16f, 0.38f);
                case FatManSkinBone.UpperArmRight: return new Vector2(0.72f, 0.68f);
                case FatManSkinBone.ForearmRight: return new Vector2(0.80f, 0.53f);
                case FatManSkinBone.HandRight: return new Vector2(0.84f, 0.38f);
                case FatManSkinBone.ThighLeft: return new Vector2(0.41f, 0.34f);
                case FatManSkinBone.ShinLeft: return new Vector2(0.39f, 0.19f);
                case FatManSkinBone.FootLeft: return new Vector2(0.37f, 0.07f);
                case FatManSkinBone.ThighRight: return new Vector2(0.59f, 0.34f);
                case FatManSkinBone.ShinRight: return new Vector2(0.61f, 0.19f);
                case FatManSkinBone.FootRight: return new Vector2(0.63f, 0.07f);
                default: return new Vector2(0.50f, 0.50f);
            }
        }

        private static Vector2 GetSideAnchor(FatManSkinBone bone)
        {
            switch (bone)
            {
                case FatManSkinBone.Pelvis: return new Vector2(0.50f, 0.37f);
                case FatManSkinBone.Spine: return new Vector2(0.49f, 0.54f);
                case FatManSkinBone.Chest: return new Vector2(0.50f, 0.68f);
                case FatManSkinBone.Belly: return new Vector2(0.55f, 0.52f);
                case FatManSkinBone.ShirtHem: return new Vector2(0.53f, 0.43f);
                case FatManSkinBone.ChestSoft: return new Vector2(0.53f, 0.66f);
                case FatManSkinBone.Neck: return new Vector2(0.50f, 0.78f);
                case FatManSkinBone.Head: return new Vector2(0.52f, 0.86f);
                case FatManSkinBone.ChinSoft: return new Vector2(0.56f, 0.79f);
                case FatManSkinBone.UpperArmLeft: return new Vector2(0.43f, 0.67f);
                case FatManSkinBone.ForearmLeft: return new Vector2(0.42f, 0.52f);
                case FatManSkinBone.HandLeft: return new Vector2(0.42f, 0.38f);
                case FatManSkinBone.UpperArmRight: return new Vector2(0.58f, 0.67f);
                case FatManSkinBone.ForearmRight: return new Vector2(0.60f, 0.52f);
                case FatManSkinBone.HandRight: return new Vector2(0.61f, 0.38f);
                case FatManSkinBone.ThighLeft: return new Vector2(0.45f, 0.34f);
                case FatManSkinBone.ShinLeft: return new Vector2(0.44f, 0.19f);
                case FatManSkinBone.FootLeft: return new Vector2(0.43f, 0.07f);
                case FatManSkinBone.ThighRight: return new Vector2(0.57f, 0.34f);
                case FatManSkinBone.ShinRight: return new Vector2(0.59f, 0.19f);
                case FatManSkinBone.FootRight: return new Vector2(0.61f, 0.07f);
                default: return new Vector2(0.50f, 0.50f);
            }
        }

        private static float GetMaximumVertexDisplacement(
            CharacterFacing view)
        {
            if (IsSide(view))
            {
                return 34f;
            }
            return view == CharacterFacing.Back ? 43f : 48f;
        }

        private static Vector2 ApplyDriver(
            Vector2 point,
            Vector2 anchor,
            DriverPose pose)
        {
            Vector2 relative = point - anchor;
            relative = Vector2.Scale(relative, pose.scale);
            float radians = pose.rotation * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            Vector2 rotated = new Vector2(
                relative.x * cos - relative.y * sin,
                relative.x * sin + relative.y * cos);
            return anchor + rotated + pose.translation;
        }

        private static Vector3 ApplyDriver(
            Vector3 point,
            Vector3 anchor,
            DriverPose pose)
        {
            Vector2 result = ApplyDriver(
                new Vector2(point.x, point.y),
                new Vector2(anchor.x, anchor.y),
                pose);
            return new Vector3(result.x, result.y, point.z);
        }

        private static Vector2 ExtractPosition(Matrix4x4 matrix)
        {
            return new Vector2(matrix.m03, matrix.m13);
        }

        private static float ExtractAngle(Matrix4x4 matrix)
        {
            return Mathf.Atan2(matrix.m10, matrix.m00) * Mathf.Rad2Deg;
        }

        private static Vector2 ExtractScale(Matrix4x4 matrix)
        {
            return new Vector2(
                new Vector2(matrix.m00, matrix.m10).magnitude,
                new Vector2(matrix.m01, matrix.m11).magnitude);
        }

        private static bool IsSide(CharacterFacing view)
        {
            return view == CharacterFacing.SideLeft ||
                   view == CharacterFacing.SideRight;
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

        private void EnsureArrays(int vertexCount)
        {
            int influenceSize = vertexCount * InfluenceCount;
            if (influenceIndices == null ||
                influenceIndices.Length != influenceSize)
            {
                influenceIndices = new int[influenceSize];
                influenceWeights = new float[influenceSize];
            }

            if (baseVertices == null || baseVertices.Length != vertexCount)
            {
                baseVertices = new Vector3[vertexCount];
                deformedVertices = new Vector3[vertexCount];
                displacementBuffer = new Vector2[vertexCount];
                smoothingBuffer = new Vector2[vertexCount];
            }
        }

        private readonly struct MotionRule
        {
            public readonly float translationGain;
            public readonly float maximumTranslation;
            public readonly float rotationGain;
            public readonly float maximumRotation;
            public readonly float scaleGain;
            public readonly float maximumScaleDelta;

            public MotionRule(
                float targetTranslationGain,
                float targetMaximumTranslation,
                float targetRotationGain,
                float targetMaximumRotation,
                float targetScaleGain,
                float targetMaximumScaleDelta)
            {
                translationGain = targetTranslationGain;
                maximumTranslation = targetMaximumTranslation;
                rotationGain = targetRotationGain;
                maximumRotation = targetMaximumRotation;
                scaleGain = targetScaleGain;
                maximumScaleDelta = targetMaximumScaleDelta;
            }
        }

        private readonly struct DriverPose
        {
            public static readonly DriverPose Identity =
                new DriverPose(Vector2.zero, 0f, Vector2.one);

            public readonly Vector2 translation;
            public readonly float rotation;
            public readonly Vector2 scale;

            public DriverPose(
                Vector2 targetTranslation,
                float targetRotation,
                Vector2 targetScale)
            {
                translation = targetTranslation;
                rotation = targetRotation;
                scale = targetScale;
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

                if (index0 == index && weight0 > 0f)
                {
                    weight0 += weight;
                    return;
                }
                if (index1 == index && weight1 > 0f)
                {
                    weight1 += weight;
                    return;
                }
                if (index2 == index && weight2 > 0f)
                {
                    weight2 += weight;
                    return;
                }
                if (index3 == index && weight3 > 0f)
                {
                    weight3 += weight;
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
                float total = weight0 + weight1 + weight2 + weight3;
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
