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
            '"full-frame-ten-clip-review-v23"',
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
            "Fa