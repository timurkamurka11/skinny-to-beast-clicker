using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Upgrades Patch 3.3's intact painted body into a Lamar-style weighted
    /// cutout surface. CharacterSpriteRigController still owns direction,
    /// stage scaling and visibility bounds; this component replaces its flat
    /// Image renderer with a continuous skinned mesh driven by the live bones.
    /// </summary>
    [DefaultExecutionOrder(950)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterSpriteRigController))]
    [RequireComponent(typeof(CharacterRigController))]
    [RequireComponent(typeof(CharacterSkinController))]
    public sealed class CharacterLayeredRigController : MonoBehaviour
    {
        private const string FlatBodyName =
            "Sprite.RealFatManBody";
        private const string SkinnedSurfaceName =
            "Sprite.RealFatManLayeredSurface";

        private readonly List<Image> legacyFaceImages = new(4);

        private CharacterSpriteRigController spriteController;
        private CharacterRigController rigController;
        private CharacterSkinController skinController;
        private RectTransform flatBodyRect;
        private Image flatBodyImage;
        private RectTransform surfaceRect;
        private CharacterSkinnedSpriteGraphic skinnedGraphic;
        private RectTransform faceOverlayRoot;
        private Image leftEyelid;
        private Image rightEyelid;
        private Image mouthOverlay;
        private Sprite observedSprite;
        private CharacterFacing observedFacing =
            (CharacterFacing)(-1);
        private int observedStage = -1;
        private float nextBlinkAt;
        private float blinkUntil;
        private bool failureLogged;
        private bool ready;

        public bool IsReady =>
            ready &&
            spriteController != null &&
            spriteController.IsReady &&
            skinnedGraphic != null &&
            skinnedGraphic.IsReady;
        public float DeformationMagnitude =>
            skinnedGraphic != null
                ? skinnedGraphic.DeformationMagnitude
                : 0f;

        private void Awake()
        {
            spriteController =
                GetComponent<CharacterSpriteRigController>();
            rigController =
                GetComponent<CharacterRigController>();
            skinController =
                GetComponent<CharacterSkinController>();
        }

        private void Update()
        {
            if (!ready)
            {
                TryBuild();
            }
        }

        private void LateUpdate()
        {
            if (!ready)
            {
                return;
            }

            // CharacterSpriteRigController recreates its direction Sprite and
            // asserts alpha every frame. Keep its Image as a non-rendering data
            // source while our child CanvasRenderer displays the weighted mesh.
            if (flatBodyImage != null)
            {
                flatBodyImage.enabled = false;
            }

            DisableLegacyFaceImages();
            SyncViewAndStage();
            skinnedGraphic?.RefreshDeformation();
            UpdateFaceOverlay();
        }

        private void TryBuild()
        {
            if (spriteController == null ||
                rigController == null ||
                skinController == null ||
                !spriteController.IsReady ||
                rigController.VisualRoot == null)
            {
                return;
            }

            flatBodyRect =
                rigController.VisualRoot.Find(FlatBodyName)
                    as RectTransform;
            flatBodyImage =
                flatBodyRect != null
                    ? flatBodyRect.GetComponent<Image>()
                    : null;
            if (flatBodyRect == null ||
                flatBodyImage == null ||
                flatBodyImage.sprite == null)
            {
                LogFailureOnce(
                    "Patch 3.4 is waiting for Patch 3.3's painted body " +
                    "source Image.");
                return;
            }

            CreateSkinnedSurface();
            if (skinnedGraphic == null ||
                !skinnedGraphic.IsReady)
            {
                LogFailureOnce(
                    "Patch 3.4 could not create the weighted painted mesh.");
                ClearRuntimeObjects();
                return;
            }

            HideLegacyFaceImages();
            CreateFaceOverlay();
            observedSprite = flatBodyImage.sprite;
            observedFacing = rigController.Facing;
            observedStage = skinController.CurrentArtIndex;
            flatBodyImage.enabled = false;
            ScheduleBlink();
            ready = true;
            failureLogged = false;
            skinnedGraphic.RefreshDeformation();

            Debug.Log(
                "Real Fat Man Layered Rig Patch 3.4 active: the visible " +
                "painted man is weighted to arms, legs, torso, belly, chest, " +
                "head and soft-body bones; blink and reaction overlays follow " +
                "the animated head.",
                this);
        }

        private void CreateSkinnedSurface()
        {
            GameObject target = new(
                SkinnedSurfaceName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(CharacterSkinnedSpriteGraphic));
            target.layer = gameObject.layer;

            surfaceRect = target.GetComponent<RectTransform>();
            surfaceRect.SetParent(flatBodyRect, false);
            surfaceRect.anchorMin = Vector2.zero;
            surfaceRect.anchorMax = Vector2.one;
            surfaceRect.pivot = new Vector2(0.5f, 0.5f);
            surfaceRect.offsetMin = Vector2.zero;
            surfaceRect.offsetMax = Vector2.zero;
            surfaceRect.localRotation = Quaternion.identity;
            surfaceRect.localScale = Vector3.one;
            surfaceRect.SetAsLastSibling();

            Sprite sprite = flatBodyImage.sprite;
            skinnedGraphic =
                target.GetComponent<CharacterSkinnedSpriteGraphic>();
            skinnedGraphic.Configure(
                sprite.texture,
                ToRectInt(sprite.textureRect),
                rigController.VisualRoot,
                BuildBoneMap(),
                rigController.Facing);
        }

        private RectTransform[] BuildBoneMap()
        {
            RectTransform[] mapped = new RectTransform[
                Enum.GetValues(typeof(FatManSkinBone)).Length];

            mapped[(int)FatManSkinBone.Root] =
                rigController.GetBone("Bone.Root");
            mapped[(int)FatManSkinBone.Pelvis] =
                rigController.GetBone("Bone.Pelvis");
            mapped[(int)FatManSkinBone.Spine] =
                rigController.GetBone("Bone.Spine");
            mapped[(int)FatManSkinBone.Chest] =
                rigController.GetBone("Bone.Chest");
            mapped[(int)FatManSkinBone.Belly] =
                rigController.GetBone("Bone.Belly");
            mapped[(int)FatManSkinBone.ShirtHem] =
                rigController.GetBone("Bone.ShirtHem");
            mapped[(int)FatManSkinBone.ChestSoft] =
                rigController.GetBone("Bone.ChestSoft");
            mapped[(int)FatManSkinBone.Neck] =
                rigController.GetBone("Bone.Neck");
            mapped[(int)FatManSkinBone.Head] =
                rigController.GetBone("Bone.Head");
            mapped[(int)FatManSkinBone.ChinSoft] =
                rigController.GetBone("Bone.ChinSoft");
            mapped[(int)FatManSkinBone.ShoulderLeft] =
                rigController.GetBone("Bone.Shoulder.L");
            mapped[(int)FatManSkinBone.UpperArmLeft] =
                rigController.GetBone("Bone.UpperArm.L");
            mapped[(int)FatManSkinBone.ForearmLeft] =
                rigController.GetBone("Bone.Forearm.L");
            mapped[(int)FatManSkinBone.HandLeft] =
                rigController.GetBone("Bone.Hand.L");
            mapped[(int)FatManSkinBone.ShoulderRight] =
                rigController.GetBone("Bone.Shoulder.R");
            mapped[(int)FatManSkinBone.UpperArmRight] =
                rigController.GetBone("Bone.UpperArm.R");
            mapped[(int)FatManSkinBone.ForearmRight] =
                rigController.GetBone("Bone.Forearm.R");
            mapped[(int)FatManSkinBone.HandRight] =
                rigController.GetBone("Bone.Hand.R");
            mapped[(int)FatManSkinBone.ThighLeft] =
                rigController.GetBone("Bone.Thigh.L");
            mapped[(int)FatManSkinBone.ShinLeft] =
                rigController.GetBone("Bone.Shin.L");
            mapped[(int)FatManSkinBone.FootLeft] =
                rigController.GetBone("Bone.Foot.L");
            mapped[(int)FatManSkinBone.ThighRight] =
                rigController.GetBone("Bone.Thigh.R");
            mapped[(int)FatManSkinBone.ShinRight] =
                rigController.GetBone("Bone.Shin.R");
            mapped[(int)FatManSkinBone.FootRight] =
                rigController.GetBone("Bone.Foot.R");

            return mapped;
        }

        private void SyncViewAndStage()
        {
            if (flatBodyImage == null ||
                flatBodyImage.sprite == null ||
                skinnedGraphic == null)
            {
                return;
            }

            Sprite currentSprite = flatBodyImage.sprite;
            CharacterFacing facing = rigController.Facing;
            if (currentSprite != observedSprite ||
                facing != observedFacing)
            {
                observedSprite = currentSprite;
                observedFacing = facing;
                skinnedGraphic.SetView(
                    ToRectInt(currentSprite.textureRect),
                    facing);
                skinnedGraphic.CaptureBindPose();
                PositionFaceOverlay(
                    facing == CharacterFacing.SideLeft ||
                    facing == CharacterFacing.SideRight,
                    facing == CharacterFacing.Back);
            }

            int stage = skinController.CurrentArtIndex;
            if (stage != observedStage)
            {
                observedStage = stage;
                skinnedGraphic.CaptureBindPose();
            }
        }

        private void HideLegacyFaceImages()
        {
            legacyFaceImages.Clear();
            AddLegacyFaceImage("SpriteFace.Eyelid.L");
            AddLegacyFaceImage("SpriteFace.Eyelid.R");
            AddLegacyFaceImage("SpriteFace.Mouth");
            DisableLegacyFaceImages();
        }

        private void AddLegacyFaceImage(string childName)
        {
            if (flatBodyRect == null)
            {
                return;
            }

            Transform child = flatBodyRect.Find(childName);
            Image image =
                child != null
                    ? child.GetComponent<Image>()
                    : null;
            if (image != null)
            {
                legacyFaceImages.Add(image);
            }
        }

        private void DisableLegacyFaceImages()
        {
            for (int i = 0;
                 i < legacyFaceImages.Count;
                 i++)
            {
                if (legacyFaceImages[i] != null)
                {
                    legacyFaceImages[i].enabled = false;
                }
            }
        }

        private void CreateFaceOverlay()
        {
            RectTransform headBone =
                rigController.GetBone("Bone.Head");
            if (headBone == null)
            {
                return;
            }

            faceOverlayRoot = CreateRect(
                headBone,
                "LayeredPaintedFaceOverlay",
                new Vector2(0f, 88f),
                new Vector2(190f, 180f));
            faceOverlayRoot.SetAsLastSibling();

            Color skinColor = SampleSkinColor();
            leftEyelid = CreateSolidImage(
                faceOverlayRoot,
                "LayeredFace.Eyelid.L",
                skinColor,
                new Vector2(46f, 8f));
            rightEyelid = CreateSolidImage(
                faceOverlayRoot,
                "LayeredFace.Eyelid.R",
                skinColor,
                new Vector2(46f, 8f));
            mouthOverlay = CreateSolidImage(
                faceOverlayRoot,
                "LayeredFace.Mouth",
                new Color(0.16f, 0.07f, 0.06f, 0.94f),
                new Vector2(44f, 9f));

            leftEyelid.gameObject.SetActive(false);
            rightEyelid.gameObject.SetActive(false);
            mouthOverlay.gameObject.SetActive(false);
            PositionFaceOverlay(
                rigController.Facing == CharacterFacing.SideLeft ||
                rigController.Facing == CharacterFacing.SideRight,
                rigController.Facing == CharacterFacing.Back);
        }

        private void PositionFaceOverlay(
            bool side,
            bool back)
        {
            if (leftEyelid == null ||
                rightEyelid == null ||
                mouthOverlay == null)
            {
                return;
            }

            if (back)
            {
                leftEyelid.gameObject.SetActive(false);
                rightEyelid.gameObject.SetActive(false);
                mouthOverlay.gameObject.SetActive(false);
                blinkUntil = 0f;
                return;
            }

            if (side)
            {
                Vector2 eyePosition = new Vector2(18f, 25f);
                leftEyelid.rectTransform.anchoredPosition =
                    eyePosition;
                rightEyelid.rectTransform.anchoredPosition =
                    eyePosition;
                mouthOverlay.rectTransform.anchoredPosition =
                    new Vector2(20f, -31f);
            }
            else
            {
                leftEyelid.rectTransform.anchoredPosition =
                    new Vector2(-33f, 25f);
                rightEyelid.rectTransform.anchoredPosition =
                    new Vector2(33f, 25f);
                mouthOverlay.rectTransform.anchoredPosition =
                    new Vector2(0f, -31f);
            }
        }

        private void UpdateFaceOverlay()
        {
            if (leftEyelid == null ||
                rightEyelid == null ||
                mouthOverlay == null)
            {
                return;
            }

            CharacterFacing facing = rigController.Facing;
            bool back = facing == CharacterFacing.Back;
            bool side =
                facing == CharacterFacing.SideLeft ||
                facing == CharacterFacing.SideRight;
            float now = Time.unscaledTime;

            if (!back && now >= nextBlinkAt)
            {
                blinkUntil = now + 0.12f;
                ScheduleBlink();
            }

            bool blink = !back && now < blinkUntil;
            leftEyelid.gameObject.SetActive(blink);
            rightEyelid.gameObject.SetActive(
                blink && !side);

            bool expressive =
                !back &&
                (rigController.IsTapReacting ||
                 rigController.ActiveAction ==
                 CharacterRoutineAction.Yawn ||
                 rigController.ActiveAction ==
                 CharacterRoutineAction.Flex ||
                 rigController.ActiveAction ==
                 CharacterRoutineAction.Stretch);
            mouthOverlay.gameObject.SetActive(expressive);
            if (expressive)
            {
                bool yawn =
                    rigController.ActiveAction ==
                    CharacterRoutineAction.Yawn;
                mouthOverlay.rectTransform.sizeDelta =
                    new Vector2(
                        yawn ? 38f : 44f,
                        yawn ? 34f : 9f);
            }
        }

        private Color SampleSkinColor()
        {
            Sprite sprite =
                flatBodyImage != null
                    ? flatBodyImage.sprite
                    : null;
            Texture2D texture =
                sprite != null
                    ? sprite.texture
                    : null;
            if (sprite == null ||
                texture == null ||
                !texture.isReadable)
            {
                return new Color(
                    0.78f,
                    0.56f,
                    0.43f,
                    1f);
            }

            Rect rect = sprite.textureRect;
            int centerX =
                Mathf.RoundToInt(
                    rect.x + rect.width * 0.5f);
            int centerY =
                Mathf.RoundToInt(
                    rect.y + rect.height * 0.82f);

            for (int radius = 0;
                 radius <= 24;
                 radius += 4)
            {
                for (int y = -radius;
                     y <= radius;
                     y += 4)
                {
                    for (int x = -radius;
                         x <= radius;
                         x += 4)
                    {
                        Color sample = texture.GetPixel(
                            Mathf.Clamp(
                                centerX + x,
                                0,
                                texture.width - 1),
                            Mathf.Clamp(
                                centerY + y,
                                0,
                                texture.height - 1));
                        if (sample.a > 0.8f &&
                            sample.r > 0.28f &&
                            sample.g > 0.18f)
                        {
                            sample.a = 1f;
                            return sample;
                        }
                    }
                }
            }

            return new Color(
                0.78f,
                0.56f,
                0.43f,
                1f);
        }

        private void ScheduleBlink()
        {
            nextBlinkAt =
                Time.unscaledTime +
                UnityEngine.Random.Range(2.1f, 5.2f);
        }

        private static RectTransform CreateRect(
            Transform parent,
            string objectName,
            Vector2 position,
            Vector2 size)
        {
            GameObject target = new(
                objectName,
                typeof(RectTransform));
            target.layer = parent.gameObject.layer;
            RectTransform rect =
                target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static Image CreateSolidImage(
            RectTransform parent,
            string objectName,
            Color color,
            Vector2 size)
        {
            GameObject target = new(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            target.layer = parent.gameObject.layer;
            RectTransform rect =
                target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();

            Image image = target.GetComponent<Image>();
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            image.maskable = false;
            image.color = color;
            return image;
        }

        private static RectInt ToRectInt(Rect rect)
        {
            return new RectInt(
                Mathf.RoundToInt(rect.x),
                Mathf.RoundToInt(rect.y),
                Mathf.RoundToInt(rect.width),
                Mathf.RoundToInt(rect.height));
        }

        private void LogFailureOnce(string message)
        {
            if (failureLogged)
            {
                return;
            }

            failureLogged = true;
            Debug.LogError(message, this);
        }

        private void ClearRuntimeObjects()
        {
            if (surfaceRect != null)
            {
                Destroy(surfaceRect.gameObject);
            }
            if (faceOverlayRoot != null)
            {
                Destroy(faceOverlayRoot.gameObject);
            }

            surfaceRect = null;
            skinnedGraphic = null;
            faceOverlayRoot = null;
            leftEyelid = null;
            rightEyelid = null;
            mouthOverlay = null;
            ready = false;
        }

        private void OnDestroy()
        {
            ClearRuntimeObjects();
        }
    }
}
