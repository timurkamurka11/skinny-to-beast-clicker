using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    public enum CharacterExpression
    {
        Neutral,
        Relaxed,
        Tired,
        Focused,
        Happy,
        Strain,
        Yawn
    }

    public enum CharacterMouthShape
    {
        Neutral,
        Relaxed,
        Frown,
        Focused,
        Smile,
        Strain,
        Yawn
    }

    [DisallowMultipleComponent]
    public sealed class CharacterFaceController : MonoBehaviour
    {
        private RectTransform faceRoot;
        private CharacterMeshGraphic leftEye;
        private CharacterMeshGraphic rightEye;
        private CharacterMeshGraphic leftPupil;
        private CharacterMeshGraphic rightPupil;
        private CharacterMeshGraphic leftEyelid;
        private CharacterMeshGraphic rightEyelid;
        private CharacterMeshGraphic leftBrow;
        private CharacterMeshGraphic rightBrow;
        private CharacterMeshGraphic mouthLine;
        private CharacterMeshGraphic mouthOpen;
        private CharacterMeshGraphic leftCheek;
        private CharacterMeshGraphic rightCheek;
        private CharacterMeshGraphic sweatDrop;
        private CharacterAnimationDriver animationDriver;

        private CharacterFaceStyle style;
        private CharacterExpression baseExpression;
        private CharacterExpression activeExpression;
        private CharacterFacing facing = CharacterFacing.Front;
        private Vector2 lookTarget;
        private Vector2 currentLook;
        private float lookUntil;
        private float expressionUntil;
        private float nextBlinkAt;
        private float blinkStartedAt = -10f;
        private bool doubleBlink;
        private bool secondBlinkTriggered;
        private bool built;
        private bool requestedVisible = true;

        public CharacterMouthShape CurrentMouthShape { get; private set; }
        public bool IsBuilt => built;
        public bool IsActuallyVisible =>
            built &&
            faceRoot != null &&
            faceRoot.gameObject.activeInHierarchy;

        public void Build(RectTransform headBone)
        {
            if (built || headBone == null)
            {
                return;
            }

            faceRoot = CreateRect(
                headBone,
                "FaceRig",
                new Vector2(0f, 88f),
                new Vector2(190f, 180f));

            leftEye = CreateMesh(
                faceRoot,
                "Eye.L",
                CharacterMeshShape.Ellipse,
                CharacterVisualRole.EyeWhite,
                new Vector2(-33f, 25f),
                new Vector2(48f, 35f),
                Color.white,
                3f);
            rightEye = CreateMesh(
                faceRoot,
                "Eye.R",
                CharacterMeshShape.Ellipse,
                CharacterVisualRole.EyeWhite,
                new Vector2(33f, 25f),
                new Vector2(48f, 35f),
                Color.white,
                3f);

            leftPupil = CreateMesh(
                leftEye.rectTransform,
                "Pupil.L",
                CharacterMeshShape.Ellipse,
                CharacterVisualRole.Iris,
                Vector2.zero,
                new Vector2(16f, 20f),
                Color.black,
                1f);
            rightPupil = CreateMesh(
                rightEye.rectTransform,
                "Pupil.R",
                CharacterMeshShape.Ellipse,
                CharacterVisualRole.Iris,
                Vector2.zero,
                new Vector2(16f, 20f),
                Color.black,
                1f);

            leftEyelid = CreateMesh(
                faceRoot,
                "Eyelid.L",
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Skin,
                new Vector2(-33f, 25f),
                new Vector2(52f, 38f),
                Color.white,
                2f);
            rightEyelid = CreateMesh(
                faceRoot,
                "Eyelid.R",
                CharacterMeshShape.Capsule,
                CharacterVisualRole.Skin,
                new Vector2(33f, 25f),
                new Vector2(52f, 38f),
                Color.white,
                2f);

            leftBrow = CreateMesh(
                faceRoot,
                "Brow.L",
                CharacterMeshShape.Brow,
                CharacterVisualRole.Brow,
                new Vector2(-34f, 57f),
                new Vector2(50f, 9f),
                Color.black,
                1f);
            rightBrow = CreateMesh(
                faceRoot,
                "Brow.R",
                CharacterMeshShape.Brow,
                CharacterVisualRole.Brow,
                new Vector2(34f, 57f),
                new Vector2(50f, 9f),
                Color.black,
                1f);

            mouthOpen = CreateMesh(
                faceRoot,
                "Mouth.Open",
                CharacterMeshShape.Ellipse,
                CharacterVisualRole.Mouth,
                new Vector2(0f, -34f),
                new Vector2(40f, 14f),
                Color.black,
                2f);
            mouthLine = CreateMesh(
                faceRoot,
                "Mouth.Line",
                CharacterMeshShape.Mouth,
                CharacterVisualRole.Mouth,
                new Vector2(0f, -31f),
                new Vector2(62f, 7f),
                Color.black,
                1f);

            leftCheek = CreateMesh(
                faceRoot,
                "Cheek.L",
                CharacterMeshShape.Ellipse,
                CharacterVisualRole.Cheek,
                new Vector2(-58f, -8f),
                new Vector2(28f, 14f),
                Color.clear,
                0f);
            rightCheek = CreateMesh(
                faceRoot,
                "Cheek.R",
                CharacterMeshShape.Ellipse,
                CharacterVisualRole.Cheek,
                new Vector2(58f, -8f),
                new Vector2(28f, 14f),
                Color.clear,
                0f);
            sweatDrop = CreateMesh(
                faceRoot,
                "SweatDrop",
                CharacterMeshShape.Ellipse,
                CharacterVisualRole.Accent,
                new Vector2(75f, 42f),
                new Vector2(13f, 27f),
                Color.clear,
                0f);

            built = true;
            ScheduleBlink();
            ApplyVisibility();
        }

        public void ApplyStyle(CharacterFaceStyle nextStyle)
        {
            if (!built)
            {
                return;
            }

            style = nextStyle;
            baseExpression = style.defaultExpression;
            activeExpression = baseExpression;
            expressionUntil = 0f;
            faceRoot.localScale =
                Vector3.one * Mathf.Max(0.1f, style.overlayScale);

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
            SetColor(leftCheek, style.cheek);
            SetColor(rightCheek, style.cheek);
            SetColor(sweatDrop, new Color(0.45f, 0.83f, 1f, 0f));

            leftEye.rectTransform.anchoredPosition =
                new Vector2(-style.eyeSeparation, style.eyeY);
            rightEye.rectTransform.anchoredPosition =
                new Vector2(style.eyeSeparation, style.eyeY);
            leftEyelid.rectTransform.anchoredPosition =
                leftEye.rectTransform.anchoredPosition;
            rightEyelid.rectTransform.anchoredPosition =
                rightEye.rectTransform.anchoredPosition;
            leftBrow.rectTransform.anchoredPosition =
                new Vector2(-style.eyeSeparation, style.eyeY + 31f);
            rightBrow.rectTransform.anchoredPosition =
                new Vector2(style.eyeSeparation, style.eyeY + 31f);

            ApplyExpression(baseExpression);
            ApplyBlink(0f);
            ApplyVisibility();
        }

        public void ConfigureAnimationDriver(
            CharacterAnimationDriver driver)
        {
            animationDriver = driver;
        }

        public void SetVisible(bool shouldShow)
        {
            requestedVisible = shouldShow;
            ApplyVisibility();
        }

        public void SetFacing(CharacterFacing nextFacing)
        {
            facing = nextFacing;
            ApplyVisibility();
            if (!built || faceRoot == null)
            {
                return;
            }

            bool side = facing == CharacterFacing.SideLeft ||
                        facing == CharacterFacing.SideRight;
            faceRoot.anchoredPosition = side
                ? new Vector2(
                    facing == CharacterFacing.SideLeft ? -25f : 25f,
                    88f)
                : new Vector2(0f, 88f);
            faceRoot.localScale = new Vector3(
                side ? 0.86f : 1f,
                1f,
                1f) * Mathf.Max(0.1f, style.overlayScale);

            bool hideLeft = facing == CharacterFacing.SideRight;
            bool hideRight = facing == CharacterFacing.SideLeft;
            leftEye.gameObject.SetActive(!hideLeft);
            leftEyelid.gameObject.SetActive(!hideLeft);
            leftBrow.gameObject.SetActive(!hideLeft);
            rightEye.gameObject.SetActive(!hideRight);
            rightEyelid.gameObject.SetActive(!hideRight);
            rightBrow.gameObject.SetActive(!hideRight);
        }

        public void LookAt(Vector2 normalizedDirection, float duration)
        {
            lookTarget = Vector2.ClampMagnitude(normalizedDirection, 1f);
            lookUntil = Time.unscaledTime + Mathf.Max(0.05f, duration);
        }

        public void SetExpression(
            CharacterExpression expression,
            float duration)
        {
            activeExpression = expression;
            expressionUntil =
                Time.unscaledTime + Mathf.Max(0.05f, duration);
        }

        public void ResetExpression()
        {
            activeExpression = baseExpression;
            expressionUntil = 0f;
            ApplyExpression(activeExpression);
        }

        public void ForceBlink(bool twice)
        {
            blinkStartedAt = Time.unscaledTime;
            doubleBlink = twice;
            secondBlinkTriggered = false;
            if (animationDriver != null &&
                animationDriver.IsReady)
            {
                animationDriver.TriggerFaceBlink();
            }
        }

        private void Update()
        {
            if (!built || !IsActuallyVisible)
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

            Vector2 desiredLook =
                now <= lookUntil ? lookTarget : Vector2.zero;
            currentLook = Vector2.Lerp(
                currentLook,
                desiredLook,
                1f - Mathf.Exp(-delta * 7.5f));
            Vector2 pupilOffset =
                new Vector2(currentLook.x * 7f, currentLook.y * 5f);
            leftPupil.rectTransform.anchoredPosition = pupilOffset;
            rightPupil.rectTransform.anchoredPosition = pupilOffset;

            if (expressionUntil > 0f && now > expressionUntil)
            {
                activeExpression = baseExpression;
                expressionUntil = 0f;
            }

            ApplyExpression(activeExpression);
            if (animationDriver != null &&
                animationDriver.IsReady)
            {
                if (doubleBlink &&
                    !secondBlinkTriggered &&
                    now - blinkStartedAt >= 0.17f)
                {
                    secondBlinkTriggered = true;
                    animationDriver.TriggerFaceBlink();
                }
            }
            else
            {
                ApplyBlink(CalculateBlinkAmount(now));
            }
        }

        private float CalculateBlinkAmount(float now)
        {
            float elapsed = now - blinkStartedAt;
            float first = BlinkPulse(elapsed);
            if (!doubleBlink)
            {
                return first;
            }

            return Mathf.Max(first, BlinkPulse(elapsed - 0.17f));
        }

        private static float BlinkPulse(float elapsed)
        {
            const float duration = 0.13f;
            if (elapsed < 0f || elapsed > duration)
            {
                return 0f;
            }

            float normalized = elapsed / duration;
            float triangle = 1f - Mathf.Abs(normalized * 2f - 1f);
            return Mathf.SmoothStep(0f, 1f, triangle);
        }

        private void ApplyBlink(float amount)
        {
            float scaleY = Mathf.Lerp(0.035f, 1f, amount);
            leftEyelid.rectTransform.localScale =
                new Vector3(1f, scaleY, 1f);
            rightEyelid.rectTransform.localScale =
                new Vector3(1f, scaleY, 1f);
        }

        private void ApplyExpression(CharacterExpression expression)
        {
            if (!built)
            {
                return;
            }

            CharacterMouthShape mouthShape = expression switch
            {
                CharacterExpression.Relaxed => CharacterMouthShape.Relaxed,
                CharacterExpression.Tired => CharacterMouthShape.Frown,
                CharacterExpression.Focused => CharacterMouthShape.Focused,
                CharacterExpression.Happy => CharacterMouthShape.Smile,
                CharacterExpression.Strain => CharacterMouthShape.Strain,
                CharacterExpression.Yawn => CharacterMouthShape.Yawn,
                _ => CharacterMouthShape.Neutral
            };
            CurrentMouthShape = mouthShape;

            float leftBrowAngle = 0f;
            float rightBrowAngle = 0f;
            float mouthAngle = 0f;
            Vector2 mouthSize = new Vector2(62f, 7f);
            Vector2 openSize = new Vector2(40f, 14f);
            float openAlpha = 0f;
            float cheekAlpha = 0f;
            float sweatAlpha = 0f;

            switch (mouthShape)
            {
                case CharacterMouthShape.Frown:
                    leftBrowAngle = -9f;
                    rightBrowAngle = 9f;
                    mouthAngle = 180f;
                    break;
                case CharacterMouthShape.Focused:
                    leftBrowAngle = 10f;
                    rightBrowAngle = -10f;
                    mouthSize = new Vector2(47f, 8f);
                    break;
                case CharacterMouthShape.Smile:
                    leftBrowAngle = 4f;
                    rightBrowAngle = -4f;
                    mouthSize = new Vector2(69f, 10f);
                    mouthAngle = -4f;
                    cheekAlpha = style.cheek.a;
                    break;
                case CharacterMouthShape.Strain:
                    leftBrowAngle = 15f;
                    rightBrowAngle = -15f;
                    mouthSize = new Vector2(54f, 11f);
                    openSize = new Vector2(49f, 18f);
                    openAlpha = 0.72f;
                    sweatAlpha = 0.92f;
                    break;
                case CharacterMouthShape.Yawn:
                    leftBrowAngle = -4f;
                    rightBrowAngle = 4f;
                    openSize = new Vector2(51f, 59f);
                    openAlpha = 1f;
                    mouthSize = new Vector2(22f, 6f);
                    break;
                case CharacterMouthShape.Relaxed:
                    mouthSize = new Vector2(51f, 7f);
                    break;
            }

            leftBrow.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, leftBrowAngle);
            rightBrow.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, rightBrowAngle);
            mouthLine.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, mouthAngle);
            mouthLine.SetSize(mouthSize);
            mouthOpen.SetSize(openSize);
            SetAlpha(mouthOpen, openAlpha);
            SetAlpha(leftCheek, cheekAlpha);
            SetAlpha(rightCheek, cheekAlpha);
            SetAlpha(sweatDrop, sweatAlpha);
        }

        private void ApplyVisibility()
        {
            if (faceRoot != null)
            {
                faceRoot.gameObject.SetActive(
                    requestedVisible &&
                    facing != CharacterFacing.Back);
            }
        }

        private void ScheduleBlink()
        {
            nextBlinkAt =
                Time.unscaledTime + Random.Range(2.5f, 6f);
        }

        private static RectTransform CreateRect(
            Transform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            target.layer = parent.gameObject.layer;
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static CharacterMeshGraphic CreateMesh(
            Transform parent,
            string name,
            CharacterMeshShape shape,
            CharacterVisualRole role,
            Vector2 position,
            Vector2 size,
            Color fill,
            float outline)
        {
            RectTransform rect = CreateRect(parent, name, position, size);
            CanvasRenderer renderer =
                rect.gameObject.AddComponent<CanvasRenderer>();
            renderer.cullTransparentMesh = false;
            CharacterMeshGraphic graphic =
                rect.gameObject.AddComponent<CharacterMeshGraphic>();
            graphic.Configure(
                shape,
                role,
                size,
                new Vector2(0.5f, 0.5f),
                fill,
                new Color(0.075f, 0.045f, 0.035f, 1f),
                outline);
            return graphic;
        }

        private static void SetColor(
            CharacterMeshGraphic graphic,
            Color color)
        {
            if (graphic != null)
            {
                graphic.SetFill(color);
            }
        }

        private static void SetAlpha(
            CharacterMeshGraphic graphic,
            float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            Color color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.SetFill(color);
        }
    }
}
