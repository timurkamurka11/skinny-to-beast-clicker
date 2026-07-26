using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Renders the authored SpriteSkin character into the existing uGUI room.
    ///
    /// SpriteSkin deforms SpriteRenderer geometry, not uGUI Images. The game is
    /// currently built as a ScreenSpaceOverlay canvas, so the production rig is
    /// rendered by a dedicated transparent orthographic camera into a RawImage.
    /// This keeps the real Unity 2D Animation skeleton completely independent
    /// from the old procedural UI robot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProductionFatManRenderHost : MonoBehaviour
    {
        private const int ProductionCharacterLayer = 30;
        private const string RawImageName = "ProductionFatMan.RenderTexture";

        [SerializeField] private RectTransform targetRect;
        [SerializeField] private CharacterRigController signalRig;
        [SerializeField] private CharacterSkinController signalSkin;
        [SerializeField] private bool entryMode;

        private GameObject worldRoot;
        private Camera renderCamera;
        private RenderTexture renderTexture;
        private RawImage rawImage;
        private ProductionFatManRigContract contract;
        private CharacterFacing lastFacing = (CharacterFacing)(-1);
        private CharacterRoutineAction lastAction = (CharacterRoutineAction)(-1);
        private int lastStage = -1;
        private bool tapWasActive;
        private bool built;
        private bool missingLogged;

        public bool IsReady => built && contract != null && rawImage != null;
        public ProductionFatManRigContract Contract => contract;

        public void Configure(
            RectTransform target,
            CharacterRigController sourceRig,
            CharacterSkinController sourceSkin,
            bool useEntryMode)
        {
            targetRect = target;
            signalRig = sourceRig;
            signalSkin = sourceSkin;
            entryMode = useEntryMode;
        }

        private void Start()
        {
            TryBuild();
        }

        private void LateUpdate()
        {
            if (!built)
            {
                TryBuild();
                return;
            }

            DriveAnimator();
            SuppressLegacyVisuals();
        }

        private void TryBuild()
        {
            if (built)
            {
                return;
            }

            if (targetRect == null)
            {
                targetRect = transform as RectTransform;
            }
            if (signalRig == null)
            {
                signalRig = GetComponentInParent<CharacterRigController>();
            }
            if (signalSkin == null)
            {
                signalSkin = GetComponentInParent<CharacterSkinController>();
            }

            GameObject prefab =
                Resources.Load<GameObject>(ProductionFatManRigContract.ResourcePath);
            if (prefab == null)
            {
                if (!missingLogged)
                {
                    missingLogged = true;
                    Debug.LogWarning(
                        "Production fat-man rig is not installed. Expected " +
                        "Assets/Resources/Characters/FatManProduction/FatManRig.prefab. " +
                        "The broken generated PNG rig will not be accepted as the final model.",
                        this);
                }
                return;
            }

            worldRoot = new GameObject("ProductionFatMan.WorldRoot");
            worldRoot.transform.position = Vector3.zero;
            GameObject rigObject = Instantiate(prefab, worldRoot.transform, false);
            rigObject.name = "ProductionFatManRig";
            SetLayerRecursively(rigObject, ProductionCharacterLayer);

            contract = rigObject.GetComponent<ProductionFatManRigContract>();
            if (contract == null)
            {
                Debug.LogError(
                    "FatManRig.prefab has no ProductionFatManRigContract.",
                    this);
                Destroy(worldRoot);
                worldRoot = null;
                return;
            }

            if (!contract.Validate(out string error))
            {
                Debug.LogError(
                    "Production fat-man rig validation failed:\n" + error,
                    contract);
                Destroy(worldRoot);
                worldRoot = null;
                contract = null;
                return;
            }

            CreateRenderTarget();
            FrameCameraToRig();
            built = rawImage != null && renderCamera != null;
            if (!built)
            {
                DisposeRuntimeObjects();
                return;
            }

            rawImage.transform.SetAsLastSibling();
            DriveAnimator(force: true);
            SuppressLegacyVisuals();
            Debug.Log(
                "Production fat-man SpriteSkin rig is active through a transparent RenderTexture host.",
                this);
        }

        private void CreateRenderTarget()
        {
            if (targetRect == null)
            {
                return;
            }

            Vector2 size = targetRect.rect.size;
            int width = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Abs(size.x)),
                512,
                1536);
            int height = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Abs(size.y)),
                512,
                1536);

            renderTexture = new RenderTexture(
                width,
                height,
                16,
                RenderTextureFormat.ARGB32)
            {
                name = "ProductionFatManRT",
                antiAliasing = 2,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            renderTexture.Create();

            GameObject cameraObject = new GameObject(
                "ProductionFatMan.Camera",
                typeof(Camera));
            cameraObject.transform.SetParent(worldRoot.transform, false);
            renderCamera = cameraObject.GetComponent<Camera>();
            renderCamera.orthographic = true;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = Color.clear;
            renderCamera.cullingMask = 1 << ProductionCharacterLayer;
            renderCamera.nearClipPlane = 0.1f;
            renderCamera.farClipPlane = 100f;
            renderCamera.depth = -100f;
            renderCamera.targetTexture = renderTexture;
            renderCamera.allowHDR = false;
            renderCamera.allowMSAA = true;

            GameObject imageObject = new GameObject(
                RawImageName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            imageObject.transform.SetParent(targetRect, false);
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;

            rawImage = imageObject.GetComponent<RawImage>();
            rawImage.texture = renderTexture;
            rawImage.color = Color.white;
            rawImage.raycastTarget = false;
        }

        private void FrameCameraToRig()
        {
            if (renderCamera == null || contract == null)
            {
                return;
            }

            SpriteRenderer[] renderers = contract.SpriteRenderers;
            bool hasBounds = false;
            Bounds bounds = default;
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    SpriteRenderer renderer = renderers[i];
                    if (renderer == null || !renderer.enabled)
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            if (!hasBounds)
            {
                bounds = new Bounds(Vector3.zero, new Vector3(4f, 7f, 1f));
            }

            float textureAspect = renderTexture != null && renderTexture.height > 0
                ? renderTexture.width / (float)renderTexture.height
                : 1f;
            float vertical = Mathf.Max(1f, bounds.extents.y * 1.14f);
            float horizontal = Mathf.Max(
                1f,
                bounds.extents.x / Mathf.Max(0.1f, textureAspect) * 1.14f);

            renderCamera.orthographicSize = Mathf.Max(vertical, horizontal);
            renderCamera.transform.position = new Vector3(
                bounds.center.x,
                bounds.center.y,
                bounds.min.z - 10f);
            renderCamera.transform.rotation = Quaternion.identity;
        }

        private void DriveAnimator(bool force = false)
        {
            if (contract == null)
            {
                return;
            }

            CharacterFacing facing = signalRig != null
                ? signalRig.Facing
                : entryMode
                    ? CharacterFacing.Back
                    : CharacterFacing.Front;
            if (force || facing != lastFacing)
            {
                lastFacing = facing;
                contract.SetFacing(facing);
                FrameCameraToRig();
            }

            int stage = signalSkin != null
                ? Mathf.Clamp(signalSkin.CurrentArtIndex, 0, 3)
                : 0;
            if (force || stage != lastStage)
            {
                lastStage = stage;
                contract.SetStage(stage);
            }

            bool walking = entryMode;
            contract.SetLocomotion(walking, walking ? 1f : 0f);

            if (signalRig != null)
            {
                CharacterRoutineAction action = signalRig.ActiveAction;
                if (force || action != lastAction)
                {
                    lastAction = action;
                    contract.FireAction(action);
                }

                bool tapActive = signalRig.IsTapReacting;
                if (tapActive && !tapWasActive)
                {
                    contract.FireTap();
                }
                tapWasActive = tapActive;
            }
        }

        private void SuppressLegacyVisuals()
        {
            if (!IsReady || targetRect == null)
            {
                return;
            }

            Graphic[] graphics = targetRect.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null ||
                    rawImage != null &&
                    graphic.transform.IsChildOf(rawImage.transform))
                {
                    continue;
                }

                if (graphic == rawImage)
                {
                    continue;
                }

                string name = graphic.gameObject.name;
                bool oldCharacterVisual =
                    graphic.GetComponentInParent<CharacterMeshGraphic>() != null ||
                    name.StartsWith("Sprite.RealFatMan", StringComparison.Ordinal) ||
                    name.StartsWith("LayeredFace.", StringComparison.Ordinal) ||
                    name.StartsWith("SpriteFace.", StringComparison.Ordinal) ||
                    name.StartsWith("Layer.", StringComparison.Ordinal) ||
                    name == "VisibleFill" ||
                    name == "VisibleOutline";

                if (oldCharacterVisual && graphic.canvasRenderer != null)
                {
                    graphic.canvasRenderer.SetAlpha(0f);
                }
            }
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            Transform transform = root.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
            }
        }

        private void DisposeRuntimeObjects()
        {
            if (rawImage != null)
            {
                Destroy(rawImage.gameObject);
                rawImage = null;
            }
            if (renderCamera != null)
            {
                renderCamera.targetTexture = null;
                renderCamera = null;
            }
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }
            if (worldRoot != null)
            {
                Destroy(worldRoot);
                worldRoot = null;
            }
            contract = null;
            built = false;
        }

        private void OnDestroy()
        {
            DisposeRuntimeObjects();
        }
    }
}
