#!/usr/bin/env python3
"""Generate real, separate PNG layers from the painted fat-man turnaround.

The source turnaround is only used at authoring time. Runtime never renders the
whole-body PNG and never deforms it as one mesh. Each output file is a genuine
transparent layer with a joint pivot recorded in manifest.json.
"""

from __future__ import annotations

import json
import shutil
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Sequence, Tuple

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "Assets/Resources/Characters/FatMan/fat-man-turnaround-reference.png"
OUTPUT = ROOT / "Assets/Resources/Characters/FatManLayered/Generated"
MANIFEST = OUTPUT / "manifest.json"
ALPHA_THRESHOLD = 10


@dataclass(frozen=True)
class Region:
    name: str
    center: Tuple[float, float]
    radius: Tuple[float, float]
    anchor: Tuple[float, float]
    driver: str
    parent: str
    sort: int
    rotation_gain: float
    max_rotation: float
    translation_gain: float = 0.20
    max_translation: float = 10.0
    scale_gain: float = 0.15
    max_scale_delta: float = 0.06


FRONT_REGIONS: Sequence[Region] = (
    Region("Pelvis", (0.50, 0.405), (0.30, 0.13), (0.50, 0.405), "Bone.Pelvis", "", 40, 0.30, 7),
    Region("Chest", (0.50, 0.690), (0.31, 0.17), (0.50, 0.585), "Bone.Chest", "Pelvis", 52, 0.28, 6),
    Region("Belly", (0.50, 0.535), (0.36, 0.17), (0.50, 0.640), "Bone.Belly", "Pelvis", 58, 0.22, 5, 0.24, 11, 0.24, 0.07),
    Region("Head", (0.50, 0.865), (0.20, 0.16), (0.50, 0.775), "Bone.Head", "Chest", 82, 0.42, 9, 0.20, 7),
    Region("UpperArm_L", (0.245, 0.680), (0.14, 0.18), (0.305, 0.748), "Bone.UpperArm.L", "Chest", 34, 0.58, 24, 0.12, 7),
    Region("Forearm_L", (0.170, 0.520), (0.12, 0.17), (0.205, 0.605), "Bone.Forearm.L", "UpperArm_L", 36, 0.65, 33, 0.10, 5),
    Region("Hand_L", (0.150, 0.375), (0.10, 0.105), (0.160, 0.445), "Bone.Hand.L", "Forearm_L", 66, 0.52, 20, 0.08, 4),
    Region("UpperArm_R", (0.755, 0.680), (0.14, 0.18), (0.695, 0.748), "Bone.UpperArm.R", "Chest", 60, 0.58, 24, 0.12, 7),
    Region("Forearm_R", (0.830, 0.520), (0.12, 0.17), (0.795, 0.605), "Bone.Forearm.R", "UpperArm_R", 62, 0.65, 33, 0.10, 5),
    Region("Hand_R", (0.850, 0.375), (0.10, 0.105), (0.840, 0.445), "Bone.Hand.R", "Forearm_R", 68, 0.52, 20, 0.08, 4),
    Region("Thigh_L", (0.395, 0.295), (0.16, 0.16), (0.420, 0.405), "Bone.Thigh.L", "Pelvis", 42, 0.48, 19, 0.10, 6),
    Region("Shin_L", (0.375, 0.165), (0.14, 0.14), (0.390, 0.245), "Bone.Shin.L", "Thigh_L", 44, 0.58, 29, 0.08, 5),
    Region("Foot_L", (0.345, 0.060), (0.17, 0.075), (0.375, 0.105), "Bone.Foot.L", "Shin_L", 64, 0.50, 17, 0.05, 3),
    Region("Thigh_R", (0.605, 0.295), (0.16, 0.16), (0.580, 0.405), "Bone.Thigh.R", "Pelvis", 46, 0.48, 19, 0.10, 6),
    Region("Shin_R", (0.625, 0.165), (0.14, 0.14), (0.610, 0.245), "Bone.Shin.R", "Thigh_R", 48, 0.58, 29, 0.08, 5),
    Region("Foot_R", (0.655, 0.060), (0.17, 0.075), (0.625, 0.105), "Bone.Foot.R", "Shin_R", 70, 0.50, 17, 0.05, 3),
)

