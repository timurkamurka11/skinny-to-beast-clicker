using System.Text;
using SkinnyToBeast.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

namespace SkinnyToBeast.EditorTools
{
    internal static class ProductionFatManRigValidator
    {
        private const string PrefabPath =
            "Assets/Resources/Characters/FatManProduction/FatManRig.prefab";

        [MenuItem(
            "Tools/Skinny to Beast/Production Rig 4.0/Validate Fat Man Rig")]
        private static void ValidateMenu()
        {
            if (Validate(out string report))
            {
                Debug.Log("Production Fat Man Rig 4.0 validation passed.\n" + report);
                EditorUtility.DisplayDialog(
                    "Production Fat Man Rig 4.0",
                    "Validation passed. The prefab uses its own authored " +
                    "SpriteSkin skeleton and can replace the procedural robot.",
                    "OK");
            }
            else
            {
                Debug.LogError(
                    "Production Fat Man Rig 4.0 validation failed.\n" + report);
                EditorUtility.DisplayDialog(
                    "Production Fat Man Rig 4.0",
                    "Validation failed. See the first Console error.",
                    "OK");
            }
        }

        [MenuItem(
            "Tools/Skinny to Beast/Production Rig 4.0/Select Fat Man Rig")]
        private static void SelectRig()
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(PrefabPath);
            Selection.activeObject = asset;
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
            }
            else
            {
                Debug.LogError(
                    "Production rig prefab is missing at " + PrefabPath);
            }
        }

        internal static bool Validate(out string report)
        {
            StringBuilder result = new StringBuilder();
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                report =
                    "Missing prefab: " + PrefabPath + "\n" +
                    "Import the layered PSB/PSD, rig it with Unity 2D " +
                    "Animation, create the prefab and attach " +
                    "ProductionFatManRigContract.";
                return false;
            }

            ProductionFatManRigContract contract =
                prefab.GetComponent<ProductionFatManRigContract>();
            if (contract == null)
            {
                report =
                    "FatManRig.prefab has no ProductionFatManRigContract.";
                return false;
            }

            bool valid = contract.Validate(out string contractError);
            if (!valid)
            {
                result.AppendLine(contractError);
            }

            CharacterMeshGraphic[] procedural =
                prefab.GetComponentsInChildren<CharacterMeshGraphic>(true);
            if (procedural.Length > 0)
            {
                valid = false;
                result.AppendLine(
                    "The production prefab contains CharacterMeshGraphic. " +
                    "It must not contain or inherit the old robot renderer.");
            }

            Image[] uiImages = prefab.GetComponentsInChildren<Image>(true);
            if (uiImages.Length > 0)
            {
                valid = false;
                result.AppendLine(
                    "The production prefab contains uGUI Image layers. " +
                    "Use SpriteRenderer + SpriteSkin inside the production rig; " +
                    "the game displays it through a RenderTexture host.");
            }

            SpriteSkin[] skins =
                prefab.GetComponentsInChildren<SpriteSkin>(true);
            SpriteRenderer[] renderers =
                prefab.GetComponentsInChildren<SpriteRenderer>(true);
            Animator animator = prefab.GetComponentInChildren<Animator>(true);

            result.AppendLine("SpriteSkin count: " + skins.Length);
            result.AppendLine("SpriteRenderer count: " + renderers.Length);
            result.AppendLine(
                "Animator controller: " +
                (animator != null && animator.runtimeAnimatorController != null
                    ? animator.runtimeAnimatorController.name
                    : "MISSING"));

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                valid = false;
            }

            if (skins.Length == 0 || renderers.Length < 8)
            {
                valid = false;
            }

            report = result.ToString().Trim();
            return valid;
        }
    }
}
