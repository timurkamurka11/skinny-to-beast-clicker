using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Adds delayed, low-amplitude motion to dedicated soft bones after Animator.
    /// Previous offsets are removed every frame, so authored animation remains intact.
    /// </summary>
    [DefaultExecutionOrder(1050)]
    [DisallowMultipleComponent]
    public sealed class Patch4SecondaryMotionController : MonoBehaviour
    {
        [Serializable]
        private sealed class SoftBoneChannel
        {
            public string name;
            public Transform target;
            public Vector3 positionAmplitude;
            public Vector3 rotationAmplitude;
            [Min(0.05f)] public float frequencyMultiplier = 1f;
            [Range(-1f, 1f)] public float phaseOffset;
            [Range(0f, 1f)] public float response = 0.35f;

            [NonSerialized] public Vector3 previousPositionOffset;
            [NonSerialized] public Quaternion previousRotationOffset = Quaternion.identity;
            [NonSerialized] public float smoothedSignal;
        }

        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField, Min(0.05f)] private float breathingFrequency = 0.32f;
        [SerializeField, Range(0f, 2f)] private float globalAmplitude = 1f;
        [SerializeField] private List<SoftBoneChannel> channels = new();

        private float phase;
#if UNITY_EDITOR
        private bool editorReviewActive;
#endif

        private void Reset()
        {
            rigController = GetComponent<Patch4CharacterRigController>();
        }

        private void OnDisable()
        {
            RemovePreviousOffsets();
        }

        private void LateUpdate()
        {
            RemovePreviousOffsets();

            bool canAnimate =
                rigController != null && rigController.Patch4Enabled;
#if UNITY_EDITOR
            canAnimate |= editorReviewActive;
#endif
            if (!canAnimate)
            {
                return;
            }

            phase += Time.deltaTime * breathingFrequency * Mathf.PI * 2f;

            foreach (SoftBoneChannel channel in channels)
            {
                if (channel == null || channel.target == null)
                {
                    continue;
                }

                float frequency = Mathf.Max(0.05f, channel.frequencyMultiplier);
                float rawSignal = Mathf.Sin(
                    phase * frequency +
                    channel.phaseOffset * Mathf.PI);
                float blend = 1f - Mathf.Exp(
                    -Mathf.Lerp(4f, 24f, channel.response) * Time.deltaTime);
                channel.smoothedSignal = Mathf.Lerp(
                    channel.smoothedSignal,
                    rawSignal,
                    blend);

                float signal = channel.smoothedSignal * globalAmplitude;
                channel.previousPositionOffset =
                    channel.positionAmplitude * signal;
                channel.previousRotationOffset = Quaternion.Euler(
                    channel.rotationAmplitude * signal);

                channel.target.localPosition += channel.previousPositionOffset;
                channel.target.localRotation *= channel.previousRotationOffset;
            }
        }

        public void SetAmplitude(float amplitude)
        {
            globalAmplitude = Mathf.Max(0f, amplitude);
        }

#if UNITY_EDITOR
        public void SetEditorReviewActive(bool active)
        {
            editorReviewActive = active;
            if (!active)
            {
                RemovePreviousOffsets();
            }
        }
#endif

        private void RemovePreviousOffsets()
        {
            foreach (SoftBoneChannel channel in channels)
            {
                if (channel == null || channel.target == null)
                {
                    continue;
                }

                channel.target.localPosition -= channel.previousPositionOffset;
                channel.target.localRotation *=
                    Quaternion.Inverse(channel.previousRotationOffset);
                channel.previousPositionOffset = Vector3.zero;
                channel.previousRotationOffset = Quaternion.identity;
            }
        }
    }
}
