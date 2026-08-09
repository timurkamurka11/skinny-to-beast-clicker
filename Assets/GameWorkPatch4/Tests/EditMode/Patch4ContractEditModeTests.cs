using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Tests.EditMode
{
    public sealed class Patch4ContractEditModeTests
    {
        private const string ExpectedSha =
            "7b151f1ded93f3852bc8a7218ab26f94298b7f822094304bbcea9c076cad72a3";

        [Test]
        public void Contract_CollectionsHaveExpectedCountsAndNoDuplicates()
        {
            Type contract = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Patch4RigContract");

            AssertCollection(contract, "RequiredBoneNames", 31);
            AssertCollection(contract, "RequiredLayerPaths", 40);
            AssertCollection(contract, "RuntimeNeutralLayerPaths", 1);
            AssertCollection(contract, "RuntimeRigidLayerPaths", 9);
            AssertCollection(contract, "RequiredClipNames", 10);
            AssertCollection(contract, "ProtectedPathFragments", 6);
            AssertRepositoryMaster();
        }

        [Test]
        public void Contract_ContainsCriticalRigAndFaceEntries()
        {
            Type contract = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Patch4RigContract");

            IReadOnlyList<string> bones = GetStrings(contract, "RequiredBoneNames");
            IReadOnlyList<string> layers = GetStrings(contract, "RequiredLayerPaths");
            IReadOnlyList<string> clips = GetStrings(contract, "RequiredClipNames");

            CollectionAssert.IsSubsetOf(
                new[]
                {
                    "Root",
                    "CharacterRoot",
                    "Pelvis",
                    "BellyTip",
                    "Head",
                    "Jaw",
                    "EyeL",
                    "EyeR",
                    "GroundShadow"
                },
                bones);

            CollectionAssert.IsSubsetOf(
                new[]
                {
                    "Body/TorsoBase",
                    "Body/BellyFront",
                    "Face/EyeWhiteL",
                    "Face/IrisR",
                    "Face/LidL",
                    "Face/MouthOpen",
                    "Face/MouthSmile",
                    "Clothes/ShirtBellyOverlay",
                    "FX/Shadow"
                },
                layers);

            CollectionAssert.IsSubsetOf(
                new[]
                {
                    "FatMan_Idle_Breathe",
                    "FatMan_TapReact_01",
                    "FatMan_Walk_InRoom",
                    "FatMan_UpgradeReact"
                },
                clips);

            Type neutralPoseValidator = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Editor." +
                "Patch4NeutralPoseValidator");
            PropertyInfo neutralReportPath =
                neutralPoseValidator.GetProperty(
                    "ReportPath",
                    BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(
                neutralReportPath,
                "Neutral-pose QA report path is missing.");

            PropertyInfo facePoseContactSheetPath =
                neutralPoseValidator.GetProperty(
                    "FacePoseContactSheetPath",
                    BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(
                facePoseContactSheetPath,
                "Independent face-pose review path is missing.");

            RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Editor." +
                "Patch4FacePoseReviewWindow");
            RequireType(
                "SkinnyToBeast.Gameplay.Patch4." +
                "Patch4CanvasSkinDeformer");
            RequireType(
                "SkinnyToBeast.Gameplay.Patch4." +
                "Patch4AnimationRoomReviewDriver");
            RequireType(
                "SkinnyToBeast.Gameplay.Patch4." +
                "Patch4V23FullFramePresentation");
            Type animationRoomReview = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Editor." +
                "Patch4AnimationRoomReview");
            Assert.NotNull(
                animationRoomReview.GetMethod(
                    "StartAfterTests",
                    BindingFlags.Static | BindingFlags.Public));
            RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Editor." +
                "Patch4AnimationRoomReviewWindow");

            Type faceController = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Patch4FaceController");
            MethodInfo bindPresentationLayers =
                faceController.GetMethod(
                    "BindPresentationLayers",
                    BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(bindPresentationLayers);
            Assert.AreEqual(
                9,
                bindPresentationLayers.GetParameters().Length,
                "Blink replacement must bind open-eye layers as well as lids.");

            AssertWalkClipHasArticulatedGait();
            AssertV23FullFrameSheetsAreImportable();
            AssertV24UpgradeCorrectionIsImportable();
            AssertWholeFramePlaybackCadence();
            AssertAnimatorControllerHasCanonicalRootStatePaths(clips);
            AssertAnimatorControllerRoutesGameplayActions();
            AssertAutomatedTestRunnerOwnsPlayMode();
            AssertLockedInteractiveGameplayPreview();
        }

        [Test]
        public void ArtReadiness_DefaultAssetRejectsActivation()
        {
            Type readinessType = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Patch4ArtReadinessAsset");
            ScriptableObject readiness = ScriptableObject.CreateInstance(readinessType);

            try
            {
                MethodInfo isApprovedFor = readinessType.GetMethod(
                    "IsApprovedFor",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(isApprovedFor);

                Assert.IsFalse((bool)isApprovedFor.Invoke(
                    readiness,
                    new object[] { ExpectedSha }));
                Assert.IsFalse((bool)isApprovedFor.Invoke(
                    readiness,
                    new object[] { string.Empty }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(readiness);
            }
        }

        [Test]
        public void ArtReadiness_RequiresApprovalAndExactMasterSha()
        {
            Type readinessType = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Patch4ArtReadinessAsset");
            ScriptableObject readiness = ScriptableObject.CreateInstance(readinessType);

            try
            {
                SetPrivateField(readiness, "productionArtApproved", true);
                SetPrivateField(readiness, "approvedSourceSha256", ExpectedSha);
                SetPrivateField(readiness, "approvedBy", "Automated test fixture");

                MethodInfo isApprovedFor = readinessType.GetMethod(
                    "IsApprovedFor",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(isApprovedFor);

                Assert.IsTrue((bool)isApprovedFor.Invoke(
                    readiness,
                    new object[] { ExpectedSha }));
                Assert.IsTrue((bool)isApprovedFor.Invoke(
                    readiness,
                    new object[] { ExpectedSha.ToUpperInvariant() }));
                Assert.IsFalse((bool)isApprovedFor.Invoke(
                    readiness,
                    new object[] { new string('0', 64) }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(readiness);
            }
        }

        private static void AssertCollection(
            Type contract,
            string propertyName,
            int expectedCount)
        {
            IReadOnlyList<string> values = GetStrings(contract, propertyName);
            Assert.AreEqual(expectedCount, values.Count, propertyName);
            Assert.AreEqual(
                values.Count,
                values.Distinct(StringComparer.Ordinal).Count(),
                propertyName + " contains duplicate values.");
            Assert.IsFalse(
                values.Any(string.IsNullOrWhiteSpace),
                propertyName + " contains an empty value.");
        }

        private static void AssertRepositoryMaster()
        {
            string path = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                "Assets",
                "GameWorkPatch4",
                "Art",
                "Character",
                "FatMan",
                "FatMan_NeutralFront_Master.png");
            Assert.IsTrue(
                File.Exists(path),
                "Exact Patch 4 repository master is missing.");

            byte[] bytes = File.ReadAllBytes(path);
            string actualSha;
            using (SHA256 sha256 = SHA256.Create())
            {
                actualSha = BitConverter.ToString(
                        sha256.ComputeHash(bytes))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }

            Assert.AreEqual(
                ExpectedSha,
                actualSha,
                "Repository master bytes do not match the readiness contract.");
        }

        private static void AssertWalkClipHasArticulatedGait()
        {
            const string clipPath =
                "Assets/GameWorkPatch4/Animations/FatMan_Walk_InRoom.anim";
            const string visual =
                "Patch4VisualRoot/Root/CharacterRoot";
            const string pelvis = visual + "/Pelvis";
            const string thighLeft = pelvis + "/ThighL";
            const string thighRight = pelvis + "/ThighR";
            const string shinLeft = thighLeft + "/ShinL";
            const string shinRight = thighRight + "/ShinR";
            const string footLeft = shinLeft + "/FootL";
            const string footRight = shinRight + "/FootR";
            const string spineLower = pelvis + "/SpineLower";
            const string spineUpper = spineLower + "/SpineUpper";
            const string upperArmLeft =
                spineUpper + "/ClavicleL/UpperArmL";
            const string upperArmRight =
                spineUpper + "/ClavicleR/UpperArmR";
            const string forearmLeft = upperArmLeft + "/ForearmL";
            const string forearmRight = upperArmRight + "/ForearmR";
            const string handLeft = forearmLeft + "/HandL";
            const string handRight = forearmRight + "/HandR";

            AnimationClip walk =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            Assert.NotNull(walk, "The generated walk clip is missing.");
            float firstPeak = walk.length * 0.25f;
            float secondPeak = walk.length * 0.75f;

            string[] anchoredBones =
            {
                thighLeft,
                thighRight,
                upperArmLeft,
                upperArmRight
            };
            string[] anchoredProperties =
            {
                "m_LocalPosition.x",
                "m_LocalPosition.y"
            };
            for (int boneIndex = 0;
                 boneIndex < anchoredBones.Length;
                 boneIndex++)
            {
                for (int propertyIndex = 0;
                     propertyIndex < anchoredProperties.Length;
                     propertyIndex++)
                {
                    Assert.IsNull(
                        AnimationUtility.GetEditorCurve(
                            walk,
                            EditorCurveBinding.FloatCurve(
                                anchoredBones[boneIndex],
                                typeof(Transform),
                                anchoredProperties[propertyIndex])),
                        "Walk must keep shoulder and hip anchors fixed; " +
                        anchoredBones[boneIndex] + " animates " +
                        anchoredProperties[propertyIndex] + ".");
                }
            }

            AnimationCurve leftThighRotation = RequireCurve(
                walk,
                thighLeft,
                "localEulerAnglesRaw.z");
            AnimationCurve rightThighRotation = RequireCurve(
                walk,
                thighRight,
                "localEulerAnglesRaw.z");
            Assert.Greater(
                leftThighRotation.Evaluate(firstPeak) *
                rightThighRotation.Evaluate(firstPeak),
                0f,
                "Mirrored legs need matching raw rotation signs at the first " +
                "gait peak so their endpoints move anatomically opposite.");
            Assert.Greater(
                leftThighRotation.Evaluate(secondPeak) *
                rightThighRotation.Evaluate(secondPeak),
                0f,
                "Mirrored legs need matching raw rotation signs at the second " +
                "gait peak so their endpoints move anatomically opposite.");
            Assert.Greater(
                Mathf.Abs(leftThighRotation.Evaluate(firstPeak)),
                Mathf.Abs(rightThighRotation.Evaluate(firstPeak)),
                "The left leg must lead the first step.");
            Assert.Greater(
                Mathf.Abs(rightThighRotation.Evaluate(secondPeak)),
                Mathf.Abs(leftThighRotation.Evaluate(secondPeak)),
                "The right leg must lead the second step.");
            AssertCurveSweep(
                walk,
                shinLeft,
                firstPeak,
                secondPeak,
                35f,
                "The left knee does not bend across the gait cycle.");
            AssertCurveSweep(
                walk,
                shinRight,
                firstPeak,
                secondPeak,
                35f,
                "The right knee does not bend across the gait cycle.");
            AssertCurveSweep(
                walk,
                footLeft,
                firstPeak,
                secondPeak,
                18f,
                "The left foot does not plant and release.");
            AssertCurveSweep(
                walk,
                footRight,
                firstPeak,
                secondPeak,
                18f,
                "The right foot does not plant and release.");

            Assert.Greater(
                Mathf.Abs(
                    RequireCurve(
                            walk,
                            upperArmLeft,
                            "localEulerAnglesRaw.z")
                        .Evaluate(firstPeak) -
                    RequireCurve(
                            walk,
                            upperArmLeft,
                            "localEulerAnglesRaw.z")
                        .Evaluate(secondPeak)),
                35f,
                "The left arm does not counter-swing across the gait cycle.");
            Assert.Greater(
                Mathf.Abs(
                    RequireCurve(
                            walk,
                            upperArmRight,
                            "localEulerAnglesRaw.z")
                        .Evaluate(firstPeak) -
                    RequireCurve(
                            walk,
                            upperArmRight,
                            "localEulerAnglesRaw.z")
                        .Evaluate(secondPeak)),
                35f,
                "The right arm does not counter-swing across the gait cycle.");

            AnimationCurve leftArmRotation = RequireCurve(
                walk,
                upperArmLeft,
                "localEulerAnglesRaw.z");
            AnimationCurve rightArmRotation = RequireCurve(
                walk,
                upperArmRight,
                "localEulerAnglesRaw.z");
            Assert.Greater(
                leftArmRotation.Evaluate(firstPeak) *
                rightArmRotation.Evaluate(firstPeak),
                0f,
                "Mirrored arms need matching raw rotation signs at the first " +
                "peak so their endpoints counter-swing.");
            Assert.Greater(
                leftArmRotation.Evaluate(secondPeak) *
                rightArmRotation.Evaluate(secondPeak),
                0f,
                "Mirrored arms need matching raw rotation signs at the second " +
                "peak so their endpoints counter-swing.");
            AssertCurveSweep(
                walk,
                forearmLeft,
                firstPeak,
                secondPeak,
                20f,
                "The left elbow does not follow the arm swing.");
            AssertCurveSweep(
                walk,
                forearmRight,
                firstPeak,
                secondPeak,
                20f,
                "The right elbow does not follow the arm swing.");
            AssertCurveSweep(
                walk,
                handLeft,
                firstPeak,
                secondPeak,
                8f,
                "The left hand remains rigid through the walk.");
            AssertCurveSweep(
                walk,
                handRight,
                firstPeak,
                secondPeak,
                8f,
                "The right hand remains rigid through the walk.");

            AnimationCurve pelvisBalance = RequireCurve(
                walk,
                pelvis,
                "localEulerAnglesRaw.z");
            Assert.LessOrEqual(
                Mathf.Max(
                    Mathf.Abs(pelvisBalance.Evaluate(firstPeak)),
                    Mathf.Abs(pelvisBalance.Evaluate(secondPeak))),
                0.75f,
                "Pelvis rock is large enough to disguise a static gait.");

            AnimationCurve rootSway = AnimationUtility.GetEditorCurve(
                walk,
                EditorCurveBinding.FloatCurve(
                    visual,
                    typeof(Transform),
                    "m_LocalPosition.x"));
            Assert.IsNull(
                rootSway,
                "Walk must not fake locomotion with side-to-side root sway.");
        }

        private static void AssertV23FullFrameSheetsAreImportable()
        {
            string root =
                "Assets/GameWorkPatch4/Art/Character/FatMan/" +
                "V23FullFrame/";
            string[] fileNames =
            {
                "FatMan_Idle_V23.png",
                "FatMan_Face_V23.png",
                "FatMan_Tap_V23.png",
                "FatMan_Pose_V23.png",
                "FatMan_Upgrade_V23.png",
                "FatMan_WalkRight_V23.png"
            };

            for (int i = 0; i < fileNames.Length; i++)
            {
                string path = root + fileNames[i];
                Texture2D sheet =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.NotNull(
                    sheet,
                    "A V23 complete-frame sheet is missing: " + path);
                Assert.AreEqual(1536, sheet.width, path);
                Assert.AreEqual(1024, sheet.height, path);
                Assert.AreEqual(0, sheet.width % 4, path);
                Assert.AreEqual(0, sheet.height % 2, path);

                TextureImporter importer =
                    AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.NotNull(importer, path);
                Assert.IsTrue(importer.isReadable, path);
                Assert.IsTrue(importer.alphaIsTransparency, path);
                Assert.IsFalse(importer.mipmapEnabled, path);
                Assert.AreEqual(
                    TextureWrapMode.Clamp,
                    importer.wrapMode,
                    path);
                Assert.AreEqual(
                    TextureImporterCompression.Uncompressed,
                    importer.textureCompression,
                    path);
            }
        }

        private static void AssertV24UpgradeCorrectionIsImportable()
        {
            const string path =
                "Assets/GameWorkPatch4/Art/Character/FatMan/" +
                "V24Corrections/FatMan_Upgrade_V24.png";
            Texture2D sheet =
                AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.NotNull(
                sheet,
                "The complete-body V24 upgrade correction is missing.");
            Assert.AreEqual(1536, sheet.width, path);
            Assert.AreEqual(1024, sheet.height, path);

            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.NotNull(importer, path);
            Assert.IsTrue(importer.isReadable, path);
            Assert.IsTrue(importer.alphaIsTransparency, path);
            Assert.IsFalse(importer.mipmapEnabled, path);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode, path);
            Assert.AreEqual(
                TextureImporterCompression.Uncompressed,
                importer.textureCompression,
                path);
            Type prefabBuilder = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Editor." +
                "Patch4PrefabBuilder");
            FieldInfo upgradeSheetPath = prefabBuilder.GetField(
                "V23UpgradeSheetPath",
                BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(
                upgradeSheetPath,
                "The prefab builder upgrade-sheet path is missing.");
            Assert.AreEqual(
                path,
                (string)upgradeSheetPath.GetRawConstantValue(),
                "The prefab builder must bind the corrected full-body " +
                "upgrade sheet rather than the cropped V23 source.");
        }

        private static void AssertWholeFramePlaybackCadence()
        {
            Type presentation = RequireType(
                "SkinnyToBeast.Gameplay.Patch4." +
                "Patch4V23FullFramePresentation");
            MethodInfo resolve = presentation.GetMethod(
                "ResolvePlaybackDuration",
                BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(resolve);
            Assert.LessOrEqual(
                (float)resolve.Invoke(null, new object[]
                {
                    "FatMan_Walk_InRoom"
                }),
                0.8f,
                "The eight-frame walk must play at a responsive whole-frame " +
                "cadence rather than the slow diagnostic timing.");
            Assert.LessOrEqual(
                (float)resolve.Invoke(null, new object[]
                {
                    "FatMan_TapReact_01"
                }),
                0.5f,
                "Tap reactions must not linger at screenshot-review speed.");
        }

        private static void AssertCurveSweep(
            AnimationClip clip,
            string path,
            float firstTime,
            float secondTime,
            float minimumSweep,
            string message)
        {
            AnimationCurve curve = RequireCurve(
                clip,
                path,
                "localEulerAnglesRaw.z");
            Assert.Greater(
                Mathf.Abs(
                    curve.Evaluate(firstTime) -
                    curve.Evaluate(secondTime)),
                minimumSweep,
                message);
        }

        private static void AssertAnimatorControllerHasCanonicalRootStatePaths(
            IReadOnlyList<string> requiredStateNames)
        {
            const string controllerPath =
                "Assets/GameWorkPatch4/Animations/FatMan_Patch4.controller";
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    controllerPath);
            Assert.NotNull(
                controller,
                "The generated Patch 4 Animator Controller is missing.");
            Assert.Greater(
                controller.layers.Length,
                0,
                "The generated Animator Controller has no layer.");

            AnimatorControllerLayer layer = controller.layers[0];
            AnimatorStateMachine machine = layer.stateMachine;
            Assert.NotNull(
                machine,
                "The generated Animator layer has no root state machine.");
            Assert.AreEqual(
                layer.name,
                machine.name,
                "The root state-machine name must match its layer so " +
                "<layer>.<state> hashes resolve at runtime.");

            HashSet<string> stateNames = new(
                machine.states
                    .Where(child => child.state != null)
                    .Select(child => child.state.name),
                StringComparer.Ordinal);
            CollectionAssert.IsSubsetOf(
                requiredStateNames,
                stateNames,
                "The controller does not expose every required clip as a " +
                "direct root state.");
        }

        private static void AssertAnimatorControllerRoutesGameplayActions()
        {
            const string controllerPath =
                "Assets/GameWorkPatch4/Animations/FatMan_Patch4.controller";
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    controllerPath);
            Assert.NotNull(controller);

            string[] expectedParameters =
            {
                "Speed",
                "Look",
                "Shift",
                "Turn",
                "Sit",
                "TapVariant",
                "Tap",
                "Blink",
                "Upgrade"
            };
            CollectionAssert.IsSubsetOf(
                expectedParameters,
                controller.parameters.Select(parameter => parameter.name),
                "The Animator is missing a gameplay-action parameter.");

            AnimatorStateMachine machine =
                controller.layers[0].stateMachine;
            Dictionary<string, AnimatorState> states = machine.states
                .Where(child => child.state != null)
                .ToDictionary(
                    child => child.state.name,
                    child => child.state,
                    StringComparer.Ordinal);
            AnimatorState idle = states["FatMan_Idle_Breathe"];

            AssertStateTransition(
                idle,
                "FatMan_Walk_InRoom",
                "Speed",
                AnimatorConditionMode.Greater);
            AssertStateTransition(
                idle,
                "FatMan_LookAround",
                "Look",
                AnimatorConditionMode.If);
            AssertStateTransition(
                idle,
                "FatMan_Idle_ShiftWeight",
                "Shift",
                AnimatorConditionMode.If);
            AssertStateTransition(
                idle,
                "FatMan_SitOrLean",
                "Sit",
                AnimatorConditionMode.If);

            AssertAnyStateTransition(
                machine,
                "FatMan_Turn",
                "Turn",
                AnimatorConditionMode.If);
            AssertAnyStateTransition(
                machine,
                "FatMan_Blink_Random",
                "Blink",
                AnimatorConditionMode.If);
            AssertAnyStateTransition(
                machine,
                "FatMan_UpgradeReact",
                "Upgrade",
                AnimatorConditionMode.If);
            AssertTapTransition(machine, "FatMan_TapReact_01", 1f);
            AssertTapTransition(machine, "FatMan_TapReact_02", 2f);

            Type presentation = RequireType(
                "SkinnyToBeast.Gameplay.Patch4." +
                "Patch4V23FullFramePresentation");
            MethodInfo resolveDuration = presentation.GetMethod(
                "ResolvePlaybackDuration",
                BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(resolveDuration);
            foreach (AnimatorState state in states.Values)
            {
                AnimationClip clip = state.motion as AnimationClip;
                Assert.NotNull(
                    clip,
                    state.name + " has no source clip.");
                float targetDuration = (float)resolveDuration.Invoke(
                    null,
                    new object[] { state.name });
                float expectedSpeed = clip.length /
                    Mathf.Max(0.05f, targetDuration);
                Assert.AreEqual(
                    expectedSpeed,
                    state.speed,
                    0.001f,
                    state.name +
                    " does not finish with its visible whole-frame cadence.");
            }

            Assert.IsFalse(
                idle.transitions.Any(transition =>
                    transition.destinationState ==
                        states["FatMan_Idle_ShiftWeight"] &&
                    transition.conditions.Length == 0),
                "ShiftWeight must be requested by the real routine action, " +
                "not entered automatically after every idle loop.");
        }

        private static void AssertAutomatedTestRunnerOwnsPlayMode()
        {
            Type automatedRunner = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Editor." +
                "Patch4AutomatedTestRunner");
            Assert.NotNull(
                automatedRunner.GetProperty(
                    "IsRunInProgress",
                    BindingFlags.Static | BindingFlags.Public),
                "Room-review code cannot identify Test Runner ownership.");

            Type roomReview = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Editor." +
                "Patch4AnimationRoomReview");
            Assert.NotNull(
                roomReview.GetMethod(
                    "PrepareForAutomatedTests",
                    BindingFlags.Static | BindingFlags.Public),
                "Automated tests cannot clear stale room-review ownership.");

            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                Application.dataPath;
            string runnerSource = File.ReadAllText(Path.Combine(
                projectRoot,
                "Assets/GameWorkPatch4/Editor/" +
                "Patch4AutomatedTestRunner.cs"));
            foreach (string snippet in new[]
            {
                "Patch4InteractiveGameplayPreview.PrepareForAutomatedTests()",
                "Patch4AnimationRoomReview.PrepareForAutomatedTests()",
                "LivingGameplayAnimatorAssetBuilder.EnsureCurrentAssets()",
                "SessionState.SetBool(",
                "LegacyAnimatorResumePlayKey"
            })
            {
                StringAssert.Contains(
                    snippet,
                    runnerSource,
                    "PlayMode preflight is missing: " + snippet);
            }

            string roomReviewSource = File.ReadAllText(Path.Combine(
                projectRoot,
                "Assets/GameWorkPatch4/Editor/" +
                "Patch4AnimationRoomReview.cs"));
            foreach (string snippet in new[]
            {
                "Patch4AutomatedTestRunner.IsRunInProgress",
                "ClearReviewOwnership()",
                "blocked a stale room-review request"
            })
            {
                StringAssert.Contains(
                    snippet,
                    roomReviewSource,
                    "Room review can still steal Test Runner PlayMode: " +
                    snippet);
            }
        }

        private static void AssertLockedInteractiveGameplayPreview()
        {
            Type preview = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Editor." +
                "Patch4InteractiveGameplayPreview");
            Assert.NotNull(
                preview.GetMethod(
                    "StartAfterFreshReview",
                    BindingFlags.Static | BindingFlags.Public));
            Assert.NotNull(
                preview.GetMethod(
                    "PrepareForAutomatedTests",
                    BindingFlags.Static | BindingFlags.Public));
            RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Editor." +
                "Patch4InteractiveGameplayPreviewDriver");

            Type presentation = RequireType(
                "SkinnyToBeast.Gameplay.Patch4." +
                "Patch4V23FullFramePresentation");
            Assert.NotNull(
                presentation.GetMethod(
                    "SetEditorGameplayPreviewActive",
                    BindingFlags.Instance | BindingFlags.Public),
                "The complete-frame surface cannot follow the live Animator " +
                "without opening production readiness.");

            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                Application.dataPath;
            string previewSource = File.ReadAllText(Path.Combine(
                projectRoot,
                "Assets/GameWorkPatch4/Editor/" +
                "Patch4InteractiveGameplayPreview.cs"));
            foreach (string snippet in new[]
            {
                "GameplayWindowController.Show()",
                "Patch4RuntimeInstaller.InstallAvailableGameplayRigs()",
                "LivingGameplayAnimatorAssetBuilder.EnsureCurrentAssets()",
                "Play Mode will remain on until you stop it",
                "Patch4AnimationRoomReviewWindow.Open()"
            })
            {
                StringAssert.Contains(
                    snippet,
                    previewSource,
                    "Interactive actual-room preview is missing: " + snippet);
            }

            string driverSource = File.ReadAllText(Path.Combine(
                projectRoot,
                "Assets/GameWorkPatch4/Editor/" +
                "Patch4InteractiveGameplayPreviewDriver.cs"));
            foreach (string snippet in new[]
            {
                "rigController.SetPatch4Enabled(false)",
                "stateMachine.SetLockedReviewActive(true)",
                "visibilityGuard.enabled = false",
                "rollbackGroup.alpha = 0f",
                "SetEditorGameplayPreviewActive(true)"
            })
            {
                StringAssert.Contains(
                    snippet,
                    driverSource,
                    "Locked visual override is missing: " + snippet);
            }

            StringAssert.DoesNotContain(
                "SetPatch4Enabled(true)",
                previewSource + driverSource,
                "Interactive preview must never activate Patch 4 readiness.");
        }

        private static void AssertStateTransition(
            AnimatorState source,
            string destination,
            string parameter,
            AnimatorConditionMode mode)
        {
            Assert.IsTrue(
                source.transitions.Any(transition =>
                    transition.destinationState != null &&
                    string.Equals(
                        transition.destinationState.name,
                        destination,
                        StringComparison.Ordinal) &&
                    transition.conditions.Any(condition =>
                        string.Equals(
                            condition.parameter,
                            parameter,
                            StringComparison.Ordinal) &&
                        condition.mode == mode)),
                source.name + " does not route " + parameter +
                " to " + destination + ".");
        }

        private static void AssertAnyStateTransition(
            AnimatorStateMachine machine,
            string destination,
            string parameter,
            AnimatorConditionMode mode)
        {
            Assert.IsTrue(
                machine.anyStateTransitions.Any(transition =>
                    transition.destinationState != null &&
                    string.Equals(
                        transition.destinationState.name,
                        destination,
                        StringComparison.Ordinal) &&
                    transition.conditions.Any(condition =>
                        string.Equals(
                            condition.parameter,
                            parameter,
                            StringComparison.Ordinal) &&
                        condition.mode == mode)),
                "Any State does not route " + parameter +
                " to " + destination + ".");
        }

        private static void AssertTapTransition(
            AnimatorStateMachine machine,
            string destination,
            float variant)
        {
            Assert.IsTrue(
                machine.anyStateTransitions.Any(transition =>
                    transition.destinationState != null &&
                    string.Equals(
                        transition.destinationState.name,
                        destination,
                        StringComparison.Ordinal) &&
                    transition.conditions.Any(condition =>
                        condition.parameter == "Tap" &&
                        condition.mode == AnimatorConditionMode.If) &&
                    transition.conditions.Any(condition =>
                        condition.parameter == "TapVariant" &&
                        condition.mode == AnimatorConditionMode.Equals &&
                        Mathf.Abs(condition.threshold - variant) < 0.001f)),
                "Tap variant " + variant + " does not route to " +
                destination + ".");
        }

        private static AnimationCurve RequireCurve(
            AnimationClip clip,
            string path,
            string property)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    property));
            Assert.NotNull(
                curve,
                "Missing animation curve: " + path + " :: " + property);
            return curve;
        }

        private static IReadOnlyList<string> GetStrings(
            Type type,
            string propertyName)
        {
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(property, propertyName);

            IEnumerable enumerable = property.GetValue(null) as IEnumerable;
            Assert.NotNull(enumerable, propertyName);

            return enumerable.Cast<object>()
                .Select(value => value != null ? value.ToString() : string.Empty)
                .ToArray();
        }

        private static Type RequireType(string fullName)
        {
            Type type = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(candidate => candidate != null);

            Assert.NotNull(
                type,
                "Could not find " + fullName +
                ". Patch 4 runtime scripts may have failed to compile.");
            return type;
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, fieldName);
            field.SetValue(target, value);
        }
    }
}
