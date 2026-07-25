# Fat Man Skin 3.1

`fat-man-turnaround-reference.png` is the visual source of truth for the
starter character's silhouette, clothing, palette and front/side/back read.
It is documentation only and is intentionally outside `Assets/Resources`.

The runtime character does not display this sheet and does not swap raster
frames. `CharacterRigController` reconstructs the design as separate
texture-free cutout parts attached to the shared skeletal rig.

## Runtime invariants

- one persistent character skeleton in entry and gameplay;
- one active item per skin slot;
- fat-man silhouettes on all core body parts;
- four secondary bones: `Belly`, `ShirtHem`, `ChestSoft`, `ChinSoft`;
- front, mirrored side and back detail sets;
- no `RawImage`, `uvRect`, crop rig or frame animation;
- the original 26 Animator clips remain the primary motion source.

## Reference-generation prompt

The built-in image generator was asked for a polished 2D mobile-game
turnaround of one sympathetic adult overweight man with a large soft belly,
thick limbs, short neck, double chin, tousled dark hair, worn blue-gray tank
top, charcoal shorts and battered house shoes. It required identical front,
right-side and back views, natural facial proportions, visible rig overlap
zones, cel shading, fabric folds and stains, and explicitly excluded a
robot/mannequin, geometric body, chibi anatomy, photorealism and extra limbs.
