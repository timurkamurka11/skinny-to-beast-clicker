using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Patch 3.6 renders genuine transparent body-part textures. The legacy
    /// skeleton supplies animation signals only; art-specific proxy bones keep
    /// shoulders, hips, elbows, knees and the face aligned to the painted man.
    /// No whole-body Image or skinned full-PNG mesh is rendered.
    /// </summary>
    [DefaultExecutionOrder(1100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterRigController))]
    [RequireComponent(typeof(CharacterSkinController))]
    public sealed class CharacterLayeredRigController : MonoBehaviour
    {
        private const string ManifestResource =
            "Characters/FatManLayered/Generated/manifest";
        private const string LayeredRootName = "RealFatMan.LayeredArt3_6";
        private const float MinimumScreenFraction = 0.08f;
        private const float MaximumScreenFraction = 0.90f;

        [Serializable]
        private sealed class Manifest
        {
            public int version;
            public float displayHeight = 1080f;
            public float bodyOffsetX;
            public float bodyOffsetY = -28f;
            public StageProfile[] stages;
            public ViewProfile[] views;
        }

        [Serializable]
        private sealed class StageProfile
        {
            public int index;
            public float scale = 1f;
        }

        [Serializable]
        private sealed class ViewProfile
        {
            public string name;
            public int width;
            public int height;
            public PartProfile[] parts;
        }

        [Serializable]
        private sealed class PartProfile
        {
            public string name;
            public string resource;
            public string driver;
            public string parent;
            public float anchorX;
            public float anchorY;
            public float pivotX = 0.5f;
            public float pivotY = 0.5f;
            public int cropWidth;
            public int cropHeight;
            public int sort;
            public float rotationGain;
            public float maxRotation;
            public float translationGain;
            public float maxTranslation;
            public float scaleGain;
            public float maxScaleDelta;
            public string faceGroup;
            public string faceState;
        }

        private sealed class PartBinding
        {
            public PartProfile profile;
            public RectTransform proxyBone;
            public RectTransform imageRect;
            public Image image;
            public Sprite runtimeSprite;
            public RectTransform sourceBone;
            public Vector2 baseLocalPosition;
            public Vector3 baseLocalScale;
            public Vector2 sourceBindPosition;
            public Vector2 sourceBindScale;
            public float sourceBindAngle;
        }

        private sealed class ViewInstance
        {
            public string name;
            public ViewProfile profile;
            public RectTransform root;
            public float displayWidth;
            public float displayHeight;
            public readonly List<PartBinding> bindings = new();
            public readonly Dictionary<string, PartBinding> parts = new();
            public readonly Dictionary<string, Dictionary<string, Image>>
                face = new();
        }

        private readonly Dictionary<string, ViewInstance> views = new();
        private readonly List<Sprite> runtimeSprites = new();
        private readonly Vector3[] worldCorners = new Vector3[4];

        private CharacterRigController rigController;
        private CharacterSkinController skinController;
        private CharacterSpriteRigController flatSpriteController;
        private RectTransform visualRoot;
        private RectTransform layeredRoot;
        private Manifest manifest;
        private ViewInstance activeView;
        private CharacterFacing observedFacing = (CharacterFacing)(-1);
        private int observedStage = -1;
        private float fitScale = 1f;
        private float nextBlinkAt;
        private float blinkUntil;
        private bool failureLogged;
        private bool ready;

        public bool IsReady =>
            ready && activeView != null && layeredRoot != null;
        public bool UsesNativeSideProfile => true;
        public int VisiblePartCount =>
            activeView != null ? activeView.bindings.Count : 0;
        public float DeformationMagnitude { get; private set; }

        private void Awake()
        {
            rigController = GetComponent<CharacterRigController>();
            skinController = GetComponent<CharacterSkinController>();
            flatSpriteController = GetComponent<CharacterSpriteRigController>();
        }

        private void Update()
        {
            if (!ready)
            {
                TryBuild();
            }
        }

        private void LateUpdate()
        {
            if (!ready)
            {
                return;
            }

            SuppressLegacyVisuals();
            SyncFacingAndStage();
            DriveActiveView();
            UpdateFace();
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

            TextAsset manifestAsset =
                Resources.Load<TextAsset>(ManifestResource);
            if (manifestAsset == null)
            {
                LogFailureOnce(
                    "Patch 3.6 layered-art manifest is missing. Run the " +
                    "layered-art generator or pull the generated assets.");
                return;
            }

            try
            {
                manifest = JsonUtility.FromJson<Manifest>(manifestAsset.text);
            }
            catch (Exception exception)
            {
                LogFailureOnce(
                    "Patch 3.6 could not parse layered-art manifest: " +
                    exception.Message);
                return;
            }

            if (manifest == null ||
                manifest.version < 36 ||
                manifest.views == null ||
                manifest.views.Length < 3)
            {
                LogFailureOnce(
                    "Patch 3.6 layered-art manifest is incomplete.");
                return;
            }

            visualRoot = rigController.VisualRoot;
            ClearLayeredRuntime();
            layeredRoot = CreateRect(
                visualRoot,
                LayeredRootName,
                new Vector2(manifest.bodyOffsetX, manifest.bodyOffsetY),
                visualRoot.rect.size);
            layeredRoot.SetAsLastSibling();

            for (int i = 0; i < manifest.views.Length; i++)
            {
                if (!BuildView(manifest.views[i]))
                {
                    ClearLayeredRuntime();
                    return;
                }
            }

            if (!views.ContainsKey("Front") ||
                !views.ContainsKey("Side") ||
                !views.ContainsKey("Back"))
            {
                LogFailureOnce(
                    "Patch 3.6 requires Front, Side and Back layered sets.");
                ClearLayeredRuntime();
                return;
            }

            ready = true;
            failureLogged = false;
            fitScale = 1f;
            observedFacing = (CharacterFacing)(-1);
            observedStage = -1;
            ScheduleBlink();
            SyncFacingAndStage();
            SuppressLegacyVisuals();

            Debug.Log(
                "Real Fat Man Layered Art Patch 3.6 active: separate PNG " +
                "pelvis, torso, belly, head, arms, legs, feet and facial " +
                "states are driven by art-specific proxy bones.",
                this);
        }

        private bool BuildView(ViewProfile profile)
        {
            if (profile == null ||
                string.IsNullOrWhiteSpace(profile.name) ||
                profile.width < 2 ||
                profile.height < 2 ||
                profile.parts == null ||
                profile.parts.Length < 16)
            {
                LogFailureOnce("A Patch 3.6 view profile is invalid.");
                return false;
            }

            float displayHeight = Mathf.Clamp(
                manifest.displayHeight,
                820f,
                1260f);
            float displayWidth =
                displayHeight * profile.width / profile.height;
            RectTransform viewRoot = CreateRect(
                layeredRoot,
                "View." + profile.name,
                Vector2.zero,
                new Vector2(displayWidth, displayHeight));

            ViewInstance instance = new()
            {
                name = profile.name,
                profile = profile,
                root = viewRoot,
                displayWidth = displayWidth,
                displayHeight = displayHeight
            };

            Dictionary<string, Vector2> globalAnchors = new();
            for (int i = 0; i < profile.parts.Length; i++)
            {
                PartProfile part = profile.parts[i];
                if (part == null ||
                    string.IsNullOrWhiteSpace(part.name) ||
                    string.IsNullOrWhiteSpace(part.resource))
                {
                    continue;
                }

                Texture2D texture = Resources.Load<Texture2D>(part.resource);
                if (texture == null)
                {
                    LogFailureOnce(
                        $"Patch 3.6 texture is missing: {part.resource}");
                    Destroy(viewRoot.gameObject);
                    return false;
                }

                RectTransform proxy = CreateRect(
                    viewRoot,
                    "ArtBone." + part.name,
                    Vector2.zero,
                    Vector2.zero);
                proxy.pivot = new Vector2(0.5f, 0.5f);

                GameObject imageObject = new(
                    "Layer." + part.name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Canvas),
                    typeof(Image));
                imageObject.layer = gameObject.layer;
                RectTransform imageRect =
                    imageObject.GetComponent<RectTransform>();
                imageRect.SetParent(proxy, false);
                imageRect.anchorMin = new Vector2(0.5f, 0.5f);
                imageRect.anchorMax = new Vector2(0.5f, 0.5f);
                imageRect.pivot = new Vector2(part.pivotX, part.pivotY);
                imageRect.anchoredPosition = Vector2.zero;
                imageRect.localRotation = Quaternion.identity;
                imageRect.localScale = Vector3.one;
                imageRect.sizeDelta = new Vector2(
                    part.cropWidth / (float)profile.width * displayWidth,
                    part.cropHeight / (float)profile.height * displayHeight);

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);
                sprite.name =
                    $"FatMan3_6.{profile.name}.{part.name}";
                runtimeSprites.Add(sprite);

                Image image = imageObject.GetComponent<Image>();
                image.sprite = sprite;
                image.color = Color.white;
                image.raycastTarget = false;
                image.maskable = false;
                image.preserveAspect = false;
                image.type = Image.Type.Simple;

                Canvas canvas = imageObject.GetComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 500 + part.sort;

                RectTransform sourceBone =
                    !string.IsNullOrWhiteSpace(part.driver)
                        ? rigController.GetBone(part.driver)
                        : null;
                PartBinding binding = new()
                {
                    profile = part,
                    proxyBone = proxy,
                    imageRect = imageRect,
                    image = image,
                    runtimeSprite = sprite,
                    sourceBone = sourceBone
                };
                instance.bindings.Add(binding);
                instance.parts[part.name] = binding;
                globalAnchors[part.name] = new Vector2(
                    (part.anchorX - 0.5f) * displayWidth,
                    (part.anchorY - 0.5f) * displayHeight);

                if (!string.IsNullOrWhiteSpace(part.faceGroup))
                {
                    if (!instance.face.TryGetValue(
                            part.faceGroup,
                            out Dictionary<string, Image> states))
                    {
                        states = new Dictionary<string, Image>(
                            StringComparer.OrdinalIgnoreCase);
                        instance.face.Add(part.faceGroup, states);
                    }
                    states[part.faceState] = image;
                }
            }

            for (int i = 0; i < instance.bindings.Count; i++)
            {
                PartBinding binding = instance.bindings[i];
                string parentName = binding.profile.parent;
                RectTransform parent = viewRoot;
                Vector2 parentGlobal = Vector2.zero;
                if (!string.IsNullOrWhiteSpace(parentName) &&
                    instance.parts.TryGetValue(
                        parentName,
                        out PartBinding parentBinding))
                {
                    parent = parentBinding.proxyBone;
                    parentGlobal = globalAnchors[parentName];
                }

                Vector2 global = globalAnchors[binding.profile.name];
                binding.proxyBone.SetParent(parent, false);
                binding.proxyBone.anchoredPosition = global - parentGlobal;
                binding.proxyBone.localRotation = Quaternion.identity;
                binding.proxyBone.localScale = Vector3.one;
                binding.baseLocalPosition =
                    binding.proxyBone.anchoredPosition;
                binding.baseLocalScale = Vector3.one;
                CaptureSourceBind(binding);
            }

            viewRoot.gameObject.SetActive(false);
            views.Add(profile.name, instance);
            SetDefaultFaceState(instance);
            return true;
        }

        private static RectTransform CreateRect(
            Transform parent,
            string objectName,
            Vector2 position,
            Vector2 size)
        {
            GameObject target = new(objectName, typeof(RectTransform));
            target.layer = parent.gameObject.layer;
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            return rect;
        }

        private void CaptureSourceBind(PartBinding binding)
        {
            if (binding.sourceBone == null)
            {
                binding.sourceBindPosition = Vector2.zero;
                binding.sourceBindScale = Vector2.one;
                binding.sourceBindAngle = 0f;
                return;
            }

            binding.sourceBindPosition =
                binding.sourceBone.anchoredPosition;
            binding.sourceBindScale = new Vector2(
                Mathf.Abs(binding.sourceBone.localScale.x),
                Mathf.Abs(binding.sourceBone.localScale.y));
            binding.sourceBindAngle =
                binding.sourceBone.localEulerAngles.z;
        }

        private void SyncFacingAndStage()
        {
            CharacterFacing facing = rigController.Facing;
            int stage = Mathf.Max(0, skinController.CurrentArtIndex);
            if (facing != observedFacing)
            {
                observedFacing = facing;
                string name = facing == CharacterFacing.Back
                    ? "Back"
                    : facing == CharacterFacing.SideLeft ||
                      facing == CharacterFacing.SideRight
                        ? "Side"
                        : "Front";
                foreach (ViewInstance view in views.Values)
                {
                    view.root.gameObject.SetActive(view.name == name);
                }
                activeView = views[name];
                SetDefaultFaceState(activeView);
                ScheduleBlink();
            }

            if (stage != observedStage)
            {
                observedStage = stage;
            }
            ApplyCombinedScale();
        }

        private void ApplyCombinedScale()
        {
            if (layeredRoot == null)
            {
                return;
            }
            float stageScale = GetStageScale(observedStage);
            float combined = Mathf.Clamp(stageScale * fitScale, 0.55f, 2.4f);
            layeredRoot.localScale =
                new Vector3(combined, combined, 1f);
        }

        private float GetStageScale(int stage)
        {
            if (manifest?.stages == null ||
                manifest.stages.Length == 0)
            {
                return 1f;
            }
            for (int i = 0; i < manifest.stages.Length; i++)
            {
                if (manifest.stages[i].index == stage)
                {
                    return Mathf.Clamp(
                        manifest.stages[i].scale,
                        0.8f,
                        1.2f);
                }
            }
            return 1f;
        }

        private void DriveActiveView()
        {
            if (activeView == null)
            {
                return;
            }

            float totalMotion = 0f;
            int samples = 0;
            for (int i = 0; i < activeView.bindings.Count; i++)
            {
                PartBinding binding = activeView.bindings[i];
                if (binding.sourceBone == null ||
                    !string.IsNullOrWhiteSpace(binding.profile.faceGroup))
                {
                    continue;
                }

                Vector2 sourcePosition =
                    binding.sourceBone.anchoredPosition;
                Vector2 translation =
                    (sourcePosition - binding.sourceBindPosition) *
                    binding.profile.translationGain;
                translation = Vector2.ClampMagnitude(
                    translation,
                    Mathf.Max(0f, binding.profile.maxTranslation));

                float sourceAngle =
                    binding.sourceBone.localEulerAngles.z;
                float angle = Mathf.Clamp(
                    Mathf.DeltaAngle(
                        binding.sourceBindAngle,
                        sourceAngle) *
                    binding.profile.rotationGain,
                    -binding.profile.maxRotation,
                    binding.profile.maxRotation);

                Vector2 sourceScale = new(
                    Mathf.Abs(binding.sourceBone.localScale.x),
                    Mathf.Abs(binding.sourceBone.localScale.y));
                Vector2 scaleRatio = new(
                    binding.sourceBindScale.x > 0.0001f
                        ? sourceScale.x / binding.sourceBindScale.x
                        : 1f,
                    binding.sourceBindScale.y > 0.0001f
                        ? sourceScale.y / binding.sourceBindScale.y
                        : 1f);
                Vector2 scaleDelta =
                    (scaleRatio - Vector2.one) * binding.profile.scaleGain;
                float scaleLimit =
                    Mathf.Max(0f, binding.profile.maxScaleDelta);
                scaleDelta.x = Mathf.Clamp(
                    scaleDelta.x,
                    -scaleLimit,
                    scaleLimit);
                scaleDelta.y = Mathf.Clamp(
                    scaleDelta.y,
                    -scaleLimit,
                    scaleLimit);

                binding.proxyBone.anchoredPosition =
                    binding.baseLocalPosition + translation;
                binding.proxyBone.localRotation =
                    Quaternion.Euler(0f, 0f, angle);
                binding.proxyBone.localScale = new Vector3(
                    binding.baseLocalScale.x * (1f + scaleDelta.x),
                    binding.baseLocalScale.y * (1f + scaleDelta.y),
                    1f);
                totalMotion += translation.magnitude + Mathf.Abs(angle) * 0.15f;
                samples++;
            }

            DeformationMagnitude =
                samples > 0 ? totalMotion / samples : 0f;
        }

        private void UpdateFace()
        {
            if (activeView == null ||
                activeView.name == "Back" ||
                activeView.face.Count == 0)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now >= nextBlinkAt)
            {
                blinkUntil = now + 0.13f;
                ScheduleBlink();
            }
            bool blink = now < blinkUntil;
            SetFaceGroup(activeView, "EyeL", blink ? "Closed" : "Open");
            SetFaceGroup(activeView, "EyeR", blink ? "Closed" : "Open");
            SetFaceGroup(activeView, "Eye", blink ? "Closed" : "Open");

            string mouth = "Neutral";
            if (rigController.ActiveAction == CharacterRoutineAction.Yawn)
            {
                mouth = "Yawn";
            }
            else if (rigController.IsTapReacting)
            {
                mouth = "Open";
            }
            else if (rigController.ActiveAction == CharacterRoutineAction.Flex ||
                     rigController.ActiveAction == CharacterRoutineAction.Stretch)
            {
                mouth = "Strain";
            }
            SetFaceGroup(activeView, "Mouth", mouth);
        }

        private static void SetDefaultFaceState(ViewInstance view)
        {
            if (view == null)
            {
                return;
            }
            SetFaceGroup(view, "EyeL", "Open");
            SetFaceGroup(view, "EyeR", "Open");
            SetFaceGroup(view, "Eye", "Open");
            SetFaceGroup(view, "Mouth", "Neutral");
        }

        private static void SetFaceGroup(
            ViewInstance view,
            string group,
            string state)
        {
            if (view == null ||
                !view.face.TryGetValue(
                    group,
                    out Dictionary<string, Image> states))
            {
                return;
            }
            foreach (KeyValuePair<string, Image> pair in states)
            {
                if (pair.Value != null)
                {
                    pair.Value.gameObject.SetActive(
                        string.Equals(
                            pair.Key,
                            state,
                            StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        private void ScheduleBlink()
        {
            nextBlinkAt =
                Time.unscaledTime + UnityEngine.Random.Range(2.0f, 5.0f);
        }

        private void SuppressLegacyVisuals()
        {
            if (flatSpriteController != null)
            {
                flatSpriteController.enabled = false;
            }
            if (visualRoot == null)
            {
                return;
            }

            CharacterMeshGraphic[] legacyMeshes =
                visualRoot.GetComponentsInChildren<CharacterMeshGraphic>(true);
            for (int i = 0; i < legacyMeshes.Length; i++)
            {
                CanvasRenderer renderer = legacyMeshes[i].canvasRenderer;
                if (renderer != null)
                {
                    renderer.SetAlpha(0f);
                }
            }

            Image[] images = visualRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null ||
                    (layeredRoot != null &&
                     image.transform.IsChildOf(layeredRoot)))
                {
                    continue;
                }
                string objectName = image.gameObject.name;
                if (objectName.StartsWith("Sprite.RealFatMan", StringComparison.Ordinal) ||
                    objectName.StartsWith("LayeredFace.", StringComparison.Ordinal) ||
                    objectName.StartsWith("SpriteFace.", StringComparison.Ordinal))
                {
                    image.enabled = false;
                    image.canvasRenderer.SetAlpha(0f);
                }
            }

            DisableLegacyObject("Sprite.RealFatManBody");
            DisableLegacyObject("Sprite.RealFatManLayeredSurface");
            DisableLegacyObject("LayeredPaintedFaceOverlay");
        }

        private void DisableLegacyObject(string objectName)
        {
            Transform[] transforms =
                visualRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null &&
                    candidate.gameObject.name == objectName &&
                    (layeredRoot == null ||
                     !candidate.IsChildOf(layeredRoot)))
                {
                    candidate.gameObject.SetActive(false);
                }
            }
        }

        public bool TryGetWorldBounds(out Bounds bounds)
        {
            bounds = default;
            if (!IsReady ||
                activeView == null ||
                !activeView.root.gameObject.activeInHierarchy)
            {
                return false;
            }
            activeView.root.GetWorldCorners(worldCorners);
            bounds = new Bounds(worldCorners[0], Vector3.zero);
            for (int i = 1; i < worldCorners.Length; i++)
            {
                bounds.Encapsulate(worldCorners[i]);
            }
            return bounds.size.x > 2f && bounds.size.y > 2f;
        }

        public bool TryGetScreenHeightFraction(out float fraction)
        {
            fraction = 0f;
            if (!TryGetWorldBounds(out Bounds bounds) ||
                Screen.height <= 1)
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
            fraction = Mathf.Abs(top.y - bottom.y) / Screen.height;
            return fraction >= MinimumScreenFraction &&
                   fraction <= MaximumScreenFraction;
        }

        public bool FitToScreenHeight(float targetFraction)
        {
            if (!TryGetScreenHeightFraction(out float current) ||
                current <= 0.0001f)
            {
                return false;
            }
            float target = Mathf.Clamp(targetFraction, 0.10f, 0.80f);
            fitScale = Mathf.Clamp(
                fitScale * target / current,
                0.55f,
                2.4f);
            ApplyCombinedScale();
            return true;
        }

        private void LogFailureOnce(string message)
        {
            if (failureLogged)
            {
                return;
            }
            failureLogged = true;
            Debug.LogError(message, this);
        }

        private void ClearLayeredRuntime()
        {
            ready = false;
            activeView = null;
            views.Clear();
            if (layeredRoot != null)
            {
                Destroy(layeredRoot.gameObject);
                layeredRoot = null;
            }
            for (int i = 0; i < runtimeSprites.Count; i++)
            {
                if (runtimeSprites[i] != null)
                {
                    Destroy(runtimeSprites[i]);
                }
            }
            runtimeSprites.Clear();
        }

        private void OnDestroy()
        {
            ClearLayeredRuntime();
        }
    }
}
