using System;
using System.Collections;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Opens the room after the independent generated bone rig has stable bounds.
    /// The intact legacy sprite remains a compatibility fallback only when the
    /// generated host is absent. A timed fail-open guarantees that character
    /// diagnostics can never leave the room permanently black.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(9000)]
    public sealed class CharacterVisibilityGate : MonoBehaviour
    {
        private const int MaximumFitAttempts = 4;
        private const float SafeVisibleMinimum = 0.075f;
        private const float SafeVisibleMaximum = 0.92f;
        private const float AbsoluteFailOpenDelay = 2.75f;

        private RectTransform characterRoot;
        private CharacterRigController rigController;
        private CharacterSkinController skinController;
        private CharacterRigValidator validator;
        private GeneratedFatManRigHost generatedRigHost;
        private CharacterSpriteRigController spriteRigController;
        private float minimumHeightFraction = 0.34f;
        private float maximumHeightFraction = 0.50f;
        private int stableFrames;
        private int fitAttempts;
        private bool validationRequested;
        private bool fallbackWarningLogged;
        private float validationStartedAt;

        public bool IsReady { get; private set; }
        public string LastError { get; private set; } =
            "Visibility has not been validated.";
        public float LastHeightFraction { get; private set; }
        public bool UsedVisibleSpriteFallback { get; private set; }

        public void Configure(
            RectTransform root,
            CharacterRigController rig,
            CharacterSkinController skin,
            CharacterRigValidator rigValidator,
            float minimumScreenHeight = 0.34f,
            float maximumScreenHeight = 0.50f)
        {
            characterRoot = root;
            rigController = rig;
            skinController = skin;
            validator = rigValidator;
            generatedRigHost = root != null
                ? root.GetComponent<GeneratedFatManRigHost>()
                : null;
            spriteRigController = root != null
                ? root.GetComponent<CharacterSpriteRigController>()
                : null;
            minimumHeightFraction =
                Mathf.Clamp(minimumScreenHeight, 0.05f, 0.9f);
            maximumHeightFraction = Mathf.Clamp(
                maximumScreenHeight,
                minimumHeightFraction + 0.01f,
                1f);
            BeginValidation();
        }

        public void BeginValidation()
        {
            IsReady = false;
            stableFrames = 0;
            fitAttempts = 0;
            UsedVisibleSpriteFallback = false;
            fallbackWarningLogged = false;
            validationRequested = true;
            validationStartedAt = Time.unscaledTime;
            LastError = "Waiting for two stable rendered frames.";
        }

        public bool EvaluateNow(out string error)
        {
            if (characterRoot == null ||
                !characterRoot.gameObject.activeInHierarchy)
            {
                return FailOrWait(
                    "CharacterRoot is inactive.",
                    out error);
            }

            if (skinController == null ||
                skinController.CurrentArtIndex < 0)
            {
                return FailOrWait(
                    "The saved body stage has not been applied.",
                    out error);
            }

            if (rigController == null)
            {
                return FailOrWait(
                    "CharacterRigController is missing.",
                    out error);
            }

            if (Mathf.Approximately(
                    characterRoot.lossyScale.x,
                    0f) ||
                Mathf.Approximately(
                    characterRoot.lossyScale.y,
                    0f))
            {
                return FailOrWait(
                    "CharacterRoot has a zero world scale.",
                    out error);
            }

            generatedRigHost ??=
                characterRoot.GetComponent<GeneratedFatManRigHost>();
            spriteRigController ??=
                characterRoot.GetComponent<CharacterSpriteRigController>();

            bool generatedCandidate = generatedRigHost != null;
            bool generatedPath =
                generatedCandidate && generatedRigHost.IsReady;
            bool legacySpritePath =
                !generatedCandidate &&
                spriteRigController != null &&
                spriteRigController.IsReady;

            Bounds worldBounds;
            if (generatedPath)
            {
                if (!generatedRigHost.TryGetWorldBounds(
                        out worldBounds))
                {
                    return FailOrWait(
                        "The independent fat-man bone rig has no " +
                        "visible UI bounds.",
                        out error);
                }
            }
            else if (generatedCandidate)
            {
                return FailOrWait(
                    "Waiting for the independent generated fat-man " +
                    "bone rig.",
                    out error);
            }
            else if (legacySpritePath)
            {
                if (!spriteRigController.TryGetWorldBounds(
                        out worldBounds))
                {
                    return FailOrWait(
                        "The intact compatibility sprite has no " +
                        "visible bounds.",
                        out error);
                }
            }
            else
            {
                if (validator == null ||
                    !validator.ValidateNow(false))
                {
                    return FailOrWait(
                        validator != null
                            ? validator.LastError
                            : "CharacterRigValidator is missing.",
                        out error);
                }

                worldBounds =
                    rigController.GetWorldGeometryBounds();
                if (worldBounds.size.x <= 10f ||
                    worldBounds.size.y <= 10f)
                {
                    return FailOrWait(
                        "Character mesh bounds are empty.",
                        out error);
                }
            }

            if (Screen.width <= 1 ||
                Screen.height <= 1)
            {
                error = string.Empty;
                return true;
            }

            Canvas canvas =
                characterRoot.GetComponentInParent<Canvas>();
            Camera camera = canvas != null &&
                            canvas.renderMode !=
                            RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector2 screenMin =
                RectTransformUtility.WorldToScreenPoint(
                    camera,
                    worldBounds.min);
            Vector2 screenMax =
                RectTransformUtility.WorldToScreenPoint(
                    camera,
                    worldBounds.max);
            Rect characterRect = Rect.MinMaxRect(
                Mathf.Min(screenMin.x, screenMax.x),
                Mathf.Min(screenMin.y, screenMax.y),
                Mathf.Max(screenMin.x, screenMax.x),
                Mathf.Max(screenMin.y, screenMax.y));
            Rect screenRect = new Rect(
                0f,
                0f,
                Screen.width,
                Screen.height);
            if (!characterRect.Overlaps(screenRect, true))
            {
                return FailOrWait(
                    "Character bounds do not intersect the screen.",
                    out error);
            }

            LastHeightFraction =
                characterRect.height / Screen.height;
            bool idealSize =
                LastHeightFraction >= minimumHeightFraction &&
                LastHeightFraction <= maximumHeightFraction;
            if (idealSize)
            {
                error = string.Empty;
                return true;
            }

            bool fittedPath =
                generatedPath || legacySpritePath;
            if (fittedPath &&
                fitAttempts < MaximumFitAttempts)
            {
                fitAttempts++;
                float target =
                    (minimumHeightFraction +
                     maximumHeightFraction) * 0.5f;
                bool fitted = generatedPath
                    ? generatedRigHost.FitToScreenHeight(target)
                    : spriteRigController.FitToScreenHeight(target);
                if (fitted)
                {
                    error =
                        $"Calibrating character height from " +
                        $"{LastHeightFraction:P0} toward {target:P0}.";
                    return false;
                }
            }

            if (fittedPath &&
                LastHeightFraction >= SafeVisibleMinimum &&
                LastHeightFraction <= SafeVisibleMaximum)
            {
                UsedVisibleSpriteFallback = true;
                error = string.Empty;
                return true;
            }

            return FailOrWait(
                $"Character height is {LastHeightFraction:P0}; " +
                $"expected {minimumHeightFraction:P0}–" +
                $"{maximumHeightFraction:P0}.",
                out error);
        }

        public IEnumerator WaitUntilReady(
            float timeout,
            Action<bool, string> completion)
        {
            BeginValidation();
            float deadline =
                Time.unscaledTime + Mathf.Max(0.1f, timeout);
            while (!IsReady &&
                   Time.unscaledTime < deadline)
            {
                yield return null;
            }

            completion?.Invoke(IsReady, LastError);
        }

        private bool FailOrWait(
            string diagnostic,
            out string error)
        {
            if (validationRequested &&
                Time.unscaledTime - validationStartedAt >=
                AbsoluteFailOpenDelay)
            {
                UsedVisibleSpriteFallback = true;
                error = string.Empty;
                LastError = diagnostic;
                return true;
            }

            error = diagnostic;
            return false;
        }

        private void LateUpdate()
        {
            if (!validationRequested || IsReady)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            if (EvaluateNow(out string error))
            {
                stableFrames++;
                if (stableFrames >= 2)
                {
                    IsReady = true;
                    validationRequested = false;
                    if (UsedVisibleSpriteFallback &&
                        !fallbackWarningLogged)
                    {
                        fallbackWarningLogged = true;
                        Debug.LogWarning(
                            "CharacterVisibilityGate 3.8 used the " +
                            "timed/visible fail-open. The room was " +
                            "revealed instead of leaving a black screen. " +
                            "Last diagnostic: " + LastError,
                            this);
                    }
                    else
                    {
                        LastError = string.Empty;
                    }
                }
            }
            else
            {
                stableFrames = 0;
                LastError = error;
            }
        }
    }
}
