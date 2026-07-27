using System.Collections.Generic;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Builds the painted character from independent sprites parented to the
    /// new Patch 4 skeleton. No procedural/basic-shape body is generated.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Patch4LayerRenderer : MonoBehaviour
    {
        private const string GeneratedRootName = "GeneratedPaintedLayers";
        private const string GeneratedPrefix = "Layer.";

        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private Patch4LayerCatalog catalog;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField] private bool autoEnableWhenComplete;

        private readonly List<SpriteRenderer> renderers = new();
        private readonly List<string> missingLayers = new();
        private Transform generatedRoot;
        private bool complete;

        public bool IsComplete => complete;
        public IReadOnlyList<string> MissingLayers => missingLayers;
        public IReadOnlyList<SpriteRenderer> Renderers => renderers;

        private void Reset()
        {
            rigController = GetComponent<Patch4CharacterRigController>();
        }

        private void Awake()
        {
            if (buildOnAwake)
            {
                RebuildLayers();
            }
        }

        public bool RebuildLayers()
        {
            ClearGeneratedLayers();
            missingLayers.Clear();
            renderers.Clear();
            complete = false;

            if (rigController == null)
            {
                rigController = GetComponent<Patch4CharacterRigController>();
            }

            if (rigController == null ||
                !rigController.RebuildBoneMap() ||
                catalog == null)
            {
                if (autoEnableWhenComplete && rigController != null)
                {
                    rigController.SetPatch4Enabled(false);
                }

                return false;
            }

            catalog.IsComplete(out List<string> catalogMissing);
            missingLayers.AddRange(catalogMissing);

            Transform parent = visualRoot != null
                ? visualRoot
                : rigController.RigRoot;
            generatedRoot = new GameObject(GeneratedRootName).transform;
            generatedRoot.SetParent(parent, false);

            IReadOnlyList<Patch4LayerCatalog.Entry> entries = catalog.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                Patch4LayerCatalog.Entry entry = entries[i];
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.contractPath) ||
                    !entry.visibleByDefault)
                {
                    continue;
                }

                if (entry.sprite == null)
                {
                    if (entry.required &&
                        !missingLayers.Contains(entry.contractPath))
                    {
                        missingLayers.Add(entry.contractPath);
                    }

                    continue;
                }

                Transform layerParent = ResolveParent(entry.parentBone);
                GameObject layerObject = new(
                    GeneratedPrefix + entry.contractPath.Replace('/', '.'));
                layerObject.layer = gameObject.layer;
                layerObject.transform.SetParent(layerParent, false);
                layerObject.transform.localPosition = Vector3.zero;
                layerObject.transform.localRotation = Quaternion.identity;
                layerObject.transform.localScale = Vector3.one;

                SpriteRenderer spriteRenderer =
                    layerObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = entry.sprite;
                spriteRenderer.sortingOrder = entry.sortingOrder;
                if (!string.IsNullOrWhiteSpace(sortingLayerName))
                {
                    spriteRenderer.sortingLayerName = sortingLayerName;
                }

                renderers.Add(spriteRenderer);
            }

            complete = missingLayers.Count == 0 && renderers.Count > 0;
            if (autoEnableWhenComplete)
            {
                rigController.SetPatch4Enabled(complete);
            }

            return complete;
        }

        public void ClearGeneratedLayers()
        {
            if (generatedRoot == null)
            {
                Transform searchRoot = visualRoot != null
                    ? visualRoot
                    : transform;
                generatedRoot = searchRoot.Find(GeneratedRootName);
            }

            if (generatedRoot == null)
            {
                return;
            }

            GameObject target = generatedRoot.gameObject;
            generatedRoot = null;

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private Transform ResolveParent(string boneName)
        {
            if (!string.IsNullOrWhiteSpace(boneName))
            {
                Transform bone = rigController.GetBone(boneName);
                if (bone != null)
                {
                    return bone;
                }
            }

            return generatedRoot;
        }
    }
}
