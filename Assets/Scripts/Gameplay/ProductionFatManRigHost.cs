using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Bridge for a genuinely authored Unity 2D Animation character.
    ///
    /// Final art is expected as a layered PSB-derived prefab containing
    /// SpriteSkin, SpriteRenderer, authored bones/weights and its own Animator.
    /// It is rendered by a dedicated transparent camera into the existing uGUI
    /// layout, so it does not reuse or deform the old procedural mannequin.
    ///
    /// Until that prefab exists, the intact whole-body painted sprite is used as
    /// an explicit temporary fallback. Patch 3.6 generated cut-outs are disabled.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(22000)]
    public sealed class ProductionFatManRigHost : MonoBehaviour
    {
        public const string ProductionPrefabResourcePath =
            "Characters/FatManProduction/FatManProductionRig";

        private const string LayeredRootName = "RealFatMan.LayeredArt3_6";
        private const string SurfaceName = "ProductionFatMan.RenderSurface";
        private const int RenderLayer = 31;
        private const int TextureWidth = 768;
        private const int TextureHeight = 1280;
        private const float DisplayHeight = 1120f;

        private static readonly int FacingParameter = Animator.StringToHash("Facing");
        private static readonly int StageParameter = Animator.StringToHash("Stage");
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int TapParameter = Animator.StringToHash("Tap");
        private static readonly int ActionParameter = Animator.StringToHash("Action");

        private readonly Dictionary<int, AnimatorControllerParameterType>
            animatorParameters = new();
        private readonly Vector3[] worldCorners = new Vector3[4];

        private CharacterRigController rigController;
        private CharacterSkinController skinController;
        private CharacterLayeredRigController brokenLayeredController;
        private CharacterSpriteRigController flatFallbackController;
        private RectTransform visualRoot;
        private RectTransform renderSurfaceRect;
        private RawImage renderSurface;
        private GameObject worldRoot;
        private GameObject productionInstance;
        private Camera renderCamera;
        private RenderTexture renderTexture;
        private Animator productionAnimator;
        private SpriteSkin[] spriteSkins;
        private SpriteRenderer[] spriteRenderers;
        private float displayFitScale = 1f;
        private int lastFacing = int.MinValue;
        private int lastStage = int.MinValue;
        private bool buildAttempted;
        private bool missingLogged;
        private bool invalidLogged;

        public bool IsProductionReady { get; private set; }
        public bool IsFallbackReady =>
            !IsProductionReady &&
            flatFallbackController != null &&
            flatFallbackController.IsReady;
        public bool IsReady => IsProductionReady || IsFallbackReady;
        public bool RequiresAuthoredAsset => !IsProductionReady;
        public string ActiveMode => IsProductionReady
            ? "ProductionSpriteSkin"
            : IsFallbackReady
                ? "TemporaryWholeBodyFallback"
                : "Waiting";

        private void Awake()
        {
            CacheControllers();
        }

        private void Update()
        {
            CacheControllers();
            if (!buildAttempted)
            {
                TryBuild();
            }

            if (IsProductionReady)
            {
                SyncAnimatorSignals();
                ReframeCamera();
            }
            else
            {
                ActivateSafeFallback();
            }
        }

        private void LateUpdate()
        {
            DisableBrokenLayeredPath();
            if (IsProductionReady)
            {
                HideLegacyVisuals(hideWholeBodyFallback: true);
                renderSurfaceRect?.SetAsLastSibling();
            }
            else
            {
                HideLegacyVisuals(hideWholeBodyFallback: false);
            }
        }

        private void CacheControllers()
        {
            rigController ??= GetComponent<CharacterRigController>();
            skinController ??= GetComponent<CharacterSkinController>();
            brokenLayeredController ??=
                GetComponent<CharacterLayeredRigController>();
            flatFallbackController ??=
                GetComponent<CharacterSpriteRigController>();
            visualRoot ??= rigController != null
                ? rigController.VisualRoot
                : null;
        }

        private void TryBuild()
        {
            if (rigController == null ||
                skinController == null ||
                rigController.VisualRoot == null ||
                !rigController.HasAppliedSkin)
            {
                return;
            }

            buildAttempted = true;
            visualRoot = rigController.VisualRoot;
            DisableBrokenLayeredPath();

            GameObject prefab =
                Resources.Load<GameObject>(ProductionPrefabResourcePath);
            if (prefab == null)
            {
                ActivateSafeFallback();
                if (!missingLogged)
                {
                    missingLogged = true;
                    Debug.LogWarning(
                        "A production fat-man 2D rig has not been supplied yet. " +
                        "Expected Assets/Resources/Characters/FatManProduction/" +
                        "FatManProductionRig.prefab. The clean intact body sprite " +
                        "is used temporarily and all generated cut-outs are off.",
                        this);
                }
                return;
            }

            if (!CreateProductionPipeline(prefab))
            {
                ActivateSafeFallback();
                if (!invalidLogged)
                {
                    invalidLogged = true;
                    Debug.LogError(
                        "FatManProductionRig.prefab is not a valid Unity 2D " +
                        "Animation actor. It must contain an Animator, one or " +
                        "more SpriteSkin components, authored boneTransforms and " +
                        "visible SpriteRenderers.",
                        this);
                }
                return;
            }

            IsProductionReady = true;
            missingLogged = false;
            invalidLogged = false;
            if (flatFallbackController != null)
            {
                flatFallbackController.enabled = false;
            }
            HideLegacyVisuals(hideWholeBodyFallback: true);
            Debug.Log(
                $"Production fat-man rig active: {spriteSkins.Length} " +
                "SpriteSkin surfaces with independent authored bones/weights.",
                this);
        }

        private bool CreateProductionPipeline(GameObject prefab)
        {
            ClearProductionPipeline();

            renderTexture = new RenderTexture(
                TextureWidth,
                TextureHeight,
                24,
                RenderTextureFormat.ARGB32)
            {
                name = "FatManProductionRigRT",
                antiAliasing = 2,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            renderTexture.Create();

            GameObject surfaceObject = new(
                SurfaceName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            surfaceObject.layer = gameObject.layer;
            renderSurfaceRect = surfaceObject.GetComponent<RectTransform>();
            renderSurfaceRect.SetParent(visualRoot, false);
            renderSurfaceRect.anchorMin = new Vector2(0.5f, 0.5f);
            renderSurfaceRect.anchorMax = new Vector2(0.5f, 0.5f);
            renderSurfaceRect.pivot = new Vector2(0.5f, 0.5f);
            renderSurfaceRect.anchoredPosition = new Vector2(0f, -18f);
            renderSurfaceRect.sizeDelta = new Vector2(
                DisplayHeight * TextureWidth / TextureHeight,
                DisplayHeight);
            renderSurfaceRect.localScale = Vector3.one;
            renderSurfaceRect.SetAsLastSibling();

            renderSurface = surfaceObject.GetComponent<RawImage>();
            renderSurface.texture = renderTexture;
            renderSurface.color = Color.white;
            renderSurface.raycastTarget = false;
            renderSurface.maskable = false;

            worldRoot = new GameObject("ProductionFatMan.WorldRig");
            worldRoot.transform.position = new Vector3(20000f, 20000f, 0f);

            GameObject cameraObject = new("ProductionFatMan.Camera");
            cameraObject.transform.SetParent(worldRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            renderCamera = cameraObject.AddComponent<Camera>();
            renderCamera.orthographic = true;
            renderCamera.orthographicSize = 5f;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            renderCamera.cullingMask = 1 << RenderLayer;
            renderCamera.targetTexture = renderTexture;
            renderCamera.allowHDR = false;
            renderCamera.allowMSAA = true;
            renderCamera.depth = -100f;

            productionInstance = Instantiate(prefab, worldRoot.transform, false);
            productionInstance.name = "FatManProductionRig.Runtime";
            productionInstance.transform.localPosition = Vector3.zero;
            productionInstance.transform.localRotation = Quaternion.identity;
            SetLayerRecursively(productionInstance.transform, RenderLayer);

            productionAnimator =
                productionInstance.GetComponentInChildren<Animator>(true);
            spriteSkins =
                productionInstance.GetComponentsInChildren<SpriteSkin>(true);
            spriteRenderers =
                productionInstance.GetComponentsInChildren<SpriteRenderer>(true);

            if (productionAnimator == null ||
                spriteSkins == null || spriteSkins.Length == 0 ||
                spriteRenderers == null || spriteRenderers.Length == 0)
            {
                ClearProductionPipeline();
                return false;
            }

            int validSkinCount = 0;
            for (int i = 0; i < spriteSkins.Length; i++)
            {
                SpriteSkin skin = spriteSkins[i];
                if (skin != null &&
                    skin.spriteRenderer != null &&
                    skin.boneTransforms != null &&
                    skin.boneTransforms.Length > 0)
                {
                    validSkinCount++;
                }
            }
            if (validSkinCount == 0)
            {
                ClearProductionPipeline();
                return false;
            }

            CacheAnimatorParameters();
            ReframeCamera(force: true);
            SyncAnimatorSignals(force: true);
            return true;
        }

        private void ActivateSafeFallback()
        {
            IsProductionReady = false;
            DisableBrokenLayeredPath();
            if (flatFallbackController != null)
            {
                flatFallbackController.enabled = true;
            }
            if (renderSurfaceRect != null)
            {
                renderSurfaceRect.gameObject.SetActive(false);
            }
        }

        private void DisableBrokenLayeredPath()
        {
            if (brokenLayeredController != null)
            {
                brokenLayeredController.enabled = false;
            }
            if (visualRoot == null)
            {
                return;
            }
            Transform root = visualRoot.Find(LayeredRootName);
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }

        private void HideLegacyVisuals(bool hideWholeBodyFallback)
        {
            if (visualRoot == null)
            {
                return;
            }

            CharacterMeshGraphic[] meshes =
                visualRoot.GetComponentsInChildren<CharacterMeshGraphic>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                CharacterMeshGraphic mesh = meshes[i];
                if (mesh == null)
                {
                    continue;
                }
                mesh.canvasRenderer.SetAlpha(0f);
                Graphic[] graphics =
                    mesh.GetComponentsInChildren<Graphic>(true);
                for (int g = 0; g < graphics.Length; g++)
                {
                    if (graphics[g] != null)
                    {
                        graphics[g].canvasRenderer.SetAlpha(0f);
                    }
                }
            }

            Image[] images = visualRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null)
                {
                    continue;
                }

                string objectName = image.gameObject.name;
                bool faceOverlay =
                    objectName.StartsWith("LayeredFace.", StringComparison.Ordinal) ||
                    objectName.StartsWith("SpriteFace.", StringComparison.Ordinal);
                bool wholeBody =
                    objectName.StartsWith("Sprite.RealFatMan", StringComparison.Ordinal);
                if (faceOverlay ||
                    objectName == "VisibleFill" ||
                    objectName == "VisibleOutline" ||
                    (hideWholeBodyFallback && wholeBody))
                {
                    image.canvasRenderer.SetAlpha(0f);
                }
            }
        }

        private void CacheAnimatorParameters()
        {
            animatorParameters.Clear();
            AnimatorControllerParameter[] parameters =
                productionAnimator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                animatorParameters[parameters[i].nameHash] =
                    parameters[i].type;
            }
        }

        private void SyncAnimatorSignals(bool force = false)
        {
            if (productionAnimator == null || rigController == null)
            {
                return;
            }

            int facing = (int)rigController.Facing;
            int stage = skinController != null
                ? Mathf.Max(0, skinController.CurrentArtIndex)
                : 0;
            if (force || facing != lastFacing)
            {
                SetInt(FacingParameter, facing);
                lastFacing = facing;
            }
            if (force || stage != lastStage)
            {
                SetInt(StageParameter, stage);
                lastStage = stage;
            }

            SetFloat(SpeedParameter, rigController.IsMoving ? 1f : 0f);
            SetBool(TapParameter, rigController.IsTapReacting);
            SetInt(ActionParameter, (int)rigController.ActiveAction);
        }

        private void SetInt(int hash, int value)
        {
            if (animatorParameters.TryGetValue(
                    hash,
                    out AnimatorControllerParameterType type) &&
                type == AnimatorControllerParameterType.Int)
            {
                productionAnimator.SetInteger(hash, value);
            }
        }

        private void SetFloat(int hash, float value)
        {
            if (animatorParameters.TryGetValue(
                    hash,
                    out AnimatorControllerParameterType type) &&
                type == AnimatorControllerParameterType.Float)
            {
                productionAnimator.SetFloat(hash, value);
            }
        }

        private void SetBool(int hash, bool value)
        {
            if (animatorParameters.TryGetValue(
                    hash,
                    out AnimatorControllerParameterType type) &&
                type == AnimatorControllerParameterType.Bool)
            {
                productionAnimator.SetBool(hash, value);
            }
        }

        private void ReframeCamera(bool force = false)
        {
            if ((!IsProductionReady && !force) ||
                renderCamera == null ||
                spriteRenderers == null)
            {
                return;
            }

            bool found = false;
            Bounds bounds = default;
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer renderer = spriteRenderers[i];
                if (renderer == null ||
                    !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy ||
                    renderer.sprite == null ||
                    renderer.color.a <= 0.001f)
                {
                    continue;
                }
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!found || bounds.size.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float aspect = TextureWidth / (float)TextureHeight;
            float halfHeight = Mathf.Max(
                bounds.extents.y,
                bounds.extents.x / Mathf.Max(0.01f, aspect));
            renderCamera.orthographicSize = Mathf.Max(0.1f, halfHeight * 1.12f);
            Vector3 position = renderCamera.transform.position;
            position.x = bounds.center.x;
            position.y = bounds.center.y;
            renderCamera.transform.position = position;
        }

        public bool TryGetWorldBounds(out Bounds bounds)
        {
            if (IsProductionReady &&
                renderSurfaceRect != null &&
                renderSurfaceRect.gameObject.activeInHierarchy)
            {
                renderSurfaceRect.GetWorldCorners(worldCorners);
                bounds = new Bounds(worldCorners[0], Vector3.zero);
                for (int i = 1; i < worldCorners.Length; i++)
                {
                    bounds.Encapsulate(worldCorners[i]);
                }
                return bounds.size.x > 2f && bounds.size.y > 2f;
            }

            if (flatFallbackController != null &&
                flatFallbackController.IsReady)
            {
                return flatFallbackController.TryGetWorldBounds(out bounds);
            }

            bounds = default;
            return false;
        }

        public bool FitToScreenHeight(float targetFraction)
        {
            if (!IsProductionReady)
            {
                return flatFallbackController != null &&
                       flatFallbackController.FitToScreenHeight(targetFraction);
            }

            if (renderSurfaceRect == null || Screen.height <= 1 ||
                !TryGetWorldBounds(out Bounds bounds))
            {
                return false;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null &&
                            canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector2 bottom = RectTransformUtility.WorldToScreenPoint(
                camera,
                bounds.min);
            Vector2 top = RectTransformUtility.WorldToScreenPoint(
                camera,
                bounds.max);
            float current = Mathf.Abs(top.y - bottom.y) / Screen.height;
            if (current <= 0.0001f)
            {
                return false;
            }

            float target = Mathf.Clamp(targetFraction, 0.08f, 0.82f);
            displayFitScale = Mathf.Clamp(
                displayFitScale * target / current,
                0.45f,
                2.4f);
            renderSurfaceRect.localScale = Vector3.one * displayFitScale;
            Canvas.ForceUpdateCanvases();
            return true;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        private void ClearProductionPipeline()
        {
            IsProductionReady = false;
            animatorParameters.Clear();
            productionAnimator = null;
            spriteSkins = null;
            spriteRenderers = null;

            if (productionInstance != null)
            {
                Destroy(productionInstance);
                productionInstance = null;
            }
            if (worldRoot != null)
            {
                Destroy(worldRoot);
                worldRoot = null;
            }
            if (renderSurfaceRect != null)
            {
                Destroy(renderSurfaceRect.gameObject);
                renderSurfaceRect = null;
                renderSurface = null;
            }
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }
            renderCamera = null;
        }

        private void OnDestroy()
        {
            ClearProductionPipeline();
        }
    }

    [DefaultExecutionOrder(-20000)]
    internal sealed class ProductionFatManRigBootstrap : MonoBehaviour
    {
        private static ProductionFatManRigBootstrap instance;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (instance != null)
            {
                return;
            }
            GameObject host = new("ProductionFatManRigBootstrap");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<ProductionFatManRigBootstrap>();
        }

        private void Update()
        {
            CharacterRigController[] rigs =
                Resources.FindObjectsOfTypeAll<CharacterRigController>();
            for (int i = 0; i < rigs.Length; i++)
            {
                CharacterRigController rig = rigs[i];
                if (rig == null ||
                    !rig.gameObject.scene.IsValid() ||
                    rig.GetComponent<ProductionFatManRigHost>() != null)
                {
                    continue;
                }
                rig.gameObject.AddComponent<ProductionFatManRigHost>();
            }
        }
    }
}
