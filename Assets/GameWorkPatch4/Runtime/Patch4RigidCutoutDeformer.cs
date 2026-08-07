using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Moves one transparent full-canvas anatomy cutout as a rigid 2D piece.
    /// It intentionally ignores animated bone scale and never averages matrices,
    /// so arm/leg motion cannot squash or stretch painted pixels.
    /// </summary>
    [DefaultExecutionOrder(1225)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class Patch4RigidCutoutDeformer : BaseMeshEffect
    {
        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private string boneName = string.Empty;
        [SerializeField, Range(0f, 1f)] private float rotationMultiplier = 1f;
        [SerializeField, Range(0f, 1f)] private float translationMultiplier = 1f;
        [SerializeField, Range(0f, 30f)] private float maxRotationDegrees = 18f;

        private Transform bone;
        private RectTransform imageTransform;
        private Vector2 bindPivot;
        private float bindAngle;
        private bool bound;

        public bool IsBound => bound && bone != null;
        public string BoneName => boneName;

        public void Configure(
            Patch4CharacterRigController rig,
            string targetBoneName,
            float rotationScale,
            float translationScale,
            float rotationLimit)
        {
            rigController = rig;
            boneName = targetBoneName ?? string.Empty;
            rotationMultiplier = Mathf.Clamp01(rotationScale);
            translationMultiplier = Mathf.Clamp01(translationScale);
            maxRotationDegrees = Mathf.Clamp(rotationLimit, 0f, 30f);
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
            if (IsBound &&
                graphic != null &&
                graphic.gameObject.activeInHierarchy)
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

            Vector2 currentPivot = BonePivotInImageSpace();
            float currentAngle = BoneAngleInImageSpace();
            float deltaAngle = Mathf.DeltaAngle(bindAngle, currentAngle) *
                               rotationMultiplier;
            deltaAngle = Mathf.Clamp(
                deltaAngle,
                -maxRotationDegrees,
                maxRotationDegrees);
            Vector2 translation =
                (currentPivot - bindPivot) * translationMultiplier;

            float radians = deltaAngle * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians);
            float sine = Mathf.Sin(radians);

            UIVertex vertex = default;
            for (int i = 0; i < vertexHelper.currentVertCount; i++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, i);
                Vector2 original = vertex.position;
                Vector2 offset = original - bindPivot;
                Vector2 rotated = new(
                    offset.x * cosine - offset.y * sine,
                    offset.x * sine + offset.y * cosine);
                Vector2 deformed = bindPivot + rotated + translation;
                vertex.position = new Vector3(
                    deformed.x,
                    deformed.y,
                    vertex.position.z);
                vertexHelper.SetUIVertex(vertex, i);
            }
        }

        private void ResolveReferences()
        {
            if (imageTransform == null && graphic != null)
            {
                imageTransform = graphic.rectTransform;
            }

            if (rigController == null)
            {
                rigController =
                    GetComponentInParent<Patch4CharacterRigController>(true);
            }

            if (bone == null &&
                rigController != null &&
                !string.IsNullOrWhiteSpace(boneName))
            {
                bone = rigController.GetBone(boneName);
            }
        }

        private bool CaptureBindPose()
        {
            ResolveReferences();
            bound = false;
            if (imageTransform == null ||
                rigController == null ||
                string.IsNullOrWhiteSpace(boneName))
            {
                return false;
            }

            bone = rigController.GetBone(boneName);
            if (bone == null)
            {
                return false;
            }

            bindPivot = BonePivotInImageSpace();
            bindAngle = BoneAngleInImageSpace();
            bound = true;
            return true;
        }

        private Vector2 BonePivotInImageSpace()
        {
            if (imageTransform == null || bone == null)
            {
                return Vector2.zero;
            }

            Vector3 local = imageTransform.InverseTransformPoint(bone.position);
            return new Vector2(local.x, local.y);
        }

        private float BoneAngleInImageSpace()
        {
            if (imageTransform == null || bone == null)
            {
                return 0f;
            }

            Vector3 origin = imageTransform.InverseTransformPoint(bone.position);
            Vector3 right = imageTransform.InverseTransformPoint(
                bone.TransformPoint(Vector3.right));
            Vector3 direction = right - origin;
            if (direction.sqrMagnitude < 0.000001f)
            {
                return 0f;
            }

            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        private void MarkDirty()
        {
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }
    }
}
