using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    public static class Patch4ArtReadinessAssetBuilder
    {
        public const string AssetPath =
            "Assets/GameWorkPatch4/Art/Patch4ArtReadiness.asset";

        public static Patch4ArtReadinessAsset EnsureAsset()
        {
            Patch4ArtReadinessAsset asset =
                AssetDatabase.LoadAssetAtPath<Patch4ArtReadinessAsset>(AssetPath);
            if (asset != null)
            {
                return asset;
            }

            EnsureFolder("Assets/GameWorkPatch4/Art");
            asset = ScriptableObject.CreateInstance<Patch4ArtReadinessAsset>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Art/Select Readiness Gate")]
        public static void SelectAsset()
        {
            Selection.activeObject = EnsureAsset();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
