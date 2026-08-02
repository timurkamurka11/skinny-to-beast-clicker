using System;
using System.Collections.Generic;
using UnityEngine;
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
        [SerializeField, Range(1, 96)] private int gridColumns = 8;
        [SerializeField, Range(1, 144)] private int gridRows = 12;
        [SerializeField, Range(1.25f, 4f)]
        private float weightFalloff = 2.35f;
        [SerializeField] private List<BoneBinding> boneBindings = new();

        [NonSerialized] private bool bindPoseCaptured;
        [NonSerialized] private Matrix4x4[] skinMatrices =
            Array.Empty<Matrix4x4>();

        public string ContractPath => contractPath;
        public Sprite SourceSprite => ResolveSprite();
        public int BoneCount => boneBindings.Count;
        public string PrimaryBoneName =>
            boneBindings.Count > 0 && boneBindings[0] != null
                ? boneBindings[0].boneName
                : string.Empty;
        public bool IsRigidlyBound => boneBindings.Count == 1;
        public bool HasMultipleBoneWeights => boneBindings.Count > 1;
        public bool UsesContinuousBodyWeights =>
            Patch4RigContract.IsRuntimeContinuousBodyLayer(contractPath) &&
            HasMultipleBoneWeights;
        public bool IsBound => bindPoseCaptured && boneBindings.Count > 0;
        public bool UsesFullCanvasUv => HasFullCanvasUv(ResolveSprite());
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
            gridColumns = Mathf.Clamp(columns, 1, 96);
            gridRows = Mathf.Clamp(rows, 1, 144);
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
            Vector4 outerUv = ResolveFullCanvasUv(sprite);
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
                graphic != null &&
                graphic.gameObject.activeInHierarchy)
            {
                // Both the intact full-body grid and sparse one-bone face/FX
                // replacements deform through this mesh effect. Every active
                // layer therefore needs a per-frame refresh while bones move.
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

            if (UsesContinuousBodyWeights)
            {
                return DeformContinuousBody(
                    original,
                    spritePixel,
                    skinMatrices);
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

        /// <summary>
        /// Deforms the one intact painted body with smoothly blended anatomical
        /// zones. This avoids exposing rectangular shoulder/elbow/knee cutouts
        /// while still letting the authored Patch 4 skeleton drive the visible
        /// head, arms, legs, torso and soft belly. Face replacements use the
        /// exact same Head matrix as the head region below.
        /// </summary>
        private Vector3 DeformContinuousBody(
            Vector3 original,
            Vector2 spritePixel,
            IReadOnlyList<Matrix4x4> matrices)
        {
            Sprite sprite = ResolveSprite();
            if (sprite == null)
            {
                return original;
            }

            float normalizedX = spritePixel.x /
                Mathf.Max(1f, sprite.rect.width);
            float topY = 1f - spritePixel.y /
                Mathf.Max(1f, sprite.rect.height);
            Vector3 torso = DeformTorso(
                original,
                normalizedX,
                topY,
                matrices);

            // The complete face-replacement region and the underlying painted
            // head share one matrix. The neck then blends back into the upper
            // torso instead of opening a seam below the chin.
            if (normalizedX >= .34f &&
                normalizedX <= .66f &&
                topY <= .335f)
            {
                Vector3 head = BonePoint(
                    "Head",
                    original,
                    matrices);
                if (topY <= .255f)
                {
                    return head;
                }

                Vector3 neck = BonePoint(
                    "Neck",
                    original,
                    matrices);
                if (topY <= .300f)
                {
                    return Vector3.Lerp(
                        head,
                        neck,
                        SmoothRange(.255f, .300f, topY));
                }

                return Vector3.Lerp(
                    neck,
                    torso,
                    SmoothRange(.300f, .335f, topY));
            }

            Vector3 result = torso;
            if (topY >= .255f && topY <= .55f)
            {
                bool left = normalizedX < .5f;
                float sideX = left
                    ? normalizedX
                    : 1f - normalizedX;
                float centerX = ArmCenterX(topY);
                float radius = ArmRadius(topY);
                float centerlineInfluence =
                    1f - SmoothRange(
                        radius * .75f,
                        radius * 1.12f,
                        Mathf.Abs(sideX - centerX));
                float torsoBoundary = ArmTorsoBoundary(topY);
                float outsideTorso =
                    1f - SmoothRange(
                        torsoBoundary - .018f,
                        torsoBoundary + .002f,
                        sideX);
                float verticalInfluence =
                    SmoothRange(.245f, .272f, topY) *
                    (1f - SmoothRange(.525f, .55f, topY));
                float armInfluence =
                    centerlineInfluence *
                    outsideTorso *
                    verticalInfluence;
                if (armInfluence > .0001f)
                {
                    Vector3 arm = DeformArm(
                        left,
                        original,
                        topY,
                        matrices);
                    result = Vector3.Lerp(
                        result,
                        arm,
                        armInfluence);
                }
            }

            if (topY >= .515f)
            {
                bool left = normalizedX < .5f;
                float sideX = left
                    ? normalizedX
                    : 1f - normalizedX;
                float centerX = LegCenterX(topY);
                float radius = LegRadius(topY);
                float centerlineInfluence =
                    1f - SmoothRange(
                        radius * .86f,
                        radius * 1.08f,
                        Mathf.Abs(sideX - centerX));
                float centerSeparation =
                    1f - SmoothRange(.468f, .493f, sideX);
                float verticalInfluence =
                    SmoothRange(.515f, .575f, topY);
                float legInfluence =
                    centerlineInfluence *
                    centerSeparation *
                    verticalInfluence;
                if (legInfluence > .0001f)
                {
                    Vector3 leg = DeformLeg(
                        left,
                        original,
                        topY,
                        matrices);
                    result = Vector3.Lerp(
                        result,
                        leg,
                        legInfluence);
                }
            }

            return result;
        }

        private Vector3 DeformTorso(
            Vector3 original,
            float normalizedX,
            float topY,
            IReadOnlyList<Matrix4x4> matrices)
        {
            Vector3 upper = BonePoint(
                "SpineUpper",
                original,
                matrices);
            Vector3 lower = BonePoint(
                "SpineLower",
                original,
                matrices);
            Vector3 pelvis = BonePoint(
                "Pelvis",
                original,
                matrices);
            Vector3 torso = topY <= .36f
                ? upper
                : topY <= .47f
                    ? Vector3.Lerp(
                        upper,
                        lower,
                        SmoothRange(.36f, .47f, topY))
                    : Vector3.Lerp(
                        lower,
                        pelvis,
                        SmoothRange(.47f, .56f, topY));

            float centerDistance = Mathf.Abs(normalizedX - .5f);
            float bellyInfluence =
                (1f - SmoothRange(.16f, .29f, centerDistance)) *
                SmoothRange(.33f, .385f, topY) *
                (1f - SmoothRange(.505f, .565f, topY)) *
                .48f;
            if (bellyInfluence > .0001f)
            {
                Vector3 bellyBase = BonePoint(
                    "BellyBase",
                    original,
                    matrices);
                Vector3 bellyTip = BonePoint(
                    "BellyTip",
                    original,
                    matrices);
                Vector3 belly = Vector3.Lerp(
                    bellyBase,
                    bellyTip,
                    SmoothRange(.37f, .52f, topY));
                torso = Vector3.Lerp(
                    torso,
                    belly,
                    bellyInfluence);
            }

            float chestInfluence =
                SmoothRange(.27f, .315f, topY) *
                (1f - SmoothRange(.37f, .415f, topY)) *
                (1f - SmoothRange(.22f, .31f, centerDistance)) *
                .22f;
            if (chestInfluence > .0001f)
            {
                Vector3 chest = BonePoint(
                    normalizedX < .5f
                        ? "ChestSoftL"
                        : "ChestSoftR",
                    original,
                    matrices);
                torso = Vector3.Lerp(
                    torso,
                    chest,
                    chestInfluence);
            }

            return torso;
        }

        private Vector3 DeformArm(
            bool left,
            Vector3 original,
            float topY,
            IReadOnlyList<Matrix4x4> matrices)
        {
            string upperName = left ? "UpperArmL" : "UpperArmR";
            string forearmName = left ? "ForearmL" : "ForearmR";
            string handName = left ? "HandL" : "HandR";
            string clavicleName = left ? "ClavicleL" : "ClavicleR";
            Vector3 clavicle = BonePoint(
                clavicleName,
                original,
                matrices);
            Vector3 upper = BonePoint(upperName, original, matrices);
            Vector3 forearm = BonePoint(forearmName, original, matrices);
            Vector3 hand = BonePoint(handName, original, matrices);

            if (topY <= .325f)
            {
                return Vector3.Lerp(
                    clavicle,
                    upper,
                    SmoothRange(.275f, .325f, topY));
            }

            if (topY <= .385f)
            {
                return upper;
            }

            if (topY <= .435f)
            {
                return Vector3.Lerp(
                    upper,
                    forearm,
                    SmoothRange(.385f, .435f, topY));
            }

            if (topY <= .48f)
            {
                return forearm;
            }

            return topY <= .525f
                ? Vector3.Lerp(
                    forearm,
                    hand,
                    SmoothRange(.48f, .525f, topY))
                : hand;
        }

        private Vector3 DeformLeg(
            bool left,
            Vector3 original,
            float topY,
            IReadOnlyList<Matrix4x4> matrices)
        {
            string thighName = left ? "ThighL" : "ThighR";
            string shinName = left ? "ShinL" : "ShinR";
            string footName = left ? "FootL" : "FootR";
            Vector3 thigh = BonePoint(thighName, original, matrices);
            Vector3 shin = BonePoint(shinName, original, matrices);
            Vector3 foot = BonePoint(footName, original, matrices);

            if (topY <= .60f)
            {
                return thigh;
            }

            if (topY <= .65f)
            {
                return Vector3.Lerp(
                    thigh,
                    shin,
                    SmoothRange(.60f, .65f, topY));
            }

            if (topY <= .71f)
            {
                return shin;
            }

            return topY <= .76f
                ? Vector3.Lerp(
                    shin,
                    foot,
                    SmoothRange(.71f, .76f, topY))
                : foot;
        }

        private Vector3 BonePoint(
            string boneName,
            Vector3 original,
            IReadOnlyList<Matrix4x4> matrices)
        {
            for (int i = 0; i < boneBindings.Count; i++)
            {
                BoneBinding binding = boneBindings[i];
                if (binding != null &&
                    string.Equals(
                        binding.boneName,
                        boneName,
                        StringComparison.Ordinal))
                {
                    return matrices[i].MultiplyPoint3x4(original);
                }
            }

            return matrices.Count > 0
                ? matrices[0].MultiplyPoint3x4(original)
                : original;
        }

        private static float SmoothRange(
            float start,
            float end,
            float value)
        {
            return Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(start, end, value));
        }

        private static float ArmCenterX(float topY)
        {
            if (topY <= .315f)
            {
                return Mathf.Lerp(
                    .340f,
                    .325f,
                    Mathf.InverseLerp(.245f, .315f, topY));
            }

            if (topY <= .425f)
            {
                return Mathf.Lerp(
                    .325f,
                    .275f,
                    Mathf.InverseLerp(.315f, .425f, topY));
            }

            return Mathf.Lerp(
                .275f,
                .255f,
                Mathf.InverseLerp(.425f, .535f, topY));
        }

        private static float ArmRadius(float topY)
        {
            if (topY <= .315f)
            {
                return Mathf.Lerp(
                    .050f,
                    .045f,
                    Mathf.InverseLerp(.245f, .315f, topY));
            }

            if (topY <= .425f)
            {
                return Mathf.Lerp(
                    .045f,
                    .050f,
                    Mathf.InverseLerp(.315f, .425f, topY));
            }

            return Mathf.Lerp(
                .050f,
                .055f,
                Mathf.InverseLerp(.425f, .535f, topY));
        }

        private static float ArmTorsoBoundary(float topY)
        {
            if (topY <= .315f)
            {
                return Mathf.Lerp(
                    .360f,
                    .355f,
                    Mathf.InverseLerp(.245f, .315f, topY));
            }

            if (topY <= .425f)
            {
                return Mathf.Lerp(
                    .355f,
                    .312f,
                    Mathf.InverseLerp(.315f, .425f, topY));
            }

            return Mathf.Lerp(
                .312f,
                .300f,
                Mathf.InverseLerp(.425f, .535f, topY));
        }

        private static float LegCenterX(float topY)
        {
            if (topY <= .625f)
            {
                return Mathf.Lerp(
                    .415f,
                    .398f,
                    Mathf.InverseLerp(.54f, .625f, topY));
            }

            if (topY <= .71f)
            {
                return Mathf.Lerp(
                    .398f,
                    .380f,
                    Mathf.InverseLerp(.625f, .71f, topY));
            }

            return Mathf.Lerp(
                .380f,
                .355f,
                Mathf.InverseLerp(.71f, .80f, topY));
        }

        private static float LegRadius(float topY)
        {
            if (topY <= .625f)
            {
                return Mathf.Lerp(
                    .098f,
                    .088f,
                    Mathf.InverseLerp(.54f, .625f, topY));
            }

            return Mathf.Lerp(
                .088f,
                .084f,
                Mathf.InverseLerp(.625f, .80f, topY));
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

        private static Vector4 ResolveFullCanvasUv(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return Vector4.zero;
            }

            Rect spriteRect = sprite.rect;
            float textureWidth = Mathf.Max(1f, sprite.texture.width);
            float textureHeight = Mathf.Max(1f, sprite.texture.height);
            return new Vector4(
                spriteRect.xMin / textureWidth,
                spriteRect.yMin / textureHeight,
                spriteRect.xMax / textureWidth,
                spriteRect.yMax / textureHeight);
        }

        private static bool HasFullCanvasUv(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return false;
            }

            Rect spriteRect = sprite.rect;
            return Mathf.Abs(spriteRect.xMin) < 0.5f &&
                   Mathf.Abs(spriteRect.yMin) < 0.5f &&
                   Mathf.Abs(
                       spriteRect.width -
                       sprite.texture.width) < 0.5f &&
                   Mathf.Abs(
                       spriteRect.height -
                       sprite.texture.height) < 0.5f;
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