BACK_REGIONS: Sequence[Region] = tuple(
    Region(
        r.name,
        r.center,
        r.radius,
        r.anchor,
        r.driver,
        r.parent,
        ({34: 60, 36: 62, 60: 34, 62: 36, 42: 46, 44: 48, 46: 42, 48: 44}.get(r.sort, r.sort)),
        r.rotation_gain * (0.88 if "Arm" in r.name or "Forearm" in r.name else 1.0),
        r.max_rotation * (0.88 if "Arm" in r.name or "Forearm" in r.name else 1.0),
        r.translation_gain,
        r.max_translation,
        r.scale_gain,
        r.max_scale_delta,
    )
    for r in FRONT_REGIONS
)

SIDE_REGIONS: Sequence[Region] = (
    Region("Pelvis", (0.49, 0.405), (0.31, 0.13), (0.49, 0.405), "Bone.Pelvis", "", 40, 0.22, 5),
    Region("Chest", (0.485, 0.690), (0.28, 0.17), (0.485, 0.585), "Bone.Chest", "Pelvis", 52, 0.20, 4),
    Region("Belly", (0.555, 0.535), (0.38, 0.18), (0.500, 0.640), "Bone.Belly", "Pelvis", 58, 0.16, 4, 0.18, 8, 0.20, 0.055),
    Region("Head", (0.510, 0.865), (0.205, 0.16), (0.480, 0.775), "Bone.Head", "Chest", 82, 0.32, 7, 0.15, 5),
    Region("UpperArm_L", (0.395, 0.675), (0.13, 0.18), (0.430, 0.742), "Bone.UpperArm.L", "Chest", 30, 0.36, 17, 0.08, 5),
    Region("Forearm_L", (0.390, 0.515), (0.12, 0.17), (0.400, 0.595), "Bone.Forearm.L", "UpperArm_L", 32, 0.42, 22, 0.07, 4),
    Region("Hand_L", (0.405, 0.370), (0.10, 0.105), (0.400, 0.440), "Bone.Hand.L", "Forearm_L", 33, 0.34, 14, 0.05, 3),
    Region("UpperArm_R", (0.675, 0.675), (0.15, 0.18), (0.620, 0.742), "Bone.UpperArm.R", "Chest", 66, 0.43, 20, 0.08, 5),
    Region("Forearm_R", (0.690, 0.515), (0.13, 0.17), (0.680, 0.595), "Bone.Forearm.R", "UpperArm_R", 68, 0.48, 25, 0.07, 4),
    Region("Hand_R", (0.675, 0.370), (0.11, 0.105), (0.680, 0.440), "Bone.Hand.R", "Forearm_R", 70, 0.38, 15, 0.05, 3),
    Region("Thigh_L", (0.415, 0.295), (0.17, 0.16), (0.440, 0.405), "Bone.Thigh.L", "Pelvis", 36, 0.34, 14, 0.07, 4),
    Region("Shin_L", (0.420, 0.165), (0.15, 0.14), (0.420, 0.245), "Bone.Shin.L", "Thigh_L", 38, 0.42, 20, 0.06, 4),
    Region("Foot_L", (0.400, 0.060), (0.18, 0.075), (0.420, 0.105), "Bone.Foot.L", "Shin_L", 39, 0.34, 12, 0.04, 2),
    Region("Thigh_R", (0.595, 0.295), (0.17, 0.16), (0.560, 0.405), "Bone.Thigh.R", "Pelvis", 60, 0.38, 16, 0.07, 4),
    Region("Shin_R", (0.610, 0.165), (0.15, 0.14), (0.590, 0.245), "Bone.Shin.R", "Thigh_R", 62, 0.46, 22, 0.06, 4),
    Region("Foot_R", (0.640, 0.060), (0.18, 0.075), (0.610, 0.105), "Bone.Foot.R", "Shin_R", 64, 0.36, 13, 0.04, 2),
)

