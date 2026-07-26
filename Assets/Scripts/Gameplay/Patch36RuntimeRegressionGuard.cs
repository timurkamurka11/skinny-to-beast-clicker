using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay
{
    /// <summary>
    /// Runtime safety net for Patch 3.6.
    ///
    /// The generated layered character uses nested override-sorting canvases.
    /// Their original 500-range orders were lower than the gameplay window's
    /// 15000 canvas, so the real painted layers rendered behind the room while
    /// the legacy CharacterPartSurface children remained visible. This guard
    /// raises the layered canvases above their parent canvas and hides only the
    /// legacy CanvasRenderer output without disabling the old animation bones.
    ///
    /// It also repairs an old zero-volume preference state that can leave SFX
    /// toggled ON while every UI/gameplay sound is effectively silent.
    /// </summary>
    [DefaultExecutionOrder(30000)]
    internal sealed class Patch36RuntimeRegressionGuard : MonoBehaviour
    {
        private const string HostName = "Patch36.RuntimeRegressionGuard";
        private const string LayeredRootName = "RealFatMan.LayeredArt3_6";
        private const string PersistentListenerName =
            "SkinnyToBeast.PersistentAudioListener";
        private const string SfxEnabledKey = "settings.sfx";
        private const string SfxVolumeKey = "settings.sfx.volume";
        private const string MusicEnabledKey = "settings.music";
        private const string MusicVolumeKey = "settings.music.volume";

        private static Patch36RuntimeRegressionGuard instance;
        private AudioListener recoveryListener;
        private bool preferencesRepaired;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EnsureInstance();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureAfterSceneLoad()
        {
            EnsureInstance();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInstance();
        }

        private static Patch36RuntimeRegressionGuard EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            Patch36RuntimeRegressionGuard existing =
                UnityEngine.Object.FindFirstObjectByType<
                    Patch36RuntimeRegressionGuard>();
            if (existing != null)
            {
                instance = existing;
                return existing;
            }

            GameObject host = new GameObject(HostName);
            UnityEngine.Object.DontDestroyOnLoad(host);
            instance = host.AddComponent<Patch36RuntimeRegressionGuard>();
            return instance;
        }

        private IEnumerator Start()
        {
            // Repair every frame while runtime-created character/UI objects are
            // being assembled. Afterwards LateUpdate keeps the render contract.
            for (int frame = 0; frame < 600; frame++)
            {
                RepairAudio();
                RepairCharacters();
                yield return null;
            }
        }

        private void LateUpdate()
        {
            RepairCharacters();
            RepairAudio();
        }

        private void RepairCharacters()
        {
            CharacterLayeredRigController[] layeredControllers =
                Resources.FindObjectsOfTypeAll<
                    CharacterLayeredRigController>();

            for (int i = 0; i < layeredControllers.Length; i++)
            {
                CharacterLayeredRigController layered =
                    layeredControllers[i];
                if (layered == null ||
                    !layered.gameObject.scene.IsValid() ||
                    !layered.isActiveAndEnabled)
                {
                    continue;
                }

                CharacterRigController rig =
                    layered.GetComponent<CharacterRigController>();
                RectTransform visualRoot = rig != null
                    ? rig.VisualRoot
                    : null;
                if (visualRoot == null)
                {
                    continue;
                }

                Transform layeredRoot = visualRoot.Find(LayeredRootName);
                if (layeredRoot == null)
                {
                    continue;
                }

                layeredRoot.SetAsLastSibling();
                RaiseLayeredCanvases(layered, layeredRoot);
                HideLegacyRenderSurfaces(visualRoot, layeredRoot);
            }
        }

        private static void RaiseLayeredCanvases(
            Component layered,
            Transform layeredRoot)
        {
            Canvas parentCanvas = layered.GetComponentInParent<Canvas>();
            int parentOrder = parentCanvas != null
                ? parentCanvas.sortingOrder
                : 0;
            int minimumOrder = parentOrder + 500;

            Canvas[] canvases =
                layeredRoot.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null)
                {
                    continue;
                }

                // The generated value is 500 + part.sort. Preserve the part
                // offset, but move the complete character above GameplayWindow.
                int relativeOrder = canvas.sortingOrder;
                if (relativeOrder >= parentOrder + 300)
                {
                    continue;
                }

                relativeOrder = Mathf.Clamp(relativeOrder, 500, 999);
                canvas.overrideSorting = true;
                if (parentCanvas != null)
                {
                    canvas.sortingLayerID = parentCanvas.sortingLayerID;
                }
                canvas.sortingOrder = parentOrder + relativeOrder;
                canvas.enabled = true;
            }
        }

        private static void HideLegacyRenderSurfaces(
            RectTransform visualRoot,
            Transform layeredRoot)
        {
            CharacterMeshGraphic[] meshes =
                visualRoot.GetComponentsInChildren<CharacterMeshGraphic>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                CharacterMeshGraphic mesh = meshes[i];
                if (mesh == null || mesh.transform.IsChildOf(layeredRoot))
                {
                    continue;
                }

                // Do not disable the component: readiness and the legacy
                // Animator still use it as a signal. Hide only its renderer.
                CanvasRenderer meshRenderer = mesh.canvasRenderer;
                if (meshRenderer != null)
                {
                    meshRenderer.SetAlpha(0f);
                }

                Graphic[] childGraphics =
                    mesh.GetComponentsInChildren<Graphic>(true);
                for (int graphicIndex = 0;
                     graphicIndex < childGraphics.Length;
                     graphicIndex++)
                {
                    Graphic graphic = childGraphics[graphicIndex];
                    if (graphic == null ||
                        graphic.transform.IsChildOf(layeredRoot))
                    {
                        continue;
                    }

                    // CharacterPartSurface readiness checks Image.enabled, so
                    // retain the component and color while making the actual
                    // CanvasRenderer transparent.
                    if (graphic.canvasRenderer != null)
                    {
                        graphic.canvasRenderer.SetAlpha(0f);
                    }
                }
            }

            Image[] images = visualRoot.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image.transform.IsChildOf(layeredRoot))
                {
                    continue;
                }

                string objectName = image.gameObject.name;
                bool obsolete =
                    objectName.StartsWith(
                        "Sprite.RealFatMan",
                        StringComparison.Ordinal) ||
                    objectName.StartsWith(
                        "LayeredFace.",
                        StringComparison.Ordinal) ||
                    objectName.StartsWith(
                        "SpriteFace.",
                        StringComparison.Ordinal) ||
                    objectName == "VisibleFill" ||
                    objectName == "VisibleOutline";
                if (obsolete && image.canvasRenderer != null)
                {
                    image.canvasRenderer.SetAlpha(0f);
                }
            }
        }

        private void RepairAudio()
        {
            bool preferencesChanged = false;
            if (!preferencesRepaired)
            {
                if (PlayerPrefs.GetInt(SfxEnabledKey, 1) != 0 &&
                    PlayerPrefs.GetFloat(SfxVolumeKey, 1f) <= 0.01f)
                {
                    PlayerPrefs.SetFloat(SfxVolumeKey, 1f);
                    preferencesChanged = true;
                }

                if (PlayerPrefs.GetInt(MusicEnabledKey, 1) != 0 &&
                    PlayerPrefs.GetFloat(MusicVolumeKey, 0.12f) <= 0.001f)
                {
                    PlayerPrefs.SetFloat(MusicVolumeKey, 0.12f);
                    preferencesChanged = true;
                }

                if (preferencesChanged)
                {
                    PlayerPrefs.Save();
                }
                preferencesRepaired = true;
            }

            AudioListener.pause = false;
            AudioListener.volume = 1f;
            EnsureListener();

            bool sfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) != 0;
            if (!sfxEnabled)
            {
                return;
            }

            AudioSource[] sources =
                Resources.FindObjectsOfTypeAll<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null ||
                    !source.gameObject.scene.IsValid() &&
                    source.gameObject.name != "UISoundPlayer")
                {
                    continue;
                }

                bool patchAudio =
                    source.GetComponent<GameplayAudioController>() != null ||
                    source.gameObject.name == "UISoundPlayer";
                if (!patchAudio)
                {
                    continue;
                }

                source.enabled = true;
                source.mute = false;
                source.spatialBlend = 0f;
                if (source.volume <= 0.001f)
                {
                    source.volume = source.loop ? 0.12f : 1f;
                }
            }
        }

        private void EnsureListener()
        {
            AudioListener[] listeners =
                Resources.FindObjectsOfTypeAll<AudioListener>();
            AudioListener preferred = null;
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener candidate = listeners[i];
                if (candidate == null)
                {
                    continue;
                }

                if (candidate.gameObject.name == PersistentListenerName)
                {
                    preferred = candidate;
                    break;
                }

                if (preferred == null && candidate.enabled)
                {
                    preferred = candidate;
                }
            }

            if (preferred == null)
            {
                if (recoveryListener == null)
                {
                    GameObject host = new GameObject(PersistentListenerName);
                    DontDestroyOnLoad(host);
                    recoveryListener = host.AddComponent<AudioListener>();
                }
                preferred = recoveryListener;
            }

            preferred.enabled = true;
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener candidate = listeners[i];
                if (candidate != null && candidate != preferred)
                {
                    candidate.enabled = false;
                }
            }
        }
    }
}
