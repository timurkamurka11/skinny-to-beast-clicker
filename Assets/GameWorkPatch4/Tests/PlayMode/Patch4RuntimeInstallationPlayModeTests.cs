using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SkinnyToBeast.Gameplay.Patch4.Tests.PlayMode
{
    public sealed class Patch4RuntimeInstallationPlayModeTests
    {
        private const string LegacyPrefabResourcePath =
            "UI/Gameplay/Living/CharacterRig2D";
        private const string Patch4PrefabResourcePath = "FatMan_Patch4";
        private const string Patch4InstanceName = "FatMan_Patch4_Instance";

        [UnityTest]
        public IEnumerator LivingGameplayRoomGetsLockedRollbackInstance()
        {
            GameObject room = new(
                "LivingGameplayScene",
                typeof(RectTransform),
                typeof(Canvas));

            try
            {
                Canvas roomCanvas = room.GetComponent<Canvas>();
                roomCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                GameObject legacyPrefab =
                    Resources.Load<GameObject>(LegacyPrefabResourcePath);
                Assert.NotNull(
                    legacyPrefab,
                    "The existing CharacterRig2D resource is missing.");

                GameObject patch4Prefab =
                    Resources.Load<GameObject>(Patch4PrefabResourcePath);
                Assert.NotNull(
                    patch4Prefab,
                    "The generated Patch 4 runtime resource is missing.");

                GameObject legacyRoot = UnityEngine.Object.Instantiate(
                    legacyPrefab,
                    room.transform,
                    false);
                legacyRoot.name = "CharacterRoot";

                Component legacyRig = BuildLegacyRig(legacyRoot);
                Assert.NotNull(legacyRig);

                Type installerType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4.Patch4RuntimeInstaller");
                MethodInfo install = installerType.GetMethod(
                    "InstallAvailableGameplayRigs",
                    BindingFlags.Static | BindingFlags.Public);
                Assert.NotNull(install);
                install.Invoke(null, null);

                yield return null;

                Transform patchInstance =
                    legacyRoot.transform.Find(Patch4InstanceName);
                Assert.NotNull(
                    patchInstance,
                    "Patch 4 was not installed beside the gameplay rig.");
                Assert.AreSame(
                    legacyRoot.transform,
                    patchInstance.parent);

                Type patchRigType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4CharacterRigController");
                Component patchRig =
                    patchInstance.GetComponent(patchRigType);
                Assert.NotNull(patchRig);
                Assert.IsFalse(
                    GetBoolProperty(patchRig, "Patch4Enabled"),
                    "A runtime installation must stay locked.");

                Transform patchVisual =
                    patchInstance.Find("Patch4VisualRoot");
                Assert.NotNull(patchVisual);
                Assert.IsFalse(patchVisual.gameObject.activeSelf);

                Type canvasPresentationType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4CanvasPresentation");
                Component canvasPresentation =
                    patchInstance.GetComponent(canvasPresentationType);
                Assert.NotNull(canvasPresentation);
                Assert.IsTrue(
                    GetBoolProperty(
                        canvasPresentation,
                        "IsCanvasReady"),
                    "The painted layers were not prepared for the room Canvas.");
                Assert.AreEqual(
                    40,
                    GetIntProperty(canvasPresentation, "ImageCount"));
                Assert.AreSame(
                    roomCanvas,
                    GetObjectProperty(canvasPresentation, "HostCanvas"));
                Assert.Greater(
                    GetFloatProperty(canvasPresentation, "RoomScale"),
                    1f);
                Assert.IsTrue(
                    GetBoolProperty(
                        canvasPresentation,
                        "SkinBindingsReady"),
                    "Canvas bone-weight bind poses were not captured.");
                Assert.IsTrue(
                    GetBoolProperty(
                        canvasPresentation,
                        "BindAnchorsFrozen"),
                    "Canvas bind anchors must remain fixed after capture. " +
                    "Following the animated bones again would cancel the " +
                    "visible motion.");
                Assert.AreEqual(
                    40,
                    GetIntProperty(
                        canvasPresentation,
                        "SkinDeformerCount"));
                Assert.GreaterOrEqual(
                    GetIntProperty(
                        canvasPresentation,
                        "WeightedLayerCount"),
                    1,
                    "The intact painted body needs a dense multi-bone Canvas grid.");
                Assert.IsTrue(
                    GetBoolProperty(
                        canvasPresentation,
                        "ContinuousBodyBindingReady"),
                    "The visible character must use one continuous full-body " +
                    "deformation surface.");
                Assert.IsTrue(
                    GetBoolProperty(
                        canvasPresentation,
                        "RuntimeRigidBindingsReady"),
                    "Sparse face replacements must follow one bone.");
                Assert.AreEqual(
                    9,
                    GetIntProperty(
                        canvasPresentation,
                        "RuntimeRigidLayerCount"));

                Type canvasSkinType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4CanvasSkinDeformer");
                Component[] canvasSkins =
                    patchVisual.GetComponentsInChildren(
                        canvasSkinType,
                        true);
                Assert.AreEqual(
                    40,
                    canvasSkins.Length,
                    "Each required UI Image needs one Canvas skin deformer.");
                Assert.IsTrue(
                    canvasSkins.All(
                        skin =>
                            GetBoolProperty(skin, "IsBound")),
                    "Every Canvas skin deformer must retain a valid bind pose.");
                Assert.IsTrue(
                    canvasSkins.All(
                        skin =>
                            GetBoolProperty(
                                skin,
                                "UsesFullCanvasUv")),
                    "Every deformer must map the complete transparent sprite " +
                    "canvas instead of a Tight opaque crop.");
                Assert.GreaterOrEqual(
                    canvasSkins.Count(
                        skin =>
                            GetBoolProperty(
                                skin,
                                "HasMultipleBoneWeights")),
                    1);
                Assert.GreaterOrEqual(
                    canvasSkins.Count(
                        skin =>
                            GetBoolProperty(
                                skin,
                                "IsRigidlyBound")),
                    9);
                Component continuousBodySkin = canvasSkins.Single(
                    skin =>
                        GetBoolProperty(
                            skin,
                            "UsesContinuousBodyWeights"));
                Assert.GreaterOrEqual(
                    GetIntProperty(
                        continuousBodySkin,
                        "ExpectedVertexCount"),
                    6000,
                    "The intact body needs enough vertices for smooth joint " +
                    "transitions instead of rectangular limb cuts.");

                Image[] paintedImages =
                    patchVisual.GetComponentsInChildren<Image>(true);
                Assert.AreEqual(
                    40,
                    paintedImages.Length);
                Assert.IsTrue(
                    paintedImages.All(
                        image =>
                            image != null &&
                            image.sprite != null &&
                            !image.useSpriteMesh),
                    "All painted UI Images must bypass the imported source " +
                    "mesh. Patch4CanvasSkinDeformer generates and validates " +
                    "the full-canvas weighted grid itself.");

                Vector3 localScale = patchInstance.localScale;
                Assert.AreEqual(
                    localScale.x,
                    localScale.y,
                    0.001f);
                Assert.Less(
                    patchInstance.localPosition.y,
                    0f,
                    "The master must align its painted pelvis to the legacy " +
                    "room origin.");

                SpriteRenderer[] fallbackRenderers =
                    patchVisual.GetComponentsInChildren<SpriteRenderer>(true);
                Assert.IsNotEmpty(fallbackRenderers);
                Assert.IsTrue(
                    fallbackRenderers.All(renderer => !renderer.enabled),
                    "SpriteRenderer fallbacks must not compete with UI Images.");

                AssertLayerActive(
                    patchVisual,
                    "Layer.Face.EyeWhiteL",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.Face.EyeWhiteR",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.Face.IrisL",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.Face.IrisR",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.Face.MouthClosed",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.Face.LidL",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.Face.LidR",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.Face.MouthOpen",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.Face.MouthSmile",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.FX.Sweat",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.FX.ImpactFold",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.Body.TorsoBase",
                    true);
                AssertLayerActive(
                    patchVisual,
                    "Layer.Head.HeadBase",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.ArmL.Upper",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.ArmR.Upper",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.LegL.Thigh",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.LegR.Thigh",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.Clothes.ShirtBase",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.Head.EarL",
                    false);
                AssertLayerActive(
                    patchVisual,
                    "Layer.Clothes.Bottoms",
                    false);

                Type patchFaceType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4.Patch4FaceController");
                Component patchFace =
                    patchInstance.GetComponent(patchFaceType);
                Assert.NotNull(patchFace);
                Assert.NotNull(
                    GetPrivateField(patchFace, "eyeWhiteLeft"));
                Assert.NotNull(
                    GetPrivateField(patchFace, "eyeWhiteRight"));
                Assert.IsNull(
                    GetPrivateField(patchFace, "irisLeft"));
                Assert.IsNull(
                    GetPrivateField(patchFace, "irisRight"));

                Transform rollbackVisual =
                    GetObjectProperty(legacyRig, "VisualRoot") as Transform;
                Assert.NotNull(rollbackVisual);
                Assert.IsTrue(
                    rollbackVisual.gameObject.activeSelf,
                    "Patch 3.5 must remain visible in rollback mode.");

                Type bridgeType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4.Patch4LegacySignalBridge");
                Component bridge = patchInstance.GetComponent(bridgeType);
                Assert.NotNull(bridge);
                Assert.AreSame(
                    legacyRig,
                    GetPrivateField(bridge, "legacyRig"));

                Type visibilityType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4CharacterVisibilityGuard");
                Component visibility =
                    patchInstance.GetComponent(visibilityType);
                Assert.NotNull(visibility);
                Assert.AreSame(
                    rollbackVisual.gameObject,
                    GetPrivateField(visibility, "patch35RollbackRoot"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(room);
            }
        }

        private static Component BuildLegacyRig(GameObject root)
        {
            Type rigType = RequireType(
                "SkinnyToBeast.Gameplay.CharacterRigController");
            Type faceType = RequireType(
                "SkinnyToBeast.Gameplay.CharacterFaceController");

            Component rig = root.GetComponent(rigType);
            Component face = root.GetComponent(faceType);
            Assert.NotNull(rig);
            Assert.NotNull(face);

            MethodInfo build = rigType.GetMethod(
                "Build",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(build);
            build.Invoke(
                rig,
                new object[]
                {
                    root.GetComponent<RectTransform>(),
                    face
                });
            return rig;
        }

        private static void AssertLayerActive(
            Transform root,
            string layerName,
            bool expected)
        {
            Transform generatedLayers =
                root.Find("GeneratedCanvasLayers");
            Assert.NotNull(
                generatedLayers,
                "Generated Canvas layer root is missing.");

            Transform layer = generatedLayers
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate =>
                    string.Equals(
                        candidate.name,
                        layerName,
                        StringComparison.Ordinal));
            Assert.NotNull(layer, layerName);
            Assert.AreEqual(
                expected,
                layer.gameObject.activeSelf,
                layerName);
        }

        private static bool GetBoolProperty(object target, string name)
        {
            return (bool)GetObjectProperty(target, name);
        }

        private static int GetIntProperty(object target, string name)
        {
            return (int)GetObjectProperty(target, name);
        }

        private static float GetFloatProperty(object target, string name)
        {
            return (float)GetObjectProperty(target, name);
        }

        private static object GetObjectProperty(object target, string name)
        {
            PropertyInfo property = target.GetType().GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(property, name);
            return property.GetValue(target);
        }

        private static object GetPrivateField(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, name);
            return field.GetValue(target);
        }

        private static Type RequireType(string fullName)
        {
            Type type = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(candidate => candidate != null);
            Assert.NotNull(type, "Could not find " + fullName + ".");
            return type;
        }
    }
}
