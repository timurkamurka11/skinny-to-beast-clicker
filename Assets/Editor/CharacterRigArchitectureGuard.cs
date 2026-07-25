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

        [MenuItem("Tools/Skinny to Beast/Validate Patch 3 Architecture")]
        private static void ValidateFromMenu()
        {
            ValidateOrThrow();
            Debug.Log(
                "Patch 3 architecture guard passed: no frame-based " +
                "character path and every required clip has real curves.");
        }

        private static void ValidateOrThrow()
        {
            ValidateForbiddenRuntimeTokens();
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
