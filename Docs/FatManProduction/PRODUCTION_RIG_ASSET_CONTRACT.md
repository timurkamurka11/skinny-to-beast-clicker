# Production Fat Man 2D Rig — Asset Contract 3.7

## Honest boundary

A polished Lamar-like character cannot be reconstructed from one flattened PNG by code alone. The final character must be authored as layered art and manually rigged/weighted. Runtime code can import, validate, animate and display that rig, but it cannot invent clean hidden anatomy, joint overlap and production-quality vertex weights from a single turnaround.

Patch 3.7 therefore does two things:

1. disables every broken auto-cut Patch 3.6 visual;
2. provides a production host for a real Unity 2D Animation prefab.

Until the prefab below is supplied, the game intentionally uses the intact whole-body painted sprite as a temporary non-animated fallback.

## Required delivery

Place the final prefab at:

`Assets/Resources/Characters/FatManProduction/FatManProductionRig.prefab`

Preferred source file:

`Assets/Art/Characters/FatManProduction/FatManProductionRig.psb`

The project already contains Unity 2D Animation and 2D PSD Importer packages.

## Art source requirements

The artist must deliver a layered PSB, not a flattened PNG. Minimum layers:

- pelvis/shorts;
- belly and shirt hem;
- chest/torso;
- head, hair and chin;
- left/right upper arms;
- left/right forearms;
- left/right hands;
- left/right thighs;
- left/right shins;
- left/right feet;
- eyes open/closed;
- eyebrows;
- mouth neutral/open/strain/yawn.

Every joint needs painted overlap underneath the neighbouring part. Hidden shoulder, elbow, hip, knee, neck and ankle areas must be drawn. White matte, rectangular backgrounds and AI cut-out remnants are forbidden.

Recommended master size: 2500–3500 px character height. Transparent background. Keep the neutral pose relaxed and slightly bent rather than perfectly straight.

## Required authored skeleton

The prefab must contain real SpriteSkin components with manual geometry and weights. Required logical bones:

- Root
- Pelvis
- Spine
- Chest
- Belly
- ShirtHem
- Neck
- Head
- ChinSoft
- UpperArm.L / Forearm.L / Hand.L
- UpperArm.R / Forearm.R / Hand.R
- Thigh.L / Shin.L / Foot.L
- Thigh.R / Shin.R / Foot.R

Recommended optional soft bones:

- Belly.Left / Belly.Center / Belly.Right
- ChestSoft.Left / ChestSoft.Right
- ShirtHem.Left / ShirtHem.Right
- ChinSoft.Left / ChinSoft.Right

The production rig must not reuse the coordinates, meshes or weights of `CharacterRigController`'s procedural mannequin.

## Weighting rules

- torso and pelvis must retain volume during rotation;
- belly weights blend between Pelvis, Spine and Belly soft bones;
- shirt hem follows belly with delayed secondary motion;
- shoulders use broad overlap and smooth falloff;
- elbows and knees use manually corrected geometry;
- hands and feet remain rigid near their ends;
- head and facial sprites never receive leg/torso weights;
- no automatic global weight generation is accepted without manual cleanup.

## Animator contract

The production Animator may expose these optional parameters. Patch 3.7 drives parameters only when they exist:

- `Facing` — int: 0 front, 1 left, 2 right, 3 back;
- `Stage` — int: 0–3;
- `Speed` — float: 0 idle, 1 walking;
- `Tap` — bool;
- `Action` — int matching `CharacterRoutineAction`.

Required clips/state coverage:

- Idle breathing;
- Blink via sprite swap or facial bones;
- Tap reaction;
- Walk;
- Look around;
- Scratch;
- Yawn;
- Stretch;
- Flex;
- Adjust clothes;
- Warm shoulders;
- Sit down / sit loop / stand up.

## View strategy

Best quality: separately authored Front, Side and Back artwork using Sprite Library swaps while sharing a compatible skeleton. Mirroring one side view is acceptable only when clothing and lighting are symmetrical.

Do not deform the front illustration into a side or back view.

## Prefab validation

`FatManProductionRig.prefab` is accepted only when it contains:

- at least one Animator;
- at least one SpriteSkin;
- each accepted SpriteSkin has a SpriteRenderer and non-empty boneTransforms;
- at least one visible SpriteRenderer;
- no white background cards;
- no CharacterMeshGraphic;
- no Patch 3.6 runtime cut-outs.

## What to send for final integration

Any one of these is sufficient:

1. layered `.psb` plus the original high-resolution turnaround/reference;
2. a Unity package containing the already rigged prefab, SpriteSkin data and Animator Controller;
3. a Spine project plus exported JSON/binary, atlas and textures, if the project deliberately switches to spine-unity.

A set of flattened front/side/back PNG files is not sufficient for production-quality skeletal animation unless an artist also supplies separated parts, hidden overlap art, meshes and weights.
