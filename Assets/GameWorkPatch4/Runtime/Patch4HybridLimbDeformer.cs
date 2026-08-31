using System;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// v21 local limb skin. One continuous painted arm/leg is deformed by its
    /// three-bone chain. Most of every segment is 100% owned by one bone; only
    /// narrow elbow/wrist or knee/ankle bands blend adjacent transforms.
    ///
    /// Bone scale is intentionally ignored. This removes the two previous
    /// failure modes at once: no broad whole-body LBS squash, and no rigid
    /// pixel cut at the internal joints.
    /// </summary>
    [DefaultExecutionOrder(1230)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class Patch4HybridLimbDeformer : BaseMeshEffect
    {
        public enum LimbProfile
        {
            LeftArm,
            RightArm,
            LeftLeg,
            RightLeg
        }

        [Serializable]
        private sealed class BoneState
        {
            public string name = string.Empty;
            public Transform bone;
            public Vector2 bindPivot;
            public float bindAngle;
        }

        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private LimbProfile profile;
        [SerializeField] private Sprite sourceSprite;
        [SerializeField, Range(12, 64)] private int gridColumns = 40;
        [SerializeField, Range(24, 128)] private int gridRows = 80;

        private readonly BoneState[] chain =
        {
            new(),
            new(),
            new()
        };

        private RectTransform imageTransform;
        private bool bound;

        public bool IsBound => bound;
        public LimbProfile Profile => profile;

        public bool TryGetDeformedSample(
            float normalizedX,
            float normalizedY,
            out Vector2 sample)
        {
            sample = default;
            ResolveReferences();
            if (!bound && !CaptureBindPose()) return false;
            if (imageTransform == null) return false;

            float u = Mathf.Clamp01(normalizedX);
            float v = Mathf.Clamp01(normalizedY);
            Rect rect = imageTransform.rect;
            Vector2 original = new(
                Mathf.Lerp(rect.xMin, rect.xMax, u),
                Mathf.Lerp(rect.yMin, rect.yMax, v));
            Vector3 weights = ResolveWeights(1f - v);
            sample = TransformByBone(0, original) * weights.x +
                TransformByBone(1, original) * weights.y +
                TransformByBone(2, original) * weights.z;
            return IsFinite(sample.x) && IsFinite(sample.y);
        }

        public void Configure(
            Patch4CharacterRigController rig,
            LimbProfile limbProfile,
            Sprite sprite,
            int columns = 40,
            int rows = 80)
        {
            rigController = rig;
            profile = limbProfile;
            sourceSprite = sprite;
            gridColumns = Mathf.Clamp(columns, 12, 64);
            gridRows = Mathf.Clamp(rows, 24, 128);
            bound = false;
            ResolveReferences();
            CaptureBindPose();
            MarkDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResolveReferences();
            if (!bound)
            {
                CaptureBindPose();
            }
        }

        private void LateUpdate()
        {
            if (bound && graphic != null && graphic.gameObject.activeInHierarchy)
            {
                graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || vertexHelper == null)
            {
                return;
            }

            ResolveReferences();
            if (!bound && !CaptureBindPose())
            {
                return;
            }

            Sprite sprite = ResolveSprite();
            if (sprite == null || imageTransform == null)
            {
                return;
            }

            Rect rect = imageTransform.rect;
            Vector4 outerUv = DataUtility.GetOuterUV(sprite);
            Color32 color = graphic.color;

            vertexHelper.Clear();
            for (int row = 0; row <= gridRows; row++)
            {
                float v = row / (float)gridRows;
                float y = Mathf.Lerp(rect.yMin, rect.yMax, v);
                float topY = 1f - v;
                Vector3 weights = ResolveWeights(topY);

                for (int column = 0; column <= gridColumns; column++)
                {
                    float u = column / (float)gridColumns;
                    float x = Mathf.Lerp(rect.xMin, rect.xMax, u);
                    Vector2 original = new(x, y);
                    Vector2 deformed =
                        TransformByBone(0, original) * weights.x +
                        TransformByBone(1, original) * weights.y +
                        TransformByBone(2, original) * weights.z;

                    UIVertex vertex = UIVertex.simpleVert;
                    vertex.position = new Vector3(deformed.x, deformed.y, 0f);
                    vertex.color = color;
                    vertex.uv0 = new Vector2(
                        Mathf.Lerp(outerUv.x, outerUv.z, u),
                        Mathf.Lerp(outerUv.y, outerUv.w, v));
                    vertexHelper.AddVert(vertex);
                }
            }

            int stride = gridColumns + 1;
            for (int row = 0; row < gridRows; row++)
            {
                for (int column = 0; column < gridColumns; column++)
                {
                    int lowerLeft = row * stride + column;
                    int lowerRight = lowerLeft + 1;
                    int upperLeft = lowerLeft + stride;
                    int upperRight = upperLeft + 1;
                    vertexHelper.AddTriangle(lowerLeft, upperLeft, upperRight);
                    vertexHelper.AddTriangle(lowerLeft, upperRight, lowerRight);
                }
            }
        }

        private bool CaptureBindPose()
        {
            ResolveReferences();
            bound = false;
            if (rigController == null || imageTransform == null)
            {
                return false;
            }

            string[] names = ResolveBoneNames(profile);
            for (int i = 0; i < chain.Length; i++)
            {
                Transform bone = rigController.GetBone(names[i]);
                if (bone == null)
                {
                    return false;
                }

                chain[i].name = names[i];
                chain[i].bone = bone;
                chain[i].bindPivot = BonePivotInImageSpace(bone);
                chain[i].bindAngle = BoneAngleInImageSpace(bone);
            }

            bound = true;
            MarkDirty();
            return true;
        }

        private Vector2 TransformByBone(int index, Vector2 original)
        {
            BoneState state = chain[index];
            if (state == null || state.bone == null)
            {
                return original;
            }

            Vector2 currentPivot = BonePivotInImageSpace(state.bone);
            float currentAngle = BoneAngleInImageSpace(state.bone);
            float delta = Mathf.DeltaAngle(state.bindAngle, currentAngle);
            float limit = IsArm(profile) ? 55f : 52f;
            delta = Mathf.Clamp(delta, -limit, limit);

            float radians = delta * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians);
            float sine = Mathf.Sin(radians);
            Vector2 offset = original - state.bindPivot;
            Vector2 rotated = new(
                offset.x * cosine - offset.y * sine,
                offset.x * sine + offset.y * cosine);
            return state.bindPivot + rotated + (currentPivot - state.bindPivot);
        }

        private Vector3 ResolveWeights(float topY)
        {
            if (IsArm(profile))
            {
                // Upper arm -> forearm. About 40 px of the 1536 px master is
                // allowed to blend; the rest remains effectively rigid.
                if (topY <= .386f)
                {
                    return new Vector3(1f, 0f, 0f);
                }
                if (topY < .425f)
                {
                    float t = Smooth01(Mathf.InverseLerp(.386f, .425f, topY));
                    return new Vector3(1f - t, t, 0f);
                }

                // Forearm -> hand.
                if (topY <= .477f)
                {
                    return new Vector3(0f, 1f, 0f);
                }
                if (topY < .512f)
                {
                    float t = Smooth01(Mathf.InverseLerp(.477f, .512f, topY));
                    return new Vector3(0f, 1f - t, t);
                }

                return new Vector3(0f, 0f, 1f);
            }

            // Thigh -> shin.
            if (topY <= .600f)
            {
                return new Vector3(1f, 0f, 0f);
            }
            if (topY < .648f)
            {
                float t = Smooth01(Mathf.InverseLerp(.600f, .648f, topY));
                return new Vector3(1f - t, t, 0f);
            }

            // Shin -> foot.
            if (topY <= .711f)
            {
                return new Vector3(0f, 1f, 0f);
            }
            if (topY < .754f)
            {
                float t = Smooth01(Mathf.InverseLerp(.711f, .754f, topY));
                return new Vector3(0f, 1f - t, t);
            }

            return new Vector3(0f, 0f, 1f);
        }

        private void ResolveReferences()
        {
            if (graphic != null && imageTransform == null)
            {
                imageTransform = graphic.rectTransform;
            }
            if (rigController == null)
            {
                rigController =
                    GetComponentInParent<Patch4CharacterRigController>(true);
            }
        }

        private Sprite ResolveSprite()
        {
            if (sourceSprite != null)
            {
                return sourceSprite;
            }
            Image image = graphic as Image;
            return image != null ? image.sprite : null;
        }

        private Vector2 BonePivotInImageSpace(Transform bone)
        {
            Vector3 local = imageTransform.InverseTransformPoint(bone.position);
            return new Vector2(local.x, local.y);
        }

        private float BoneAngleInImageSpace(Transform bone)
        {
            Vector3 origin = imageTransform.InverseTransformPoint(bone.position);
            Vector3 right = imageTransform.InverseTransformPoint(
                bone.TransformPoint(Vector3.right));
            Vector3 direction = right - origin;
            if (direction.sqrMagnitude < .000001f)
            {
                return 0f;
            }
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        private static string[] ResolveBoneNames(LimbProfile value)
        {
            switch (value)
            {
                case LimbProfile.LeftArm:
                    return new[] { "UpperArmL", "ForearmL", "HandL" };
                case LimbProfile.RightArm:
                    return new[] { "UpperArmR", "ForearmR", "HandR" };
                case LimbProfile.LeftLeg:
                    return new[] { "ThighL", "ShinL", "FootL" };
                case LimbProfile.RightLeg:
                    return new[] { "ThighR", "ShinR", "FootR" };
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static bool IsArm(LimbProfile value)
        {
            return value == LimbProfile.LeftArm || value == LimbProfile.RightArm;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        private void MarkDirty()
        {
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }
    }
}
