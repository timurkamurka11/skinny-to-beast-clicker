# Real Fat Man Rig Rebuild Patch 3.5

## Regression fixed

Patch 3.4 applied unrestricted skeleton matrices from the old procedural body directly to a new painted PNG. Because the old bone placement did not match the fat-man artwork, the texture folded into an hourglass, feet and head appeared detached, and side/back poses exploded.

## New architecture

The old skeleton is now only an animation signal. `CharacterSkinnedSpriteGraphic` remaps those signals onto art-specific normalized anchors for front, side and back views.

Every driver has independent limits for:

- translation;
- rotation;
- scale and soft-body breathing;
- front, side and back view strength.

The painted body uses a 30 × 46 continuous mesh. A permanent root influence keeps all regions attached. Limb weights fade near shoulders and hips so seams remain connected.

## Runtime safety

Before rendering, Patch 3.5:

1. smooths the displacement field;
2. clamps every vertex to a view-specific maximum displacement;
3. constrains the mesh to a small envelope around the original body;
4. detects inverted triangles;
5. restores folded cells toward their original pose.

The face overlay is no longer parented directly to the mismatched old head bone. It follows the same bounded head driver as the painted mesh, preserving blinking and mouth reactions without a detached face.

## Unity verification

1. Run `Tools > Skinny to Beast > Bake Real Fat Man Rig Rebuild 3.5`.
2. Run `Tools > Skinny to Beast > Validate Real Fat Man Rig Rebuild 3.5`.
3. Test front, side and back in idle, walk, tap, stretch, flex, sit and stand.
4. Confirm there are no detached body fragments, inverted hourglass frames or black-screen entry failures.
5. Confirm breathing, belly/chest motion, blinking and mouth reactions remain visible.
