using System;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    public enum CharacterSkinSlot
    {
        Body,
        Hair,
        Face,
        Top,
        Bottom,
        Shoes,
        Accessory,
        HandItem,
        Effect
    }

    [Serializable]
    public struct CharacterSkinSlotSelection
    {
        [SerializeField] private CharacterSkinSlot slot;
        [SerializeField] private string itemId;
        [SerializeField] private bool visible;

        public CharacterSkinSlot Slot => slot;
        public string ItemId => itemId;
        public bool Visible => visible && !string.IsNullOrWhiteSpace(itemId);

        public CharacterSkinSlotSelection(
            CharacterSkinSlot targetSlot,
            string targetItemId,
            bool shouldShow)
        {
            slot = targetSlot;
            itemId = targetItemId;
            visible = shouldShow;
        }
    }

    /// <summary>
    /// Atomic appearance selection for the one persistent skeletal actor. A
    /// stage changes colors and proportions on existing mesh parts; it never
    /// creates a second body and never swaps an animation frame.
    /// </summary>
    [Serializable]
    public sealed class CharacterSkinDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private int artIndex;
        [SerializeField] private CharacterAppearance appearance;
        [SerializeField] private CharacterFaceStyle faceStyle;
        [SerializeField] private CharacterSkinSlotSelection[] slots;

        public string Id => id;
        public int ArtIndex => artIndex;
        public CharacterAppearance Appearance => appearance;
        public CharacterFaceStyle FaceStyle => faceStyle;
        public CharacterSkinSlotSelection[] Slots => slots;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(id) &&
            appearance.IsValid &&
            HasVisibleSlot(CharacterSkinSlot.Body);

        public static CharacterSkinDefinition Create(int index)
        {
            int safeIndex = Mathf.Clamp(index, 0, 3);
            string stage = $"{safeIndex + 1:00}";
            bool hasAccent = safeIndex >= 2;
            return new CharacterSkinDefinition
            {
                id = $"stage_{stage}",
                artIndex = safeIndex,
                appearance = CharacterAppearance.Create(safeIndex),
                faceStyle = CharacterFaceStyle.Create(safeIndex),
                slots = new[]
                {
                    new CharacterSkinSlotSelection(
                        CharacterSkinSlot.Body,
                        $"body_{stage}",
                        true),
                    new CharacterSkinSlotSelection(
                        CharacterSkinSlot.Hair,
                        $"hair_{stage}",
                        true),
                    new CharacterSkinSlotSelection(
                        CharacterSkinSlot.Face,
                        $"face_{stage}",
                        true),
                    new CharacterSkinSlotSelection(
                        CharacterSkinSlot.Top,
                        $"top_{stage}",
                        true),
                    new CharacterSkinSlotSelection(
                        CharacterSkinSlot.Bottom,
                        $"bottom_{stage}",
                        true),
                    new CharacterSkinSlotSelection(
                        CharacterSkinSlot.Shoes,
                        $"shoes_{stage}",
                        true),
                    new CharacterSkinSlotSelection(
                        CharacterSkinSlot.Accessory,
                        $"wrist_wraps_{stage}",
                        hasAccent),
                    new CharacterSkinSlotSelection(
                        CharacterSkinSlot.HandItem,
                        string.Empty,
                        false),
                    new CharacterSkinSlotSelection(
                        CharacterSkinSlot.Effect,
                        string.Empty,
                        false)
                }
            };
        }

        public bool HasVisibleSlot(CharacterSkinSlot slot)
        {
            if (slots == null)
            {
                return false;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Slot == slot && slots[i].Visible)
                {
                    return true;
                }
            }

            return false;
        }

        public static int ResolveArtIndexForStrength(double strength)
        {
            if (strength >= 5000d)
            {
                return 3;
            }

            if (strength >= 250d)
            {
                return 2;
            }

            if (strength >= 50d)
            {
                return 1;
            }

            return 0;
        }

        public static int ResolveArtIndexForBodyStage(
            int bodyStageIndex)
        {
            if (bodyStageIndex <= 0)
            {
                return 0;
            }

            if (bodyStageIndex == 1)
            {
                return 1;
            }

            return bodyStageIndex <= 3 ? 2 : 3;
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
        public Color cheek;
        public float overlayScale;
        public float eyeSeparation;
        public float eyeY;
        public CharacterExpression defaultExpression;

        public static CharacterFaceStyle Create(int artIndex)
        {
            CharacterAppearance appearance =
                CharacterAppearance.Create(artIndex);
            int stage = Mathf.Clamp(artIndex, 0, 3);
            return new CharacterFaceStyle
            {
                skin = appearance.skin,
                eyeWhite = new Color(0.985f, 0.98f, 0.94f, 1f),
                iris = stage >= 2
                    ? new Color(0.06f, 0.16f, 0.20f, 1f)
                    : new Color(0.075f, 0.045f, 0.025f, 1f),
                brow = appearance.hair,
                mouth = new Color(0.30f, 0.075f, 0.055f, 1f),
                cheek = new Color(0.95f, 0.30f, 0.24f, 0.32f),
                overlayScale = appearance.headScale,
                eyeSeparation = stage == 3 ? 30f : 33f,
                eyeY = 25f,
                defaultExpression = appearance.defaultExpression
            };
        }
    }
}
