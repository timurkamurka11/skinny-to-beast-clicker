using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Connects the independent generated fat-man bone rig to the existing
    /// gameplay state. The old rig remains only as a state source; all visible
    /// deformation is performed by GeneratedFatManRigActor's own bones.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(12000)]
    public sealed class GeneratedFatManRigHost : MonoBehaviour
    {
        private const int RenderLayer = 30;
        private const int TextureWidth = 768;
        private const int TextureHeight = 1280;
        private const float DisplayHeight = 1120f;
        private const string SurfaceName =
            "GeneratedFatMan.RenderSurface";
        private const string WorldName =
            "GeneratedFatMan.World";

        private readonly Vector3[] worldCorners = new Vector3[4];
        private CharacterRigController stateRig;
        private CharacterSkinController skinController;
        private CharacterSpriteRigController legacySprite;
        private RectTransform visualRoot;
        private RectTransform surfaceRect;
        private RawImage surfaceImage;
        private GameObject renderWorld;
        private Camera actorCamera;
        private RenderTexture actorTexture;
        private GeneratedFatManRigActor actor;
        private bool attempted;
        private bool ready;
        private float fitScale = 1f;
        private CharacterFacing framedFacing =
            (CharacterFacing)(-1);
        private int framedStage = -1;

        public bool IsReady =>
            ready &&
            actor != null &&
            actor.IsReady &&
            surfaceRect != null &&
            surfaceImage != null &&
            actorTexture != null &&
            actorTexture.IsCreated();

        public int BoneCount =>
            actor != null ? actor.BoneCount : 0;
        public int SkinnedSurfaceCount =>
            actor != null ? actor.SkinnedSurfaceCount : 0;
        public string ActiveMode =>
            IsReady ? "IndependentGeneratedBoneRig" : "Waiting";

        private void Awake()
        {
            CacheReferences();
            if (visualRoot != null)
            {
                HideLegacyVisuals();
            }
        }

        private void Update()
        {
            CacheReferences();
            if (!attempted)
            {
                TryInitialize();
            }

            if (!IsReady)
            {
                return;
            }

            CharacterFacing currentFacing = stateRig.Facing;
            int currentStage = Mathf.Clamp(
                skinController.CurrentArtIndex,
                0,
                3);
            actor.SetSignals(
                currentFacing,
                currentStage,
                stateRig.IsMoving,
                stateRig.IsTapReacting,
                stateRig.ActiveTapVariant,
                stateRig.ActiveAction);

            if (currentFacing != framedFacing ||
                currentStage != framedStage)
            {
                FrameActor();
                framedFacing = currentFacing;
                framedStage = currentStage;
            }
        }

        private void LateUpdate()
        {
            if (!IsReady)
            {
                return;
            }

            HideLegacyVisuals();
            if (surfaceRect != null)
            {
                surfaceRect.SetAsLastSibling();
            }
        }

        private void CacheReferences()
        {
            stateRig ??= GetComponent<CharacterRigController>();
            skinController ??=
                GetComponent<CharacterSkinController>();
            legacySprite ??=
                GetComponent<CharacterSpriteRigController>();
            visualRoot ??= stateRig != null
                ? stateRig.VisualRoot
                : null;
        }

        private void TryInitialize()
        {
            if (stateRig == null ||
                skinController == null ||
                stateRig.VisualRoot == null ||
                !stateRig.HasAppliedSkin)
            {
                return;
            }

            attempted = true;
            visualRoot = stateRig.VisualRoot;

            try
            {
                BuildRenderPipeline();
                ready = actor != null &&
                        actor.IsReady &&
                        actor.BoneCount >= 45 &&
                        actor.SkinnedSurfaceCount >= 45;
                if (!ready)
                {
                    throw new InvalidOperationException(
                        "The generated actor did not meet the minimum " +
                        "independent-bone/surface contract.");
                }

                if (legacySprite != null)
                {
                    legacySprite.enabled = false;
                }
                HideLegacyVisuals();
                FrameActor();
                framedFacing = stateRig.Facing;
                framedStage = Mathf.Clamp(
                    skinController.CurrentArtIndex,
                    0,
                    3);

                Debug.Log(
                    $"Generated Fat Man Bone Rig 3.8 active: " +
                    $"{actor.BoneCount} independent bones, " +
                    $"{actor.SkinnedSurfaceCount} skinned vector surfaces, " +
                    "Front/Side/Back, facial states and secondary motion.",
                    this);
            }
            catch (Exception exception)
            {
                ready = false;
                attempted = false;
                Debug.LogError(
                    $"Generated Fat Man Bone Rig 3.8 could not initialize: " +
                    exception,
                    this);
                ClearRuntimeObjects();
            }
        }

        private void BuildRenderPipeline()
        {
            ClearRuntimeObjects();

            actorTexture = new RenderTexture(
                TextureWidth,
                TextureHeight,
                24,
                RenderTextureFormat.ARGB32)
            {
                name = "GeneratedFatManRigRT",
                antiAliasing = 2,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            actorTexture.Create();

            GameObject surfaceObject = new GameObject(
                SurfaceName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            surfaceObject.layer = gameObject.layer;
            surfaceRect =
                surfaceObject.GetComponent<RectTransform>();
            surfaceRect.SetParent(visualRoot, false);
            surfaceRect.anchorMin = new Vector2(0.5f, 0.5f);
            surfaceRect.anchorMax = new Vector2(0.5f, 0.5f);
            surfaceRect.pivot = new Vector2(0.5f, 0.5f);
            surfaceRect.anchoredPosition =
                new Vector2(0f, -18f);
            surfaceRect.sizeDelta = new Vector2(
                DisplayHeight * TextureWidth / TextureHeight,
                DisplayHeight);
            surfaceRect.localScale = Vector3.one;
            surfaceRect.SetAsLastSibling();

            surfaceImage = surfaceObject.GetComponent<RawImage>();
            surfaceImage.texture = actorTexture;
            surfaceImage.color = Color.white;
            surfaceImage.raycastTarget = false;
            surfaceImage.maskable = false;

            renderWorld = new GameObject(WorldName);
            renderWorld.transform.position =
                new Vector3(12000f, 12000f, 0f);

            GameObject cameraObject =
                new GameObject("GeneratedFatMan.Camera");
            cameraObject.transform.SetParent(
                renderWorld.transform,
                false);
            actorCamera = cameraObject.AddComponent<Camera>();
            actorCamera.orthographic = true;
            actorCamera.orthographicSize = 5.0f;
            actorCamera.clearFlags = CameraClearFlags.SolidColor;
            actorCamera.backgroundColor =
                new Color(0f, 0f, 0f, 0f);
            actorCamera.cullingMask = 1 << RenderLayer;
            actorCamera.targetTexture = actorTexture;
            actorCamera.allowHDR = false;
            actorCamera.allowMSAA = true;
            actorCamera.depth = -100f;
            actorCamera.nearClipPlane = 0.01f;
            actorCamera.farClipPlane = 50f;

            GameObject actorObject =
                new GameObject("GeneratedFatMan.Actor");
            actorObject.transform.SetParent(
                renderWorld.transform,
                false);
            SetLayerRecursively(
                actorObject.transform,
                RenderLayer);
            actor = actorObject.AddComponent<
                GeneratedFatManRigActor>();
            actor.Build();
            SetLayerRecursively(
                actorObject.transform,
                RenderLayer);

            if (!actor.IsReady)
            {
                throw new InvalidOperationException(
                    "GeneratedFatManRigActor.Build returned an invalid rig.");
            }

            actor.SetSignals(
                stateRig.Facing,
                Mathf.Clamp(skinController.CurrentArtIndex, 0, 3),
                stateRig.IsMoving,
                stateRig.IsTapReacting,
                stateRig.ActiveTapVariant,
                stateRig.ActiveAction);
        }

        private void FrameActor()
        {
            if (actor == null || actorCamera == null)
            {
                return;
            }

            Bounds bounds = actor.CalculateVisibleBounds();
            float aspect = TextureWidth / (float)TextureHeight;
            float vertical = Mathf.Max(4.2f, bounds.extents.y * 1.16f);
            float horizontal =
                bounds.extents.x / Mathf.Max(0.1f, aspect) * 1.16f;
            actorCamera.orthographicSize =
                Mathf.Max(vertical, horizontal);
            actorCamera.transform.position = new Vector3(
                bounds.center.x,
                bounds.center.y + 0.08f,
                renderWorld.transform.position.z - 10f);
            actorCamera.transform.rotation =
                Quaternion.identity;
        }

        private void HideLegacyVisuals()
        {
            if (visualRoot == null)
            {
                return;
            }

            if (legacySprite != null)
            {
                legacySprite.enabled = false;
            }

            Behaviour[] behaviours =
                GetComponents<Behaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour behaviour = behaviours[i];
                if (behaviour == null ||
                    behaviour == this ||
                    behaviour == stateRig ||
                    behaviour == skinController)
                {
                    continue;
                }

                string typeName =
                    behaviour.GetType().Name;
                if (typeName == "CharacterLayeredRigController" ||
                    typeName == "CharacterSkinnedSpriteGraphic")
                {
                    behaviour.enabled = false;
                }
            }

            CharacterMeshGraphic[] meshes =
                visualRoot.GetComponentsInChildren<
                    CharacterMeshGraphic>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                CharacterMeshGraphic mesh = meshes[i];
                if (mesh == null)
                {
                    continue;
                }

                if (mesh.canvasRenderer != null)
                {
                    mesh.canvasRenderer.SetAlpha(0f);
                }

                Graphic[] children =
                    mesh.GetComponentsInChildren<Graphic>(true);
                for (int child = 0;
                     child < children.Length;
                     child++)
                {
                    Graphic graphic = children[child];
                    if (graphic != null &&
                        graphic.canvasRenderer != null)
                    {
                        graphic.canvasRenderer.SetAlpha(0f);
                    }
                }
            }

            Graphic[] graphics =
                visualRoot.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null ||
                    graphic == surfaceImage ||
                    graphic.transform.IsChildOf(surfaceRect))
                {
                    continue;
                }

                string name = graphic.gameObject.name;
                bool obsolete =
                    name.StartsWith(
                        "Sprite.RealFatMan",
                        StringComparison.Ordinal) ||
                    name.StartsWith(
                        "LayeredFace.",
                        StringComparison.Ordinal) ||
                    name.StartsWith(
                        "SpriteFace.",
                        StringComparison.Ordinal) ||
                    name == "VisibleFill" ||
                    name == "VisibleOutline";
                if (obsolete && graphic.canvasRenderer != null)
                {
                    graphic.canvasRenderer.SetAlpha(0f);
                }
            }

            Transform broken =
                visualRoot.Find("RealFatMan.LayeredArt3_6");
            if (broken != null)
            {
                broken.gameObject.SetActive(false);
            }
        }

        public bool TryGetWorldBounds(out Bounds bounds)
        {
            bounds = default;
            if (!IsReady ||
                !surfaceRect.gameObject.activeInHierarchy ||
                surfaceImage.color.a <= 0.001f)
            {
                return false;
            }

            surfaceRect.GetWorldCorners(worldCorners);
            bounds = new Bounds(worldCorners[0], Vector3.zero);
            for (int i = 1; i < worldCorners.Length; i++)
            {
                bounds.Encapsulate(worldCorners[i]);
            }
            return bounds.size.x > 2f &&
                   bounds.size.y > 2f;
        }

        public bool TryGetScreenHeightFraction(
            out float fraction)
        {
            fraction = 0f;
            if (!TryGetWorldBounds(out Bounds bounds) ||
                Screen.width <= 1 ||
                Screen.height <= 1)
            {
                return false;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null &&
                            canvas.renderMode !=
                            RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector2 screenMin =
                RectTransformUtility.WorldToScreenPoint(
                    camera,
                    bounds.min);
            Vector2 screenMax =
                RectTransformUtility.WorldToScreenPoint(
                    camera,
                    bounds.max);
            fraction =
                Mathf.Abs(screenMax.y - screenMin.y) /
                Screen.height;
            return fraction > 0.001f;
        }

        public bool FitToScreenHeight(float targetFraction)
        {
            if (!TryGetScreenHeightFraction(
                    out float currentFraction) ||
                currentFraction <= 0.001f)
            {
                return false;
            }

            float target = Mathf.Clamp(
                targetFraction,
                0.08f,
                0.82f);
            float ratio = target / currentFraction;
            if (Mathf.Abs(1f - ratio) < 0.015f)
            {
                return true;
            }

            fitScale = Mathf.Clamp(
                fitScale * ratio,
                0.45f,
                2.4f);
            surfaceRect.localScale =
                Vector3.one * fitScale;
            Canvas.ForceUpdateCanvases();
            return true;
        }

        private static void SetLayerRecursively(
            Transform root,
            int layer)
        {
            if (root == null)
            {
                return;
            }

            root.gameObject.layer = layer;
            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursively(root.GetChild(i), layer);
            }
        }

        private void ClearRuntimeObjects()
        {
            if (surfaceRect != null)
            {
                DestroyObject(surfaceRect.gameObject);
            }
            if (renderWorld != null)
            {
                DestroyObject(renderWorld);
            }
            if (actorTexture != null)
            {
                actorTexture.Release();
                DestroyObject(actorTexture);
            }

            surfaceRect = null;
            surfaceImage = null;
            renderWorld = null;
            actorCamera = null;
            actorTexture = null;
            actor = null;
            ready = false;
        }

        private static void DestroyObject(
            UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }

        private void OnDestroy()
        {
            ClearRuntimeObjects();
        }
    }

    /// <summary>
    /// Adds the generated rig to every runtime character before its visibility
    /// gate performs the first stable-frame check.
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    internal sealed class GeneratedFatManRigBootstrap : MonoBehaviour
    {
        private static GeneratedFatManRigBootstrap instance;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (instance != null)
            {
                return;
            }

            GameObject host =
                new GameObject("GeneratedFatManRig.Bootstrap");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<
                GeneratedFatManRigBootstrap>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            CharacterRigController[] rigs =
                Resources.FindObjectsOfTypeAll<
                    CharacterRigController>();
            for (int i = 0; i < rigs.Length; i++)
            {
                CharacterRigController rig = rigs[i];
                if (rig == null ||
                    !rig.gameObject.scene.IsValid() ||
                    rig.GetComponent<GeneratedFatManRigHost>() != null)
                {
                    continue;
                }

                rig.gameObject.AddComponent<
                    GeneratedFatManRigHost>();
            }
        }
    }
}
