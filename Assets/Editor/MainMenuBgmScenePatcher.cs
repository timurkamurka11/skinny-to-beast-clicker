#if UNITY_EDITOR
using SkinnyToBeast.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkinnyToBeast.EditorTools
{
    public static class MainMenuBgmScenePatcher
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string AudioPath = "Assets/Resources/Audio/MainMenuBGM.ogg";
        private const string MusicSettingKey = "settings.music";

        [MenuItem("Tools/Skinny To Beast/Attach And Enable Main Menu BGM")]
        public static void AttachAndEnableMainMenuBgm()
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
            if (clip == null)
            {
                Debug.LogError(
                    $"BGM file was not found at {AudioPath}. " +
                    "Copy the OGG file there and let Unity finish importing it."
                );
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = Object.FindFirstObjectByType<Camera>();
            }

            if (mainCamera != null && mainCamera.GetComponent<AudioListener>() == null)
            {
                mainCamera.gameObject.AddComponent<AudioListener>();
                EditorUtility.SetDirty(mainCamera.gameObject);
            }

            GameObject musicObject = GameObject.Find("MainMenuBGM");
            if (musicObject == null)
            {
                musicObject = new GameObject("MainMenuBGM");
            }

            AudioSource source = musicObject.GetComponent<AudioSource>();
            if (source == null)
            {
                source = musicObject.AddComponent<AudioSource>();
            }

            MainMenuBgmBootstrap player = musicObject.GetComponent<MainMenuBgmBootstrap>();
            if (player == null)
            {
                player = musicObject.AddComponent<MainMenuBgmBootstrap>();
            }

            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = 0.7f;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.priority = 0;

            SerializedObject serializedPlayer = new SerializedObject(player);
            serializedPlayer.FindProperty("clipOverride").objectReferenceValue = clip;
            serializedPlayer.FindProperty("audioSource").objectReferenceValue = source;
            serializedPlayer.FindProperty("volume").floatValue = 0.7f;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

            PlayerPrefs.SetInt(MusicSettingKey, 1);
            PlayerPrefs.Save();

            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(player);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = musicObject;
            Debug.Log(
                $"Main menu BGM attached and enabled: {clip.name}. " +
                "AudioListener is present. Press Play in MainMenu to test it."
            );
        }
    }
}
#endif
