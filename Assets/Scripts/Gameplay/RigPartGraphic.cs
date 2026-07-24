using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RigPartGraphic : MaskableGraphic
    {
        private Texture sourceTexture;
        private Rect sourceUv;
        private Vector2[] polygon;
        private Vector2 sourcePivot;
        private Vector2 visualSize;

        public Texture SourceTexture => sourceTexture;
        public override Texture mainTexture =>
            sourceTexture != null ? sourceTexture : s_WhiteTexture;

        public void Configure(
            Texture texture,
            Rect textureUv,
            CharacterRigCrop crop,
            Vector2 size)
        {
            sourceTexture = texture;
            sourceUv = textureUv;
            sourcePivot = crop.Pivot;
            visualSize = size;
            polygon = crop.Polygon;
            rectTransform.sizeDelta = visualSize;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            raycastTarget = false;
            color = Color.white;
            SetVerticesDirty();
            SetMaterialDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (sourceTexture == null || polygon == null || polygon.Length < 3)
            {
                return;
            }

            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 point = polygon[i];
                UIVertex vertex = UIVertex.simpleVert;
                vertex.color = color;
                vertex.position = Vector2.Scale(point - sourcePivot, visualSize);
                vertex.uv0 = new Vector4(
                    sourceUv.x + point.x * sourceUv.width,
                    sourceUv.y + point.y * sourceUv.height,
                    0f,
                    0f);
                vertexHelper.AddVert(vertex);
            }

            for (int i = 1; i < polygon.Length - 1; i++)
            {
                vertexHelper.AddTriangle(0, i, i + 1);
            }
        }
    }
}
