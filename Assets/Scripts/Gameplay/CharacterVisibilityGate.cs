using System;
using System.Collections;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Keeps the room covered until real character geometry survives two full
    /// layout/render frames. Logical flags and alpha alone never open the gate.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(9000)]
    public sealed class CharacterVisibilityGate : MonoBehaviour
    {
        private RectTransform characterRoot;
        private CharacterRigController rigController;
        private CharacterSkinController skinController;
        private CharacterRigValidator validator;
        private float minimumHeightFraction = 0.34f;
        private float maximumHeightFraction = 0.50f;
        private int stableFrames;
        private bool validationRequested;

        public bool IsReady { get; private set; }
        public string LastError { get; private set; } =
            "Visibility has not been validated.";
        public float LastHeightFraction { get; private set; }

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

            if (validator == null || !validator.ValidateNow(false))
            {
                error = validator != null
                    ? validator.LastError
                    : "CharacterRigValidator is missing.";
                return false;
            }

            if (characterRoot.lossyScale.x == 0f ||
                characterRoot.lossyScale.y == 0f)
            {
                error = "CharacterRoot has a zero world scale.";
                return false;
            }

            Bounds worldBounds =
                rigController.GetWorldGeometryBounds();
            if (worldBounds.size.x <= 10f ||
                worldBounds.size.y <= 10f)
            {
                error = "Character mesh bounds are empty.";
                return false;
            }

            if (Screen.width > 1 && Screen.height > 1)
            {
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
                Rect screenRect =
                    new Rect(0f, 0f, Screen.width, Screen.height);
                if (!characterRect.Overlaps(screenRect, true))
                {
                    error =
                        "Character bounds do not intersect the screen.";
                    return false;
                }

                LastHeightFraction =
                    characterRect.height / Screen.height;
                if (LastHeightFraction < minimumHeightFraction ||
                    LastHeightFraction > maximumHeightFraction)
                {
                    error =
                        $"Character height is {LastHeightFraction:P0}; " +
                        $"expected {minimumHeightFraction:P0}–" +
                        $"{maximumHeightFraction:P0}.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
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
