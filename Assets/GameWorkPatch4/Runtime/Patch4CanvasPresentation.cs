using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Presents the full-canvas painted Patch 4 sprites as UI Images inside
    /// the Screen Space Overlay canvas used by LivingGameplayScene.
    ///
    /// Images stay in one flat hierarchy so their canonical sorting order is
    /// deterministic. Their transforms follow the independent rig bones in
    /// LateUpdate. This component never changes the art-readiness gate and
    /// never enables Patch 4.
    /// </summary>
    [DefaultExecutionOrder(1200)]
    [DisallowMultipleComponent]
    public sealed class Patch4CanvasPresentation : MonoBehaviour
    {
        private const string GeneratedRootName = "GeneratedCanvasLayers";
        private const string AnchorPrefix = "CanvasAnchor.";
        private const string LayerPrefix = "Layer.";
        private const float MinimumDimension = 0.001f;

        [SerializeField] private Patch4CharacterRigController rigController;
        [SerializeField] private Patch4LayerCatalog catalog;
        [SerializeField] private Patch4FaceController faceController;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool buildOnAwake = true;

        [Header("Approved Master")]
        [SerializeField] private Vector2 sourceCanvasPixels =
            new(1024f, 1536f);
        [SerializeField, Min(1f)] private float sourcePixelsPerUnit = 100f;
        [SerializeField, Min(0f)] private float sourcePelvisFromBottomPixels =
            706f;

        [Header("Living Gameplay Room")]
        [SerializeField, Range(0.1f, 1f)]
        private float legacyPresentationScale = 0.74f;

        private sealed class LayerBinding
        {
            public string contractPath;
            public Transform bone;
            public RectTransform followerTransform;
            public RectTransform imageTransform;
            public Image image;
        }

        private readonly List<LayerBinding> bindings = new();
        private readonly List<Image> images = new();
        private readonly List<string> missingLayers = new();

        private RectTransform generatedRoot;
        private Canvas hostCanvas;
        private bool prepared;
        private bool gameplayLayoutConfigured;
        private float roomScale;

        public bool IsPrepared => prepared;
        public bool IsCanvasReady =>
            prepared && gameplayLayoutConfigured && hostCanvas != null;
        public int ImageCount => images.Count;
        public IReadOnlyList<string> MissingLayers => missingLayers;
        public Canvas HostCanvas => hostCanvas;
        public float RoomScale => roomScale;
        public RectTransform GeneratedRoot => generatedRoot;

        private void Reset()
        {
            rigController = GetComponent<Patch4CharacterRigController>();
            faceController = GetComponent<Patch4FaceController>();
        }

        private void Awake()
        {
            if (buildOnAwake)
            {
                RebuildCanvasLayers();
            }
        }

        private void LateUpdate()
        {
            if (prepared)
            {
                SyncLayerTransforms();
            }
        }

        public bool RebuildCanvasLayers()
        {
            ClearGeneratedLayers();
            bindings.Clear();
            images.Clear();
            missingLayers.Clear();
            prepared = false;

            ResolveReferences();
            if (rigController == null ||
                !rigController.RebuildBoneMap() ||
                catalog == null)
            {
                return false;
            }

            catalog.IsComplete(out List<string> catalogMissing);
            missingLayers.AddRange(catalogMissing);

            Transform parent = visualRoot != null
                ? visualRoot
                : rigController.RigRoot;
            if (parent == null)
            {
                AddMissing("<visualRoot>");
                return false;
            }

            GameObject rootObject = new(
                GeneratedRootName,
                typeof(RectTransform));
            rootObject.layer = gameObject.layer;
            generatedRoot = rootObject.GetComponent<RectTransform>();
            generatedRoot.SetParent(parent, false);
            generatedRoot.anchorMin = new Vector2(0.5f, 0f);
            generatedRoot.anchorMax = new Vector2(0.5f, 0f);
            generatedRoot.pivot = new Vector2(0.5f, 0f);
            generatedRoot.sizeDelta =
                sourceCanvasPixels / SafePixelsPerUnit();
            generatedRoot.anchoredPosition3D = Vector3.zero;
            generatedRoot.localRotation = Quaternion.identity;
            generatedRoot.localScale = Vector3.one;

            IReadOnlyList<Patch4LayerCatalog.Entry> entries =
                catalog.Entries;
            List<int> orderedIndices = BuildOrderedIndices(entries);
            int expectedImageCount = 0;

            for (int i = 0; i < orderedIndices.Count; i++)
            {
                Patch4LayerCatalog.Entry entry =
                    entries[orderedIndices[i]];
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.contractPath) ||
                    !entry.visibleByDefault)
                {
                    continue;
                }

                expectedImageCount++;
                if (entry.sprite == null)
                {
                    if (entry.required)
                    {
                        AddMissing(entry.contractPath);
                    }

                    continue;
                }

                Transform bone = rigController.GetBone(entry.parentBone);
                if (bone == null)
                {
                    if (entry.required)
                    {
                        AddMissing(
                            entry.contractPath + "@" + entry.parentBone);
                    }

                    continue;
                }

                LayerBinding binding = CreateImage(entry, bone);
                bindings.Add(binding);
                images.Add(binding.image);
            }

            DisableFallbackSpriteRenderers();
            prepared =
                missingLayers.Count == 0 &&
                images.Count == expectedImageCount &&
                images.Count > 0;

            if (prepared)
            {
                SyncLayerTransforms();
                BindFaceLayers();
            }

            return prepared;
        }

        public bool ConfigureForGameplayRoom(
            RectTransform legacyCharacterRoot)
        {
            gameplayLayoutConfigured = false;
            hostCanvas = null;
            roomScale = 0f;

            if (legacyCharacterRoot == null)
            {
                return false;
            }

            if (!prepared && !RebuildCanvasLayers())
            {
                return false;
            }

            hostCanvas =
                legacyCharacterRoot.GetComponentInParent<Canvas>(true);
            if (hostCanvas == null)
            {
                return false;
            }

            float roomHeight = Mathf.Abs(legacyCharacterRoot.rect.height);
            if (roomHeight < MinimumDimension)
            {
                roomHeight =
                    Mathf.Abs(legacyCharacterRoot.sizeDelta.y);
            }

            float sourceHeightUnits =
                sourceCanvasPixels.y / SafePixelsPerUnit();
            if (roomHeight < MinimumDimension ||
                sourceHeightUnits < MinimumDimension)
            {
                return false;
            }

            roomScale =
                roomHeight * legacyPresentationScale / sourceHeightUnits;
            float pelvisHeightUnits =
                sourcePelvisFromBottomPixels / SafePixelsPerUnit();

            transform.localPosition =
                new Vector3(0f, -pelvisHeightUnits * roomScale, 0f);
            transform.localRotation = Quaternion.identity;
            transform.localScale =
                new Vector3(roomScale, roomScale, 1f);

            gameplayLayoutConfigured = true;
            DisableFallbackSpriteRenderers();
            SyncLayerTransforms();
            return IsCanvasReady;
        }

        public void ClearGeneratedLayers()
        {
            prepared = false;
            bindings.Clear();
            images.Clear();

            Transform searchRoot = visualRoot != null
                ? visualRoot
                : transform;
            if (generatedRoot == null && searchRoot != null)
            {
                generatedRoot =
                    searchRoot.Find(GeneratedRootName) as RectTransform;
            }

            if (generatedRoot == null)
            {
                return;
            }

            GameObject target = generatedRoot.gameObject;
            generatedRoot = null;
            target.SetActive(false);
            DestroyGeneratedObject(target);
        }

        private LayerBinding CreateImage(
            Patch4LayerCatalog.Entry entry,
            Transform bone)
        {
            string generatedName =
                entry.contractPath.Replace('/', '.');
            GameObject anchorObject = new(
                AnchorPrefix + generatedName,
                typeof(RectTransform));
            anchorObject.layer = gameObject.layer;

            RectTransform follower =
                anchorObject.GetComponent<RectTransform>();
            follower.SetParent(generatedRoot, false);
            follower.anchorMin = new Vector2(0.5f, 0f);
            follower.anchorMax = new Vector2(0.5f, 0f);
            follower.pivot = new Vector2(0.5f, 0.5f);
            follower.sizeDelta = Vector2.zero;
            follower.anchoredPosition3D = Vector3.zero;
            follower.localRotation = Quaternion.identity;
            follower.localScale = Vector3.one;

            GameObject layerObject = new(
                LayerPrefix + generatedName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            layerObject.layer = gameObject.layer;

            RectTransform rect =
                layerObject.GetComponent<RectTransform>();
            rect.SetParent(follower, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);

            Sprite sprite = entry.sprite;
            Vector2 spriteSize = sprite.rect.size;
            rect.pivot = new Vector2(
                SafeDivide(sprite.pivot.x, spriteSize.x, 0.5f),
                SafeDivide(sprite.pivot.y, spriteSize.y, 0.5f));
            rect.sizeDelta = spriteSize / Mathf.Max(
                MinimumDimension,
                sprite.pixelsPerUnit);
            rect.anchoredPosition3D = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            Image image = layerObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            image.maskable = false;
            image.preserveAspect = false;
            image.type = Image.Type.Simple;
            image.useSpriteMesh = true;

            return new LayerBinding
            {
                contractPath = entry.contractPath,
                bone = bone,
                followerTransform = follower,
                imageTransform = rect,
                image = image
            };
        }

        private void ResolveReferences()
        {
            if (rigController == null)
            {
                rigController =
                    GetComponent<Patch4CharacterRigController>();
            }

            if (faceController == null)
            {
                faceController = GetComponent<Patch4FaceController>();
            }
        }

        private void BindFaceLayers()
        {
            if (faceController == null)
            {
                return;
            }

            faceController.BindPresentationLayers(
                FindLayerTransform("Face/LidL"),
                FindLayerTransform("Face/LidR"),
                FindLayerObject("Face/MouthClosed"),
                FindLayerObject("Face/MouthOpen"),
                FindLayerObject("Face/MouthSmile"));
        }

        private Transform FindLayerTransform(string contractPath)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (string.Equals(
                    bindings[i].contractPath,
                    contractPath,
                    StringComparison.Ordinal))
                {
                    return bindings[i].imageTransform;
                }
            }

            return null;
        }

        private GameObject FindLayerObject(string contractPath)
        {
            Transform result = FindLayerTransform(contractPath);
            return result != null ? result.gameObject : null;
        }

        private void SyncLayerTransforms()
        {
            if (generatedRoot == null)
            {
                return;
            }

            Vector3 rootScale = generatedRoot.lossyScale;
            for (int i = 0; i < bindings.Count; i++)
            {
                LayerBinding binding = bindings[i];
                if (binding.bone == null ||
                    binding.followerTransform == null ||
                    binding.imageTransform == null)
                {
                    continue;
                }

                RectTransform follower = binding.followerTransform;
                follower.anchoredPosition3D =
                    generatedRoot.InverseTransformPoint(
                        binding.bone.position);
                follower.localRotation =
                    Quaternion.Inverse(generatedRoot.rotation) *
                    binding.bone.rotation;

                Vector3 boneScale = binding.bone.lossyScale;
                follower.localScale = new Vector3(
                    SafeDivide(boneScale.x, rootScale.x, 1f),
                    SafeDivide(boneScale.y, rootScale.y, 1f),
                    SafeDivide(boneScale.z, rootScale.z, 1f));
            }
        }

        private void DisableFallbackSpriteRenderers()
        {
            Transform searchRoot = visualRoot != null
                ? visualRoot
                : transform;
            SpriteRenderer[] renderers =
                searchRoot.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer != null &&
                    renderer.gameObject.name.StartsWith(
                        LayerPrefix,
                        StringComparison.Ordinal))
                {
                    renderer.enabled = false;
                }
            }
        }

        private static List<int> BuildOrderedIndices(
            IReadOnlyList<Patch4LayerCatalog.Entry> entries)
        {
            List<int> result = new(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                result.Add(i);
            }

            result.Sort((left, right) =>
            {
                Patch4LayerCatalog.Entry leftEntry = entries[left];
                Patch4LayerCatalog.Entry rightEntry = entries[right];
                int leftOrder =
                    leftEntry != null ? leftEntry.sortingOrder : int.MinValue;
                int rightOrder =
                    rightEntry != null ? rightEntry.sortingOrder : int.MinValue;
                int order = leftOrder.CompareTo(rightOrder);
                return order != 0 ? order : left.CompareTo(right);
            });
            return result;
        }

        private void AddMissing(string value)
        {
            if (!missingLayers.Contains(value))
            {
                missingLayers.Add(value);
            }
        }

        private float SafePixelsPerUnit()
        {
            return Mathf.Max(MinimumDimension, sourcePixelsPerUnit);
        }

        private static float SafeDivide(
            float numerator,
            float denominator,
            float fallback)
        {
            return Mathf.Abs(denominator) >= MinimumDimension
                ? numerator / denominator
                : fallback;
        }

        private static void DestroyGeneratedObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
