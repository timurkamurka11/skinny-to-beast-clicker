using SkinnyToBeast.Economy;
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
        [SerializeField] private UpgradeManager upgradeManager;
        [SerializeField] private Patch4CharacterStateMachine stateMachine;
        [SerializeField] private Patch4FaceController faceController;
        [SerializeField, Min(0.05f)] private float turnPulseDuration = 0.24f;
        [SerializeField, Min(0.05f)] private float mouthReactionDuration = 0.42f;
        [SerializeField, Min(0.5f)] private float minimumBlinkDelay = 2.8f;
        [SerializeField, Min(0.5f)] private float maximumBlinkDelay = 5.4f;

        private int observedTapCount;
        private int observedStage = -1;
        private CharacterFacing observedFacing;
        private float turningUntil;
        private float mouthResetAt;
        private float nextBlinkAt;
        private float nextUpgradeManagerLookupAt;
        private float lastUpgradeReactionAt = -10f;
        private bool upgradeManagerSubscribed;
        private bool initialized;

        private void Reset()
        {
            stateMachine = GetComponent<Patch4CharacterStateMachine>();
            faceController = GetComponent<Patch4FaceController>();
        }

        private void OnEnable()
        {
            BindUpgradeManager(upgradeManager);
            InitializeObservation();
        }

        private void OnDisable()
        {
            UnsubscribeUpgradeManager();
        }

        public void BindLegacy(
            CharacterRigController rig,
            CharacterSkinController skin)
        {
            legacyRig = rig;
            legacySkin = skin;
            initialized = false;
            TryResolveUpgradeManager(true);
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

            TryResolveUpgradeManager(false);
            MirrorTapSignals();
            MirrorStageSignals();
            MirrorMovementAndRoutine();
            MirrorIdleBlink();
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

            ScheduleNextBlink();
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
            TriggerUpgradeReaction();
            if (faceController != null)
            {
                faceController.SetMouth(Patch4FaceController.MouthPose.Smile);
                mouthResetAt = Time.unscaledTime + 0.9f;
            }
        }

        private void MirrorMovementAndRoutine()
        {
            bool moving = legacyRig.IsMoving;
            CharacterRoutineAction action = legacyRig.ActiveAction;
            bool sitting = !moving && (
                action == CharacterRoutineAction.Sit ||
                action == CharacterRoutineAction.SitDown ||
                action == CharacterRoutineAction.SitLoop ||
                action == CharacterRoutineAction.StandUp);
            bool looking =
                !moving &&
                !sitting &&
                action == CharacterRoutineAction.LookAround;
            bool shifting =
                !moving &&
                !sitting &&
                !looking &&
                action == CharacterRoutineAction.ShiftWeight;

            stateMachine.SetWalkSpeed(moving ? 1f : 0f);
            stateMachine.SetLooking(looking);
            stateMachine.SetShiftingWeight(shifting);
            stateMachine.SetSittingOrLeaning(sitting);

            CharacterFacing currentFacing = legacyRig.Facing;
            if (currentFacing != observedFacing)
            {
                observedFacing = currentFacing;
                turningUntil = Time.unscaledTime + turnPulseDuration;
            }

            stateMachine.SetTurning(
                !moving &&
                !sitting &&
                legacyRig.ActiveActionRemaining <= 0f &&
                Time.unscaledTime < turningUntil);
        }

        private void MirrorIdleBlink()
        {
            if (Time.unscaledTime < nextBlinkAt || legacyRig == null)
            {
                return;
            }

            if (legacyRig.IsMoving ||
                legacyRig.ActiveAction != CharacterRoutineAction.None ||
                legacyRig.ActiveActionRemaining > 0f ||
                Time.unscaledTime < turningUntil)
            {
                nextBlinkAt = Time.unscaledTime + 0.25f;
                return;
            }

            stateMachine.PlayBlink();
            ScheduleNextBlink();
        }

        private void ScheduleNextBlink()
        {
            float minimum = Mathf.Max(0.5f, minimumBlinkDelay);
            float maximum = Mathf.Max(minimum, maximumBlinkDelay);
            nextBlinkAt = Time.unscaledTime + Random.Range(minimum, maximum);
        }

        private void TryResolveUpgradeManager(bool immediate)
        {
            if (upgradeManager != null)
            {
                BindUpgradeManager(upgradeManager);
                return;
            }

            if (!immediate && Time.unscaledTime < nextUpgradeManagerLookupAt)
            {
                return;
            }

            nextUpgradeManagerLookupAt = Time.unscaledTime + 1f;
            BindUpgradeManager(
                Object.FindFirstObjectByType<UpgradeManager>());
        }

        private void BindUpgradeManager(UpgradeManager target)
        {
            if (upgradeManager == target && upgradeManagerSubscribed)
            {
                return;
            }

            UnsubscribeUpgradeManager();
            upgradeManager = target;
            if (upgradeManager != null && isActiveAndEnabled)
            {
                upgradeManager.UpgradesChanged += OnUpgradePurchased;
                upgradeManagerSubscribed = true;
            }
        }

        private void UnsubscribeUpgradeManager()
        {
            if (upgradeManager != null && upgradeManagerSubscribed)
            {
                upgradeManager.UpgradesChanged -= OnUpgradePurchased;
            }

            upgradeManagerSubscribed = false;
        }

        private void OnUpgradePurchased()
        {
            TriggerUpgradeReaction();
        }

        private void TriggerUpgradeReaction()
        {
            // A purchase and a body-art stage change can arrive in the same
            // frame. They represent one visible celebration, not two restarts.
            if (stateMachine == null ||
                Time.unscaledTime - lastUpgradeReactionAt < 0.25f)
            {
                return;
            }

            lastUpgradeReactionAt = Time.unscaledTime;
            stateMachine.PlayUpgradeReaction();
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
