using System.Collections.Generic;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Owns the isolated Patch 4 rig and keeps Patch 3.5 available as rollback.
    /// Patch 4 never activates unless its complete named skeleton is present.
    /// </summary>
    [DefaultExecutionOrder(900)]
    [DisallowMultipleComponent]
    public sealed class Patch4CharacterRigController : MonoBehaviour
    {
        [Header("Patch 4 Rig")]
        [SerializeField] private Transform rigRoot;
        [SerializeField] private GameObject patch4VisualRoot;

        [Header("Rollback")]
        [SerializeField] private GameObject patch35RollbackRoot;
        [SerializeField] private bool patch4Enabled;

        [Header("Validation")]
        [SerializeField] private bool validateOnAwake = true;
        [SerializeField] private bool logValidationErrors = true;

        private readonly Dictionary<string, Transform> bones = new();
        private readonly List<string> missingBones = new();
        private bool rigValid;

        public bool Patch4Enabled => patch4Enabled && rigValid;
        public bool IsRigValid => rigValid;
        public IReadOnlyList<string> MissingBones => missingBones;
        public Transform RigRoot => rigRoot;

        private void Awake()
        {
            if (validateOnAwake)
            {
                RebuildBoneMap();
            }

            ApplyVisualState();
        }

        private void OnEnable()
        {
            if (bones.Count == 0)
            {
                RebuildBoneMap();
            }

            ApplyVisualState();
        }

        public bool RebuildBoneMap()
        {
            bones.Clear();
            missingBones.Clear();

            if (rigRoot == null)
            {
                rigValid = false;
                missingBones.Add("<rigRoot>");
                LogFailure("Patch 4 rigRoot is not assigned.");
                return false;
            }

            CacheHierarchy(rigRoot);

            foreach (string requiredName in Patch4RigContract.RequiredBoneNames)
            {
                if (!bones.ContainsKey(requiredName))
                {
                    missingBones.Add(requiredName);
                }
            }

            rigValid = missingBones.Count == 0;
            if (!rigValid)
            {
                LogFailure(
                    "Patch 4 skeleton is incomplete. Missing: " +
                    string.Join(", ", missingBones));
            }

            return rigValid;
        }

        public bool TryGetBone(string boneName, out Transform bone)
        {
            if (bones.Count == 0)
            {
                RebuildBoneMap();
            }

            return bones.TryGetValue(boneName, out bone);
        }

        public Transform GetBone(string boneName)
        {
            TryGetBone(boneName, out Transform bone);
            return bone;
        }

        public bool SetPatch4Enabled(bool enabled)
        {
            if (enabled && !RebuildBoneMap())
            {
                patch4Enabled = false;
                ApplyVisualState();
                return false;
            }

            patch4Enabled = enabled;
            ApplyVisualState();
            return Patch4Enabled == enabled;
        }

        private void ApplyVisualState()
        {
            bool showPatch4 = patch4Enabled && rigValid;

            if (patch4VisualRoot != null &&
                patch4VisualRoot.activeSelf != showPatch4)
            {
                patch4VisualRoot.SetActive(showPatch4);
            }

            if (patch35RollbackRoot != null &&
                patch35RollbackRoot.activeSelf == showPatch4)
            {
                patch35RollbackRoot.SetActive(!showPatch4);
            }
        }

        private void CacheHierarchy(Transform node)
        {
            if (!bones.ContainsKey(node.name))
            {
                bones.Add(node.name, node);
            }

            for (int i = 0; i < node.childCount; i++)
            {
                CacheHierarchy(node.GetChild(i));
            }
        }

        private void LogFailure(string message)
        {
            if (logValidationErrors)
            {
                Debug.LogWarning(message, this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                // Keep rollback visible while the source art and Sprite Skin are
                // still being assembled in the editor.
                if (patch4VisualRoot != null && !patch4Enabled)
                {
                    patch4VisualRoot.SetActive(false);
                }

                if (patch35RollbackRoot != null && !patch4Enabled)
                {
                    patch35RollbackRoot.SetActive(true);
                }
            }
        }
#endif
    }
}
