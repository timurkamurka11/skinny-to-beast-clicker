using System.Collections.Generic;
using SkinnyToBeast.Gameplay;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Owns the isolated Patch 4 rig and keeps Patch 3.5 available as rollback.
    /// Patch 4 never activates unless its complete named skeleton and explicitly
    /// approved production art are both present.
    /// </summary>
    [DefaultExecutionOrder(900)]
    [DisallowMultipleComponent]
    public sealed class Patch4CharacterRigController : MonoBehaviour
    {
        [Header("Patch 4 Rig")]
        [SerializeField] private Transform rigRoot;
        [SerializeField] private GameObject patch4VisualRoot;

        [Header("Production Art Gate")]
        [SerializeField] private Patch4ArtReadinessAsset artReadiness;
        [SerializeField] private string expectedSourceSha256 =
            "7b151f1ded93f3852bc8a7218ab26f94298b7f822094304bbcea9c076cad72a3";

        [Header("Rollback")]
        [SerializeField] private GameObject patch35RollbackRoot;
        [SerializeField] private bool patch4Enabled;

        [Header("Validation")]
        [SerializeField] private bool validateOnAwake = true;
        [SerializeField] private bool logValidationErrors = true;

        private readonly Dictionary<string, Transform> bones = new();
        private readonly List<string> missingBones = new();
        private CharacterSpriteRigController rollbackRenderer;
        private bool rigValid;

        public bool IsArtApproved =>
            artReadiness != null &&
            artReadiness.IsApprovedFor(expectedSourceSha256);

        public bool Patch4Enabled =>
            patch4Enabled && rigValid && IsArtApproved;

        public bool IsRigValid => rigValid;
        public bool HasRollbackBinding =>
            patch35RollbackRoot != null && rollbackRenderer != null;
        public bool IsRuntimeReady =>
            TryGetRuntimeReadinessError(out _);
        public string RuntimeReadinessError
        {
            get
            {
                TryGetRuntimeReadinessError(out string error);
                return error;
            }
        }
        public IReadOnlyList<string> MissingBones => missingBones;
        public Transform RigRoot => rigRoot;
        public Patch4ArtReadinessAsset ArtReadiness => artReadiness;
        public string ExpectedSourceSha256 => expectedSourceSha256;

        private void Awake()
        {
            if (validateOnAwake)
            {
                RebuildBoneMap();
            }

            SynchronizeVisualState();
        }

        private void OnEnable()
        {
            if (bones.Count == 0)
            {
                RebuildBoneMap();
            }

            SynchronizeVisualState();
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

        public void BindRollbackRoot(GameObject rollbackRoot)
        {
            bool bindingChanged = patch35RollbackRoot != rollbackRoot;
            patch35RollbackRoot = rollbackRoot;
            rollbackRenderer = rollbackRoot != null
                ? rollbackRoot.GetComponentInParent<
                    CharacterSpriteRigController>(true)
                : null;
            if (bindingChanged)
            {
                patch4Enabled = false;
            }

            RebuildBoneMap();
            SynchronizeVisualState();
        }

        public bool SetPatch4Enabled(bool enabled)
        {
            if (enabled)
            {
                if (!RebuildBoneMap())
                {
                    patch4Enabled = false;
                    SynchronizeVisualState();
                    return false;
                }

                if (!IsArtApproved)
                {
                    patch4Enabled = false;
                    SynchronizeVisualState();
                    LogFailure(
                        "Patch 4 activation rejected: production art is not " +
                        "approved for the expected master SHA-256. Draft mask " +
                        "layers can never approve this gate automatically.");
                    return false;
                }
            }

            patch4Enabled = enabled;
            SynchronizeVisualState();
            return Patch4Enabled == enabled;
        }

        public bool SynchronizeVisualState()
        {
            bool showPatch4 = Patch4Enabled;
            bool repaired = false;

            if (patch4VisualRoot != null &&
                patch4VisualRoot.activeSelf != showPatch4)
            {
                patch4VisualRoot.SetActive(showPatch4);
                repaired = true;
            }

            if (rollbackRenderer != null)
            {
                if (patch35RollbackRoot != null &&
                    !patch35RollbackRoot.activeSelf)
                {
                    patch35RollbackRoot.SetActive(true);
                    repaired = true;
                }

                if (rollbackRenderer.PixelsSuppressedByReplacement !=
                    showPatch4)
                {
                    rollbackRenderer.SetReplacementPixelsSuppressed(
                        showPatch4);
                    repaired = true;
                }
            }
            else if (patch35RollbackRoot != null &&
                     patch35RollbackRoot.activeSelf == showPatch4)
            {
                // A generated or legacy rig without a renderer owner is never
                // runtime-ready, but keep the old root-toggle fallback so a
                // broken candidate still restores Patch 3.5 safely.
                patch35RollbackRoot.SetActive(!showPatch4);
                repaired = true;
            }

            return repaired;
        }

        public bool TryGetRuntimeReadinessError(out string error)
        {
            if (patch4VisualRoot == null)
            {
                error = "Patch4VisualRoot is not assigned.";
                return false;
            }

            if (!RebuildBoneMap())
            {
                error =
                    "the required Patch 4 rig hierarchy is incomplete.";
                return false;
            }

            if (!IsArtApproved)
            {
                error =
                    "production art is not approved for the expected master " +
                    "SHA-256. Draft mask layers cannot approve this gate.";
                return false;
            }

            Patch4CharacterStateMachine stateMachine =
                GetComponent<Patch4CharacterStateMachine>();
            if (stateMachine == null || !stateMachine.IsConfigured)
            {
                error = stateMachine != null
                    ? stateMachine.AnimatorReadinessError
                    : "Patch4CharacterStateMachine is missing.";
                return false;
            }

            Patch4CanvasPresentation canvasPresentation =
                GetComponent<Patch4CanvasPresentation>();
            if (canvasPresentation == null ||
                !canvasPresentation.IsCanvasReady)
            {
                error =
                    "the Canvas skin/presentation is not configured for the " +
                    "LivingGameplayScene room.";
                return false;
            }

            Patch4V23FullFramePresentation fullFramePresentation =
                GetComponent<Patch4V23FullFramePresentation>();
            if (fullFramePresentation == null ||
                !fullFramePresentation.IsReady)
            {
                error =
                    "the continuous full-frame presentation is not ready.";
                return false;
            }

            Patch4LegacySignalBridge signalBridge =
                GetComponent<Patch4LegacySignalBridge>();
            if (signalBridge == null || !signalBridge.IsBound)
            {
                error =
                    "the authoritative legacy gameplay/skin signal bridge " +
                    "is not configured.";
                return false;
            }

            if (!HasRollbackBinding)
            {
                error =
                    "the Patch 3.5 renderer-owned rollback binding is missing.";
                return false;
            }

            if (GetComponent<Patch4CharacterVisibilityGuard>() == null)
            {
                error = "Patch4CharacterVisibilityGuard is missing.";
                return false;
            }

            if (GetComponent<Patch4V21FootPlantController>() != null)
            {
                error =
                    "the obsolete V21 foot solver is still attached and would " +
                    "compete with Animator-owned leg channels.";
                return false;
            }

            error = string.Empty;
            return true;
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
                bool canShowPatch4 =
                    patch4Enabled && rigValid && IsArtApproved;

                if (patch4VisualRoot != null &&
                    patch4VisualRoot.activeSelf != canShowPatch4)
                {
                    patch4VisualRoot.SetActive(canShowPatch4);
                }

                if (patch35RollbackRoot != null &&
                    patch35RollbackRoot.activeSelf == canShowPatch4)
                {
                    patch35RollbackRoot.SetActive(!canShowPatch4);
                }
            }
        }
#endif
    }
}
