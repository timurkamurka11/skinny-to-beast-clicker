using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    [CreateAssetMenu(
        fileName = "FatManSpriteCatalog",
        menuName = "Skinny to Beast/Fat Man Sprite Catalog")]
    public sealed class FatManSpriteSet : ScriptableObject
    {
        [SerializeField] private Texture2D turnaround;
        [SerializeField] private float[] stageScales =
        {
            1f,
            0.985f,
            0.97f,
            0.955f
        };
        [SerializeField] private int frontColumn;
        [SerializeField] private int sideColumn = 1;
        [SerializeField] private int backColumn = 2;

        public Texture2D Turnaround => turnaround;
        public bool IsValid =>
            turnaround != null &&
            stageScales != null &&
            stageScales.Length >= 4;

        public int GetColumn(CharacterFacing facing)
        {
            return facing switch
            {
                CharacterFacing.Back => Mathf.Clamp(backColumn, 0, 2),
                CharacterFacing.SideLeft => Mathf.Clamp(sideColumn, 0, 2),
                CharacterFacing.SideRight => Mathf.Clamp(sideColumn, 0, 2),
                _ => Mathf.Clamp(frontColumn, 0, 2)
            };
        }

        public float GetStageScale(int stageIndex)
        {
            if (stageScales == null || stageScales.Length == 0)
            {
                return 1f;
            }

            int safeIndex = Mathf.Clamp(
                stageIndex,
                0,
                stageScales.Length - 1);
            return Mathf.Clamp(stageScales[safeIndex], 0.8f, 1.2f);
        }
    }
}
