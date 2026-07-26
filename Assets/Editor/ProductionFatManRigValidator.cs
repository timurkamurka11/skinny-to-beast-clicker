using System.Text;
using SkinnyToBeast.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SkinnyToBeast.EditorTools
{
    internal static class ProductionFatManRigValidator
    {
        private const string PrefabPath =
            "Assets/Resources/Characters/FatManProduction/" +
            "FatManProductionRig.prefab";

        [MenuItem(
            "Tools/Skinny to Beast/Validate Production Fat Man Rig 3.7")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate(out string report);
            if (valid)
            {
                Debug.Log(report);
                EditorUtility.DisplayDialog(
                    "Production Fat Man Rig 3.7",
                    report,
                    "OK");
            }
            else
            {
                Debug.LogError(report);
                EditorUtility.DisplayDialog(
                    "Production Fat Man Rig 3.7 — Not Ready",
                    report,
                    "OK");
            }
        }

        internal static bool Validate(out string report)
        {
            StringBuilder text = new();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);
            if (prefab == null)
            {
                text.AppendLine("Production rig asset is not installed.");
                text.AppendLine();
                text.AppendLine("Expected:");
                text.AppendLine(PrefabPath);
                text.AppendLine();
                text.AppendLine(
                    "The game will use the intact whole-body fallback. " +
                    "It will not show Patch 3.6 generated cut-outs, but " +
                    "production skeletal animation cannot exist until a " +
                    "layered PSB/rigged prefab is supplied.");
                report = text.ToString();
                return false;
            }

            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            SpriteSkin[] skins =
                prefab.GetComponentsInChildren<SpriteSkin>(true);
            SpriteRenderer[] renderers =
                prefab.GetComponentsInChildren<SpriteRenderer>(true);
            CharacterMeshGraphic[] legacy =
                prefab.GetComponentsInChildren<CharacterMeshGraphic>(true);

            bool valid = true;
            if (animator == null)
            {
                valid = false;
                text.AppendLine("ERROR: Animator is missing.");
            }
            if (skins == null || skins.Length == 0)
            {
                valid = false;
                text.AppendLine("ERROR: no SpriteSkin components found.");
            }
            else
            {
                int validSkins = 0;
                for (int i = 0; i < skins.Length; i++)
                {
                    SpriteSkin skin = skins[i];
                    if (skin != null &&
                        skin.GetComponent<SpriteRenderer>() != null &&
                        skin.boneTransforms != null &&
                        skin.boneTransforms.Length > 0)
                    {
                        validSkins++;
                    }
                }
                if (validSkins == 0)
                {
                    valid = false;
                    text.AppendLine(
                        "ERROR: SpriteSkin components have no authored bones.");
                }
                else
                {
                    text.AppendLine(
                        $"OK: {validSkins}/{skins.Length} SpriteSkin surfaces " +
                        "have renderers and bone transforms.");
                }
            }

            if (renderers == null || renderers.Length == 0)
            {
                valid = false;
                text.AppendLine("ERROR: no SpriteRenderer surfaces found.");
            }
            else
            {
                text.AppendLine(
                    $"OK: {renderers.Length} SpriteRenderer surfaces found.");
            }

            if (legacy != null && legacy.Length > 0)
            {
                valid = false;
                text.AppendLine(
                    "ERROR: production prefab contains CharacterMeshGraphic " +
                    "from the procedural mannequin.");
            }

            if (valid)
            {
                text.Insert(
                    0,
                    "Production Fat Man Rig 3.7 validation passed.\n\n");
            }
            else
            {
                text.Insert(
                    0,
                    "Production Fat Man Rig 3.7 validation failed.\n\n");
            }

            report = text.ToString();
            return valid;
        }
    }
}
