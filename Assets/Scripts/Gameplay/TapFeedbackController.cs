using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    internal sealed class TapFeedbackController : MonoBehaviour
    {
        private sealed class FloatingGain
        {
            public RectTransform Rect;
            public TMP_Text Text;
            public Vector2 Start;
            public float Age;
            public float Duration;
            public bool Active;
        }

        private sealed class SparkParticle
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;
            public Vector2 Velocity;
            public float RotationSpeed;
            public float Age;
            public float Duration;
            public bool Active;
        }

        private readonly List<FloatingGain> floatingPool = new List<FloatingGain>();
        private readonly List<SparkParticle> sparkPool = new List<SparkParticle>();
        private RectTransform root;
        private Vector2 sourcePosition;
        private int nextFloating;
        private int nextSpark;

        public void Build(RectTransform parent, Vector2 tapSource)
        {
            root = parent;
            sourcePosition = tapSource;

            for (int i = 0; i < 18; i++)
            {
                floatingPool.Add(CreateFloatingGain(i));
            }

            for (int i = 0; i < 54; i++)
            {
                sparkPool.Add(CreateSpark(i));
            }
        }

        public void EmitTap(string value, int chain)
        {
            EmitFloating(
                value,
                sourcePosition + new Vector2(
                    UnityEngine.Random.Range(-130f, 130f),
                    UnityEngine.Random.Range(125f, 185f)),
                chain >= 5 ? new Color(1f, 0.91f, 0.34f, 1f) : new Color(1f, 0.72f, 0.1f, 1f));

            int count = chain >= 8 ? 13 : chain >= 3 ? 10 : 8;
            EmitBurst(sourcePosition, count, 165f, false);
        }

        public void EmitUpgrade(Vector2 position)
        {
            EmitFloating("UPGRADE!", position + Vector2.up * 110f, new Color(0.2f, 0.82f, 1f, 1f));
            EmitBurst(position, 18, 235f, true);
        }

        public void EmitStageChange()
        {
            EmitFloating(
                "NEW BODY!",
                new Vector2(0f, 950f),
                new Color(1f, 0.75f, 0.12f, 1f));
            EmitBurst(new Vector2(0f, 930f), 30, 345f, true);
        }

        private FloatingGain CreateFloatingGain(int index)
        {
            RectTransform rect = LivingGameplayVisualFactory.CreateRect(
                root,
                $"FloatingPower_{index:00}",
                new Vector2(0.5f, 0f),
                Vector2.zero,
                new Vector2(470f, 72f));

            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            text.fontSize = 36f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 24f;
            text.fontSizeMax = 38f;
            text.raycastTarget = false;
            text.outlineColor = new Color32(11, 17, 27, 255);
            text.outlineWidth = 0.22f;
            text.gameObject.SetActive(false);

            return new FloatingGain
            {
                Rect = rect,
                Text = text,
                Duration = 0.78f
            };
        }

        private SparkParticle CreateSpark(int index)
        {
            float size = UnityEngine.Random.Range(20f, 36f);
            Image image = LivingGameplayVisualFactory.CreateImage(
                root,
                $"TapSpark_{index:00}",
                new Vector2(0.5f, 0f),
                Vector2.zero,
                new Vector2(size, size),
                LivingGameplayVisualFactory.GetSparkSprite(),
                Color.white);
            image.gameObject.SetActive(false);

            return new SparkParticle
            {
                Rect = image.rectTransform,
                Image = image
            };
        }

        private void EmitFloating(string value, Vector2 position, Color color)
        {
            FloatingGain item = floatingPool[nextFloating++ % floatingPool.Count];
            item.Active = true;
            item.Age = 0f;
            item.Duration = 0.78f;
            item.Start = position;
            item.Rect.anchoredPosition = position;
            item.Rect.localScale = Vector3.one * 0.72f;
            item.Text.text = value;
            item.Text.color = color;
            item.Text.alpha = 1f;
            item.Text.gameObject.SetActive(true);
            item.Rect.SetAsLastSibling();
        }

        private void EmitBurst(
            Vector2 center,
            int count,
            float speed,
            bool blueAndGold)
        {
            for (int i = 0; i < count; i++)
            {
                SparkParticle particle = sparkPool[nextSpark++ % sparkPool.Count];
                float angle = UnityEngine.Random.Range(20f, 160f) * Mathf.Deg2Rad;
                float resolvedSpeed = UnityEngine.Random.Range(speed * 0.55f, speed);

                particle.Active = true;
                particle.Age = 0f;
                particle.Duration = UnityEngine.Random.Range(0.42f, 0.72f);
                particle.Position = center + new Vector2(
                    UnityEngine.Random.Range(-85f, 85f),
                    UnityEngine.Random.Range(-25f, 65f));
                particle.Velocity = new Vector2(
                    Mathf.Cos(angle) * resolvedSpeed,
                    Mathf.Sin(angle) * resolvedSpeed);
                particle.RotationSpeed = UnityEngine.Random.Range(-300f, 300f);
                particle.Rect.anchoredPosition = particle.Position;
                particle.Rect.localScale = Vector3.one * UnityEngine.Random.Range(0.65f, 1.25f);
                particle.Image.color = blueAndGold && i % 3 == 0
                    ? new Color(0.08f, 0.68f, 1f, 1f)
                    : new Color(1f, UnityEngine.Random.Range(0.55f, 0.92f), 0.08f, 1f);
                particle.Image.gameObject.SetActive(true);
                particle.Rect.SetAsLastSibling();
            }
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;

            foreach (FloatingGain item in floatingPool)
            {
                if (!item.Active)
                {
                    continue;
                }

                item.Age += delta;
                float t = Mathf.Clamp01(item.Age / item.Duration);
                float eased = 1f - (1f - t) * (1f - t);
                item.Rect.anchoredPosition = item.Start + Vector2.up * (135f * eased);
                item.Rect.localScale =
                    Vector3.one * Mathf.Lerp(0.72f, 1.16f, Mathf.Sin(t * Mathf.PI));
                item.Text.alpha = 1f - t;

                if (t >= 1f)
                {
                    item.Active = false;
                    item.Text.gameObject.SetActive(false);
                }
            }

            foreach (SparkParticle particle in sparkPool)
            {
                if (!particle.Active)
                {
                    continue;
                }

                particle.Age += delta;
                float t = Mathf.Clamp01(particle.Age / particle.Duration);
                particle.Velocity += Vector2.down * (460f * delta);
                particle.Position += particle.Velocity * delta;
                particle.Rect.anchoredPosition = particle.Position;
                particle.Rect.Rotate(0f, 0f, particle.RotationSpeed * delta);
                particle.Rect.localScale *= 1f - delta * 0.72f;
                particle.Image.color = LivingGameplayVisualFactory.WithAlpha(
                    particle.Image.color,
                    1f - t);

                if (t >= 1f)
                {
                    particle.Active = false;
                    particle.Image.gameObject.SetActive(false);
                }
            }
        }
    }
}
