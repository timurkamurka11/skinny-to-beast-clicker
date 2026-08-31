using System.Collections.Generic;
using SkinnyToBeast.Gameplay;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Finds the dynamically-created character in LivingGameplayScene and
    /// installs the isolated Patch 4 prefab beside it. Installation begins in
    /// rollback, then deterministically re-evaluates the existing candidate
    /// until Stage 4, art, Animator, presentation, skin and signal bindings are
    /// all ready. The installer never changes the human production-art gate.
    /// </summary>
    [DefaultExecutionOrder(8200)]
    [DisallowMultipleComponent]
    public sealed class Patch4RuntimeInstaller : MonoBehaviour
    {
        public const string InstanceName = "FatMan_Patch4_Instance";
        public const string PrefabResourcePath = "FatMan_Patch4";

        private const string HostName = "Patch4RuntimeInstaller";
        private const string GameplayRoomName = "LivingGameplayScene";
        private const int FinalStageArtIndex = 3;
        private const float ScanInterval = 0.20f;

        private static readonly HashSet<int> FailedRigIds = new();
        private static readonly HashSet<int> ActivatedRigIds = new();

        private static Patch4RuntimeInstaller instance;
        private static GameObject cachedPrefab;
        private static bool missingPrefabLogged;

        private float nextScanAt;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            cachedPrefab = null;
            missingPrefabLogged = false;
            FailedRigIds.Clear();
            ActivatedRigIds.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureHost();
        }

        public static int InstallAvailableGameplayRigs()
        {
            CharacterRigController[] legacyRigs =
                Object.FindObjectsByType<CharacterRigController>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            int installedCount = 0;

            for (int i = 0; i < legacyRigs.Length; i++)
            {
                CharacterRigController legacyRig = legacyRigs[i];
                if (!IsGameplayRoomRig(legacyRig) ||
                    legacyRig.VisualRoot == null ||
                    FailedRigIds.Contains(legacyRig.GetInstanceID()))
                {
                    continue;
                }

                bool candidateAlreadyExisted =
                    legacyRig.transform.Find(InstanceName) != null;
                if (TryInstallBeside(legacyRig) != null &&
                    !candidateAlreadyExisted)
                {
                    installedCount++;
                }
            }

            return installedCount;
        }

        public static GameObject TryInstallBeside(
            CharacterRigController legacyRig)
        {
            if (legacyRig == null ||
                legacyRig.VisualRoot == null ||
                !IsGameplayRoomRig(legacyRig))
            {
                return null;
            }

            Transform existing = legacyRig.transform.Find(InstanceName);
            if (existing != null)
            {
                TryConfigureAndActivate(
                    legacyRig,
                    existing.gameObject,
                    false);
                return existing.gameObject;
            }

            GameObject prefab = LoadPrefab();
            if (prefab == null)
            {
                return null;
            }

            GameObject patchInstance = Object.Instantiate(
                prefab,
                legacyRig.transform,
                false);
            patchInstance.name = InstanceName;
            patchInstance.transform.localPosition = Vector3.zero;
            patchInstance.transform.localRotation = Quaternion.identity;
            patchInstance.transform.localScale = Vector3.one;
            patchInstance.transform.SetAsLastSibling();

            if (!TryConfigureAndActivate(
                    legacyRig,
                    patchInstance,
                    true))
            {
                return null;
            }

            return patchInstance;
        }

        private static bool TryConfigureAndActivate(
            CharacterRigController legacyRig,
            GameObject patchInstance,
            bool newlyInstalled)
        {
            if (legacyRig == null || patchInstance == null)
            {
                return false;
            }

            Patch4CharacterRigController patchRig =
                patchInstance.GetComponent<Patch4CharacterRigController>();
            Patch4CharacterVisibilityGuard visibility =
                patchInstance.GetComponent<Patch4CharacterVisibilityGuard>();
            Patch4LegacySignalBridge bridge =
                patchInstance.GetComponent<Patch4LegacySignalBridge>();
            Patch4CanvasPresentation canvasPresentation =
                patchInstance.GetComponent<Patch4CanvasPresentation>();
            Patch4V23FullFramePresentation fullFramePresentation =
                patchInstance.GetComponent<
                    Patch4V23FullFramePresentation>();
            Patch4CharacterStateMachine stateMachine =
                patchInstance.GetComponent<Patch4CharacterStateMachine>();
            Patch4V21HybridPuppetController hybridPresentation =
                patchInstance.GetComponent<Patch4V21HybridPuppetController>();
            Patch4V21FaceSwapBridge facePresentation =
                patchInstance.GetComponent<Patch4V21FaceSwapBridge>();
            Animator animator = patchInstance.GetComponent<Animator>();

            if (patchRig == null ||
                visibility == null ||
                bridge == null ||
                canvasPresentation == null ||
                fullFramePresentation == null ||
                stateMachine == null ||
                hybridPresentation == null ||
                facePresentation == null ||
                animator == null ||
                animator.runtimeAnimatorController == null)
            {
                FailedRigIds.Add(legacyRig.GetInstanceID());
                Debug.LogError(
                    "Patch 4 runtime installation failed: the generated " +
                    "prefab is missing its root Animator/controller, rig, " +
                    "Canvas/full-frame presentation, visibility guard, " +
                    "state machine or legacy signal bridge.",
                    patchInstance);
                Object.Destroy(patchInstance);
                return false;
            }

            // The editor-only room/animation review deliberately owns the
            // visual override while leaving the production art gate closed.
            // Its driver has already bound the real dependencies and acquired
            // the central presentation owner; a background installer pass
            // must not rewrite that explicitly scoped technical-review state.
            if (stateMachine.IsLockedReviewActive)
            {
                return true;
            }

            GameObject rollbackRoot = legacyRig.VisualRoot.gameObject;
            CharacterSkinController legacySkin =
                legacyRig.GetComponent<CharacterSkinController>();

            // Prepare the candidate completely while its presentation remains
            // hidden. The only visible-state mutation happens later through
            // Patch4CharacterRigController's atomic handoff.
            patchRig.BindRollbackRoot(rollbackRoot);
            visibility.BindRollbackRoot(rollbackRoot);

            if (!canvasPresentation.IsCanvasReady)
            {
                RectTransform legacyCharacterRoot =
                    legacyRig.transform as RectTransform;
                canvasPresentation.ConfigureForGameplayRoom(
                    legacyCharacterRoot);
            }
            fullFramePresentation.RebuildPresentation();

            animator.applyRootMotion = false;
            // A valid generated prefab arrives with an enabled root Animator.
            // Initialize that Animator once if Unity has not done so yet, but
            // never turn a disabled/broken dependency back on here. Rebinding
            // every 0.2-second installer scan would restart live reactions and
            // would also defeat the rollback contract for an Animator failure.
            if (animator.enabled && !animator.isInitialized)
            {
                animator.Rebind();
                animator.Update(0f);
            }
            stateMachine.BindRuntimeDependencies(patchRig, animator);
            hybridPresentation.PrepareHiddenPresentation();
            facePresentation.PrepareHiddenPresentation();
            bridge.BindLegacy(legacyRig, legacySkin);

            bool activated = TryActivateIfReady(
                legacyRig,
                legacySkin,
                patchRig,
                visibility,
                canvasPresentation,
                fullFramePresentation,
                stateMachine,
                bridge,
                animator);

            if (newlyInstalled && !activated)
            {
                string reason = patchRig.RuntimeReadinessError;
                Debug.Log(
                    "Patch 4 installed automatically beside the " +
                    "LivingGameplayScene character and remains in safe " +
                    "rollback until its deterministic runtime readiness " +
                    "contract passes. Patch 3.5 remains visible. " + reason,
                    patchInstance);
            }

            return true;
        }

        private static bool TryActivateIfReady(
            CharacterRigController legacyRig,
            CharacterSkinController legacySkin,
            Patch4CharacterRigController patchRig,
            Patch4CharacterVisibilityGuard visibility,
            Patch4CanvasPresentation canvasPresentation,
            Patch4V23FullFramePresentation fullFramePresentation,
            Patch4CharacterStateMachine stateMachine,
            Patch4LegacySignalBridge bridge,
            Animator animator)
        {
            if (patchRig == null)
            {
                return false;
            }

            if (!patchRig.IsRuntimeReady ||
                legacyRig == null ||
                legacySkin == null ||
                !legacySkin.IsConfigured ||
                legacySkin.TargetArtIndex != FinalStageArtIndex ||
                legacySkin.IsTransitioning ||
                legacySkin.CurrentArtIndex != FinalStageArtIndex ||
                !legacySkin.IsVisualReady ||
                visibility == null ||
                !visibility.HasRollbackBinding ||
                canvasPresentation == null ||
                !canvasPresentation.IsCanvasReady ||
                fullFramePresentation == null ||
                !fullFramePresentation.IsReady ||
                stateMachine == null ||
                !stateMachine.IsConfigured ||
                bridge == null ||
                !bridge.IsBound ||
                !Patch4CharacterStateMachine.ValidateAnimatorContract(
                    animator,
                    out _))
            {
                ActivatedRigIds.Remove(patchRig.GetInstanceID());
                patchRig.SetPatch4Enabled(false);
                return false;
            }

            if (!patchRig.SetPatch4Enabled(true))
            {
                ActivatedRigIds.Remove(patchRig.GetInstanceID());
                return false;
            }

            if (!bridge.SynchronizeCurrentGameplayState())
            {
                ActivatedRigIds.Remove(patchRig.GetInstanceID());
                patchRig.SetPatch4Enabled(false);
                return false;
            }

            if (ActivatedRigIds.Add(patchRig.GetInstanceID()))
            {
                Debug.Log(
                    "Patch 4 runtime readiness passed. Exactly one approved " +
                    "Patch 4 character is active and Patch 3.5 pixels are " +
                    "suppressed at their renderer owner.",
                    patchRig);
            }

            return true;
        }

        private static void EnsureHost()
        {
            if (instance != null)
            {
                return;
            }

            Patch4RuntimeInstaller existing =
                Object.FindFirstObjectByType<Patch4RuntimeInstaller>();
            if (existing != null)
            {
                instance = existing;
                return;
            }

            GameObject host = new(HostName);
            host.hideFlags = HideFlags.HideInHierarchy;
            instance = host.AddComponent<Patch4RuntimeInstaller>();
            Object.DontDestroyOnLoad(host);
        }

        private static GameObject LoadPrefab()
        {
            if (cachedPrefab != null)
            {
                return cachedPrefab;
            }

            cachedPrefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (cachedPrefab == null && !missingPrefabLogged)
            {
                missingPrefabLogged = true;
                Debug.LogError(
                    "Patch 4 runtime prefab is missing from Resources. Run " +
                    "the Patch 4 runtime-asset rebuild before entering the room.");
            }

            return cachedPrefab;
        }

        private static bool IsGameplayRoomRig(
            CharacterRigController legacyRig)
        {
            if (legacyRig == null)
            {
                return false;
            }

            Transform current = legacyRig.transform;
            while (current != null)
            {
                if (string.Equals(
                    current.name,
                    GameplayRoomName,
                    System.StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScanAt)
            {
                return;
            }

            nextScanAt = Time.unscaledTime + ScanInterval;
            InstallAvailableGameplayRigs();
        }
    }
}
