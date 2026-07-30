using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Controls independent Patch 4 eyelids and mouth artwork parented to Head.
    /// The face remains attached while the body changes pose or direction.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Patch4FaceController : MonoBehaviour
    {
        public enum MouthPose
        {
            Closed,
            Open,
            Smile
        }

        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private GameObject eyeWhiteLeft;
        [SerializeField] private GameObject eyeWhiteRight;
        [SerializeField] private GameObject irisLeft;
        [SerializeField] private GameObject irisRight;
        [SerializeField] private Transform lidLeft;
        [SerializeField] private Transform lidRight;
        [SerializeField] private GameObject mouthClosed;
        [SerializeField] private GameObject mouthOpen;
        [SerializeField] private GameObject mouthSmile;

        [Header("Blink")]
        [SerializeField, Min(0.02f)] private float closeDuration = 0.07f;
        [SerializeField, Min(0.02f)] private float holdDuration = 0.04f;
        [SerializeField, Min(0.02f)] private float openDuration = 0.09f;
        [SerializeField] private Vector2 blinkInterval = new(2.2f, 5.5f);
        [SerializeField, Range(0.01f, 0.25f)] private float openScaleY = 0.04f;
        [SerializeField, Range(0.25f, 0.75f)]
        private float eyeSwapThreshold = 0.52f;

        private Vector3 leftClosedScale = Vector3.one;
        private Vector3 rightClosedScale = Vector3.one;
        private float blinkStartedAt = -100f;
        private float nextBlinkAt;
        private bool blinking;
#if UNITY_EDITOR
        private bool editorReviewActive;
#endif

        private float BlinkLength => closeDuration + holdDuration + openDuration;

        private void Reset()
        {
            rigController = GetComponentInParent<Patch4CharacterRigController>();
        }

        private void Awake()
        {
            CacheClosedLidScales();
            RestoreOpenEyes();
            SetMouth(MouthPose.Closed);
            ScheduleBlink();
        }

        private void OnDisable()
        {
            blinking = false;
            RestoreOpenEyes();
        }

        private void Update()
        {
            bool canAnimate =
                rigController != null && rigController.Patch4Enabled;
#if UNITY_EDITOR
            canAnimate |= editorReviewActive;
#endif
            if (!canAnimate)
            {
                RestoreOpenEyes();
                return;
            }

            if (!blinking && Time.time >= nextBlinkAt)
            {
                BeginBlink();
            }

            if (!blinking)
            {
                return;
            }

            float elapsed = Time.time - blinkStartedAt;
            float closure;

            if (elapsed < closeDuration)
            {
                closure = Mathf.Clamp01(elapsed / closeDuration);
            }
            else if (elapsed < closeDuration + holdDuration)
            {
                closure = 1f;
            }
            else
            {
                float openElapsed = elapsed - closeDuration - holdDuration;
                closure =
                    1f -
                    Mathf.Clamp01(openElapsed / openDuration);
            }

            ApplyLidClosure(closure);

            if (elapsed >= BlinkLength)
            {
                blinking = false;
                RestoreOpenEyes();
                ScheduleBlink();
            }
        }

        public void BlinkNow()
        {
            BeginBlink();
        }

#if UNITY_EDITOR
        public void SetEditorReviewActive(bool active)
        {
            editorReviewActive = active;
            if (!active)
            {
                blinking = false;
                RestoreOpenEyes();
            }
            else
            {
                ScheduleBlink();
            }
        }
#endif

        public void SetMouth(MouthPose pose)
        {
            SetActive(mouthClosed, pose == MouthPose.Closed);
            SetActive(mouthOpen, pose == MouthPose.Open);
            SetActive(mouthSmile, pose == MouthPose.Smile);
        }

        public void BindPresentationLayers(
            GameObject leftEyeWhite,
            GameObject rightEyeWhite,
            GameObject leftIris,
            GameObject rightIris,
            Transform leftLid,
            Transform rightLid,
            GameObject closedMouth,
            GameObject openMouth,
            GameObject smileMouth)
        {
            eyeWhiteLeft = leftEyeWhite;
            eyeWhiteRight = rightEyeWhite;
            irisLeft = leftIris;
            irisRight = rightIris;
            lidLeft = leftLid;
            lidRight = rightLid;
            mouthClosed = closedMouth;
            mouthOpen = openMouth;
            mouthSmile = smileMouth;
            CacheClosedLidScales();
            RestoreOpenEyes();
            SetMouth(MouthPose.Closed);
        }

        private void BeginBlink()
        {
            blinking = true;
            blinkStartedAt = Time.time;
            SetLidsActive(true);
            ApplyLidClosure(0f);
        }

        private void ScheduleBlink()
        {
            float min = Mathf.Max(0.1f, Mathf.Min(blinkInterval.x, blinkInterval.y));
            float max = Mathf.Max(min, Mathf.Max(blinkInterval.x, blinkInterval.y));
            nextBlinkAt = Time.time + Random.Range(min, max);
        }

        private void ApplyLidClosure(float closure)
        {
            closure = Mathf.Clamp01(closure);
            SetOpenEyesActive(closure < eyeSwapThreshold);
            if (lidLeft != null)
            {
                Vector3 scale = leftClosedScale;
                scale.y *= Mathf.Lerp(
                    openScaleY,
                    1f,
                    closure);
                lidLeft.localScale = scale;
            }

            if (lidRight != null)
            {
                Vector3 scale = rightClosedScale;
                scale.y *= Mathf.Lerp(
                    openScaleY,
                    1f,
                    closure * 0.98f);
                lidRight.localScale = scale;
            }
        }

        private void RestoreOpenEyes()
        {
            SetOpenEyesActive(true);
            if (lidLeft != null)
            {
                lidLeft.localScale = leftClosedScale;
            }

            if (lidRight != null)
            {
                lidRight.localScale = rightClosedScale;
            }

            SetLidsActive(false);
        }

        private void CacheClosedLidScales()
        {
            if (lidLeft != null)
            {
                leftClosedScale = lidLeft.localScale;
            }

            if (lidRight != null)
            {
                rightClosedScale = lidRight.localScale;
            }
        }

        private void SetLidsActive(bool active)
        {
            SetActive(
                lidLeft != null ? lidLeft.gameObject : null,
                active);
            SetActive(
                lidRight != null ? lidRight.gameObject : null,
                active);
        }

        private void SetOpenEyesActive(bool active)
        {
            SetActive(eyeWhiteLeft, active);
            SetActive(eyeWhiteRight, active);
            SetActive(irisLeft, active);
            SetActive(irisRight, active);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
