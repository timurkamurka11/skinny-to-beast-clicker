using System.Text;
using SkinnyToBeast.Gameplay;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.EditorTools
{
    internal static class GeneratedFatManRigValidator
    {
        [MenuItem(
            "Tools/Skinny to Beast/Validate Generated Fat Man Bone Rig 3.8")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate(out string report);
            if (valid)
            {
                Debug.Log(report);
            }
            else
            {
                Debug.LogError(report);
            }

            EditorUtility.DisplayDialog(
                valid
                    ? "Generated Fat Man Rig 3.8"
                    : "Generated Fat Man Rig 3.8 — Failed",
                report,
                "OK");
        }

        internal static bool Validate(out string report)
        {
            StringBuilder text = new StringBuilder();
            GameObject preview =
                new GameObject("GeneratedFatManRig.ValidationPreview");
            preview.hideFlags = HideFlags.HideAndDontSave;
            bool valid = true;

            try
            {
                GeneratedFatManRigActor actor =
                    preview.AddComponent<GeneratedFatManRigActor>();
                actor.Build();

                int views = actor.ViewCount;
                int bones = actor.BoneCount;
                int surfaces = actor.SkinnedSurfaceCount;
                SkinnedMeshRenderer[] renderers =
                    preview.GetComponentsInChildren<
                        SkinnedMeshRenderer>(true);
                CharacterMeshGraphic[] legacy =
                    preview.GetComponentsInChildren<
                        CharacterMeshGraphic>(true);

                if (!actor.IsReady)
                {
                    valid = false;
                    text.AppendLine(
                        "ERROR: actor factory did not report ready.");
                }
                if (views != 3)
                {
                    valid = false;
                    text.AppendLine(
                        $"ERROR: expected 3 view rigs, got {views}.");
                }
                if (bones < 45)
                {
                    valid = false;
                    text.AppendLine(
                        $"ERROR: expected at least 45 independent bones, " +
                        $"got {bones}.");
                }
                if (surfaces < 45)
                {
                    valid = false;
                    text.AppendLine(
                        $"ERROR: expected at least 45 skinned surfaces, " +
                        $"got {surfaces}.");
                }
                if (legacy.Length > 0)
                {
                    valid = false;
                    text.AppendLine(
                        "ERROR: generated actor contains CharacterMeshGraphic " +
                        "from the old procedural mannequin.");
                }

                int validRenderers = 0;
                for (int i = 0; i < renderers.Length; i++)
                {
                    SkinnedMeshRenderer renderer = renderers[i];
                    if (renderer == null ||
                        renderer.sharedMesh == null ||
                        renderer.bones == null ||
                        renderer.bones.Length == 0 ||
                        renderer.sharedMesh.boneWeights == null ||
                        renderer.sharedMesh.boneWeights.Length == 0)
                    {
                        valid = false;
                        text.AppendLine(
                            $"ERROR: invalid skinned surface at index {i}.");
                        continue;
                    }
                    validRenderers++;
                }

                text.AppendLine($"Views: {views}/3.");
                text.AppendLine($"Independent bones: {bones}.");
                text.AppendLine(
                    $"Valid SkinnedMeshRenderer surfaces: " +
                    $"{validRenderers}/{renderers.Length}.");
                text.AppendLine(
                    "Legacy CharacterMeshGraphic surfaces: " +
                    legacy.Length + ".");
                text.AppendLine(
                    "The generated actor uses its own bone transforms and " +
                    "does not use the old red/procedural skeleton as skinning " +
                    "bones.");

                text.Insert(
                    0,
                    valid
                        ? "Generated Fat Man Bone Rig 3.8 validation passed.\n\n"
                        : "Generated Fat Man Bone Rig 3.8 validation failed.\n\n");
            }
            catch (System.Exception exception)
            {
                valid = false;
                text.Insert(
                    0,
                    "Generated Fat Man Bone Rig 3.8 validation failed.\n\n");
                text.AppendLine(exception.ToString());
            }
            finally
            {
                Object.DestroyImmediate(preview);
            }

            report = text.ToString();
            return valid;
        }
    }
}
