using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    [CustomEditor(typeof(Patch4ArtReadinessAsset))]
    public sealed class Patch4ArtReadinessInspector : UnityEditor.Editor
    {
        private const string ExpectedSha =
            "5873cf6df0df2b5ebd4947b687693162d4b34899202326d1b1ae62df9f50587c";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty approved =
                serializedObject.FindProperty("productionArtApproved");
            SerializedProperty sha =
                serializedObject.FindProperty("approvedSourceSha256");
            SerializedProperty approvedBy =
                serializedObject.FindProperty("approvedBy");
            SerializedProperty notes =
                serializedObject.FindProperty("reviewNotes");

            EditorGUILayout.HelpBox(
                "This asset is the final human gate for Patch 4. Automated " +
                "mask cutting, successful PNG import, or technical validation " +
                "must never approve it automatically.",
                MessageType.Warning);

            bool previous = approved.boolValue;
            EditorGUILayout.PropertyField(approved);
            EditorGUILayout.PropertyField(sha);
            EditorGUILayout.PropertyField(approvedBy);
            EditorGUILayout.PropertyField(notes);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Expected master SHA-256", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(ExpectedSha, EditorStyles.textField, GUILayout.Height(36f));

            if (GUILayout.Button("Fill Expected Master SHA"))
            {
                sha.stringValue = ExpectedSha;
            }

            if (GUILayout.Button("Lock Patch 4 Art Again"))
            {
                approved.boolValue = false;
                approvedBy.stringValue = string.Empty;
            }

            if (!previous && approved.boolValue)
            {
                bool shaMatches = string.Equals(
                    sha.stringValue,
                    ExpectedSha,
                    System.StringComparison.OrdinalIgnoreCase);
                bool confirmed = shaMatches && EditorUtility.DisplayDialog(
                    "Approve GameWork Patch 4.0 production art?",
                    "Confirm only after hidden joint continuations, all facial " +
                    "poses, pixel coverage, joint overlap, Animator clips, " +
                    "Play Mode and rollback behavior were reviewed. This will " +
                    "allow the Patch 4 character to be enabled.",
                    "Approve exact SHA",
                    "Keep locked");

                if (!confirmed)
                {
                    approved.boolValue = false;
                    if (!shaMatches)
                    {
                        Debug.LogError(
                            "Patch 4 art approval rejected because the source " +
                            "SHA does not match the approved neutral master.",
                            target);
                    }
                }
            }

            if (approved.boolValue &&
                !string.Equals(
                    sha.stringValue,
                    ExpectedSha,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                EditorGUILayout.HelpBox(
                    "Approval is ineffective because the stored SHA does not " +
                    "match the approved master.",
                    MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
