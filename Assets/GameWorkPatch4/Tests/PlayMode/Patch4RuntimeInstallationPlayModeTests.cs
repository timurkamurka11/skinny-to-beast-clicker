using System;
using System.Collections;
using System.Collections.Generic;
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
        private const float MinimumV23FaceDifference = 0.02f;

        [UnityTest]
        public IEnumerator Stage4Sync_LatchesReadinessFailureAndRecoversWithoutRetrySpam()
        {
            GameObject room = new(
                "LivingGameplayScene",
                typeof(RectTransform),
                typeof(Canvas));
            List<string> stageFailureLogs = new();
            List<string> visibilityRepairLogs = new();
            Application.LogCallback captureFailure =
                (condition, _, type) =>
                {
                    if (type == LogType.Error &&
                        condition.Contains(
                            "Character stage 4",
                            StringComparison.Ordinal))
                    {
                        stageFailureLogs.Add(condition);
                    }

                    if (type == LogType.Warning &&
                        condition.Contains(
                            "Patch 4 visibility guard repaired a conflicting " +
                            "body state",
                            StringComparison.Ordinal))
                    {
                        visibilityRepairLogs.Add(condition);
                    }
                };
            Application.logMessageReceived += captureFailure;
            try
            {
                Canvas roomCanvas = room.GetComponent<Canvas>();
                roomCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                Type stageControllerType = RequireType(
                    "SkinnyToBeast.Gameplay.GameplayVisualStageController");
                Component stageController =
                    room.AddComponent(stageControllerType);
                MethodInfo build = stageControllerType.GetMethod(
                    "Build",
                    BindingFlags.Instance | BindingFlags.Public);
                MethodInfo sync = stageControllerType.GetMethod(
                    "Sync",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(build);
                Assert.NotNull(sync);

                build.Invoke(stageController, null);
                sync.Invoke(
                    stageController,
                    new object[] { 0, null, false });
                yield return null;

                Transform characterRoot = room.transform.Find(
                    "CharacterActors/CharacterRoot");
                Assert.NotNull(characterRoot);
                Type skinType = RequireType(
                    "SkinnyToBeast.Gameplay.CharacterSkinController");
                Type legacyRigType = RequireType(
                    "SkinnyToBeast.Gameplay.CharacterRigController");
                Component skin = characterRoot.GetComponent(skinType);
                Component legacyRig = characterRoot.GetComponent(
                    legacyRigType);
                Animator animator = characterRoot.GetComponent<Animator>();
                Assert.NotNull(skin);
                Assert.NotNull(legacyRig);
                Assert.NotNull(animator);
                int[] bodyStageInputs = { 0, 1, 2, 4 };
                for (int artStage = 0;
                     artStage < bodyStageInputs.Length;
                     artStage++)
                {
                    sync.Invoke(
                        stageController,
                        new object[]
                        {
                            bodyStageInputs[artStage],
                            null,
                            false
                        });
                    yield return null;
                    Assert.AreEqual(
                        artStage,
                        GetIntProperty(skin, "CurrentArtIndex"),
                        $"Character art stage {artStage + 1} was not selected.");
                    Assert.AreEqual(
                        1,
                        GetIntProperty(skin, "ActiveBaseSkinCount"),
                        $"Character art stage {artStage + 1} has duplicate or missing " +
                        "base skins.");
                    Assert.IsTrue(
                        GetBoolProperty(skin, "IsVisualReady"),
                        $"Character art stage {artStage + 1} is not visually ready.");
                    Assert.IsTrue(
                        GetBoolProperty(legacyRig, "HasVisibleSkin"),
                        $"Character art stage {artStage + 1} has no visible rig.");
                }

                Assert.Zero(
                    stageFailureLogs.Count,
                    "A normal Stage 1-4 selection emitted a readiness error.");
                Assert.AreEqual(
                    1,
                    characterRoot.GetComponents(legacyRigType).Length,
                    "Stage 4 must own exactly one legacy gameplay rig.");

                Type installerType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4.Patch4RuntimeInstaller");
                MethodInfo install = installerType.GetMethod(
                    "InstallAvailableGameplayRigs",
                    BindingFlags.Static | BindingFlags.Public);
                Assert.NotNull(install);
                install.Invoke(null, null);
                yield return null;

                Transform patchInstance = characterRoot.Find(
                    Patch4InstanceName);
                Assert.NotNull(
                    patchInstance,
                    "The locked Patch 4 rollback candidate was not installed.");
                Type patchRigType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4CharacterRigController");
                Component patchRig = patchInstance.GetComponent(patchRigType);
                Assert.NotNull(patchRig);
                Assert.AreEqual(
                    1,
                    characterRoot.GetComponentsInChildren(
                        patchRigType,
                        true).Length,
                    "Stage 4 installed duplicate Patch 4 rig candidates.");
                Assert.IsFalse(
                    GetBoolProperty(patchRig, "Patch4Enabled"),
                    "The locked Patch 4 preview cannot become a second " +
                    "visible gameplay rig.");
                Transform patchVisual = patchInstance.Find("Patch4VisualRoot");
                Assert.NotNull(patchVisual);
                Assert.IsFalse(
                    patchVisual.gameObject.activeSelf,
                    "The rollback candidate draws a second gameplay character.");

                Type spriteRigType = RequireType(
                    "SkinnyToBeast.Gameplay.CharacterSpriteRigController");
                Component spriteRig = characterRoot.GetComponent(spriteRigType);
                Assert.NotNull(spriteRig);
                Type stateMachineType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4CharacterStateMachine");
                Component stateMachine = patchInstance.GetComponent(
                    stateMachineType);
                Assert.NotNull(stateMachine);
                MethodInfo setReviewActive = stateMachineType.GetMethod(
                    "SetLockedReviewActive",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(setReviewActive);
                Type visibilityType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4CharacterVisibilityGuard");
                Behaviour visibility = patchInstance.GetComponent(
                    visibilityType) as Behaviour;
                Assert.NotNull(visibility);
                MethodInfo suppressReplacementPixels =
                    spriteRigType.GetMethod(
                        "SetReplacementPixelsSuppressed",
                        BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(suppressReplacementPixels);

                setReviewActive.Invoke(
                    stateMachine,
                    new object[] { true });
                visibility.enabled = false;
                suppressReplacementPixels.Invoke(
                    spriteRig,
                    new object[] { true });
                patchVisual.gameObject.SetActive(true);
                Transform legacyVisual =
                    GetObjectProperty(legacyRig, "VisualRoot") as Transform;
                Assert.NotNull(legacyVisual);
                Assert.IsTrue(
                    GetBoolProperty(
                        spriteRig,
                        "PixelsSuppressedByReplacement"),
                    "The locked review did not claim legacy renderer " +
                    "ownership.");
                Assert.IsTrue(
                    patchVisual.gameObject.activeSelf,
                    "The locked review has no authoritative Patch 4 pixels.");
                Assert.IsTrue(
                    legacyVisual.gameObject.activeInHierarchy,
                    "Renderer suppression deactivated the logical Stage 4 rig.");
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    sync.Invoke(
                        stageController,
                        new object[] { 4, null, false });
                }
                Assert.Zero(
                    stageFailureLogs.Count,
                    "Renderer-only preview suppression made Stage 4 retry.");
                Assert.IsTrue(
                    GetBoolProperty(skin, "IsVisualReady"),
                    "Renderer-only suppression broke logical visual readiness.");

                animator.enabled = false;
                Assert.IsFalse(GetBoolProperty(skin, "IsVisualReady"));
                LogAssert.Expect(
                    LogType.Error,
                    "Character stage 4 was selected but did not produce a " +
                    "ready visible rig. Stage selection remains stable while " +
                    "dependencies recover. The character Animator is disabled, " +
                    "missing its controller, or missing required layers.");
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    sync.Invoke(
                        stageController,
                        new object[] { 4, null, false });
                }

                Assert.AreEqual(
                    1,
                    stageFailureLogs.Count,
                    "A persistent Stage 4 readiness fault must be reported " +
                    "once, not once per gameplay refresh.");
                StringAssert.DoesNotContain(
                    "next Sync will retry",
                    stageFailureLogs[0]);
                Assert.AreEqual(
                    3,
                    (int)GetPrivateField(
                        stageController,
                        "currentCharacterArt"),
                    "A transient readiness fault must not deselect Stage 4.");
                Assert.AreEqual(3, GetIntProperty(skin, "CurrentArtIndex"));
                Assert.AreEqual(1, GetIntProperty(skin, "ActiveBaseSkinCount"));

                animator.enabled = true;
                yield return null;
                sync.Invoke(
                    stageController,
                    new object[] { 4, null, false });

                Assert.IsTrue(
                    GetBoolProperty(skin, "IsVisualReady"),
                    "Stage 4 did not recover when its Animator became ready.");
                Assert.AreEqual(
                    1,
                    stageFailureLogs.Count,
                    "Recovery must not emit another Stage 4 failure.");
                Assert.AreEqual(
                    3,
                    (int)GetPrivateField(
                        stageController,
                        "currentCharacterArt"));

                setReviewActive.Invoke(
                    stateMachine,
                    new object[] { false });
                patchVisual.gameObject.SetActive(false);
                suppressReplacementPixels.Invoke(
                    spriteRig,
                    new object[] { false });
                visibility.enabled = true;
                yield return null;
                Assert.IsFalse(
                    GetBoolProperty(
                        spriteRig,
                        "PixelsSuppressedByReplacement"),
                    "The locked review did not restore legacy renderer " +
                    "ownership.");
                Assert.IsFalse(patchVisual.gameObject.activeSelf);
                Assert.Zero(
                    visibilityRepairLogs.Count,
                    "A correctly owned locked review required a visibility " +
                    "repair.");

                stageFailureLogs.Clear();
                MethodInfo configureSkin = skinType.GetMethod(
                    "Configure",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(configureSkin);
                configureSkin.Invoke(
                    skin,
                    new object[]
                    {
                        null,
                        characterRoot.GetComponent<CanvasGroup>(),
                        4
                    });
                LogAssert.Expect(
                    LogType.Error,
                    "Character stage 4 was selected but did not produce a " +
                    "ready visible rig. Stage selection remains stable while " +
                    "dependencies recover. Character skin controller is not " +
                    "configured.");
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    sync.Invoke(
                        stageController,
                        new object[] { 4, null, false });
                }

                Assert.AreEqual(
                    1,
                    stageFailureLogs.Count,
                    "A failed skin application must also latch instead of " +
                    "retrying every gameplay refresh.");
                Assert.AreEqual(
                    3,
                    (int)GetPrivateField(
                        stageController,
                        "currentCharacterArt"));

                configureSkin.Invoke(
                    skin,
                    new object[]
                    {
                        legacyRig,
                        characterRoot.GetComponent<CanvasGroup>(),
                        4
                    });
                MethodInfo applySkin = skinType.GetMethod(
                    "ApplySkin",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(applySkin);
                applySkin.Invoke(skin, new object[] { 3, false });
                sync.Invoke(
                    stageController,
                    new object[] { 4, null, false });
                Assert.IsTrue(
                    GetBoolProperty(skin, "IsConfigured"),
                    "The negative-path skin fault was not restored.");
                Assert.IsTrue(
                    GetBoolProperty(skin, "IsVisualReady"),
                    "Stage 4 did not recover after its real skin dependency " +
                    "was rebound.");
            }
            finally
            {
                Application.logMessageReceived -= captureFailure;
                UnityEngine.Object.DestroyImmediate(room);
            }
        }

        [UnityTest]
        public IEnumerator Stage4Installer_ActivatesOnlyAfterRuntimeReadiness()
        {
            GameObject room = new(
                "LivingGameplayScene",
                typeof(RectTransform),
                typeof(Canvas));
            ScriptableObject approvedReadiness = null;

            try
            {
                Canvas roomCanvas = room.GetComponent<Canvas>();
                roomCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                Type stageControllerType = RequireType(
                    "SkinnyToBeast.Gameplay.GameplayVisualStageController");
                Component stageController =
                    room.AddComponent(stageControllerType);
                MethodInfo build = stageControllerType.GetMethod(
                    "Build",
                    BindingFlags.Instance | BindingFlags.Public);
                MethodInfo sync = stageControllerType.GetMethod(
                    "Sync",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(build);
                Assert.NotNull(sync);

                build.Invoke(stageController, null);
                sync.Invoke(
                    stageController,
                    new object[] { 4, null, false });
                yield return null;
                yield return null;

                Transform characterRoot = room.transform.Find(
                    "CharacterActors/CharacterRoot");
                Assert.NotNull(characterRoot);

                Type legacyRigType = RequireType(
                    "SkinnyToBeast.Gameplay.CharacterRigController");
                Type skinType = RequireType(
                    "SkinnyToBeast.Gameplay.CharacterSkinController");
                Type spriteRigType = RequireType(
                    "SkinnyToBeast.Gameplay.CharacterSpriteRigController");
                Component legacyRig = characterRoot.GetComponent(
                    legacyRigType);
                Component skin = characterRoot.GetComponent(skinType);
                Component spriteRig = characterRoot.GetComponent(
                    spriteRigType);
                Assert.NotNull(legacyRig);
                Assert.NotNull(skin);
                Assert.NotNull(spriteRig);
                Assert.IsTrue(GetBoolProperty(skin, "IsConfigured"));
                Assert.IsTrue(GetBoolProperty(skin, "IsVisualReady"));
                Assert.AreEqual(3, GetIntProperty(skin, "CurrentArtIndex"));

                Type installerType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4.Patch4RuntimeInstaller");
                MethodInfo install = installerType.GetMethod(
                    "InstallAvailableGameplayRigs",
                    BindingFlags.Static | BindingFlags.Public);
                Assert.NotNull(install);
                install.Invoke(null, null);
                yield return null;

                Transform patchInstance = characterRoot.Find(
                    Patch4InstanceName);
                Assert.NotNull(patchInstance);
                Type patchRigType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4CharacterRigController");
                Component patchRig = patchInstance.GetComponent(
                    patchRigType);
                Assert.NotNull(patchRig);

                Animator animator = patchInstance.GetComponent<Animator>();
                AssertRuntimeAnimatorContract(animator);
                Type stateMachineType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4CharacterStateMachine");
                Component stateMachine = patchInstance.GetComponent(
                    stateMachineType);
                Assert.NotNull(stateMachine);
                Type bridgeType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4LegacySignalBridge");
                Component bridge = patchInstance.GetComponent(bridgeType);
                Assert.NotNull(bridge);
                Transform patchVisual = patchInstance.Find(
                    "Patch4VisualRoot");
                Assert.NotNull(patchVisual);
                Type visibilityType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4CharacterVisibilityGuard");
                Behaviour visibility = patchInstance.GetComponent(
                    visibilityType) as Behaviour;
                Assert.NotNull(visibility);
                MethodInfo setReviewActive = stateMachineType.GetMethod(
                    "SetLockedReviewActive",
                    BindingFlags.Instance | BindingFlags.Public);
                MethodInfo suppressReplacementPixels =
                    spriteRigType.GetMethod(
                        "SetReplacementPixelsSuppressed",
                        BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(setReviewActive);
                Assert.NotNull(suppressReplacementPixels);

                visibility.enabled = false;
                setReviewActive.Invoke(
                    stateMachine,
                    new object[] { true });
                suppressReplacementPixels.Invoke(
                    spriteRig,
                    new object[] { true });
                patchVisual.gameObject.SetActive(true);
                install.Invoke(null, null);
                yield return null;
                Assert.IsTrue(
                    patchVisual.gameObject.activeSelf,
                    "RuntimeInstaller clobbered the editor-only locked " +
                    "gameplay review visual override.");
                Assert.IsTrue(
                    GetBoolProperty(
                        spriteRig,
                        "PixelsSuppressedByReplacement"),
                    "RuntimeInstaller restored Patch 3.5 pixels during the " +
                    "locked gameplay review.");
                Assert.IsFalse(
                    GetBoolProperty(patchRig, "Patch4Enabled"),
                    "The locked review must not approve production Patch 4.");
                setReviewActive.Invoke(
                    stateMachine,
                    new object[] { false });
                patchVisual.gameObject.SetActive(false);
                suppressReplacementPixels.Invoke(
                    spriteRig,
                    new object[] { false });
                visibility.enabled = true;

                SetPrivateField(stateMachine, "animator", null);
                SetPrivateField(bridge, "stateMachine", null);

                Type readinessType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4ArtReadinessAsset");
                approvedReadiness =
                    ScriptableObject.CreateInstance(readinessType);
                SetPrivateField(
                    approvedReadiness,
                    "productionArtApproved",
                    true);
                SetPrivateField(
                    approvedReadiness,
                    "approvedSourceSha256",
                    GetObjectProperty(patchRig, "ExpectedSourceSha256"));
                SetPrivateField(
                    approvedReadiness,
                    "approvedBy",
                    "Patch4RuntimeInstallationPlayModeTests");
                SetPrivateField(
                    patchRig,
                    "artReadiness",
                    approvedReadiness);

                install.Invoke(null, null);

                Assert.AreSame(
                    animator,
                    GetPrivateField(stateMachine, "animator"),
                    "RuntimeInstaller did not bind the authoritative root " +
                    "Animator after resolving the prefab.");
                Assert.IsTrue(
                    GetBoolProperty(stateMachine, "IsConfigured"));
                Assert.AreSame(
                    stateMachine,
                    GetPrivateField(bridge, "stateMachine"),
                    "RuntimeInstaller did not restore the Patch 4 signal " +
                    "bridge dependency.");
                Assert.IsTrue(GetBoolProperty(bridge, "IsBound"));

                Assert.IsTrue(
                    GetBoolProperty(patchRig, "Patch4Enabled"),
                    "A fully configured, approved Stage 4 rig did not " +
                    "activate.");
                Assert.IsTrue(patchVisual.gameObject.activeSelf);
                Assert.IsTrue(
                    GetBoolProperty(
                        spriteRig,
                        "PixelsSuppressedByReplacement"),
                    "Patch 3.5 pixels were not suppressed by their renderer " +
                    "owner.");

                Transform legacyVisual =
                    GetObjectProperty(legacyRig, "VisualRoot") as Transform;
                Assert.NotNull(legacyVisual);
                Assert.IsTrue(
                    legacyVisual.gameObject.activeInHierarchy,
                    "Patch 4 activation must keep the logical Stage 4 " +
                    "hierarchy alive.");
                Assert.IsTrue(
                    GetBoolProperty(skin, "IsVisualReady"),
                    "The authoritative Patch 4 replacement made Stage 4 " +
                    "readiness reject its configured legacy signal owner.");
                List<string> activationFailureLogs = new();
                Application.LogCallback captureActivationFailure =
                    (condition, _, type) =>
                    {
                        if (type == LogType.Error &&
                            condition.Contains(
                                "Character stage 4",
                                StringComparison.Ordinal))
                        {
                            activationFailureLogs.Add(condition);
                        }
                    };
                Application.logMessageReceived += captureActivationFailure;
                try
                {
                    for (int refresh = 0; refresh < 5; refresh++)
                    {
                        sync.Invoke(
                            stageController,
                            new object[] { 4, null, false });
                        install.Invoke(null, null);
                    }
                }
                finally
                {
                    Application.logMessageReceived -=
                        captureActivationFailure;
                }

                Assert.Zero(
                    activationFailureLogs.Count,
                    "A valid active Patch 4 replacement emitted Stage 4 " +
                    "readiness errors or retry spam.");
                Assert.AreEqual(
                    1,
                    characterRoot.GetComponentsInChildren(
                        patchRigType,
                        true).Length,
                    "Stage 4 produced duplicate Patch 4 rigs.");

                animator.enabled = false;
                install.Invoke(null, null);
                Assert.IsFalse(
                    GetBoolProperty(patchRig, "Patch4Enabled"),
                    "A broken Animator must restore rollback instead of " +
                    "forcing Patch 4 visible.");
                Assert.IsFalse(patchVisual.gameObject.activeSelf);
                Assert.IsFalse(
                    GetBoolProperty(
                        spriteRig,
                        "PixelsSuppressedByReplacement"));
                Assert.IsTrue(legacyVisual.gameObject.activeInHierarchy);
                Assert.IsTrue(GetBoolProperty(skin, "IsVisualReady"));

                animator.enabled = true;
                install.Invoke(null, null);
                Assert.IsTrue(
                    GetBoolProperty(patchRig, "Patch4Enabled"),
                    "Stage 4 did not recover after the Animator contract " +
                    "became valid again.");
                Assert.IsTrue(patchVisual.gameObject.activeSelf);

                MethodInfo configureSkin = skinType.GetMethod(
                    "Configure",
                    BindingFlags.Instance | BindingFlags.Public);
                MethodInfo applySkin = skinType.GetMethod(
                    "ApplySkin",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(configureSkin);
                Assert.NotNull(applySkin);
                configureSkin.Invoke(
                    skin,
                    new object[]
                    {
                        null,
                        characterRoot.GetComponent<CanvasGroup>(),
                        4
                    });
                install.Invoke(null, null);
                Assert.IsFalse(
                    GetBoolProperty(patchRig, "Patch4Enabled"),
                    "An unconfigured gameplay skin dependency must restore " +
                    "Patch 3.5 rollback.");
                Assert.IsFalse(patchVisual.gameObject.activeSelf);
                Assert.IsFalse(
                    GetBoolProperty(
                        spriteRig,
                        "PixelsSuppressedByReplacement"));

                configureSkin.Invoke(
                    skin,
                    new object[]
                    {
                        legacyRig,
                        characterRoot.GetComponent<CanvasGroup>(),
                        4
                    });
                applySkin.Invoke(skin, new object[] { 3, false });
                install.Invoke(null, null);
                Assert.IsTrue(
                    GetBoolProperty(patchRig, "Patch4Enabled"),
                    "Stage 4 did not recover after its authoritative skin " +
                    "controller was configured again.");
                Assert.IsTrue(GetBoolProperty(skin, "IsVisualReady"));

                sync.Invoke(
                    stageController,
                    new object[] { 0, null, false });
                install.Invoke(null, null);
                Assert.IsFalse(
                    GetBoolProperty(patchRig, "Patch4Enabled"),
                    "Patch 4 remained visible after leaving Stage 4.");
                Assert.IsFalse(
                    GetBoolProperty(
                        spriteRig,
                        "PixelsSuppressedByReplacement"));
                Assert.IsTrue(GetBoolProperty(skin, "IsVisualReady"));
            }
            finally
            {
                if (approvedReadiness != null)
                {
                    UnityEngine.Object.DestroyImmediate(approvedReadiness);
                }

                UnityEngine.Object.DestroyImmediate(room);
            }
        }

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

                Component legacyRig = BuildReadyLegacyGameplayOwner(
                    legacyRoot,
                    out Component legacySkin,
                    out Animator legacyAnimator);
                Assert.NotNull(legacyRig);
                Assert.NotNull(legacySkin);
                Assert.IsTrue(
                    GetBoolProperty(legacySkin, "IsConfigured"),
                    "The rollback skin must be configured before Patch 4 " +
                    "preview suppression begins.");
                Assert.AreEqual(
                    3,
                    GetIntProperty(legacySkin, "CurrentArtIndex"),
                    "The rollback owner must have the Stage 4 skin applied " +
                    "before Patch 4 preview suppression begins.");
                AssertLegacyAnimatorContract(legacyAnimator);
                Assert.IsTrue(
                    GetBoolProperty(legacyRig, "AnimatorReady"),
                    "The rollback Animator must be ready before Patch 4 " +
                    "preview suppression begins.");
                Assert.IsTrue(
                    GetBoolProperty(legacySkin, "IsVisualReady"),
                    "The configured rollback skin must pass its complete " +
                    "readiness gate before Patch 4 preview suppression begins.");

                Transform legacyVisual =
                    GetObjectProperty(legacyRig, "VisualRoot") as Transform;
                Assert.NotNull(legacyVisual);
                Assert.IsTrue(
                    GetBoolProperty(legacyRig, "HasVisibleSkin"),
                    "The rollback rig must be logically visible before " +
                    "Patch 4 preview suppression begins.");
                Type legacySpriteRigType = RequireType(
                    "SkinnyToBeast.Gameplay.CharacterSpriteRigController");
                Component legacySpriteRig =
                    legacyRoot.GetComponent(legacySpriteRigType);
                Assert.NotNull(legacySpriteRig);
                MethodInfo suppressPreviewPixels =
                    legacySpriteRigType.GetMethod(
                        "SetEditorPreviewSuppressed",
                        BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(suppressPreviewPixels);
                bool legacyVisualWasActive = legacyVisual.gameObject.activeSelf;
                suppressPreviewPixels.Invoke(
                    legacySpriteRig,
                    new object[] { true });
                Assert.IsTrue(
                    GetBoolProperty(
                        legacySpriteRig,
                        "EditorPreviewSuppressed"));
                Assert.AreEqual(
                    legacyVisualWasActive,
                    legacyVisual.gameObject.activeSelf,
                    "Pixel suppression must not deactivate VisualRoot; doing " +
                    "so makes the live stage controller retry forever.");
                Assert.IsTrue(
                    GetBoolProperty(legacyRig, "HasVisibleSkin"),
                    "Renderer-only suppression must preserve the logical " +
                    "Stage 4 skin while Patch 4 owns the visible pixels.");
                suppressPreviewPixels.Invoke(
                    legacySpriteRig,
                    new object[] { false });
                Assert.IsFalse(
                    GetBoolProperty(
                        legacySpriteRig,
                        "EditorPreviewSuppressed"));
                Assert.AreEqual(
                    legacyVisualWasActive,
                    legacyVisual.gameObject.activeSelf);

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
                AssertRuntimeAnimatorContract(
                    patchInstance.GetComponent<Animator>());
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
                    13000,
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

                Type v23PresentationType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4V23FullFramePresentation");
                Component v23Presentation =
                    patchInstance.GetComponent(v23PresentationType);
                Assert.NotNull(
                    v23Presentation,
                    "The generated prefab has no ten-state full-frame surface.");
                Assert.IsTrue(
                    GetBoolProperty(v23Presentation, "IsReady"),
                    "One or more V23 complete-frame sheets were not bound.");
                Assert.IsTrue(
                    GetBoolProperty(
                        v23Presentation,
                        "FrameCalibrationReady"),
                    "Whole-frame shoe-line and scale calibration is missing.");
                MethodInfo measureCalibration =
                    v23PresentationType.GetMethod(
                        "TryMeasureFrameCalibration",
                        BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(measureCalibration);
                object[] calibrationMetrics = { 0, 0f, 0f };
                Assert.IsTrue(
                    (bool)measureCalibration.Invoke(
                        v23Presentation,
                        calibrationMetrics));
                Assert.AreEqual(
                    0,
                    (int)calibrationMetrics[0],
                    "A complete-body source frame touches its atlas edge.");
                Assert.Greater(
                    (float)calibrationMetrics[1],
                    0f,
                    "The test fixture must exercise real source-padding " +
                    "correction rather than a no-op calibration.");
                Assert.LessOrEqual(
                    (float)calibrationMetrics[2],
                    0.14f,
                    "Per-state scale correction exceeded the bounded " +
                    "calibration contract.");
                Assert.AreEqual(
                    16,
                    GetIntProperty(v23Presentation, "FrameCount"));
                Assert.AreEqual(
                    10,
                    GetIntProperty(v23Presentation, "StateCount"));
                Assert.NotNull(
                    GetObjectProperty(v23Presentation, "WalkSheet"));
                MethodInfo measureFace = v23PresentationType.GetMethod(
                    "TryMeasureFaceArticulation",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(measureFace);
                object[] faceMetrics = { 0f, 0f };
                Assert.IsTrue(
                    (bool)measureFace.Invoke(v23Presentation, faceMetrics));
                Assert.GreaterOrEqual(
                    (float)faceMetrics[0],
                    MinimumV23FaceDifference,
                    "The complete-frame blink is too weak.");
                Assert.GreaterOrEqual(
                    (float)faceMetrics[1],
                    MinimumV23FaceDifference,
                    "The complete-frame look-around motion is too weak.");
                RawImage[] frameImages =
                    patchVisual.GetComponentsInChildren<RawImage>(true);
                Assert.AreEqual(
                    1,
                    frameImages.Length,
                    "All ten clips must share one intact complete-frame surface.");
                Assert.IsFalse(
                    frameImages[0].enabled,
                    "The locked rollback instance must not expose Patch 4.");

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
                Assert.IsNull(
                    GetPrivateField(patchFace, "eyeWhiteLeft"),
                    "Neutral left-eye artwork must remain inside the exact " +
                    "master body instead of rendering as a duplicate layer.");
                Assert.IsNull(
                    GetPrivateField(patchFace, "eyeWhiteRight"),
                    "Neutral right-eye artwork must remain inside the exact " +
                    "master body instead of rendering as a duplicate layer.");
                Assert.IsNull(
                    GetPrivateField(patchFace, "irisLeft"));
                Assert.IsNull(
                    GetPrivateField(patchFace, "irisRight"));
                Assert.IsNull(
                    GetPrivateField(patchFace, "mouthClosed"),
                    "The neutral closed mouth must remain inside the exact " +
                    "master body instead of rendering as a duplicate layer.");
                Assert.NotNull(
                    GetPrivateField(patchFace, "lidLeft"),
                    "The left feathered blink replacement is missing.");
                Assert.NotNull(
                    GetPrivateField(patchFace, "lidRight"),
                    "The right feathered blink replacement is missing.");
                Assert.NotNull(
                    GetPrivateField(patchFace, "mouthOpen"),
                    "The feathered open-mouth replacement is missing.");
                Assert.NotNull(
                    GetPrivateField(patchFace, "mouthSmile"),
                    "The feathered smile replacement is missing.");

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
                Assert.NotNull(
                    GetPrivateField(bridge, "legacySkin"),
                    "The installer did not bind the authoritative gameplay " +
                    "skin controller.");

                Type visibilityType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4CharacterVisibilityGuard");
                Component visibility =
                    patchInstance.GetComponent(visibilityType);
                Assert.NotNull(visibility);
                Assert.AreSame(
                    rollbackVisual.gameObject,
                    GetPrivateField(visibility, "patch35RollbackRoot"));

                AssertWalkAnimatorStateProducesArticulation(
                    patchInstance,
                    patchVisual);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(room);
            }
        }

        private static void AssertWalkAnimatorStateProducesArticulation(
            Transform patchInstance,
            Transform patchVisual)
        {
            Animator animator = patchInstance.GetComponent<Animator>();
            Assert.NotNull(
                animator,
                "The generated Patch 4 prefab has no Animator.");

            bool visualWasActive = patchVisual.gameObject.activeSelf;
            float previousAnimatorSpeed = animator.speed;
            string layerName = animator.GetLayerName(0);
            string statePath = layerName + ".FatMan_Walk_InRoom";
            int stateHash = Animator.StringToHash(statePath);
            int idleStateHash = Animator.StringToHash(
                layerName + ".FatMan_Idle_Breathe");
            Type stateMachineType = RequireType(
                "SkinnyToBeast.Gameplay.Patch4." +
                "Patch4CharacterStateMachine");
            Component stateMachine =
                patchInstance.GetComponent(stateMachineType);
            Assert.NotNull(
                stateMachine,
                "The generated Patch 4 prefab has no gameplay state bridge.");
            MethodInfo setReviewActive = stateMachineType.GetMethod(
                "SetLockedReviewActive",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setWalkSpeed = stateMachineType.GetMethod(
                "SetWalkSpeed",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(setReviewActive);
            Assert.NotNull(setWalkSpeed);

            try
            {
                patchVisual.gameObject.SetActive(true);
                animator.Rebind();
                animator.Update(0f);
                Assert.IsTrue(
                    animator.HasState(0, stateHash),
                    "The runtime controller does not expose " + statePath + ".");

                // Exercise the same public gameplay-action route used by the
                // actual-room review. Direct Animator.Play alone cannot catch
                // a broken Idle -> Walk transition or a disabled review API.
                setReviewActive.Invoke(stateMachine, new object[] { true });
                animator.SetBool("Look", false);
                animator.SetBool("Shift", false);
                animator.SetBool("Turn", false);
                animator.SetBool("Sit", false);
                animator.SetFloat("Speed", 0f);
                animator.speed = 1f;
                animator.Play(idleStateHash, 0, 0f);
                animator.Update(0f);
                setWalkSpeed.Invoke(stateMachine, new object[] { 1f });
                for (int frame = 0; frame < 12; frame++)
                {
                    animator.Update(0.02f);
                }

                Assert.AreEqual(
                    stateHash,
                    animator.GetCurrentAnimatorStateInfo(0).fullPathHash,
                    "SetWalkSpeed must route Idle into the full-path Walk " +
                    "state before the room review begins sampling.");
                Assert.IsFalse(
                    animator.IsInTransition(0),
                    "The gameplay-routed Walk transition did not settle.");

                float walkTimeBeforeRepeatedTick =
                    animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                setWalkSpeed.Invoke(stateMachine, new object[] { 1f });
                animator.Update(0.02f);
                AnimatorStateInfo walkAfterRepeatedTick =
                    animator.GetCurrentAnimatorStateInfo(0);
                Assert.AreEqual(
                    stateHash,
                    walkAfterRepeatedTick.fullPathHash,
                    "A repeated movement tick left the Walk state.");
                Assert.Greater(
                    walkAfterRepeatedTick.normalizedTime,
                    walkTimeBeforeRepeatedTick,
                    "Repeated Speed = 1 ticks must not restart the walk " +
                    "cycle every gameplay frame.");

                animator.SetBool("Look", false);
                animator.SetBool("Shift", false);
                animator.SetBool("Turn", false);
                animator.SetBool("Sit", false);
                animator.SetFloat("Speed", 1f);
                animator.speed = 0f;
                animator.Play(stateHash, 0, 0f);
                animator.Update(0f);
                Assert.AreEqual(
                    stateHash,
                    animator.GetCurrentAnimatorStateInfo(0).fullPathHash,
                    "Animator.Play must enter the full-path walk state.");

                animator.Play(stateHash, 0, 0.5f);
                animator.Update(0f);
                Assert.AreEqual(
                    stateHash,
                    animator.GetCurrentAnimatorStateInfo(0).fullPathHash,
                    "The full-path walk state was lost before V23 sampling.");

                Type v23PresentationType = RequireType(
                    "SkinnyToBeast.Gameplay.Patch4." +
                    "Patch4V23FullFramePresentation");
                Component v23Presentation =
                    patchInstance.GetComponent(v23PresentationType);
                Assert.NotNull(v23Presentation);
                Assert.IsTrue(
                    GetBoolProperty(v23Presentation, "UsesContinuousLayeredRig"),
                    "Live animation must use one persistent layered character.");
                MethodInfo setReviewPose = v23PresentationType.GetMethod(
                    "SetReviewPose",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(setReviewPose);
                Assert.IsTrue(
                    (bool)setReviewPose.Invoke(
                        v23Presentation,
                        new object[] { "FatMan_Walk_InRoom", 0.5f }),
                    "The layered walk review surface could not be activated.");
                Assert.IsTrue(
                    GetBoolProperty(v23Presentation, "IsLayeredRigActive"));
                Assert.IsFalse(
                    GetBoolProperty(v23Presentation, "IsDisplaying"),
                    "A full-body atlas frame must never cover the live walk.");

                Component patchRig = patchInstance.GetComponent(
                    RequireType(
                        "SkinnyToBeast.Gameplay.Patch4." +
                        "Patch4CharacterRigController"));
                MethodInfo getBone = patchRig.GetType().GetMethod(
                    "GetBone",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.NotNull(getBone);
                Transform handL = (Transform)getBone.Invoke(
                    patchRig,
                    new object[] { "HandL" });
                Transform handR = (Transform)getBone.Invoke(
                    patchRig,
                    new object[] { "HandR" });
                Transform footL = (Transform)getBone.Invoke(
                    patchRig,
                    new object[] { "FootL" });
                Transform footR = (Transform)getBone.Invoke(
                    patchRig,
                    new object[] { "FootR" });
                Assert.NotNull(handL);
                Assert.NotNull(handR);
                Assert.NotNull(footL);
                Assert.NotNull(footR);

                Vector3 handLStart = handL.position;
                Vector3 handRStart = handR.position;
                Vector3 footLStart = footL.position;
                Vector3 footRStart = footR.position;
                animator.Play(stateHash, 0, 0.25f);
                animator.Update(0f);
                Assert.Greater(
                    Vector3.Distance(handLStart, handL.position),
                    0.01f,
                    "The left hand has no continuous walk trajectory.");
                Assert.Greater(
                    Vector3.Distance(handRStart, handR.position),
                    0.01f,
                    "The right hand has no continuous walk trajectory.");
                Assert.Greater(
                    Vector3.Distance(footLStart, footL.position) +
                    Vector3.Distance(footRStart, footR.position),
                    0.01f,
                    "The feet do not articulate during the walk cycle.");
            }
            finally
            {
                setWalkSpeed.Invoke(stateMachine, new object[] { 0f });
                setReviewActive.Invoke(stateMachine, new object[] { false });
                animator.SetFloat("Speed", 0f);
                animator.speed = 0f;
                if (animator.HasState(0, idleStateHash))
                {
                    animator.Play(idleStateHash, 0, 0f);
                    animator.Update(0f);
                }

                animator.speed = previousAnimatorSpeed;
                patchVisual.gameObject.SetActive(visualWasActive);
            }
        }

        private static void AssertRuntimeAnimatorContract(Animator animator)
        {
            Assert.NotNull(
                animator,
                "The generated Patch 4 prefab has no root Animator.");
            Assert.AreEqual(
                1,
                animator.GetComponentsInChildren<Animator>(true).Length,
                "The generated Patch 4 prefab has duplicate Animator owners.");
            Assert.IsTrue(
                animator.enabled,
                "The generated Patch 4 Animator is disabled.");
            Assert.NotNull(
                animator.runtimeAnimatorController,
                "The generated Patch 4 Animator has no runtime controller.");
            Assert.AreEqual(
                1,
                animator.layerCount,
                "Patch 4 must have one authoritative Animator layer.");
            Assert.AreEqual("Base Layer", animator.GetLayerName(0));

            string[] states =
            {
                "FatMan_Idle_Breathe",
                "FatMan_Idle_ShiftWeight",
                "FatMan_Blink_Random",
                "FatMan_LookAround",
                "FatMan_TapReact_01",
                "FatMan_TapReact_02",
                "FatMan_Walk_InRoom",
                "FatMan_Turn",
                "FatMan_SitOrLean",
                "FatMan_UpgradeReact"
            };
            for (int i = 0; i < states.Length; i++)
            {
                string path = "Base Layer." + states[i];
                Assert.IsTrue(
                    animator.HasState(0, Animator.StringToHash(path)),
                    "The runtime Animator is missing " + path + ".");
            }

            string[] parameters =
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
                parameters,
                animator.parameters.Select(parameter => parameter.name),
                "The runtime Animator is missing gameplay parameters.");
        }

        private static void AssertLegacyAnimatorContract(Animator animator)
        {
            Assert.NotNull(
                animator,
                "The rollback gameplay owner has no Animator.");
            Assert.IsTrue(
                animator.enabled,
                "The rollback gameplay Animator is disabled.");
            Assert.NotNull(
                animator.runtimeAnimatorController,
                "The rollback gameplay Animator has no runtime controller.");

            string[] requiredLayers =
            {
                "Base",
                "UpperBody",
                "Face",
                "FullBodyAction"
            };
            string[] actualLayers = Enumerable.Range(0, animator.layerCount)
                .Select(index => animator.GetLayerName(index))
                .ToArray();
            CollectionAssert.IsSubsetOf(
                requiredLayers,
                actualLayers,
                "The rollback gameplay Animator is missing required layers.");
        }

        private static Component BuildReadyLegacyGameplayOwner(
            GameObject root,
            out Component skin,
            out Animator animator)
        {
            Type rigType = RequireType(
                "SkinnyToBeast.Gameplay.CharacterRigController");
            Type faceType = RequireType(
                "SkinnyToBeast.Gameplay.CharacterFaceController");
            Type skinType = RequireType(
                "SkinnyToBeast.Gameplay.CharacterSkinController");

            Component rig = root.GetComponent(rigType);
            Component face = root.GetComponent(faceType);
            skin = root.GetComponent(skinType);
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            Assert.NotNull(rig);
            Assert.NotNull(face);
            Assert.NotNull(skin);
            Assert.NotNull(group);

            MethodInfo build = rigType.GetMethod(
                "Build",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo configureSkin = skinType.GetMethod(
                "Configure",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo applySkin = skinType.GetMethod(
                "ApplySkin",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(build);
            Assert.NotNull(configureSkin);
            Assert.NotNull(applySkin);
            build.Invoke(
                rig,
                new object[]
                {
                    root.GetComponent<RectTransform>(),
                    face
                });
            configureSkin.Invoke(
                skin,
                new object[] { rig, group, 4 });
            applySkin.Invoke(
                skin,
                new object[] { 3, false });
            animator = root.GetComponent<Animator>();
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

        private static void SetPrivateField(
            object target,
            string name,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, name);
            field.SetValue(target, value);
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