VIEW_REGIONS: Dict[str, Sequence[Region]] = {
    "Front": FRONT_REGIONS,
    "Side": SIDE_REGIONS,
    "Back": BACK_REGIONS,
}


def runs(values: np.ndarray) -> List[Tuple[int, int]]:
    result: List[Tuple[int, int]] = []
    start = None
    for index, active in enumerate(values.tolist() + [False]):
        if active and start is None:
            start = index
        elif not active and start is not None:
            result.append((start, index))
            start = None
    return result


def find_view_bounds(image: Image.Image) -> List[Tuple[int, int, int, int]]:
    rgba = np.asarray(image.convert("RGBA"))
    alpha = rgba[:, :, 3]
    active_columns = (alpha > ALPHA_THRESHOLD).any(axis=0)
    column_runs = [r for r in runs(active_columns) if r[1] - r[0] >= image.width * 0.08]
    if len(column_runs) < 3:
        third = image.width // 3
        column_runs = [(0, third), (third, third * 2), (third * 2, image.width)]
    if len(column_runs) > 3:
        column_runs = sorted(column_runs, key=lambda r: r[1] - r[0], reverse=True)[:3]
        column_runs.sort()

    bounds: List[Tuple[int, int, int, int]] = []
    for x0, x1 in column_runs[:3]:
        sub = alpha[:, x0:x1]
        active_rows = (sub > ALPHA_THRESHOLD).any(axis=1)
        row_runs = runs(active_rows)
        if not row_runs:
            raise RuntimeError(f"No opaque pixels found in turnaround column {x0}:{x1}")
        y0 = min(r[0] for r in row_runs)
        y1 = max(r[1] for r in row_runs)
        margin_x = max(3, int((x1 - x0) * 0.015))
        margin_y = max(3, int((y1 - y0) * 0.012))
        bounds.append((max(0, x0 - margin_x), max(0, y0 - margin_y), min(image.width, x1 + margin_x), min(image.height, y1 + margin_y)))
    if len(bounds) != 3:
        raise RuntimeError(f"Expected three turnaround views, found {len(bounds)}")
    return bounds


def elliptical_scores(width: int, height: int, regions: Sequence[Region]) -> np.ndarray:
    yy, xx = np.mgrid[0:height, 0:width]
    nx = (xx + 0.5) / width
    ny = 1.0 - (yy + 0.5) / height
    scores = []
    for region in regions:
        dx = (nx - region.center[0]) / max(0.015, region.radius[0])
        dy = (ny - region.center[1]) / max(0.015, region.radius[1])
        scores.append(dx * dx + dy * dy)
    return np.stack(scores, axis=0)


def grow_mask(mask: np.ndarray, pixels: int, silhouette: np.ndarray) -> np.ndarray:
    if pixels <= 0:
        return mask & silhouette
    image = Image.fromarray((mask.astype(np.uint8) * 255), mode="L")
    size = pixels * 2 + 1
    grown = np.asarray(image.filter(ImageFilter.MaxFilter(size=size))) > 0
    return grown & silhouette


def crop_layer(view: np.ndarray, mask: np.ndarray, padding: int = 6) -> Tuple[Image.Image, Tuple[int, int, int, int]]:
    ys, xs = np.nonzero(mask)
    if len(xs) == 0:
        return Image.new("RGBA", (2, 2), (0, 0, 0, 0)), (0, 0, 2, 2)
    x0 = max(0, int(xs.min()) - padding)
    y0 = max(0, int(ys.min()) - padding)
    x1 = min(view.shape[1], int(xs.max()) + padding + 1)
    y1 = min(view.shape[0], int(ys.max()) + padding + 1)
    cropped = view[y0:y1, x0:x1].copy()
    alpha = cropped[:, :, 3].astype(np.uint16)
    local_mask = mask[y0:y1, x0:x1].astype(np.uint16)
    cropped[:, :, 3] = (alpha * local_mask).clip(0, 255).astype(np.uint8)
    return Image.fromarray(cropped, mode="RGBA"), (x0, y0, x1, y1)


