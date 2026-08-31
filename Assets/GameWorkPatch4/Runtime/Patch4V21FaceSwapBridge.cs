using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// v21 face bridge. HeadBase owns the exact neutral master face. Only full
    /// feathered replacement poses are bound here, so neutral eyes or mouth can
    /// never be drawn twice over the head artwork.
    /// </summary>
    [DefaultExecutionOrder(1250)]
    [DisallowMultipleComponent]
    public sealed class Patch4V21FaceSwapBridge : MonoBehaviour
    {
        private const string VisualRootName = "Patch4VisualRoot";
        private const string GeneratedRootName = "GeneratedCanvasLayers";
        private const string LayerPrefix = "Layer.";

        [SerializeField] private Patch4FaceController faceController;
        [SerializeField] private Patch4CanvasPresentation canvasPresentation;
        private Transform generatedRoot;
        private int boundRootId;

        public bool IsPresentationReady =>
            generatedRoot != null && boundRootId == generatedRoot.GetInstanceID();

        public bool PrepareHiddenPresentation()
        {
            TryBind();
            return IsPresentationReady;
        }

        private void Reset()
        {
            faceController = GetComponent<Patch4FaceController>();
            canvasPresentation = GetComponent<Patch4CanvasPresentation>();
        }

        private void OnEnable()
        {
            TryBind();
        }

        private void Update()
        {
            if (!IsPresentationReady) TryBind();
        }

        private void TryBind()
        {
            if (faceController == null)
            {
                faceController = GetComponent<Patch4FaceController>();
            }
            if (canvasPresentation == null)
            {
                canvasPresentation = GetComponent<Patch4CanvasPresentation>();
            }
            if (faceController == null)
            {
                return;
            }

            Transform candidate = canvasPresentation != null
                ? canvasPresentation.GeneratedRoot
                : null;
            if (candidate == null)
            {
                return;
            }
            generatedRoot = candidate;
            int rootId = generatedRoot.GetInstanceID();
            if (boundRootId == rootId)
            {
                return;
            }

            Dictionary<string, Image> images = BuildImageMap(generatedRoot);
            if (!TryGet(images, "Face/EyeWhiteL", out Image eyeL) ||
                !TryGet(images, "Face/EyeWhiteR", out Image eyeR) ||
                !TryGet(images, "Face/LidL", out Image lidL) ||
                !TryGet(images, "Face/LidR", out Image lidR) ||
                !TryGet(images, "Face/MouthOpen", out Image open) ||
                !TryGet(images, "Face/MouthSmile", out Image smile))
            {
                return;
            }

            faceController.BindPresentationLayers(
                null,
                null,
                null,
                null,
                lidL.transform,
                lidR.transform,
                null,
                open.gameObject,
                smile.gameObject);
            faceController.BindLookReplacementLayers(
                eyeL.gameObject,
                eyeR.gameObject);
            boundRootId = rootId;

            Debug.Log(
                "Patch 4 v21 face bound with one exact neutral owner plus " +
                "feathered blink, gaze and mouth replacement poses.",
                this);
        }

        private static Dictionary<string, Image> BuildImageMap(Transform root)
        {
            Dictionary<string, Image> result = new(StringComparer.Ordinal);
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null ||
                    !image.gameObject.name.StartsWith(
                        LayerPrefix,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                string dotted = image.gameObject.name.Substring(LayerPrefix.Length);
                result[dotted.Replace('.', '/')] = image;
            }
            return result;
        }

        private static bool TryGet(
            IReadOnlyDictionary<string, Image> images,
            string path,
            out Image image)
        {
            return images.TryGetValue(path, out image) && image != null;
        }
    }
}
