# Patch 4 Fat Man Art Source

This directory is reserved for the new original hand-drawn Patch 4 character.

## Current state

The neutral front master has been approved and exported as a 1024 × 1536 transparent RGBA PNG. It has also been uploaded to Adobe Creative Cloud and converted to an editable SVG trace.

The master is still a single visual reference and must be manually separated into the layer contract below before Unity Sprite Skin import. Do not place the flattened master or concept sheet into the runtime prefab as the final character.

Source metadata, checksum, Adobe references and Figma node IDs are stored in `master-source.json`.

## Required source groups

```text
Body/TorsoBase
Body/BellyFront
Body/ChestSoft
Body/Neck
Head/HeadBase
Head/EarL
Head/EarR
Face/BrowL
Face/BrowR
Face/EyeWhiteL
Face/EyeWhiteR
Face/IrisL
Face/IrisR
Face/LidL
Face/LidR
Face/Nose
Face/MouthClosed
Face/MouthOpen
Face/MouthSmile
Face/CheekL
Face/CheekR
ArmL/Upper
ArmL/Forearm
ArmL/Hand
ArmR/Upper
ArmR/Forearm
ArmR/Hand
LegL/Thigh
LegL/Shin
LegL/Foot
LegR/Thigh
LegR/Shin
LegR/Foot
Clothes/ShirtBase
Clothes/ShirtBellyOverlay
Clothes/Bottoms
Clothes/Shoes
FX/Sweat
FX/ImpactFold
FX/Shadow
```

## Production rules

- Transparent background.
- Organic illustrated contours; no visible basic-shape body construction.
- At least 24 px hidden overlap beneath connected layers at target import resolution.
- Neutral front pose is the first riggable master.
- Arms must remain separated enough from the torso for deformation.
- Face layers must remain independently animatable.
- Belly, chest, cheeks, and shirt hem require soft deformation zones.
- Reassembled neutral layers must match the approved master before weight painting.
- Preserve the Patch 3.5 prefab as rollback until Patch 4 validation passes.

## Protected systems

Character art work must not change the main menu, `MainMenuLoop.mp4`, music, audio settings, or general settings systems.
