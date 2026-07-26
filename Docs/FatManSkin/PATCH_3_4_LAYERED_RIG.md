# Real Fat Man Layered Rig Patch 3.4

Patch 3.4 replaces the visible flat body from 3.3 with a continuous weighted UI mesh.

## Runtime model

- `CharacterSpriteRigController` keeps loading front/side/back art, stage scale, screen fitting and visibility bounds.
- `CharacterLayeredRigController` disables the flat source `Image` and uses it only as direction/texture data.
- `CharacterSkinnedSpriteGraphic` builds a 24 x 38 continuous mesh and blends every vertex across up to four existing bones.
- Arm, forearm, hand, thigh, shin, foot, pelvis, spine, chest, belly, shirt hem, neck, head and soft-body bones deform the visible artwork.
- No rectangular limb crops are created, so neighbouring body parts cannot be duplicated.
- Blinking and tap/yawn/flex/stretch mouth reactions are attached to `Bone.Head`.

## Unity verification

1. Run `Tools > Skinny to Beast > Bake Real Fat Man Layered Rig 3.4`.
2. Run `Tools > Skinny to Beast > Validate Real Fat Man Layered Rig 3.4`.
3. Enter Play Mode and press START.
4. Confirm breathing and belly/chest movement while idle.
5. Confirm visible arm and leg deformation during tap, idle actions and room travel.
6. Confirm random blinking, mouth reactions, Stage 1-4 and front/side/back views.
7. Confirm the room opens and no procedural mannequin is visible.
