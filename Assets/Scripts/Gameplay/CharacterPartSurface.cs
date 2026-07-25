using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Stable uGUI mirror under every artistic skeletal part. Both child
    /// surfaces use the exact fat-man silhouette, so Unity's fallback render
    /// contract can no longer turn the actor back into rounded rectangles.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class CharacterPartSurface : MonoBehaviour
    {
        private const string OutlineName = "ArtOutline";
        private const string FillName = "ArtFill";

        private RectTransform owner;
        private CharacterSurfaceGraphic outlineSurface;
        private CharacterSurfaceGraphic fillSurface;
        private CharacterMeshShape shape;
        private Color fillColor = Color.clear;
        private Color outlineColor = Color.clear;
        private float outlineWidth;
        private float topWidth = 1f;
        private float bottomWidth = 1f;
        private bool configured;

        public bool IsRenderable
        {
            get
            {
                if (!configured ||
                    !isActiveAndEnabled ||
                    fillSurface == null ||
                    !fillSurface.isActiveAndEnabled ||
                    fillColor.a <= 0.001f ||
                    owner == null ||
                    owner.rect.width <= 0.5f ||
                    owner.rect.height <= 0.5f)
                {
                    return false;
                }

                // An ancestor CanvasGroup intentionally remains transparent
                // until readiness succeeds, so inherited alpha cannot be used
                // here. The dedicated vector surface is the render contract.
                return fillSurface.IsRenderable;
            }
        }

        public void Configure(
            CharacterMeshShape nextShape,
            Vector2 size,
            Color fill,
            Color outline,
            float border,
            float topRatio,
            float bottomRatio)
        {
            owner = transform as RectTransform;
            shape = nextShape;
            fillColor = fill;
            outlineColor = outline;
            outlineWidth = Mathf.Max(0f, border);
            topWidth = Mathf.Clamp(topRatio, 0.35f, 1.5f);
            bottomWidth = Mathf.Clamp(bottomRatio, 0.35f, 1.5f);

            EnsureSurfaces();
            SetSize(size);
            RefreshAppearance();
            configured = owner != null &&
                         outlineSurface != null &&
                         fillSurface != null;
        }

        public void SetFill(Color fill)
        {
            fillColor = fill;
            RefreshAppearance();
        }

        public void SetOutline(Color outline)
        {
            outlineColor = outline;
            RefreshAppearance();
        }

        public void SetSize(Vector2 size)
        {
            if (owner != null)
            {
                owner.sizeDelta = new Vector2(
                    Mathf.Max(1f, size.x),
                    Mathf.Max(1f, size.y));
            }

            RefreshLayout();
        }

        public void SetWidthProfile(
            float topRatio,
            float bottomRatio)
        {
            topWidth = Mathf.Clamp(topRatio, 0.35f, 1.5f);
            bottomWidth = Mathf.Clamp(bottomRatio, 0.35f, 1.5f);
            RefreshLayout();
        }

        public void ForceRefresh()
        {
            EnsureSurfaces();
            RefreshLayout();
            RefreshAppearance();
            configured = owner != null &&
                         outlineSurface != null &&
                         fillSurface != null;
            outlineSurface?.SetAllDirty();
            fillSurface?.SetAllDirty();
        }

        private void EnsureSurfaces()
        {
            if (owner == null)
            {
                owner = transform as RectTransform;
            }

            DisableLegacyPrimitive("VisibleOutline");
            DisableLegacyPrimitive("VisibleFill");

            if (outlineSurface == null)
            {
                outlineSurface =
                    GetOrCreateSurface(OutlineName);
            }

            if (fillSurface == null)
            {
                fillSurface =
                    GetOrCreateSurface(FillName);
            }

            ApplyShape();
        }

        private CharacterSurfaceGraphic GetOrCreateSurface(
            string objectName)
        {
            Transform existing = transform.Find(objectName);
            GameObject target;
            if (existing == null)
            {
                target = new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer));
                target.layer = gameObject.layer;
                target.transform.SetParent(transform, false);
            }
            else
            {
                target = existing.gameObject;
                target.layer = gameObject.layer;
                if (target.GetComponent<CanvasRenderer>() == null)
                {
                    target.AddComponent<CanvasRenderer>();
                }
            }

            RectTransform rect =
                target.GetComponent<RectTransform>();
            if (rect == null)
            {
                throw new MissingComponentException(
                    $"{target.name} requires a RectTransform.");
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            CharacterSurfaceGraphic surface =
                target.GetComponent<CharacterSurfaceGraphic>();
            if (surface == null)
            {
                surface =
                    target.AddComponent<CharacterSurfaceGraphic>();
            }

            surface.raycastTarget = false;
            surface.maskable = false;
            surface.canvasRenderer.cullTransparentMesh = false;
            return surface;
        }

        private void ApplyShape()
        {
            if (outlineSurface == null ||
                fillSurface == null)
            {
                return;
            }

            Color visibleOutline = outlineColor;
            visibleOutline.a *= fillColor.a;
            outlineSurface.Configure(
                shape,
                visibleOutline,
                topWidth,
                bottomWidth);
            fillSurface.Configure(
                shape,
                fillColor,
                topWidth,
                bottomWidth);
        }

        private void RefreshLayout()
        {
            if (owner == null ||
                outlineSurface == null ||
                fillSurface == null)
            {
                return;
            }

            RectTransform outlineRect =
                outlineSurface.rectTransform;
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;

            float minimumDimension = Mathf.Max(
                1f,
                Mathf.Min(
                    owner.rect.width > 0f
                        ? owner.rect.width
                        : owner.sizeDelta.x,
                    owner.rect.height > 0f
                        ? owner.rect.height
                        : owner.sizeDelta.y));
            float inset = Mathf.Clamp(
                outlineWidth,
                0f,
                minimumDimension * 0.18f);

            RectTransform fillRect = fillSurface.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(inset, inset);
            fillRect.offsetMax = new Vector2(-inset, -inset);
            outlineRect.localScale = Vector3.one;
            fillRect.localScale = Vector3.one;
            ApplyShape();
        }

        private void RefreshAppearance()
        {
            if (outlineSurface == null ||
                fillSurface == null)
            {
                return;
            }

            ApplyShape();
            bool visible = fillColor.a > 0.001f;
            outlineSurface.enabled = visible;
            fillSurface.enabled = visible;
            outlineSurface.canvasRenderer.cullTransparentMesh = false;
            fillSurface.canvasRenderer.cullTransparentMesh = false;
        }

        private void DisableLegacyPrimitive(string objectName)
        {
            Transform existing = transform.Find(objectName);
            if (existing == null)
            {
                return;
            }

            Component[] components =
                existing.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component != null &&
                    component.GetType().Name == "Image" &&
                    component is Behaviour behaviour)
                {
                    behaviour.enabled = false;
                }
            }
        }
    }
}
