using System;
using UnityEngine;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4
{
    /// <summary>
    /// Replaces only the visible one-piece body's broad LBS pass with the
    /// volume-stable deformer after Patch4CanvasPresentation builds/rebuilds its
    /// generated UI hierarchy. Contract components stay present and bound for
    /// rollback/readiness validation; only their visible mesh pass is disabled.
    /// </summary>
    [DefaultExecutionOrder(1230)]
    [DisallowMultipleComponent]
    public sealed class Patch4StableBodySkinController : MonoBehaviour
    {
        private const string TargetLayerName = "Layer.Body.TorsoBase";

        [SerializeField] private Patch4CharacterRigController rigController;

        private Image patchedImage;
        private Patch4StableBodyCanvasDeformer stableDeformer;

        private void Reset()
        {
            rigController = GetComponent<Patch4CharacterRigController>();
        }

        private void Awake()
        {
            ResolveRig();
            TryInstall();
        }

        private void OnEnable()
        {
            ResolveRig();
            TryInstall();
        }

        private void Update()
        {
            if (patchedImage == null ||
                stableDeformer == null ||
                !stableDeformer.enabled ||
                !patchedImage.gameObject.activeInHierarchy)
            {
                TryInstall();
            }
        }

        private void ResolveRig()
        {
            if (rigController == null)
            {
                rigController = GetComponent<Patch4CharacterRigController>();
            }
        }

        private void TryInstall()
        {
            ResolveRig();
            if (rigController == null)
            {
                return;
            }

            Image[] images = GetComponentsInChildren<Image>(true);
            Image target = null;
            for (int i = 0; i < images.Length; i++)
            {
                Image candidate = images[i];
                if (candidate != null &&
                    string.Equals(
                        candidate.gameObject.name,
                        TargetLayerName,
                        StringComparison.Ordinal))
                {
                    target = candidate;
                    break;
                }
            }

            if (target == null)
            {
                patchedImage = null;
                stableDeformer = null;
                return;
            }

            Patch4CanvasSkinDeformer legacyContinuous =
                target.GetComponent<Patch4CanvasSkinDeformer>();
            if (legacyContinuous != null && legacyContinuous.enabled)
            {
                legacyContinuous.enabled = false;
            }

            Patch4StableBodyCanvasDeformer stable =
                target.GetComponent<Patch4StableBodyCanvasDeformer>();
            if (stable == null)
            {
                stable = target.gameObject.AddComponent<
                    Patch4StableBodyCanvasDeformer>();
            }

            stable.enabled = true;
            stable.Configure(rigController);
            target.SetVerticesDirty();

            patchedImage = target;
            stableDeformer = stable;
        }
    }
}
