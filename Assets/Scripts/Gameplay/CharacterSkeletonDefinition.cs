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
        public bool accentVisible;
        public CharacterExpression defaultExpression;

        public bool IsValid =>
            stage >= 0 &&
            chestWidth > 0f &&
            bellyWidth > 0f &&
            armWidth > 0f &&
            legWidth > 0f;

        public static CharacterAppearance Create(int artIndex)
        {
            int stage = Mathf.Clamp(artIndex, 0, 3);
            Color skin = stage == 0
                ? new Color(0.88f, 0.48f, 0.31f, 1f)
                : new Color(0.91f, 0.51f, 0.32f, 1f);
            Color outline = new Color(0.075f, 0.045f, 0.035f, 1f);

            return stage switch
            {
                0 => new CharacterAppearance
                {
                    stage = stage,
                    skin = skin,
                    hair = new Color(0.12f, 0.065f, 0.035f, 1f),
                    top = new Color(0.09f, 0.43f, 0.62f, 1f),
                    bottom = new Color(0.075f, 0.12f, 0.20f, 1f),
                    shoes = new Color(0.90f, 0.93f, 0.94f, 1f),
                    accent = new Color(0.98f, 0.57f, 0.09f, 0f),
                    outline = outline,
                    chestWidth = 0.78f,
                    bellyWidth = 0.72f,
                    hipWidth = 0.78f,
                    shoulderWidth = 0.78f,
                    armWidth = 0.68f,
                    legWidth = 0.74f,
                    headScale = 1.06f,
                    heightScale = 0.98f,
                    accentVisible = false,
                    defaultExpression = CharacterExpression.Tired
                },
                1 => new CharacterAppearance
                {
                    stage = stage,
                    skin = skin,
                    hair = new Color(0.10f, 0.055f, 0.03f, 1f),
                    top = new Color(0.12f, 0.55f, 0.34f, 1f),
                    bottom = new Color(0.065f, 0.10f, 0.17f, 1f),
                    shoes = new Color(0.93f, 0.94f, 0.94f, 1f),
                    accent = new Color(0.12f, 0.64f, 0.90f, 0f),
                    outline = outline,
                    chestWidth = 1.06f,
                    bellyWidth = 1.04f,
                    hipWidth = 1.02f,
                    shoulderWidth = 1.02f,
                    armWidth = 1.02f,
                    legWidth = 1f,
                    headScale = 1f,
                    heightScale = 1f,
                    accentVisible = false,
                    defaultExpression = CharacterExpression.Relaxed
                },
                2 => new CharacterAppearance
                {
                    stage = stage,
                    skin = skin,
                    hair = new Color(0.085f, 0.045f, 0.025f, 1f),
                    top = new Color(0.76f, 0.16f, 0.12f, 1f),
                    bottom = new Color(0.055f, 0.075f, 0.12f, 1f),
                    shoes = new Color(0.12f, 0.16f, 0.20f, 1f),
                    accent = new Color(1f, 0.65f, 0.08f, 1f),
                    outline = outline,
                    chestWidth = 1.18f,
                    bellyWidth = 0.98f,
                    hipWidth = 1.02f,
                    shoulderWidth = 1.13f,
                    armWidth = 1.17f,
                    legWidth = 1.10f,
                    headScale = 0.98f,
                    heightScale = 1.015f,
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
                    chestWidth = 1.32f,
                    bellyWidth = 1.02f,
                    hipWidth = 1.08f,
                    shoulderWidth = 1.25f,
                    armWidth = 1.32f,
                    legWidth = 1.22f,
                    headScale = 0.96f,
                    heightScale = 1.03f,
                    accentVisible = true,
                    defaultExpression = CharacterExpression.Happy
                }
            };
        }
    }
}
