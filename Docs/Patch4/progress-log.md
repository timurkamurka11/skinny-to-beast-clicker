# GameWork Patch 4.0 — Progress Log

## 2026-07-28 — P4.0-A started

### Completed

- Confirmed isolated branch `patch-4.0` based on `main` commit `6226d7891c2c706d510c7d376c1a58a6c96b4202`.
- Kept the protected menu, video, music, and settings scope unchanged.
- Generated the first five-view directional character sheet with Adobe Firefly.
- Added a dedicated Figma page: `P4.0-A Concept Sheet`.
- Added the annotated production board: `P4.0-A Concept Sheet v1`.
- Recorded accepted design direction and known redraw requirements.
- Added `Docs/Patch4/concept-sheet-v1.md`.

### Current status

The visual direction is accepted as a starting point, but the generated sheet is not yet valid final rig art. The exact back view and true three-quarter views still require controlled redraw. The current Adobe image remains a reference, not a one-piece Unity sprite.

### Next tasks

- [ ] Create a clean neutral front-pose master.
- [ ] Separate the front master into deformable layer groups.
- [ ] Add at least 24 px hidden overlap beneath neighboring joint layers.
- [ ] Build the new Patch 4 skeleton blueprint.
- [ ] Define Sprite Skin weight zones for belly, chest, cheeks, and shirt hem.
- [ ] Create transparent source exports.
- [ ] Add the first Unity import manifest under `Assets/GameWorkPatch4/`.

### Do not touch

- `MainMenuLoop.mp4`
- main-menu scenes and prefabs
- music and audio configuration
- settings UI and persistence
