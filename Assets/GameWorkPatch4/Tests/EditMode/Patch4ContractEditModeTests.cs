using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
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
            AssertCollection(contract, "RuntimeNeutralLayerPaths", 4);
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
