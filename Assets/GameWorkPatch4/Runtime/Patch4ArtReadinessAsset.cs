using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Explicit human-review gate for Patch 4 painted art.
    /// Automated mask cutting can produce draft layers, but it can never approve
    /// the character for runtime activation by itself.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Patch4ArtReadiness",
        menuName = "Skinny To Beast/Patch 4/Art Readiness")]
    public sealed class Patch4ArtReadinessAsset : ScriptableObject
    {
        [SerializeField] private bool productionArtApproved;
        [SerializeField] private string approvedSourceSha256 = string.Empty;
        [SerializeField] private string approvedBy = string.Empty;
        [SerializeField, TextArea(3, 10)] private string reviewNotes =
            "Draft mask cuts are not production art. Redraw hidden joints, " +
            "verify all mouth and eye poses, then approve manually.";

        public bool ProductionArtApproved => productionArtApproved;
        public string ApprovedSourceSha256 => approvedSourceSha256;
        public string ApprovedBy => approvedBy;
        public string ReviewNotes => reviewNotes;

        public bool IsApprovedFor(string expectedSourceSha256)
        {
            return productionArtApproved &&
                   !string.IsNullOrWhiteSpace(approvedSourceSha256) &&
                   string.Equals(
                       approvedSourceSha256,
                       expectedSourceSha256,
                       System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
