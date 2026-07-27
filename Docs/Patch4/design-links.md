# Patch 4.0 Design Workspace

## Figma

GameWork Patch 4.0 — Fat Man Art & Rig

https://www.figma.com/design/tZSr9vinRs9EbZzgatxjda

Key nodes:

- concept sheet board: `4:3`
- layer map and rig blueprint: `6:3`

Current contents:

- original editable fat-man concept built from organic vector paths;
- approved neutral front master reference;
- new art-authored skeleton blueprint;
- complete layer contract and protected-scope notes.

Figma Starter has reached its current MCP write-call limit. The latest attempted P4.0-C status write was rejected atomically and did not modify the file.

## Adobe Photoshop / Firefly

Approved P4.0-C source:

- asset ID: `urn:aaid:sc:AP:aa1abfc7-66c2-4260-a320-6781833d46cb`
- source URL: https://at.adobe.com/SGSnfFAvaBd9wjrT
- canvas: `1024 × 1536` RGBA PNG
- approved SHA-256: `5873cf6df0df2b5ebd4947b687693162d4b34899202326d1b1ae62df9f50587c`

Firefly rigging-parts reference:

https://photoshop-api.adobe.io/v2/short-url/urn:aaid:ps:US:5b427aac-252e-45c2-9a79-272568e505b8

Adobe mask source manifest:

`Assets/GameWorkPatch4/Art/Character/FatMan/Masks/adobe-mask-manifest.json`

The mask pipeline contains valid selections for hair, face, eyebrows, nose, ears, neck, clothing, hands and shoes. Failed whole-subject selections are explicitly invalid and excluded from production download.

## Canva

GameWork Patch 4.0 — P4.0-C Production Status

- editable: https://www.canva.com/d/Zwy2RkpL4DJRJYs
- view-only: https://www.canva.com/d/VESyL19jdqnHkif

Canva is a visual progress report. GitHub `Docs/Patch4/CHECKPOINT.md` remains the canonical continuation source.

## Protected scope

No design operation may change:

- `MainMenuLoop.mp4`
- menu scenes, prefabs, transitions or button logic
- music, ambient audio or audio mixers
- settings UI or persistence
