using System.Collections.Generic;
using SkinnyToBeast.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4
{
    [DefaultExecutionOrder(900)]
    [DisallowMultipleComponent]
    public sealed class Patch4CharacterRigController : MonoBehaviour
    {
        public enum PresentationState
        {
            LegacyRollback,
            Patch4Initializing,
            Patch4Production,
            EditorDevelopmentPreview,
            EditorReviewOverride,
            EditorPreviewBlackout
        }

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
        private readonly List<string> duplicateBones = new();
        private readonly List<string> wrongParentBones = new();
        private readonly List<string> invalidTransformBones = new();
        private CharacterSpriteRigController rollbackRenderer;
        private bool rigValid;
        private PresentationState presentationState = PresentationState.LegacyRollback;

#if UNITY_EDITOR
        private UnityEngine.Object editorPresentationOwner;
        private bool editorPresentationIsReview;
        private bool editorPresentationPixelsVisible = true;
        private bool editorLegacyPixelsWereSuppressed;
#endif

        public bool IsArtApproved => artReadiness != null &&
            artReadiness.IsApprovedFor(expectedSourceSha256);
        public bool Patch4Enabled => patch4Enabled && rigValid && IsArtApproved &&
            IsTechnicalRuntimeReady;
        public bool IsRigValid => rigValid;
        public bool HasRollbackBinding => patch35RollbackRoot != null && rollbackRenderer != null;
        public bool IsRuntimeReady => TryGetRuntimeReadinessError(out _);
        public bool IsTechnicalRuntimeReady => TryGetTechnicalReadinessError(out _);
        public string RuntimeReadinessError
        {
            get
            {
                TryGetRuntimeReadinessError(out string error);
                return error;
            }
        }
        public IReadOnlyList<string> MissingBones => missingBones;
        public IReadOnlyList<string> DuplicateBones => duplicateBones;
        public IReadOnlyList<string> WrongParentBones => wrongParentBones;
        public IReadOnlyList<string> InvalidTransformBones => invalidTransformBones;
        public Transform RigRoot => rigRoot;
        public Patch4ArtReadinessAsset ArtReadiness => artReadiness;
        public string ExpectedSourceSha256 => expectedSourceSha256;
        public PresentationState CurrentPresentationState => presentationState;
        public bool IsPatch4PresentationActive => patch4VisualRoot != null &&
            patch4VisualRoot.activeInHierarchy &&
            (presentationState == PresentationState.Patch4Production ||
             presentationState == PresentationState.EditorDevelopmentPreview ||
             presentationState == PresentationState.EditorReviewOverride);
        public bool IsPatch4PresentationVisible =>
            IsPatch4PresentationActive && HasVisiblePixels(patch4VisualRoot);
        public int VisiblePresentationCount
        {
            get
            {
                int count = IsPatch4PresentationVisible ? 1 : 0;
                if (patch35RollbackRoot != null && rollbackRenderer != null &&
                    !rollbackRenderer.PixelsSuppressedByReplacement &&
                    HasVisiblePixels(patch35RollbackRoot))
                {
                    count++;
                }
                return count;
            }
        }
        public bool HasExclusiveVisiblePresentation => VisiblePresentationCount == 1;
        public bool HasEditorPresentationOverride
        {
            get
            {
#if UNITY_EDITOR
                return editorPresentationOwner != null;
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (validateOnAwake) RebuildBoneMap();
            SynchronizeVisualState();
        }

        private void OnEnable()
        {
            if (bones.Count == 0) RebuildBoneMap();
            SynchronizeVisualState();
        }

        public bool RebuildBoneMap()
        {
            bones.Clear();
            missingBones.Clear();
            duplicateBones.Clear();
            wrongParentBones.Clear();
            invalidTransformBones.Clear();
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
                if (!bones.TryGetValue(requiredName, out Transform requiredBone))
                {
                    missingBones.Add(requiredName);
                    continue;
                }
                if (!HasFiniteNonZeroTransform(requiredBone))
                {
                    invalidTransformBones.Add(requiredName);
                }
                if (Patch4RigContract.TryGetRequiredParent(
                        requiredName, out string expectedParent) &&
                    (requiredBone.parent == null ||
                     !string.Equals(requiredBone.parent.name, expectedParent,
                         System.StringComparison.Ordinal)))
                {
                    wrongParentBones.Add(requiredName + "->" +
                        (requiredBone.parent != null ? requiredBone.parent.name : "<null>") +
                        " (expected " + expectedParent + ")");
                }
            }

            rigValid = missingBones.Count == 0 && duplicateBones.Count == 0 &&
                wrongParentBones.Count == 0 && invalidTransformBones.Count == 0;
            if (!rigValid)
            {
                LogFailure("Patch 4 skeleton is invalid. Missing: " +
                    Describe(missingBones) + "; duplicates: " + Describe(duplicateBones) +
                    "; wrong parents: " + Describe(wrongParentBones) +
                    "; invalid transforms: " + Describe(invalidTransformBones) + ".");
            }
            return rigValid;
        }

        public bool TryGetBone(string boneName, out Transform bone)
        {
            if (bones.Count == 0) RebuildBoneMap();
            return bones.TryGetValue(boneName, out bone);
        }

        public Transform GetBone(string boneName)
        {
            TryGetBone(boneName, out Transform bone);
            return bone;
        }

        public void BindRollbackRoot(GameObject rollbackRoot)
        {
            bool changed = patch35RollbackRoot != rollbackRoot;
            patch35RollbackRoot = rollbackRoot;
            rollbackRenderer = rollbackRoot != null
                ? rollbackRoot.GetComponentInParent<CharacterSpriteRigController>(true)
                : null;
            if (changed) patch4Enabled = false;
            RebuildBoneMap();
            ApplyPresentationState();
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
                    LogFailure("Patch 4 activation rejected: production art is not " +
                        "approved for the expected master SHA-256. Draft mask " +
                        "layers can never approve this gate automatically.");
                    return false;
                }
                if (!TryGetTechnicalReadinessError(out string error))
                {
                    patch4Enabled = false;
                    ApplyPresentationState();
                    LogFailure("Patch 4 activation rejected: " + error);
                    return false;
                }
            }
            patch4Enabled = enabled;
            ApplyPresentationState();
            if (enabled && !HasExclusiveVisiblePresentation)
            {
                patch4Enabled = false;
                ApplyPresentationState();
                LogFailure(
                    "Patch 4 activation rejected: the atomic handoff did not " +
                    "produce exactly one visible character presentation.");
                return false;
            }
            return Patch4Enabled == enabled;
        }

        public bool SynchronizeVisualState() => ApplyPresentationState();

        private bool ApplyPresentationState()
        {
            bool showPatch4 = Patch4Enabled;
            if (patch4Enabled && !showPatch4)
            {
                // A production dependency failed after activation. Latch the
                // request off while rolling back so a later guard tick cannot
                // reveal Patch 4 merely because that dependency recovered;
                // the installer must re-run the complete readiness + signal
                // synchronization path before the next forward handoff.
                patch4Enabled = false;
            }
            bool hideLegacy = showPatch4;
#if UNITY_EDITOR
            if (editorPresentationOwner != null)
            {
                if (!TryGetTechnicalReadinessError(out _))
                {
                    editorPresentationOwner = null;
                    editorPresentationIsReview = false;
                    editorPresentationPixelsVisible = true;
                    editorLegacyPixelsWereSuppressed = false;
                    showPatch4 = false;
                    hideLegacy = false;
                    presentationState = PresentationState.LegacyRollback;
                }
                else
                {
                    showPatch4 = editorPresentationPixelsVisible;
                    hideLegacy = true;
                    presentationState = editorPresentationPixelsVisible
                        ? (editorPresentationIsReview
                            ? PresentationState.EditorReviewOverride
                            : PresentationState.EditorDevelopmentPreview)
                        : PresentationState.EditorPreviewBlackout;
                }
            }
            else
#endif
            {
                presentationState = showPatch4
                    ? PresentationState.Patch4Production
                    : (patch35RollbackRoot != null
                        ? PresentationState.LegacyRollback
                        : PresentationState.Patch4Initializing);
            }

            bool repaired = false;
            if (rollbackRenderer != null)
            {
                if (patch35RollbackRoot != null && !patch35RollbackRoot.activeSelf)
                {
                    patch35RollbackRoot.SetActive(true);
                    repaired = true;
                }
                if (hideLegacy && !rollbackRenderer.PixelsSuppressedByReplacement)
                {
                    rollbackRenderer.SetReplacementPixelsSuppressed(true);
                    repaired = true;
                }
            }
            if (!showPatch4 && patch4VisualRoot != null && patch4VisualRoot.activeSelf)
            {
                patch4VisualRoot.SetActive(false);
                repaired = true;
            }
            if (rollbackRenderer == null && hideLegacy &&
                patch35RollbackRoot != null && patch35RollbackRoot.activeSelf)
            {
                patch35RollbackRoot.SetActive(false);
                repaired = true;
            }
            if (showPatch4 && patch4VisualRoot != null && !patch4VisualRoot.activeSelf)
            {
                patch4VisualRoot.SetActive(true);
                repaired = true;
            }
            if (rollbackRenderer != null && !hideLegacy &&
                rollbackRenderer.PixelsSuppressedByReplacement)
            {
                rollbackRenderer.SetReplacementPixelsSuppressed(false);
                repaired = true;
            }
            else if (rollbackRenderer == null && !hideLegacy &&
                     patch35RollbackRoot != null && !patch35RollbackRoot.activeSelf)
            {
                patch35RollbackRoot.SetActive(true);
                repaired = true;
            }
            return repaired;
        }

