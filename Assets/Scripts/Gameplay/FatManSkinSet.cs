using System;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// One complete artistic stage for the starter fat-man character. Every
    /// stage is applied atomically to the same bone hierarchy.
    /// </summary>
    [Serializable]
    public sealed class FatManSkinSet
    {
        [SerializeField] private string id;
        [SerializeField] private int stage;
        [SerializeField] private CharacterAppearance appearance;
        [SerializeField] private CharacterFaceStyle faceStyle;

        public string Id => id;
        public int Stage => stage;
        public CharacterAppearance Appearance => appearance;
        public CharacterFaceStyle FaceStyle => faceStyle;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(id) &&
            stage >= 0 &&
            appearance.IsValid &&
            appearance.bellyWidth >= 1.12f &&
            appearance.softness > 0.05f;

        public static FatManSkinSet Create(int artIndex)
        {
            int safeIndex = Mathf.Clamp(artIndex, 0, 3);
            return new FatManSkinSet
            {
                id = $"fat_man_stage_{safeIndex + 1:00}",
                stage = safeIndex,
                appearance = CharacterAppearance.Create(safeIndex),
                faceStyle = CharacterFaceStyle.Create(safeIndex)
            };
        }
    }
}
