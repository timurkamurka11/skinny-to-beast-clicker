using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    public enum FatManArtPartKind
    {
        Thigh,
        Calf,
        Foot,
        Pelvis,
        Belly,
        BellyProfile,
        BellyBand,
        ShirtHem,
        Chest,
        Shoulder,
        UpperArm,
        Forearm,
        Hand,
        Neck,
        Head,
        DoubleChin,
        Hair,
        Ear,
        FaceFeature,
        ClothingDetail,
        SkinDetail,
        ShoeDetail
    }

    /// <summary>
    /// Semantic binding between one drawn cutout part and the shared
    /// skeleton. It also owns front/side/back visibility for details such as
    /// the drawstring, side belly profile, and rear pockets.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterMeshGraphic))]
    public sealed class CharacterArtPart : MonoBehaviour
    {
        [SerializeField] private FatManArtPartKind kind;
        [SerializeField] private CharacterSkinSlot slot;
        [SerializeField] private bool corePart;
        [SerializeField] private bool frontVisible = true;
        [SerializeField] private bool sideVisible = true;
        [SerializeField] private bool backVisible = true;

        private CharacterMeshGraphic graphic;
        private CharacterFacing facing = CharacterFacing.Front;

        public FatManArtPartKind Kind => kind;
        public CharacterSkinSlot Slot => slot;
        public bool IsCorePart => corePart;
        public bool IsConfigured => graphic != null;
        public CharacterFacing Facing => facing;
        public bool UsesFatManSilhouette =>
            graphic != null &&
            IsFatManShape(graphic.Shape);

        public void Configure(
            CharacterMeshGraphic targetGraphic,
            FatManArtPartKind targetKind,
            CharacterSkinSlot targetSlot,
            bool isCorePart,
            bool showFront = true,
            bool showSide = true,
            bool showBack = true)
        {
            graphic = targetGraphic;
            kind = targetKind;
            slot = targetSlot;
            corePart = isCorePart;
            frontVisible = showFront;
            sideVisible = showSide;
            backVisible = showBack;
            SetFacing(facing);
        }

        public void SetFacing(CharacterFacing nextFacing)
        {
            facing = nextFacing;
            bool shouldShow = nextFacing switch
            {
                CharacterFacing.Back => backVisible,
                CharacterFacing.SideLeft => sideVisible,
                CharacterFacing.SideRight => sideVisible,
                _ => frontVisible
            };

            if (gameObject.activeSelf != shouldShow)
            {
                gameObject.SetActive(shouldShow);
            }
        }

        private static bool IsFatManShape(CharacterMeshShape shape)
        {
            return shape == CharacterMeshShape.FatThigh ||
                   shape == CharacterMeshShape.FatCalf ||
                   shape == CharacterMeshShape.FatPelvis ||
                   shape == CharacterMeshShape.FatBelly ||
                   shape == CharacterMeshShape.FatChest ||
                   shape == CharacterMeshShape.FatShoulder ||
                   shape == CharacterMeshShape.FatUpperArm ||
                   shape == CharacterMeshShape.FatForearm ||
                   shape == CharacterMeshShape.FatHand ||
                   shape == CharacterMeshShape.FatNeck ||
                   shape == CharacterMeshShape.FatHead ||
                   shape == CharacterMeshShape.DoubleChin ||
                   shape == CharacterMeshShape.MessyHair ||
                   shape == CharacterMeshShape.WornShoe ||
                   shape == CharacterMeshShape.ShirtHem;
        }
    }
}