#if UNITY_EDITOR
        public bool TryBeginEditorPresentationOverride(
            UnityEngine.Object owner, bool review, out string error)
        {
            if (owner == null)
            {
                error = "Editor presentation owner is missing.";
                return false;
            }
            if (editorPresentationOwner != null && editorPresentationOwner != owner)
            {
                error = "Patch 4 presentation is already owned by " +
                    editorPresentationOwner.name + ".";
                return false;
            }
            if (!TryGetTechnicalReadinessError(out error)) return false;
            editorPresentationOwner = owner;
            editorPresentationIsReview = review;
            editorPresentationPixelsVisible = true;
            editorLegacyPixelsWereSuppressed = rollbackRenderer != null &&
                rollbackRenderer.PixelsSuppressedByReplacement;
            ApplyPresentationState();
            if (!HasExclusiveVisiblePresentation)
            {
                error = "The Editor handoff did not produce exactly one visible " +
                    "character presentation.";
                bool restoreSuppression = editorLegacyPixelsWereSuppressed;
                editorPresentationOwner = null;
                editorPresentationIsReview = false;
                editorPresentationPixelsVisible = true;
                ApplyPresentationState();
                if (rollbackRenderer != null && restoreSuppression)
                {
                    rollbackRenderer.SetReplacementPixelsSuppressed(true);
                }
                editorLegacyPixelsWereSuppressed = false;
                return false;
            }
            error = string.Empty;
            return true;
        }

        public bool SetEditorPresentationPixelsVisible(
            UnityEngine.Object owner, bool visible)
        {
            if (owner == null || editorPresentationOwner != owner) return false;
            editorPresentationPixelsVisible = visible;
            ApplyPresentationState();
            return visible ? HasExclusiveVisiblePresentation : VisiblePresentationCount == 0;
        }

        public bool EndEditorPresentationOverride(UnityEngine.Object owner)
        {
            if (owner == null || editorPresentationOwner != owner) return false;
            editorPresentationOwner = null;
            editorPresentationIsReview = false;
            editorPresentationPixelsVisible = true;
            patch4Enabled = false;
            ApplyPresentationState();
            if (rollbackRenderer != null && editorLegacyPixelsWereSuppressed)
            {
                rollbackRenderer.SetReplacementPixelsSuppressed(true);
            }
            bool restored = rollbackRenderer == null ||
                rollbackRenderer.PixelsSuppressedByReplacement ==
                    editorLegacyPixelsWereSuppressed;
            editorLegacyPixelsWereSuppressed = false;
            return restored && presentationState == PresentationState.LegacyRollback;
        }
