using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    internal sealed class AmbientAnimationController : MonoBehaviour
    {
        private sealed class DustParticle
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;
            public Vector2 Velocity;
            public float Phase;
            public float BaseAlpha;
        }

        private readonly List<DustParticle> dustParticles = new List<DustParticle>();
        private Image lampGlow;
        private Image windowGlow;
        private RectTransform windowGlowRect;
        private float tapPulse;
        private float flickerTarget = 1f;
        private float nextFlickerAt;
        private bool improvedRoom;

        public void Build(Transform parent)
        {
            Sprite softCircle = LivingGameplayVisualFactory.GetSoftCircleSprite();

            windowGlow = LivingGameplayVisualFactory.CreateImage(
                parent,
                "WindowNightGlow",
                new Vector2(0.5f, 0f),
                new Vector2(70f, 1170f),
                new Vector2(760f, 820f),
                softCircle,
                new Color(0.02f, 0.39f, 1f, 0.10f));
            windowGlowRect = windowGlow.rectTransform;

            lampGlow = LivingGameplayVisualFactory.CreateImage(
                parent,
                "LampWarmGlow",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 1750f),
                new Vector2(640f, 560f),
                softCircle,
                new Color(1f, 0.53f, 0.16f, 0.17f));

            RectTransform dustLayer =
                LivingGameplayVisualFactory.CreateStretchRect(parent, "FloatingDust");
            BuildDust(dustLayer);
            ScheduleFlicker();
        }

        public void PulseFromTap()
        {
            tapPulse = Mathf.Min(1.5f, tapPulse + 0.58f);
        }

        public void SetImprovedRoom(bool improved)
        {
            improvedRoom = improved;
        }

        private void BuildDust(RectTransform parent)
        {
            Sprite softCircle = LivingGameplayVisualFactory.GetSoftCircleSprite();
            for (int i = 0; i < 18; i++)
            {
                float size = UnityEngine.Random.Range(4f, 13f);
                Image image = LivingGameplayVisualFactory.CreateImage(
                    parent,
                    $"Dust_{i:00}",
                    new Vector2(0.5f, 0f),
                    Vector2.zero,
                    new Vector2(size, size),
                    softCircle,
                    Color.white);

                DustParticle particle = new DustParticle
                {
                    Rect = image.rectTransform,
                    Image = image,
                    Position = new Vector2(
                        UnityEngine.Random.Range(-500f, 500f),
                        UnityEngine.Random.Range(250f, 1750f)),
                    Velocity = new Vector2(
                        UnityEngine.Random.Range(-5f, 8f),
                        UnityEngine.Random.Range(7f, 18f)),
                    Phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                    BaseAlpha = UnityEngine.Random.Range(0.035f, 0.12f)
                };

                particle.Rect.anchoredPosition = particle.Position;
                dustParticles.Add(particle);
            }
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            float delta = Time.unscaledDeltaTime;
            tapPulse = Mathf.MoveTowards(tapPulse, 0f, delta * 2.8f);

            if (now >= nextFlickerAt)
            {
                flickerTarget = UnityEngine.Random.value < 0.28f
                    ? UnityEngine.Random.Range(0.72f, 0.9f)
                    : UnityEngine.Random.Range(0.96f, 1.04f);
                ScheduleFlicker();
            }

            float roomMultiplier = improvedRoom ? 1.08f : 1f;
            if (lampGlow != null)
            {
                float softWave = 0.96f + Mathf.Sin(now * 1.75f) * 0.035f;
                float alpha = (0.14f + tapPulse * 0.045f) *
                              Mathf.Lerp(1f, flickerTarget, 0.55f) *
                              roomMultiplier;
                lampGlow.color = new Color(1f, 0.53f, 0.16f, alpha);
                lampGlow.rectTransform.localScale =
                    Vector3.one * (softWave + tapPulse * 0.025f);
            }

            if (windowGlow != null)
            {
                float wave = Mathf.Sin(now * 0.55f);
                windowGlow.color = new Color(
                    0.02f,
                    0.39f,
                    1f,
                    (0.065f + (wave + 1f) * 0.018f) * (improvedRoom ? 0.82f : 1f));
                windowGlowRect.localScale = new Vector3(
                    1f + wave * 0.018f,
                    1f - wave * 0.012f,
                    1f);
                windowGlowRect.anchoredPosition =
                    new Vector2(70f + wave * 5f, 1170f);
            }

            UpdateDust(now, delta);
        }

        private void UpdateDust(float now, float delta)
        {
            foreach (DustParticle particle in dustParticles)
            {
                particle.Position += particle.Velocity * delta;
                particle.Position.x +=
                    Mathf.Sin(now * 0.8f + particle.Phase) * delta * 8f;

                if (particle.Position.y > 1810f)
                {
                    particle.Position.y = 220f;
                    particle.Position.x = UnityEngine.Random.Range(-500f, 500f);
                }

                if (particle.Position.x > 535f)
                {
                    particle.Position.x = -535f;
                }
                else if (particle.Position.x < -535f)
                {
                    particle.Position.x = 535f;
                }

                particle.Rect.anchoredPosition = particle.Position;
                float alphaWave =
                    (Mathf.Sin(now * 1.3f + particle.Phase) + 1f) * 0.5f;
                particle.Image.color = new Color(
                    improvedRoom ? 0.74f : 1f,
                    improvedRoom ? 0.85f : 0.79f,
                    1f,
                    particle.BaseAlpha * Mathf.Lerp(0.35f, 1f, alphaWave));
            }
        }

        private void ScheduleFlicker()
        {
            nextFlickerAt = Time.unscaledTime + UnityEngine.Random.Range(0.08f, 1.8f);
        }
    }
}
