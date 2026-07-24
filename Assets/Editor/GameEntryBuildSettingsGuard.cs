#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Editor
{
    [InitializeOnLoad]
    internal static class GameEntryBuildSettingsGuard
    {
        private const string ScenePath = "Assets/Scenes/GameEntry.unity";

        static GameEntryBuildSettingsGuard()
        {
            EditorApplication.delayCall -= EnsureSceneIsIncluded;
            EditorApplication.delayCall += EnsureSceneIsIncluded;
        }

        private static void EnsureSceneIsIncluded()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall -= EnsureSceneIsIncluded;
                EditorApplication.delayCall += EnsureSceneIsIncluded;
                return;
            }

            if (!File.Exists(ScenePath))
            {
                return;
            }

            List<EditorBuildSettingsScene> scenes =
                new List<EditorBuildSettingsScene>(
                    EditorBuildSettings.scenes);
            bool changed = false;
            bool found = false;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path != ScenePath)
                {
                    continue;
                }

                found = true;
                if (!scenes[i].enabled)
                {
                    scenes[i] = new EditorBuildSettingsScene(
                        ScenePath,
                        true);
                    changed = true;
                }

                break;
            }

            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                changed = true;
            }

            if (changed)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "GameEntry scene added to Build Settings for the " +
                    "START loading flow.");
            }
        }
    }
}
#endif
