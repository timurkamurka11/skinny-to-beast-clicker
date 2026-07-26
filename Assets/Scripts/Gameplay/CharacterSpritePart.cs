using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    public enum FatManSpritePartId
    {
        ShinLeft,
        ShinRight,
        ThighLeft,
        ThighRight,
        Pelvis,
        Belly,
        Chest,
        UpperArmLeft,
        UpperArmRight,
        ForearmLeft,
        ForearmRight,
        Head
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(Image))]
    public sealed class CharacterSpritePart : MonoBehaviour
    {
        [SerializeField] private FatManSpritePartId partId;

        private RectTransform targetRect;
        private Canvas targetCanvas;
        private Image targetImage;
        private Texture2D sourceTexture;
        private Rect normalizedCrop;
        private Vector2 jointPivot;
        private Vector2 baseSize;
        private Sprite runtimeSprite;

        public FatManSpritePartId PartId => partId;
        public bool IsReady =>
            sourceTexture != null &&
            targetRect != null &&
            targetImage != null &&
            runtimeSprite != null;

        public void Configure(
            FatManSpritePartId id,
            Texture2D texture,
            Rect crop,
            Vector2 pivot,
            Vector2 displaySize,
            int sortingOrder)
        {
            partId = id;
            sourceTexture = texture;
            normalizedCrop = crop;
            jointPivot = new Vector2(
                Mathf.Clamp01(pivot.x),
                Mathf.Clamp01(pivot.y));
            baseSize = new Vector2(
                Mathf.Max(2f, displaySize.x),
                Mathf.Max(2f, displaySize.y));

            targetRect = GetComponent<RectTransform>();
            targetCanvas = GetComponent<Canvas>();
            targetImage = GetComponent<Image>();

            targetRect.anchorMin = new Vector2(0.5f, 0.5f);
            targetRect.anchorMax = new Vector2(0.5f, 0.5f);
            targetRect.pivot = jointPivot;
            targetRect.anchoredPosition = Vector2.zero;
            targetRect.localRotation = Quaternion.identity;
            targetRect.localScale = Vector3.one;
            targetRect.sizeDelta = baseSize;

            targetCanvas.overrideSorting = true;
            targetCanvas.sortingOrder = sortingOrder;

            targetImage.raycastTarget = false;
            targetImage.maskable = false;
            targetImage.preserveAspect = false;
            targetImage.type = Image.Type.Simple;
            targetImage.color = Color.white;
        }

        public bool ApplyView(RectInt viewBounds)
        {
            if (sourceTexture == null ||
                targetImage == null ||
                viewBounds.width < 4 ||
                viewBounds.height < 4)
            {
                return false;
            }

            RectInt pixelRect = BuildPixelRect(viewBounds);
            if (pixelRect.width < 2 || pixelRect.height < 2)
            {
                return false;
            }

            if (runtimeSprite != null)
            {
                Destroy(runtimeSprite);
                runtimeSprite = null;
            }

            runtimeSprite = Sprite.Create(
                sourceTexture,
                new Rect(
                    pixelRect.x,
                    pixelRect.y,
                    pixelRect.width,
                    pixelRect.height),
                jointPivot,
                100f,
                0,
                SpriteMeshType.FullRect);
            runtimeSprite.name = $"FatMan.{partId}.{pixelRect.x}.{pixelRect.y}";
            targetImage.sprite = runtimeSprite;
            targetImage.SetAllDirty();
            return true;
        }

        public void ApplyStageScale(float scale)
        {
            float safeScale = Mathf.Clamp(scale, 0.8f, 1.2f);
            targetRect.localScale =
                new Vector3(safeScale, safeScale, 1f);
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        private RectInt BuildPixelRect(RectInt viewBounds)
        {
            int x = viewBounds.x +
                    Mathf.RoundToInt(
                        normalizedCrop.x * viewBounds.width);
            int y = viewBounds.y +
                    Mathf.RoundToInt(
                        normalizedCrop.y * viewBounds.height);
            int width = Mathf.RoundToInt(
                normalizedCrop.width * viewBounds.width);
            int height = Mathf.RoundToInt(
                normalizedCrop.height * viewBounds.height);

            int xMin = Mathf.Clamp(
                x,
                viewBounds.x,
                viewBounds.xMax - 2);
            int yMin = Mathf.Clamp(
                y,
                viewBounds.y,
                viewBounds.yMax - 2);
            int xMax = Mathf.Clamp(
                x + Mathf.Max(2, width),
                xMin + 2,
                viewBounds.xMax);
            int yMax = Mathf.Clamp(
                y + Mathf.Max(2, height),
                yMin + 2,
                viewBounds.yMax);

            return new RectInt(
                xMin,
                yMin,
                xMax - xMin,
                yMax - yMin);
        }

        private void OnDestroy()
        {
            if (runtimeSprite != null)
            {
                Destroy(runtimeSprite);
                runtimeSprite = null;
            }
        }
    }
}
