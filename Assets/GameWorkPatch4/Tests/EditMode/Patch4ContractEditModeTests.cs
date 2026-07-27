using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Tests.EditMode
{
    public sealed class Patch4ContractEditModeTests
    {
        private const string ExpectedSha =
            "5873cf6df0df2b5ebd4947b687693162d4b34899202326d1b1ae62df9f50587c";

        [Test]
        public void Contract_CollectionsHaveExpectedCountsAndNoDuplicates()
        {
            Type contract = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Patch4RigContract");

            AssertCollection(contract, "RequiredBoneNames", 31);
            AssertCollection(contract, "RequiredLayerPaths", 40);
            AssertCollection(contract, "RequiredClipNames", 10);
            AssertCollection(contract, "ProtectedPathFragments", 6);
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
