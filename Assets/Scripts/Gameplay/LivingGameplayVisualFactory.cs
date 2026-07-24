using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    internal static class LivingGameplayVisualFactory
    {
        private static Sprite softCircleSprite;
        private static Sprite sparkSprite;
        private static Sprite roundedSprite;

        public static RectTransform CreateStretchRect(Transform parent, string name)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static RectTransform CreateRect(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image CreateImage(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            Sprite sprite,
            Color color)
        {
            RectTransform rect = CreateRect(parent, name, anchor, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static Image CreateStretchImage(
            Transform parent,
            string name,
            Sprite sprite,
            Color color)
        {
            RectTransform rect = CreateStretchRect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Sprite LoadSprite(string resourcePath)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                Debug.LogError($"Living gameplay sprite is missing at Resources/{resourcePath}.");
                return null;
            }

            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = $"{texture.name}_RuntimeSprite";
            return sprite;
        }

        public static Sprite GetSoftCircleSprite()
        {
            if (softCircleSprite != null)
            {
                return softCircleSprite;
            }

            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "LivingGameplaySoftCircle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / center;
                    float ny = (y - center) / center;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.35f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            softCircleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            softCircleSprite.name = "LivingGameplaySoftCircleSprite";
            return softCircleSprite;
        }

        public static Sprite GetSparkSprite()
        {
            if (sparkSprite != null)
            {
                return sparkSprite;
            }

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "LivingGameplaySpark",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Abs((x - center) / center);
                    float ny = Mathf.Abs((y - center) / center);
                    float horizontal = Mathf.Clamp01(1f - nx * 6.4f) *
                                       Mathf.Clamp01(1f - ny);
                    float vertical = Mathf.Clamp01(1f - ny * 6.4f) *
                                     Mathf.Clamp01(1f - nx);
                    float core = Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny) * 5f);
                    float alpha = Mathf.Clamp01(Mathf.Max(horizontal, vertical) + core);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sparkSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            sparkSprite.name = "LivingGameplaySparkSprite";
            return sparkSprite;
        }

        public static Sprite GetRoundedSprite()
        {
            if (roundedSprite != null)
            {
                return roundedSprite;
            }

            const int size = 64;
            const float radius = 19f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "LivingGameplayRoundedRect",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            float straight = center - radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - center) - straight, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - center) - straight, 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.75f - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            roundedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(20f, 20f, 20f, 20f));
            roundedSprite.name = "LivingGameplayRoundedSprite";
            return roundedSprite;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
