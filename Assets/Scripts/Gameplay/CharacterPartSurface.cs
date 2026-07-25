using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Standard uGUI render surface mirrored under every procedural character
    /// part. CharacterMeshGraphic still owns the exact vector geometry, while
    /// this surface guarantees that Unity 6 produces visible CanvasRenderer
    /// output on every supported render pipeline.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class CharacterPartSurface : MonoBehaviour
    {
        private const string OutlineName = "VisibleOutline";
        private const string FillName = "VisibleFill";

        private RectTransform owner;
        private Image outlineImage;
        private Image fillImage;
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
                    fillImage == null ||
                    !fillImage.isActiveAndEnabled ||
                    fillColor.a <= 0.001f ||
                    owner == null ||
                    owner.rect.width <= 0.5f ||
                    owner.rect.height <= 0.5f)
                {
                    return false;
                }

                // The gameplay window intentionally keeps an ancestor
                // CanvasGroup at alpha zero until validation succeeds. Do not
                // use inherited alpha here or readiness would deadlock. The
                // standard Image + CanvasRenderer pair is the render contract.
                return fillImage.canvasRenderer != null;
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

            EnsureImages();
            SetSize(size);
            RefreshAppearance();
            configured = owner != null &&
                         outlineImage != null &&
                         fillImage != null;
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

        public void SetWidthProfile(float topRatio, float bottomRatio)
        {
            topWidth = Mathf.Clamp(topRatio, 0.35f, 1.5f);
            bottomWidth = Mathf.Clamp(bottomRatio, 0.35f, 1.5f);
            RefreshLayout();
        }

        public void ForceRefresh()
        {
            EnsureImages();
            RefreshLayout();
            RefreshAppearance();
            configured = owner != null &&
                         outlineImage != null &&
                         fillImage != null;
            outlineImage?.SetAllDirty();
            fillImage?.SetAllDirty();
        }

        private void EnsureImages()
        {
            if (owner == null)
            {
                owner = transform as RectTransform;
            }

            if (outlineImage == null)
            {
                Transform existing = transform.Find(OutlineName);
                outlineImage = existing != null
                    ? existing.GetComponent<Image>()
                    : CreateImage(OutlineName);
            }

            if (fillImage == null)
            {
                Transform existing = transform.Find(FillName);
                fillImage = existing != null
                    ? existing.GetComponent<Image>()
                    : CreateImage(FillName);
            }

            ApplyShape();
        }

        private Image CreateImage(string objectName)
        {
            GameObject target = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.layer = gameObject.layer;
            target.transform.SetParent(transform, false);

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            Image image = target.GetComponent<Image>();
            image.raycastTarget = false;
            image.maskable = false;
            image.preserveAspect = false;
            image.canvasRenderer.cullTransparentMesh = false;
            return image;
        }

        private void ApplyShape()
        {
            if (outlineImage == null || fillImage == null)
            {
                return;
            }

            bool rounded =
                shape == CharacterMeshShape.Torso ||
                shape == CharacterMeshShape.Shoe ||
                shape == CharacterMeshShape.Brow ||
                shape == CharacterMeshShape.Mouth;
            Sprite sprite = rounded
                ? LivingGameplayVisualFactory.GetRoundedSprite()
                : LivingGameplayVisualFactory.GetSoftCircleSprite();
            Image.Type type =
                rounded ? Image.Type.Sliced : Image.Type.Simple;

            outlineImage.sprite = sprite;
            fillImage.sprite = sprite;
            outlineImage.type = type;
            fillImage.type = type;
        }

        private void RefreshLayout()
        {
            if (owner == null ||
                outlineImage == null ||
                fillImage == null)
            {
                return;
            }

            RectTransform outlineRect = outlineImage.rectTransform;
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

            RectTransform fillRect = fillImage.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(inset, inset);
            fillRect.offsetMax = new Vector2(-inset, -inset);

            float widthProfile =
                Mathf.Clamp((topWidth + bottomWidth) * 0.5f, 0.62f, 1.22f);
            outlineRect.localScale =
                new Vector3(widthProfile, 1f, 1f);
            fillRect.localScale =
                new Vector3(widthProfile, 1f, 1f);
        }

        private void RefreshAppearance()
        {
            if (outlineImage == null || fillImage == null)
            {
                return;
            }

            Color visibleOutline = outlineColor;
            visibleOutline.a *= fillColor.a;
            outlineImage.color = visibleOutline;
            fillImage.color = fillColor;

            bool visible = fillColor.a > 0.001f;
            outlineImage.enabled = visible;
            fillImage.enabled = visible;
            outlineImage.canvasRenderer.cullTransparentMesh = false;
            fillImage.canvasRenderer.cullTransparentMesh = false;
        }
    }
}
