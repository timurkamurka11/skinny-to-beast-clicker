using System.Collections.Generic;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Shared vector silhouettes for the live cutout character. The fat-man
    /// shapes are deliberately asymmetric and softly tapered so the actor
    /// reads as a drawn person instead of a stack of UI primitives.
    /// </summary>
    internal static class CharacterShapeGeometry
    {
        private const int SmoothSegments = 28;

        public static void BuildBoundary(
            CharacterMeshShape shape,
            Rect rect,
            float topWidth,
            float bottomWidth,
            List<Vector2> points)
        {
            points.Clear();
            switch (shape)
            {
                case CharacterMeshShape.FatThigh:
                    BuildProfiledLimb(
                        rect,
                        points,
                        0.86f,
                        1.08f,
                        0.82f,
                        0.18f);
                    break;
                case CharacterMeshShape.FatCalf:
                    BuildProfiledLimb(
                        rect,
                        points,
                        0.82f,
                        1.12f,
                        0.70f,
                        -0.10f);
                    break;
                case CharacterMeshShape.FatPelvis:
                    BuildFatPelvis(rect, points);
                    break;
                case CharacterMeshShape.FatBelly:
                    BuildFatBelly(rect, points, topWidth, bottomWidth);
                    break;
                case CharacterMeshShape.FatChest:
                    BuildFatChest(rect, points, topWidth, bottomWidth);
                    break;
                case CharacterMeshShape.FatShoulder:
                    BuildOrganicEllipse(rect, points, 0.04f, -0.02f);
                    break;
                case CharacterMeshShape.FatUpperArm:
                    BuildProfiledLimb(
                        rect,
                        points,
                        0.86f,
                        1.12f,
                        0.78f,
                        0.12f);
                    break;
                case CharacterMeshShape.FatForearm:
                    BuildProfiledLimb(
                        rect,
                        points,
                        0.78f,
                        1.02f,
                        0.68f,
                        -0.08f);
                    break;
                case CharacterMeshShape.FatHand:
                    BuildFatHand(rect, points);
                    break;
                case CharacterMeshShape.FatNeck:
                    BuildProfiledLimb(
                        rect,
                        points,
                        0.88f,
                        1.12f,
                        1.02f,
                        0f);
                    break;
                case CharacterMeshShape.FatHead:
                    BuildFatHead(rect, points);
                    break;
                case CharacterMeshShape.DoubleChin:
                    BuildDoubleChin(rect, points);
                    break;
                case CharacterMeshShape.MessyHair:
                    BuildMessyHair(rect, points);
                    break;
                case CharacterMeshShape.WornShoe:
                    BuildWornShoe(rect, points);
                    break;
                case CharacterMeshShape.ShirtHem:
                    BuildShirtHem(rect, points);
                    break;
                case CharacterMeshShape.Neckline:
                    BuildNeckline(rect, points);
                    break;
                case CharacterMeshShape.BellyBand:
                    BuildBellyBand(rect, points);
                    break;
                case CharacterMeshShape.Waistband:
                    BuildWaistband(rect, points);
                    break;
                case CharacterMeshShape.Pocket:
                    BuildPocket(rect, points);
                    break;
                case CharacterMeshShape.FabricFold:
                    BuildCapsule(rect, points);
                    break;
                case CharacterMeshShape.Stain:
                    BuildStain(rect, points);
                    break;
                case CharacterMeshShape.Ear:
                    BuildEar(rect, points);
                    break;
                case CharacterMeshShape.Nose:
                    BuildNose(rect, points);
                    break;
                case CharacterMeshShape.Torso:
                    BuildTorso(rect, points, topWidth, bottomWidth);
                    break;
                case CharacterMeshShape.Shoe:
                    BuildShoe(rect, points);
                    break;
                case CharacterMeshShape.Hair:
                    BuildHair(rect, points);
                    break;
                case CharacterMeshShape.Brow:
                case CharacterMeshShape.Mouth:
                    BuildCapsule(rect, points);
                    break;
                case CharacterMeshShape.Ellipse:
                    BuildEllipse(rect, points, 24);
                    break;
                default:
                    BuildCapsule(rect, points);
                    break;
            }
        }

        private static void BuildFatBelly(
            Rect rect,
            List<Vector2> points,
            float topWidth,
            float bottomWidth)
        {
            float x = rect.center.x;
            float y = rect.center.y;
            float rx = rect.width * 0.5f;
            float ry = rect.height * 0.5f;
            float top = Mathf.Clamp(topWidth, 0.72f, 1.35f);
            float bottom = Mathf.Clamp(bottomWidth, 0.72f, 1.35f);

            AddBezier(points,
                new Vector2(x, y + ry),
                new Vector2(x + rx * 0.55f * top, y + ry * 1.02f),
                new Vector2(x + rx * 1.02f, y + ry * 0.46f),
                new Vector2(x + rx, y - ry * 0.16f),
                7);
            AddBezier(points,
                new Vector2(x + rx, y - ry * 0.16f),
                new Vector2(x + rx * 0.98f, y - ry * 0.76f),
                new Vector2(x + rx * 0.48f * bottom, y - ry * 1.08f),
                new Vector2(x, y - ry),
                7,
                true);
            AddBezier(points,
                new Vector2(x, y - ry),
                new Vector2(x - rx * 0.48f * bottom, y - ry * 1.08f),
                new Vector2(x - rx * 0.98f, y - ry * 0.76f),
                new Vector2(x - rx, y - ry * 0.16f),
                7,
                true);
            AddBezier(points,
                new Vector2(x - rx, y - ry * 0.16f),
                new Vector2(x - rx * 1.02f, y + ry * 0.46f),
                new Vector2(x - rx * 0.55f * top, y + ry * 1.02f),
                new Vector2(x, y + ry),
                7,
                true);
        }

        private static void BuildFatChest(
            Rect rect,
            List<Vector2> points,
            float topWidth,
            float bottomWidth)
        {
            float x = rect.center.x;
            float top = rect.yMax;
            float bottom = rect.yMin;
            float rx = rect.width * 0.5f;
            float upper = rx * Mathf.Clamp(topWidth, 0.74f, 1.18f);
            float lower = rx * Mathf.Clamp(bottomWidth, 0.64f, 1.18f);
            float h = rect.height;

            points.Add(new Vector2(x, top));
            AddBezier(points,
                new Vector2(x, top),
                new Vector2(x + upper * 0.56f, top + h * 0.015f),
                new Vector2(x + upper * 1.05f, top - h * 0.13f),
                new Vector2(x + upper, top - h * 0.28f),
                5,
                true);
            AddBezier(points,
                new Vector2(x + upper, top - h * 0.28f),
                new Vector2(x + rx * 1.03f, top - h * 0.47f),
                new Vector2(x + lower * 1.04f, bottom + h * 0.18f),
                new Vector2(x + lower, bottom),
                6,
                true);
            AddBezier(points,
                new Vector2(x + lower, bottom),
                new Vector2(x + lower * 0.42f, bottom - h * 0.02f),
                new Vector2(x - lower * 0.42f, bottom - h * 0.02f),
                new Vector2(x - lower, bottom),
                5,
                true);
            AddBezier(points,
                new Vector2(x - lower, bottom),
                new Vector2(x - lower * 1.04f, bottom + h * 0.18f),
                new Vector2(x - rx * 1.03f, top - h * 0.47f),
                new Vector2(x - upper, top - h * 0.28f),
                6,
                true);
            AddBezier(points,
                new Vector2(x - upper, top - h * 0.28f),
                new Vector2(x - upper * 1.05f, top - h * 0.13f),
                new Vector2(x - upper * 0.56f, top + h * 0.015f),
                new Vector2(x, top),
                5,
                true);
        }

        private static void BuildFatPelvis(
            Rect rect,
            List<Vector2> points)
        {
            float x = rect.center.x;
            float top = rect.yMax;
            float bottom = rect.yMin;
            float rx = rect.width * 0.5f;
            float h = rect.height;

            points.Add(new Vector2(x - rx * 0.87f, top));
            points.Add(new Vector2(x + rx * 0.87f, top));
            AddBezier(points,
                new Vector2(x + rx * 0.87f, top),
                new Vector2(x + rx * 1.03f, top - h * 0.24f),
                new Vector2(x + rx, bottom + h * 0.24f),
                new Vector2(x + rx * 0.82f, bottom),
                6,
                true);
            AddBezier(points,
                new Vector2(x + rx * 0.82f, bottom),
                new Vector2(x + rx * 0.42f, bottom - h * 0.02f),
                new Vector2(x + rx * 0.18f, bottom + h * 0.16f),
                new Vector2(x, bottom + h * 0.12f),
                4,
                true);
            AddBezier(points,
                new Vector2(x, bottom + h * 0.12f),
                new Vector2(x - rx * 0.18f, bottom + h * 0.16f),
                new Vector2(x - rx * 0.42f, bottom - h * 0.02f),
                new Vector2(x - rx * 0.82f, bottom),
                4,
                true);
            AddBezier(points,
                new Vector2(x - rx * 0.82f, bottom),
                new Vector2(x - rx, bottom + h * 0.24f),
                new Vector2(x - rx * 1.03f, top - h * 0.24f),
                new Vector2(x - rx * 0.87f, top),
                6,
                true);
        }

        private static void BuildFatHead(
            Rect rect,
            List<Vector2> points)
        {
            Vector2 center = rect.center + new Vector2(0f, rect.height * 0.035f);
            float rx = rect.width * 0.5f;
            float ry = rect.height * 0.5f;
            for (int i = 0; i < SmoothSegments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / SmoothSegments;
                float sin = Mathf.Sin(angle);
                float vertical = (sin + 1f) * 0.5f;
                float width = vertical > 0.72f
                    ? Mathf.Lerp(1.00f, 0.78f, (vertical - 0.72f) / 0.28f)
                    : vertical > 0.30f
                        ? Mathf.Lerp(1.08f, 1.00f, (vertical - 0.30f) / 0.42f)
                        : Mathf.Lerp(0.73f, 1.08f, vertical / 0.30f);
                points.Add(center + new Vector2(
                    Mathf.Cos(angle) * rx * width,
                    sin * ry));
            }
        }

        private static void BuildDoubleChin(
            Rect rect,
            List<Vector2> points)
        {
            float x = rect.center.x;
            float y = rect.center.y;
            float rx = rect.width * 0.5f;
            float ry = rect.height * 0.5f;
            AddBezier(points,
                new Vector2(x - rx, y + ry * 0.26f),
                new Vector2(x - rx * 0.65f, y + ry),
                new Vector2(x + rx * 0.65f, y + ry),
                new Vector2(x + rx, y + ry * 0.26f),
                8);
            AddBezier(points,
                new Vector2(x + rx, y + ry * 0.26f),
                new Vector2(x + rx * 0.76f, y - ry),
                new Vector2(x - rx * 0.76f, y - ry),
                new Vector2(x - rx, y + ry * 0.26f),
                8,
                true);
        }

        private static void BuildProfiledLimb(
            Rect rect,
            List<Vector2> points,
            float topRatio,
            float middleRatio,
            float bottomRatio,
            float middleOffset)
        {
            float x = rect.center.x;
            float top = rect.yMax;
            float bottom = rect.yMin;
            float rx = rect.width * 0.5f;
            float h = rect.height;
            float middleY = rect.center.y + h * middleOffset;

            points.Add(new Vector2(x, top));
            AddBezier(points,
                new Vector2(x, top),
                new Vector2(x + rx * topRatio, top),
                new Vector2(x + rx * middleRatio, middleY + h * 0.15f),
                new Vector2(x + rx * middleRatio, middleY),
                6,
                true);
            AddBezier(points,
                new Vector2(x + rx * middleRatio, middleY),
                new Vector2(x + rx * middleRatio, middleY - h * 0.18f),
                new Vector2(x + rx * bottomRatio, bottom),
                new Vector2(x, bottom),
                6,
                true);
            AddBezier(points,
                new Vector2(x, bottom),
                new Vector2(x - rx * bottomRatio, bottom),
                new Vector2(x - rx * middleRatio, middleY - h * 0.18f),
                new Vector2(x - rx * middleRatio, middleY),
                6,
                true);
            AddBezier(points,
                new Vector2(x - rx * middleRatio, middleY),
                new Vector2(x - rx * middleRatio, middleY + h * 0.15f),
                new Vector2(x - rx * topRatio, top),
                new Vector2(x, top),
                6,
                true);
        }

        private static void BuildFatHand(
            Rect rect,
            List<Vector2> points)
        {
            float w = rect.width;
            float h = rect.height;
            points.Add(new Vector2(rect.xMin + w * 0.26f, rect.yMax));
            points.Add(new Vector2(rect.xMax - w * 0.22f, rect.yMax));
            points.Add(new Vector2(rect.xMax - w * 0.04f, rect.yMax - h * 0.29f));
            points.Add(new Vector2(rect.xMax, rect.yMin + h * 0.47f));
            points.Add(new Vector2(rect.xMax - w * 0.16f, rect.yMin + h * 0.22f));
            points.Add(new Vector2(rect.xMax - w * 0.37f, rect.yMin));
            points.Add(new Vector2(rect.xMin + w * 0.34f, rect.yMin + h * 0.02f));
            points.Add(new Vector2(rect.xMin + w * 0.10f, rect.yMin + h * 0.24f));
            points.Add(new Vector2(rect.xMin, rect.yMin + h * 0.55f));
            points.Add(new Vector2(rect.xMin + w * 0.12f, rect.yMax - h * 0.24f));
        }

        private static void BuildMessyHair(
            Rect rect,
            List<Vector2> points)
        {
            float w = rect.width;
            float h = rect.height;
            Vector2 center = rect.center;
            points.Add(new Vector2(rect.xMin + w * 0.05f, rect.yMin + h * 0.12f));
            points.Add(new Vector2(rect.xMin, rect.yMin + h * 0.54f));
            points.Add(new Vector2(rect.xMin + w * 0.10f, rect.yMin + h * 0.48f));
            points.Add(new Vector2(rect.xMin + w * 0.06f, rect.yMin + h * 0.78f));
            points.Add(new Vector2(rect.xMin + w * 0.22f, rect.yMin + h * 0.68f));
            points.Add(new Vector2(rect.xMin + w * 0.25f, rect.yMax));
            points.Add(new Vector2(center.x - w * 0.03f, rect.yMin + h * 0.82f));
            points.Add(new Vector2(center.x + w * 0.12f, rect.yMax - h * 0.04f));
            points.Add(new Vector2(rect.xMax - w * 0.24f, rect.yMin + h * 0.70f));
            points.Add(new Vector2(rect.xMax - w * 0.06f, rect.yMin + h * 0.82f));
            points.Add(new Vector2(rect.xMax - w * 0.10f, rect.yMin + h * 0.49f));
            points.Add(new Vector2(rect.xMax, rect.yMin + h * 0.55f));
            points.Add(new Vector2(rect.xMax - w * 0.07f, rect.yMin + h * 0.10f));
            points.Add(new Vector2(rect.xMax - w * 0.22f, rect.yMin + h * 0.27f));
            points.Add(new Vector2(rect.xMax - w * 0.34f, rect.yMin + h * 0.05f));
            points.Add(new Vector2(center.x + w * 0.06f, rect.yMin + h * 0.23f));
            points.Add(new Vector2(center.x - w * 0.12f, rect.yMin + h * 0.03f));
            points.Add(new Vector2(rect.xMin + w * 0.30f, rect.yMin + h * 0.27f));
            points.Add(new Vector2(rect.xMin + w * 0.18f, rect.yMin + h * 0.04f));
        }

        private static void BuildWornShoe(
            Rect rect,
            List<Vector2> points)
        {
            float w = rect.width;
            float h = rect.height;
            points.Add(new Vector2(rect.xMin + w * 0.10f, rect.yMax - h * 0.12f));
            points.Add(new Vector2(rect.xMax - w * 0.35f, rect.yMax));
            points.Add(new Vector2(rect.xMax - w * 0.14f, rect.yMax - h * 0.21f));
            points.Add(new Vector2(rect.xMax, rect.yMin + h * 0.36f));
            points.Add(new Vector2(rect.xMax - w * 0.03f, rect.yMin + h * 0.12f));
            points.Add(new Vector2(rect.xMax - w * 0.19f, rect.yMin));
            points.Add(new Vector2(rect.xMin + w * 0.08f, rect.yMin));
            points.Add(new Vector2(rect.xMin, rect.yMin + h * 0.22f));
            points.Add(new Vector2(rect.xMin + w * 0.03f, rect.yMax - h * 0.30f));
        }

        private static void BuildShirtHem(
            Rect rect,
            List<Vector2> points)
        {
            float w = rect.width;
            float h = rect.height;
            points.Add(new Vector2(rect.xMin + w * 0.03f, rect.yMax));
            points.Add(new Vector2(rect.xMax - w * 0.03f, rect.yMax));
            points.Add(new Vector2(rect.xMax, rect.yMin + h * 0.42f));
            points.Add(new Vector2(rect.xMax - w * 0.13f, rect.yMin + h * 0.10f));
            points.Add(new Vector2(rect.xMax - w * 0.33f, rect.yMin + h * 0.20f));
            points.Add(new Vector2(rect.center.x, rect.yMin));
            points.Add(new Vector2(rect.xMin + w * 0.31f, rect.yMin + h * 0.18f));
            points.Add(new Vector2(rect.xMin + w * 0.12f, rect.yMin + h * 0.08f));
            points.Add(new Vector2(rect.xMin, rect.yMin + h * 0.40f));
        }

        private static void BuildNeckline(
            Rect rect,
            List<Vector2> points)
        {
            float w = rect.width;
            float h = rect.height;
            points.Add(new Vector2(rect.xMin, rect.yMax));
            points.Add(new Vector2(rect.xMax, rect.yMax));
            points.Add(new Vector2(rect.xMax - w * 0.08f, rect.yMin + h * 0.50f));
            points.Add(new Vector2(rect.xMax - w * 0.28f, rect.yMin + h * 0.18f));
            points.Add(new Vector2(rect.center.x, rect.yMin));
            points.Add(new Vector2(rect.xMin + w * 0.28f, rect.yMin + h * 0.18f));
            points.Add(new Vector2(rect.xMin + w * 0.08f, rect.yMin + h * 0.50f));
        }

        private static void BuildBellyBand(
            Rect rect,
            List<Vector2> points)
        {
            float w = rect.width;
            float h = rect.height;
            points.Add(new Vector2(rect.xMin + w * 0.04f, rect.yMax - h * 0.24f));
            points.Add(new Vector2(rect.xMax - w * 0.04f, rect.yMax - h * 0.24f));
            points.Add(new Vector2(rect.xMax, rect.yMin + h * 0.55f));
            points.Add(new Vector2(rect.xMax - w * 0.18f, rect.yMin + h * 0.12f));
            points.Add(new Vector2(rect.center.x, rect.yMin));
            points.Add(new Vector2(rect.xMin + w * 0.18f, rect.yMin + h * 0.12f));
            points.Add(new Vector2(rect.xMin, rect.yMin + h * 0.55f));
        }

        private static void BuildWaistband(
            Rect rect,
            List<Vector2> points)
        {
            float inset = rect.height * 0.22f;
            points.Add(new Vector2(rect.xMin + inset, rect.yMax));
            points.Add(new Vector2(rect.xMax - inset, rect.yMax));
            points.Add(new Vector2(rect.xMax, rect.center.y));
            points.Add(new Vector2(rect.xMax - inset, rect.yMin));
            points.Add(new Vector2(rect.xMin + inset, rect.yMin));
            points.Add(new Vector2(rect.xMin, rect.center.y));
        }

        private static void BuildPocket(
            Rect rect,
            List<Vector2> points)
        {
            float w = rect.width;
            float h = rect.height;
            points.Add(new Vector2(rect.xMin, rect.yMax));
            points.Add(new Vector2(rect.xMax, rect.yMax));
            points.Add(new Vector2(rect.xMax - w * 0.06f, rect.yMin + h * 0.18f));
            points.Add(new Vector2(rect.center.x, rect.yMin));
            points.Add(new Vector2(rect.xMin + w * 0.06f, rect.yMin + h * 0.18f));
        }

        private static void BuildStain(
            Rect rect,
            List<Vector2> points)
        {
            Vector2 center = rect.center;
            float rx = rect.width * 0.5f;
            float ry = rect.height * 0.5f;
            for (int i = 0; i < 15; i++)
            {
                float angle = Mathf.PI * 2f * i / 15f;
                float wobble = 0.86f +
                               0.10f * Mathf.Sin(i * 2.31f) +
                               0.06f * Mathf.Cos(i * 4.17f);
                points.Add(center + new Vector2(
                    Mathf.Cos(angle) * rx * wobble,
                    Mathf.Sin(angle) * ry * wobble));
            }
        }

        private static void BuildEar(
            Rect rect,
            List<Vector2> points)
        {
            BuildOrganicEllipse(rect, points, 0.10f, -0.02f);
        }

        private static void BuildNose(
            Rect rect,
            List<Vector2> points)
        {
            float w = rect.width;
            float h = rect.height;
            points.Add(new Vector2(rect.center.x, rect.yMax));
            points.Add(new Vector2(rect.xMax - w * 0.16f, rect.yMin + h * 0.42f));
            points.Add(new Vector2(rect.xMax, rect.yMin + h * 0.18f));
            points.Add(new Vector2(rect.center.x, rect.yMin));
            points.Add(new Vector2(rect.xMin, rect.yMin + h * 0.18f));
            points.Add(new Vector2(rect.xMin + w * 0.16f, rect.yMin + h * 0.42f));
        }

        private static void BuildOrganicEllipse(
            Rect rect,
            List<Vector2> points,
            float xOffset,
            float yOffset)
        {
            Vector2 center = rect.center + new Vector2(
                rect.width * xOffset,
                rect.height * yOffset);
            float rx = rect.width * 0.5f;
            float ry = rect.height * 0.5f;
            for (int i = 0; i < 24; i++)
            {
                float angle = (Mathf.PI * 2f * i) / 24f;
                float wobble = 1f + 0.025f * Mathf.Sin(angle * 3f);
                points.Add(center + new Vector2(
                    Mathf.Cos(angle) * rx * wobble,
                    Mathf.Sin(angle) * ry));
            }
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

        private static void BuildCapsule(
            Rect rect,
            List<Vector2> points)
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

        private static void BuildTorso(
            Rect rect,
            List<Vector2> points,
            float topWidth,
            float bottomWidth)
        {
            float halfWidth = rect.width * 0.5f;
            float top = rect.yMax;
            float bottom = rect.yMin;
            float upper = Mathf.Min(
                halfWidth * topWidth,
                rect.width * 0.72f);
            float lower = Mathf.Min(
                halfWidth * bottomWidth,
                rect.width * 0.72f);
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

        private static void BuildShoe(
            Rect rect,
            List<Vector2> points)
        {
            float width = rect.width;
            float height = rect.height;
            points.Add(new Vector2(
                rect.xMin + width * 0.08f,
                rect.yMax));
            points.Add(new Vector2(
                rect.xMax - width * 0.28f,
                rect.yMax));
            points.Add(new Vector2(
                rect.xMax - width * 0.04f,
                rect.yMin + height * 0.42f));
            points.Add(new Vector2(
                rect.xMax,
                rect.yMin + height * 0.18f));
            points.Add(new Vector2(
                rect.xMax - width * 0.08f,
                rect.yMin));
            points.Add(new Vector2(
                rect.xMin + width * 0.06f,
                rect.yMin));
            points.Add(new Vector2(
                rect.xMin,
                rect.yMin + height * 0.28f));
        }

        private static void BuildHair(
            Rect rect,
            List<Vector2> points)
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

            points.Add(new Vector2(
                rect.xMin + rect.width * 0.08f,
                rect.yMin));
            points.Add(new Vector2(
                rect.xMin + rect.width * 0.18f,
                rect.yMin + rect.height * 0.24f));
            points.Add(new Vector2(
                rect.xMin + rect.width * 0.29f,
                rect.yMin));
            points.Add(new Vector2(
                rect.xMin + rect.width * 0.41f,
                rect.yMin + rect.height * 0.20f));
            points.Add(new Vector2(
                rect.xMin + rect.width * 0.53f,
                rect.yMin));
            points.Add(new Vector2(
                rect.xMin + rect.width * 0.66f,
                rect.yMin + rect.height * 0.18f));
            points.Add(new Vector2(
                rect.xMin + rect.width * 0.80f,
                rect.yMin));
            points.Add(new Vector2(
                rect.xMax - rect.width * 0.05f,
                rect.yMin + rect.height * 0.22f));
        }

        private static void AddBezier(
            List<Vector2> points,
            Vector2 start,
            Vector2 controlA,
            Vector2 controlB,
            Vector2 end,
            int segments,
            bool skipStart = false)
        {
            int first = skipStart ? 1 : 0;
            for (int i = first; i <= segments; i++)
            {
                float t = i / (float)segments;
                float inverse = 1f - t;
                points.Add(
                    inverse * inverse * inverse * start +
                    3f * inverse * inverse * t * controlA +
                    3f * inverse * t * t * controlB +
                    t * t * t * end);
            }
        }
    }
}
