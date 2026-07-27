# Patch 4 Fat Man Art Source

This directory is reserved for the new original hand-drawn Patch 4 character.

## Current state

The Adobe/Figma concept sheet is a reference only. Final game art has not yet been imported here.

Do not place a flattened concept-sheet image into the runtime prefab as the final character.

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
- Preserve the Patch 3.5 prefab as rollback until Patch 4 validation passes.

## Protected systems

Character art work must not change the main menu, `MainMenuLoop.mp4`, music, audio settings, or general settings systems.
