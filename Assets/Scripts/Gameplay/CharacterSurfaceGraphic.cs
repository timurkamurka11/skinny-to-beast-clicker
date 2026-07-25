using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Stable uGUI surface for one artistic cutout part. It mirrors the exact
    /// vector silhouette owned by CharacterMeshGraphic without creating a
    /// second rig, sampling a texture, or swapping animation frames.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class CharacterSurfaceGraphic : MaskableGraphic
    {
        [SerializeField] private CharacterMeshShape shape;
        [SerializeField, Range(0.35f, 1.5f)] private float topWidth = 1f;
        [SerializeField, Range(0.35f, 1.5f)] private float bottomWidth = 1f;

        private readonly List<Vector2> boundary = new(40);

        public bool IsRenderable =>
            isActiveAndEnabled &&
            color.a > 0.001f &&
            rectTransform.rect.width > 0.5f &&
            rectTransform.rect.height > 0.5f &&
            canvasRenderer != null;

        public void Configure(
            CharacterMeshShape nextShape,
            Color fill,
            float topRatio,
            float bottomRatio)
        {
            shape = nextShape;
            topWidth = Mathf.Clamp(topRatio, 0.35f, 1.5f);
            bottomWidth = Mathf.Clamp(bottomRatio, 0.35f, 1.5f);
            color = fill;
            raycastTarget = false;
            maskable = false;
            canvasRenderer.cullTransparentMesh = false;
            SetAllDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            canvasRenderer.cullTransparentMesh = false;
            SetAllDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = rectTransform.rect;
            if (rect.width <= 0.5f ||
                rect.height <= 0.5f ||
                color.a <= 0.001f)
            {
                return;
            }

            CharacterShapeGeometry.BuildBoundary(
                shape,
                rect,
                topWidth,
                bottomWidth,
                boundary);
            if (boundary.Count < 3)
            {
                return;
            }

            int start = vertexHelper.currentVertCount;
            Vector2 center = Vector2.zero;
            for (int i = 0; i < boundary.Count; i++)
            {
                center += boundary[i];
            }

            center /= boundary.Count;
            vertexHelper.AddVert(
                center,
                color,
                new Vector2(0.5f, 0.5f));
            for (int i = 0; i < boundary.Count; i++)
            {
                vertexHelper.AddVert(
                    boundary[i],
                    color,
                    Vector2.zero);
            }

            for (int i = 0; i < boundary.Count; i++)
            {
                int next = (i + 1) % boundary.Count;
                vertexHelper.AddTriangle(
                    start,
                    start + 1 + i,
                    start + 1 + next);
            }
        }
    }
}
