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
        private RoomAnchor currentAnchor;
        private RoomAnchor trainingAnchor;
        private Coroutine routineLoop;
        private Coroutine tapRoutine;
        private int queuedTapReactions;
        private bool configured;

        public RoomAnchor CurrentAnchor => currentAnchor;

        public void Configure(
            RectTransform root,
            CharacterRigController rig,
            CharacterFaceController face,
            IEnumerable<RoomAnchor> roomAnchors)
        {
            characterRoot = root;
            rigController = rig;
            faceController = face;
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
                StartRoutineLoop(8f);
            }
        }

        public void ReactToTap()
        {
            if (!configured)
            {
                rigController?.TriggerTap();
                return;
            }

            queuedTapReactions = Mathf.Min(2, queuedTapReactions + 1);
            StopRoutineLoop();
            rigController.CancelAction();

            float distance = trainingAnchor != null
                ? Vector2.Distance(characterRoot.anchoredPosition, trainingAnchor.Position)
                : 0f;
            if (distance <= 55f)
            {
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
            StartRoutineLoop(Random.Range(6f, 10f));
        }

        private void OnEnable()
        {
            if (configured && routineLoop == null && tapRoutine == null)
            {
                StartRoutineLoop(8f);
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

                yield return new WaitForSecondsRealtime(Random.Range(8f, 16f));
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
            int reactions = Mathf.Clamp(queuedTapReactions, 1, 2);
            queuedTapReactions = 0;
            for (int i = 0; i < reactions; i++)
            {
                rigController.TriggerTap();
                if (i + 1 < reactions)
                {
                    yield return new WaitForSecondsRealtime(0.13f);
                }
            }

            yield return new WaitForSecondsRealtime(2.2f);
            tapRoutine = null;
            StartRoutineLoop(Random.Range(5f, 9f));
        }

        private IEnumerator ResumeAfterTap()
        {
            yield return new WaitForSecondsRealtime(2.2f);
            tapRoutine = null;
            StartRoutineLoop(Random.Range(5f, 9f));
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
            float elapsed = 0f;
            rigController.SetLocomotion(direction, 1f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector2 position = Vector2.Lerp(startPosition, endPosition, eased);
                position.y += Mathf.Sin(t * Mathf.PI) * 18f;
                characterRoot.anchoredPosition = position;
                characterRoot.localScale = Vector3.Lerp(startScale, endScale, eased);
                rigController.SetLocomotion(direction, 1f);
                yield return null;
            }

            characterRoot.anchoredPosition = endPosition;
            characterRoot.localScale = endScale;
            currentAnchor = destination;
            rigController.StopLocomotion(destination.RestingFacing);
        }

        private IEnumerator PlayAnchorAction(RoomAnchor anchor)
        {
            CharacterRoutineAction action = ResolveAction(anchor.Kind);
            float duration = Random.Range(anchor.MinimumStay, anchor.MaximumStay);
            if (action == CharacterRoutineAction.Sit)
            {
                duration += Random.Range(7f, 11f);
            }
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

            yield return new WaitForSecondsRealtime(duration);
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
                if (candidate != currentAnchor)
                {
                    return candidate;
                }
            }

            int currentIndex = Mathf.Max(0, anchors.IndexOf(currentAnchor));
            return anchors[(currentIndex + 1) % anchors.Count];
        }

        private static CharacterRoutineAction ResolveAction(RoomAnchorKind kind)
        {
            return kind switch
            {
                RoomAnchorKind.Sofa => CharacterRoutineAction.Sit,
                RoomAnchorKind.Window => CharacterRoutineAction.LookAround,
                RoomAnchorKind.Mirror => CharacterRoutineAction.Flex,
                RoomAnchorKind.Center => Random.value < 0.5f
                    ? CharacterRoutineAction.Stretch
                    : CharacterRoutineAction.Yawn,
                _ => Random.value < 0.5f
                    ? CharacterRoutineAction.Scratch
                    : CharacterRoutineAction.ShiftWeight
            };
        }
    }
}
