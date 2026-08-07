using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Volume-stable walk presentation for the one-piece painted body.
    ///
    /// The previous continuous skin used broad linear blends between many bone
    /// matrices. With the larger walk angles that behaves like classic linear
    /// blend skinning and visibly collapses/squashes the torso and limbs. This
    /// effect keeps the painted torso rigid to CharacterRoot and assigns the
    /// actual arm/leg/head regions to their articulated chains, using only very
    /// narrow blends at anatomical seams. The result preserves the silhouette
    /// while still allowing shoulder/elbow/wrist and hip/knee/ankle motion.
    /// </summary>
    [DefaultExecutionOrder(1215)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class Patch4StableBodyCanvasDeformer : BaseMeshEffect
    {
        private sealed class BoneBind
        {
            public Transform bone;
            public Matrix4x4 bindPose;
        }

        private const int GridColumns = 96;
        private const int GridRows = 144;
        private const float Epsilon = 0.0001f;

        private readonly Dictionary<string, BoneBind> binds =
            new(StringComparer.Ordinal);

        private Patch4CharacterRigController rigController;
        private Image targetImage;
        private bool bindPoseCaptured;

        private static readonly string[] RequiredBones =
        {
            "CharacterRoot",
            "Head",
            "Neck",
            "UpperArmL",
            "ForearmL",
            "HandL",
            "UpperArmR",
            "ForearmR",
            "HandR",
            "ThighL",
            "ShinL",
            "FootL",
            "ThighR",
            "ShinR",
            "FootR"
        };

        public bool IsBound => bindPoseCaptured;

        public void Configure(Patch4CharacterRigController rig)
        {
            rigController = rig;
            targetImage = graphic as Image;
            binds.Clear();
            bindPoseCaptured = false;
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResolveReferences();
            CaptureBindPose();
        }

        private void LateUpdate()
        {
            if (bindPoseCaptured &&
                graphic != null &&
                graphic.gameObject.activeInHierarchy)
            {
                graphic.SetVerticesDirty();
            }
        }

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || vertexHelper == null)
            {
                return;
            }

            ResolveReferences();
            if (targetImage == null ||
                targetImage.sprite == null ||
                rigController == null)
            {
                return;
            }

            if (!bindPoseCaptured && !CaptureBindPose())
            {
                return;
            }

            RectTransform rectTransform = targetImage.rectTransform;
            Rect rect = rectTransform.rect;
            if (Mathf.Abs(rect.width) < Epsilon ||
                Mathf.Abs(rect.height) < Epsilon)
            {
                return;
            }

            Vector4 uv = DataUtility.GetOuterUV(targetImage.sprite);
            Color32 color = targetImage.color;

            vertexHelper.Clear();
            for (int row = 0; row <= GridRows; row++)
            {
                float v = row / (float)GridRows;
                float y = Mathf.Lerp(rect.yMin, rect.yMax, v);
                for (int column = 0; column <= GridColumns; column++)
                {
                    float u = column / (float)GridColumns;
                    float x = Mathf.Lerp(rect.xMin, rect.xMax, u);
                    Vector3 original = new(x, y, 0f);
                    Vector3 deformed = DeformStable(original, u, 1f - v);

                    UIVertex vertex = UIVertex.simpleVert;
                    vertex.position = deformed;
                    vertex.color = color;
                    vertex.uv0 = new Vector2(
                        Mathf.Lerp(uv.x, uv.z, u),
                        Mathf.Lerp(uv.y, uv.w, v));
                    vertexHelper.AddVert(vertex);
                }
            }

            int stride = GridColumns + 1;
            for (int row = 0; row < GridRows; row++)
            {
                for (int column = 0; column < GridColumns; column++)
                {
                    int lowerLeft = row * stride + column;
                    int lowerRight = lowerLeft + 1;
                    int upperLeft = lowerLeft + stride;
                    int upperRight = upperLeft + 1;
                    vertexHelper.AddTriangle(
                        lowerLeft,
                        upperLeft,
                        upperRight);
                    vertexHelper.AddTriangle(
                        lowerLeft,
                        upperRight,
                        lowerRight);
                }
            }
        }

        private Vector3 DeformStable(
            Vector3 original,
            float normalizedX,
            float topY)
        {
            // The painted torso is intentionally rigid. This is the key change
            // from v18: no broad multi-bone matrix average can compress the
            // chest, belly or pelvis while the limbs swing.
            Vector3 body = BonePoint("CharacterRoot", original);
            Vector3 result = body;

            float headHorizontal =
                SmoothBand(normalizedX, .33f, .37f, .63f, .67f);
            if (headHorizontal > Epsilon && topY <= .345f)
            {
                Vector3 head = topY <= .285f
                    ? BonePoint("Head", original)
                    : topY <= .315f
                        ? Vector3.Lerp(
                            BonePoint("Head", original),
                            BonePoint("Neck", original),
                            Smooth01(.285f, .315f, topY))
                        : Vector3.Lerp(
                            BonePoint("Neck", original),
                            body,
                            Smooth01(.315f, .345f, topY));
                result = Vector3.Lerp(result, head, headHorizontal);
            }

            float leftArm = ArmInfluence(true, normalizedX, topY);
            float rightArm = ArmInfluence(false, normalizedX, topY);
            if (leftArm > Epsilon)
            {
                result = Vector3.Lerp(
                    result,
                    ArmPoint(true, original, topY),
                    leftArm);
            }
            if (rightArm > Epsilon)
            {
                result = Vector3.Lerp(
                    result,
                    ArmPoint(false, original, topY),
                    rightArm);
            }

            float leftLeg = LegInfluence(true, normalizedX, topY);
            float rightLeg = LegInfluence(false, normalizedX, topY);
            if (leftLeg > Epsilon)
            {
                result = Vector3.Lerp(
                    result,
                    LegPoint(true, original, topY),
                    leftLeg);
            }
            if (rightLeg > Epsilon)
            {
                result = Vector3.Lerp(
                    result,
                    LegPoint(false, original, topY),
                    rightLeg);
            }

            return result;
        }

        private Vector3 ArmPoint(bool left, Vector3 original, float topY)
        {
            string upper = left ? "UpperArmL" : "UpperArmR";
            string fore = left ? "ForearmL" : "ForearmR";
            string hand = left ? "HandL" : "HandR";

            if (topY <= .365f)
            {
                return BonePoint(upper, original);
            }
            if (topY <= .395f)
            {
                return Vector3.Lerp(
                    BonePoint(upper, original),
                    BonePoint(fore, original),
                    Smooth01(.365f, .395f, topY));
            }
            if (topY <= .475f)
            {
                return BonePoint(fore, original);
            }
            if (topY <= .505f)
            {
                return Vector3.Lerp(
                    BonePoint(fore, original),
                    BonePoint(hand, original),
                    Smooth01(.475f, .505f, topY));
            }

            return BonePoint(hand, original);
        }

        private Vector3 LegPoint(bool left, Vector3 original, float topY)
        {
            string thigh = left ? "ThighL" : "ThighR";
            string shin = left ? "ShinL" : "ShinR";
            string foot = left ? "FootL" : "FootR";

            if (topY <= .655f)
            {
                return BonePoint(thigh, original);
            }
            if (topY <= .685f)
            {
                return Vector3.Lerp(
                    BonePoint(thigh, original),
                    BonePoint(shin, original),
                    Smooth01(.655f, .685f, topY));
            }
            if (topY <= .790f)
            {
                return BonePoint(shin, original);
            }
            if (topY <= .820f)
            {
                return Vector3.Lerp(
                    BonePoint(shin, original),
                    BonePoint(foot, original),
                    Smooth01(.790f, .820f, topY));
            }

            return BonePoint(foot, original);
        }

        private static float ArmInfluence(
            bool left,
            float x,
            float topY)
        {
            if (topY < .245f || topY > .565f)
            {
                return 0f;
            }

            float horizontal = left
                ? SmoothBand(x, .16f, .19f, .345f, .385f)
                : SmoothBand(x, .615f, .655f, .81f, .84f);
            float vertical =
                Smooth01(.245f, .270f, topY) *
                (1f - Smooth01(.540f, .565f, topY));
            return horizontal * vertical;
        }

        private static float LegInfluence(
            bool left,
            float x,
            float topY)
        {
            if (topY < .515f)
            {
                return 0f;
            }

            float horizontal = left
                ? SmoothBand(x, .245f, .285f, .470f, .500f)
                : SmoothBand(x, .500f, .530f, .715f, .755f);
            float vertical = Smooth01(.515f, .555f, topY);
            return horizontal * vertical;
        }

        private Vector3 BonePoint(string boneName, Vector3 original)
        {
            if (!binds.TryGetValue(boneName, out BoneBind bind) ||
                bind == null ||
                bind.bone == null ||
                targetImage == null)
            {
                return original;
            }

            RectTransform imageTransform = targetImage.rectTransform;
            Matrix4x4 matrix =
                imageTransform.worldToLocalMatrix *
                bind.bone.localToWorldMatrix *
                bind.bindPose;
            return matrix.MultiplyPoint3x4(original);
        }

        private void ResolveReferences()
        {
            if (targetImage == null)
            {
                targetImage = graphic as Image;
            }
            if (rigController == null)
            {
                rigController =
                    GetComponentInParent<Patch4CharacterRigController>(true);
            }
        }

        private bool CaptureBindPose()
        {
            ResolveReferences();
            binds.Clear();
            bindPoseCaptured = false;
            if (targetImage == null || rigController == null)
            {
                return false;
            }

            RectTransform imageTransform = targetImage.rectTransform;
            for (int i = 0; i < RequiredBones.Length; i++)
            {
                string name = RequiredBones[i];
                Transform bone = rigController.GetBone(name);
                if (bone == null)
                {
                    binds.Clear();
                    return false;
                }

                binds[name] = new BoneBind
                {
                    bone = bone,
                    bindPose =
                        bone.worldToLocalMatrix *
                        imageTransform.localToWorldMatrix
                };
            }

            bindPoseCaptured = true;
            SetVerticesDirty();
            return true;
        }

        private static float SmoothBand(
            float value,
            float outerMin,
            float innerMin,
            float innerMax,
            float outerMax)
        {
            return Smooth01(outerMin, innerMin, value) *
                   (1f - Smooth01(innerMax, outerMax, value));
        }

        private static float Smooth01(float min, float max, float value)
        {
            if (max <= min + Epsilon)
            {
                return value >= max ? 1f : 0f;
            }

            float t = Mathf.Clamp01((value - min) / (max - min));
            return t * t * (3f - 2f * t);
        }
    }
}
