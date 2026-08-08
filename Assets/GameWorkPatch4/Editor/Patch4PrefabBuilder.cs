using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    public static class Patch4PrefabBuilder
    {
        public const string PrefabRoot = "Assets/GameWorkPatch4/Resources";
        public const string PrefabPath = PrefabRoot + "/FatMan_Patch4.prefab";
        public const string PrefabResourcePath = "FatMan_Patch4";
        public const string V23SheetRoot =
            "Assets/GameWorkPatch4/Art/Character/FatMan/V23FullFrame/";
        public const string V23IdleSheetPath =
            V23SheetRoot + "FatMan_Idle_V23.png";
        public const string V23FaceSheetPath =
            V23SheetRoot + "FatMan_Face_V23.png";
        public const string V23TapSheetPath =
            V23SheetRoot + "FatMan_Tap_V23.png";
        public const string V23PoseSheetPath =
            V23SheetRoot + "FatMan_Pose_V23.png";
        public const string V23UpgradeSheetPath =
            V23SheetRoot + "FatMan_Upgrade_V23.png";
        public const string V23WalkRightSheetPath =
            V23SheetRoot + "FatMan_WalkRight_V23.png";

        private sealed class BoneSpec
        {
            public string name;
            public string parent;
            public Vector3 world;

            public BoneSpec(string name, string parent, float x, float y)
            {
                this.name = name;
                this.parent = parent;
                world = new Vector3(x, y, 0f);
            }
        }

        [MenuItem("Tools/GameWork/Patch 4.0/Build/Rebuild Character Prefab")]
        public static void RebuildPrefab()
        {
            Patch4AnimationLibraryBuilder.RebuildLibrary();
            Patch4LayerCatalogBuilder.RebuildCatalog();
            EnsureFolder(PrefabRoot);

            GameObject root = new("FatMan_Patch4");
            try
            {
                Animator animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController =
                    AssetDatabase.LoadAssetAtPath<AnimatorController>(
                        Patch4AnimationLibraryBuilder.ControllerPath);
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                Patch4CharacterRigController rigController =
                    root.AddComponent<Patch4CharacterRigController>();
                Patch4CharacterStateMachine stateMachine =
                    root.AddComponent<Patch4CharacterStateMachine>();
                Patch4FaceController faceController =
                    root.AddComponent<Patch4FaceController>();
                Patch4SecondaryMotionController secondaryMotion =
                    root.AddComponent<Patch4SecondaryMotionController>();
                Patch4LayerRenderer layerRenderer =
                    root.AddComponent<Patch4LayerRenderer>();
                Patch4CanvasPresentation canvasPresentation =
                    root.AddComponent<Patch4CanvasPresentation>();
                Patch4V23FullFramePresentation v23Presentation =
                    root.AddComponent<Patch4V23FullFramePresentation>();
                Patch4LegacySignalBridge signalBridge =
                    root.AddComponent<Patch4LegacySignalBridge>();
                Patch4CharacterVisibilityGuard visibilityGuard =
                    root.AddComponent<Patch4CharacterVisibilityGuard>();

                Transform visualRoot = CreateChild(root.transform, "Patch4VisualRoot");
                Dictionary<string, Transform> bones = BuildSkeleton(visualRoot);
                Transform rigRoot = bones["Root"];

                ConfigureRig(rigController, rigRoot, visualRoot.gameObject);
                ConfigureStateMachine(stateMachine, rigController, animator);
                ConfigureLayerRenderer(layerRenderer, rigController, visualRoot);
                ConfigureFace(faceController, rigController);
                ConfigureCanvasPresentation(
                    canvasPresentation,
                    rigController,
                    faceController,
                    visualRoot);
                ConfigureV23Presentation(
                    v23Presentation,
                    rigController,
                    canvasPresentation,
                    animator);
                ConfigureSecondaryMotion(secondaryMotion, rigController, bones);
                ConfigureSignalBridge(signalBridge, stateMachine, faceController);
                ConfigureVisibility(visibilityGuard, rigController, visualRoot.gameObject);

                layerRenderer.RebuildLayers();
                canvasPresentation.RebuildCanvasLayers();
                if (!v23Presentation.RebuildPresentation())
                {
                    throw new InvalidOperationException(
                        "The V23 ten-state full-frame presentation could not " +
                        "be built from " + V23SheetRoot + ".");
                }

                visualRoot.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Selection.activeObject = prefab;
                Debug.Log(
                    "Patch 4 prefab rebuilt. It remains disabled until all " +
                    "required painted layers are present and validation passes.",
                    prefab);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Dictionary<string, Transform> BuildSkeleton(Transform visualRoot)
        {
            List<BoneSpec> specs = new()
            {
                new BoneSpec("Root", null, 0f, 0f),
                new BoneSpec("CharacterRoot", "Root", 0f, 0f),
                new BoneSpec("Pelvis", "CharacterRoot", 0f, 7.06f),
                new BoneSpec("SpineLower", "Pelvis", 0f, 8.61f),
                new BoneSpec("BellyBase", "SpineLower", 0f, 9.26f),
                new BoneSpec("BellyTip", "BellyBase", 0f, 7.86f),
                new BoneSpec("SpineUpper", "SpineLower", 0f, 10.26f),
                new BoneSpec("ChestSoftL", "SpineUpper", -0.72f, 10.01f),
                new BoneSpec("ChestSoftR", "SpineUpper", 0.72f, 10.01f),
                new BoneSpec("Neck", "SpineUpper", 0f, 11.31f),
                new BoneSpec("Head", "Neck", 0f, 12.51f),
                new BoneSpec("Jaw", "Head", 0f, 11.86f),
                new BoneSpec("BrowL", "Head", -0.40f, 12.69f),
                new BoneSpec("BrowR", "Head", 0.40f, 12.69f),
                new BoneSpec("EyeL", "Head", -0.40f, 12.44f),
                new BoneSpec("EyeR", "Head", 0.40f, 12.44f),
                new BoneSpec("ClavicleL", "SpineUpper", -0.82f, 10.81f),
                new BoneSpec("UpperArmL", "ClavicleL", -1.57f, 10.56f),
                new BoneSpec("ForearmL", "UpperArmL", -2.12f, 8.86f),
                new BoneSpec("HandL", "ForearmL", -2.42f, 7.26f),
                new BoneSpec("ClavicleR", "SpineUpper", 0.82f, 10.81f),
                new BoneSpec("UpperArmR", "ClavicleR", 1.57f, 10.56f),
                new BoneSpec("ForearmR", "UpperArmR", 2.12f, 8.86f),
                new BoneSpec("HandR", "ForearmR", 2.42f, 7.26f),
                new BoneSpec("ThighL", "Pelvis", -0.72f, 6.66f),
                new BoneSpec("ShinL", "ThighL", -0.82f, 4.96f),
                new BoneSpec("FootL", "ShinL", -0.97f, 3.46f),
                new BoneSpec("ThighR", "Pelvis", 0.72f, 6.66f),
                new BoneSpec("ShinR", "ThighR", 0.82f, 4.96f),
                new BoneSpec("FootR", "ShinR", 0.97f, 3.46f),
                new BoneSpec("GroundShadow", "Root", 0f, 0.12f)
            };

            Dictionary<string, BoneSpec> specsByName = new();
            Dictionary<string, Transform> bones = new();
            for (int i = 0; i < specs.Count; i++)
            {
                specsByName.Add(specs[i].name, specs[i]);
            }

            for (int i = 0; i < specs.Count; i++)
            {
                BoneSpec spec = specs[i];
                Transform parent = spec.parent == null
                    ? visualRoot
                    : bones[spec.parent];
                Transform bone = CreateChild(parent, spec.name);
                Vector3 parentWorld = spec.parent == null
                    ? Vector3.zero
                    : specsByName[spec.parent].world;
                bone.localPosition = spec.world - parentWorld;
                bones.Add(spec.name, bone);
            }

            return bones;
        }

        private static void ConfigureRig(
            Patch4CharacterRigController target,
            Transform rigRoot,
            GameObject visualRoot)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty("rigRoot").objectReferenceValue = rigRoot;
            serialized.FindProperty("patch4VisualRoot").objectReferenceValue = visualRoot;
            serialized.FindProperty("patch35RollbackRoot").objectReferenceValue = null;
            serialized.FindProperty("patch4Enabled").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureStateMachine(
            Patch4CharacterStateMachine target,
            Patch4CharacterRigController rig,
            Animator animator)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty("rigController").objectReferenceValue = rig;
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLayerRenderer(
            Patch4LayerRenderer target,
            Patch4CharacterRigController rig,
            Transform visualRoot)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty("rigController").objectReferenceValue = rig;
            serialized.FindProperty("catalog").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Patch4LayerCatalog>(
                    Patch4LayerCatalogBuilder.CatalogPath);
            serialized.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            serialized.FindProperty("buildOnAwake").boolValue = true;
            serialized.FindProperty("autoEnableWhenComplete").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFace(
            Patch4FaceController target,
            Patch4CharacterRigController rig)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty("rigController").objectReferenceValue = rig;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCanvasPresentation(
            Patch4CanvasPresentation target,
            Patch4CharacterRigController rig,
            Patch4FaceController face,
            Transform visualRoot)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty("rigController").objectReferenceValue = rig;
            serialized.FindProperty("catalog").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Patch4LayerCatalog>(
                    Patch4LayerCatalogBuilder.CatalogPath);
            serialized.FindProperty("faceController").objectReferenceValue = face;
            serialized.FindProperty("visualRoot").objectReferenceValue = visualRoot;
            serialized.FindProperty("buildOnAwake").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureV23Presentation(
            Patch4V23FullFramePresentation target,
            Patch4CharacterRigController rig,
            Patch4CanvasPresentation presentation,
            Animator animator)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty("rigController").objectReferenceValue = rig;
            serialized.FindProperty("canvasPresentation").objectReferenceValue =
                presentation;
            serialized.FindProperty("animator").objectReferenceValue = animator;
            BindV23Texture(
                serialized,
                "idleSheet",
                V23IdleSheetPath);
            BindV23Texture(
                serialized,
                "faceSheet",
                V23FaceSheetPath);
            BindV23Texture(
                serialized,
                "tapSheet",
                V23TapSheetPath);
            BindV23Texture(
                serialized,
                "poseSheet",
                V23PoseSheetPath);
            BindV23Texture(
                serialized,
                "upgradeSheet",
                V23UpgradeSheetPath);
            BindV23Texture(
                serialized,
                "walkRightSheet",
                V23WalkRightSheetPath);
            serialized.FindProperty("canvasHeightRatio").floatValue = 0.8f;
            serialized.FindProperty("canvasBottomOffsetRatio").floatValue =
                0.156f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindV23Texture(
            SerializedObject serialized,
            string propertyName,
            string assetPath)
        {
            Texture2D texture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                throw new InvalidOperationException(
                    "A required V23 full-frame sheet is missing at " +
                    assetPath + ".");
            }

            serialized.FindProperty(propertyName).objectReferenceValue =
                texture;
        }

        private static void ConfigureSecondaryMotion(
            Patch4SecondaryMotionController target,
            Patch4CharacterRigController rig,
            IReadOnlyDictionary<string, Transform> bones)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty("rigController").objectReferenceValue = rig;
            SerializedProperty channels = serialized.FindProperty("channels");
            channels.arraySize = 5;
            SetChannel(channels.GetArrayElementAtIndex(0), "BellyTip", bones["BellyTip"], new Vector3(0f, 0.035f, 0f), new Vector3(0f, 0f, 1.4f), 0.85f, 0f, 0.28f);
            SetChannel(channels.GetArrayElementAtIndex(1), "ChestSoftL", bones["ChestSoftL"], new Vector3(0f, 0.018f, 0f), new Vector3(0f, 0f, -0.8f), 1.05f, -0.12f, 0.42f);
            SetChannel(channels.GetArrayElementAtIndex(2), "ChestSoftR", bones["ChestSoftR"], new Vector3(0f, 0.018f, 0f), new Vector3(0f, 0f, 0.8f), 1.05f, 0.12f, 0.42f);
            SetChannel(channels.GetArrayElementAtIndex(3), "Jaw", bones["Jaw"], new Vector3(0f, 0.012f, 0f), new Vector3(0f, 0f, 0.5f), 1.18f, 0.2f, 0.52f);
            SetChannel(channels.GetArrayElementAtIndex(4), "SpineUpper", bones["SpineUpper"], new Vector3(0f, 0.012f, 0f), new Vector3(0f, 0f, 0.35f), 0.72f, -0.25f, 0.3f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetChannel(
            SerializedProperty channel,
            string name,
            Transform target,
            Vector3 position,
            Vector3 rotation,
            float frequency,
            float phase,
            float response)
        {
            channel.FindPropertyRelative("name").stringValue = name;
            channel.FindPropertyRelative("target").objectReferenceValue = target;
            channel.FindPropertyRelative("positionAmplitude").vector3Value = position;
            channel.FindPropertyRelative("rotationAmplitude").vector3Value = rotation;
            channel.FindPropertyRelative("frequencyMultiplier").floatValue = frequency;
            channel.FindPropertyRelative("phaseOffset").floatValue = phase;
            channel.FindPropertyRelative("response").floatValue = response;
        }

        private static void ConfigureSignalBridge(
            Patch4LegacySignalBridge target,
            Patch4CharacterStateMachine stateMachine,
            Patch4FaceController face)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty("stateMachine").objectReferenceValue = stateMachine;
            serialized.FindProperty("faceController").objectReferenceValue = face;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureVisibility(
            Patch4CharacterVisibilityGuard target,
            Patch4CharacterRigController rig,
            GameObject visualRoot)
        {
            SerializedObject serialized = new(target);
            serialized.FindProperty("rigController").objectReferenceValue = rig;
            serialized.FindProperty("patch4VisualRoot").objectReferenceValue = visualRoot;
            serialized.FindProperty("patch35RollbackRoot").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child.transform;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
