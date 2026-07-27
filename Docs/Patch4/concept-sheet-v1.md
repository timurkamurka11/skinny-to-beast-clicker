# GameWork Patch 4.0 — Concept Sheet v1

## Status

Milestone: `P4.0-A — Art and rig foundation`

State: **directional concept captured; not final layered rig art**.

The first production turnaround reference for the original overweight male character has been generated with Adobe Firefly and recorded in the Patch 4.0 Figma file.

## Design links

- Figma file: https://www.figma.com/design/tZSr9vinRs9EbZzgatxjda/GameWork-Patch-4-0-Fat-Man-Art-Rig
- Figma concept board node: https://www.figma.com/design/tZSr9vinRs9EbZzgatxjda/GameWork-Patch-4-0-Fat-Man-Art-Rig?node-id=4-3
- Adobe concept asset: https://photoshop-api.adobe.io/v2/short-url/urn:aaid:ps:US:3c11c8c8-9044-48d3-81ab-2becc422903d

## Accepted art direction

- Original adult overweight man; no Lamar tracing or asset reuse.
- Heavy hanging belly and broad soft torso.
- Thick upper arms and large thighs.
- Soft jawline and short dark hair.
- Dirty gray sleeveless shirt.
- Loose dark sweatpants.
- Simple sneakers.
- Hand-drawn mobile-game rendering rather than basic-shape construction.

## Known issues in v1

This image is a directional turnaround reference only. It must not be imported as the final one-piece game sprite.

Required corrections before rigging:

1. Draw an exact rear view facing fully away from the camera.
2. Replace repeated side profiles with true three-quarter views.
3. Separate arms farther from the torso for clean deformation.
4. Standardize hands, feet, face, clothing folds, and body proportions across every view.
5. Remove the studio background and prepare transparent source art.
6. Create clean hidden overlaps beneath shoulders, elbows, wrists, hips, knees, ankles, neck, belly, and shirt hem.
7. Produce the neutral front pose as a layered source matching the Patch 4 layer contract.

## Next production slice

1. Neutral front-pose redraw.
2. Layer map and overlap guide.
3. New skeleton blueprint.
4. Layered source export manifest.
5. Unity Sprite Skin import only after the art review passes.

## Protected scope

This concept-sheet step does not modify:

- `MainMenuLoop.mp4`
- main-menu scenes, prefabs, transitions, or button logic
- music, ambient audio, mixers, or sound settings
- settings screens, persistence, language, vibration, or notifications
