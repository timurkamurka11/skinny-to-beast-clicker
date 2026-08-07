using System.Reflection;
using SkinnyToBeast.Gameplay.Patch4;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// v18 proved that the room review's old 180 px cap was too short, but the
    /// first correction still produced only ~292 px on the current Canvas while
    /// the silhouette check required ~348 px. This editor-only pass adds a safe
    /// margin to the diagnostic runway. It never changes production locomotion.
    /// </summary>
    [InitializeOnLoad]
    internal static class Patch4V19WalkReviewTravelBoost
    {
        private static readonly FieldInfo ReviewTravelField =
            typeof(Patch4AnimationRoomReviewDriver).GetField(
                "reviewTravelLocalDistance",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static int patchedDriverId;

        static Patch4V19WalkReviewTravelBoost()
        {
            EditorApplication.update += ApplyBoost;
        }

        private static void ApplyBoost()
        {
            if (!EditorApplication.isPlaying || ReviewTravelField == null)
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

            int id = driver.GetInstanceID();
            if (patchedDriverId == id)
            {
                return;
            }

            float current = (float)ReviewTravelField.GetValue(driver);
            if (current <= 0.0001f)
            {
                return;
            }

            // Whether this runs just before or just after the v18 correction,
            // multiplying the prepared runway is safe and deterministic. The
            // extra 35% covers the measured Canvas conversion loss with margin.
            ReviewTravelField.SetValue(driver, current * 1.35f);
            patchedDriverId = id;
            Debug.Log(
                "Patch 4 v19 walk review runway boosted by 35%. " +
                "Production locomotion was not modified.");
        }
    }
}
