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

Current P4.0-H quality source:

- repository:
  `Assets/GameWorkPatch4/Art/Character/FatMan/FatMan_NeutralFront_Master.png`
- exact Photoshop/Firefly output:
  https://photoshop-api.adobe.io/v2/short-url/urn:aaid:ps:US:72e5364f-ba61-4f62-96f5-51c0d8ac09bf
- canvas: `1024 × 1536` RGBA PNG
- approved SHA-256: `7b151f1ded93f3852bc8a7218ab26f94298b7f822094304bbcea9c076cad72a3`

The previous repository preview was `96 × 144` and is retained only in Git
history. Unity restores the committed 1024 PNG and creates all masks locally;
none of these links is a workflow dependency.

Firefly rigging-parts reference:

https://photoshop-api.adobe.io/v2/short-url/urn:aaid:ps:US:5b427aac-252e-45c2-9a79-272568e505b8

Adobe mask source manifest:

`Assets/GameWorkPatch4/Art/Character/FatMan/Masks/adobe-mask-manifest.json`

The mask pipeline records the historical selections for hair, face, eyebrows,
nose, ears, neck, clothing, hands and shoes. Unity regenerates the ten usable
masks locally. Failed whole-subject selections remain explicitly invalid and
are never treated as production input.

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
