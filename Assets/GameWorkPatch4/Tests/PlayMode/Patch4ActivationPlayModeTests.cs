using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SkinnyToBeast.Gameplay.Patch4.Tests.PlayMode
{
    public sealed class Patch4ActivationPlayModeTests
    {
        private const string ExpectedSha =
            "5873cf6df0df2b5ebd4947b687693162d4b34899202326d1b1ae62df9f50587c";

        [UnityTest]
        public IEnumerator CompleteSkeletonWithoutArtApprovalKeepsRollbackVisible()
        {
            RigFixture fixture = CreateFixture(
                completeSkeleton: true,
                approvedArt: false);

            try
            {
                bool enabled = InvokeSetEnabled(fixture.Controller, true);
                yield return null;

                Assert.IsFalse(enabled);
                Assert.IsFalse(GetBoolProperty(fixture.Controller, "Patch4Enabled"));
                Assert.IsFalse(fixture.Patch4Visual.activeSelf);
                Assert.IsTrue(fixture.RollbackVisual.activeSelf);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ApprovedArtWithCompleteSkeletonCanSwitchAndRollback()
        {
            RigFixture fixture = CreateFixture(
                completeSkeleton: true,
                approvedArt: true);

            try
            {
                bool enabled = InvokeSetEnabled(fixture.Controller, true);
                yield return null;

                Assert.IsTrue(enabled);
                Assert.IsTrue(GetBoolProperty(fixture.Controller, "Patch4Enabled"));
                Assert.IsTrue(fixture.Patch4Visual.activeSelf);
                Assert.IsFalse(fixture.RollbackVisual.activeSelf);

                bool disabled = InvokeSetEnabled(fixture.Controller, false);
                yield return null;

                Assert.IsTrue(disabled);
                Assert.IsFalse(GetBoolProperty(fixture.Controller, "Patch4Enabled"));
                Assert.IsFalse(fixture.Patch4Visual.activeSelf);
                Assert.IsTrue(fixture.RollbackVisual.activeSelf);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ApprovedArtCannotBypassIncompleteSkeleton()
        {
            RigFixture fixture = CreateFixture(
                completeSkeleton: false,
                approvedArt: true);

            try
            {
                bool enabled = InvokeSetEnabled(fixture.Controller, true);
                yield return null;

                Assert.IsFalse(enabled);
                Assert.IsFalse(GetBoolProperty(fixture.Controller, "Patch4Enabled"));
                Assert.IsFalse(GetBoolProperty(fixture.Controller, "IsRigValid"));
                Assert.IsFalse(fixture.Patch4Visual.activeSelf);
                Assert.IsTrue(fixture.RollbackVisual.activeSelf);
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static RigFixture CreateFixture(
            bool completeSkeleton,
            bool approvedArt)
        {
            Type controllerType = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Patch4CharacterRigController");
            Type readinessType = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Patch4ArtReadinessAsset");

            GameObject host = new GameObject("Patch4.PlayModeTest.Host");
            host.SetActive(false);

            GameObject patch4Visual = new GameObject("Patch4VisualRoot");
            patch4Visual.transform.SetParent(host.transform, false);
            patch4Visual.SetActive(false);

            GameObject rollbackVisual = new GameObject("Patch35RollbackRoot");
            rollbackVisual.transform.SetParent(host.transform, false);
            rollbackVisual.SetActive(true);

            GameObject rigRootObject = new GameObject("Root");
            rigRootObject.transform.SetParent(patch4Visual.transform, false);

            if (completeSkeleton)
            {
                foreach (string boneName in GetRequiredBoneNames())
                {
                    if (string.Equals(boneName, "Root", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    GameObject bone = new GameObject(boneName);
                    bone.transform.SetParent(rigRootObject.transform, false);
                }
            }

            Component controller = host.AddComponent(controllerType);
            SetPrivateField(controller, "rigRoot", rigRootObject.transform);
            SetPrivateField(controller, "patch4VisualRoot", patch4Visual);
            SetPrivateField(controller, "patch35RollbackRoot", rollbackVisual);
            SetPrivateField(controller, "validateOnAwake", false);
            SetPrivateField(controller, "logValidationErrors", false);
            SetPrivateField(controller, "patch4Enabled", false);

            ScriptableObject readiness = ScriptableObject.CreateInstance(readinessType);
            SetPrivateField(readiness, "productionArtApproved", approvedArt);
            SetPrivateField(
                readiness,
                "approvedSourceSha256",
                approvedArt ? ExpectedSha : string.Empty);
            SetPrivateField(
                readiness,
                "approvedBy",
                approvedArt ? "PlayMode test fixture" : string.Empty);
            SetPrivateField(controller, "artReadiness", readiness);
            SetPrivateField(controller, "expectedSourceSha256", ExpectedSha);

            host.SetActive(true);

            return new RigFixture(
                host,
                patch4Visual,
                rollbackVisual,
                controller,
                readiness);
        }

        private static bool InvokeSetEnabled(Component controller, bool enabled)
        {
            MethodInfo method = controller.GetType().GetMethod(
                "SetPatch4Enabled",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(method);
            return (bool)method.Invoke(controller, new object[] { enabled });
        }

        private static bool GetBoolProperty(Component controller, string name)
        {
            PropertyInfo property = controller.GetType().GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, name);
            return (bool)property.GetValue(controller);
        }

        private static IReadOnlyList<string> GetRequiredBoneNames()
        {
            Type contractType = RequireType(
                "SkinnyToBeast.Gameplay.Patch4.Patch4RigContract");
            PropertyInfo property = contractType.GetProperty(
                "RequiredBoneNames",
                BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(property);

            IEnumerable values = property.GetValue(null) as IEnumerable;
            Assert.NotNull(values);
            return values.Cast<object>()
                .Select(value => value.ToString())
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

        private sealed class RigFixture : IDisposable
        {
            public readonly GameObject Host;
            public readonly GameObject Patch4Visual;
            public readonly GameObject RollbackVisual;
            public readonly Component Controller;
            public readonly ScriptableObject Readiness;

            public RigFixture(
                GameObject host,
                GameObject patch4Visual,
                GameObject rollbackVisual,
                Component controller,
                ScriptableObject readiness)
            {
                Host = host;
                Patch4Visual = patch4Visual;
                RollbackVisual = rollbackVisual;
                Controller = controller;
                Readiness = readiness;
            }

            public void Dispose()
            {
                if (Host != null)
                {
                    UnityEngine.Object.DestroyImmediate(Host);
                }

                if (Readiness != null)
                {
                    UnityEngine.Object.DestroyImmediate(Readiness);
                }
            }
        }
    }
}
