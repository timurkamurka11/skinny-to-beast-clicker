using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Activates the production rig only when the authored prefab exists and
    /// passes its contract. Until that asset is supplied, this installer makes
    /// no visual substitutions and logs one explicit requirement instead of
    /// pretending that runtime-cropped PNG pieces are a finished rig.
    /// </summary>
    [DefaultExecutionOrder(25000)]
    internal sealed class ProductionFatManRigAutoInstaller : MonoBehaviour
    {
        private const string HostName = "ProductionFatMan.AutoInstaller";
        private static ProductionFatManRigAutoInstaller instance;
        private bool missingLogged;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureInstance();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureCurrentScene()
        {
            EnsureInstance();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInstance();
        }

        private static ProductionFatManRigAutoInstaller EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            ProductionFatManRigAutoInstaller existing =
                Object.FindFirstObjectByType<ProductionFatManRigAutoInstaller>();
            if (existing != null)
            {
                instance = existing;
                return existing;
            }

            GameObject host = new GameObject(HostName);
            Object.DontDestroyOnLoad(host);
            instance = host.AddComponent<ProductionFatManRigAutoInstaller>();
            return instance;
        }

        private IEnumerator Start()
        {
            while (true)
            {
                InstallIntoAvailableRigs();
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }

        private void InstallIntoAvailableRigs()
        {
            GameObject productionPrefab = Resources.Load<GameObject>(
                ProductionFatManRigContract.ResourcePath);
            if (productionPrefab == null)
            {
                if (!missingLogged)
                {
                    missingLogged = true;
                    Debug.LogWarning(
                        "Production rig 4.0 is waiting for a real authored " +
                        "FatManRig.prefab. Supply a layered PSB/PSD or a ready " +
                        "Unity 2D Animation prefab; runtime PNG cutting is no " +
                        "longer treated as a production solution.");
                }
                return;
            }

            CharacterRigController[] rigs =
                Resources.FindObjectsOfTypeAll<CharacterRigController>();
            for (int i = 0; i < rigs.Length; i++)
            {
                CharacterRigController rig = rigs[i];
                if (rig == null ||
                    !rig.gameObject.scene.IsValid() ||
                    !rig.isActiveAndEnabled ||
                    rig.VisualRoot == null)
                {
                    continue;
                }

                ProductionFatManRenderHost host =
                    rig.VisualRoot.GetComponent<ProductionFatManRenderHost>();
                if (host != null)
                {
                    continue;
                }

                host = rig.VisualRoot.gameObject.AddComponent<
                    ProductionFatManRenderHost>();
                CharacterSkinController skin =
                    rig.GetComponent<CharacterSkinController>();
                bool entry =
                    rig.gameObject.name.Contains("Entry") ||
                    rig.transform.root.name.Contains("GameEntry");
                host.Configure(rig.VisualRoot, rig, skin, entry);
            }
        }
    }
}
