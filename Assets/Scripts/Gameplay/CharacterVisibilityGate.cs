using System;
using System.Collections;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Opens the room only after the pixels the player actually sees survive
    /// two layout frames. Patch 3.3 measures CharacterSpriteRigController
    /// instead of the hidden procedural skeleton, automatically fits the real
    /// body to the requested screen range and fails open when a visible sprite
    /// is present so a diagnostic can never leave the player on a black screen.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(9000)]
    public sealed class CharacterVisibilityGate : MonoBehaviour
    {
        private const int MaximumFitAttempts = 4;
        private const float SafeVisibleMinimum = 0.075f;
        private const float SafeVisibleMaximum = 0.92f;

        private RectTransform characterRoot;
        private CharacterRigController rigController;
        private CharacterSkinController skinController;
        private CharacterRigValidator validator;
        private CharacterSpriteRigController spriteRigController;
        private CharacterLayeredRigController layeredRigController;
        private float minimumHeightFraction = 0.34f;
        private float maximumHeightFraction = 0.50f;
        private int stableFrames;
        private int fitAttempts;
        private bool validationRequested;
        private bool fallbackWarningLogged;

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
            spriteRigController =
                root != null
                    ? root.GetComponent<CharacterSpriteRigController>()
                    : null;
            layeredRigController =
                root != null
                    ? root.GetComponent<CharacterLayeredRigController>()
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
            LastError = "Waiting for two stable rendered frames.";
        }

        public bool EvaluateNow(out string error)
        {
            if (characterRoot == null ||
                !characterRoot.gameObject.activeInHierarchy)
            {
                error = "CharacterRoot is inactive.";
                return false;
            }

            if (skinController == null ||
                skinController.CurrentArtIndex < 0)
            {
                error = "The saved body stage has not been applied.";
                return false;
            }

            if (rigController == null)
            {
                error = "CharacterRigController is missing.";
                return false;
            }

            if (characterRoot.lossyScale.x == 0f ||
                characterRoot.lossyScale.y == 0f)
            {
                error = "CharacterRoot has a zero world scale.";
                return false;
            }

            spriteRigController ??=
                characterRoot.GetComponent<CharacterSpriteRigController>();

            bool realSpritePath = spriteRigController != null;
            Bounds worldBounds;
            if (realSpritePath)
            {
                if (!spriteRigController.IsReady)
                {
                    error = "Waiting for the intact real fat-man sprite.";
                    return false;
                }

                layeredRigController ??=
                    characterRoot.GetComponent<CharacterLayeredRigController>();
                if (layeredRigController != null && !layeredRigController.IsReady)
                {
                    error = "Waiting for the final bounded legacy puppet.";
                    return false;
                }

                if (!spriteRigController.TryGetWorldBounds(out worldBounds))
                {
                    error = "The real fat-man sprite has no visible bounds.";
                    return false;
                }
            }
            else
            {
                if (validator == null || !validator.ValidateNow(false))
                {
                    error = validator != null
                        ? validator.LastError
                        : "CharacterRigValidator is missing.";
                    return false;
                }

                worldBounds = rigController.GetWorldGeometryBounds();
                if (worldBounds.size.x <= 10f ||
                    worldBounds.size.y <= 10f)
                {
                    error = "Character mesh bounds are empty.";
                    return false;
                }
            }

            if (Screen.width <= 1 || Screen.height <= 1)
            {
                error = string.Empty;
                return true;
            }

            Canvas canvas = characterRoot.GetComponentInParent<Canvas>();
            Camera camera = canvas != null &&
                            canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(
                camera,
                worldBounds.min);
            Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(
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
                error = "Character bounds do not intersect the screen.";
                return false;
            }

            LastHeightFraction = characterRect.height / Screen.height;
            bool idealSize =
                LastHeightFraction >= minimumHeightFraction &&
                LastHeightFraction <= maximumHeightFraction;
            if (idealSize)
            {
                error = string.Empty;
                return true;
            }

            if (realSpritePath && fitAttempts < MaximumFitAttempts)
            {
                fitAttempts++;
                float target =
                    (minimumHeightFraction + maximumHeightFraction) * 0.5f;
                if (spriteRigController.FitToScreenHeight(target))
                {
                    error =
                        $"Calibrating real character height from " +
                        $"{LastHeightFraction:P0} toward {target:P0}.";
                    return false;
                }
            }

            // A visible intact sprite is always safer than permanently covering
            // the room. Keep the diagnostic, but allow the transition to finish.
            if (realSpritePath &&
                LastHeightFraction >= SafeVisibleMinimum &&
                LastHeightFraction <= SafeVisibleMaximum)
            {
                UsedVisibleSpriteFallback = true;
                error = string.Empty;
                return true;
            }

            error =
                $"Character height is {LastHeightFraction:P0}; " +
                $"expected {minimumHeightFraction:P0}–" +
                $"{maximumHeightFraction:P0}.";
            return false;
        }

        public IEnumerator WaitUntilReady(
            float timeout,
            Action<bool, string> completion)
        {
            BeginValidation();
            float deadline =
                Time.unscaledTime + Mathf.Max(0.1f, timeout);
            while (!IsReady && Time.unscaledTime < deadline)
            {
                yield return null;
            }

            completion?.Invoke(IsReady, LastError);
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
                LastError = string.Empty;
                if (stableFrames >= 2)
                {
                    IsReady = true;
                    validationRequested = false;
                    if (UsedVisibleSpriteFallback &&
                        !fallbackWarningLogged)
                    {
                        fallbackWarningLogged = true;
                        Debug.LogWarning(
                            "CharacterVisibilityGate used the visible-sprite " +
                            $"fail-open at {LastHeightFraction:P0}. The room " +
                            "was revealed instead of leaving a black screen.",
                            this);
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
