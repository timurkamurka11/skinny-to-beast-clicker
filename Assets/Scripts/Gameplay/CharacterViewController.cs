using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CharacterViewController : MonoBehaviour
    {
        private RectTransform visualRoot;
        private CharacterFaceController faceController;
        private CharacterArtPart[] artParts;
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
            artParts = configured
                ? visualRoot.GetComponentsInChildren<
                    CharacterArtPart>(true)
                : System.Array.Empty<CharacterArtPart>();
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
                CharacterLayeredRigController layered =
                    GetComponent<CharacterLayeredRigController>();
                // Patch 3.6 has a real side-profile art set. Do not squeeze it
                // like the old procedural mannequin; only mirror for SideLeft.
                if (layered == null || !layered.UsesNativeSideProfile)
                {
                    horizontal *= 0.82f;
                }
                if (facing == CharacterFacing.SideLeft)
                {
                    horizontal *= -1f;
                }
            }

            visualRoot.localScale =
                new Vector3(horizontal, baseScale, 1f);
            for (int i = 0;
                 artParts != null && i < artParts.Length;
                 i++)
            {
                artParts[i]?.SetFacing(facing);
            }

            faceController?.SetFacing(facing);
        }
    }
}
