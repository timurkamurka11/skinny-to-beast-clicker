using System;
using System.Collections.Generic;
using System.IO;
using SkinnyToBeast.Gameplay;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SkinnyToBeast.Editor
{
    internal sealed class CharacterRigArchitectureGuard :
        IPreprocessBuildWithReport
    {
        private const string AnimationFolder =
            "Assets/Resources/UI/Gameplay/Living/Animations";
        private const string CharacterPrefabPath =
            "Assets/Resources/UI/Gameplay/Living/CharacterRig2D.prefab";

        private static readonly Dictionary<string, string[]> ForbiddenByFile =
            new()
            {
                {
                    "Assets/Scripts/Gameplay/CharacterRigController.cs",
                    new[]
                    {
                        "Raw" + "Image",
                        ".uv" + "Rect",
                        "RigPart" + "Graphic",
                        "CharacterDirectional" + "Frame",
                        "walk" + "_stage_",
                        "Texture" + "2D",
                        "Sprite",
                        "Image"
                    }
                },
                {
                    "Assets/Scripts/Gameplay/CharacterFaceController.cs",
                    new[]
                    {
                        "Raw" + "Image",
                        "Texture" + "2D",
                        "Sprite",
                        "Image"
                    }
                },
                {
                    "Assets/Scripts/Gameplay/CharacterMeshGraphic.cs",
                    new[]
                    {
                        "Raw" + "Image",
                        "Texture" + "2D",
                        "Sprite",
                        "Image"
                    }
                },
                {
                    "Assets/Scripts/UI/GameEntryFlowController.cs",
                    new[]
                    {
                        "Raw" + "Image",
                        ".uv" + "Rect",
                        "CharacterDirectional" + "Frame",
                        "walk" + "_stage_"
                    }
                },
                {
                    "Assets/Scripts/Gameplay/GameplayVisualStageController.cs",
                    new[]
                    {
                        "character" + "_stage_",
                        "walk" + "_stage_"
                    }
                },
                {
                    "Assets/Scripts/Gameplay/CharacterSkinDefinition.cs",
                    new[]
                    {
                        "CharacterRig" + "Crop",
                        "DirectionalWalk",
                        "Front" + "Sprite",
                        "Texture" + "2D",
                        "Sprite",
                        "Image"
                    }
                }
            };

        private static readonly string[] SharedRigRuntimeFiles =
        {
            "Assets/Scripts/Gameplay/GameplayVisualStageController.cs",
            "Assets/Scripts/UI/GameEntryFlowController.cs"
        };

        private static readonly string[] RequiredMotionClips =
        {
            "Idle_Breathe",
            "Idle_ShiftWeight",
            "Idle_LookAround",
            "Idle_Scratch",
            "Idle_Yawn",
            "Idle_Stretch",
            "Idle_Flex",
            "Idle_AdjustClothes",
            "Idle_WarmShoulders",
            "Walk_Front",
            "Walk_Side",
            "Walk_Back",
            "SitDown",
            "SitLoop",
            "StandUp",
            "TapLift_A",
            "TapLift_B",
            "TapLift_C",
            "StageChange",
            "Entry_WalkToDoor",
            "Face_Blink",
            "Face_Look",
            "Face_Expression"
        };

        private static readonly string[] ForbiddenCharacterRasterAssets =
        {
            "Assets/Resources/UI/Gameplay/Living/character" +
            "_stage_01.png",
            "Assets/Resources/UI/Gameplay/Living/character" +
            "_stage_02.png",
            "Assets/Resources/UI/Gameplay/Living/character" +
            "_stage_03.png",
            "Assets/Resources/UI/Gameplay/Living/character" +
            "_stage_04.png",
            "Assets/Resources/UI/Gameplay/Living/Rig/walk" +
            "_stage_01.png",
            "Assets/Resources/UI/Gameplay/Living/Rig/walk" +
            "_stage_02.png",
            "Assets/Resources/UI/Gameplay/Living/Rig/walk" +
            "_stage_03.png",
            "Assets/Resources/UI/Gameplay/Living/Rig/walk" +
            "_stage_04.png"
        };

        private static readonly string[] ForbiddenLegacyRuntimeFiles =
        {
            "Assets/Scripts/Gameplay/RigPart" + "Graphic.cs",
            "Assets/Scripts/Gameplay/GameplayAnimation" + "Controller.cs",
            "Assets/Scripts/Gameplay/RandomIdle" + "Scheduler.cs"
        };

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            ValidateOrThrow();
        }

        [MenuItem("Tools/Skinny to Beast/Validate Patch 3.1 Fat Man Skin")]
        private static void ValidateFromMenu()
        {
            ValidateOrThrow();
            Debug.Log(
                "Patch 3.1 guard passed: the shared fat-man cutout rig, " +
                "soft-body bones and every required clip are valid.");
        }

        private static void ValidateOrThrow()
        {
            ValidateForbiddenRuntimeTokens();
            ValidateCanvasRendererContract();
            ValidateFatManSkinContract();
            ValidateCharacterPrefabAndAssets();
            EnsureAnimatorAssets();
            ValidateMotionClips();
        }

        private static void ValidateForbiddenRuntimeTokens()
        {
            foreach (KeyValuePair<string, string[]> entry
                     in ForbiddenByFile)
            {
                if (!File.Exists(entry.Key))
                {
                    throw new BuildFailedException(
                        $"Required Patch 3 source is missing: {entry.Key}");
                }

                string source = File.ReadAllText(entry.Key);
                for (int i = 0; i < entry.Value.Length; i++)
                {
                    if (source.Contains(
                            entry.Value[i],
                            StringComparison.Ordinal))
                    {
                        throw new BuildFailedException(
                            $"Forbidden character animation token " +
                            $"'{entry.Value[i]}' returned in {entry.Key}.");
                    }
                }
            }

            const string sharedRigResource =
                "UI/Gameplay/Living/CharacterRig2D";
            for (int i = 0; i < SharedRigRuntimeFiles.Length; i++)
            {
                string path = SharedRigRuntimeFiles[i];
                if (!File.Exists(path) ||
                    !File.ReadAllText(path).Contains(
                        sharedRigResource,
                        StringComparison.Ordinal))
                {
                    throw new BuildFailedException(
                        $"Entry and room must both load the shared rig: {path}");
                }
            }
        }

        private static void ValidateCanvasRendererContract()
        {
            const string meshPath =
                "Assets/Scripts/Gameplay/CharacterMeshGraphic.cs";
            const string surfacePath =
                "Assets/Scripts/Gameplay/CharacterPartSurface.cs";
            const string rigPath =
                "Assets/Scripts/Gameplay/CharacterRigController.cs";
            const string facePath =
                "Assets/Scripts/Gameplay/CharacterFaceController.cs";
            const string stableGraphicPath =
                "Assets/Scripts/Gameplay/CharacterSurfaceGraphic.cs";

            string meshSource = File.ReadAllText(meshPath);
            string surfaceSource = File.ReadAllText(surfacePath);
            string rigSource = File.ReadAllText(rigPath);
            string faceSource = File.ReadAllText(facePath);
            string stableGraphicSource =
                File.ReadAllText(stableGraphicPath);

            if (!meshSource.Contains(
                    "[RequireComponent(typeof(CanvasRenderer))]",
                    StringComparison.Ordinal) ||
                !surfaceSource.Contains(
                    "[RequireComponent(typeof(CanvasRenderer))]",
                    StringComparison.Ordinal) ||
                !stableGraphicSource.Contains(
                    "[RequireComponent(typeof(CanvasRenderer))]",
                    StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    "Every procedural skeletal graphic must require its own " +
                    "CanvasRenderer.");
            }

            int rigRenderer = rigSource.IndexOf(
                "GetOrAdd<CanvasRenderer>(rect.gameObject)",
                StringComparison.Ordinal);
            int rigGraphic = rigSource.IndexOf(
                "AddComponent<CharacterMeshGraphic>()",
                StringComparison.Ordinal);
            if (rigRenderer < 0 ||
                rigGraphic < 0 ||
                rigRenderer > rigGraphic)
            {
                throw new BuildFailedException(
                    "Body parts must receive CanvasRenderer before " +
                    "CharacterMeshGraphic is enabled.");
            }

            int faceRenderer = faceSource.IndexOf(
                "AddComponent<CanvasRenderer>()",
                StringComparison.Ordinal);
            int faceGraphic = faceSource.IndexOf(
                "AddComponent<CharacterMeshGraphic>()",
                StringComparison.Ordinal);
            if (faceRenderer < 0 ||
                faceGraphic < 0 ||
                faceRenderer > faceGraphic)
            {
                throw new BuildFailedException(
                    "Face parts must receive CanvasRenderer before " +
                    "CharacterMeshGraphic is enabled.");
            }

            if (!surfaceSource.Contains(
                    "GetOrCreateSurface",
                    StringComparison.Ordinal) ||
                !surfaceSource.Contains(
                    "CharacterSurfaceGraphic",
                    StringComparison.Ordinal) ||
                !surfaceSource.Contains(
                    "target.AddComponent<CanvasRenderer>()",
                    StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    "The stable character surface cannot repair a partial " +
                    "uGUI render hierarchy.");
            }
        }

        private static void ValidateFatManSkinContract()
        {
            string[] requiredFiles =
            {
                "Assets/Scripts/Gameplay/FatManSkinSet.cs",
                "Assets/Scripts/Gameplay/CharacterArtPart.cs",
                "Assets/Scripts/Gameplay/CharacterSoftBodyController.cs",
                "Assets/Scripts/Gameplay/CharacterShapeGeometry.cs",
                "Assets/Scripts/Gameplay/CharacterSurfaceGraphic.cs"
            };
            for (int i = 0; i < requiredFiles.Length; i++)
            {
                if (!File.Exists(requiredFiles[i]))
                {
                    throw new BuildFailedException(
                        $"Patch 3.1 source is missing: {requiredFiles[i]}");
                }
            }

            string rigSource = File.ReadAllText(
                "Assets/Scripts/Gameplay/CharacterRigController.cs");
            string skinSource = File.ReadAllText(
                "Assets/Scripts/Gameplay/CharacterSkinDefinition.cs");
            string appearanceSource = File.ReadAllText(
                "Assets/Scripts/Gameplay/CharacterSkeletonDefinition.cs");

            string[] requiredRigTokens =
            {
                "CharacterMeshShape.FatBelly",
                "CharacterMeshShape.FatChest",
                "CharacterMeshShape.FatHead",
                "CharacterMeshShape.MessyHair",
                "\"Bone.Belly\"",
                "\"Bone.ShirtHem\"",
                "\"Bone.ChestSoft\"",
                "\"Bone.ChinSoft\"",
                "ValidateFatManArtCoverage"
            };
            for (int i = 0; i < requiredRigTokens.Length; i++)
            {
                if (!rigSource.Contains(
                        requiredRigTokens[i],
                        StringComparison.Ordinal))
                {
                    throw new BuildFailedException(
                        "The runtime rig is missing required fat-man token: " +
                        requiredRigTokens[i]);
                }
            }

            if (!skinSource.Contains(
                    "FatManSkinSet.Create",
                    StringComparison.Ordinal) ||
                !skinSource.Contains(
                    "fat_man_body_",
                    StringComparison.Ordinal) ||
                !appearanceSource.Contains(
                    "public float softness",
                    StringComparison.Ordinal) ||
                !appearanceSource.Contains(
                    "public float bellyDrop",
                    StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    "Fat-man stages, slots or soft-body appearance data are " +
                    "not connected atomically.");
            }
        }

        private static void ValidateCharacterPrefabAndAssets()
        {
            for (int i = 0;
                 i < ForbiddenLegacyRuntimeFiles.Length;
                 i++)
            {
                if (File.Exists(ForbiddenLegacyRuntimeFiles[i]))
                {
                    throw new BuildFailedException(
                        "Legacy character runtime returned: " +
                        ForbiddenLegacyRuntimeFiles[i]);
                }
            }

            for (int i = 0;
                 i < ForbiddenCharacterRasterAssets.Length;
                 i++)
            {
                if (File.Exists(ForbiddenCharacterRasterAssets[i]))
                {
                    throw new BuildFailedException(
                        "Frame/crop character raster returned: " +
                        ForbiddenCharacterRasterAssets[i]);
                }
            }

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    CharacterPrefabPath);
            if (prefab == null)
            {
                throw new BuildFailedException(
                    "CharacterRig2D.prefab is missing.");
            }

            if (prefab.GetComponent<CharacterFaceController>() == null ||
                prefab.GetComponent<CharacterRigController>() == null ||
                prefab.GetComponent<CharacterSkinController>() == null ||
                prefab.GetComponent<CharacterRoutineController>() == null ||
                prefab.GetComponent<CharacterRigValidator>() == null)
            {
                throw new BuildFailedException(
                    "CharacterRig2D.prefab is missing a required persistent " +
                    "rig controller.");
            }

            Component[] components =
                prefab.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component != null &&
                    component.GetType().Name == "Raw" + "Image")
                {
                    throw new BuildFailedException(
                        "CharacterRig2D.prefab contains a forbidden raw " +
                        "texture UI component.");
                }
            }
        }

        private static void EnsureAnimatorAssets()
        {
            LivingGameplayAnimatorAssetBuilder.EnsureCurrentAssets();
        }

        private static void ValidateMotionClips()
        {
            for (int i = 0; i < RequiredMotionClips.Length; i++)
            {
                string clipName = RequiredMotionClips[i];
                AnimationClip clip =
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        $"{AnimationFolder}/{clipName}.anim");
                if (clip == null)
                {
                    throw new BuildFailedException(
                        $"Required motion clip is missing: {clipName}");
                }

                EditorCurveBinding[] bindings =
                    AnimationUtility.GetCurveBindings(clip);
                if (bindings.Length == 0)
                {
                    throw new BuildFailedException(
                        $"Motion clip has no curves: {clipName}");
                }

                bool hasRealTarget = false;
                bool markerOnly = true;
                for (int bindingIndex = 0;
                     bindingIndex < bindings.Length;
                     bindingIndex++)
                {
                    EditorCurveBinding binding =
                        bindings[bindingIndex];
                    hasRealTarget |=
                        binding.path.Contains(
                            "Bone.",
                            StringComparison.Ordinal) ||
                        binding.path.Contains(
                            "FaceRig",
                            StringComparison.Ordinal);
                    markerOnly &=
                        binding.propertyName == "localPosition.z";
                }

                if (!hasRealTarget || markerOnly)
                {
                    throw new BuildFailedException(
                        $"Motion clip '{clipName}' is a marker instead of " +
                        "a bone/face animation.");
                }
            }
        }
    }
}
