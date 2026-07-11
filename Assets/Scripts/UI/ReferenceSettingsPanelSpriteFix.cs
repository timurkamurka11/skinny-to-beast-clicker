using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DefaultExecutionOrder(1200)]
    internal sealed class ReferenceSettingsPanelSpriteFix : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private const string BackgroundObjectName = "ReferencePanelBackground";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInitial()
        {
            EnsureForScene(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureForScene(scene);
        }

        private static void EnsureForScene(Scene scene)
        {
            if (scene.name != MainMenuSceneName) return;
            if (Object.FindFirstObjectByType<ReferenceSettingsPanelSpriteFix>() != null) return;
            new GameObject("ReferenceSettingsPanelSpriteFix").AddComponent<ReferenceSettingsPanelSpriteFix>();
        }

        private IEnumerator Start()
        {
            float timeout = 6f;
            while (timeout > 0f)
            {
                if (TryApply())
                {
                    yield break;
                }

                timeout -= 0.1f;
                yield return new WaitForSecondsRealtime(0.1f);
            }

            Debug.LogError("ReferenceSettingsPanelSpriteFix: failed to apply settings reference texture.");
        }

        private static bool TryApply()
        {
            GameObject panel = GameObject.Find("ReferencePanel");
            if (panel == null) return false;

            Texture texture = LoadReferenceTexture();
            if (texture == null) return false;

            Image blocker = panel.GetComponent<Image>();
            if (blocker == null)
            {
                blocker = panel.AddComponent<Image>();
            }

            // Keep the panel blocking clicks, but never let its fallback white image cover the reference.
            blocker.sprite = null;
            blocker.color = new Color(1f, 1f, 1f, 0.001f);
            blocker.raycastTarget = true;

            Transform existing = panel.transform.Find(BackgroundObjectName);
            RawImage rawImage;
            if (existing == null)
            {
                GameObject background = new GameObject(BackgroundObjectName, typeof(RectTransform), typeof(RawImage));
                background.transform.SetParent(panel.transform, false);
                background.transform.SetAsFirstSibling();
                rawImage = background.GetComponent<RawImage>();

                RectTransform rect = background.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rawImage = existing.GetComponent<RawImage>();
                if (rawImage == null)
                {
                    rawImage = existing.gameObject.AddComponent<RawImage>();
                }

                existing.SetAsFirstSibling();
            }

            rawImage.texture = texture;
            rawImage.color = Color.white;
            rawImage.raycastTarget = false;
            rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);

            Debug.Log($"ReferenceSettingsPanelSpriteFix: settings reference texture applied ({texture.width}x{texture.height}).");
            return true;
        }

        private static Texture LoadReferenceTexture()
        {
            string[] candidates =
            {
                "UI/Settings/settings_ref",
                "UI/Settings/settings_ref_q85",
                "UI/Settings/settings_ref_q75",
                "UI/Settings/settings_ref_q65",
                "UI/Settings/settings_panel_base"
            };

            foreach (string path in candidates)
            {
                Texture2D texture = Resources.Load<Texture2D>(path);
                if (texture != null) return texture;

                Sprite sprite = Resources.Load<Sprite>(path);
                if (sprite != null && sprite.texture != null) return sprite.texture;
            }

            return null;
        }
    }
}
