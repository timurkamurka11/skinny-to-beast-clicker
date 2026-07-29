using System.Collections.Generic;
using SkinnyToBeast.Gameplay;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Finds the dynamically-created character in LivingGameplayScene and
    /// installs the isolated Patch 4 prefab beside it. The approved Patch 3.5
    /// visual remains active because every installation starts in rollback
    /// mode and the production-art gate is never changed here.
    /// </summary>
    [DefaultExecutionOrder(8200)]
    [DisallowMultipleComponent]
    public sealed class Patch4RuntimeInstaller : MonoBehaviour
    {
        public const string InstanceName = "FatMan_Patch4_Instance";
        public const string PrefabResourcePath = "FatMan_Patch4";

        private const string HostName = "Patch4RuntimeInstaller";
        private const string GameplayRoomName = "LivingGameplayScene";
        private const float ScanInterval = 0.20f;

        private static readonly HashSet<int> FailedRigIds = new();

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
                    legacyRig.transform.Find(InstanceName) != null ||
                    FailedRigIds.Contains(legacyRig.GetInstanceID()))
                {
                    continue;
                }

                if (TryInstallBeside(legacyRig) != null)
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

            Patch4CharacterRigController patchRig =
                patchInstance.GetComponent<Patch4CharacterRigController>();
            Patch4CharacterVisibilityGuard visibility =
                patchInstance.GetComponent<Patch4CharacterVisibilityGuard>();
            Patch4LegacySignalBridge bridge =
                patchInstance.GetComponent<Patch4LegacySignalBridge>();
            Patch4CanvasPresentation canvasPresentation =
                patchInstance.GetComponent<Patch4CanvasPresentation>();

            if (patchRig == null ||
                visibility == null ||
                bridge == null ||
                canvasPresentation == null)
            {
                FailedRigIds.Add(legacyRig.GetInstanceID());
                Debug.LogError(
                    "Patch 4 runtime installation failed: the generated " +
                    "prefab is missing its rig, Canvas presentation, " +
                    "visibility guard or legacy signal bridge.",
                    patchInstance);
                Object.Destroy(patchInstance);
                return null;
            }

            RectTransform legacyCharacterRoot =
                legacyRig.transform as RectTransform;
            if (!canvasPresentation.ConfigureForGameplayRoom(
                    legacyCharacterRoot))
            {
                FailedRigIds.Add(legacyRig.GetInstanceID());
                Debug.LogError(
                    "Patch 4 runtime installation failed: the painted layer " +
                    "presentation could not bind to the LivingGameplayScene " +
                    "Canvas. Patch 3.5 remains active.",
                    patchInstance);
                Object.Destroy(patchInstance);
                return null;
            }

            GameObject rollbackRoot = legacyRig.VisualRoot.gameObject;
            CharacterSkinController legacySkin =
                legacyRig.GetComponent<CharacterSkinController>();

            patchRig.BindRollbackRoot(rollbackRoot);
            visibility.BindRollbackRoot(rollbackRoot);
            bridge.BindLegacy(legacyRig, legacySkin);
            patchRig.SetPatch4Enabled(false);

            Debug.Log(
                "Patch 4 installed automatically beside the LivingGameplayScene " +
                "character with a Canvas-ready painted layer presentation in " +
                "locked rollback mode. Patch 3.5 remains visible.",
                patchInstance);
            return patchInstance;
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
