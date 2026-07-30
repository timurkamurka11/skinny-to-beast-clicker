using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Read-only close-up review for the four independently assembled Patch 4
    /// facial states. This window never writes readiness or runtime state.
    /// </summary>
    public sealed class Patch4FacePoseReviewWindow : EditorWindow
    {
        private static readonly string[] Labels =
        {
            "NEUTRAL",
            "BLINK",
            "OPEN MOUTH",
            "SMILE"
        };

        private static readonly Color[] Accents =
        {
            new(0.25f, 0.82f, 0.47f),
            new(0.44f, 0.66f, 1f),
            new(1f, 0.68f, 0.29f),
            new(0.93f, 0.41f, 0.64f)
        };

        private Texture2D contactSheet;

        [MenuItem(
            "Tools/GameWork/Patch 4.0/Validation/Open Face Pose Review")]
        public static void Open()
        {
            Patch4FacePoseReviewWindow window =
                GetWindow<Patch4FacePoseReviewWindow>(
                    "Patch 4 Face Poses");
            window.minSize = new Vector2(960f, 420f);
            window.position = new Rect(80f, 80f, 1420f, 620f);
            window.Reload();
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            Reload();
        }

        private void OnDisable()
        {
            DestroyTexture();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "GameWork Patch 4.0 — Locked Independent Face Review",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Neutral, blink, open-mouth and smile are assembled from " +
                "independent painted layers. This review is read-only; human " +
                "approval is still required and production activation remains " +
                "locked.",
                MessageType.Info);

            if (contactSheet == null)
            {
                EditorGUILayout.HelpBox(
                    "Face-pose preview is unavailable. See: " +
                    Patch4NeutralPoseValidator.ReportPath,
                    MessageType.Warning);
                return;
            }

            Rect area = GUILayoutUtility.GetRect(
                400f,
                5000f,
                260f,
                5000f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            DrawPanels(area);

            EditorGUILayout.LabelField(
                "Report: " + Patch4NeutralPoseValidator.ReportPath,
                EditorStyles.miniLabel);
        }

        private void DrawPanels(Rect area)
        {
            const float gap = 10f;
            const float labelHeight = 22f;
            float panelWidth = Mathf.Max(
                1f,
                (area.width - gap * (Labels.Length - 1)) /
                Labels.Length);
            float availableHeight = Mathf.Max(
                1f,
                area.height - labelHeight);
            float imageHeight = Mathf.Min(
                availableHeight,
                panelWidth * 1.125f);
            float top = area.y +
                Mathf.Max(
                    0f,
                    (availableHeight - imageHeight) * 0.5f);

            for (int i = 0; i < Labels.Length; i++)
            {
                Rect imageRect = new(
                    area.x + i * (panelWidth + gap),
                    top,
                    panelWidth,
                    imageHeight);
                Rect uv = new(
                    i / (float)Labels.Length,
                    0f,
                    1f / Labels.Length,
                    1f);
                GUI.DrawTextureWithTexCoords(
                    imageRect,
                    contactSheet,
                    uv,
                    true);

                Rect labelRect = new(
                    imageRect.x,
                    imageRect.yMax,
                    imageRect.width,
                    labelHeight);
                Color previous = GUI.color;
                GUI.color = Accents[i];
                GUI.Box(
                    labelRect,
                    Labels[i],
                    EditorStyles.helpBox);
                GUI.color = previous;
            }
        }

        private void Reload()
        {
            DestroyTexture();
            contactSheet = LoadTexture(
                Patch4NeutralPoseValidator.FacePoseContactSheetPath);
            Repaint();
        }

        private void DestroyTexture()
        {
            if (contactSheet == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(contactSheet);
            contactSheet = null;
        }

        private static Texture2D LoadTexture(string path)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                !File.Exists(path))
            {
                return null;
            }

            try
            {
                Texture2D texture = new(
                    2,
                    2,
                    TextureFormat.RGBA32,
                    false,
                    false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                if (texture.LoadImage(File.ReadAllBytes(path), false))
                {
                    return texture;
                }

                UnityEngine.Object.DestroyImmediate(texture);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Patch 4 face review could not load " + path +
                    ": " + exception.Message);
            }

            return null;
        }
    }
}
