using System;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    [Serializable]
    public sealed class CharacterSkinDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private int artIndex;
        [SerializeField] private Sprite frontSprite;
        [SerializeField] private Texture2D directionalWalkSheet;
        [SerializeField] private CharacterDirectionalFrame[] directionalFrames;
        [SerializeField] private CharacterRigProfile rigProfile;
        [SerializeField] private CharacterFaceStyle faceStyle;

        public string Id => id;
        public int ArtIndex => artIndex;
        public Sprite FrontSprite => frontSprite;
        public Texture2D DirectionalWalkSheet => directionalWalkSheet;
        public CharacterRigProfile RigProfile => rigProfile;
        public CharacterFaceStyle FaceStyle => faceStyle;
        public bool IsValid => frontSprite != null && rigProfile != null;

        public CharacterDirectionalFrame GetDirectionalFrame(
            bool backFacing,
            int frame)
        {
            int safeFrame = Mathf.Abs(frame) % 2;
            int index = (backFacing ? 2 : 0) + safeFrame;
            return directionalFrames != null && index < directionalFrames.Length
                ? directionalFrames[index]
                : CharacterDirectionalFrame.Default;
        }

        public static CharacterSkinDefinition Create(
            int index,
            Sprite sprite,
            Texture2D walkSheet)
        {
            int safeIndex = Mathf.Clamp(index, 0, 3);
            return new CharacterSkinDefinition
            {
                id = $"body_stage_{safeIndex + 1:00}",
                artIndex = safeIndex,
                frontSprite = sprite,
                directionalWalkSheet = walkSheet,
                directionalFrames =
                    CharacterDirectionalFrame.CreateForStage(safeIndex),
                rigProfile = CharacterRigProfile.Create(safeIndex),
                faceStyle = CharacterFaceStyle.Create(safeIndex)
            };
        }

        public static int ResolveArtIndexForStrength(double strength)
        {
            if (strength < 50d)
            {
                return 0;
            }

            if (strength < 250d)
            {
                return 1;
            }

            if (strength < 5000d)
            {
                return 2;
            }

            return 3;
        }
    }

    [Serializable]
    public struct CharacterDirectionalFrame
    {
        [SerializeField] private float scale;
        [SerializeField] private Vector2 offset;

        public float Scale => scale > 0f ? scale : 1f;
        public Vector2 Offset => offset;
        public static CharacterDirectionalFrame Default =>
            new CharacterDirectionalFrame(1f, Vector2.zero);

        public CharacterDirectionalFrame(float frameScale, Vector2 frameOffset)
        {
            scale = Mathf.Max(0.1f, frameScale);
            offset = frameOffset;
        }

        public static CharacterDirectionalFrame[] CreateForStage(int artIndex)
        {
            // The generated figures occupy different regions inside their
            // equally-sized sheet cells. These values normalize each frame to
            // the front sprite's height and lock its feet to the same baseline.
            return Mathf.Clamp(artIndex, 0, 3) switch
            {
                0 => new[]
                {
                    Frame(0.9721f, -55.6f, 12.9f),
                    Frame(0.9811f, 56.1f, 9.7f),
                    Frame(1.0250f, -27.2f, -42.5f),
                    Frame(0.9979f, 89.6f, -26.3f)
                },
                1 => new[]
                {
                    Frame(0.9926f, 19.2f, -7.7f),
                    Frame(0.9926f, 26.3f, -9.7f),
                    Frame(0.9798f, -2.0f, -30.3f),
                    Frame(0.9691f, 26.7f, -23.8f)
                },
                2 => new[]
                {
                    Frame(0.9299f, -47.5f, 40.1f),
                    Frame(0.9299f, 82.6f, 40.1f),
                    Frame(1.1864f, -66.6f, -190.5f),
                    Frame(1.1864f, 122.3f, -190.5f)
                },
                _ => new[]
                {
                    Frame(0.9607f, -18.6f, 16.9f),
                    Frame(0.9654f, 80.8f, 20.0f),
                    Frame(1.1301f, -24.2f, -130.7f),
                    Frame(1.1171f, 74.1f, -122.4f)
                }
            };
        }

        private static CharacterDirectionalFrame Frame(
            float frameScale,
            float x,
            float y)
        {
            return new CharacterDirectionalFrame(
                frameScale,
                new Vector2(x, y));
        }
    }

    [Serializable]
    public sealed class CharacterRigProfile
    {
        [Header("Logical joints in normalized source-image coordinates")]
        public Vector2 pelvis;
        public Vector2 spine;
        public Vector2 chest;
        public Vector2 neck;
        public Vector2 head;
        public Vector2 leftShoulder;
        public Vector2 leftElbow;
        public Vector2 leftWrist;
        public Vector2 rightShoulder;
        public Vector2 rightElbow;
        public Vector2 rightWrist;
        public Vector2 leftHip;
        public Vector2 leftKnee;
        public Vector2 leftAnkle;
        public Vector2 rightHip;
        public Vector2 rightKnee;
        public Vector2 rightAnkle;

        [Header("Visible source regions")]
        public CharacterRigCrop torso;
        public CharacterRigCrop pelvisArt;
        public CharacterRigCrop headArt;
        public CharacterRigCrop leftUpperArm;
        public CharacterRigCrop leftForearm;
        public CharacterRigCrop leftHand;
        public CharacterRigCrop rightUpperArm;
        public CharacterRigCrop rightForearm;
        public CharacterRigCrop rightHand;
        public CharacterRigCrop leftThigh;
        public CharacterRigCrop leftShin;
        public CharacterRigCrop leftFoot;
        public CharacterRigCrop rightThigh;
        public CharacterRigCrop rightShin;
        public CharacterRigCrop rightFoot;

        public Vector2 faceCenter;
        public float visualWidth = 720f;
        public float visualHeight = 1280f;

        public static CharacterRigProfile Create(int artIndex)
        {
            int stage = Mathf.Clamp(artIndex, 0, 3);
            float shoulderX = stage switch
            {
                0 => 0.315f,
                1 => 0.315f,
                2 => 0.285f,
                _ => 0.255f
            };
            float elbowX = stage switch
            {
                0 => 0.225f,
                1 => 0.225f,
                2 => 0.195f,
                _ => 0.155f
            };
            float wristX = stage switch
            {
                0 => 0.205f,
                1 => 0.205f,
                2 => 0.185f,
                _ => 0.145f
            };
            float hipX = stage >= 3 ? 0.39f : 0.405f;
            float kneeX = stage >= 3 ? 0.365f : 0.375f;
            float ankleX = stage >= 3 ? 0.35f : 0.36f;
            float shoulderY = stage == 0 ? 0.705f : 0.71f;
            float elbowY = stage >= 2 ? 0.555f : 0.54f;
            float wristY = stage >= 2 ? 0.405f : 0.40f;
            float torsoLeft = stage >= 3 ? 0.24f : 0.255f;

            CharacterRigProfile profile = new CharacterRigProfile
            {
                pelvis = new Vector2(0.5f, 0.37f),
                spine = new Vector2(0.5f, 0.49f),
                chest = new Vector2(0.5f, 0.655f),
                neck = new Vector2(0.5f, 0.755f),
                head = new Vector2(0.5f, 0.785f),
                leftShoulder = new Vector2(shoulderX, shoulderY),
                leftElbow = new Vector2(elbowX, elbowY),
                leftWrist = new Vector2(wristX, wristY),
                rightShoulder = Mirror(new Vector2(shoulderX, shoulderY)),
                rightElbow = Mirror(new Vector2(elbowX, elbowY)),
                rightWrist = Mirror(new Vector2(wristX, wristY)),
                leftHip = new Vector2(hipX, 0.355f),
                leftKnee = new Vector2(kneeX, 0.205f),
                leftAnkle = new Vector2(ankleX, 0.085f),
                rightHip = Mirror(new Vector2(hipX, 0.355f)),
                rightKnee = Mirror(new Vector2(kneeX, 0.205f)),
                rightAnkle = Mirror(new Vector2(ankleX, 0.085f)),
                faceCenter = new Vector2(0.5f, stage == 3 ? 0.862f : 0.855f)
            };

            float torsoTop = stage >= 3 ? 0.765f : 0.755f;
            float torsoBottom = stage >= 2 ? 0.37f : 0.345f;
            float torsoWaist = stage >= 3 ? 0.31f : 0.275f;
            profile.torso = PolygonCrop(
                profile.chest,
                new Vector2(shoulderX + 0.04f, torsoTop + 0.035f),
                new Vector2(1f - shoulderX - 0.04f, torsoTop + 0.035f),
                new Vector2(1f - shoulderX + 0.11f, torsoTop - 0.035f),
                new Vector2(1f - torsoLeft, 0.665f),
                new Vector2(1f - torsoWaist, torsoBottom),
                new Vector2(torsoWaist, torsoBottom),
                new Vector2(torsoLeft, 0.665f),
                new Vector2(shoulderX - 0.11f, torsoTop - 0.035f));
            profile.pelvisArt = PolygonCrop(
                profile.pelvis,
                new Vector2(0.245f, 0.43f),
                new Vector2(0.755f, 0.43f),
                new Vector2(0.765f, 0.275f),
                new Vector2(0.235f, 0.275f));
            profile.headArt = PolygonCrop(
                profile.head,
                new Vector2(0.31f, 0.755f),
                new Vector2(0.69f, 0.755f),
                new Vector2(0.735f, 0.835f),
                new Vector2(0.69f, 0.975f),
                new Vector2(0.31f, 0.975f),
                new Vector2(0.265f, 0.835f));

            // The polygons overlap at every joint. This keeps the original
            // silhouette closed while neighbouring bones rotate away from one
            // another and prevents transparent shoulder, wrist and ankle seams.
            float upperArmStartWidth = stage >= 3 ? 225f : 203f;
            float upperArmEndWidth = stage >= 3 ? 174f : 146f;
            float forearmStartWidth = stage >= 3 ? 180f : 141f;
            float forearmEndWidth = stage >= 3 ? 146f : 113f;
            profile.leftUpperArm = LimbCrop(
                profile.leftShoulder,
                profile.leftElbow,
                upperArmStartWidth,
                upperArmEndWidth,
                profile);
            profile.leftForearm = LimbCrop(
                profile.leftElbow,
                profile.leftWrist,
                forearmStartWidth,
                forearmEndWidth,
                profile);
            profile.leftHand = HandCrop(
                profile.leftElbow,
                profile.leftWrist,
                stage >= 3 ? 152f : 113f,
                profile);

            profile.rightUpperArm = Mirror(profile.leftUpperArm);
            profile.rightForearm = Mirror(profile.leftForearm);
            profile.rightHand = Mirror(profile.leftHand);

            profile.leftThigh = LimbCrop(
                profile.leftHip,
                profile.leftKnee,
                stage >= 3 ? 169f : 141f,
                stage >= 3 ? 135f : 118f,
                profile);
            profile.leftShin = LimbCrop(
                profile.leftKnee,
                profile.leftAnkle,
                stage >= 3 ? 135f : 118f,
                stage >= 3 ? 101f : 84f,
                profile);
            profile.leftFoot = FootCrop(
                profile.leftKnee,
                profile.leftAnkle,
                false,
                stage >= 3 ? 135f : 107f,
                profile);

            profile.rightThigh = Mirror(profile.leftThigh);
            profile.rightShin = Mirror(profile.leftShin);
            profile.rightFoot = Mirror(profile.leftFoot);
            return profile;
        }

        private static CharacterRigCrop PolygonCrop(
            Vector2 pivot,
            params Vector2[] points)
        {
            return new CharacterRigCrop(pivot, points);
        }

        private static CharacterRigCrop LimbCrop(
            Vector2 start,
            Vector2 end,
            float startWidth,
            float endWidth,
            CharacterRigProfile profile)
        {
            Vector2 size = new Vector2(profile.visualWidth, profile.visualHeight);
            Vector2 startPixels = Vector2.Scale(start, size);
            Vector2 endPixels = Vector2.Scale(end, size);
            Vector2 direction = (endPixels - startPixels).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            startPixels -= direction * 20f;
            endPixels += direction * 25f;

            return PolygonCrop(
                start,
                Divide(startPixels + perpendicular * startWidth * 0.5f, size),
                Divide(endPixels + perpendicular * endWidth * 0.5f, size),
                Divide(endPixels - perpendicular * endWidth * 0.5f, size),
                Divide(startPixels - perpendicular * startWidth * 0.5f, size));
        }

        private static CharacterRigCrop HandCrop(
            Vector2 elbow,
            Vector2 wrist,
            float width,
            CharacterRigProfile profile)
        {
            Vector2 size = new Vector2(profile.visualWidth, profile.visualHeight);
            Vector2 elbowPixels = Vector2.Scale(elbow, size);
            Vector2 wristPixels = Vector2.Scale(wrist, size);
            Vector2 direction = (wristPixels - elbowPixels).normalized;
            Vector2 end = wristPixels + direction * width * 1.25f;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            return PolygonCrop(
                wrist,
                Divide(wristPixels + perpendicular * width * 0.55f - direction * 23f, size),
                Divide(end + perpendicular * width * 0.48f, size),
                Divide(end - perpendicular * width * 0.48f, size),
                Divide(wristPixels - perpendicular * width * 0.55f - direction * 23f, size));
        }

        private static CharacterRigCrop FootCrop(
            Vector2 knee,
            Vector2 ankle,
            bool pointsRight,
            float width,
            CharacterRigProfile profile)
        {
            Vector2 size = new Vector2(profile.visualWidth, profile.visualHeight);
            Vector2 anklePixels = Vector2.Scale(ankle, size);
            float horizontal = pointsRight ? width * 1.1f : -width * 1.1f;
            float direction = pointsRight ? 1f : -1f;
            return PolygonCrop(
                ankle,
                Divide(anklePixels + new Vector2(width * 0.62f, 28f), size),
                Divide(anklePixels + new Vector2(-width * 0.55f, 28f), size),
                Divide(
                    anklePixels + new Vector2(
                        horizontal + direction * width * 0.28f,
                        -62f),
                    size),
                Divide(
                    anklePixels + new Vector2(-direction * width * 0.52f, -50f),
                    size));
        }

        private static Vector2 Divide(Vector2 value, Vector2 divisor)
        {
            return new Vector2(value.x / divisor.x, value.y / divisor.y);
        }

        private static Vector2 Mirror(Vector2 point)
        {
            return new Vector2(1f - point.x, point.y);
        }

        private static CharacterRigCrop Mirror(CharacterRigCrop crop)
        {
            Vector2[] polygon = crop.Polygon;
            Vector2[] mirrored = new Vector2[polygon.Length];
            for (int i = 0; i < polygon.Length; i++)
            {
                mirrored[i] = Mirror(polygon[polygon.Length - 1 - i]);
            }

            return new CharacterRigCrop(Mirror(crop.Pivot), mirrored);
        }
    }

    [Serializable]
    public struct CharacterRigCrop
    {
        [SerializeField] private Rect uv;
        [SerializeField] private Vector2 pivot;
        [SerializeField] private Vector2[] polygon;

        public Rect Uv => uv;
        public Vector2 Pivot => pivot;
        public Vector2[] Polygon => polygon;

        public CharacterRigCrop(Rect sourceUv, Vector2 sourcePivot)
        {
            uv = sourceUv;
            pivot = sourcePivot;
            polygon = new[]
            {
                new Vector2(uv.xMin, uv.yMin),
                new Vector2(uv.xMax, uv.yMin),
                new Vector2(uv.xMax, uv.yMax),
                new Vector2(uv.xMin, uv.yMax)
            };
        }

        public CharacterRigCrop(Vector2 sourcePivot, params Vector2[] sourcePolygon)
        {
            pivot = sourcePivot;
            polygon = sourcePolygon ?? Array.Empty<Vector2>();
            if (polygon.Length == 0)
            {
                uv = new Rect(sourcePivot, Vector2.zero);
                return;
            }

            float minX = polygon[0].x;
            float minY = polygon[0].y;
            float maxX = polygon[0].x;
            float maxY = polygon[0].y;
            for (int i = 1; i < polygon.Length; i++)
            {
                minX = Mathf.Min(minX, polygon[i].x);
                minY = Mathf.Min(minY, polygon[i].y);
                maxX = Mathf.Max(maxX, polygon[i].x);
                maxY = Mathf.Max(maxY, polygon[i].y);
            }

            uv = Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }

    [Serializable]
    public struct CharacterFaceStyle
    {
        public Color skin;
        public Color eyeWhite;
        public Color iris;
        public Color brow;
        public Color mouth;
        public float overlayScale;
        public float eyeSeparation;
        public float eyeY;
        public CharacterExpression defaultExpression;

        public static CharacterFaceStyle Create(int artIndex)
        {
            int stage = Mathf.Clamp(artIndex, 0, 3);
            return new CharacterFaceStyle
            {
                skin = stage == 0
                    ? new Color(0.91f, 0.49f, 0.31f, 1f)
                    : new Color(0.92f, 0.51f, 0.31f, 1f),
                eyeWhite = new Color(0.97f, 0.97f, 0.94f, 1f),
                iris = new Color(0.055f, 0.045f, 0.035f, 1f),
                brow = new Color(0.10f, 0.055f, 0.035f, 1f),
                mouth = new Color(0.24f, 0.075f, 0.045f, 1f),
                overlayScale = stage == 3 ? 0.92f : 1f,
                eyeSeparation = stage == 3 ? 31f : 33f,
                eyeY = 31f,
                defaultExpression = stage switch
                {
                    0 => CharacterExpression.Tired,
                    1 => CharacterExpression.Neutral,
                    2 => CharacterExpression.Focused,
                    _ => CharacterExpression.Happy
                }
            };
        }
    }
}
