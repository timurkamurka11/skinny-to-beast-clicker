using System.Collections;
using System.Reflection;
using UnityEngine;

namespace SkinnyToBeast.UI
{
    /// <summary>
    /// The room background and controls are allowed to appear even if the
    /// character asset is still being imported or rejected by validation.
    /// Character diagnostics remain in the Console, but they can no longer
    /// leave the player behind a permanent black CanvasGroup.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    internal sealed class GameplayRoomRevealFailsafe : MonoBehaviour
    {
        private const float RevealDelay = 0.65f;
        private static GameplayRoomRevealFailsafe instance;

        private static readonly FieldInfo WindowGroupField =
            typeof(GameplayWindowController).GetField(
                "windowGroup",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo InitializedField =
            typeof(GameplayWindowController).GetField(
                "initialized",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo CharacterReadyField =
            typeof(GameplayWindowController).GetField(
                "characterReady",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo VisualStageField =
            typeof(GameplayWindowController).GetField(
                "visualStageController",
                BindingFlags.Instance | BindingFlags.NonPublic);

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (instance != null)
            {
                return;
            }

            GameObject host = new GameObject("GameplayRoomRevealFailsafe");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<GameplayRoomRevealFailsafe>();
        }

        private IEnumerator Start()
        {
            while (true)
            {
                GameplayWindowController[] windows =
                    Resources.FindObjectsOfTypeAll<GameplayWindowController>();
                for (int i = 0; i < windows.Length; i++)
                {
                    GameplayWindowController window = windows[i];
                    if (window == null ||
                        !window.gameObject.scene.IsValid() ||
                        !window.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    yield return RevealAfterDelay(window);
                }
                yield return null;
            }
        }

        private static IEnumerator RevealAfterDelay(
            GameplayWindowController window)
        {
            float deadline = Time.unscaledTime + RevealDelay;
            while (window != null && Time.unscaledTime < deadline)
            {
                yield return null;
            }

            if (window == null)
            {
                yield break;
            }

            bool initialized = InitializedField != null &&
                               (bool)InitializedField.GetValue(window);
            object visualStage = VisualStageField != null
                ? VisualStageField.GetValue(window)
                : null;
            if (!initialized || visualStage == null)
            {
                yield break;
            }

            CanvasGroup group = WindowGroupField != null
                ? WindowGroupField.GetValue(window) as CanvasGroup
                : null;
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            CharacterReadyField?.SetValue(window, true);
        }
    }
}
