using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    internal sealed class GameplayAnimationController : MonoBehaviour
    {
        private const string AnimatorResourcePath =
            "UI/Gameplay/Living/Animations/LivingCharacter";

        private static readonly int TapAHash = Animator.StringToHash("TapA");
        private static readonly int TapBHash = Animator.StringToHash("TapB");
        private static readonly int RareLookHash = Animator.StringToHash("RareLook");
        private static readonly int RareScratchHash = Animator.StringToHash("RareScratch");
        private static readonly int UpgradeHash = Animator.StringToHash("Upgrade");
        private static readonly int StageChangeHash = Animator.StringToHash("StageChange");

        private RectTransform characterRoot;
        private RectTransform animatorLayer;
        private RectTransform bellyLayer;
        private Image leftEyelid;
        private Image rightEyelid;
        private RandomIdleScheduler idleScheduler;
        private Animator animator;

        private Vector2 basePosition;
        private Vector3 baseScale;
        private float tapAge = 1f;
        private float tapStrength;
        private float bellyImpulse;
        private int tapVariant;
        private LivingIdleAction activeRareIdle;
        private float rareIdleAge = 10f;
        private Coroutine blinkRoutine;
        private bool configured;

        public void Configure(
            RectTransform root,
            RectTransform animationLayer,
            RectTransform belly,
            Image eyelidLeft,
            Image eyelidRight,
            RandomIdleScheduler scheduler)
        {
            characterRoot = root;
            animatorLayer = animationLayer;
            bellyLayer = belly;
            leftEyelid = eyelidLeft;
            rightEyelid = eyelidRight;
            idleScheduler = scheduler;

            basePosition = characterRoot.anchoredPosition;
            baseScale = characterRoot.localScale;

            animator = animatorLayer.GetComponent<Animator>();
            if (animator == null)
            {
                animator = animatorLayer.gameObject.AddComponent<Animator>();
            }

            RuntimeAnimatorController controller =
                Resources.Load<RuntimeAnimatorController>(AnimatorResourcePath);
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            SetEyelidsVisible(false);

            if (idleScheduler != null)
            {
                idleScheduler.BlinkRequested += HandleBlinkRequested;
                idleScheduler.RareIdleRequested += HandleRareIdleRequested;
            }

            configured = true;
        }

        public void TriggerTap()
        {
            if (!configured)
            {
                return;
            }

            idleScheduler?.NotifyActivity();
            tapVariant++;
            tapAge = 0f;
            tapStrength = Mathf.Min(1.65f, tapStrength + 0.72f);
            bellyImpulse = Mathf.Min(1.7f, bellyImpulse + 0.82f);

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(tapVariant % 2 == 0 ? TapAHash : TapBHash);
            }
        }

        public void TriggerUpgrade()
        {
            if (!configured)
            {
                return;
            }

            idleScheduler?.NotifyActivity();
            tapAge = 0f;
            tapStrength = Mathf.Max(tapStrength, 1.15f);
            bellyImpulse = Mathf.Max(bellyImpulse, 0.85f);

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(UpgradeHash);
            }
        }

        public void TriggerStageChange()
        {
            if (!configured)
            {
                return;
            }

            idleScheduler?.NotifyActivity();
            tapAge = 0f;
            tapStrength = 1.5f;
            bellyImpulse = 1.35f;

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(StageChangeHash);
            }
        }

        private void HandleBlinkRequested(bool doubleBlink)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (blinkRoutine != null)
            {
                StopCoroutine(blinkRoutine);
            }

            blinkRoutine = StartCoroutine(BlinkRoutine(doubleBlink));
        }

        private void HandleRareIdleRequested(LivingIdleAction action)
        {
            activeRareIdle = action;
            rareIdleAge = 0f;

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            animator.SetTrigger(
                action == LivingIdleAction.LookDown
                    ? RareLookHash
                    : RareScratchHash);
        }

        private IEnumerator BlinkRoutine(bool doubleBlink)
        {
            yield return AnimateEyelids(0f, 1f, 0.055f);
            yield return new WaitForSecondsRealtime(0.055f);
            yield return AnimateEyelids(1f, 0f, 0.065f);

            if (doubleBlink)
            {
                yield return new WaitForSecondsRealtime(0.095f);
                yield return AnimateEyelids(0f, 1f, 0.045f);
                yield return new WaitForSecondsRealtime(0.045f);
                yield return AnimateEyelids(1f, 0f, 0.055f);
            }

            SetEyelidsVisible(false);
            blinkRoutine = null;
        }

        private IEnumerator AnimateEyelids(float from, float to, float duration)
        {
            float elapsed = 0f;
            SetEyelidsVisible(true);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float amount = Mathf.SmoothStep(from, to, Mathf.Clamp01(elapsed / duration));
                SetEyelidAmount(amount);
                yield return null;
            }

            SetEyelidAmount(to);
        }

        private void SetEyelidAmount(float amount)
        {
            float clamped = Mathf.Clamp01(amount);
            if (leftEyelid != null)
            {
                leftEyelid.color = LivingGameplayVisualFactory.WithAlpha(
                    leftEyelid.color,
                    clamped);
                leftEyelid.rectTransform.localScale =
                    new Vector3(1f, Mathf.Lerp(0.2f, 1f, clamped), 1f);
            }

            if (rightEyelid != null)
            {
                rightEyelid.color = LivingGameplayVisualFactory.WithAlpha(
                    rightEyelid.color,
                    clamped);
                rightEyelid.rectTransform.localScale =
                    new Vector3(1f, Mathf.Lerp(0.2f, 1f, clamped), 1f);
            }
        }

        private void SetEyelidsVisible(bool visible)
        {
            float alpha = visible ? 1f : 0f;
            if (leftEyelid != null)
            {
                leftEyelid.color = LivingGameplayVisualFactory.WithAlpha(
                    leftEyelid.color,
                    alpha);
            }

            if (rightEyelid != null)
            {
                rightEyelid.color = LivingGameplayVisualFactory.WithAlpha(
                    rightEyelid.color,
                    alpha);
            }
        }

        private void Update()
        {
            if (!configured || characterRoot == null)
            {
                return;
            }

            float delta = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;
            float breath = Mathf.Sin(now * Mathf.PI * 2f / 2.4f);
            float sway = Mathf.Sin(now * 0.76f);

            tapAge += delta;
            rareIdleAge += delta;
            tapStrength = Mathf.MoveTowards(tapStrength, 0f, delta * 4.8f);
            bellyImpulse = Mathf.MoveTowards(bellyImpulse, 0f, delta * 3.9f);

            float tapT = Mathf.Clamp01(tapAge / 0.34f);
            float tapWave = tapAge < 0.34f
                ? Mathf.Sin(tapT * Mathf.PI) * tapStrength
                : 0f;
            float tapShake = tapAge < 0.34f
                ? Mathf.Sin(tapT * Mathf.PI * 4f) * tapStrength
                : 0f;

            float rareOffsetY = 0f;
            float rareRotation = 0f;
            float rareScaleX = 0f;
            if (activeRareIdle == LivingIdleAction.LookDown && rareIdleAge < 1.15f)
            {
                float rareT = rareIdleAge / 1.15f;
                float wave = Mathf.Sin(rareT * Mathf.PI);
                rareOffsetY = -7f * wave;
                rareRotation = 0.65f * wave;
            }
            else if (activeRareIdle == LivingIdleAction.Scratch && rareIdleAge < 1.4f)
            {
                float rareT = rareIdleAge / 1.4f;
                float wave = Mathf.Sin(rareT * Mathf.PI);
                rareScaleX = Mathf.Sin(rareT * Mathf.PI * 5f) * 0.007f * wave;
                rareRotation = Mathf.Sin(rareT * Mathf.PI * 3f) * 0.42f * wave;
            }

            Vector2 position = basePosition;
            position.x += sway * 2.2f + tapShake * (tapVariant % 2 == 0 ? 6f : -6f);
            position.y += breath * 4.2f + tapWave * 18f + rareOffsetY;
            characterRoot.anchoredPosition = position;

            float scaleX = baseScale.x * (1f - breath * 0.0045f + tapWave * 0.022f + rareScaleX);
            float scaleY = baseScale.y * (1f + breath * 0.011f - tapWave * 0.026f);
            characterRoot.localScale = new Vector3(scaleX, scaleY, baseScale.z);
            characterRoot.localRotation = Quaternion.Euler(
                0f,
                0f,
                sway * 0.22f + tapShake * 0.8f + rareRotation);

            if (bellyLayer != null)
            {
                float jiggle = Mathf.Sin(now * 48f) * bellyImpulse;
                bellyLayer.localScale = new Vector3(
                    1f + breath * 0.008f + jiggle * 0.012f,
                    1f - breath * 0.004f - jiggle * 0.008f,
                    1f);
                bellyLayer.anchoredPosition = new Vector2(
                    jiggle * 1.8f,
                    -Mathf.Abs(jiggle) * 1.4f);
            }
        }

        private void OnDestroy()
        {
            if (idleScheduler != null)
            {
                idleScheduler.BlinkRequested -= HandleBlinkRequested;
                idleScheduler.RareIdleRequested -= HandleRareIdleRequested;
            }
        }
    }
}
