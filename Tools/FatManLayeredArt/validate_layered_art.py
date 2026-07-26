#!/usr/bin/env python3
"""Fail CI if Patch 3.6 generated art violates the runtime contract."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Dict, Set

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "Assets/Resources/Characters/FatManLayered/Generated/manifest.json"
RESOURCES = ROOT / "Assets/Resources"

BODY_REQUIRED: Set[str] = {
    "Pelvis", "Chest", "Belly", "Head",
    "UpperArm_L", "Forearm_L", "Hand_L",
    "UpperArm_R", "Forearm_R", "Hand_R",
    "Thigh_L", "Shin_L", "Foot_L",
    "Thigh_R", "Shin_R", "Foot_R",
}
FRONT_FACE = {
    "EyeL_Open", "EyeL_Closed", "EyeR_Open", "EyeR_Closed",
    "Mouth_Neutral", "Mouth_Open", "Mouth_Strain", "Mouth_Yawn",
}
SIDE_FACE = {
    "Eye_Open", "Eye_Closed",
    "Mouth_Neutral", "Mouth_Open", "Mouth_Strain", "Mouth_Yawn",
}
MAX_WIDTH: Dict[str, float] = {
    "Pelvis": 0.68,
    "Chest": 0.76,
    "Belly": 0.82,
    "Head": 0.52,
    "UpperArm_L": 0.38,
    "UpperArm_R": 0.38,
    "Forearm_L": 0.34,
    "Forearm_R": 0.34,
    "Hand_L": 0.30,
    "Hand_R": 0.30,
    "Thigh_L": 0.38,
    "Thigh_R": 0.38,
    "Shin_L": 0.38,
    "Shin_R": 0.38,
    "Foot_L": 0.46,
    "Foot_R": 0.46,
}


def fail(message: str) -> None:
    raise SystemExit("Patch 3.6 validation failed: " + message)


def main() -> None:
    if not MANIFEST.exists():
        fail("manifest.json is missing")
    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    if data.get("version") != 36:
        fail("manifest version must be 36")
    if len(data.get("stages", [])) < 4:
        fail("four stage profiles are required")

    views = {view["name"]: view for view in data.get("views", [])}
    if set(views) != {"Front", "Side", "Back"}:
        fail("Front, Side and Back are required")

    total = 0
    for view_name, view in views.items():
        parts = view.get("parts", [])
        names = {part["name"] for part in parts}
        missing = BODY_REQUIRED - names
        if missing:
            fail(f"{view_name} is missing body parts: {sorted(missing)}")
        if view_name == "Front" and FRONT_FACE - names:
            fail(f"Front is missing face states: {sorted(FRONT_FACE - names)}")
        if view_name == "Side" and SIDE_FACE - names:
            fail(f"Side is missing face states: {sorted(SIDE_FACE - names)}")
        if view_name == "Back" and any(part.get("faceGroup") for part in parts):
            fail("Back must not contain face states")

        seen_resources = set()
        for part in parts:
            resource = part.get("resource", "")
            if not resource or resource in seen_resources:
                fail(f"{view_name}/{part.get('name')} has an invalid resource")
            seen_resources.add(resource)
            path = RESOURCES / f"{resource}.png"
            if not path.exists():
                fail(f"missing texture: {path}")
            image = Image.open(path).convert("RGBA")
            alpha = np.asarray(image)[:, :, 3]
            opaque = int((alpha > 8).sum())
            if opaque < 20:
                fail(f"{view_name}/{part['name']} is empty")
            if image.width != part["cropWidth"] or image.height != part["cropHeight"]:
                fail(f"{view_name}/{part['name']} dimensions disagree with manifest")
            if part["name"] in MAX_WIDTH:
                ratio = image.width / float(view["width"])
                limit = MAX_WIDTH[part["name"]]
                if view_name == "Side":
                    limit += 0.10
                if ratio > limit:
                    fail(
                        f"{view_name}/{part['name']} is too wide "
                        f"({ratio:.2f} > {limit:.2f}); likely contains stray body pixels"
                    )
            if not (-0.35 <= part["pivotX"] <= 1.35 and -0.35 <= part["pivotY"] <= 1.35):
                fail(f"{view_name}/{part['name']} pivot is outside safe overlap range")
            total += 1

    if total < 60:
        fail(f"expected at least 60 layered textures, found {total}")
    print(f"Patch 3.6 layered-art validation passed with {total} textures.")


if __name__ == "__main__":
    main()
