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

## 2026-07-28 — P4.0-A master and rig foundation

### Art completed

- [x] Created the clean neutral front-pose master.
- [x] Exported it as a 1024 × 1536 RGBA PNG with real transparency.
- [x] Uploaded the master to Adobe Creative Cloud.
- [x] Produced an Adobe editable SVG vector trace.
- [x] Recorded the raster SHA-256, Adobe asset ID and Figma node IDs in `master-source.json`.
- [x] Created the Figma page `P4.0-B Layer Map & Rig`.
- [x] Created the full skeleton overlay and production layer contract on board `6:3`.
- [x] Defined minimum 24 px hidden joint overlap and Sprite Skin preparation rules.

## 2026-07-28 — P4.0-B runtime and editor pipeline

### Runtime completed

- [x] Added `Patch4RigContract` with mandatory bones, layers and clips.
- [x] Added `Patch4CharacterRigController` with safe Patch 3.5 rollback.
- [x] Added `Patch4CharacterStateMachine` gameplay-to-Animator bridge.
- [x] Added `Patch4FaceController` for random blink and independent mouth poses.
- [x] Added `Patch4SecondaryMotionController` for additive soft-body motion.
- [x] Added `Patch4CharacterVisibilityGuard` to prevent stacked character systems.
- [x] Added `Patch4LayerCatalog` and `Patch4LayerRenderer`.
- [x] Added `Patch4LegacySignalBridge` for tap, movement, routine and stage signals.

### Editor automation completed

- [x] Added required-bone, clip and protected-path validation.
- [x] Added deterministic transparent PNG import settings.
- [x] Added exact skeleton-aligned pivot metadata for 1024 × 1536 layer exports.
- [x] Added automatic layer-catalog generation.
- [x] Added generation of all ten mandatory animation clips.
- [x] Added generation and sanitizing of the Patch 4 Animator Controller.
- [x] Added automatic skeleton and isolated prefab generation.
- [x] Added a safe scene installer that binds to the existing character in rollback mode.
- [x] Added `Docs/Patch4/CHECKPOINT.md` as the canonical continuation state.

### Current status

The new code path is structurally ready, but Patch 4 activation remains disabled. The approved master must still be converted into the complete set of independent painted PNG layers. Unity compilation and Play Mode verification have not yet been run in the actual editor.

### P4.0-C next tasks

- [ ] Separate the neutral front master into the full canonical layer set.
- [ ] Reconstruct hidden anatomy and clothing beneath every joint cut.
- [ ] Add at least 24 px hidden overlap beneath connected layers.
- [ ] Export every layer as a full-canvas 1024 × 1536 transparent PNG using the contract filename.
- [ ] Rebuild the layer catalog and prefab in Unity.
- [ ] Compile the new runtime/editor scripts in Unity 6000.3.19f1.
- [ ] Test all ten animations and the legacy signal bridge in the room.
- [ ] Paint Sprite Skin weights for belly, chest, cheeks and shirt hem after rigid-layer validation.
- [ ] Activate Patch 4 only after contract, deformation and rollback validation pass.

### Do not touch

- `MainMenuLoop.mp4`
- main-menu scenes and prefabs
- music and audio configuration
- settings UI and persistence
