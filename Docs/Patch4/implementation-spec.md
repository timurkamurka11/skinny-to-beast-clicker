# GameWork Patch 4.0 — Implementation Specification

## Status

Branch: `patch-4.0`
Base: `main` at `6226d7891c2c706d510c7d376c1a58a6c96b4202`

Patch 4.0 replaces the Patch 3.5 bounded procedural puppet with a fully original, hand-drawn, layered 2D character and a new art-authored skeleton. The work is isolated from menu, video, music, and settings systems.

## Hard protection scope

The following assets and systems must not be modified by Patch 4.0:

- `MainMenuLoop.mp4`
- Main menu scenes, prefabs, layout, transitions, and button logic
- Music, ambient audio, sound settings, and mixer configuration
- Settings screens, persistence, vibration, notification, and language settings

Any Patch 4.0 change touching those areas must fail review.

## Character art target

Create one original overweight adult male character for the opening stage of the game.

Requirements:

- Fully illustrated anatomy and clothing; no circles, rectangles, capsules, or other visible primitive/basic-shape construction.
- Distinct silhouette with heavy belly, broad torso, thick upper arms, large thighs, soft jawline, neck folds, and uneven clothing folds.
- Front, 3/4 left, 3/4 right, side, and back art-direction references.
- Layered source suitable for Unity 2D Animation deformation.
- Transparent background and clean overlap beneath neighboring layers.
- Neutral dirty sleeveless shirt, shorts or sweatpants, simple shoes, and an original face.
- Do not imitate or trace Lamar Idle Vlogger assets. Match only the desired production quality and animation richness.

## Source layer contract

The source character must be split into deformable art groups:

- `Body/TorsoBase`
- `Body/BellyFront`
- `Body/ChestSoft`
- `Body/Neck`
- `Head/HeadBase`
- `Head/EarL`, `Head/EarR`
- `Face/BrowL`, `Face/BrowR`
- `Face/EyeWhiteL`, `Face/EyeWhiteR`
- `Face/IrisL`, `Face/IrisR`
- `Face/LidL`, `Face/LidR`
- `Face/Nose`
- `Face/MouthClosed`, `Face/MouthOpen`, `Face/MouthSmile`
- `Face/CheekL`, `Face/CheekR`
- `ArmL/Upper`, `ArmL/Forearm`, `ArmL/Hand`
- `ArmR/Upper`, `ArmR/Forearm`, `ArmR/Hand`
- `LegL/Thigh`, `LegL/Shin`, `LegL/Foot`
- `LegR/Thigh`, `LegR/Shin`, `LegR/Foot`
- `Clothes/ShirtBase`, `Clothes/ShirtBellyOverlay`, `Clothes/Bottoms`, `Clothes/Shoes`
- `FX/Sweat`, `FX/ImpactFold`, `FX/Shadow`

Layer edges hidden under joints require at least 24 px overlap at the target import resolution.

## New skeleton

Patch 4.0 must not reuse the Patch 3.5 runtime procedural skeleton as the final deformation rig. A new art-authored hierarchy is required:

```text
Root
└── CharacterRoot
    ├── Pelvis
    │   ├── SpineLower
    │   │   ├── BellyBase
    │   │   ├── BellyTip
    │   │   └── SpineUpper
    │   │       ├── ChestSoftL
    │   │       ├── ChestSoftR
    │   │       ├── Neck
    │   │       │   └── Head
    │   │       │       ├── Jaw
    │   │       │       ├── BrowL
    │   │       │       ├── BrowR
    │   │       │       ├── EyeL
    │   │       │       └── EyeR
    │   │       ├── ClavicleL
    │   │       │   └── UpperArmL
    │   │       │       └── ForearmL
    │   │       │           └── HandL
    │   │       └── ClavicleR
    │   │           └── UpperArmR
    │   │               └── ForearmR
    │   │                   └── HandR
    │   ├── ThighL
    │   │   └── ShinL
    │   │       └── FootL
    │   └── ThighR
    │       └── ShinR
    │           └── FootR
    └── GroundShadow
```

The belly, chest, cheeks, shirt hem, and secondary folds use additional soft bones or Unity Sprite Skin weights rather than scaling the whole body image.

## Animation set

Minimum new clips:

1. `FatMan_Idle_Breathe` — breathing, belly delay, shirt drag, small head counter-motion.
2. `FatMan_Idle_ShiftWeight` — weight transfer, knee compression, hip drift.
3. `FatMan_Blink_Random` — asymmetrical lids with randomized scheduling.
4. `FatMan_LookAround` — eye movement followed by head and chest.
5. `FatMan_TapReact_01` — short body recoil with belly and chest follow-through.
6. `FatMan_TapReact_02` — irritated hand gesture and facial reaction.
7. `FatMan_Walk_InRoom` — full leg and arm cycle with mass-aware vertical movement.
8. `FatMan_Turn` — controlled directional transition without mirroring facial asymmetry.
9. `FatMan_SitOrLean` — alternate room pose.
10. `FatMan_UpgradeReact` — longer positive reaction.

Animation principles:

- Limbs rotate around anatomical joints, not texture centers.
- Belly and shirt settle 2–5 frames after torso acceleration.
- Head and hands use subtle arcs rather than linear translation.
- Facial animation remains attached during all body poses.
- No static bob-only idle is accepted.

## Unity implementation direction

Create a new isolated path under:

```text
Assets/GameWorkPatch4/
  Art/Character/FatMan/
  Animations/FatMan/
  Prefabs/
  Runtime/
  Editor/
  Tests/
```

Planned runtime components:

- `Patch4CharacterRigController`
- `Patch4CharacterStateMachine`
- `Patch4FaceController`
- `Patch4SecondaryMotionController`
- `Patch4CharacterVisibilityGuard`

The Patch 4 character is introduced behind a feature flag or prefab swap. Patch 3.5 remains available as rollback until Patch 4 acceptance tests pass.

## Non-regression guard

Add an editor/build guard that verifies Patch 4 commits do not change protected menu/media/settings paths. The guard must also verify:

- no visible primitive-rendered body parts;
- required character source layers exist;
- required skeleton bone names exist;
- all mandatory clips exist;
- only one visible body renderer is active;
- face layers follow the head rig;
- character remains inside safe screen bounds;
- Patch 3.5 rollback prefab remains intact during migration.

## Milestones

### P4.0-A — Art and rig foundation

- Original character concept sheet.
- Layer naming and overlap map.
- New skeleton blueprint.
- Neutral front-pose layered source.

### P4.0-B — Unity import

- Sprite Library and Sprite Skin setup.
- Weight painting and deformation validation.
- Prefab and feature-flag integration.

### P4.0-C — Motion

- Idle, blink, look, reactions, walk, turn, alternate pose.
- Secondary belly, shirt, cheek, and chest motion.

### P4.0-D — Validation

- Runtime tests, build guard, screen-bound checks, and rollback verification.

## Acceptance criteria for the first implementation slice

- Branch exists separately from `main`.
- This specification is committed on that branch.
- A new Figma rig/art-direction file exists.
- The first original character concept is started without basic-shape rendering.
- Protected menu/video/music/settings files remain byte-identical to `main`.
