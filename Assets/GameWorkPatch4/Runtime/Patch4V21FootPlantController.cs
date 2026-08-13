using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Continuous foot-target correction for the v21 room walk. Animator still
    /// supplies torso and arm counter-swing; LateUpdate gives both legs one
    /// mirrored, phase-locked support/swing cycle. The targets are evaluated
    /// around the moving pelvis, so room travel cannot stretch a planted leg to
    /// its reach limit or leave one foot almost motionless.
    ///
    /// The two-bone solution preserves thigh/shin length, blends into the gait
    /// instead of snapping on state entry, and never uses Transform scale.
    /// </summary>
    [DefaultExecutionOrder(1320)]
    [DisallowMultipleComponent]
    public sealed class Patch4V21FootPlantController : MonoBehaviour
    {
        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private Animator animator;
        [SerializeField, Range(.22f, .55f)] private float stepLengthRatio = .42f;
        [SerializeField, Range(.05f, .20f)] private float footLiftRatio = .11f;

        private Transform pelvis;
        private Transform characterRoot;
        private Transform thighL;
        private Transform shinL;
        private Transform footL;
        private Transform thighR;
        private Transform shinR;
        private Transform footR;

        private Quaternion bindThighL;
        private Quaternion bindShinL;
        private Quaternion bindFootL;
        private Quaternion bindThighR;
        private Quaternion bindShinR;
        private Quaternion bindFootR;

        private Vector3 bindFootPelvisL;
        private Vector3 bindFootPelvisR;
        private Vector3 previousRootPosition;
        private Vector3 travelDirection = Vector3.right;
        private float walkBlend;
        private bool wasWalking;
        private bool ready;

        private void Reset()
        {
            rigController = GetComponent<Patch4CharacterRigController>();
            animator = GetComponent<Animator>();
        }

        private void Awake()
        {
            Resolve();
        }

        private void OnEnable()
        {
            Resolve();
        }

        private void LateUpdate()
        {
            if (!ready || animator == null || rigController == null)
            {
                return;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool walking =
                state.IsName("Base Layer.FatMan_Walk_InRoom") ||
                state.IsName("FatMan_Walk_InRoom");
            if (!walking)
            {
                if (wasWalking)
                {
                    RestoreBindRotations();
                    wasWalking = false;
                    walkBlend = 0f;
                }
                return;
            }

            float phase = Mathf.Repeat(state.normalizedTime, 1f);
            UpdateTravelDirection();

            walkBlend = Mathf.MoveTowards(
                walkBlend,
                1f,
                Time.unscaledDeltaTime / .20f);
            float blend = Mathf.SmoothStep(0f, 1f, walkBlend);

            // One gait function drives both legs, exactly half a cycle apart.
            // This makes their ranges identical and keeps the loop continuous
            // at phase 0/1. A support foot rolls back relative to the pelvis as
            // the opposite foot follows its eased, lifted transfer arc.
            Vector3 rightTarget = EvaluateGaitTarget(
                bindFootPelvisR,
                LegLength(thighR, shinR, footR),
                phase,
                blend);
            Vector3 leftTarget = EvaluateGaitTarget(
                bindFootPelvisL,
                LegLength(thighL, shinL, footL),
                Mathf.Repeat(phase + .5f, 1f),
                blend);

            SolveLeg(thighL, shinL, footL, leftTarget, true);
            SolveLeg(thighR, shinR, footR, rightTarget, false);

            previousRootPosition = characterRoot.position;
            wasWalking = true;
        }

        private void Resolve()
        {
            if (rigController == null)
            {
                rigController = GetComponent<Patch4CharacterRigController>();
            }
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            if (rigController == null)
            {
                ready = false;
                return;
            }

            pelvis = rigController.GetBone("Pelvis");
            characterRoot = rigController.GetBone("CharacterRoot");
            thighL = rigController.GetBone("ThighL");
            shinL = rigController.GetBone("ShinL");
            footL = rigController.GetBone("FootL");
            thighR = rigController.GetBone("ThighR");
            shinR = rigController.GetBone("ShinR");
            footR = rigController.GetBone("FootR");

            ready = pelvis != null && characterRoot != null &&
                    thighL != null && shinL != null && footL != null &&
                    thighR != null && shinR != null && footR != null;
            if (!ready)
            {
                return;
            }

            bindThighL = thighL.localRotation;
            bindShinL = shinL.localRotation;
            bindFootL = footL.localRotation;
            bindThighR = thighR.localRotation;
            bindShinR = shinR.localRotation;
            bindFootR = footR.localRotation;
            bindFootPelvisL = pelvis.InverseTransformPoint(footL.position);
            bindFootPelvisR = pelvis.InverseTransformPoint(footR.position);
            previousRootPosition = characterRoot.position;
        }

        private void UpdateTravelDirection()
        {
            Vector3 delta = characterRoot.position - previousRootPosition;
            delta.z = 0f;
            float threshold = .00005f * Mathf.Max(1f, transform.lossyScale.x);
            if (delta.sqrMagnitude > threshold * threshold)
            {
                travelDirection = delta.normalized;
            }
        }

        private Vector3 EvaluateGaitTarget(
            Vector3 bindFootPelvis,
            float legLength,
            float phase,
            float blend)
        {
            phase = Mathf.Repeat(phase, 1f);
            bool transferring = phase < .5f;
            float halfPhase = transferring
                ? phase * 2f
                : (phase - .5f) * 2f;
            float eased = Smooth01(halfPhase);
            float halfStride = legLength * stepLengthRatio * .5f;
            float forward = transferring
                ? Mathf.Lerp(-halfStride, halfStride, eased)
                : Mathf.Lerp(halfStride, -halfStride, eased);
            float lift = transferring
                ? Mathf.Sin(Mathf.PI * eased) * legLength * footLiftRatio
                : 0f;

            Vector3 neutral = pelvis.TransformPoint(bindFootPelvis);
            Vector3 gait =
                neutral + travelDirection * forward + Vector3.up * lift;
            return Vector3.Lerp(neutral, gait, blend);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float LegLength(
            Transform thigh,
            Transform shin,
            Transform foot)
        {
            return Vector3.Distance(thigh.position, shin.position) +
                   Vector3.Distance(shin.position, foot.position);
        }

        private static void SolveLeg(
            Transform thigh,
            Transform shin,
            Transform foot,
            Vector3 targetWorld,
            bool left)
        {
            Vector2 hip = thigh.position;
            Vector2 currentKnee = shin.position;
            Vector2 target = targetWorld;

            float upperLength = Vector2.Distance(thigh.position, shin.position);
            float lowerLength = Vector2.Distance(shin.position, foot.position);
            if (upperLength < .0001f || lowerLength < .0001f)
            {
                return;
            }

            Vector2 hipToTarget = target - hip;
            float rawDistance = hipToTarget.magnitude;
            if (rawDistance < .0001f)
            {
                return;
            }

            float minDistance = Mathf.Abs(upperLength - lowerLength) + .001f;
            float maxDistance = upperLength + lowerLength - .001f;
            float distance = Mathf.Clamp(rawDistance, minDistance, maxDistance);
            Vector2 direction = hipToTarget / rawDistance;
            Vector2 clampedTarget = hip + direction * distance;

            float along =
                (upperLength * upperLength - lowerLength * lowerLength +
                 distance * distance) /
                (2f * distance);
            float heightSquared = Mathf.Max(
                0f,
                upperLength * upperLength - along * along);
            float height = Mathf.Sqrt(heightSquared);
            Vector2 basePoint = hip + direction * along;
            Vector2 perpendicular = new(-direction.y, direction.x);

            float currentSide = Cross(
                direction,
                currentKnee - hip);
            float side = Mathf.Abs(currentSide) > .0001f
                ? Mathf.Sign(currentSide)
                : left ? -1f : 1f;
            Vector2 knee = basePoint + perpendicular * height * side;

            OrientChildVector(thigh, shin.localPosition, knee - hip);
            // After the thigh rotates, shin.position follows the solved knee.
            Vector2 solvedKnee = shin.position;
            OrientChildVector(shin, foot.localPosition, clampedTarget - solvedKnee);

            // Keep the shoe visually level with the room floor. Bone scale is
            // untouched, and the whole-leg skin blends only near the ankle.
            float parentWorld = shin.eulerAngles.z;
            foot.localRotation = Quaternion.Euler(0f, 0f, -parentWorld);
        }

        private static void OrientChildVector(
            Transform parent,
            Vector3 restChildLocal,
            Vector2 desiredWorldDirection)
        {
            if (desiredWorldDirection.sqrMagnitude < .000001f ||
                restChildLocal.sqrMagnitude < .000001f)
            {
                return;
            }

            float desiredWorld = Mathf.Atan2(
                desiredWorldDirection.y,
                desiredWorldDirection.x) * Mathf.Rad2Deg;
            float restLocal = Mathf.Atan2(
                restChildLocal.y,
                restChildLocal.x) * Mathf.Rad2Deg;
            float parentOfParentWorld = parent.parent != null
                ? parent.parent.eulerAngles.z
                : 0f;
            float local = Mathf.DeltaAngle(
                0f,
                desiredWorld - parentOfParentWorld - restLocal);
            parent.localRotation = Quaternion.Euler(0f, 0f, local);
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private void RestoreBindRotations()
        {
            thighL.localRotation = bindThighL;
            shinL.localRotation = bindShinL;
            footL.localRotation = bindFootL;
            thighR.localRotation = bindThighR;
            shinR.localRotation = bindShinR;
            footR.localRotation = bindFootR;
        }
    }
}
