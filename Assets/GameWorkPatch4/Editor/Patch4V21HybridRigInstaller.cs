using System;
using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    public static class Patch4V21HybridRigInstaller
    {
        public static void Apply()
        {
            const string prefabPath =
                "Assets/GameWorkPatch4/Resources/FatMan_Patch4.prefab";

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (root == null)
                {
                    throw new InvalidOperationException(
                        "Patch 4 v21 could not load the runtime prefab.");
                }

                RemoveIfPresent<Patch4StableBodySkinController>(root);
                RemoveIfPresent<Patch4CutoutPuppetController>(root);
                RemoveIfPresent<Patch4V21FootPlantController>(root);

                Patch4CharacterRigController rig =
                    root.GetComponent<Patch4CharacterRigController>();
                Patch4V21HybridPuppetController controller =
                    root.GetComponent<Patch4V21HybridPuppetController>();
                if (controller == null)
                {
                    controller = root.AddComponent<Patch4V21HybridPuppetController>();
                }

                Sprite torso = LoadSprite(Patch4V21HybridArtworkBuilder.TorsoPath);
                Sprite armL = LoadSprite(Patch4V21HybridArtworkBuilder.ArmLPath);
                Sprite armR = LoadSprite(Patch4V21HybridArtworkBuilder.ArmRPath);
                Sprite legL = LoadSprite(Patch4V21HybridArtworkBuilder.LegLPath);
                Sprite legR = LoadSprite(Patch4V21HybridArtworkBuilder.LegRPath);

                SerializedObject serialized = new(controller);
                serialized.FindProperty("rigController").objectReferenceValue = rig;
                serialized.FindProperty("canvasPresentation").objectReferenceValue =
                    root.GetComponent<Patch4CanvasPresentation>();
                serialized.FindProperty("torsoSprite").objectReferenceValue = torso;
                serialized.FindProperty("armLSprite").objectReferenceValue = armL;
                serialized.FindProperty("armRSprite").objectReferenceValue = armR;
                serialized.FindProperty("legLSprite").objectReferenceValue = legL;
                serialized.FindProperty("legRSprite").objectReferenceValue = legR;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Patch4V21FaceSwapBridge faceBridge =
                    root.GetComponent<Patch4V21FaceSwapBridge>();
                if (faceBridge == null)
                {
                    faceBridge = root.AddComponent<Patch4V21FaceSwapBridge>();
                }
                SerializedObject faceSerialized = new(faceBridge);
                faceSerialized.FindProperty("faceController").objectReferenceValue =
                    root.GetComponent<Patch4FaceController>();
                faceSerialized.FindProperty("canvasPresentation").objectReferenceValue =
                    root.GetComponent<Patch4CanvasPresentation>();
                faceSerialized.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(faceBridge);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "Patch 4 v21 hybrid rig installed: v20 rigid puppet removed; " +
                    "continuous whole-arm/whole-leg artwork uses localized joint " +
                    "deformation, the Animator exclusively owns the eight-phase " +
                    "walk leg channels, " +
                    "and neutral/expression facial sprites are explicitly rebound.");
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    "Patch 4 v21 hybrid sprite was not imported: " + path);
            }
            return sprite;
        }

        private static void RemoveIfPresent<T>(GameObject root)
            where T : Component
        {
            T component = root.GetComponent<T>();
            if (component != null)
            {
                UnityEngine.Object.DestroyImmediate(component, true);
            }
        }
    }
}
