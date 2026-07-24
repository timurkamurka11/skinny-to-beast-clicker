using System;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    internal enum LivingIdleAction
    {
        LookDown,
        Scratch
    }

    [DisallowMultipleComponent]
    internal sealed class RandomIdleScheduler : MonoBehaviour
    {
        public event Action<bool> BlinkRequested;
        public event Action<LivingIdleAction> RareIdleRequested;

        private float nextBlinkAt;
        private float nextRareIdleAt;
        private int rareIdleIndex;

        private void OnEnable()
        {
            ScheduleBlink();
            ScheduleRareIdle();
        }

        public void NotifyActivity()
        {
            nextRareIdleAt = Time.unscaledTime + UnityEngine.Random.Range(8f, 16f);
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (now >= nextBlinkAt)
            {
                BlinkRequested?.Invoke(UnityEngine.Random.value < 0.18f);
                ScheduleBlink();
            }

            if (now >= nextRareIdleAt)
            {
                LivingIdleAction action = rareIdleIndex++ % 2 == 0
                    ? LivingIdleAction.LookDown
                    : LivingIdleAction.Scratch;
                RareIdleRequested?.Invoke(action);
                ScheduleRareIdle();
            }
        }

        private void ScheduleBlink()
        {
            nextBlinkAt = Time.unscaledTime + UnityEngine.Random.Range(2.5f, 6f);
        }

        private void ScheduleRareIdle()
        {
            nextRareIdleAt = Time.unscaledTime + UnityEngine.Random.Range(8f, 16f);
        }
    }
}
