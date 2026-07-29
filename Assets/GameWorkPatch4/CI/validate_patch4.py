#!/usr/bin/env python3
"""Static safety checks for GameWork Patch 4.0.

This script intentionally does not pretend to compile Unity. It validates the
source-of-truth contract, protected paths, readiness SHA and JSON manifests on
any machine that has Python and git.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path
from typing import Iterable

EXPECTED_SHA = "5873cf6df0df2b5ebd4947b687693162d4b34899202326d1b1ae62df9f50587c"
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
    "Assets/GameWorkPatch4/Runtime/Patch4RuntimeInstaller.cs",
    "Assets/GameWorkPatch4/Editor/Patch4ProductionPipeline.cs",
    "Assets/GameWorkPatch4/Editor/Patch4DraftLayerValidator.cs",
    "Assets/GameWorkPatch4/Editor/Patch4PrefabReadinessBinder.cs",
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
        )
        for snippet in required_snippets:
            if snippet not in presentation:
                fail(errors, f"Canvas presentation is missing: {snippet}")

        if "SetPatch4Enabled(" in presentation:
            fail(errors, "Canvas presentation must never change Patch 4 activation")

    builder = read_text(
        root,
        "Assets/GameWorkPatch4/Editor/Patch4PrefabBuilder.cs",
        errors,
    )
    if builder and 'PrefabRoot = "Assets/GameWorkPatch4/Resources"' not in builder:
        fail(errors, "Patch 4 prefab must be generated into isolated Resources")


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
    validate_readiness_gate(root, errors)
    validate_runtime_installation(root, errors)
    validate_protected_paths(root, args.base_ref, errors)

    if errors:
        print("Patch 4 static guard FAILED")
        for index, error in enumerate(errors, start=1):
            print(f"{index}. {error}")
        return 1

    print("Patch 4 static guard PASSED")
    print("- contract counts and uniqueness verified")
    print("- approved master SHA and manifests verified")
    print("- automatic readiness approval blocked")
    print("- Canvas runtime installation remains locked to rollback mode")
    print("- protected menu, video, music and settings paths unchanged")
    return 0


if __name__ == "__main__":
    sys.exit(main())
