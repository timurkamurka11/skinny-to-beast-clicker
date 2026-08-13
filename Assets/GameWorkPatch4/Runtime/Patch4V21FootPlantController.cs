using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Foot-target correction for the v21 room walk. Animator still supplies the
    /// authored gait rhythm, torso and arm counter-swing; in LateUpdate the legs
    /// are solved from planted/swinging foot targets instead of trusting three
    /// unrelated thigh/shin/foot angles.
    ///
    /// During stance the target remains in the same world position while the
    /// character root travels. During swing it follows a lifted arc to the next
    /// contact. The two-bone solution preserves thigh/shin length and never uses
    /// Transform scale.
    /// </summary>
    [DefaultExecutionOrder(1320)]
    [DisallowMultipleComponent]
    public sealed class Patch4V21FootPlantController : MonoBehaviour
    {
        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private Animator animator;
        [SerializeField, Range(.22f, .55f)] private float stepLengthRatio = .36f;
        [SerializeField, Range(.05f, .20f)] private float footLiftRatio = .10f;

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
        private Vector3 plantL;
        private Vector3 plantR;
        private Vector3 swingStartL;
        private Vector3 swingStartR;
        private Vector3 swingEndL;
        private Vector3 swingEndR;
        private Vector3 previousRootPosition;
        private Vector3 travelDirection = Vector3.right;
        private float previousPhase;
        private bool wasWalking;
        private bool leftSwingInitialized;
        private bool rightSwingInitialized;
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
                    leftSwingInitialized = false;
                    rightSwingInitialized = false;
                }
                return;
            }

            float phase = Mathf.Repeat(state.normalizedTime, 1f);
            UpdateTravelDirection();

            if (!wasWalking)
            {
                BeginCycle(phase);
            }
            else if (phase + .45f < previousPhase)
            {
                BeginCycle(phase, false);
            }

            // Right leg swings during the first half; left leg during the
            // second half. The opposite foot stays planted in world space.
            if (phase < .5f)
            {
                if (!rightSwingInitialized)
                {
                    BeginRightSwing();
                }
                rightSwingInitialized = true;
                leftSwingInitialized = false;

                float t = phase * 2f;
                Vector3 rightTarget = SwingArc(
                    swingStartR,
                    swingEndR,
                    t,
                    LegLength(thighR, shinR, footR) * footLiftRatio);
                SolveLeg(thighL, shinL, footL, plantL, true);
                SolveLeg(thighR, shinR, footR, rightTarget, false);
                if (phase >= .47f)
                {
                    plantR = swingEndR;
                }
            }
            else
            {
                if (!leftSwingInitialized)
                {
                    BeginLeftSwing();
                }
                leftSwingInitialized = true;
                rightSwingInitialized = false;

                float t = (phase - .5f) * 2f;
                Vector3 leftTarget = SwingArc(
                    swingStartL,
                    swingEndL,
                    t,
                    LegLength(thighL, shinL, footL) * footLiftRatio);
                SolveLeg(thighR, shinR, footR, plantR, false);
                SolveLeg(thighL, shinL, footL, leftTarget, true);
                if (phase >= .97f)
                {
                    plantL = swingEndL;
                }
            }

            previousPhase = phase;
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
            plantL = footL.position;
            plantR = footR.position;
            previousRootPosition = characterRoot.position;
        }

        private void BeginCycle(float phase, bool resetPlants = true)
        {
            if (resetPlants)
            {
                plantL = footL.position;
                plantR = footR.position;
            }
            previousRootPosition = characterRoot.position;
            previousPhase = phase;
            leftSwingInitialized = false;
            rightSwingInitialized = false;
        }

        private void BeginRightSwing()
        {
            swingStartR = footR.position;
            float length = LegLength(thighR, shinR, footR);
            Vector3 neutral = pelvis.TransformPoint(bindFootPelvisR);
            swingEndR = neutral +
                travelDirection * length * stepLengthRatio;
        }

        private void BeginLeftSwing()
        {
            swingStartL = footL.position;
            float length = LegLength(thighL, shinL, footL);
            Vector3 neutral = pelvis.TransformPoint(bindFootPelvisL);
            swingEndL = neutral +
                travelDirection * length * stepLengthRatio;
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

        private static Vector3 SwingArc(
            Vector3 start,
            Vector3 end,
            float t,
            float lift)
        {
            t = Mathf.Clamp01(t);
            float smooth = t * t * (3f - 2f * t);
            Vector3 result = Vector3.Lerp(start, end, smooth);
            result.y += 4f * t * (1f - t) * lift;
            return result;
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
