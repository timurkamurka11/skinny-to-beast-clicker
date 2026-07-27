using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SkinnyToBeast.Gameplay.Patch4;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SkinnyToBeast.Editor.Patch4
{
    public static class Patch4RigContractValidator
    {
        private const string ValidateRigMenu =
            "Tools/GameWork/Patch 4.0/Validate Selected Rig";
        private const string ValidatePathsMenu =
            "Tools/GameWork/Patch 4.0/Verify Protected Paths";

        [MenuItem(ValidateRigMenu)]
        public static void ValidateSelectedRig()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogError(
                    "Patch 4 validation requires a selected character root or prefab.");
                return;
            }

            Patch4CharacterRigController controller =
                selected.GetComponentInChildren<Patch4CharacterRigController>(true);
            Transform hierarchyRoot =
                controller != null && controller.RigRoot != null
                    ? controller.RigRoot
                    : selected.transform;

            HashSet<string> transformNames = new(StringComparer.Ordinal);
            CollectNames(hierarchyRoot, transformNames);

            List<string> missingBones = new();
            foreach (string boneName in Patch4RigContract.RequiredBoneNames)
            {
                if (!transformNames.Contains(boneName))
                {
                    missingBones.Add(boneName);
                }
            }

            Animator animator = selected.GetComponentInChildren<Animator>(true);
            HashSet<string> clipNames = new(StringComparer.Ordinal);
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                foreach (AnimationClip clip in
                         animator.runtimeAnimatorController.animationClips)
                {
                    if (clip != null)
                    {
                        clipNames.Add(clip.name);
                    }
                }
            }

            List<string> missingClips = new();
            foreach (string clipName in Patch4RigContract.RequiredClipNames)
            {
                if (!clipNames.Contains(clipName))
                {
                    missingClips.Add(clipName);
                }
            }

            if (missingBones.Count == 0 && missingClips.Count == 0)
            {
                Debug.Log(
                    "Patch 4 rig contract passed for " + selected.name + ".",
                    selected);
                return;
            }

            if (missingBones.Count > 0)
            {
                Debug.LogError(
                    "Patch 4 missing bones: " + string.Join(", ", missingBones),
                    selected);
            }

            if (missingClips.Count > 0)
            {
                Debug.LogWarning(
                    "Patch 4 animation set is incomplete: " +
                    string.Join(", ", missingClips),
                    selected);
            }
        }

        [MenuItem(ValidateRigMenu, true)]
        private static bool CanValidateSelectedRig()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem(ValidatePathsMenu)]
        public static void VerifyProtectedPaths()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                Debug.LogError("Could not resolve the Unity project root.");
                return;
            }

            try
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = "git",
                    Arguments = "diff --name-only main...HEAD",
                    WorkingDirectory = projectRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = Process.Start(startInfo);
                if (process == null)
                {
                    Debug.LogError("Git process could not be started.");
                    return;
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(5000))
                {
                    process.Kill();
                    Debug.LogError("Protected-path validation timed out.");
                    return;
                }

                if (process.ExitCode != 0)
                {
                    Debug.LogError(
                        "Protected-path validation failed to run git diff: " + error);
                    return;
                }

                List<string> violations = FindProtectedPathViolations(output);
                if (violations.Count == 0)
                {
                    Debug.Log(
                        "Patch 4 protected-path check passed. Menu, video, " +
                        "music and settings paths are unchanged.");
                }
                else
                {
                    Debug.LogError(
                        "Patch 4 touched protected paths and must not be merged:\n" +
                        string.Join("\n", violations));
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Protected-path validation could not run: " + exception.Message);
            }
        }

        private static List<string> FindProtectedPathViolations(string gitOutput)
        {
            List<string> violations = new();
            string[] paths = gitOutput.Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawPath in paths)
            {
                string path = "/" + rawPath.Replace('\\', '/').TrimStart('/');
                foreach (string fragment in Patch4RigContract.ProtectedPathFragments)
                {
                    if (path.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        violations.Add(rawPath);
                        break;
                    }
                }
            }

            return violations;
        }

        private static void CollectNames(
            Transform node,
            ISet<string> destination)
        {
            destination.Add(node.name);
            for (int i = 0; i < node.childCount; i++)
            {
                CollectNames(node.GetChild(i), destination);
            }
        }
    }
}
