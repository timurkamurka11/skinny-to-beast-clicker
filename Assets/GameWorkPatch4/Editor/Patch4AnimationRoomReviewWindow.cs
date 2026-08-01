using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    /// <summary>
    /// Read-only result surface for the ten clips sampled in the actual room.
    /// The window contains no readiness or activation controls.
    /// </summary>
    public sealed class Patch4AnimationRoomReviewWindow : EditorWindow
    {
        [Serializable]
        private sealed class ReviewStatus
        {
            public string runToken = string.Empty;
            public bool completed;
            public bool passedTechnicalChecks;
            public string generatedUtc = string.Empty;
            public string error = string.Empty;
        }

        private const int Columns = 5;
        private const int Rows = 2;

        private static readonly Color[] Accents =
        {
            new(0.22f, 0.78f, 0.48f),
            new(0.31f, 0.62f, 0.98f),
            new(0.97f, 0.65f, 0.25f),
            new(0.92f, 0.38f, 0.65f),
            new(0.55f, 0.78f, 0.95f),
            new(0.45f, 0.82f, 0.82f),
            new(0.89f, 0.74f, 0.28f),
            new(0.76f, 0.58f, 0.96f),
            new(0.94f, 0.52f, 0.36f),
            new(0.42f, 0.84f, 0.42f)
        };

        private Texture2D contactSheet;
        private ReviewStatus reviewStatus;

        [MenuItem(
            "Tools/GameWork/Patch 4.0/Validation/" +
            "Open Animation Room Review")]
        public static void Open()
        {
            Patch4AnimationRoomReviewWindow window =
                GetWindow<Patch4AnimationRoomReviewWindow>(
                    "Patch 4 Room Animations");
            window.minSize = new Vector2(1000f, 620f);
            window.position = new Rect(70f, 60f, 1500f, 900f);
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
            reviewStatus = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "GameWork Patch 4.0 — Locked Actual-Room Animation Review",
                EditorStyles.boldLabel);
            bool currentRun =
                reviewStatus != null &&
                !string.IsNullOrWhiteSpace(
                    Patch4AnimationRoomReview.CurrentRunToken) &&
                string.Equals(
                    reviewStatus.runToken,
                    Patch4AnimationRoomReview.CurrentRunToken,
                    StringComparison.Ordinal);
            bool passed =
                currentRun &&
                reviewStatus.completed &&
                reviewStatus.passedTechnicalChecks;
            if (passed)
            {
                EditorGUILayout.HelpBox(
                    "This is a fresh completed review. These ten frames were " +
                    "captured while the generated character played every " +
                    "required clip inside the real LivingGameplayScene with " +
                    "one intact continuous Canvas body, constrained anatomical " +
                    "weights and frozen bind anchors. Every peak retained the " +
                    "neutral silhouette without excessive expansion, and the " +
                    "walk also passed focused arm/leg articulation. The legacy " +
                    "robot-like footstep was " +
                    "paused only during this review and restored afterward. " +
                    "Human review is still required and activation remains " +
                    "locked.",
                    MessageType.Info);
            }
            else if (currentRun && reviewStatus.completed)
            {
                EditorGUILayout.HelpBox(
                    "This contact sheet belongs to a fresh review that FAILED " +
                    "technical checks. It is shown only for diagnosis and must " +
                    "not be accepted. " + reviewStatus.error,
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No fresh completed animation-room report is available. " +
                    "An older contact sheet is deliberately blocked.",
                    MessageType.Warning);
            }

            if (contactSheet == null)
            {
                EditorGUILayout.HelpBox(
                    "Animation-room contact sheet is unavailable. See: " +
                    Patch4AnimationRoomReview.ReportPath,
                    MessageType.Warning);
                return;
            }

            Rect available = GUILayoutUtility.GetRect(
                800f,
                5000f,
                420f,
                5000f,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            float aspect =
                contactSheet.width / (float)contactSheet.height;
            Rect imageArea = FitAspect(available, aspect);
            GUI.DrawTexture(
                imageArea,
                contactSheet,
                ScaleMode.StretchToFill,
                false);
            DrawLabels(imageArea);

            EditorGUILayout.LabelField(
                "Report: " + Patch4AnimationRoomReview.ReportPath,
                EditorStyles.miniLabel);
        }

        private void DrawLabels(Rect imageArea)
        {
            float cellWidth = imageArea.width / Columns;
            float cellHeight = imageArea.height / Rows;
            for (int i = 0;
                 i < Patch4RigContract.RequiredClipNames.Count;
                 i++)
            {
                int column = i % Columns;
                int row = i / Columns;
                Rect label = new(
                    imageArea.x + column * cellWidth + 4f,
                    imageArea.y + row * cellHeight + 4f,
                    cellWidth - 8f,
                    24f);
                Color previous = GUI.color;
                GUI.color = Accents[i];
                GUI.Box(
                    label,
                    (i + 1) + ". " +
                    Patch4RigContract.RequiredClipNames[i],
                    EditorStyles.helpBox);
                GUI.color = previous;
            }
        }

        private void Reload()
        {
            DestroyTexture();
            reviewStatus = LoadReviewStatus(
                Patch4AnimationRoomReview.ReportPath);
            if (reviewStatus == null ||
                !reviewStatus.completed ||
                string.IsNullOrWhiteSpace(
                    Patch4AnimationRoomReview.CurrentRunToken) ||
                !string.Equals(
                    reviewStatus.runToken,
                    Patch4AnimationRoomReview.CurrentRunToken,
                    StringComparison.Ordinal))
            {
                Repaint();
                return;
            }

            contactSheet = LoadTexture(
                Patch4AnimationRoomReview.ContactSheetPath);
            Repaint();
        }

        private static ReviewStatus LoadReviewStatus(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<ReviewStatus>(
                    File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Patch 4 animation review could not read " +
                    path + ": " + exception.Message);
                return null;
            }
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
                if (texture.LoadImage(
                    File.ReadAllBytes(path),
                    false))
                {
                    return texture;
                }

                UnityEngine.Object.DestroyImmediate(texture);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "Patch 4 animation review could not load " +
                    path + ": " + exception.Message);
            }

            return null;
        }

        private static Rect FitAspect(Rect bounds, float aspect)
        {
            float width = bounds.width;
            float height = width / Mathf.Max(0.01f, aspect);
            if (height > bounds.height)
            {
                height = bounds.height;
                width = height * aspect;
            }

            return new Rect(
                bounds.x + (bounds.width - width) * 0.5f,
                bounds.y + (bounds.height - height) * 0.5f,
                width,
                height);
        }
    }
}
