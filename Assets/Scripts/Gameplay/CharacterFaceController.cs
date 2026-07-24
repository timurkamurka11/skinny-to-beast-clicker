using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    public enum CharacterExpression
    {
        Neutral,
        Tired,
        Focused,
        Happy,
        Strain,
        Yawn
    }

    [DisallowMultipleComponent]
    public sealed class CharacterFaceController : MonoBehaviour
    {
        private RectTransform faceRoot;
        private Image leftEye;
        private Image rightEye;
        private Image leftPupil;
        private Image rightPupil;
        private Image leftEyelid;
        private Image rightEyelid;
        private Image leftBrow;
        private Image rightBrow;
        private Image mouthLine;
        private Image mouthOpen;
        private Image leftSmileCorner;
        private Image rightSmileCorner;

        private CharacterFaceStyle style;
        private CharacterExpression baseExpression;
        private CharacterExpression activeExpression;
        private Vector2 lookTarget;
        private Vector2 currentLook;
        private float lookUntil;
        private float expressionUntil;
        private float nextBlinkAt;
        private float blinkStartedAt = -10f;
        private bool doubleBlink;
        private bool built;
        private bool visible = true;

        public void Build(RectTransform headBone)
        {
            if (built || headBone == null)
            {
                return;
            }

            faceRoot = LivingGameplayVisualFactory.CreateRect(
                headBone,
                "FaceRig",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(190f, 170f));

            leftEye = CreateOval(faceRoot, "Eye.L", new Vector2(-33f, 31f), new Vector2(43f, 34f));
            rightEye = CreateOval(faceRoot, "Eye.R", new Vector2(33f, 31f), new Vector2(43f, 34f));
            leftPupil = CreateOval(leftEye.rectTransform, "Pupil.L", Vector2.zero, new Vector2(15f, 19f));
            rightPupil = CreateOval(rightEye.rectTransform, "Pupil.R", Vector2.zero, new Vector2(15f, 19f));

            leftEyelid = CreateRounded(
                faceRoot,
                "Eyelid.L",
                new Vector2(-33f, 32f),
                new Vector2(48f, 15f));
            rightEyelid = CreateRounded(
                faceRoot,
                "Eyelid.R",
                new Vector2(33f, 32f),
                new Vector2(48f, 15f));

            leftBrow = CreateRounded(
                faceRoot,
                "Brow.L",
                new Vector2(-34f, 60f),
                new Vector2(48f, 9f));
            rightBrow = CreateRounded(
                faceRoot,
                "Brow.R",
                new Vector2(34f, 60f),
                new Vector2(48f, 9f));

            mouthOpen = CreateOval(
                faceRoot,
                "Mouth.Open",
                new Vector2(0f, -31f),
                new Vector2(34f, 10f));
            mouthLine = CreateRounded(
                faceRoot,
                "Mouth.Line",
                new Vector2(0f, -28f),
                new Vector2(60f, 7f));
            leftSmileCorner = CreateRounded(
                faceRoot,
                "Mouth.Corner.L",
                new Vector2(-28f, -24f),
                new Vector2(22f, 6f));
            rightSmileCorner = CreateRounded(
                faceRoot,
                "Mouth.Corner.R",
                new Vector2(28f, -24f),
                new Vector2(22f, 6f));

            ScheduleBlink();
            built = true;
        }

        public void ApplyStyle(
            CharacterFaceStyle nextStyle,
            CharacterRigProfile profile)
        {
            if (!built || profile == null)
            {
                return;
            }

            style = nextStyle;
            baseExpression = style.defaultExpression;
            activeExpression = baseExpression;
            expressionUntil = 0f;

            Vector2 fullSize = new Vector2(profile.visualWidth, profile.visualHeight);
            faceRoot.anchoredPosition = Vector2.Scale(
                profile.faceCenter - profile.head,
                fullSize);
            faceRoot.localScale = Vector3.one * Mathf.Max(0.1f, style.overlayScale);

            SetColor(leftEye, style.eyeWhite);
            SetColor(rightEye, style.eyeWhite);
            SetColor(leftPupil, style.iris);
            SetColor(rightPupil, style.iris);
            SetColor(leftEyelid, style.skin);
            SetColor(rightEyelid, style.skin);
            SetColor(leftBrow, style.brow);
            SetColor(rightBrow, style.brow);
            SetColor(mouthLine, style.mouth);
            SetColor(mouthOpen, style.mouth);
            SetColor(leftSmileCorner, style.mouth);
            SetColor(rightSmileCorner, style.mouth);

            leftEye.rectTransform.anchoredPosition = new Vector2(-style.eyeSeparation, style.eyeY);
            rightEye.rectTransform.anchoredPosition = new Vector2(style.eyeSeparation, style.eyeY);
            leftEyelid.rectTransform.anchoredPosition = leftEye.rectTransform.anchoredPosition;
            rightEyelid.rectTransform.anchoredPosition = rightEye.rectTransform.anchoredPosition;
            leftBrow.rectTransform.anchoredPosition = new Vector2(-style.eyeSeparation, style.eyeY + 29f);
            rightBrow.rectTransform.anchoredPosition = new Vector2(style.eyeSeparation, style.eyeY + 29f);
            ApplyExpression(baseExpression, 1f);
            ApplyBlink(0f);
        }

        public void SetVisible(bool shouldShow)
        {
            visible = shouldShow;
            if (faceRoot != null)
            {
                faceRoot.gameObject.SetActive(shouldShow);
            }
        }

        public void LookAt(Vector2 normalizedDirection, float duration)
        {
            lookTarget = Vector2.ClampMagnitude(normalizedDirection, 1f);
            lookUntil = Time.unscaledTime + Mathf.Max(0.05f, duration);
        }

        public void SetExpression(CharacterExpression expression, float duration)
        {
            activeExpression = expression;
            expressionUntil = Time.unscaledTime + Mathf.Max(0.05f, duration);
        }

        public void ResetExpression()
        {
            activeExpression = baseExpression;
            expressionUntil = 0f;
        }

        public void ForceBlink(bool twice)
        {
            blinkStartedAt = Time.unscaledTime;
            doubleBlink = twice;
        }

        private void Update()
        {
            if (!built || !visible)
            {
                return;
            }

            float now = Time.unscaledTime;
            float delta = Time.unscaledDeltaTime;

            if (now >= nextBlinkAt)
            {
                ForceBlink(Random.value < 0.18f);
                ScheduleBlink();
            }

            Vector2 desiredLook = now <= lookUntil ? lookTarget : Vector2.zero;
            currentLook = Vector2.Lerp(
                currentLook,
                desiredLook,
                1f - Mathf.Exp(-delta * 7.5f));
            Vector2 pupilOffset = new Vector2(currentLook.x * 7f, currentLook.y * 5f);
            leftPupil.rectTransform.anchoredPosition = pupilOffset;
            rightPupil.rectTransform.anchoredPosition = pupilOffset;

            CharacterExpression expression =
                now <= expressionUntil ? activeExpression : baseExpression;
            ApplyExpression(expression, delta);
            ApplyBlink(GetBlinkAmount(now));
        }

        private float GetBlinkAmount(float now)
        {
            float age = now - blinkStartedAt;
            float first = BlinkPulse(age);
            if (!doubleBlink)
            {
                return first;
            }

            return Mathf.Max(first, BlinkPulse(age - 0.19f));
        }

        private static float BlinkPulse(float age)
        {
            const float half = 0.055f;
            if (age < 0f || age >= half * 2f)
            {
                return 0f;
            }

            return age <= half
                ? Mathf.SmoothStep(0f, 1f, age / half)
                : Mathf.SmoothStep(1f, 0f, (age - half) / half);
        }

        private void ApplyBlink(float amount)
        {
            float clamped = Mathf.Clamp01(amount);
            SetAlpha(leftEyelid, clamped);
            SetAlpha(rightEyelid, clamped);
            float eyelidScale = Mathf.Lerp(0.18f, 1.35f, clamped);
            leftEyelid.rectTransform.localScale = new Vector3(1f, eyelidScale, 1f);
            rightEyelid.rectTransform.localScale = new Vector3(1f, eyelidScale, 1f);
        }

        private void ApplyExpression(CharacterExpression expression, float delta)
        {
            float blend = delta >= 0.99f
                ? 1f
                : 1f - Mathf.Exp(-Mathf.Max(0f, delta) * 10f);

            float leftBrowRotation = 0f;
            float rightBrowRotation = 0f;
            float browY = style.eyeY + 29f;
            Vector2 mouthSize = new Vector2(60f, 7f);
            float mouthRotation = 0f;
            Vector2 openSize = new Vector2(34f, 4f);
            float cornerAlpha = 0f;

            switch (expression)
            {
                case CharacterExpression.Tired:
                    leftBrowRotation = -11f;
                    rightBrowRotation = 11f;
                    browY -= 4f;
                    mouthRotation = -2f;
                    break;
                case CharacterExpression.Focused:
                    leftBrowRotation = 12f;
                    rightBrowRotation = -12f;
                    browY -= 2f;
                    mouthSize = new Vector2(48f, 8f);
                    break;
                case CharacterExpression.Happy:
                    leftBrowRotation = -4f;
                    rightBrowRotation = 4f;
                    mouthSize = new Vector2(54f, 8f);
                    mouthRotation = 2f;
                    cornerAlpha = 1f;
                    break;
                case CharacterExpression.Strain:
                    leftBrowRotation = 17f;
                    rightBrowRotation = -17f;
                    browY -= 5f;
                    mouthSize = new Vector2(43f, 10f);
                    openSize = new Vector2(29f, 13f);
                    break;
                case CharacterExpression.Yawn:
                    leftBrowRotation = -7f;
                    rightBrowRotation = 7f;
                    browY -= 3f;
                    mouthSize = new Vector2(25f, 6f);
                    openSize = new Vector2(31f, 42f);
                    break;
            }

            SetRotation(leftBrow.rectTransform, leftBrowRotation, blend);
            SetRotation(rightBrow.rectTransform, rightBrowRotation, blend);
            SetY(leftBrow.rectTransform, browY, blend);
            SetY(rightBrow.rectTransform, browY, blend);
            mouthLine.rectTransform.sizeDelta = Vector2.Lerp(
                mouthLine.rectTransform.sizeDelta,
                mouthSize,
                blend);
            SetRotation(mouthLine.rectTransform, mouthRotation, blend);
            mouthOpen.rectTransform.sizeDelta = Vector2.Lerp(
                mouthOpen.rectTransform.sizeDelta,
                openSize,
                blend);
            SetAlpha(mouthOpen, Mathf.InverseLerp(5f, 14f, openSize.y));
            SetAlpha(leftSmileCorner, cornerAlpha);
            SetAlpha(rightSmileCorner, cornerAlpha);
            SetRotation(leftSmileCorner.rectTransform, -32f, blend);
            SetRotation(rightSmileCorner.rectTransform, 32f, blend);
        }

        private void ScheduleBlink()
        {
            nextBlinkAt = Time.unscaledTime + Random.Range(2.5f, 6f);
        }

        private static Image CreateOval(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            Image image = LivingGameplayVisualFactory.CreateImage(
                parent,
                name,
                new Vector2(0.5f, 0.5f),
                position,
                size,
                LivingGameplayVisualFactory.GetSoftCircleSprite(),
                Color.white);
            return image;
        }

        private static Image CreateRounded(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            return LivingGameplayVisualFactory.CreateImage(
                parent,
                name,
                new Vector2(0.5f, 0.5f),
                position,
                size,
                LivingGameplayVisualFactory.GetRoundedSprite(),
                Color.white);
        }

        private static void SetColor(Image image, Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }

        private static void SetAlpha(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            Color color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        private static void SetRotation(RectTransform rect, float target, float blend)
        {
            float current = rect.localEulerAngles.z;
            float next = Mathf.LerpAngle(current, target, blend);
            rect.localRotation = Quaternion.Euler(0f, 0f, next);
        }

        private static void SetY(RectTransform rect, float target, float blend)
        {
            Vector2 position = rect.anchoredPosition;
            position.y = Mathf.Lerp(position.y, target, blend);
            rect.anchoredPosition = position;
        }
    }
}
