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
    "Assets/GameWorkPatch4/Runtime/Patch4RuntimeInstaller.cs",
    "Assets/GameWorkPatch4/Editor/Patch4ProductionPipeline.cs",
    "Assets/GameWorkPatch4/Editor/Patch4LayerImportPostprocessor.cs",
    "Assets/GameWorkPatch4/Editor/Patch4DraftLayerValidator.cs",
    "Assets/GameWorkPatch4/Editor/Patch4NeutralPoseValidator.cs",
    "Assets/GameWorkPatch4/Editor/Patch4NeutralPoseReviewWindow.cs",
    "Assets/GameWorkPatch4/Editor/Patch4FacePoseReviewWindow.cs",
    "Assets/GameWorkPatch4/Editor/Patch4AnimationRoomReview.cs",
    "Assets/GameWorkPatch4/Editor/Patch4AnimationRoomReviewWindow.cs",
    "Assets/GameWorkPatch4/Editor/Patch4PrefabReadinessBinder.cs",
    REPOSITORY_MASTER,
    "Assets/GameWorkPatch4/Art/Character/FatMan/master-source.json",
    "Assets/GameWorkPatch4/Art/Character/FatMan/Masks/adobe-mask-manifest.json",
    "Docs/Patch4/CHECKPOINT.md",
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
            '"fresh-room-review-handoff-v8"',
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
            'FindLayerObject("Face/EyeWhiteL")',
            'FindLayerObject("Face/IrisR")',
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
    if builder and 'PrefabRoot = "Assets/GameWorkPatch4/Resources"' not in builder:
        fail(errors, "Patch 4 prefab must be generated into isolated Resources")

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
            "CaptureBindPose()",
        )
        for snippet in required_snippets:
            if snippet not in deformer:
                fail(errors, f"Canvas skin deformer is missing: {snippet}")
        if "SetPatch4Enabled(" in deformer:
            fail(errors, "Canvas skin deformer must never change Patch 4 activation")
        if "DataUtility.GetOuterUV" in deformer:
            fail(
                errors,
                "Canvas skin deformer must not expand a Tight opaque UV crop "
                "over the full layer canvas",
            )

    importer = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4LayerImportPostprocessor.cs",
        errors,
    )
    if importer and "spriteMeshType = SpriteMeshType.FullRect" not in importer:
        fail(errors, "Patch 4 layer imports must force FullRect sprite meshes")

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
            "minimumMotionCoverage",
            "neutralWidthRetention",
            "visualSanityPassed",
            "visibleMotionPassed",
            "legacyRoutine.enabled = false",
            "legacySignalBridge.enabled = false",
            "source.Stop()",
            "runToken = reviewRunToken",
            "string.IsNullOrWhiteSpace(reviewRunToken)",
            "Application.logMessageReceived",
            "reviewConsoleErrorCount == 0",
            "SetEditorReviewActive(true)",
            "SetEditorReviewActive(false)",
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
            "AddFloatTransition(",
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
            "QueueEnterPlayMode()",
            "ClearPreviousReviewArtifacts()",
            "CurrentRunToken",
            "HasFreshRoomArtifacts()",
            "hasFreshRoomArtifacts",
            "Patch4AnimationRoomReviewWindow.Open()",
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
            '"Face/LidL"',
            '"Face/LidR"',
            '"Face/MouthOpen"',
            '"Face/MouthSmile"',
            '"FX/Sweat"',
            '"FX/ImpactFold"',
            '"FX/Shadow"',
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
        for hidden_layer in (
            '"Face/LidL"',
            '"Face/LidR"',
            '"Face/MouthOpen"',
            '"Face/MouthSmile"',
            '"FX/Sweat"',
            '"FX/ImpactFold"',
        ):
            if hidden_layer not in presentation:
                fail(
                    errors,
                    f"Canvas neutral visibility is missing: {hidden_layer}",
                )

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
        ):
            if required not in baker:
                fail(errors, f"Joint/face candidate baker is missing: {required}")
        if "PaintJointScaffold(" in baker:
            fail(errors, "Legacy five-pixel joint scaffolding is still present")
        if "overlayOnly: true" in baker:
            fail(errors, "Alternate facial layers still contain opaque backing patches")
        if "CopyMasterPatch(" in baker or "ClearPatch(" in baker:
            fail(errors, "Face swapping still uses a hard rectangular copy or cut")

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
            "openScaleY",
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
    print("- Canvas grids use frozen bind anchors, full-canvas UVs and FullRect sprites")
    print("- actual-room review blocks weak motion, collapsed silhouettes and Console errors")
    print("- legacy walk routine and one-shot footstep stay isolated from Patch 4 review")
    print("- rollback rig stays logically active and is restored after review")
    print("- neutral and independent face-pose QA remain read-only and human-gated")
    print("- protected menu, video, music and settings paths unchanged")
    return 0


if __name__ == "__main__":
    sys.exit(main())