#endif

        public bool TryGetRuntimeReadinessError(out string error)
        {
            if (!TryGetTechnicalReadinessError(out error)) return false;
            if (!IsArtApproved)
            {
                error = "production art is not approved for the expected master " +
                    "SHA-256. Draft mask layers cannot approve this gate.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        public bool TryGetTechnicalReadinessError(out string error)
        {
            if (patch4VisualRoot == null)
            {
                error = "Patch4VisualRoot is not assigned.";
                return false;
            }
            if (bones.Count == 0 && !RebuildBoneMap())
            {
                error = "the required Patch 4 rig hierarchy is incomplete.";
                return false;
            }
            if (!rigValid)
            {
                error = "the required Patch 4 rig hierarchy is incomplete.";
                return false;
            }

            Patch4CharacterStateMachine stateMachine = GetComponent<Patch4CharacterStateMachine>();
            if (stateMachine == null || !stateMachine.IsConfigured)
            {
                error = stateMachine != null ? stateMachine.AnimatorReadinessError
                    : "Patch4CharacterStateMachine is missing.";
                return false;
            }
            Patch4CanvasPresentation canvas = GetComponent<Patch4CanvasPresentation>();
            if (canvas == null || !canvas.IsCanvasReady ||
                canvas.ImageCount != Patch4RigContract.RequiredLayerPaths.Count)
            {
                error = "the Canvas skin/presentation is not configured for the " +
                    "LivingGameplayScene room.";
                return false;
            }
            Patch4V23FullFramePresentation fullFrame =
                GetComponent<Patch4V23FullFramePresentation>();
            if (fullFrame == null || !fullFrame.IsReady)
            {
                error = "the continuous full-frame presentation is not ready.";
                return false;
            }
            RawImage[] referenceSurfaces =
                patch4VisualRoot.GetComponentsInChildren<RawImage>(true);
            if (referenceSurfaces.Length != 1 || referenceSurfaces[0].enabled)
            {
                error = "the V23 full-frame QA surface is not uniquely disabled.";
                return false;
            }
            SpriteRenderer[] fallbackRenderers =
                patch4VisualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < fallbackRenderers.Length; i++)
            {
                if (fallbackRenderers[i] != null && fallbackRenderers[i].enabled)
                {
                    error = "a fallback SpriteRenderer can compete with the " +
                        "authoritative Canvas presentation.";
                    return false;
                }
            }
            Patch4V21HybridPuppetController hybrid =
                GetComponent<Patch4V21HybridPuppetController>();
            if (hybrid == null || !hybrid.IsPresentationReady)
            {
                error = "the V21 continuous hybrid body/limb presentation was " +
                    "not prepared while hidden.";
                return false;
            }
            Patch4V21FaceSwapBridge face = GetComponent<Patch4V21FaceSwapBridge>();
            if (face == null || !face.IsPresentationReady)
            {
                error = "the Patch 4 face replacement presentation was not bound " +
                    "while hidden.";
                return false;
            }
            if (!HasRollbackBinding)
            {
                error = "the Patch 3.5 renderer-owned rollback binding is missing.";
                return false;
            }

            if (!rollbackRenderer.IsReady)
            {
                error = "the authoritative legacy painted renderer is not ready.";
                return false;
            }
            CharacterLayeredRigController rollbackLayered =
                rollbackRenderer.GetComponent<CharacterLayeredRigController>();
            if (rollbackLayered == null || !rollbackLayered.IsReady)
            {
                error = "the final bounded legacy rollback presentation is not ready.";
                return false;
            }

            CharacterRigController rollbackRig = rollbackRenderer.GetComponent<CharacterRigController>();
            CharacterSkinController rollbackSkin = rollbackRenderer.GetComponent<CharacterSkinController>();
            if (rollbackRig == null)
            {
                error = "the authoritative legacy CharacterRigController is " +
                    "missing from the rollback owner.";
                return false;
            }
            if (!rollbackRig.AnimatorReady)
            {
                error = rollbackRig.AnimatorReadinessError;
                return false;
            }
            if (rollbackSkin == null || !rollbackSkin.IsConfigured)
            {
                error = "Character skin controller is not configured.";
                return false;
            }
            if (!rollbackRig.HasVisibleSkin || !rollbackSkin.IsVisualReady)
            {
                error = rollbackSkin.VisualReadinessError;
                return false;
            }
            Patch4LegacySignalBridge bridge = GetComponent<Patch4LegacySignalBridge>();
            if (bridge == null || !bridge.IsBound)
            {
                error = "the authoritative legacy gameplay/skin signal bridge " +
                    "is not configured.";
                return false;
            }
            if (!bridge.enabled && !stateMachine.IsLockedReviewActive)
            {
                error = "the authoritative legacy gameplay signal bridge is disabled.";
                return false;
            }
            Patch4CharacterVisibilityGuard visibilityGuard =
                GetComponent<Patch4CharacterVisibilityGuard>();
            if (visibilityGuard == null || !visibilityGuard.enabled)
            {
                error = "Patch4CharacterVisibilityGuard is missing or disabled.";
                return false;
            }
            if (GetComponent<Patch4V21FootPlantController>() != null)
            {
                error = "the obsolete V21 foot solver is still attached and would " +
                    "compete with Animator-owned leg channels.";
                return false;
            }
            error = string.Empty;
            return true;
        }

        private void CacheHierarchy(Transform node)
        {
            if (!bones.ContainsKey(node.name)) bones.Add(node.name, node);
            else duplicateBones.Add(node.name);
            for (int i = 0; i < node.childCount; i++) CacheHierarchy(node.GetChild(i));
        }

        private static bool HasFiniteNonZeroTransform(Transform target)
        {
            if (target == null) return false;
            Vector3 p = target.localPosition;
            Vector3 s = target.localScale;
            Quaternion r = target.localRotation;
            return IsFinite(p.x) && IsFinite(p.y) && IsFinite(p.z) &&
                IsFinite(s.x) && IsFinite(s.y) && IsFinite(s.z) &&
                Mathf.Abs(s.x) > 0.0001f && Mathf.Abs(s.y) > 0.0001f &&
                Mathf.Abs(s.z) > 0.0001f && Mathf.Abs(s.x) <= 4f &&
                Mathf.Abs(s.y) <= 4f && Mathf.Abs(s.z) <= 4f &&
                IsFinite(r.x) && IsFinite(r.y) && IsFinite(r.z) && IsFinite(r.w);
        }

        private static bool HasVisiblePixels(GameObject presentationRoot)
        {
            if (presentationRoot == null || !presentationRoot.activeInHierarchy) return false;
            Transform stopAt = presentationRoot.transform;
            SpriteRenderer[] sprites = presentationRoot.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < sprites.Length; i++)
            {
                SpriteRenderer renderer = sprites[i];
                if (renderer != null && renderer.enabled &&
                    renderer.gameObject.activeInHierarchy && renderer.sprite != null &&
                    renderer.color.a > 0.001f &&
                    HasVisibleCanvasGroups(renderer.transform, stopAt)) return true;
            }
            Graphic[] graphics = presentationRoot.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic != null && graphic.enabled &&
                    graphic.gameObject.activeInHierarchy && graphic.color.a > 0.001f &&
                    HasVisibleCanvasGroups(graphic.transform, stopAt)) return true;
            }
            return false;
        }

        private static bool HasVisibleCanvasGroups(Transform target, Transform stopAt)
        {
            Transform current = target;
            while (current != null)
            {
                CanvasGroup group = current.GetComponent<CanvasGroup>();
                if (group != null && group.alpha <= 0.001f) return false;
                if (current == stopAt) break;
                current = current.parent;
            }
            return true;
        }

        private static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);
        private static string Describe(IReadOnlyList<string> values) =>
            values != null && values.Count > 0 ? string.Join(", ", values) : "<none>";
        private void LogFailure(string message)
        {
            if (logValidationErrors) Debug.LogWarning(message, this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                RebuildBoneMap();
                ApplyPresentationState();
            }
        }
#endif
    }
}
