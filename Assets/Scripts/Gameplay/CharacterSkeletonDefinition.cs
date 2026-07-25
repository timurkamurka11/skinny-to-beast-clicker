using System;
using UnityEngine;

namespace SkinnyToBeast.Gameplay
{
    [Serializable]
    public sealed class CharacterSkeletonDefinition
    {
        public Vector2 canvasSize = new Vector2(720f, 1280f);
        public float presentationScale = 0.74f;

        public Vector2 pelvis = Vector2.zero;
        public Vector2 spine = new Vector2(0f, 112f);
        public Vector2 chest = new Vector2(0f, 154f);
        public Vector2 neck = new Vector2(0f, 132f);
        public Vector2 head = new Vector2(0f, 58f);

        public Vector2 leftShoulder = new Vector2(-132f, 66f);
        public Vector2 rightShoulder = new Vector2(132f, 66f);
        public Vector2 elbow = new Vector2(0f, -154f);
        public Vector2 wrist = new Vector2(0f, -137f);

        public Vector2 leftHip = new Vector2(-70f, -30f);
        public Vector2 rightHip = new Vector2(70f, -30f);
        public Vector2 knee = new Vector2(0f, -194f);
        public Vector2 ankle = new Vector2(0f, -174f);

        public static CharacterSkeletonDefinition CreateDefault()
        {
            return new CharacterSkeletonDefinition();
        }
    }

    [Serializable]
    public struct CharacterAppearance
    {
        public int stage;
        public Color skin;
        public Color hair;
        public Color top;
        public Color bottom;
        public Color shoes;
        public Color accent;
        public Color outline;
        public float chestWidth;
        public float bellyWidth;
        public float hipWidth;
        public float shoulderWidth;
        public float armWidth;
        public float legWidth;
        public float headScale;
        public float heightScale;
        public float chinScale;
        public float softness;
        public float bellyDrop;
        public float sideDepth;
        public float slouch;
        public float shirtWear;
        public bool accentVisible;
        public CharacterExpression defaultExpression;

        public bool IsValid =>
            stage >= 0 &&
            chestWidth > 0f &&
            bellyWidth > 0f &&
            armWidth > 0f &&
            legWidth > 0f &&
            chinScale > 0f &&
            softness > 0f &&
            bellyDrop >= 0f;

        public static CharacterAppearance Create(int artIndex)
        {
            int stage = Mathf.Clamp(artIndex, 0, 3);
            Color skin = stage == 0
                ? new Color(0.88f, 0.53f, 0.36f, 1f)
                : new Color(0.91f, 0.56f, 0.38f, 1f);
            Color outline =
                new Color(0.105f, 0.060f, 0.042f, 1f);

            return stage switch
            {
                0 => new CharacterAppearance
                {
                    stage = stage,
                    skin = skin,
                    hair = new Color(0.115f, 0.061f, 0.033f, 1f),
                    top = new Color(0.31f, 0.36f, 0.39f, 1f),
                    bottom = new Color(0.105f, 0.102f, 0.105f, 1f),
                    shoes = new Color(0.71f, 0.67f, 0.57f, 1f),
                    accent = new Color(0.98f, 0.57f, 0.09f, 0f),
                    outline = outline,
                    chestWidth = 1.38f,
                    bellyWidth = 1.72f,
                    hipWidth = 1.34f,
                    shoulderWidth = 0.98f,
                    armWidth = 1.23f,
                    legWidth = 1.28f,
                    headScale = 1.10f,
                    heightScale = 0.95f,
                    chinScale = 1.20f,
                    softness = 1.22f,
                    bellyDrop = 34f,
                    sideDepth = 1.30f,
                    slouch = 1f,
                    shirtWear = 1f,
                    accentVisible = false,
                    defaultExpression = CharacterExpression.Tired
                },
                1 => new CharacterAppearance
                {
                    stage = stage,
                    skin = skin,
                    hair = new Color(0.10f, 0.055f, 0.03f, 1f),
                    top = new Color(0.19f, 0.43f, 0.35f, 1f),
                    bottom = new Color(0.085f, 0.10f, 0.13f, 1f),
                    shoes = new Color(0.63f, 0.63f, 0.59f, 1f),
                    accent = new Color(0.12f, 0.64f, 0.90f, 0f),
                    outline = outline,
                    chestWidth = 1.34f,
                    bellyWidth = 1.55f,
                    hipWidth = 1.27f,
                    shoulderWidth = 1.04f,
                    armWidth = 1.20f,
                    legWidth = 1.23f,
                    headScale = 1.07f,
                    heightScale = 0.97f,
                    chinScale = 1.13f,
                    softness = 1.05f,
                    bellyDrop = 29f,
                    sideDepth = 1.24f,
                    slouch = 0.78f,
                    shirtWear = 0.78f,
                    accentVisible = false,
                    defaultExpression = CharacterExpression.Relaxed
                },
                2 => new CharacterAppearance
                {
                    stage = stage,
                    skin = skin,
                    hair = new Color(0.085f, 0.045f, 0.025f, 1f),
                    top = new Color(0.51f, 0.14f, 0.12f, 1f),
                    bottom = new Color(0.055f, 0.075f, 0.12f, 1f),
                    shoes = new Color(0.12f, 0.16f, 0.20f, 1f),
                    accent = new Color(1f, 0.65f, 0.08f, 1f),
                    outline = outline,
                    chestWidth = 1.39f,
                    bellyWidth = 1.36f,
                    hipWidth = 1.19f,
                    shoulderWidth = 1.15f,
                    armWidth = 1.27f,
                    legWidth = 1.19f,
                    headScale = 1.03f,
                    heightScale = 0.99f,
                    chinScale = 1.04f,
                    softness = 0.84f,
                    bellyDrop = 23f,
                    sideDepth = 1.18f,
                    slouch = 0.48f,
                    shirtWear = 0.52f,
                    accentVisible = true,
                    defaultExpression = CharacterExpression.Focused
                },
                _ => new CharacterAppearance
                {
                    stage = stage,
                    skin = skin,
                    hair = new Color(0.07f, 0.035f, 0.02f, 1f),
                    top = new Color(0.96f, 0.45f, 0.055f, 1f),
                    bottom = new Color(0.035f, 0.05f, 0.085f, 1f),
                    shoes = new Color(0.08f, 0.11f, 0.15f, 1f),
                    accent = new Color(0.13f, 0.71f, 1f, 1f),
                    outline = outline,
                    chestWidth = 1.48f,
                    bellyWidth = 1.18f,
                    hipWidth = 1.15f,
                    shoulderWidth = 1.27f,
                    armWidth = 1.37f,
                    legWidth = 1.25f,
                    headScale = 1.00f,
                    heightScale = 1.01f,
                    chinScale = 0.94f,
                    softness = 0.62f,
                    bellyDrop = 17f,
                    sideDepth = 1.12f,
                    slouch = 0.22f,
                    shirtWear = 0.28f,
                    accentVisible = true,
                    defaultExpression = CharacterExpression.Happy
                }
            };
        }
    }
}
