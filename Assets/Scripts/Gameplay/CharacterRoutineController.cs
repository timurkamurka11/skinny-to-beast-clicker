using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CharacterRoutineController : MonoBehaviour
    {
        private readonly List<RoomAnchor> anchors = new();

        private RectTransform characterRoot;
        private CharacterRigController rigController;
        private CharacterFaceController faceController;
        private GameplayAudioController audioController;
        private RoomAnchor currentAnchor;
        private RoomAnchor trainingAnchor;
        private RoomAnchor walkingTarget;
        private RoomAnchor interruptedAnchor;
        private CharacterRoutineAction interruptedAction;
        private CharacterRoutineAction currentAction;
        private Coroutine routineLoop;
        private Coroutine tapRoutine;
        private int queuedTapReactions;
        private float lastTapAt = -10f;
        private bool interruptedWhileWalking;
        private bool resumingInterruptedBehavior;
        private bool isWalking;
        private bool configured;
        private int idleActionCursor;
        private int footstepSequence;

        private static readonly CharacterRoutineAction[] IdleActionCycle =
        {
            CharacterRoutineAction.ShiftWeight,
            CharacterRoutineAction.LookAround,
            CharacterRoutineAction.Scratch,
            CharacterRoutineAction.Stretch,
            CharacterRoutineAction.Yawn,
            CharacterRoutineAction.AdjustClothes,
            CharacterRoutineAction.WarmShoulders,
            CharacterRoutineAction.Flex
        };

        public RoomAnchor CurrentAnchor => currentAnchor;
        public bool IsWalking => isWalking;

        public void Configure(
            RectTransform root,
            CharacterRigController rig,
            CharacterFaceController face,
            IEnumerable<RoomAnchor> roomAnchors)
        {
            characterRoot = root;
            rigController = rig;
            faceController = face;
            audioController =
                GetComponentInParent<GameplayAudioController>();
            anchors.Clear();

            if (roomAnchors != null)
            {
                foreach (RoomAnchor anchor in roomAnchors)
                {
                    if (anchor == null)
                    {
                        continue;
                    }

                    anchors.Add(anchor);
                    if (anchor.Kind == RoomAnchorKind.Training)
                    {
                        trainingAnchor = anchor;
                    }
                }
            }

            if (trainingAnchor == null && anchors.Count > 0)
            {
                trainingAnchor = anchors[0];
            }

            currentAnchor = trainingAnchor;
            if (currentAnchor != null)
            {
                characterRoot.anchoredPosition = currentAnchor.Position;
                characterRoot.localScale = Vector3.one * currentAnchor.CharacterScale;
                rigController.StopLocomotion(currentAnchor.RestingFacing);
            }

            configured = characterRoot != null &&
                         rigController != null &&
                         anchors.Count > 0;
            if (configured && isActiveAndEnabled)
            {
                StartRoutineLoop(Random.Range(2.5f, 4.5f));
            }
        }

        public void ReactToTap()
        {
            if (!configured)
            {
                rigController?.TriggerTap();
                return;
            }

            lastTapAt = Time.unscaledTime;
            queuedTapReactions = Mathf.Min(6, queuedTapReactions + 1);
            if (tapRoutine != null && resumingInterruptedBehavior)
            {
                // A new burst is allowed to interrupt the walk/action that was
                // restoring the previous routine. Capture that restoration as
                // the new resume target and reuse the single tap coroutine.
                StopCoroutine(tapRoutine);
                tapRoutine = null;
                resumingInterruptedBehavior = false;
            }

            if (tapRoutine == null)
            {
                interruptedWhileWalking = isWalking;
                interruptedAnchor =
                    isWalking && walkingTarget != null
                        ? walkingTarget
                        : currentAnchor;
                interruptedAction = currentAction;
            }

            StopRoutineLoop();
            walkingTarget = null;
            rigController.CancelAction();
            currentAction = CharacterRoutineAction.None;

            float distance = trainingAnchor != null
                ? Vector2.Distance(characterRoot.anchoredPosition, trainingAnchor.Position)
                : 0f;
            if (distance <= 55f)
            {
                isWalking = false;
                rigController.StopLocomotion(CharacterFacing.Front);
                rigController.TriggerTap();
                queuedTapReactions = Mathf.Max(0, queuedTapReactions - 1);
                if (tapRoutine == null)
                {
                    tapRoutine = StartCoroutine(ResumeAfterTap());
                }

                return;
            }

            if (tapRoutine == null)
            {
                tapRoutine = StartCoroutine(ReturnToTrainingForTap());
            }
        }

        public void NotifyActivity()
        {
            if (!configured)
            {
                return;
            }

            StopRoutineLoop();
            walkingTarget = null;
            isWalking = false;
            rigController.StopLocomotion(rigController.Facing);
            StartRoutineLoop(Random.Range(3f, 5f));
        }

        private void OnEnable()
        {
            if (configured && routineLoop == null && tapRoutine == null)
            {
                StartRoutineLoop(Random.Range(2.5f, 4.5f));
            }
        }

        private void OnDisable()
        {
            StopRoutineLoop();
            if (tapRoutine != null)
            {
                StopCoroutine(tapRoutine);
                tapRoutine = null;
            }

            queuedTapReactions = 0;
            walkingTarget = null;
            interruptedWhileWalking = false;
            resumingInterruptedBehavior = false;
            isWalking = false;
            currentAction = CharacterRoutineAction.None;
            rigController?.StopLocomotion(CharacterFacing.Front);
        }

        private void StartRoutineLoop(float initialDelay)
        {
            if (!configured || routineLoop != null || tapRoutine != null)
            {
                return;
            }

            routineLoop = StartCoroutine(RoutineLoop(Mathf.Max(0f, initialDelay)));
        }

        private void StopRoutineLoop()
        {
            if (routineLoop == null)
            {
                return;
            }

            StopCoroutine(routineLoop);
            routineLoop = null;
        }

        private IEnumerator RoutineLoop(float initialDelay)
        {
            if (initialDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(initialDelay);
            }

            while (configured)
            {
                RoomAnchor next = SelectNextAnchor();
                if (next != null)
                {
                    float distance = Vector2.Distance(
                        characterRoot.anchoredPosition,
                        next.Position);
                    float travelTime = Mathf.Clamp(distance / 420f, 0.65f, 1.8f);
                    yield return WalkTo(next, travelTime);
                    yield return PlayAnchorAction(next);

                    if (next.Kind == RoomAnchorKind.Sofa)
                    {
                        yield return new WaitForSecondsRealtime(1.2f);
                        continue;
                    }
                }

                yield return new WaitForSecondsRealtime(Random.Range(4f, 7f));
            }
        }

        private IEnumerator ReturnToTrainingForTap()
        {
            if (trainingAnchor != null)
            {
                float distance = Vector2.Distance(
                    characterRoot.anchoredPosition,
                    trainingAnchor.Position);
                float travelTime = Mathf.Clamp(distance / 760f, 0.28f, 0.68f);
                yield return WalkTo(trainingAnchor, travelTime);
            }

            rigController.StopLocomotion(CharacterFacing.Front);
            int reactions = Mathf.Max(1, queuedTapReactions);
            queuedTapReactions = 0;
            for (int i = 0; i < reactions; i++)
            {
                rigController.TriggerTap();
                if (i + 1 < reactions)
                {
                    yield return new WaitForSecondsRealtime(0.11f);
                }
            }

            yield return WaitForTapBurstToEnd();
            yield return ResumeInterruptedBehavior();
        }

        private IEnumerator ResumeAfterTap()
        {
            yield return WaitForTapBurstToEnd();
            yield return ResumeInterruptedBehavior();
        }

        private IEnumerator WaitForTapBurstToEnd()
        {
            while (Time.unscaledTime - lastTapAt < 0.72f)
            {
                yield return null;
            }
        }

        private IEnumerator ResumeInterruptedBehavior()
        {
            resumingInterruptedBehavior = true;
            RoomAnchor resumeAnchor = interruptedAnchor;
            CharacterRoutineAction resumeAction = interruptedAction;
            bool resumeWalk = interruptedWhileWalking;
            interruptedAnchor = null;
            interruptedAction = CharacterRoutineAction.None;
            interruptedWhileWalking = false;

            if (resumeAnchor != null &&
                resumeAnchor != trainingAnchor &&
                Vector2.Distance(
                    characterRoot.anchoredPosition,
                    resumeAnchor.Position) > 20f)
            {
                float distance = Vector2.Distance(
                    characterRoot.anchoredPosition,
                    resumeAnchor.Position);
                float travelTime = Mathf.Clamp(distance / 520f, 0.55f, 1.55f);
                yield return WalkTo(resumeAnchor, travelTime);
            }

            bool resumeAnchorAction =
                resumeWalk ||
                resumeAction == CharacterRoutineAction.SitDown ||
                resumeAction == CharacterRoutineAction.SitLoop ||
                resumeAction == CharacterRoutineAction.StandUp ||
                resumeAction == CharacterRoutineAction.Sit;
            if (resumeAnchorAction && resumeAnchor != null)
            {
                yield return PlayAnchorAction(resumeAnchor);
            }
            else if (resumeAction != CharacterRoutineAction.None)
            {
                currentAction = resumeAction;
                rigController.PlayAction(resumeAction, 1.15f);
                yield return new WaitForSecondsRealtime(1.15f);
                currentAction = CharacterRoutineAction.None;
            }

            resumingInterruptedBehavior = false;
            tapRoutine = null;
            StartRoutineLoop(Random.Range(3f, 5f));
        }

        private IEnumerator WalkTo(RoomAnchor destination, float duration)
        {
            if (destination == null)
            {
                yield break;
            }

            Vector2 startPosition = characterRoot.anchoredPosition;
            Vector2 endPosition = destination.Position;
            Vector3 startScale = characterRoot.localScale;
            Vector3 endScale = Vector3.one * destination.CharacterScale;
            Vector2 direction = endPosition - startPosition;
            float distance = Mathf.Max(1f, direction.magnitude);
            float normalizedStepSpeed = Mathf.Clamp(
                (distance / Mathf.Max(0.01f, duration)) / 420f,
                0.65f,
                1.75f);
            float elapsed = 0f;
            float nextFootstepAt = 0.05f;
            walkingTarget = destination;
            isWalking = true;
            rigController.SetLocomotion(
                direction,
                normalizedStepSpeed);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                characterRoot.anchoredPosition =
                    Vector2.Lerp(startPosition, endPosition, t);
                characterRoot.localScale = Vector3.Lerp(
                    startScale,
                    endScale,
                    Mathf.SmoothStep(0f, 1f, t));
                rigController.SetLocomotion(
                    direction,
                    normalizedStepSpeed);
                if (elapsed >= nextFootstepAt)
                {
                    if (audioController == null)
                    {
                        audioController =
                            GetComponentInParent<GameplayAudioController>();
                    }

                    audioController?.PlayFootstep(
                        footstepSequence++);
                    nextFootstepAt += 0.27f;
                }

                yield return null;
            }

            characterRoot.anchoredPosition = endPosition;
            characterRoot.localScale = endScale;
            currentAnchor = destination;
            walkingTarget = null;
            isWalking = false;
            rigController.StopLocomotion(destination.RestingFacing);
        }

        private IEnumerator PlayAnchorAction(RoomAnchor anchor)
        {
            CharacterRoutineAction action = ResolveAction(anchor.Kind);
            float duration = Random.Range(anchor.MinimumStay, anchor.MaximumStay);
            if (anchor.Kind == RoomAnchorKind.Sofa)
            {
                currentAction = CharacterRoutineAction.SitDown;
                rigController.PlayAction(CharacterRoutineAction.SitDown, 0.72f);
                yield return new WaitForSecondsRealtime(0.72f);

                currentAction = CharacterRoutineAction.SitLoop;
                float sitDuration = Random.Range(5f, 7f);
                rigController.PlayAction(
                    CharacterRoutineAction.SitLoop,
                    sitDuration);
                yield return new WaitForSecondsRealtime(sitDuration);

                currentAction = CharacterRoutineAction.StandUp;
                rigController.PlayAction(CharacterRoutineAction.StandUp, 0.68f);
                yield return new WaitForSecondsRealtime(0.68f);
                currentAction = CharacterRoutineAction.None;
                yield break;
            }

            currentAction = action;
            rigController.PlayAction(action, duration);

            if (action == CharacterRoutineAction.LookAround)
            {
                faceController?.LookAt(new Vector2(-0.8f, 0.2f), duration * 0.45f);
            }
            else if (action == CharacterRoutineAction.Flex)
            {
                faceController?.SetExpression(CharacterExpression.Happy, duration);
            }
            else if (action == CharacterRoutineAction.Yawn)
            {
                faceController?.SetExpression(CharacterExpression.Yawn, duration);
            }
            else if (action == CharacterRoutineAction.AdjustClothes)
            {
                faceController?.LookAt(new Vector2(0f, -0.7f), duration * 0.65f);
            }
            else if (action == CharacterRoutineAction.WarmShoulders)
            {
                faceController?.SetExpression(
                    CharacterExpression.Focused,
                    duration);
            }

            yield return new WaitForSecondsRealtime(duration);
            currentAction = CharacterRoutineAction.None;
        }

        private RoomAnchor SelectNextAnchor()
        {
            if (anchors.Count == 0)
            {
                return null;
            }

            if (anchors.Count == 1)
            {
                return anchors[0];
            }

            for (int attempt = 0; attempt < 8; attempt++)
            {
                RoomAnchor candidate = anchors[Random.Range(0, anchors.Count)];
                if (candidate != currentAnchor &&
                    (rigController == null ||
                     rigController.ObservedIdleActionCount >= 3 ||
                     candidate.Kind != RoomAnchorKind.Sofa))
                {
                    return candidate;
                }
            }

            int currentIndex =
                Mathf.Max(0, anchors.IndexOf(currentAnchor));
            for (int offset = 1; offset <= anchors.Count; offset++)
            {
                RoomAnchor candidate =
                    anchors[(currentIndex + offset) % anchors.Count];
                if (candidate == currentAnchor)
                {
                    continue;
                }

                if (rigController != null &&
                    rigController.ObservedIdleActionCount < 3 &&
                    candidate.Kind == RoomAnchorKind.Sofa)
                {
                    continue;
                }

                return candidate;
            }

            return currentAnchor;
        }

        private CharacterRoutineAction ResolveAction(RoomAnchorKind kind)
        {
            if (kind == RoomAnchorKind.Sofa)
            {
                return CharacterRoutineAction.SitDown;
            }

            CharacterRoutineAction action =
                IdleActionCycle[
                    idleActionCursor % IdleActionCycle.Length];
            idleActionCursor++;
            return action;
        }
    }
}
