using SkinnyToBeast.Gameplay;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Mirrors the already-working gameplay signals into the isolated Patch 4
    /// Animator. This avoids editing the existing tap, routine, movement, skin,
    /// menu or settings systems while Patch 4 remains behind its feature flag.
    /// </summary>
    [DefaultExecutionOrder(980)]
    [DisallowMultipleComponent]
    public sealed class Patch4LegacySignalBridge : MonoBehaviour
    {
        [SerializeField] private CharacterRigController legacyRig;
        [SerializeField] private CharacterSkinController legacySkin;
        [SerializeField] private Patch4CharacterStateMachine stateMachine;
        [SerializeField] private Patch4FaceController faceController;
        [SerializeField, Min(0.05f)] private float turnPulseDuration = 0.24f;
        [SerializeField, Min(0.05f)] private float mouthReactionDuration = 0.42f;

        private int observedTapCount;
        private int observedStage = -1;
        private CharacterFacing observedFacing;
        private float turningUntil;
        private float mouthResetAt;
        private bool initialized;

        private void Reset()
        {
            stateMachine = GetComponent<Patch4CharacterStateMachine>();
            faceController = GetComponent<Patch4FaceController>();
        }

        private void OnEnable()
        {
            InitializeObservation();
        }

        public void BindLegacy(
            CharacterRigController rig,
            CharacterSkinController skin)
        {
            legacyRig = rig;
            legacySkin = skin;
            initialized = false;
            InitializeObservation();
        }

        private void Update()
        {
            if (legacyRig == null || stateMachine == null)
            {
                return;
            }

            if (!initialized)
            {
                InitializeObservation();
            }

            MirrorTapSignals();
            MirrorStageSignals();
            MirrorMovementAndRoutine();
            ResetMouthWhenDue();
        }

        private void InitializeObservation()
        {
            if (legacyRig != null)
            {
                observedTapCount = legacyRig.AcceptedTapCount;
                observedFacing = legacyRig.Facing;
            }

            if (legacySkin != null)
            {
                observedStage = legacySkin.CurrentArtIndex;
            }

            initialized = true;
        }

        private void MirrorTapSignals()
        {
            int tapCount = legacyRig.AcceptedTapCount;
            if (tapCount <= observedTapCount)
            {
                observedTapCount = tapCount;
                return;
            }

            int delta = tapCount - observedTapCount;
            observedTapCount = tapCount;
            for (int i = 0; i < delta; i++)
            {
                int variant = ((tapCount - delta + i) & 1) + 1;
                stateMachine.PlayTapReaction(variant);
            }

            if (faceController != null)
            {
                faceController.SetMouth(Patch4FaceController.MouthPose.Open);
                mouthResetAt = Time.unscaledTime + mouthReactionDuration;
            }
        }

        private void MirrorStageSignals()
        {
            if (legacySkin == null)
            {
                return;
            }

            int currentStage = legacySkin.CurrentArtIndex;
            if (observedStage < 0)
            {
                observedStage = currentStage;
                return;
            }

            if (currentStage == observedStage)
            {
                return;
            }

            observedStage = currentStage;
            stateMachine.PlayUpgradeReaction();
            if (faceController != null)
            {
                faceController.SetMouth(Patch4FaceController.MouthPose.Smile);
                mouthResetAt = Time.unscaledTime + 0.9f;
            }
        }

        private void MirrorMovementAndRoutine()
        {
            stateMachine.SetWalkSpeed(legacyRig.IsMoving ? 1f : 0f);

            CharacterRoutineAction action = legacyRig.ActiveAction;
            stateMachine.SetLooking(action == CharacterRoutineAction.LookAround);
            stateMachine.SetSittingOrLeaning(
                action == CharacterRoutineAction.Sit ||
                action == CharacterRoutineAction.SitDown ||
                action == CharacterRoutineAction.SitLoop ||
                action == CharacterRoutineAction.StandUp);

            CharacterFacing currentFacing = legacyRig.Facing;
            if (currentFacing != observedFacing)
            {
                observedFacing = currentFacing;
                turningUntil = Time.unscaledTime + turnPulseDuration;
            }

            stateMachine.SetTurning(Time.unscaledTime < turningUntil);
        }

        private void ResetMouthWhenDue()
        {
            if (faceController != null &&
                mouthResetAt > 0f &&
                Time.unscaledTime >= mouthResetAt)
            {
                mouthResetAt = 0f;
                faceController.SetMouth(Patch4FaceController.MouthPose.Closed);
            }
        }
    }
}
