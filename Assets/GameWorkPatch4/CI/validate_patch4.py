#!/usr/bin/env python3
"""Static safety checks for GameWork Patch 4.0.

This script intentionally does not pretend to compile Unity. It validates the
source-of-truth contract, protected paths, readiness SHA and JSON manifests on
any machine that has Python and git.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import struct
import subprocess
import sys
import zlib
from pathlib import Path
from typing import Iterable

EXPECTED_SHA = "7b151f1ded93f3852bc8a7218ab26f94298b7f822094304bbcea9c076cad72a3"
REPOSITORY_MASTER = (
    "Assets/GameWorkPatch4/Art/Character/FatMan/"
    "FatMan_NeutralFront_Master.png"
)
EXPECTED_COUNTS = {
    "RequiredBoneNames": 31,
    "RequiredLayerPaths": 40,
    "RuntimeNeutralLayerPaths": 1,
    "RuntimeRigidLayerPaths": 9,
    "RequiredClipNames": 10,
    "ProtectedPathFragments": 6,
}
PROTECTED_FRAGMENTS = (
    "MainMenuLoop.mp4",
    "/MainMenu/",
    "/Menu/",
    "/Music/",
    "/Audio/Mixers/",
    "/Settings/",
)
REQUIRED_FILES = (
    "Assets/GameWorkPatch4/Runtime/Patch4RigContract.cs",
    "Assets/GameWorkPatch4/Runtime/Patch4CharacterRigController.cs",
    "Assets/GameWorkPatch4/Runtime/Patch4ArtReadinessAsset.cs",
    "Assets/GameWorkPatch4/Runtime/Patch4CanvasPresentation.cs",
    "Assets/GameWorkPatch4/Runtime/Patch4CanvasSkinDeformer.cs",
    "Assets/GameWorkPatch4/Runtime/Patch4AnimationRoomReviewDriver.cs",
    "Assets/GameWorkPatch4/Runtime/Patch4V23FullFramePresentation.cs",
    "Assets/GameWorkPatch4/Runtime/Patch4RuntimeInstaller.cs",
    "Assets/GameWorkPatch4/Editor/Patch4ProductionPipeline.cs",
    "Assets/GameWorkPatch4/Editor/Patch4LayerImportPostprocessor.cs",
    "Assets/GameWorkPatch4/Editor/Patch4DraftLayerValidator.cs",
    "Assets/GameWorkPatch4/Editor/Patch4NeutralPoseValidator.cs",
    "Assets/GameWorkPatch4/Editor/Patch4NeutralPoseReviewWindow.cs",
    "Assets/GameWorkPatch4/Editor/Patch4FacePoseReviewWindow.cs",
    "Assets/GameWorkPatch4/Editor/Patch4AnimationRoomReview.cs",
    "Assets/GameWorkPatch4/Editor/Patch4AnimationRoomReviewWindow.cs",
    "Assets/GameWorkPatch4/Editor/Patch4AnimationLibraryBuilder.cs",
    "Assets/GameWorkPatch4/Editor/Patch4AnimatorControllerSanitizer.cs",
    "Assets/GameWorkPatch4/Editor/Patch4EditorSmokeValidator.cs",
    "Assets/GameWorkPatch4/Editor/Patch4PrefabReadinessBinder.cs",
    REPOSITORY_MASTER,
    "Assets/GameWorkPatch4/Art/Character/FatMan/V23FullFrame/"
    "FatMan_Idle_V23.png",
    "Assets/GameWorkPatch4/Art/Character/FatMan/V23FullFrame/"
    "FatMan_Face_V23.png",
    "Assets/GameWorkPatch4/Art/Character/FatMan/V23FullFrame/"
    "FatMan_Tap_V23.png",
    "Assets/GameWorkPatch4/Art/Character/FatMan/V23FullFrame/"
    "FatMan_Pose_V23.png",
    "Assets/GameWorkPatch4/Art/Character/FatMan/V23FullFrame/"
    "FatMan_Upgrade_V23.png",
    "Assets/GameWorkPatch4/Art/Character/FatMan/V23FullFrame/"
    "FatMan_WalkRight_V23.png",
    "Assets/GameWorkPatch4/Art/Character/FatMan/V24Corrections/"
    "FatMan_Upgrade_V24.png",
    "Assets/GameWorkPatch4/Art/Character/FatMan/master-source.json",
    "Assets/GameWorkPatch4/Art/Character/FatMan/Masks/adobe-mask-manifest.json",
    "Docs/Patch4/CHECKPOINT.md",
    "Docs/Patch4/CURRENT_HANDOFF.md",
)


def fail(errors: list[str], message: str) -> None:
    errors.append(message)


def read_text(root: Path, relative: str, errors: list[str]) -> str:
    path = root / relative
    if not path.is_file():
        fail(errors, f"Missing required file: {relative}")
        return ""
    return path.read_text(encoding="utf-8")


def extract_contract_values(source: str, property_name: str) -> list[str]:
    pattern = re.compile(
        rf"{re.escape(property_name)}\s*\{{\s*get;\s*\}}\s*=.*?new\[\]\s*\{{(.*?)\}}\s*\)\s*;",
        re.DOTALL,
    )
    match = pattern.search(source)
    if not match:
        return []
    return re.findall(r'"([^"\\]*(?:\\.[^"\\]*)*)"', match.group(1))


def validate_contract(root: Path, errors: list[str]) -> None:
    source = read_text(
        root,
        "Assets/GameWorkPatch4/Runtime/Patch4RigContract.cs",
        errors,
    )
    if not source:
        return

    values_by_property: dict[str, list[str]] = {}
    for property_name, expected_count in EXPECTED_COUNTS.items():
        values = extract_contract_values(source, property_name)
        values_by_property[property_name] = values
        if len(values) != expected_count:
            fail(
                errors,
                f"{property_name}: expected {expected_count} values, found {len(values)}",
            )
        if len(values) != len(set(values)):
            fail(errors, f"{property_name}: duplicate values detected")

    bones = values_by_property.get("RequiredBoneNames", [])
    layers = values_by_property.get("RequiredLayerPaths", [])
    clips = values_by_property.get("RequiredClipNames", [])
    neutral_layers = values_by_property.get("RuntimeNeutralLayerPaths", [])
    if neutral_layers != ["Body/TorsoBase"]:
        fail(
            errors,
            "Runtime neutral stack must preserve the one exact intact master body",
        )
    forbidden_runtime_cutouts = {
        "Head/HeadBase",
        "ArmL/Upper",
        "ArmR/Upper",
        "LegL/Thigh",
        "LegR/Thigh",
        "Clothes/ShirtBase",
    }
    if forbidden_runtime_cutouts.intersection(neutral_layers):
        fail(errors, "Hidden anatomical reference cutouts are visible at runtime")

    for required in ("Root", "CharacterRoot", "Pelvis", "BellyTip", "Head", "GroundShadow"):
        if required not in bones:
            fail(errors, f"Required bone missing from contract: {required}")

    for required in (
        "Body/TorsoBase",
        "Face/EyeWhiteL",
        "Face/MouthOpen",
        "ArmL/Upper",
        "LegR/Foot",
        "Clothes/ShirtBellyOverlay",
        "FX/Shadow",
    ):
        if required not in layers:
            fail(errors, f"Required layer missing from contract: {required}")

    for required in (
        "FatMan_Idle_Breathe",
        "FatMan_TapReact_01",
        "FatMan_Walk_InRoom",
        "FatMan_UpgradeReact",
    ):
        if required not in clips:
            fail(errors, f"Required animation missing from contract: {required}")


def validate_json_files(root: Path, errors: list[str]) -> None:
    master_path = root / "Assets/GameWorkPatch4/Art/Character/FatMan/master-source.json"
    mask_path = root / "Assets/GameWorkPatch4/Art/Character/FatMan/Masks/adobe-mask-manifest.json"

    try:
        master = json.loads(master_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        fail(errors, f"master-source.json is unreadable: {exc}")
        master = {}

    try:
        masks = json.loads(mask_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        fail(errors, f"adobe-mask-manifest.json is unreadable: {exc}")
        masks = {}

    if master:
        raster = master.get("raster", {})
        if raster.get("sha256") != EXPECTED_SHA:
            fail(errors, "master-source.json SHA-256 does not match the approved master")
        if master.get("runtimeReady") is not False:
            fail(errors, "master-source.json runtimeReady must remain false before manual approval")
        if raster.get("width") != 1024 or raster.get("height") != 1536:
            fail(errors, "master-source.json must describe a 1024 x 1536 source")
        if raster.get("transparentBackground") is not True:
            fail(errors, "master-source.json must require a transparent background")
        if raster.get("repositoryPath") != REPOSITORY_MASTER:
            fail(errors, "master-source.json does not point at the exact repository master")

    if masks:
        if masks.get("approvedMasterSha256") != EXPECTED_SHA:
            fail(errors, "Adobe mask manifest does not target the approved master SHA")
        entries = masks.get("masks", [])
        if not isinstance(entries, list) or len(entries) < 10:
            fail(errors, "Adobe mask manifest must contain the recorded mask attempts")
        valid = [item for item in entries if isinstance(item, dict) and item.get("valid") is True]
        invalid = [item for item in entries if isinstance(item, dict) and item.get("valid") is False]
        if len(valid) < 8:
            fail(errors, "Adobe mask manifest has too few validated production selections")
        if not invalid:
            fail(errors, "Adobe mask manifest must preserve failed detections as invalid")

    draft_status = root / "Assets/GameWorkPatch4/Art/Character/FatMan/layer-draft-status.json"
    if draft_status.is_file():
        try:
            status = json.loads(draft_status.read_text(encoding="utf-8"))
            if status.get("activationAllowed") is not False:
                fail(errors, "Draft layer status must never allow runtime activation")
        except json.JSONDecodeError as exc:
            fail(errors, f"layer-draft-status.json is invalid JSON: {exc}")


def validate_repository_master(root: Path, errors: list[str]) -> None:
    path = root / REPOSITORY_MASTER
    try:
        data = path.read_bytes()
    except OSError as exc:
        fail(errors, f"Repository master is unreadable: {exc}")
        return

    actual_sha = hashlib.sha256(data).hexdigest()
    if actual_sha != EXPECTED_SHA:
        fail(
            errors,
            "Repository master SHA-256 does not match the quality master: "
            f"{actual_sha}",
        )

    if (
        len(data) < 29
        or data[:8] != b"\x89PNG\r\n\x1a\n"
        or data[12:16] != b"IHDR"
    ):
        fail(errors, "Repository master is not a valid PNG with an IHDR header")
        return

    width, height = struct.unpack(">II", data[16:24])
    if (width, height) != (1024, 1536):
        fail(
            errors,
            f"Repository master is {width} x {height}; expected 1024 x 1536",
        )
    if data[24] != 8 or data[25] != 6:
        fail(errors, "Repository master must be 8-bit RGBA PNG data")


def validate_repository_restore_pipeline(root: Path, errors: list[str]) -> None:
    downloader = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4AdobeMaskDownloader.cs",
        errors,
    )
    if downloader:
        required_snippets = (
            "FatMan_NeutralFront_Master.png",
            EXPECTED_SHA,
            "ReadAndValidateRepositoryMaster()",
            "SHA256.Create()",
            "WriteBytes(",
        )
        for snippet in required_snippets:
            if snippet not in downloader:
                fail(errors, f"Repository restore pipeline is missing: {snippet}")
        if "Patch4EmbeddedArtSource" in downloader:
            fail(errors, "Repository restore must not use the former 96 x 144 preview")
        if "private static Texture2D Resize(" in downloader:
            fail(errors, "Repository restore must not upscale a compact preview")

    automatic = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4AutoContinuation.cs",
        errors,
    )
    if automatic:
        ordered_steps = (
            '"test-runner-playmode-ownership-v26"',
            "RestoreRepositorySources()",
            "BakeDraftLayers()",
            "RebuildRuntimeAssets()",
            "RunSafetyValidation()",
            "RunAll()",
        )
        last_index = -1
        for snippet in ordered_steps:
            index = automatic.find(snippet)
            if index < 0:
                fail(errors, f"Automatic quality pass is missing: {snippet}")
                continue
            if index <= last_index:
                fail(errors, f"Automatic quality pass is out of order at: {snippet}")
            last_index = index


def validate_readiness_gate(root: Path, errors: list[str]) -> None:
    controller = read_text(
        root,
        "Assets/GameWorkPatch4/Runtime/Patch4CharacterRigController.cs",
        errors,
    )
    if controller:
        required_snippets = (
            EXPECTED_SHA,
            "artReadiness.IsApprovedFor(expectedSourceSha256)",
            "patch4Enabled && rigValid && IsArtApproved",
            "Patch 4 activation rejected",
        )
        for snippet in required_snippets:
            if snippet not in controller:
                fail(errors, f"Rig controller readiness gate is missing: {snippet}")

    automatic_files = (
        "Assets/GameWorkPatch4/Editor/Patch4ArtReadinessAssetBuilder.cs",
        "Assets/GameWorkPatch4/Editor/Patch4PrefabReadinessBinder.cs",
        "Assets/GameWorkPatch4/Editor/Patch4ProductionPipeline.cs",
        "Assets/GameWorkPatch4/Editor/Patch4NeutralPoseValidator.cs",
        "Assets/GameWorkPatch4/Editor/Patch4NeutralPoseReviewWindow.cs",
        "Assets/GameWorkPatch4/Editor/Patch4FacePoseReviewWindow.cs",
        "Assets/GameWorkPatch4/Editor/Patch4AnimationRoomReview.cs",
        "Assets/GameWorkPatch4/Editor/Patch4AnimationRoomReviewWindow.cs",
        "Assets/GameWorkPatch4/Runtime/Patch4AnimationRoomReviewDriver.cs",
    )
    dangerous = re.compile(r"productionArtApproved[^\n]{0,100}(?:=|boolValue\s*=)\s*true", re.IGNORECASE)
    for relative in automatic_files:
        source = read_text(root, relative, errors)
        if source and dangerous.search(source):
            fail(errors, f"Automatic tool may approve production art: {relative}")


def validate_runtime_installation(root: Path, errors: list[str]) -> None:
    installer = read_text(
        root,
        "Assets/GameWorkPatch4/Runtime/Patch4RuntimeInstaller.cs",
        errors,
    )
    if installer:
        required_snippets = (
            'PrefabResourcePath = "FatMan_Patch4"',
            'GameplayRoomName = "LivingGameplayScene"',
            "legacyRig.VisualRoot",
            "patchRig.BindRollbackRoot(rollbackRoot)",
            "visibility.BindRollbackRoot(rollbackRoot)",
            "bridge.BindLegacy(legacyRig, legacySkin)",
            "canvasPresentation.ConfigureForGameplayRoom(",
            "patchRig.SetPatch4Enabled(false)",
        )
        for snippet in required_snippets:
            if snippet not in installer:
                fail(errors, f"Runtime rollback installer is missing: {snippet}")

        if "patchRig.SetPatch4Enabled(true)" in installer:
            fail(errors, "Runtime installer must never enable Patch 4 automatically")

    signal_bridge = read_text(
        root,
        "Assets/GameWorkPatch4/Runtime/Patch4LegacySignalBridge.cs",
        errors,
    )
    if signal_bridge:
        for snippet in (
            "stateMachine.SetWalkSpeed(moving ? 1f : 0f)",
            "stateMachine.SetLooking(looking)",
            "stateMachine.SetShiftingWeight(shifting)",
            "stateMachine.SetSittingOrLeaning(sitting)",
            "stateMachine.PlayBlink()",
            "upgradeManager.UpgradesChanged += OnUpgradePurchased",
            "stateMachine.PlayUpgradeReaction()",
        ):
            if snippet not in signal_bridge:
                fail(errors, f"Gameplay action routing is missing: {snippet}")

    presentation = read_text(
        root,
        "Assets/GameWorkPatch4/Runtime/Patch4CanvasPresentation.cs",
        errors,
    )
    if presentation:
        required_snippets = (
            "using UnityEngine.UI;",
            "typeof(Image)",
            "image.raycastTarget = false",
            "sourceCanvasPixels",
            "new(1024f, 1536f)",
            "legacyPresentationScale = 0.74f",
            "ConfigureForGameplayRoom(",
            "DisableFallbackSpriteRenderers()",
            "Patch4CanvasSkinDeformer",
            "CaptureSkinBindPoses()",
            "SkinBindingsReady",
            "BindAnchorsFrozen",
            "AlignLayerAnchorsToBindPose()",
            "image.useSpriteMesh = false",
            "ResolveSkinProfile(",
            "ContinuousBodyBindingReady",
            "RuntimeRigidBindingsReady",
            "Patch4RigContract.IsRuntimeContinuousBodyLayer",
            "Patch4RigContract.IsRuntimeLayerVisibleByDefault",
            "faceController.BindPresentationLayers(",
            "deformer.ExpectedVertexCount >= 13000",
        )
        for snippet in required_snippets:
            if snippet not in presentation:
                fail(errors, f"Canvas presentation is missing: {snippet}")

        if "SetPatch4Enabled(" in presentation:
            fail(errors, "Canvas presentation must never change Patch 4 activation")
        if "SyncLayerTransforms" in presentation:
            fail(
                errors,
                "Canvas layer anchors must not chase live bones after their "
                "bind poses are captured",
            )

    builder = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4PrefabBuilder.cs",
        errors,
    )
    if builder:
        if 'PrefabRoot = "Assets/GameWorkPatch4/Resources"' not in builder:
            fail(errors, "Patch 4 prefab must be generated into isolated Resources")
        for snippet in (
            "Patch4V23FullFramePresentation",
            "FatMan_Idle_V23.png",
            "FatMan_Face_V23.png",
            "FatMan_Tap_V23.png",
            "FatMan_Pose_V23.png",
            "FatMan_Upgrade_V24.png",
            "FatMan_WalkRight_V23.png",
            "v23Presentation.RebuildPresentation()",
        ):
            if snippet not in builder:
                fail(errors, f"V23 ten-state frame builder is missing: {snippet}")

    v23_presentation = read_text(
        root,
        "Assets/GameWorkPatch4/Runtime/Patch4V23FullFramePresentation.cs",
        errors,
    )
    if v23_presentation:
        for snippet in (
            "RequiredStateCount = 10",
            "RequiredWalkFrameCount = 8",
            "typeof(RawImage)",
            "presentationImage.uvRect",
            "generatedLayersGroup.alpha = 0f",
            "rigController.Patch4Enabled",
            "FatMan_Walk_InRoom",
            "SetReviewPose(",
            "SetReviewActive(",
            "TryMeasureGaitArticulation(",
            "TryMeasureFaceArticulation(",
            "HasSingleVisibleCompleteFrame",
            "LegacyUnderlayHidden",
            "VisibleAlphaThreshold",
            "ResolvePlaybackDuration(",
            "TryMeasureFrameCalibration(",
            "ApplyFrameCalibration(",
            "TargetGroundPixel",
            "ResolveArtworkScale(",
        ):
            if snippet not in v23_presentation:
                fail(errors, f"V23 full-frame presentation is missing: {snippet}")
        if "SetPatch4Enabled(true)" in v23_presentation:
            fail(errors, "V23 presentation must never unlock Patch 4")

    deformer = read_text(
        root,
        "Assets/GameWorkPatch4/Runtime/Patch4CanvasSkinDeformer.cs",
        errors,
    )
    if deformer:
        required_snippets = (
            "class Patch4CanvasSkinDeformer : BaseMeshEffect",
            "ResolveFullCanvasUv(sprite)",
            "sprite.rect",
            "sprite.texture.width",
            "UsesFullCanvasUv",
            "vertexHelper.AddTriangle",
            "bone.worldToLocalMatrix",
            "imageTransform.worldToLocalMatrix",
            "HasMultipleBoneWeights",
            "UsesContinuousBodyWeights",
            "DeformContinuousBody(",
            "DeformArm(",
            "DeformLeg(",
            "ArmCenterX(",
            "ArmRadius(",
            "ArmTorsoBoundary(",
            "LegCenterX(",
            "LegRadius(",
            "Range(1, 96)",
            "Range(1, 144)",
            "Mathf.Clamp(columns, 1, 96)",
            "Mathf.Clamp(rows, 1, 144)",
            "IsRigidlyBound",
            "PrimaryBoneName",
            "CaptureBindPose()",
        )
        for snippet in required_snippets:
            if snippet not in deformer:
                fail(errors, f"Canvas skin deformer is missing: {snippet}")
        if "SetPatch4Enabled(" in deformer:
            fail(errors, "Canvas skin deformer must never change Patch 4 activation")
        if "float horizontalInfluence" in deformer:
            fail(
                errors,
                "The continuous body still applies broad horizontal strips "
                "that pull shirt pixels as arms or legs",
            )
        if "DataUtility.GetOuterUV" in deformer:
            fail(
                errors,
                "Canvas skin deformer must not expand a Tight opaque UV crop "
                "over the full layer canvas",
            )
        late_update_start = deformer.find("private void LateUpdate()")
        deform_vertex_start = deformer.find(
            "private Vector3 DeformVertex(",
            late_update_start,
        )
        late_update = (
            deformer[late_update_start:deform_vertex_start]
            if late_update_start >= 0 and deform_vertex_start > late_update_start
            else ""
        )
        if "graphic.SetVerticesDirty()" not in late_update:
            fail(
                errors,
                "Canvas deformation is not refreshed on every animated frame",
            )
        if "HasMultipleBoneWeights" in late_update:
            fail(
                errors,
                "Canvas refresh still excludes active rigid face replacements",
            )

    importer = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4LayerImportPostprocessor.cs",
        errors,
    )
    if importer and "spriteMeshType = SpriteMeshType.FullRect" not in importer:
        fail(errors, "Patch 4 layer imports must force FullRect sprite meshes")

    fallback_renderer = read_text(
        root,
        "Assets/GameWorkPatch4/Runtime/Patch4LayerRenderer.cs",
        errors,
    )
    if fallback_renderer:
        for snippet in (
            "Patch4RigContract.IsRuntimeLayerVisibleByDefault",
            "Patch4RigContract.RequiredLayerPaths.Count",
            "layerObject.SetActive(",
        ):
            if snippet not in fallback_renderer:
                fail(
                    errors,
                    "Fallback layer renderer does not preserve all 40 layers "
                    "with canonical visibility: " + snippet,
                )

    review_driver = read_text(
        root,
        "Assets/GameWorkPatch4/Runtime/Patch4AnimationRoomReviewDriver.cs",
        errors,
    )
    if review_driver:
        if not review_driver.lstrip().startswith("#if UNITY_EDITOR"):
            fail(errors, "Locked animation-room driver must be Editor-only")
        for snippet in (
            "Patch4RigContract.RequiredClipNames",
            "ScreenCapture.CaptureScreenshotAsTexture",
            "humanReviewRequired = true",
            "activationAllowed = false",
            "rollbackReviewGroup.alpha = 0f",
            "patch35RollbackRoot.SetActive(true)",
            "CaptureReviewBackground()",
            "AnalyzeRoomSilhouette(",
            "AnalyzeVisibleMotion(",
            "AnalyzeFocusedFaceMotion(",
            "AnalyzeWalkLimbMotion(",
            "MeasureAlignedRegionMotion(",
            "ResolveGlobalAlignment(",
            "TryMeasureForegroundCentroid(",
            "minimumMotionCoverage",
            "minimumFaceMotionCoverage",
            "minimumLimbMotionCoverage",
            "focusedFaceMotionPassed",
            "limbArticulationPassed",
            "allLimbRegionsPassed",
            "leftArmMotionCoverage",
            "rightArmMotionCoverage",
            "leftLegMotionCoverage",
            "rightLegMotionCoverage",
            "MinimumV23WalkArmSilhouetteDifference",
            "MinimumV23WalkLegSilhouetteDifference",
            "MinimumV23AdjacentFrameDifference",
            "MinimumV23FaceDifference",
            "IsForeground(",
            "neutralWidthRetention",
            "MaximumNeutralWidthExpansion",
            "MaximumNeutralWidthExpansion = 1.16f",
            "MaximumNeutralHeightExpansion = 1.12f",
            "MaximumNeutralAreaExpansion = 1.20f",
            "MinimumWalkArmMotionCoverage",
            "MinimumWalkLegMotionCoverage",
            "visualSanityPassed",
            "visibleMotionPassed",
            "animatorStateBindingPassed",
            "WalkPhaseCount = 8",
            "WalkCycleFileName",
            "ReviewWalkCycle(",
            "CaptureWalkPhaseFrame(",
            "SetWalkReviewTravel(",
            "walkCycleCaptured",
            "walkRootTravelPassed",
            "walkPhaseAlternationPassed",
            "Patch4V23FullFramePresentation",
            "v23FullFramePresentation.SetReviewPose(",
            "v23LeftArmSilhouetteDifference",
            "v23RightLegSilhouetteDifference",
            "v23MinimumAdjacentFrameDifference",
            "v23TenStateFullFrameReady",
            "v23FaceArticulationReady",
            "singleCompleteFramePassed",
            'animator.GetLayerName(0) + "." + clipName',
            "Animator.StringToHash(",
            "animator.HasState(",
            "GetCurrentAnimatorStateInfo(0)",
            "ConfigureAnimatorParametersForClip(",
            "PlayVerifiedAnimatorState(",
            "legacyRoutine.enabled = false",
            "legacySignalBridge.enabled = false",
            "source.Stop()",
            "runToken = reviewRunToken",
            "string.IsNullOrWhiteSpace(reviewRunToken)",
            "Application.logMessageReceived",
            "reviewConsoleErrorCount == 0",
            "SetEditorReviewActive(true)",
            "SetEditorReviewActive(false)",
            "RunUninterruptedGameplayPreview(",
            "liveGameplayPreviewCompleted",
            "liveGameplayPreviewFrameAdvances",
            "runtimeFrameCalibrationReady",
            "MinimumLivePreviewFrameAdvances",
            "gameplayActionRoutingPassed",
            "RouteGameplayActionToState(",
            "RequestGameplayActionForClip(",
            "stateMachine.SetLockedReviewActive(true)",
        ):
            if snippet not in review_driver:
                fail(errors, f"Locked animation-room driver is missing: {snippet}")
        if "patch35RollbackRoot.SetActive(false)" in review_driver:
            fail(
                errors,
                "Room review must keep the rollback rig logically active while "
                "hiding it with CanvasGroup",
            )
        if "SetPatch4Enabled(true)" in review_driver:
            fail(errors, "Locked room review must never pass the production gate")
        if "animator.Play(clip.name" in review_driver:
            fail(
                errors,
                "Room review must address Animator states by verified full-path hash",
            )

    animation_builder = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4AnimationLibraryBuilder.cs",
        errors,
    )
    if animation_builder:
        for snippet in (
            "SetAlternatingRotation(",
            "SetReactionRotation(",
            "SetCyclePosition(",
            "SetFourPhaseRotation(",
            "AddFloatTransition(",
            'controller.AddParameter("Shift"',
            'controller.AddParameter("Blink"',
            "AddExitToContext(",
            ".ResolvePlaybackDuration(clip.name)",
            "AnimatorControllerLayer layer = controller.layers[0];",
            "machine.name = layer.name;",
            "HandL",
            "HandR",
            '"FatMan_Turn"',
            "new Keyframe(0.2f, 0.94f)",
        ):
            if snippet not in animation_builder:
                fail(
                    errors,
                    "Patch 4 animation library is missing the corrected " +
                    "visible-motion contract: " +
                    snippet,
                )
        if "new Keyframe(0.18f, 0.12f)" in animation_builder:
            fail(
                errors,
                "FatMan_Turn must never collapse the character to a near-zero "
                "horizontal scale",
            )
        if 'AddBoolTransition(idle, walk, "Speed"' in animation_builder:
            fail(
                errors,
                "The float Speed parameter must use a float transition, not a "
                "bool transition",
            )
        if 'machine.name = "Patch 4 Locomotion"' in animation_builder:
            fail(
                errors,
                "The root Animator state-machine name must match its layer; "
                "a private hard-coded name breaks full-path state hashes",
            )
        if "EyeL" in animation_builder or "EyeR" in animation_builder:
            fail(
                errors,
                "Animation clips must not transform Eye bones; painted face "
                "replacement layers stay rigidly attached to Head",
            )
        if re.search(
            r'SetCurve\(\s*clip,\s*Head,\s*"m_LocalPosition\.',
            animation_builder,
        ):
            fail(
                errors,
                "Animation clips must not translate Head independently; the "
                "continuous body and sparse face replacements share its matrix",
            )
        walk_start = animation_builder.find(
            "private static AnimationClip BuildWalk()")
        walk_end = animation_builder.find(
            "private static AnimationClip BuildTurn()")
        if walk_start < 0 or walk_end <= walk_start:
            fail(errors, "Patch 4 animation library is missing BuildWalk")
        else:
            walk_source = animation_builder[walk_start:walk_end]
            if re.search(
                r'SetCurve\(\s*clip,\s*Visual,\s*"m_LocalPosition\.x"',
                walk_source,
            ):
                fail(
                    errors,
                    "FatMan_Walk_InRoom must articulate limbs instead of "
                    "moving the complete body sideways",
                )

    animator_sanitizer = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4AnimatorControllerSanitizer.cs",
        errors,
    )
    if animator_sanitizer:
        for snippet in (
            "AnimatorControllerLayer layer = controller.layers[0];",
            "machine.name = layer.name;",
            "StringComparison.Ordinal",
        ):
            if snippet not in animator_sanitizer:
                fail(
                    errors,
                    "Animator Controller sanitizer does not repair canonical "
                    "root state paths: " + snippet,
                )

    smoke_validator = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4EditorSmokeValidator.cs",
        errors,
    )
    if smoke_validator:
        for snippet in (
            "ANIMATOR_STATE_PATH_MISMATCH",
            "ANIMATOR_STATES_INCOMPLETE",
            "machine.name",
            "layer.name",
        ):
            if snippet not in smoke_validator:
                fail(
                    errors,
                    "Editor smoke validation does not enforce canonical "
                    "Animator state paths: " + snippet,
                )

    contract_tests = read_text(
        root,
        "Assets/GameWorkPatch4/Tests/EditMode/Patch4ContractEditModeTests.cs",
        errors,
    )
    if contract_tests:
        for snippet in (
            "AssertWalkClipHasArticulatedGait()",
            "FatMan_Walk_InRoom.anim",
            '"m_LocalPosition.x"',
            "RequireCurve(",
            "AssertAnimatorControllerHasCanonicalRootStatePaths(clips)",
            "AssertV23FullFrameSheetsAreImportable()",
            "FatMan_WalkRight_V23.png",
            "AssertV24UpgradeCorrectionIsImportable()",
            "GetRawConstantValue()",
            "AssertWholeFramePlaybackCadence()",
            "AssertAnimatorControllerRoutesGameplayActions()",
            "layer.name",
            "machine.name",
        ):
            if snippet not in contract_tests:
                fail(
                    errors,
                    "EditMode gait regression coverage is missing: " + snippet,
                )
        if re.search(
            r"SkinnyToBeast\.Gameplay\.Patch4\.Editor\s*\.\s*"
            r"Patch4PrefabBuilder",
            contract_tests,
        ):
            fail(
                errors,
                "EditMode tests must resolve Patch4PrefabBuilder through "
                "reflection instead of crossing the isolated asmdef boundary",
            )

    room_review = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4AnimationRoomReview.cs",
        errors,
    )
    if room_review:
        for snippet in (
            "GameplayWindowController.Show()",
            "Patch4RuntimeInstaller.InstallAvailableGameplayRigs()",
            "Patch4AnimationRoomReviewDriver",
            "StartAfterTests()",
            "WaitingForEditModeStage",
            "WaitingForEditorQuiescenceStage",
            "WaitForEditorQuiescence",
            "quiescentUpdateCount < 30",
            "stableSeconds < 1.25d",
            "QueueEnterPlayMode()",
            "ClearPreviousReviewArtifacts()",
            "CurrentRunToken",
            "HasFreshRoomArtifacts()",
            "hasFreshRoomArtifacts",
            "Patch4AnimationRoomReviewWindow.Open()",
            "WalkCyclePath",
            "PrepareForAutomatedTests()",
            "Patch4AutomatedTestRunner.IsRunInProgress",
            "ClearReviewOwnership()",
            "blocked a stale room-review request",
        ):
            if snippet not in room_review:
                fail(errors, f"Actual-room review automation is missing: {snippet}")
        if "SetPatch4Enabled(true)" in room_review:
            fail(errors, "Actual-room review automation must not enable Patch 4")

    review_window = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4AnimationRoomReviewWindow.cs",
        errors,
    )
    if review_window:
        for snippet in (
            "LoadReviewStatus(",
            "reviewStatus.runToken",
            "Patch4AnimationRoomReview.CurrentRunToken",
            "passedTechnicalChecks",
            "No fresh completed animation-room report is available",
            "An older contact sheet is deliberately blocked",
            "Patch4AnimationRoomReview.WalkCyclePath",
            "DrawWalkLabels(",
            "liveGameplayPreviewDurationSeconds",
            "liveGameplayPreviewFrameAdvances",
            "gameplayActionRoutingPassed",
            "timing from that live pass, not from this deliberately",
        ):
            if snippet not in review_window:
                fail(
                    errors,
                    "Animation review window can still mislabel stale "
                    "artifacts as current: " + snippet,
                )

    automated_tests = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4AutomatedTestRunner.cs",
        errors,
    )
    if automated_tests:
        if "Patch4AnimationRoomReview.StartAfterTests()" not in automated_tests:
            fail(errors, "Passing 4/4 tests do not start the locked room review")
        for snippet in (
            "CollectFailedLeafResults(",
            "failedTests",
            "First failure:",
        ):
            if snippet not in automated_tests:
                fail(
                    errors,
                    "Automated test reporting still hides child failures: "
                    + snippet,
                )
        for snippet in (
            "IsRunInProgress",
            "Patch4AnimationRoomReview.PrepareForAutomatedTests()",
            "LivingGameplayAnimatorAssetBuilder.EnsureCurrentAssets()",
            "LegacyAnimatorResumePlayKey",
        ):
            if snippet not in automated_tests:
                fail(
                    errors,
                    "Automated tests do not exclusively own PlayMode: " +
                    snippet,
                )

    playmode_tests = read_text(
        root,
        "Assets/GameWorkPatch4/Tests/PlayMode/"
        "Patch4RuntimeInstallationPlayModeTests.cs",
        errors,
    )
    if playmode_tests and "sprite.vertices.Length" in playmode_tests:
        fail(
            errors,
            "PlayMode must validate the custom Canvas grid contract, not "
            "Unity's internal source Sprite vertex-array cardinality",
        )
    if playmode_tests:
        for snippet in (
            "AssertWalkAnimatorStateProducesArticulation(",
            "animator.HasState(0, stateHash)",
            "GetCurrentAnimatorStateInfo(0).fullPathHash",
            "MinimumV23ArmSilhouetteDifference",
            "MinimumV23LegSilhouetteDifference",
            "MinimumV23AdjacentFrameDifference",
            "MinimumV23FaceDifference",
            "animator.Play(stateHash, 0, 0.5f)",
            "TryMeasureGaitArticulation",
            "Patch4V23FullFramePresentation",
            "GetBoolProperty(v23Presentation, \"IsReady\")",
            "GetIntProperty(v23Presentation, \"StateCount\")",
            "TryMeasureFaceArticulation",
            "TryMeasureFrameCalibration",
            "FrameCalibrationReady",
        ):
            if snippet not in playmode_tests:
                fail(
                    errors,
                    "PlayMode full-path gait regression coverage is missing: "
                    + snippet,
                )

        neutral_master_fields = (
            "eyeWhiteLeft",
            "eyeWhiteRight",
            "irisLeft",
            "irisRight",
            "mouthClosed",
        )
        for field_name in neutral_master_fields:
            null_contract = re.search(
                r'Assert\.IsNull\(\s*GetPrivateField\(patchFace,\s*"'
                + re.escape(field_name)
                + r'"\)',
                playmode_tests,
            )
            if not null_contract:
                fail(
                    errors,
                    "PlayMode still expects a separate neutral face object: "
                    + field_name,
                )

        for field_name in ("lidLeft", "lidRight", "mouthOpen", "mouthSmile"):
            replacement_contract = re.search(
                r'Assert\.NotNull\(\s*GetPrivateField\(patchFace,\s*"'
                + re.escape(field_name)
                + r'"\)',
                playmode_tests,
            )
            if not replacement_contract:
                fail(
                    errors,
                    "PlayMode does not require the feathered face replacement: "
                    + field_name,
                )


def validate_neutral_pose_qa(root: Path, errors: list[str]) -> None:
    validator = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4NeutralPoseValidator.cs",
        errors,
    )
    if validator:
        required_snippets = (
            "humanReviewRequired = true",
            "report.activationAllowed = false",
            "Patch4RigContract.IsRuntimeNeutralLayer",
            "patch4-neutral-pose-review.png",
            "patch4-face-pose-review.png",
            "report.facePosePreviewCreated = true",
            "report.facePoseUsesReplacementComposition",
            "report.faceReplacementLayersClean",
            "report.faceTransitionLayersFeathered",
            "BuildReplacementPoseComposite(",
            "ValidateFaceReplacementLayerCrops(",
            "ValidateFaceTransitionLayerCrops(",
        )
        for snippet in required_snippets:
            if snippet not in validator:
                fail(errors, f"Neutral-pose QA is missing: {snippet}")

        if "SetPatch4Enabled(" in validator:
            fail(errors, "Neutral-pose QA must never change Patch 4 activation")

    presentation = read_text(
        root,
        "Assets/GameWorkPatch4/Runtime/Patch4CanvasPresentation.cs",
        errors,
    )
    if presentation:
        if "Patch4RigContract.IsRuntimeLayerVisibleByDefault" not in presentation:
            fail(errors, "Canvas presentation does not enforce canonical visibility")

    pipeline = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4ProductionPipeline.cs",
        errors,
    )
    if pipeline and "Patch4NeutralPoseValidator.ValidateAndWriteReport();" not in pipeline:
        fail(errors, "Safety pipeline does not run neutral-pose QA")

    baker = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4MaskDrivenLayerBaker.cs",
        errors,
    )
    if baker:
        for required in (
            "PaintJointContinuation(",
            "PaintSkinUnderlay(",
            "ExtractMasterFeature(",
            "FeatherClearFeature(",
            "SolveSkinInpaint(",
            "PaintClosedLid(",
            "PaintOpenMouth(",
            "PaintSmile(",
            "CopyFeatheredMasterPatch(",
            "Patch4RigContract.IsRuntimeContinuousBodyLayer(spec.path)",
            "(Color32[])master.pixels.Clone()",
        ):
            if required not in baker:
                fail(errors, f"Joint/face candidate baker is missing: {required}")
        if "PaintJointScaffold(" in baker:
            fail(errors, "Legacy five-pixel joint scaffolding is still present")
        if "overlayOnly: true" in baker:
            fail(errors, "Alternate facial layers still contain opaque backing patches")
        if "CopyMasterPatch(" in baker or "ClearPatch(" in baker:
            fail(errors, "Face swapping still uses a hard rectangular copy or cut")
        bake_start = baker.find("public static void BakeDraftLayerPack()")
        build_specs_start = baker.find("private static List<Spec> BuildSpecs()")
        bake_method = (
            baker[bake_start:build_specs_start]
            if bake_start >= 0 and build_specs_start > bake_start
            else ""
        )
        if "EnforceExclusiveRuntimeArtworkOwnership(" in bake_method:
            fail(
                errors,
                "Draft baking still erases the intact body to expose hidden "
                "anatomical cutouts",
            )

    face = read_text(
        root,
        "Assets/GameWorkPatch4/Runtime/Patch4FaceController.cs",
        errors,
    )
    if face:
        for required in (
            "ApplyLidClosure(",
            "SetLidsActive(false)",
            "SetOpenEyesActive(",
            "SetGraphicOpacity(",
            "eyelidFadeStart",
        ):
            if required not in face:
                fail(errors, f"Independent blink controller is missing: {required}")

    draft_validator = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4DraftLayerValidator.cs",
        errors,
    )
    if draft_validator:
        if "ValidateFaceReplacementLayers(" not in draft_validator:
            fail(errors, "Draft validation does not reject rectangular face backings")
        if "ValidateFaceTransitionLayers(" not in draft_validator:
            fail(errors, "Draft validation does not reject hard face-transition cuts")
        if "ValidateContinuousBodyLayer(" not in draft_validator:
            fail(errors, "Draft validation does not require one intact runtime body")


def validate_v23_full_frame_sheets(root: Path, errors: list[str]) -> None:
    sheet_root = "Assets/GameWorkPatch4/Art/Character/FatMan/V23FullFrame/"
    sheet_names = (
        "FatMan_Idle_V23.png",
        "FatMan_Face_V23.png",
        "FatMan_Tap_V23.png",
        "FatMan_Pose_V23.png",
        "FatMan_Upgrade_V23.png",
        "FatMan_WalkRight_V23.png",
    )
    for sheet_name in sheet_names:
        sheet_relative = sheet_root + sheet_name
        sheet_path = root / sheet_relative
        try:
            sheet_data = sheet_path.read_bytes()
        except OSError as exc:
            fail(errors, f"V23 sheet is unreadable ({sheet_name}): {exc}")
            continue
        if (
            len(sheet_data) < 29
            or sheet_data[:8] != b"\x89PNG\r\n\x1a\n"
            or sheet_data[12:16] != b"IHDR"
        ):
            fail(errors, f"V23 sheet is not a valid PNG: {sheet_name}")
            continue
        sheet_width, sheet_height = struct.unpack(">II", sheet_data[16:24])
        if (sheet_width, sheet_height) != (1536, 1024):
            fail(
                errors,
                f"V23 sheet must be 1536 x 1024: {sheet_name} is "
                f"{sheet_width} x {sheet_height}",
            )
        if sheet_data[24] != 8 or sheet_data[25] != 6:
            fail(errors, f"V23 sheet must be 8-bit RGBA: {sheet_name}")
        meta = read_text(root, sheet_relative + ".meta", errors)
        for setting in (
            "enableMipMap: 0",
            "isReadable: 1",
            "wrapU: 1",
            "wrapV: 1",
            "textureCompression: 0",
            "alphaIsTransparency: 1",
        ):
            if meta and setting not in meta:
                fail(
                    errors,
                    f"V23 texture import contract is missing {setting}: "
                    f"{sheet_name}",
                )

    corrected_upgrade_relative = (
        "Assets/GameWorkPatch4/Art/Character/FatMan/V24Corrections/"
        "FatMan_Upgrade_V24.png"
    )
    corrected_upgrade_path = root / corrected_upgrade_relative
    try:
        corrected_upgrade_data = corrected_upgrade_path.read_bytes()
    except OSError as exc:
        fail(errors, f"V24 corrected upgrade sheet is unreadable: {exc}")
        corrected_upgrade_data = b""

    if corrected_upgrade_data:
        if (
            len(corrected_upgrade_data) < 29
            or corrected_upgrade_data[:8] != b"\x89PNG\r\n\x1a\n"
            or corrected_upgrade_data[12:16] != b"IHDR"
        ):
            fail(errors, "V24 corrected upgrade sheet is not a valid PNG")
        else:
            corrected_width, corrected_height = struct.unpack(
                ">II", corrected_upgrade_data[16:24]
            )
            if (corrected_width, corrected_height) != (1536, 1024):
                fail(
                    errors,
                    "V24 corrected upgrade sheet must be 1536 x 1024",
                )
            if (
                corrected_upgrade_data[24] != 8
                or corrected_upgrade_data[25] != 6
            ):
                fail(
                    errors,
                    "V24 corrected upgrade sheet must be 8-bit RGBA",
                )
            else:
                try:
                    corrected_rgba = decode_png_rgba(
                        corrected_upgrade_data,
                        corrected_width,
                        corrected_height,
                    )
                    cell_width = corrected_width // 4
                    cell_height = corrected_height // 2
                    corrected_bounds = []
                    for frame in range(8):
                        column, row = frame % 4, frame // 4
                        x_min = cell_width
                        y_min = cell_height
                        x_max = -1
                        y_max = -1
                        for local_y in range(cell_height):
                            y = row * cell_height + local_y
                            for local_x in range(cell_width):
                                x = column * cell_width + local_x
                                if (
                                    corrected_rgba[
                                        (y * corrected_width + x) * 4 + 3
                                    ]
                                    >= 32
                                ):
                                    x_min = min(x_min, local_x)
                                    y_min = min(y_min, local_y)
                                    x_max = max(x_max, local_x)
                                    y_max = max(y_max, local_y)
                        if x_max < 0:
                            fail(
                                errors,
                                f"V24 corrected upgrade frame {frame} is empty",
                            )
                            corrected_bounds.append(None)
                            continue
                        bounds = (
                            x_min,
                            y_min,
                            x_max + 1,
                            y_max + 1,
                        )
                        corrected_bounds.append(bounds)
                        if (
                            bounds[0] <= 1
                            or bounds[1] <= 1
                            or bounds[2] >= cell_width - 1
                            or bounds[3] >= cell_height - 1
                        ):
                            fail(
                                errors,
                                "V24 corrected upgrade frame touches a cell "
                                f"edge: frame {frame}, bounds {bounds}",
                            )

                    # V23 frame 5 was only an enlarged torso. Its corrected
                    # replacement must contain a full head-to-shoes figure.
                    frame_five = corrected_bounds[5]
                    if (
                        frame_five is None
                        or frame_five[3] - frame_five[1] < 360
                    ):
                        fail(
                            errors,
                            "V24 upgrade frame 5 is still cropped before the "
                            "shoes",
                        )
                except (ValueError, zlib.error) as exc:
                    fail(
                        errors,
                        "V24 corrected upgrade pixels could not be decoded: "
                        f"{exc}",
                    )

        corrected_meta = read_text(
            root,
            corrected_upgrade_relative + ".meta",
            errors,
        )
        for setting in (
            "enableMipMap: 0",
            "isReadable: 1",
            "wrapU: 1",
            "wrapV: 1",
            "textureCompression: 0",
            "alphaIsTransparency: 1",
        ):
            if corrected_meta and setting not in corrected_meta:
                fail(
                    errors,
                    "V24 corrected upgrade import contract is missing "
                    f"{setting}",
                )

    relative = sheet_root + "FatMan_WalkRight_V23.png"
    path = root / relative
    try:
        data = path.read_bytes()
    except OSError as exc:
        fail(errors, f"V23 right-profile walk sheet is unreadable: {exc}")
        return

    if (
        len(data) < 29
        or data[:8] != b"\x89PNG\r\n\x1a\n"
        or data[12:16] != b"IHDR"
    ):
        fail(errors, "V23 right-profile walk sheet is not a valid PNG")
        return

    width, height = struct.unpack(">II", data[16:24])
    if width % 4 != 0 or height % 2 != 0:
        fail(
            errors,
            "V23 walk sheet must divide into four equal columns and two rows",
        )
    if data[24] != 8 or data[25] != 6:
        fail(errors, "V23 walk sheet must be 8-bit RGBA PNG data")
        return

    try:
        rgba = decode_png_rgba(data, width, height)
    except (ValueError, zlib.error) as exc:
        fail(errors, f"V23 walk sheet pixels could not be decoded: {exc}")
        return

    cell_width = width // 4
    cell_height = height // 2
    arm_regions = (
        (0.10, 0.48, 0.15, 0.67),
        (0.52, 0.90, 0.15, 0.67),
    )
    leg_regions = (
        (0.22, 0.50, 0.52, 0.98),
        (0.50, 0.78, 0.52, 0.98),
    )
    arm_differences = [
        alpha_silhouette_difference(
            rgba, width, cell_width, cell_height, 0, 2, region
        )
        for region in arm_regions
    ]
    leg_differences = [
        alpha_silhouette_difference(
            rgba, width, cell_width, cell_height, 0, 2, region
        )
        for region in leg_regions
    ]
    adjacent_differences = [
        alpha_silhouette_difference(
            rgba,
            width,
            cell_width,
            cell_height,
            frame,
            (frame + 1) % 8,
            (0.0, 1.0, 0.0, 1.0),
        )
        for frame in range(8)
    ]
    if min(arm_differences) < 0.14:
        fail(
            errors,
            "V23 opposing contact poses do not visibly articulate both arms "
            f"({arm_differences[0]:.3f}/{arm_differences[1]:.3f})",
        )
    if min(leg_differences) < 0.14:
        fail(
            errors,
            "V23 opposing contact poses do not visibly articulate both legs "
            f"({leg_differences[0]:.3f}/{leg_differences[1]:.3f})",
        )
    if min(adjacent_differences) < 0.075:
        fail(
            errors,
            "V23 contains a duplicated or nearly static adjacent frame "
            f"(minimum difference {min(adjacent_differences):.3f})",
        )

    profile_offsets = []
    for frame in range(8):
        head_x = alpha_centroid_x(
            rgba, width, cell_width, cell_height, frame, (0.0, 1.0, 0.05, 0.32)
        )
        torso_x = alpha_centroid_x(
            rgba, width, cell_width, cell_height, frame, (0.0, 1.0, 0.30, 0.60)
        )
        profile_offsets.append(head_x - torso_x)
    if (
        sum(profile_offsets) / len(profile_offsets) < 5.0
        or sum(offset > 3.0 for offset in profile_offsets) < 6
    ):
        fail(
            errors,
            "V23 Walk must remain a screen-right profile rather than a "
            "front-facing body sliding sideways",
        )

    face_relative = sheet_root + "FatMan_Face_V23.png"
    try:
        face_data = (root / face_relative).read_bytes()
    except OSError as exc:
        fail(errors, f"V23 face sheet is unreadable: {exc}")
        return
    face_width, face_height = struct.unpack(">II", face_data[16:24])
    try:
        face_rgba = decode_png_rgba(face_data, face_width, face_height)
    except (ValueError, zlib.error) as exc:
        fail(errors, f"V23 face sheet pixels could not be decoded: {exc}")
        return
    face_region = (0.31, 0.69, 0.09, 0.33)
    blink_difference = color_region_difference(
        face_rgba,
        face_width,
        face_width // 4,
        face_height // 2,
        0,
        2,
        face_region,
    )
    look_difference = color_region_difference(
        face_rgba,
        face_width,
        face_width // 4,
        face_height // 2,
        4,
        6,
        face_region,
    )
    if blink_difference < 0.02 or look_difference < 0.02:
        fail(
            errors,
            "V23 complete-frame facial articulation is too weak "
            f"(blink {blink_difference:.3f}, look {look_difference:.3f})",
        )


def decode_png_rgba(data: bytes, width: int, height: int) -> bytes:
    cursor = 8
    compressed = bytearray()
    while cursor + 12 <= len(data):
        length = struct.unpack(">I", data[cursor : cursor + 4])[0]
        chunk_type = data[cursor + 4 : cursor + 8]
        chunk_data = data[cursor + 8 : cursor + 8 + length]
        if len(chunk_data) != length:
            raise ValueError("truncated PNG chunk")
        if chunk_type == b"IDAT":
            compressed.extend(chunk_data)
        cursor += 12 + length
        if chunk_type == b"IEND":
            break

    raw = zlib.decompress(bytes(compressed))
    stride = width * 4
    expected = height * (stride + 1)
    if len(raw) != expected:
        raise ValueError("unexpected decompressed PNG size")

    output = bytearray(height * stride)
    previous = bytearray(stride)
    source_offset = 0
    for row in range(height):
        filter_type = raw[source_offset]
        source_offset += 1
        scanline = bytearray(raw[source_offset : source_offset + stride])
        source_offset += stride
        for index in range(stride):
            left = scanline[index - 4] if index >= 4 else 0
            above = previous[index]
            upper_left = previous[index - 4] if index >= 4 else 0
            if filter_type == 1:
                scanline[index] = (scanline[index] + left) & 0xFF
            elif filter_type == 2:
                scanline[index] = (scanline[index] + above) & 0xFF
            elif filter_type == 3:
                scanline[index] = (
                    scanline[index] + ((left + above) // 2)
                ) & 0xFF
            elif filter_type == 4:
                scanline[index] = (
                    scanline[index] + paeth(left, above, upper_left)
                ) & 0xFF
            elif filter_type != 0:
                raise ValueError(f"unsupported PNG filter {filter_type}")
        start = row * stride
        output[start : start + stride] = scanline
        previous = scanline
    return bytes(output)


def paeth(left: int, above: int, upper_left: int) -> int:
    estimate = left + above - upper_left
    left_distance = abs(estimate - left)
    above_distance = abs(estimate - above)
    upper_left_distance = abs(estimate - upper_left)
    if left_distance <= above_distance and left_distance <= upper_left_distance:
        return left
    if above_distance <= upper_left_distance:
        return above
    return upper_left


def alpha_silhouette_difference(
    rgba: bytes,
    image_width: int,
    cell_width: int,
    cell_height: int,
    first_frame: int,
    second_frame: int,
    region: tuple[float, float, float, float],
) -> float:
    x0, x1, y0, y1 = region
    local_x_min = max(0, min(cell_width - 1, int(x0 * cell_width)))
    local_x_max = max(local_x_min + 1, min(cell_width, int(x1 * cell_width + 0.999)))
    local_y_min = max(0, min(cell_height - 1, int(y0 * cell_height)))
    local_y_max = max(local_y_min + 1, min(cell_height, int(y1 * cell_height + 0.999)))
    first_column, first_row = first_frame % 4, first_frame // 4
    second_column, second_row = second_frame % 4, second_frame // 4
    different = 0
    union = 0
    for local_y in range(local_y_min, local_y_max):
        first_y = first_row * cell_height + local_y
        second_y = second_row * cell_height + local_y
        for local_x in range(local_x_min, local_x_max):
            first_x = first_column * cell_width + local_x
            second_x = second_column * cell_width + local_x
            first_alpha = rgba[(first_y * image_width + first_x) * 4 + 3] >= 32
            second_alpha = rgba[(second_y * image_width + second_x) * 4 + 3] >= 32
            union += int(first_alpha or second_alpha)
            different += int(first_alpha != second_alpha)
    return different / union if union else 0.0


def alpha_centroid_x(
    rgba: bytes,
    image_width: int,
    cell_width: int,
    cell_height: int,
    frame: int,
    region: tuple[float, float, float, float],
) -> float:
    x0, x1, y0, y1 = region
    local_x_min = max(0, min(cell_width - 1, int(x0 * cell_width)))
    local_x_max = max(
        local_x_min + 1,
        min(cell_width, int(x1 * cell_width + 0.999)),
    )
    local_y_min = max(0, min(cell_height - 1, int(y0 * cell_height)))
    local_y_max = max(
        local_y_min + 1,
        min(cell_height, int(y1 * cell_height + 0.999)),
    )
    column, row = frame % 4, frame // 4
    total_x = 0
    visible = 0
    for local_y in range(local_y_min, local_y_max):
        y = row * cell_height + local_y
        for local_x in range(local_x_min, local_x_max):
            x = column * cell_width + local_x
            if rgba[(y * image_width + x) * 4 + 3] >= 32:
                total_x += local_x
                visible += 1
    return total_x / visible if visible else 0.0


def color_region_difference(
    rgba: bytes,
    image_width: int,
    cell_width: int,
    cell_height: int,
    first_frame: int,
    second_frame: int,
    region: tuple[float, float, float, float],
) -> float:
    x0, x1, y0, y1 = region
    local_x_min = max(0, min(cell_width - 1, int(x0 * cell_width)))
    local_x_max = max(
        local_x_min + 1,
        min(cell_width, int(x1 * cell_width + 0.999)),
    )
    local_y_min = max(0, min(cell_height - 1, int(y0 * cell_height)))
    local_y_max = max(
        local_y_min + 1,
        min(cell_height, int(y1 * cell_height + 0.999)),
    )
    first_column, first_row = first_frame % 4, first_frame // 4
    second_column, second_row = second_frame % 4, second_frame // 4
    changed = 0
    reference = 0
    for local_y in range(local_y_min, local_y_max):
        first_y = first_row * cell_height + local_y
        second_y = second_row * cell_height + local_y
        for local_x in range(local_x_min, local_x_max):
            first_x = first_column * cell_width + local_x
            second_x = second_column * cell_width + local_x
            first_index = (first_y * image_width + first_x) * 4
            second_index = (second_y * image_width + second_x) * 4
            first = rgba[first_index : first_index + 4]
            second = rgba[second_index : second_index + 4]
            if first[3] < 32 and second[3] < 32:
                continue
            reference += 1
            if sum(abs(first[i] - second[i]) for i in range(4)) >= 48:
                changed += 1
    return changed / reference if reference else 0.0


def changed_paths(root: Path, base_ref: str) -> Iterable[str]:
    result = subprocess.run(
        ["git", "diff", "--name-only", f"{base_ref}...HEAD"],
        cwd=root,
        check=False,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or "git diff failed")
    return [line.strip().replace("\\", "/") for line in result.stdout.splitlines() if line.strip()]


def validate_protected_paths(root: Path, base_ref: str, errors: list[str]) -> None:
    try:
        paths = list(changed_paths(root, base_ref))
    except RuntimeError as exc:
        fail(errors, f"Could not inspect protected paths: {exc}")
        return

    for path in paths:
        normalized = "/" + path.lstrip("/")
        for fragment in PROTECTED_FRAGMENTS:
            if fragment.lower() in normalized.lower():
                fail(errors, f"Protected path changed: {path}")
                break


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", default=None)
    parser.add_argument("--base-ref", default="main")
    args = parser.parse_args()

    root = Path(args.repo_root).resolve() if args.repo_root else Path(__file__).resolve().parents[3]
    errors: list[str] = []

    for relative in REQUIRED_FILES:
        if not (root / relative).is_file():
            fail(errors, f"Missing required Patch 4 file: {relative}")

    validate_contract(root, errors)
    validate_json_files(root, errors)
    validate_repository_master(root, errors)
    validate_repository_restore_pipeline(root, errors)
    validate_readiness_gate(root, errors)
    validate_runtime_installation(root, errors)
    validate_neutral_pose_qa(root, errors)
    validate_v23_full_frame_sheets(root, errors)
    validate_protected_paths(root, args.base_ref, errors)

    if errors:
        print("Patch 4 static guard FAILED")
        for index, error in enumerate(errors, start=1):
            print(f"{index}. {error}")
        return 1

    print("Patch 4 static guard PASSED")
    print("- contract counts and uniqueness verified")
    print("- exact 1024 x 1536 RGBA repository master and SHA verified")
    print("- automatic joint/face restore and full rebake order verified")
    print("- automatic readiness approval blocked")
    print("- one intact painted body uses a dense continuous anatomical deformation grid")
    print("- exact neutral face and feathered expression replacements share the Head matrix")
    print("- Test Runner exit must stay quiescent before the separate room review")
    print("- actual-room review blocks weak limbs, collapse, over-stretch and Console errors")
    print("- V23 uses one complete RGBA body for all ten clips while every legacy mesh layer stays hidden")
    print("- V23 walk is a right-facing eight-phase gait with monotonic room travel")
    print("- V23 blink and look-around use measurable painted facial changes")
    print("- V24 repairs the cropped upgrade pose and calibrates scale plus shoe line")
    print("- actual-room review includes an uninterrupted final-cadence gameplay preview")
    print("- V25 routes idle, routine, tap, walk, turn and upgrade gameplay actions")
    print("- V26 gives Test Runner exclusive PlayMode ownership after legacy Animator preflight")
    print("- legacy walk routine and one-shot footstep stay isolated from Patch 4 review")
    print("- rollback rig stays logically active and is restored after review")
    print("- neutral and independent face-pose QA remain read-only and human-gated")
    print("- protected menu, video, music and settings paths unchanged")
    return 0


if __name__ == "__main__":
    sys.exit(main())
