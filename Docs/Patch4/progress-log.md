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

## 2026-07-28 — P4.0-A master and P4.0-B foundation

### Art completed

- [x] Created the clean neutral front-pose master.
- [x] Exported it as a 1024 × 1536 RGBA PNG with real transparency.
- [x] Uploaded the master to Adobe Creative Cloud.
- [x] Produced an Adobe editable SVG vector trace.
- [x] Recorded the raster SHA-256, Adobe asset ID and Figma node IDs in `master-source.json`.
- [x] Created the Figma page `P4.0-B Layer Map & Rig`.
- [x] Created the full skeleton overlay and production layer contract on board `6:3`.
- [x] Defined minimum 24 px hidden joint overlap and Sprite Skin preparation rules.

### Runtime foundation completed

- [x] Added `Patch4RigContract` with mandatory bones, layers and clips.
- [x] Added `Patch4CharacterRigController` with safe Patch 3.5 rollback.
- [x] Added `Patch4CharacterStateMachine` gameplay-to-Animator bridge.
- [x] Added `Patch4FaceController` for random blink and independent mouth poses.
- [x] Added `Patch4SecondaryMotionController` for additive belly, chest, cheek and shirt motion.
- [x] Added `Patch4CharacterVisibilityGuard` to prevent stacked character systems.
- [x] Added editor validation for required bones and clips.
- [x] Added a Git protected-path validation command.

### Current status

The neutral master and skeleton architecture are now fixed. Patch 4 runtime activation remains disabled until the master is manually cut into the required independent layers, hidden joint artwork is reconstructed, Sprite Skin weights are painted and all mandatory animation clips exist. Patch 3.5 remains the active rollback character.

### Next tasks

- [ ] Separate the neutral front master into the full deformable layer contract.
- [ ] Reconstruct hidden anatomy and clothing beneath every joint cut.
- [ ] Add at least 24 px hidden overlap beneath connected layers.
- [ ] Export transparent individual layer PNGs or a compatible layered source.
- [ ] Import Sprite Library and Sprite Skin assets into Unity.
- [ ] Paint rigid limb weights and soft-body weight zones.
- [ ] Create the ten mandatory animation clips and Animator Controller.
- [ ] Build the Patch 4 prefab behind the activation flag.
- [ ] Run deformation, face attachment, safe-bound and rollback validation.

### Do not touch

- `MainMenuLoop.mp4`
- main-menu scenes and prefabs
- music and audio configuration
- settings UI and persistence
