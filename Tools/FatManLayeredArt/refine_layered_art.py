#!/usr/bin/env python3
"""Trim generated layers to their anatomical region and remove stray fragments."""

from __future__ import annotations

import json
from collections import deque
from pathlib import Path
from typing import Dict, Tuple

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Assets/Resources/Characters/FatManLayered/Generated"
MANIFEST = OUTPUT / "manifest.json"

FRONT_BACK: Dict[str, Tuple[float, float, float, float]] = {
    "Pelvis": (0.20, 0.80, 0.27, 0.51),
    "Chest": (0.16, 0.84, 0.56, 0.84),
    "Belly": (0.13, 0.87, 0.42, 0.67),
    "Head": (0.28, 0.72, 0.75, 1.00),
    "UpperArm_L": (0.06, 0.37, 0.53, 0.83),
    "Forearm_L": (0.03, 0.30, 0.35, 0.66),
    "Hand_L": (0.03, 0.26, 0.25, 0.47),
    "UpperArm_R": (0.63, 0.94, 0.53, 0.83),
    "Forearm_R": (0.70, 0.97, 0.35, 0.66),
    "Hand_R": (0.74, 0.97, 0.25, 0.47),
    "Thigh_L": (0.23, 0.51, 0.18, 0.43),
    "Shin_L": (0.22, 0.50, 0.045, 0.29),
    "Foot_L": (0.15, 0.52, 0.00, 0.135),
    "Thigh_R": (0.49, 0.77, 0.18, 0.43),
    "Shin_R": (0.50, 0.78, 0.045, 0.29),
    "Foot_R": (0.48, 0.85, 0.00, 0.135),
}

SIDE: Dict[str, Tuple[float, float, float, float]] = {
    "Pelvis": (0.20, 0.80, 0.27, 0.51),
    "Chest": (0.20, 0.78, 0.56, 0.84),
    "Belly": (0.19, 0.91, 0.42, 0.68),
    "Head": (0.29, 0.76, 0.75, 1.00),
    "UpperArm_L": (0.23, 0.53, 0.53, 0.83),
    "Forearm_L": (0.24, 0.53, 0.35, 0.66),
    "Hand_L": (0.27, 0.53, 0.25, 0.47),
    "UpperArm_R": (0.53, 0.83, 0.53, 0.83),
    "Forearm_R": (0.54, 0.84, 0.35, 0.66),
    "Hand_R": (0.54, 0.82, 0.25, 0.47),
    "Thigh_L": (0.25, 0.55, 0.18, 0.43),
    "Shin_L": (0.25, 0.56, 0.045, 0.29),
    "Foot_L": (0.20, 0.58, 0.00, 0.135),
    "Thigh_R": (0.45, 0.75, 0.18, 0.43),
    "Shin_R": (0.45, 0.77, 0.045, 0.29),
    "Foot_R": (0.43, 0.82, 0.00, 0.135),
}


def anchor_component(mask: np.ndarray, pivot_x: float, pivot_y: float) -> np.ndarray:
    ys, xs = np.nonzero(mask)
    if not len(xs):
        return mask
    target_x = pivot_x * mask.shape[1]
    target_y = (1.0 - pivot_y) * mask.shape[0]
    nearest = np.argmin((xs - target_x) ** 2 + (ys - target_y) ** 2)
    start = (int(ys[nearest]), int(xs[nearest]))
    visited = np.zeros_like(mask, dtype=bool)
    queue = deque([start])
    visited[start] = True
    height, width = mask.shape
    while queue:
        y, x = queue.popleft()
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if not dx and not dy:
                    continue
                ny, nx = y + dy, x + dx
                if 0 <= ny < height and 0 <= nx < width and mask[ny, nx] and not visited[ny, nx]:
                    visited[ny, nx] = True
                    queue.append((ny, nx))
    return visited


def refine_part(view: dict, part: dict) -> None:
    if part.get("faceGroup") or part["name"] in ("ShirtHem", "ChinSoft", "Hair"):
        return
    bounds = (SIDE if view["name"] == "Side" else FRONT_BACK).get(part["name"])
    if bounds is None:
        return

    resource = part["resource"]
    path = ROOT / "Assets/Resources" / f"{resource}.png"
    image = Image.open(path).convert("RGBA")
    rgba = np.asarray(image).copy()
    alpha = rgba[:, :, 3] > 8
    old_height, old_width = alpha.shape

    anchor_px_x = part["anchorX"] * view["width"]
    anchor_px_y = (1.0 - part["anchorY"]) * view["height"]
    old_x0 = anchor_px_x - part["pivotX"] * old_width
    old_y0 = anchor_px_y - (1.0 - part["pivotY"]) * old_height

    yy, xx = np.mgrid[0:old_height, 0:old_width]
    full_x = (old_x0 + xx + 0.5) / view["width"]
    full_y = 1.0 - ((old_y0 + yy + 0.5) / view["height"])
    x0, x1, y0, y1 = bounds
    alpha &= (full_x >= x0) & (full_x <= x1) & (full_y >= y0) & (full_y <= y1)
    alpha = anchor_component(alpha, part["pivotX"], part["pivotY"])

    ys, xs = np.nonzero(alpha)
    if not len(xs):
        raise RuntimeError(f"Refinement removed all pixels from {view['name']}/{part['name']}")
    padding = 5
    crop_x0 = max(0, int(xs.min()) - padding)
    crop_y0 = max(0, int(ys.min()) - padding)
    crop_x1 = min(old_width, int(xs.max()) + padding + 1)
    crop_y1 = min(old_height, int(ys.max()) + padding + 1)

    rgba[:, :, 3] = np.where(alpha, rgba[:, :, 3], 0)
    cropped = rgba[crop_y0:crop_y1, crop_x0:crop_x1]
    Image.fromarray(cropped, mode="RGBA").save(path, optimize=True)

    pivot_pixel_x = part["pivotX"] * old_width - crop_x0
    pivot_pixel_y_top = (1.0 - part["pivotY"]) * old_height - crop_y0
    new_width = crop_x1 - crop_x0
    new_height = crop_y1 - crop_y0
    part["pivotX"] = round(pivot_pixel_x / new_width, 6)
    part["pivotY"] = round(1.0 - pivot_pixel_y_top / new_height, 6)
    part["cropWidth"] = new_width
    part["cropHeight"] = new_height


def main() -> None:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    for view in manifest["views"]:
        for part in view["parts"]:
            refine_part(view, part)
    MANIFEST.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print("Refined layered art: anatomical bounds enforced and stray components removed.")


if __name__ == "__main__":
    main()
