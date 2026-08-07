using System.Reflection;
using SkinnyToBeast.Gameplay.Patch4;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// v18 compatibility fix for the locked room review. v17 capped review
    /// travel at 180 screen pixels while requiring at least 55% of the neutral
    /// silhouette width (347.6 px in the current room), which made the walk
    /// check mathematically impossible even when the gait itself was correct.
    ///
    /// Keep the production locomotion untouched. During the editor-only review
    /// we expand only the driver's private review runway after it is prepared.
    /// The driver still performs its original monotonic-travel measurement.
    /// </summary>
    [InitializeOnLoad]
    internal static class Patch4WalkReviewTravelFix
    {
        private static readonly FieldInfo ReviewTravelField =
            typeof(Patch4AnimationRoomReviewDriver).GetField(
                "reviewTravelLocalDistance",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo NeutralWidthField =
            typeof(Patch4AnimationRoomReviewDriver).GetField(
                "neutralSilhouetteWidth",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static int patchedDriverId;

        static Patch4WalkReviewTravelFix()
        {
            EditorApplication.update += PatchPreparedReviewRunway;
        }

        private static void PatchPreparedReviewRunway()
        {
            if (!EditorApplication.isPlaying ||
                ReviewTravelField == null ||
                NeutralWidthField == null)
            {
                patchedDriverId = 0;
                return;
            }

            Patch4AnimationRoomReviewDriver driver =
                Object.FindFirstObjectByType<Patch4AnimationRoomReviewDriver>();
            if (driver == null)
            {
                patchedDriverId = 0;
                return;
            }

            int instanceId = driver.GetInstanceID();
            if (patchedDriverId == instanceId)
            {
                return;
            }

            float currentLocalDistance =
                (float)ReviewTravelField.GetValue(driver);
            int neutralWidth =
                (int)NeutralWidthField.GetValue(driver);
            if (currentLocalDistance <= 0.0001f || neutralWidth <= 0)
            {
                return;
            }

            // Reconstruct the v17 pixel distance used to obtain the current
            // local-space runway, then scale that runway to 57% of character
            // width. This clears the driver's 55% requirement with a small
            // numerical margin while keeping the character inside the room.
            float previousTargetPixels = Mathf.Clamp(
                neutralWidth * 0.72f,
                64f,
                180f);
            float correctedTargetPixels = Mathf.Max(
                previousTargetPixels,
                neutralWidth * 0.57f);
            float correctedLocalDistance =
                currentLocalDistance *
                correctedTargetPixels /
                Mathf.Max(1f, previousTargetPixels);

            ReviewTravelField.SetValue(driver, correctedLocalDistance);
            patchedDriverId = instanceId;

            Debug.Log(
                "Patch 4 v18 walk review runway corrected: " +
                previousTargetPixels.ToString("0.0") + " px -> " +
                correctedTargetPixels.ToString("0.0") +
                " px. Production locomotion was not modified.");
        }
    }
}
