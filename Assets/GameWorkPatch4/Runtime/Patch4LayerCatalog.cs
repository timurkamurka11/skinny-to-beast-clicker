using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Maps the canonical Patch 4 layer contract to imported painted sprites.
    /// The catalog is deliberately separate from the prefab so art can be
    /// replaced without rebuilding gameplay code.
    /// </summary>
    [CreateAssetMenu(
        fileName = "FatMan_Patch4_LayerCatalog",
        menuName = "Skinny To Beast/Patch 4/Layer Catalog")]
    public sealed class Patch4LayerCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string contractPath;
            public Sprite sprite;
            public string parentBone;
            public int sortingOrder;
            public bool required = true;
            public bool visibleByDefault = true;
        }

        [SerializeField] private List<Entry> entries = new();

        public IReadOnlyList<Entry> Entries => entries;

        public bool IsComplete(out List<string> missingLayers)
        {
            missingLayers = new List<string>();
            HashSet<string> observed = new(StringComparer.Ordinal);

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.contractPath))
                {
                    continue;
                }

                observed.Add(entry.contractPath);
                if (entry.required && entry.sprite == null)
                {
                    missingLayers.Add(entry.contractPath);
                }
            }

            foreach (string requiredPath in Patch4RigContract.RequiredLayerPaths)
            {
                if (!observed.Contains(requiredPath))
                {
                    missingLayers.Add(requiredPath);
                }
            }

            return missingLayers.Count == 0;
        }

        public bool TryGetEntry(string contractPath, out Entry result)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry != null &&
                    string.Equals(
                        entry.contractPath,
                        contractPath,
                        StringComparison.Ordinal))
                {
                    result = entry;
                    return true;
                }
            }

            result = null;
            return false;
        }

#if UNITY_EDITOR
        public void ReplaceEntries(IEnumerable<Entry> replacement)
        {
            entries = replacement != null
                ? new List<Entry>(replacement)
                : new List<Entry>();
        }
#endif
    }
}
