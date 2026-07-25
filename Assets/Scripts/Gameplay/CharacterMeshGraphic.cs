using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    public enum CharacterMeshShape
    {
        Capsule,
        Ellipse,
        Torso,
        Shoe,
        Hair,
        Brow,
        Mouth,
        FatThigh,
        FatCalf,
        FatPelvis,
        FatBelly,
        FatChest,
        FatShoulder,
        FatUpperArm,
        FatForearm,
        FatHand,
        FatNeck,
        FatHead,
        DoubleChin,
        MessyHair,
        WornShoe,
        ShirtHem,
        Neckline,
        BellyBand,
        Waistband,
        Pocket,
        FabricFold,
        Stain,
        Ear,
        Nose
    }

    public enum CharacterVisualRole
    {
        Skin,
        SkinShadow,
        SkinHighlight,
        Hair,
        Top,
        TopShadow,
        TopHighlight,
        TopStain,
        Bottom,
        BottomShadow,
        BottomDetail,
        Shoe,
        ShoeDetail,
        Accent,
        EyeWhite,
        Iris,
        Brow,
        Mouth,
        Cheek
    }

    /// <summary>
    /// Pure vector UI geometry used by the skeletal character. It never samples
    /// a texture, sprite sheet or source PNG, so every visible pose is produced
    /// by the bone hierarchy rather than by swapping raster frames.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CharacterMeshGraphic : MaskableGraphic
    {
        [SerializeField] private CharacterMeshShape shape;
        [SerializeField] private CharacterVisualRole role;
        [SerializeField] private Color outlineColor =
            new Color(0.075f, 0.055f, 0.055f, 1f);
        [SerializeField, Range(0f, 18f)] private float outlineWidth = 5f;
        [SerializeField, Range(0.35f, 1.5f)] private float topWidth = 1f;
        [SerializeField, Range(0.35f, 1.5f)] private float bottomWidth = 1f;

        private readonly List<Vector2> boundary = new(32);
        private readonly List<Vector2> insetBoundary = new(32);
        private CharacterPartSurface stableSurface;

        public CharacterMeshShape Shape => shape;
        public CharacterVisualRole Role => role;
        public bool HasRenderableGeometry =>
            isActiveAndEnabled &&
            color.a > 0.001f &&
            rectTransform.rect.width > 0.5f &&
            rectTransform.rect.height > 0.5f &&
            HasRequiredCanvasRenderer &&
            stableSurface != null &&
            stableSurface.IsRenderable;
        public bool HasLiveRenderSurface =>
            stableSurface != null &&
            stableSurface.IsRenderable;
        public bool HasRequiredCanvasRenderer =>
            GetComponent<CanvasRenderer>() != null;

        protected override void OnEnable()
        {
            CanvasRenderer renderer = EnsureCanvasRenderer();
            base.OnEnable();
            renderer.cullTransparentMesh = false;
            EnsureStableSurface().Configure(
                shape,
                rectTransform.sizeDelta,
                color,
                outlineColor,
                outlineWidth,
                topWidth,
                bottomWidth);
            SetAllDirty();
        }

        public void Configure(
            CharacterMeshShape nextShape,
            CharacterVisualRole nextRole,
            Vector2 size,
            Vector2 pivot,
            Color fill,
            Color outline,
            float border = 5f,
            float topRatio = 1f,
            float bottomRatio = 1f)
        {
            shape = nextShape;
            role = nextRole;
            outlineColor = outline;
            outlineWidth = Mathf.Max(0f, border);
            topWidth = Mathf.Clamp(topRatio, 0.35f, 1.5f);
            bottomWidth = Mathf.Clamp(bottomRatio, 0.35f, 1.5f);
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = new Vector2(
                Mathf.Max(1f, size.x),
                Mathf.Max(1f, size.y));
            color = fill;
            raycastTarget = false;
            maskable = false;
            EnsureCanvasRenderer().cullTransparentMesh = false;
            EnsureStableSurface().Configure(
                shape,
                size,
                fill,
                outline,
                outlineWidth,
                topWidth,
                bottomWidth);
            SetAllDirty();
        }

        public void SetFill(Color fill)
        {
            color = fill;
            EnsureStableSurface().SetFill(fill);
            SetAllDirty();
        }

        public void SetOutline(Color outline)
        {
            outlineColor = outline;
            EnsureStableSurface().SetOutline(outline);
            SetAllDirty();
        }

        public void SetSize(Vector2 size)
        {
            rectTransform.sizeDelta = new Vector2(
                Mathf.Max(1f, size.x),
                Mathf.Max(1f, size.y));
            EnsureStableSurface().SetSize(size);
            SetAllDirty();
        }

        public void SetWidthProfile(float topRatio, float bottomRatio)
        {
            topWidth = Mathf.Clamp(topRatio, 0.35f, 1.5f);
            bottomWidth = Mathf.Clamp(bottomRatio, 0.35f, 1.5f);
            EnsureStableSurface().SetWidthProfile(
                topWidth,
                bottomWidth);
            SetAllDirty();
        }

        public void ForceRenderRefresh()
        {
            EnsureCanvasRenderer().cullTransparentMesh = false;
            EnsureStableSurface().ForceRefresh();
            SetAllDirty();
        }

        private CanvasRenderer EnsureCanvasRenderer()
        {
            CanvasRenderer renderer =
                GetComponent<CanvasRenderer>();
            if (renderer == null)
            {
                renderer =
                    gameObject.AddComponent<CanvasRenderer>();
            }

            return renderer;
        }

        private CharacterPartSurface EnsureStableSurface()
        {
            if (stableSurface == null)
            {
                stableSurface =
                    GetComponent<CharacterPartSurface>();
            }

            if (stableSurface == null)
            {
                stableSurface =
                    gameObject.AddComponent<CharacterPartSurface>();
            }

            return stableSurface;
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = rectTransform.rect;
            if (rect.width <= 0.5f || rect.height <= 0.5f || color.a <= 0f)
            {
                return;
            }

            BuildBoundary(rect, boundary);
            if (boundary.Count < 3)
            {
                return;
            }

            if (outlineWidth > 0.01f && outlineColor.a > 0.001f)
            {
                AddFan(vertexHelper, boundary, outlineColor);
                BuildInset(boundary, rect.center, rect, insetBoundary);
                AddFan(vertexHelper, insetBoundary, color);
            }
            else
            {
                AddFan(vertexHelper, boundary, color);
            }
        }

        private void BuildBoundary(Rect rect, List<Vector2> points)
        {
            CharacterShapeGeometry.BuildBoundary(
                shape,
                rect,
                topWidth,
                bottomWidth,
                points);
        }

        private static void BuildEllipse(
            Rect rect,
            List<Vector2> points,
            int segments)
        {
            Vector2 center = rect.center;
            float radiusX = rect.width * 0.5f;
            float radiusY = rect.height * 0.5f;
            for (int i = 0; i < segments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / segments;
                points.Add(center + new Vector2(
                    Mathf.Cos(angle) * radiusX,
                    Mathf.Sin(angle) * radiusY));
            }
        }

        private static void BuildCapsule(Rect rect, List<Vector2> points)
        {
            const int arcSegments = 10;
            Vector2 center = rect.center;
            if (rect.height >= rect.width)
            {
                float radius = rect.width * 0.5f;
                float straight = Mathf.Max(0f, rect.height * 0.5f - radius);
                for (int i = 0; i <= arcSegments; i++)
                {
                    float angle = Mathf.PI * i / arcSegments;
                    points.Add(center + new Vector2(
                        Mathf.Cos(angle) * radius,
                        straight + Mathf.Sin(angle) * radius));
                }

                for (int i = 0; i <= arcSegments; i++)
                {
                    float angle = Mathf.PI + Mathf.PI * i / arcSegments;
                    points.Add(center + new Vector2(
                        Mathf.Cos(angle) * radius,
                        -straight + Mathf.Sin(angle) * radius));
                }
            }
            else
            {
                float radius = rect.height * 0.5f;
                float straight = Mathf.Max(0f, rect.width * 0.5f - radius);
                for (int i = 0; i <= arcSegments; i++)
                {
                    float angle = -Mathf.PI * 0.5f +
                                  Mathf.PI * i / arcSegments;
                    points.Add(center + new Vector2(
                        straight + Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius));
                }

                for (int i = 0; i <= arcSegments; i++)
                {
                    float angle = Mathf.PI * 0.5f +
                                  Mathf.PI * i / arcSegments;
                    points.Add(center + new Vector2(
                        -straight + Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius));
                }
            }
        }

        private void BuildTorso(Rect rect, List<Vector2> points)
        {
            float halfWidth = rect.width * 0.5f;
            float top = rect.yMax;
            float bottom = rect.yMin;
            float upper = Mathf.Min(halfWidth * topWidth, rect.width * 0.72f);
            float lower = Mathf.Min(halfWidth * bottomWidth, rect.width * 0.72f);
            float shoulderY = Mathf.Lerp(top, bottom, 0.18f);
            float waistY = Mathf.Lerp(top, bottom, 0.78f);
            float round = Mathf.Min(rect.width, rect.height) * 0.08f;

            points.Add(new Vector2(-upper + round, top));
            points.Add(new Vector2(upper - round, top));
            points.Add(new Vector2(upper, top - round));
            points.Add(new Vector2(upper * 1.05f, shoulderY));
            points.Add(new Vector2(lower, waistY));
            points.Add(new Vector2(lower - round, bottom));
            points.Add(new Vector2(-lower + round, bottom));
            points.Add(new Vector2(-lower, waistY));
            points.Add(new Vector2(-upper * 1.05f, shoulderY));
            points.Add(new Vector2(-upper, top - round));
        }

        private static void BuildShoe(Rect rect, List<Vector2> points)
        {
            float width = rect.width;
            float height = rect.height;
            points.Add(new Vector2(rect.xMin + width * 0.08f, rect.yMax));
            points.Add(new Vector2(rect.xMax - width * 0.28f, rect.yMax));
            points.Add(new Vector2(rect.xMax - width * 0.04f, rect.yMin + height * 0.42f));
            points.Add(new Vector2(rect.xMax, rect.yMin + height * 0.18f));
            points.Add(new Vector2(rect.xMax - width * 0.08f, rect.yMin));
            points.Add(new Vector2(rect.xMin + width * 0.06f, rect.yMin));
            points.Add(new Vector2(rect.xMin, rect.yMin + height * 0.28f));
        }

        private static void BuildHair(Rect rect, List<Vector2> points)
        {
            Vector2 center = rect.center;
            float radiusX = rect.width * 0.5f;
            float radiusY = rect.height * 0.5f;
            const int arcSegments = 14;
            for (int i = 0; i <= arcSegments; i++)
            {
                float angle = Mathf.PI * i / arcSegments;
                points.Add(center + new Vector2(
                    Mathf.Cos(angle) * radiusX,
                    Mathf.Sin(angle) * radiusY));
            }

            points.Add(new Vector2(rect.xMin + rect.width * 0.08f, rect.yMin));
            points.Add(new Vector2(rect.xMin + rect.width * 0.18f, rect.yMin + rect.height * 0.24f));
            points.Add(new Vector2(rect.xMin + rect.width * 0.29f, rect.yMin));
            points.Add(new Vector2(rect.xMin + rect.width * 0.41f, rect.yMin + rect.height * 0.20f));
            points.Add(new Vector2(rect.xMin + rect.width * 0.53f, rect.yMin));
            points.Add(new Vector2(rect.xMin + rect.width * 0.66f, rect.yMin + rect.height * 0.18f));
            points.Add(new Vector2(rect.xMin + rect.width * 0.80f, rect.yMin));
            points.Add(new Vector2(rect.xMax - rect.width * 0.05f, rect.yMin + rect.height * 0.22f));
        }

        private void BuildInset(
            List<Vector2> source,
            Vector2 center,
            Rect rect,
            List<Vector2> destination)
        {
            destination.Clear();
            float minimum = Mathf.Max(1f, Mathf.Min(rect.width, rect.height));
            float scale = Mathf.Clamp01(1f - outlineWidth / minimum);
            for (int i = 0; i < source.Count; i++)
            {
                destination.Add(Vector2.Lerp(center, source[i], scale));
            }
        }

        private static void AddFan(
            VertexHelper vertexHelper,
            List<Vector2> points,
            Color32 vertexColor)
        {
            if (points.Count < 3)
            {
                return;
            }

            int start = vertexHelper.currentVertCount;
            Vector2 center = Vector2.zero;
            for (int i = 0; i < points.Count; i++)
            {
                center += points[i];
            }

            center /= points.Count;
            vertexHelper.AddVert(center, vertexColor, new Vector2(0.5f, 0.5f));
            for (int i = 0; i < points.Count; i++)
            {
                vertexHelper.AddVert(points[i], vertexColor, Vector2.zero);
            }

            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                vertexHelper.AddTriangle(
                    start,
                    start + 1 + i,
                    start + 1 + next);
            }
        }
    }
}
