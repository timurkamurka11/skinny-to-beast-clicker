using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    public enum RoomAnchorKind
    {
        Center,
        Training,
        Sofa,
        Window,
        Mirror
    }

    [DisallowMultipleComponent]
    public sealed class RoomAnchor : MonoBehaviour
    {
        [SerializeField] private RoomAnchorKind kind;
        [SerializeField] private float characterScale = 1f;
        [SerializeField] private CharacterFacing restingFacing = CharacterFacing.Front;
        [SerializeField] private float minimumStay = 2f;
        [SerializeField] private float maximumStay = 4.5f;

        public RoomAnchorKind Kind => kind;
        public RectTransform RectTransform => transform as RectTransform;
        public Vector2 Position => RectTransform != null
            ? RectTransform.anchoredPosition
            : (Vector2)transform.localPosition;
        public float CharacterScale => characterScale;
        public CharacterFacing RestingFacing => restingFacing;
        public float MinimumStay => minimumStay;
        public float MaximumStay => Mathf.Max(minimumStay, maximumStay);

        public void Configure(
            RoomAnchorKind anchorKind,
            Vector2 position,
            float scale,
            CharacterFacing facing,
            float stayMin = 2f,
            float stayMax = 4.5f)
        {
            kind = anchorKind;
            characterScale = Mathf.Clamp(scale, 0.45f, 1.15f);
            restingFacing = facing;
            minimumStay = Mathf.Max(0.5f, stayMin);
            maximumStay = Mathf.Max(minimumStay, stayMax);

            if (RectTransform != null)
            {
                RectTransform.anchoredPosition = position;
                RectTransform.sizeDelta = Vector2.zero;
            }
            else
            {
                transform.localPosition = position;
            }
        }
    }
}
