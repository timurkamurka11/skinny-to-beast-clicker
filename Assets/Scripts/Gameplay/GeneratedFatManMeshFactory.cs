using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    internal sealed class GeneratedFatManAssetScope : IDisposable
    {
        private readonly List<UnityEngine.Object> owned = new();

        public T Own<T>(T value) where T : UnityEngine.Object
        {
            if (value != null)
            {
                owned.Add(value);
            }
            return value;
        }

        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object value = owned[i];
                if (value == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(value);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(value);
                }
            }
            owned.Clear();
        }
    }

    internal static class GeneratedFatManMeshFactory
    {
        internal delegate BoneWeight WeightProvider(Vector2 normalizedPoint);

        private static readonly Color Outline =
            new Color(0.075f, 0.035f, 0.025f, 1f);

        public static SkinnedMeshRenderer CreateOutlinedEllipse(
            Transform parent,
            string name,
            Vector2 center,
            Vector2 size,
            Transform[] bones,
            WeightProvider weights,
            Color color,
            int sortingOrder,
            GeneratedFatManAssetScope scope,
            int segments = 28,
            int rings = 4,
            float outlineWidth = 0.07f)
        {
            CreateEllipse(
                parent,
                name + ".Outline",
                center,
                size + Vector2.one * outlineWidth * 2f,
                bones,
                weights,
                Outline,
                sortingOrder - 1,
                scope,
                segments,
                rings);

            return CreateEllipse(
                parent,
                name,
                center,
                size,
                bones,
                weights,
                color,
                sortingOrder,
                scope,
                segments,
                rings);
        }

        public static SkinnedMeshRenderer CreateEllipse(
            Transform parent,
            string name,
            Vector2 center,
            Vector2 size,
            Transform[] bones,
            WeightProvider weights,
            Color color,
            int sortingOrder,
            GeneratedFatManAssetScope scope,
            int segments = 28,
            int rings = 4)
        {
            GameObject target = new GameObject(name);
            target.transform.SetParent(parent, false);
            SkinnedMeshRenderer renderer =
                target.AddComponent<SkinnedMeshRenderer>();

            Mesh mesh = scope.Own(BuildEllipseMesh(
                name + ".Mesh",
                center,
                size,
                bones,
                renderer.transform,
                weights,
                segments,
                rings));

            renderer.sharedMesh = mesh;
            renderer.bones = bones;
            renderer.rootBone = bones != null && bones.Length > 0
                ? bones[0]
                : parent;
            renderer.sharedMaterial = scope.Own(CreateMaterial(
                name + ".Material",
                color));
            renderer.sortingOrder = sortingOrder;
            renderer.updateWhenOffscreen = true;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage =
                UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage =
                UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode =
                MotionVectorGenerationMode.ForceNoMotion;
            return renderer;
        }

        public static SkinnedMeshRenderer CreateOutlinedPolygon(
            Transform parent,
            string name,
            Vector2[] points,
            Transform bone,
            Color color,
            int sortingOrder,
            GeneratedFatManAssetScope scope,
            float outlineScale = 1.07f)
        {
            Vector2 center = Average(points);
            Vector2[] expanded = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                expanded[i] = center + (points[i] - center) * outlineScale;
            }

            CreatePolygon(
                parent,
                name + ".Outline",
                expanded,
                bone,
                Outline,
                sortingOrder - 1,
                scope);

            return CreatePolygon(
                parent,
                name,
                points,
                bone,
                color,
                sortingOrder,
                scope);
        }

        public static SkinnedMeshRenderer CreatePolygon(
            Transform parent,
            string name,
            Vector2[] points,
            Transform bone,
            Color color,
            int sortingOrder,
            GeneratedFatManAssetScope scope)
        {
            if (points == null || points.Length < 3)
            {
                throw new ArgumentException(
                    "A polygon needs at least three points.",
                    nameof(points));
            }

            GameObject target = new GameObject(name);
            target.transform.SetParent(parent, false);
            SkinnedMeshRenderer renderer =
                target.AddComponent<SkinnedMeshRenderer>();

            Mesh mesh = new Mesh
            {
                name = name + ".Mesh"
            };
            Vector3[] vertices = new Vector3[points.Length];
            Vector2[] uvs = new Vector2[points.Length];
            BoneWeight[] boneWeights = new BoneWeight[points.Length];
            Bounds pointBounds = CalculateBounds(points);
            float width = Mathf.Max(0.001f, pointBounds.size.x);
            float height = Mathf.Max(0.001f, pointBounds.size.y);
            for (int i = 0; i < points.Length; i++)
            {
                vertices[i] = new Vector3(points[i].x, points[i].y, 0f);
                uvs[i] = new Vector2(
                    (points[i].x - pointBounds.min.x) / width,
                    (points[i].y - pointBounds.min.y) / height);
                boneWeights[i] = RigidWeight();
            }

            int[] triangles = new int[(points.Length - 2) * 3];
            int cursor = 0;
            for (int i = 1; i < points.Length - 1; i++)
            {
                triangles[cursor++] = 0;
                triangles[cursor++] = i;
                triangles[cursor++] = i + 1;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.boneWeights = boneWeights;
            mesh.bindposes = BuildBindposes(
                new[] { bone },
                renderer.transform);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            scope.Own(mesh);

            renderer.sharedMesh = mesh;
            renderer.bones = new[] { bone };
            renderer.rootBone = bone;
            renderer.sharedMaterial = scope.Own(CreateMaterial(
                name + ".Material",
                color));
            renderer.sortingOrder = sortingOrder;
            renderer.updateWhenOffscreen = true;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage =
                UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage =
                UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode =
                MotionVectorGenerationMode.ForceNoMotion;
            return renderer;
        }

        public static BoneWeight RigidWeight(int boneIndex = 0)
        {
            return new BoneWeight
            {
                boneIndex0 = boneIndex,
                weight0 = 1f
            };
        }

        public static BoneWeight Blend(
            int first,
            float firstWeight,
            int second,
            float secondWeight,
            int third = 0,
            float thirdWeight = 0f,
            int fourth = 0,
            float fourthWeight = 0f)
        {
            float total = Mathf.Max(
                0.0001f,
                firstWeight + secondWeight +
                thirdWeight + fourthWeight);
            return new BoneWeight
            {
                boneIndex0 = first,
                weight0 = Mathf.Max(0f, firstWeight) / total,
                boneIndex1 = second,
                weight1 = Mathf.Max(0f, secondWeight) / total,
                boneIndex2 = third,
                weight2 = Mathf.Max(0f, thirdWeight) / total,
                boneIndex3 = fourth,
                weight3 = Mathf.Max(0f, fourthWeight) / total
            };
        }

        private static Mesh BuildEllipseMesh(
            string name,
            Vector2 center,
            Vector2 size,
            Transform[] bones,
            Transform rendererTransform,
            WeightProvider weights,
            int segmentCount,
            int ringCount)
        {
            int segments = Mathf.Clamp(segmentCount, 12, 64);
            int rings = Mathf.Clamp(ringCount, 2, 8);
            int vertexCount = 1 + segments * rings;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            BoneWeight[] boneWeights = new BoneWeight[vertexCount];

            vertices[0] = new Vector3(center.x, center.y, 0f);
            uvs[0] = new Vector2(0.5f, 0.5f);
            boneWeights[0] = weights != null
                ? weights(Vector2.zero)
                : RigidWeight();

            int vertex = 1;
            for (int ring = 1; ring <= rings; ring++)
            {
                float radius = ring / (float)rings;
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle =
                        segment / (float)segments * Mathf.PI * 2f;
                    Vector2 normalized = new Vector2(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius);
                    Vector2 point = center + Vector2.Scale(
                        normalized,
                        size * 0.5f);
                    vertices[vertex] =
                        new Vector3(point.x, point.y, 0f);
                    uvs[vertex] =
                        normalized * 0.5f + Vector2.one * 0.5f;
                    boneWeights[vertex] = weights != null
                        ? weights(normalized)
                        : RigidWeight();
                    vertex++;
                }
            }

            List<int> triangles =
                new List<int>(segments * (1 + (rings - 1) * 2) * 3);

            int firstRing = 1;
            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                triangles.Add(0);
                triangles.Add(firstRing + segment);
                triangles.Add(firstRing + next);
            }

            for (int ring = 2; ring <= rings; ring++)
            {
                int previousStart =
                    1 + (ring - 2) * segments;
                int currentStart =
                    1 + (ring - 1) * segments;
                for (int segment = 0; segment < segments; segment++)
                {
                    int next = (segment + 1) % segments;
                    int a = previousStart + segment;
                    int b = previousStart + next;
                    int c = currentStart + segment;
                    int d = currentStart + next;

                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(d);
                    triangles.Add(a);
                    triangles.Add(d);
                    triangles.Add(b);
                }
            }

            Mesh mesh = new Mesh
            {
                name = name
            };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles.ToArray();
            mesh.boneWeights = boneWeights;
            mesh.bindposes = BuildBindposes(
                bones,
                rendererTransform);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Matrix4x4[] BuildBindposes(
            Transform[] bones,
            Transform rendererTransform)
        {
            if (bones == null || bones.Length == 0)
            {
                return Array.Empty<Matrix4x4>();
            }

            Matrix4x4[] bindposes =
                new Matrix4x4[bones.Length];
            Matrix4x4 rendererMatrix =
                rendererTransform.localToWorldMatrix;
            for (int i = 0; i < bones.Length; i++)
            {
                bindposes[i] = bones[i].worldToLocalMatrix *
                               rendererMatrix;
            }
            return bindposes;
        }

        private static Material CreateMaterial(
            string name,
            Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader)
            {
                name = name,
                color = color,
                renderQueue = 3000,
                mainTexture = Texture2D.whiteTexture,
                doubleSidedGI = true
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }
            return material;
        }

        private static Vector2 Average(Vector2[] points)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < points.Length; i++)
            {
                sum += points[i];
            }
            return sum / Mathf.Max(1, points.Length);
        }

        private static Bounds CalculateBounds(Vector2[] points)
        {
            Bounds bounds = new Bounds(
                new Vector3(points[0].x, points[0].y, 0f),
                Vector3.zero);
            for (int i = 1; i < points.Length; i++)
            {
                bounds.Encapsulate(
                    new Vector3(points[i].x, points[i].y, 0f));
            }
            return bounds;
        }
    }
}
