using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SkinnyToBeast.UI
{
    [DefaultExecutionOrder(1100)]
    internal sealed class ReferenceSettingsPanelSpriteFix : MonoBehaviour
    {
        private const string MainMenuSceneName = "MainMenu";
        private static Sprite cachedSprite;

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
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            TryApply();
        }

        private void TryApply()
        {
            GameObject panel = GameObject.Find("ReferencePanel");
            if (panel == null)
            {
                Debug.LogWarning("ReferenceSettingsPanelSpriteFix: ReferencePanel not found.");
                return;
            }

            Image image = panel.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogWarning("ReferenceSettingsPanelSpriteFix: Image component missing on ReferencePanel.");
                return;
            }

            Sprite sprite = LoadSprite();
            if (sprite == null)
            {
                Debug.LogError("ReferenceSettingsPanelSpriteFix: Could not load settings reference sprite from Resources/UI/Settings.");
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.type = Image.Type.Simple;
            Debug.Log("ReferenceSettingsPanelSpriteFix: reference settings panel sprite applied.");
        }

        private static Sprite LoadSprite()
        {
            if (cachedSprite != null) return cachedSprite;

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
                if (texture == null) continue;

                cachedSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                cachedSprite.name = texture.name + "_Sprite";
                return cachedSprite;
            }

            return null;
        }
    }
}
