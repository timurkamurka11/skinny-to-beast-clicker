using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Secondary motion for the belly, shirt hem, chest and double chin.
    /// These helper bones sit below the existing Animator bones, so the
    /// authored 26 clips remain the primary motion and soft tissue only adds
    /// a delayed follow-through.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterSoftBodyController : MonoBehaviour
    {
        private RectTransform actorRoot;
        private RectTransform bellyBone;
        private RectTransform shirtHemBone;
        private RectTransform chestSoftBone;
        private RectTransform chinSoftBone;

        private Vector2 bellyBase;
        private Vector2 shirtHemBase;
        private Vector2 chestBase;
        private Vector2 chinBase;
        private Vector2 lastActorPosition;

        private float softness = 1f;
        private float bellyDrop = 24f;
        private float locomotion;
        private float impulse;
        private float phase;
        private float bellyAngle;
        private float bellyAngleVelocity;
        private float bellyX;
        private float bellyXVelocity;
        private float bellyY;
        private float bellyYVelocity;
        private float shirtAngle;
        private float shirtAngleVelocity;
        private float chestScale;
        private float chestScaleVelocity;
        private float chinY;
        private float chinYVelocity;
        private bool configured;

        public bool HasCompleteRig =>
            configured &&
            actorRoot != null &&
            bellyBone != null &&
            shirtHemBone != null &&
            chestSoftBone != null &&
            chinSoftBone != null;
        public int SoftBoneCount => HasCompleteRig ? 4 : 0;
        public float MotionMagnitude { get; private set; }

        public void Configure(
            RectTransform root,
            RectTransform belly,
            RectTransform shirtHem,
            RectTransform chest,
            RectTransform chin)
        {
            actorRoot = root;
            bellyBone = belly;
            shirtHemBone = shirtHem;
            chestSoftBone = chest;
            chinSoftBone = chin;
            configured =
                actorRoot != null &&
                bellyBone != null &&
                shirtHemBone != null &&
                chestSoftBone != null &&
                chinSoftBone != null;

            if (!configured)
            {
                return;
            }

            bellyBase = bellyBone.anchoredPosition;
            shirtHemBase = shirtHemBone.anchoredPosition;
            chestBase = chestSoftBone.anchoredPosition;
            chinBase = chinSoftBone.anchoredPosition;
            lastActorPosition = actorRoot.anchoredPosition;
            ResetState();
        }

        public void ApplyAppearance(CharacterAppearance appearance)
        {
            softness = Mathf.Clamp(appearance.softness, 0.08f, 1.35f);
            bellyDrop = Mathf.Clamp(appearance.bellyDrop, 4f, 46f);
            if (HasCompleteRig)
            {
                bellyBone.anchoredPosition =
                    bellyBase + new Vector2(0f, -bellyDrop);
                shirtHemBone.anchoredPosition =
                    shirtHemBase + new Vector2(0f, -bellyDrop * 0.32f);
            }
        }

        public void SetLocomotion(bool isMoving, float speed)
        {
            locomotion = isMoving
                ? Mathf.Clamp(speed, 0.2f, 1.6f)
                : 0f;
        }

        public void AddImpulse(float strength)
        {
            impulse = Mathf.Clamp(
                impulse + Mathf.Max(0f, strength),
                0f,
                2.4f);
        }

        public void ResetState()
        {
            if (!HasCompleteRig)
            {
                return;
            }

            phase = 0f;
            impulse = 0f;
            locomotion = 0f;
            bellyAngle = 0f;
            bellyAngleVelocity = 0f;
            bellyX = 0f;
            bellyXVelocity = 0f;
            bellyY = -bellyDrop;
            bellyYVelocity = 0f;
            shirtAngle = 0f;
            shirtAngleVelocity = 0f;
            chestScale = 0f;
            chestScaleVelocity = 0f;
            chinY = 0f;
            chinYVelocity = 0f;
            MotionMagnitude = 0f;
            lastActorPosition = actorRoot.anchoredPosition;
            ApplyTransforms();
        }

        private void LateUpdate()
        {
            if (!HasCompleteRig)
            {
                return;
            }

            float delta = Mathf.Clamp(
                Time.unscaledDeltaTime,
                0.0001f,
                0.05f);
            Vector2 actorPosition = actorRoot.anchoredPosition;
            Vector2 actorVelocity =
                (actorPosition - lastActorPosition) / delta;
            lastActorPosition = actorPosition;

            phase += delta * Mathf.Lerp(2.2f, 8.6f, locomotion);
            float walkWave = Mathf.Sin(phase);
            float stepWave = Mathf.Sin(phase * 2f + 0.7f);
            float horizontalInertia =
                Mathf.Clamp(-actorVelocity.x * 0.026f, -12f, 12f);
            float verticalInertia =
                Mathf.Clamp(-actorVelocity.y * 0.012f, -8f, 8f);
            float activeSoftness =
                softness * (0.45f + locomotion * 0.55f);

            float targetX =
                (walkWave * 7f * locomotion + horizontalInertia) *
                activeSoftness;
            float targetY =
                -bellyDrop +
                (Mathf.Abs(stepWave) * 5f * locomotion +
                 impulse * 11f +
                 verticalInertia) *
                softness;
            float targetBellyAngle =
                (-walkWave * 3.6f * locomotion -
                 horizontalInertia * 0.26f +
                 impulse * 2.2f) *
                softness;
            float targetShirtAngle =
                targetBellyAngle * 1.24f -
                stepWave * locomotion * softness;
            float targetChestScale =
                (Mathf.Sin(phase * 0.48f) * 0.012f +
                 Mathf.Abs(stepWave) * locomotion * 0.014f +
                 impulse * 0.025f) *
                Mathf.Lerp(0.55f, 1f, softness);
            float targetChinY =
                (Mathf.Abs(stepWave) * locomotion * 2.8f +
                 impulse * 5.5f) *
                softness;

            float response = Mathf.Lerp(0.09f, 0.18f, 1f - softness / 1.35f);
            bellyX = Mathf.SmoothDamp(
                bellyX,
                targetX,
                ref bellyXVelocity,
                response,
                Mathf.Infinity,
                delta);
            bellyY = Mathf.SmoothDamp(
                bellyY,
                targetY,
                ref bellyYVelocity,
                response * 1.12f,
                Mathf.Infinity,
                delta);
            bellyAngle = Mathf.SmoothDampAngle(
                bellyAngle,
                targetBellyAngle,
                ref bellyAngleVelocity,
                response * 1.18f,
                Mathf.Infinity,
                delta);
            shirtAngle = Mathf.SmoothDampAngle(
                shirtAngle,
                targetShirtAngle,
                ref shirtAngleVelocity,
                response * 1.34f,
                Mathf.Infinity,
                delta);
            chestScale = Mathf.SmoothDamp(
                chestScale,
                targetChestScale,
                ref chestScaleVelocity,
                0.12f,
                Mathf.Infinity,
                delta);
            chinY = Mathf.SmoothDamp(
                chinY,
                targetChinY,
                ref chinYVelocity,
                response * 1.42f,
                Mathf.Infinity,
                delta);

            impulse = Mathf.MoveTowards(
                impulse,
                0f,
                delta * 3.8f);
            ApplyTransforms();

            MotionMagnitude =
                Mathf.Abs(bellyX) +
                Mathf.Abs(bellyY + bellyDrop) +
                Mathf.Abs(bellyAngle) +
                Mathf.Abs(shirtAngle) +
                Mathf.Abs(chinY);
        }

        private void ApplyTransforms()
        {
            bellyBone.anchoredPosition =
                bellyBase + new Vector2(bellyX, bellyY);
            bellyBone.localRotation =
                Quaternion.Euler(0f, 0f, bellyAngle);

            shirtHemBone.anchoredPosition =
                shirtHemBase +
                new Vector2(
                    bellyX * 0.48f,
                    -bellyDrop * 0.32f +
                    (bellyY + bellyDrop) * 0.72f);
            shirtHemBone.localRotation =
                Quaternion.Euler(0f, 0f, shirtAngle);

            chestSoftBone.anchoredPosition =
                chestBase + new Vector2(
                    bellyX * 0.08f,
                    chestScale * 80f);
            chestSoftBone.localScale =
                new Vector3(
                    1f + chestScale * 0.55f,
                    1f + chestScale,
                    1f);

            chinSoftBone.anchoredPosition =
                chinBase + new Vector2(
                    bellyX * 0.035f,
                    -chinY);
            chinSoftBone.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    -bellyAngle * 0.16f);
        }

        private void OnDisable()
        {
            if (HasCompleteRig)
            {
                ResetState();
            }
        }
    }
}
