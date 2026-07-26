# Real Fat Man Layered Art Patch 3.6

## Production target

Patch 3.6 replaces every whole-body rendering experiment with a Lamar-like 2D cutout puppet. The painted man is rendered from separate transparent textures and an art-specific hierarchy. The legacy skeleton is retained only as an animation signal source.

## Required visible layers

Every direction contains the same stable body contract:

- Pelvis
- Chest
- Belly
- ShirtHem
- Head
- Hair
- ChinSoft
- UpperArm_L / Forearm_L / Hand_L
- UpperArm_R / Forearm_R / Hand_R
- Thigh_L / Shin_L / Foot_L
- Thigh_R / Shin_R / Foot_R

Front and side additionally contain separate facial states:

- open and closed eyes;
- neutral, open, strain and yawn mouths.

Back has no facial layers.

## Authoring pipeline

`Tools/FatManLayeredArt/generate_layered_art.py` uses the turnaround only as an authoring source. It writes real transparent PNG files under:

`Assets/Resources/Characters/FatManLayered/Generated/Common/{Front|Side|Back}`

`refine_layered_art.py` then:

1. restricts every layer to its anatomical region;
2. removes disconnected fragments;
3. keeps the component attached to the intended joint;
4. tightens each crop;
5. updates the joint pivot in `manifest.json`.

The full turnaround is never rendered by the Patch 3.6 runtime.

## Runtime hierarchy

Each view builds an independent proxy hierarchy:

- Pelvis is the body root;
- Chest, Belly and legs are children of Pelvis;
- Head and arms are children of Chest;
- forearms follow upper arms;
- hands follow forearms;
- shins follow thighs;
- feet follow shins;
- face states follow Head.

Every PNG is a separate `Image` with its own pivot and sorting canvas. Switching direction activates one complete hierarchy and deactivates the other two, so stale heads, feet or facial overlays cannot remain in the room.

## Animation transfer

The old rig does not position the artwork. For each proxy bone Patch 3.6 reads only the legacy bone's delta from its bind pose:

- rotation is multiplied by a part-specific gain and clamped;
- translation is reduced and clamped;
- scale is reduced and clamped;
- child proxy bones inherit their art-correct parent chain.

This preserves idle, breathing, tap reaction, walking, turning, stretch, flex, sit and stand while preventing old mannequin joint coordinates from pulling the painted body apart.

## Face

Blink intervals are randomized. Front uses two independent eye groups, side uses one eye group and back renders no face. Mouth state follows tap, yawn, flex and stretch actions.

## Direction and stages

Front, side and back use separate source layers. SideLeft mirrors the native side set; the old 0.82 mannequin squeeze is disabled. Stage 1–4 preserve one hierarchy and currently use stage scale profiles, while allowing future stage-specific texture replacement without changing the controller contract.

## Legacy cleanup

The prefab disables `CharacterSpriteRigController`. Patch 3.6 also suppresses:

- `Sprite.RealFatManBody`;
- `Sprite.RealFatManLayeredSurface`;
- `LayeredPaintedFaceOverlay`;
- procedural `CharacterMeshGraphic` pixels.

`CharacterSkinnedSpriteGraphic` is removed from the project.

## Acceptance criteria

- no whole-body PNG is visible;
- no full-PNG mesh deformation exists;
- no head or body fragment remains below the character;
- front, side and back each contain one active complete puppet;
- shoulders, elbows, wrists, hips, knees and ankles stay connected;
- idle breathing and body sway are visible;
- arms and legs visibly move during actions and walking;
- blink and mouth states remain attached to the head;
- Stage 1–4 remain functional;
- the visibility gate opens the room and cannot leave a black screen.

## Unity verification

1. `Tools > Skinny to Beast > Bake Real Fat Man Layered Art 3.6`
2. `Tools > Skinny to Beast > Validate Real Fat Man Layered Art 3.6`
3. Test front/side/back in idle, tap, walk, stretch, flex, sit and stand.
4. Test Stage 1–4.
5. Confirm the runtime hierarchy has only `RealFatMan.LayeredArt3_6` as the visible character root.
