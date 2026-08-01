using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    public static class Patch4LayerCatalogBuilder
    {
        public const string CatalogPath =
            "Assets/GameWorkPatch4/Art/Character/FatMan/FatMan_Patch4_LayerCatalog.asset";

        [MenuItem("Tools/GameWork/Patch 4.0/Art/Rebuild Layer Catalog")]
        public static void RebuildCatalog()
        {
            EnsureFolder("Assets/GameWorkPatch4/Art/Character/FatMan/Layers");

            Patch4LayerCatalog catalog =
                AssetDatabase.LoadAssetAtPath<Patch4LayerCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<Patch4LayerCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            List<Patch4LayerCatalog.Entry> entries = new();
            foreach (string contractPath in Patch4RigContract.RequiredLayerPaths)
            {
                string fileName = contractPath.Replace('/', '_') + ".png";
                string assetPath =
                    Patch4LayerImportPostprocessor.LayerRoot + fileName;

                entries.Add(new Patch4LayerCatalog.Entry
                {
                    contractPath = contractPath,
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath),
                    parentBone =
                        Patch4LayerPlacement.ResolveParentBone(contractPath),
                    sortingOrder = ResolveSortingOrder(contractPath),
                    required = true,
                    visibleByDefault =
                        Patch4RigContract.IsRuntimeLayerVisibleByDefault(
                            contractPath)
                });
            }

            Undo.RecordObject(catalog, "Rebuild Patch 4 layer catalog");
            catalog.ReplaceEntries(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool complete = catalog.IsComplete(out List<string> missing);
            Debug.Log(
                complete
                    ? "Patch 4 layer catalog is complete."
                    : "Patch 4 layer catalog rebuilt. Missing sprites: " +
                      string.Join(", ", missing),
                catalog);
            Selection.activeObject = catalog;
        }

        private static int ResolveSortingOrder(string path)
        {
            if (path == "FX/Shadow") return -100;
            if (path.StartsWith("Leg", StringComparison.Ordinal)) return 10;
            if (path == "Clothes/Bottoms") return 20;
            if (path.StartsWith("Body/", StringComparison.Ordinal)) return 40;
            if (path.StartsWith("Clothes/Shirt", StringComparison.Ordinal)) return 50;
            if (path.StartsWith("Arm", StringComparison.Ordinal)) return 60;
            if (path.StartsWith("Head/", StringComparison.Ordinal)) return 80;
            if (path.StartsWith("Face/", StringComparison.Ordinal)) return 100;
            if (path.StartsWith("FX/", StringComparison.Ordinal)) return 120;
            return 0;
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
