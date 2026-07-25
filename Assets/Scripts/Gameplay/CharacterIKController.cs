using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Two-bone foot planting applied after Animator evaluation. One leg is in
    /// stance during each half of the walk cycle: its ankle stays at a captured
    /// world point while the solver rotates the thigh and shin. The other leg
    /// remains under the authored swing clip.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(8500)]
    public sealed class CharacterIKController : MonoBehaviour
    {
        private RectTransform leftThigh;
        private RectTransform leftShin;
        private RectTransform leftFoot;
        private RectTransform rightThigh;
        private RectTransform rightShin;
        private RectTransform rightFoot;
        private Animator animator;
        private Vector3 leftPlantWorld;
        private Vector3 rightPlantWorld;
        private CharacterFacing facing;
        private bool locomotionActive;
        private bool stanceInitialized;
        private bool leftWasStance;
        private bool configured;

        public float LastPlantError { get; private set; }

        public void Configure(
            RectTransform leftThighBone,
            RectTransform leftShinBone,
            RectTransform leftFootBone,
            RectTransform rightThighBone,
            RectTransform rightShinBone,
            RectTransform rightFootBone,
            Animator targetAnimator)
        {
            leftThigh = leftThighBone;
            leftShin = leftShinBone;
            leftFoot = leftFootBone;
            rightThigh = rightThighBone;
            rightShin = rightShinBone;
            rightFoot = rightFootBone;
            animator = targetAnimator;
            configured =
                leftThigh != null &&
                leftShin != null &&
                leftFoot != null &&
                rightThigh != null &&
                rightShin != null &&
                rightFoot != null &&
                animator != null;
            ReplantBothFeet();
        }

        public void SetLocomotion(
            bool active,
            CharacterFacing nextFacing)
        {
            bool directionChanged = facing != nextFacing;
            facing = nextFacing;
            if (active &&
                (!locomotionActive || directionChanged))
            {
                ReplantBothFeet();
            }

            locomotionActive = active;
            if (!active)
            {
                stanceInitialized = false;
                LastPlantError = 0f;
            }
        }

        public void ResetDiagnostics()
        {
            LastPlantError = 0f;
        }

        private void LateUpdate()
        {
            if (!configured || !locomotionActive)
            {
                return;
            }

            AnimatorStateInfo state =
                animator.IsInTransition(0)
                    ? animator.GetNextAnimatorStateInfo(0)
                    : animator.GetCurrentAnimatorStateInfo(0);
            float phase =
                state.normalizedTime -
                Mathf.Floor(state.normalizedTime);
            bool leftStance = phase < 0.5f;
            if (!stanceInitialized)
            {
                ReplantBothFeet();
                leftWasStance = leftStance;
                stanceInitialized = true;
            }
            else if (leftStance != leftWasStance)
            {
                if (leftStance)
                {
                    leftPlantWorld = leftFoot.position;
                }
                else
                {
                    rightPlantWorld = rightFoot.position;
                }

                leftWasStance = leftStance;
            }

            if (leftStance)
            {
                SolveLeg(
                    leftThigh,
                    leftShin,
                    leftFoot,
                    ref leftPlantWorld,
                    -1f);
                LastPlantError =
                    Vector2.Distance(
                        leftFoot.position,
                        leftPlantWorld);
            }
            else
            {
                SolveLeg(
                    rightThigh,
                    rightShin,
                    rightFoot,
                    ref rightPlantWorld,
                    1f);
                LastPlantError =
                    Vector2.Distance(
                        rightFoot.position,
                        rightPlantWorld);
            }
        }

        private void ReplantBothFeet()
        {
            if (leftFoot != null)
            {
                leftPlantWorld = leftFoot.position;
            }

            if (rightFoot != null)
            {
                rightPlantWorld = rightFoot.position;
            }

            stanceInitialized = false;
            LastPlantError = 0f;
        }

        private static void SolveLeg(
            RectTransform thigh,
            RectTransform shin,
            RectTransform foot,
            ref Vector3 targetWorld,
            float bendSign)
        {
            Transform hipSpace = thigh.parent;
            if (hipSpace == null)
            {
                return;
            }

            Vector2 hip =
                thigh.localPosition;
            Vector2 target =
                hipSpace.InverseTransformPoint(targetWorld);
            float thighLength =
                Mathf.Max(1f, shin.anchoredPosition.magnitude);
            float shinLength =
                Mathf.Max(1f, foot.anchoredPosition.magnitude);
            Vector2 toTarget = target - hip;
            float rawDistance =
                Mathf.Max(0.001f, toTarget.magnitude);
            float minimumReach =
                Mathf.Abs(thighLength - shinLength) + 0.01f;
            float maximumReach =
                thighLength + shinLength - 0.01f;
            float distance =
                Mathf.Clamp(
                    rawDistance,
                    minimumReach,
                    maximumReach);
            Vector2 reachableTarget =
                hip + toTarget / rawDistance * distance;
            if (!Mathf.Approximately(
                    distance,
                    rawDistance))
            {
                // A depth-scale change or very long stride can move the hip
                // outside the chain's physical reach. Release only the excess
                // distance and replant at the reachable edge; never stretch a
                // joint apart to preserve an impossible target.
                targetWorld =
                    hipSpace.TransformPoint(reachableTarget);
            }

            float baseAngle =
                Mathf.Atan2(
                    reachableTarget.y - hip.y,
                    reachableTarget.x - hip.x) *
                Mathf.Rad2Deg;
            float cosine =
                (thighLength * thighLength +
                 distance * distance -
                 shinLength * shinLength) /
                (2f * thighLength * distance);
            float offset =
                Mathf.Acos(Mathf.Clamp(cosine, -1f, 1f)) *
                Mathf.Rad2Deg;
            float thighDirection =
                baseAngle + bendSign * offset;
            thigh.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    thighDirection + 90f);

            Vector2 targetInThigh =
                thigh.InverseTransformPoint(targetWorld);
            Vector2 knee = shin.localPosition;
            Vector2 kneeToTarget =
                targetInThigh - knee;
            float shinDirection =
                Mathf.Atan2(
                    kneeToTarget.y,
                    kneeToTarget.x) *
                Mathf.Rad2Deg;
            shin.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    shinDirection + 90f);

            // The foot remains an independent bone and cancels the inherited
            // leg rotation so the sole stays parallel to the room floor.
            foot.rotation = Quaternion.identity;
        }
    }
}
