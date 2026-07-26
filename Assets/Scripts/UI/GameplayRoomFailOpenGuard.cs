using System.Collections;
using System.Reflection;
using UnityEngine;

namespace SkinnyToBeast.UI
{
    /// <summary>
    /// The room background must never remain black because a character asset is
    /// missing or still importing. Character readiness is diagnostic; room
    /// readiness is authoritative. This guard gives the normal reveal one
    /// second, then releases the gameplay Canvas and entry transition.
    /// </summary>
    [DefaultExecutionOrder(40000)]
    internal sealed class GameplayRoomFailOpenGuard : MonoBehaviour
    {
        private const string HostName = "GameplayRoom.FailOpenGuard";
        private const float GraceSeconds = 1f;

        private static readonly FieldInfo InitializedField =
            typeof(GameplayWindowController).GetField(
                "initialized",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo CharacterReadyField =
            typeof(GameplayWindowController).GetField(
                "characterReady",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo WindowGroupField =
            typeof(GameplayWindowController).GetField(
                "windowGroup",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static GameplayRoomFailOpenGuard instance;
        private GameplayWindowController observed;
        private float observedAt;
        private bool forced;

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

            GameObject host = new GameObject(HostName);
            DontDestroyOnLoad(host);
            instance = host.AddComponent<GameplayRoomFailOpenGuard>();
        }

        private IEnumerator Start()
        {
            while (true)
            {
                CheckRoom();
                yield return null;
            }
        }

        private void CheckRoom()
        {
            GameplayWindowController controller =
                Object.FindFirstObjectByType<GameplayWindowController>();
            if (controller == null)
            {
                observed = null;
                forced = false;
                return;
            }

            if (controller != observed)
            {
                observed = controller;
                observedAt = Time.unscaledTime;
                forced = false;
            }

            bool initialized = InitializedField != null &&
                               (bool)InitializedField.GetValue(controller);
            if (!initialized || forced ||
                Time.unscaledTime - observedAt < GraceSeconds)
            {
                return;
            }

            CanvasGroup group = WindowGroupField != null
                ? WindowGroupField.GetValue(controller) as CanvasGroup
                : controller.GetComponent<CanvasGroup>();
            if (group == null)
            {
                return;
            }

            bool characterReady = CharacterReadyField != null &&
                                  (bool)CharacterReadyField.GetValue(controller);
            if (characterReady && group.alpha >= 0.99f)
            {
                return;
            }

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            CharacterReadyField?.SetValue(controller, true);
            forced = true;

            Debug.LogWarning(
                "Gameplay room was revealed independently of the character " +
                "visibility gate. The character can be repaired without " +
                "blocking entry or leaving a black screen.",
                controller);
        }
    }
}
