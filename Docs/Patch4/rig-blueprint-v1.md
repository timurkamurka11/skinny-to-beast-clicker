# GameWork Patch 4.0 — Rig Blueprint v1

## Current production slice

The approved neutral front master is a 1024 × 1536 RGBA PNG with real transparency. It is suitable as the visual reference for manual layer cutting, but it is not yet a runtime-ready Sprite Skin source.

- Raster master SHA-256: `5873cf6df0df2b5ebd4947b687693162d4b34899202326d1b1ae62df9f50587c`
- Adobe Creative Cloud asset: `urn:aaid:sc:AP:5086d367-0290-430e-b9a7-39e5392bdbde`
- Adobe vector trace: https://to.adobe.com/aN0OeN9oa589DR97
- Figma rig blueprint: https://www.figma.com/design/tZSr9vinRs9EbZzgatxjda/GameWork-Patch-4-0-Fat-Man-Art-Rig?node-id=6-3

## New skeleton hierarchy

```text
Root
└── CharacterRoot
    ├── Pelvis
    │   ├── SpineLower
    │   │   ├── BellyBase
    │   │   │   └── BellyTip
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

The Figma overlay uses separate colors for rigid anatomical bones, facial controls and soft-body controls. The final Unity hierarchy must use the exact names in `Patch4RigContract`.

## Layer-cut order

1. Cut and reconstruct hidden torso beneath the head, neck, arms and belly overlay.
2. Separate each arm into upper arm, forearm and hand with at least 24 px hidden joint overlap.
3. Separate each leg into thigh, shin and foot with at least 24 px hidden overlap.
4. Split shirt base from the belly/shirt-hem overlay so cloth can settle after torso acceleration.
5. Separate head base, ears, brows, eye whites, irises, lids, nose, cheeks and three mouth poses.
6. Keep FX layers outside anatomical Sprite Skin groups.
7. Validate the reassembled neutral pose pixel-for-pixel before weight painting.

## Runtime foundation added

The following isolated components now exist under `Assets/GameWorkPatch4/Runtime/`:

- `Patch4RigContract`
- `Patch4CharacterRigController`
- `Patch4CharacterStateMachine`
- `Patch4FaceController`
- `Patch4SecondaryMotionController`
- `Patch4CharacterVisibilityGuard`

Patch 4 activation fails safely when any required bone is missing. Patch 3.5 remains visible as rollback until the Patch 4 rig passes validation.

## Editor validation

`Tools → GameWork → Patch 4.0 → Validate Selected Rig` checks the required skeleton and animation names.

`Tools → GameWork → Patch 4.0 → Verify Protected Paths` runs a Git diff against `main` and reports any change to protected menu, media, music, audio-mixer or settings paths.

## Still required before enabling Patch 4

- Layered transparent art exports.
- Sprite Library and Sprite Skin setup.
- Weight painting.
- Animator Controller containing all ten mandatory clips.
- Prefab integration behind the Patch 4 activation flag.
- Runtime deformation, screen-bound and rollback tests.