def overlay_mask_for(name: str, view: np.ndarray) -> np.ndarray:
    h, w = view.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    nx = (xx + 0.5) / w
    ny = 1.0 - (yy + 0.5) / h
    alpha = view[:, :, 3] > ALPHA_THRESHOLD
    rgb = view[:, :, :3].astype(np.int16)
    if name == "ShirtHem":
        return alpha & (ny > 0.405) & (ny < 0.495) & (nx > 0.20) & (nx < 0.80)
    if name == "ChinSoft":
        return alpha & (ny > 0.750) & (ny < 0.825) & (nx > 0.34) & (nx < 0.66)
    if name == "Hair":
        darkness = rgb.mean(axis=2) < 105
        return alpha & darkness & (ny > 0.835) & (nx > 0.30) & (nx < 0.70)
    return np.zeros((h, w), dtype=bool)


def face_patch(view: np.ndarray, anchor: Tuple[float, float], size: Tuple[float, float], kind: str) -> Tuple[Image.Image, Dict[str, float]]:
    h, w = view.shape[:2]
    pw = max(10, int(w * size[0]))
    ph = max(8, int(h * size[1]))
    cx = int(anchor[0] * w)
    cy = int((1.0 - anchor[1]) * h)
    x0 = max(0, cx - pw // 2)
    y0 = max(0, cy - ph // 2)
    x1 = min(w, x0 + pw)
    y1 = min(h, y0 + ph)
    patch = Image.new("RGBA", (x1 - x0, y1 - y0), (0, 0, 0, 0))
    draw = ImageDraw.Draw(patch, "RGBA")

    sample = view[max(0, cy - ph):min(h, cy + ph), max(0, cx - pw):min(w, cx + pw)]
    valid = sample[:, :, 3] > 160
    skin_pixels = sample[:, :, :3][valid]
    if len(skin_pixels):
        warm = skin_pixels[(skin_pixels[:, 0] > skin_pixels[:, 1]) & (skin_pixels[:, 1] > skin_pixels[:, 2] * 0.65)]
        if len(warm):
            skin_pixels = warm
        skin = tuple(np.median(skin_pixels, axis=0).astype(np.uint8).tolist()) + (245,)
    else:
        skin = (190, 130, 96, 245)

    margin = max(1, int(min(patch.size) * 0.07))
    draw.rounded_rectangle((margin, margin, patch.width - margin - 1, patch.height - margin - 1), radius=max(2, ph // 3), fill=skin)
    dark = (55, 34, 27, 255)
    white = (232, 224, 208, 255)
    if kind == "eye_open":
        draw.ellipse((pw * 0.18, ph * 0.25, pw * 0.82, ph * 0.78), fill=white, outline=dark, width=max(1, pw // 18))
        draw.ellipse((pw * 0.46, ph * 0.34, pw * 0.61, ph * 0.70), fill=dark)
    elif kind == "eye_closed":
        draw.arc((pw * 0.18, ph * 0.28, pw * 0.82, ph * 0.78), start=8, end=172, fill=dark, width=max(2, pw // 12))
    elif kind == "mouth_neutral":
        draw.arc((pw * 0.18, ph * 0.30, pw * 0.82, ph * 0.72), start=15, end=165, fill=dark, width=max(2, pw // 16))
    elif kind == "mouth_open":
        draw.ellipse((pw * 0.24, ph * 0.25, pw * 0.76, ph * 0.78), fill=(62, 24, 25, 255), outline=dark, width=max(1, pw // 20))
    elif kind == "mouth_strain":
        draw.line((pw * 0.18, ph * 0.64, pw * 0.82, ph * 0.40), fill=dark, width=max(2, pw // 13))
    elif kind == "mouth_yawn":
        draw.ellipse((pw * 0.30, ph * 0.08, pw * 0.70, ph * 0.90), fill=(55, 20, 24, 255), outline=dark, width=max(1, pw // 20))

    return patch, {
        "anchorX": anchor[0],
        "anchorY": anchor[1],
        "pivotX": 0.5,
        "pivotY": 0.5,
        "cropWidth": patch.width,
        "cropHeight": patch.height,
    }


def write_layer(view_name: str, part_name: str, image: Image.Image) -> str:
    folder = OUTPUT / "Common" / view_name
    folder.mkdir(parents=True, exist_ok=True)
    path = folder / f"{part_name}.png"
    image.save(path, optimize=True)
    return str(path.relative_to(ROOT / "Assets/Resources")).replace("\\", "/")[:-4]


def part_manifest(region: Region, crop: Tuple[int, int, int, int], view_width: int, view_height: int, resource: str) -> Dict[str, object]:
    x0, y0, x1, y1 = crop
    anchor_px_x = region.anchor[0] * view_width
    anchor_px_y = (1.0 - region.anchor[1]) * view_height
    pivot_x = (anchor_px_x - x0) / max(1, x1 - x0)
    pivot_y = 1.0 - ((anchor_px_y - y0) / max(1, y1 - y0))
    return {
        "name": region.name,
        "resource": resource,
        "driver": region.driver,
        "parent": region.parent,
        "anchorX": round(region.anchor[0], 6),
        "anchorY": round(region.anchor[1], 6),
        "pivotX": round(float(np.clip(pivot_x, -0.5, 1.5)), 6),
        "pivotY": round(float(np.clip(pivot_y, -0.5, 1.5)), 6),
        "cropWidth": x1 - x0,
        "cropHeight": y1 - y0,
        "sort": region.sort,
        "rotationGain": region.rotation_gain,
        "maxRotation": region.max_rotation,
        "translationGain": region.translation_gain,
        "maxTranslation": region.max_translation,
        "scaleGain": region.scale_gain,
        "maxScaleDelta": region.max_scale_delta,
        "faceGroup": "",
        "faceState": "",
    }


def generate_view(view_name: str, image: Image.Image) -> Dict[str, object]:
    view = np.asarray(image.convert("RGBA"))
    h, w = view.shape[:2]
    silhouette = view[:, :, 3] > ALPHA_THRESHOLD
    regions = VIEW_REGIONS[view_name]
    assignment = elliptical_scores(w, h, regions).argmin(axis=0)
    parts: List[Dict[str, object]] = []

    for index, region in enumerate(regions):
        mask = silhouette & (assignment == index)
        grow = 5 if any(token in region.name for token in ("Arm", "Forearm", "Hand", "Thigh", "Shin", "Foot")) else 3
        mask = grow_mask(mask, grow, silhouette)
        layer, crop = crop_layer(view, mask, padding=7)
        resource = write_layer(view_name, region.name, layer)
        parts.append(part_manifest(region, crop, w, h, resource))

    overlays = (
        ("ShirtHem", "Bone.ShirtHem", "Belly", 72, (0.50, 0.500)),
        ("ChinSoft", "Bone.ChinSoft", "Head", 86, (0.50, 0.790)),
        ("Hair", "Bone.Head", "Head", 88, (0.50, 0.790)),
    )
    for name, driver, parent, sort, anchor in overlays:
        mask = overlay_mask_for(name, view)
        if not mask.any():
            continue
        mask = grow_mask(mask, 2, silhouette)
        layer, crop = crop_layer(view, mask, padding=5)
        resource = write_layer(view_name, name, layer)
        region = Region(name, anchor, (0.1, 0.1), anchor, driver, parent, sort, 0.16, 4, 0.16, 6, 0.22, 0.07)
        parts.append(part_manifest(region, crop, w, h, resource))

    if view_name != "Back":
        if view_name == "Front":
            face_specs = (
                ("EyeL_Open", (0.455, 0.875), (0.105, 0.045), "eye_open", "EyeL", "Open", 96),
                ("EyeL_Closed", (0.455, 0.875), (0.105, 0.045), "eye_closed", "EyeL", "Closed", 97),
                ("EyeR_Open", (0.545, 0.875), (0.105, 0.045), "eye_open", "EyeR", "Open", 96),
                ("EyeR_Closed", (0.545, 0.875), (0.105, 0.045), "eye_closed", "EyeR", "Closed", 97),
                ("Mouth_Neutral", (0.500, 0.815), (0.145, 0.052), "mouth_neutral", "Mouth", "Neutral", 98),
                ("Mouth_Open", (0.500, 0.815), (0.145, 0.060), "mouth_open", "Mouth", "Open", 99),
                ("Mouth_Strain", (0.500, 0.815), (0.145, 0.052), "mouth_strain", "Mouth", "Strain", 99),
                ("Mouth_Yawn", (0.500, 0.815), (0.145, 0.080), "mouth_yawn", "Mouth", "Yawn", 99),
            )
        else:
            face_specs = (
                ("Eye_Open", (0.590, 0.875), (0.110, 0.045), "eye_open", "Eye", "Open", 96),
                ("Eye_Closed", (0.590, 0.875), (0.110, 0.045), "eye_closed", "Eye", "Closed", 97),
                ("Mouth_Neutral", (0.640, 0.815), (0.145, 0.052), "mouth_neutral", "Mouth", "Neutral", 98),
                ("Mouth_Open", (0.640, 0.815), (0.145, 0.060), "mouth_open", "Mouth", "Open", 99),
                ("Mouth_Strain", (0.640, 0.815), (0.145, 0.052), "mouth_strain", "Mouth", "Strain", 99),
                ("Mouth_Yawn", (0.640, 0.815), (0.145, 0.080), "mouth_yawn", "Mouth", "Yawn", 99),
            )
        for name, anchor, size, kind, group, state, sort in face_specs:
            patch, info = face_patch(view, anchor, size, kind)
            resource = write_layer(view_name, name, patch)
            parts.append({
                "name": name,
                "resource": resource,
                "driver": "Bone.Head",
                "parent": "Head",
                "anchorX": round(info["anchorX"], 6),
                "anchorY": round(info["anchorY"], 6),
                "pivotX": 0.5,
                "pivotY": 0.5,
                "cropWidth": info["cropWidth"],
                "cropHeight": info["cropHeight"],
                "sort": sort,
                "rotationGain": 0.0,
                "maxRotation": 0.0,
                "translationGain": 0.0,
                "maxTranslation": 0.0,
                "scaleGain": 0.0,
                "maxScaleDelta": 0.0,
                "faceGroup": group,
                "faceState": state,
            })

    return {"name": view_name, "width": w, "height": h, "parts": parts}


def main() -> None:
    if not SOURCE.exists():
        raise SystemExit(f"Missing source turnaround: {SOURCE}")
    image = Image.open(SOURCE).convert("RGBA")
    bounds = find_view_bounds(image)
    if OUTPUT.exists():
        shutil.rmtree(OUTPUT)
    OUTPUT.mkdir(parents=True, exist_ok=True)

    views = [generate_view(view_name, image.crop(bound)) for view_name, bound in zip(("Front", "Side", "Back"), bounds)]
    manifest = {
        "version": 36,
        "displayHeight": 1080.0,
        "bodyOffsetX": 0.0,
        "bodyOffsetY": -28.0,
        "stages": [
            {"index": 0, "scale": 1.000},
            {"index": 1, "scale": 0.985},
            {"index": 2, "scale": 0.970},
            {"index": 3, "scale": 0.955},
        ],
        "views": views,
    }
    MANIFEST.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Generated {sum(len(v['parts']) for v in views)} separate layered sprites in {OUTPUT}")


if __name__ == "__main__":
    main()
