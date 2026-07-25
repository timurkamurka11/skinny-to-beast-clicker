using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CharacterViewController : MonoBehaviour
    {
        private RectTransform visualRoot;
        private CharacterFaceController faceController;
        private CharacterFacing facing = CharacterFacing.Front;
        private float baseScale = 0.78f;
        private bool configured;

        public CharacterFacing Facing => facing;
        public float BaseScale => baseScale;

        public void Configure(
            RectTransform targetVisualRoot,
            CharacterFaceController face)
        {
            visualRoot = targetVisualRoot;
            faceController = face;
            configured = visualRoot != null;
            ApplyView();
        }

        public void SetBasePresentationScale(float scale)
        {
            baseScale = Mathf.Clamp(scale, 0.25f, 1.25f);
            ApplyView();
        }

        public void SetFacing(CharacterFacing nextFacing)
        {
            facing = nextFacing;
            ApplyView();
        }

        private void ApplyView()
        {
            if (!configured)
            {
                return;
            }

            float horizontal = baseScale;
            if (facing == CharacterFacing.SideLeft ||
                facing == CharacterFacing.SideRight)
            {
                horizontal *= 0.72f;
                if (facing == CharacterFacing.SideLeft)
                {
                    horizontal *= -1f;
                }
            }

            visualRoot.localScale =
                new Vector3(horizontal, baseScale, 1f);
            faceController?.SetFacing(facing);
        }
    }
}
