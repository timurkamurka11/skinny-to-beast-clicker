using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Hosts a genuinely authored Unity 2D Animation character.
    /// The final prefab must come from layered art with SpriteSkin bones and
    /// manual weights. It is rendered through an isolated transparent camera,
    /// so the old procedural mannequin is never used as its deformation rig.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(22000)]
    public sealed class ProductionFatManRigHost : MonoBehaviour
    {
        public const string ProductionPrefabResourcePath =
            "Characters/FatManProduction/FatManProductionRig";

        private const string BrokenLayeredRootName =
            "RealFatMan.LayeredArt3_6";
        private const int RenderLayer = 31;
        private const int TextureWidth = 768;
        private const int TextureHeight = 1280;
        private const float DisplayHeight = 1120f;

        private static readonly int FacingHash =
            Animator.StringToHash("Facing");
        private static readonly int StageHash =
            Animator.StringToHash("Stage");
        private static readonly int SpeedHash =
            Animator.StringToHash("Speed");
        private static readonly int TapHash =
            Animator.StringToHash("Tap");
        private static readonly int ActionHash =
            Animator.StringToHash("Action");

        private readonly Dictionary<int, AnimatorControllerParameterType>
            animatorParameters = new();
        private readonly Vector3[] worldCorners = new Vector3[4];

        private CharacterRigController legacyRig;
        private CharacterSkinController skinController;
        private CharacterLayeredRigController brokenLayered;
        private CharacterSpriteRigController safeFallback;
        private RectTransform visualRoot;
        private RectTransform surfaceRect;
        private RawImage surfaceImage;
        private GameObject renderWorld;
        private GameObject actor;
        private Camera actorCamera;
        private RenderTexture actorTexture;
        private Animator actorAnimator;
        private SpriteSkin[] spriteSkins;
        private SpriteRenderer[] spriteRenderers;
        private float fitScale = 1f;
        private int lastFacing = int.MinValue;
        private int lastStage = int.MinValue;
        private bool attempted;
        private bool missingLogged;
        private bool invalidLogged;

        public bool IsProductionReady { get; private set; }
        public bool IsFallbackReady =>
            !IsProductionReady &&
            safeFallback != null &&
            safeFallback.IsReady;
        public bool IsReady => IsProductionReady || IsFallbackReady;
        public bool RequiresAuthoredAsset => !IsProductionReady;
        public string ActiveMode => IsProductionReady
            ? "ProductionSpriteSkin"
            : IsFallbackReady
                ? "TemporaryWholeBodyFallback"
                : "Waiting";

        private void Awake()
        {
            CacheReferences();
        }

        private void Update()
        {
            CacheReferences();
            if (!attempted)
            {
                TryInitialize();
            }

            if (IsProductionReady)
            {
                SyncAnimator();
                FrameActor();
            }
            else
            {
                EnableSafeFallback();
            }
        }

        private void LateUpdate()
        {
            DisableBrokenCutouts();
            HideProceduralVisuals(IsProductionReady);
            if (IsProductionReady && surfaceRect != null)
            {
                surfaceRect.SetAsLastSibling();
            }
        }

        private void CacheReferences()
        {
            legacyRig ??= GetComponent<CharacterRigController>();
            skinController ??= GetComponent<CharacterSkinController>();
            brokenLayered ??=
                GetComponent<CharacterLayeredRigController>();
            safeFallback ??=
                GetComponent<CharacterSpriteRigController>();
            visualRoot ??= legacyRig != null
                ? legacyRig.VisualRoot
                : null;
        }

        private void TryInitialize()
        {
            if (legacyRig == null ||
                skinController == null ||
                legacyRig.VisualRoot == null ||
                !legacyRig.HasAppliedSkin)
            {
                return;
            }

            attempted = true;
            visualRoot = legacyRig.VisualRoot;
            DisableBrokenCutouts();

            GameObject prefab =
                Resources.Load<GameObject>(ProductionPrefabResourcePath);
            if (prefab == null)
            {
                EnableSafeFallback();
                LogMissingOnce();
                return;
            }

            if (!BuildRenderPipeline(prefab))
            {
                EnableSafeFallback();
                LogInvalidOnce();
                return;
            }

            IsProductionReady = true;
            missingLogged = false;
            invalidLogged = false;
            if (safeFallback != null)
            {
                safeFallback.enabled = false;
            }
            HideProceduralVisuals(true);
            Debug.Log(
                $"Production fat-man rig active: {spriteSkins.Length} " +
                "SpriteSkin surfaces with independent authored bones and weights.",
                this);
        }

        private bool BuildRenderPipeline(GameObject prefab)
        {
            ClearProductionObjects();

            actorTexture = new RenderTexture(
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
            actorTexture.Create();

            GameObject surface = new(
                "ProductionFatMan.RenderSurface",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            surface.layer = gameObject.layer;
            surfaceRect = surface.GetComponent<RectTransform>();
            surfaceRect.SetParent(visualRoot, false);
            surfaceRect.anchorMin = new Vector2(0.5f, 0.5f);
            surfaceRect.anchorMax = new Vector2(0.5f, 0.5f);
            surfaceRect.pivot = new Vector2(0.5f, 0.5f);
            surfaceRect.anchoredPosition = new Vector2(0f, -18f);
            surfaceRect.sizeDelta = new Vector2(
                DisplayHeight * TextureWidth / TextureHeight,
                DisplayHeight);
            surfaceRect.localScale = Vector3.one;
            surfaceRect.SetAsLastSibling();

            surfaceImage = surface.GetComponent<RawImage>();
            surfaceImage.texture = actorTexture;
            surfaceImage.color = Color.white;
            surfaceImage.raycastTarget = false;
            surfaceImage.maskable = false;

            renderWorld = new GameObject("ProductionFatMan.WorldRig");
            renderWorld.transform.position =
                new Vector3(20000f, 20000f, 0f);

            GameObject cameraObject = new("ProductionFatMan.Camera");
            cameraObject.transform.SetParent(renderWorld.transform, false);
            cameraObject.transform.localPosition =
                new Vector3(0f, 0f, -10f);
            actorCamera = cameraObject.AddComponent<Camera>();
            actorCamera.orthographic = true;
            actorCamera.orthographicSize = 5f;
            actorCamera.clearFlags = CameraClearFlags.SolidColor;
            actorCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            actorCamera.cullingMask = 1 << RenderLayer;
            actorCamera.targetTexture = actorTexture;
            actorCamera.allowHDR = false;
            actorCamera.allowMSAA = true;
            actorCamera.depth = -100f;

            actor = Instantiate(prefab, renderWorld.transform, false);
            actor.name = "FatManProductionRig.Runtime";
            actor.transform.localPosition = Vector3.zero;
            actor.transform.localRotation = Quaternion.identity;
            SetLayerRecursively(actor.transform, RenderLayer);

            actorAnimator = actor.GetComponentInChildren<Animator>(true);
            spriteSkins = actor.GetComponentsInChildren<SpriteSkin>(true);
            spriteRenderers =
                actor.GetComponentsInChildren<SpriteRenderer>(true);

            if (actorAnimator == null ||
                spriteSkins == null || spriteSkins.Length == 0 ||
                spriteRenderers == null || spriteRenderers.Length == 0)
            {
                ClearProductionObjects();
                return false;
            }

            int validSkins = 0;
            for (int i = 0; i < spriteSkins.Length; i++)
            {
                SpriteSkin spriteSkin = spriteSkins[i];
                if (spriteSkin != null &&
                    spriteSkin.GetComponent<SpriteRenderer>() != null &&
                    spriteSkin.boneTransforms != null &&
                    spriteSkin.boneTransforms.Length > 0)
                {
                    spriteSkin.alwaysUpdate = true;
                    validSkins++;
                }
            }
            if (validSkins == 0)
            {
                ClearProductionObjects();
                return false;
            }

            animatorParameters.Clear();
            AnimatorControllerParameter[] parameters = actorAnimator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                animatorParameters[parameters[i].nameHash] = parameters[i].type;
            }

            FrameActor(true);
            SyncAnimator(true);
            return true;
        }

        private void EnableSafeFallback()
        {
            IsProductionReady = false;
            DisableBrokenCutouts();
            if (safeFallback != null)
            {
                safeFallback.enabled = true;
            }
            if (surfaceRect != null)
            {
                surfaceRect.gameObject.SetActive(false);
            }
        }

        private void DisableBrokenCutouts()
        {
            if (brokenLayered != null)
            {
                brokenLayered.enabled = false;
            }
            if (visualRoot == null)
            {
                return;
            }
            Transform cutoutRoot = visualRoot.Find(BrokenLayeredRootName);
            if (cutoutRoot != null)
            {
                cutoutRoot.gameObject.SetActive(false);
            }
        }

        private void HideProceduralVisuals(bool hideWholeBodyFallback)
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
                Graphic[] children = mesh.GetComponentsInChildren<Graphic>(true);
                for (int child = 0; child < children.Length; child++)
                {
                    if (children[child] != null)
                    {
                        children[child].canvasRenderer.SetAlpha(0f);
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

                string name = image.gameObject.name;
                bool oldFace =
                    name.StartsWith("LayeredFace.", StringComparison.Ordinal) ||
                    name.StartsWith("SpriteFace.", StringComparison.Ordinal);
                bool wholeBody =
                    name.StartsWith("Sprite.RealFatMan", StringComparison.Ordinal);
                if (oldFace ||
                    name == "VisibleFill" ||
                    name == "VisibleOutline" ||
                    (hideWholeBodyFallback && wholeBody))
                {
                    image.canvasRenderer.SetAlpha(0f);
                }
            }
        }

        private void SyncAnimator(bool force = false)
        {
            if (actorAnimator == null || legacyRig == null)
            {
                return;
            }

            int facing = (int)legacyRig.Facing;
            int stage = skinController != null
                ? Mathf.Max(0, skinController.CurrentArtIndex)
                : 0;
            if (force || facing != lastFacing)
            {
                SetInt(FacingHash, facing);
                lastFacing = facing;
            }
            if (force || stage != lastStage)
            {
                SetInt(StageHash, stage);
                lastStage = stage;
            }

            SetFloat(SpeedHash, legacyRig.IsMoving ? 1f : 0f);
            SetBool(TapHash, legacyRig.IsTapReacting);
            SetInt(ActionHash, (int)legacyRig.ActiveAction);
        }

        private void SetInt(int hash, int value)
        {
            if (animatorParameters.TryGetValue(
                    hash,
                    out AnimatorControllerParameterType type) &&
                type == AnimatorControllerParameterType.Int)
            {
                actorAnimator.SetInteger(hash, value);
            }
        }

        private void SetFloat(int hash, float value)
        {
            if (animatorParameters.TryGetValue(
                    hash,
                    out AnimatorControllerParameterType type) &&
                type == AnimatorControllerParameterType.Float)
            {
                actorAnimator.SetFloat(hash, value);
            }
        }

        private void SetBool(int hash, bool value)
        {
            if (animatorParameters.TryGetValue(
                    hash,
                    out AnimatorControllerParameterType type) &&
                type == AnimatorControllerParameterType.Bool)
            {
                actorAnimator.SetBool(hash, value);
            }
        }

        private void FrameActor(bool force = false)
        {
            if ((!IsProductionReady && !force) ||
                actorCamera == null ||
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
            actorCamera.orthographicSize =
                Mathf.Max(0.1f, halfHeight * 1.12f);
            Vector3 position = actorCamera.transform.position;
            position.x = bounds.center.x;
            position.y = bounds.center.y;
            actorCamera.transform.position = position;
        }

        public bool TryGetWorldBounds(out Bounds bounds)
        {
            if (IsProductionReady &&
                surfaceRect != null &&
                surfaceRect.gameObject.activeInHierarchy)
            {
                surfaceRect.GetWorldCorners(worldCorners);
                bounds = new Bounds(worldCorners[0], Vector3.zero);
                for (int i = 1; i < worldCorners.Length; i++)
                {
                    bounds.Encapsulate(worldCorners[i]);
                }
                return bounds.size.x > 2f && bounds.size.y > 2f;
            }

            if (safeFallback != null && safeFallback.IsReady)
            {
                return safeFallback.TryGetWorldBounds(out bounds);
            }

            bounds = default;
            return false;
        }

        public bool FitToScreenHeight(float targetFraction)
        {
            if (!IsProductionReady)
            {
                return safeFallback != null &&
                       safeFallback.FitToScreenHeight(targetFraction);
            }

            if (surfaceRect == null ||
                Screen.height <= 1 ||
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
            fitScale = Mathf.Clamp(
                fitScale * target / current,
                0.45f,
                2.4f);
            surfaceRect.localScale = Vector3.one * fitScale;
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

        private void LogMissingOnce()
        {
            if (missingLogged)
            {
                return;
            }
            missingLogged = true;
            Debug.LogWarning(
                "Production fat-man rig is not installed. Expected " +
                "Assets/Resources/Characters/FatManProduction/" +
                "FatManProductionRig.prefab. A clean whole-body fallback is " +
                "active; Patch 3.6 cut-outs are disabled.",
                this);
        }

        private void LogInvalidOnce()
        {
            if (invalidLogged)
            {
                return;
            }
            invalidLogged = true;
            Debug.LogError(
                "FatManProductionRig.prefab exists but lacks a valid Animator, " +
                "SpriteSkin boneTransforms or SpriteRenderer surfaces.",
                this);
        }

        private void ClearProductionObjects()
        {
            IsProductionReady = false;
            animatorParameters.Clear();
            actorAnimator = null;
            spriteSkins = null;
            spriteRenderers = null;

            if (actor != null)
            {
                Destroy(actor);
                actor = null;
            }
            if (renderWorld != null)
            {
                Destroy(renderWorld);
                renderWorld = null;
            }
            if (surfaceRect != null)
            {
                Destroy(surfaceRect.gameObject);
                surfaceRect = null;
                surfaceImage = null;
            }
            if (actorTexture != null)
            {
                actorTexture.Release();
                Destroy(actorTexture);
                actorTexture = null;
            }
            actorCamera = null;
        }

        private void OnDestroy()
        {
            ClearProductionObjects();
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
