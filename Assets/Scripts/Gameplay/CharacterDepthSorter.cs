using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CharacterDepthSorter : MonoBehaviour
    {
        private RectTransform characterRoot;
        private RectTransform shadow;
        private Graphic shadowGraphic;
        private float nearY = 850f;
        private float farY = 1250f;
        private bool configured;

        public void Configure(
            RectTransform root,
            RectTransform shadowTransform,
            float nearDepthY,
            float farDepthY)
        {
            characterRoot = root;
            shadow = shadowTransform;
            shadowGraphic = shadow != null ? shadow.GetComponent<Graphic>() : null;
            nearY = nearDepthY;
            farY = Mathf.Max(nearDepthY + 1f, farDepthY);
            configured = characterRoot != null;
        }

        private void LateUpdate()
        {
            if (!configured)
            {
                return;
            }

            float depth = Mathf.InverseLerp(
                nearY,
                farY,
                characterRoot.anchoredPosition.y);
            if (shadow != null)
            {
                float width = Mathf.Lerp(1f, 0.68f, depth);
                float height = Mathf.Lerp(1f, 0.58f, depth);
                shadow.localScale = new Vector3(width, height, 1f);
            }

            if (shadowGraphic != null)
            {
                Color color = shadowGraphic.color;
                color.a = Mathf.Lerp(0.34f, 0.17f, depth);
                shadowGraphic.color = color;
            }

            // UI renders siblings in hierarchy order. Mapping Y into the available
            // actor-layer slots keeps nearer actors in front when more characters
            // or foreground props are added.
            Transform parent = characterRoot.parent;
            if (parent != null && parent.childCount > 1)
            {
                int target = Mathf.RoundToInt(
                    Mathf.Lerp(parent.childCount - 1, 0, depth));
                characterRoot.SetSiblingIndex(
                    Mathf.Clamp(target, 0, parent.childCount - 1));
            }
        }
    }
}
