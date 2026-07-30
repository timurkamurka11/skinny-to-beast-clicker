using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Read-only review surface for the locked quality master, assembled pose
    /// and pixel difference. Opening this window does not alter readiness or
    /// runtime visibility.
    /// </summary>
    public sealed class Patch4NeutralPoseReviewWindow : EditorWindow
    {
        private Texture2D master;
        private Texture2D composite;
        private Texture2D difference;

        [MenuItem(
            "Tools/GameWork/Patch 4.0/Validation/Open Neutral Pose Review")]
        public static void Open()
        {
            Patch4NeutralPoseReviewWindow window =
                GetWindow<Patch4NeutralPoseReviewWindow>(
                    "Patch 4 Neutral Pose");
            window.minSize = new Vector2(960f, 560f);
            window.position = new Rect(50f, 50f, 1480f, 820f);
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
            DestroyTextures();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "GameWork Patch 4.0 — Locked Neutral Pose Review",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Blue: locked quality master  •  Green: assembled neutral pose  •  " +
                "Red: pixel difference. This window is read-only. Human art " +
                "review is still required and production activation remains " +
                "locked.",
                MessageType.Info);

            if (master == null ||
                composite == null ||
                difference == null)
            {
                EditorGUILayout.HelpBox(
                    "Neutral-pose preview files are unavailable. See: " +
                    Patch4NeutralPoseValidator.ReportPath,
                    MessageType.Warning);
                return;
            }

            Rect area = GUILayoutUtility.GetRect(
                300f,
                5000f,
                300f,
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
                (area.width - gap * 2f) / 3f);
            float availableHeight = Mathf.Max(
                1f,
                area.height - labelHeight);
            float imageHeight = Mathf.Min(
                availableHeight,
                panelWidth * 1.5f);
            float top = area.y +
                Mathf.Max(
                    0f,
                    (availableHeight - imageHeight) * 0.5f);

            DrawPanel(
                new Rect(
                    area.x,
                    top,
                    panelWidth,
                    imageHeight),
                "LOCKED QUALITY MASTER",
                master,
                new Color(0.28f, 0.60f, 1f));
            DrawPanel(
                new Rect(
                    area.x + panelWidth + gap,
                    top,
                    panelWidth,
                    imageHeight),
                "ASSEMBLED NEUTRAL",
                composite,
                new Color(0.25f, 0.82f, 0.47f));
            DrawPanel(
                new Rect(
                    area.x + (panelWidth + gap) * 2f,
                    top,
                    panelWidth,
                    imageHeight),
                "PIXEL DIFFERENCE",
                difference,
                new Color(1f, 0.36f, 0.36f));
        }

        private static void DrawPanel(
            Rect imageRect,
            string title,
            Texture texture,
            Color accent)
        {
            Rect labelRect = new(
                imageRect.x,
                imageRect.yMax,
                imageRect.width,
                22f);
            Color previous = GUI.color;
            GUI.color = accent;
            GUI.Box(labelRect, title, EditorStyles.helpBox);
            GUI.color = previous;

            EditorGUI.DrawTextureTransparent(
                imageRect,
                texture,
                ScaleMode.ScaleToFit);
        }

        private void Reload()
        {
            DestroyTextures();
            master = LoadTexture(
                ToAbsolutePath(
                    Patch4MaskDrivenLayerBaker.MasterPath));
            composite = LoadTexture(
                Patch4NeutralPoseValidator.CompositePath);
            difference = LoadTexture(
                Patch4NeutralPoseValidator.DifferencePath);
            Repaint();
        }

        private void DestroyTextures()
        {
            DestroyTexture(ref master);
            DestroyTexture(ref composite);
            DestroyTexture(ref difference);
        }

        private static void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(texture);
            texture = null;
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
                    "Patch 4 neutral review could not load " + path +
                    ": " + exception.Message);
            }

            return null;
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                Application.dataPath;
            return Path.Combine(
                projectRoot,
                assetPath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
        }
    }
}
