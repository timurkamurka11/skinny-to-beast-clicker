# GameWork Patch 4.0 — Progress Log

## 2026-07-28 — P4.0-A started

### Completed

- Confirmed isolated branch `patch-4.0` based on `main` commit `6226d7891c2c706d510c7d376c1a58a6c96b4202`.
- Kept the protected menu, video, music, and settings scope unchanged.
- Generated the first five-view directional character sheet with Adobe Firefly.
- Added the Figma concept-sheet and rig-blueprint boards.
- Recorded accepted design direction and known redraw requirements.

## 2026-07-28 — P4.0-A master and rig foundation

### Art completed

- [x] Created the clean neutral front-pose master.
- [x] Exported it as a 1024 × 1536 RGBA PNG with real transparency.
- [x] Uploaded the master to Adobe Creative Cloud.
- [x] Produced an Adobe editable SVG vector trace.
- [x] Recorded the raster SHA-256, Adobe asset IDs and Figma node IDs.
- [x] Created the full skeleton overlay and production layer contract.
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

## 2026-07-28 — P4.0-C Adobe masks and painted-layer production pipeline

### Adobe work completed

- [x] Re-uploaded and visually verified the exact transparent neutral master.
- [x] Generated valid masks for hair, face base, eyebrows, nose, ears, neck, upper clothes, lower clothes, hands and shoes.
- [x] Marked failed pupil, mouth, arm and leg selections invalid because Adobe returned the entire subject or an implausibly small region.
- [x] Generated a Firefly rigging-parts reference sheet for manual repaint guidance.
- [x] Added `adobe-mask-manifest.json` with URLs, bounding boxes, validity and fallback rules.

### Unity art pipeline completed

- [x] Added `Patch4AdobeMaskDownloader` to download Adobe sources directly inside Unity.
- [x] Added `Patch4MaskDrivenLayerBaker` to build the complete 40-layer full-canvas draft pack.
- [x] Added bilateral splitting and controlled geometric fallback regions.
- [x] Added `Patch4DraftLayerValidator` for dimensions, coverage, leakage, alpha content and 14 joint-overlap checks.
- [x] Added `Patch4ProductionPipeline` with ordered production commands.
- [x] Added `Patch4ProductionDashboard` with live project status and explicit build buttons.
- [x] Added a detailed P4.0-C manual-art guide.

### Activation safety completed

- [x] Added `Patch4ArtReadinessAsset` as a manual production-art signature.
- [x] Bound readiness approval to the exact approved master SHA-256.
- [x] Updated `Patch4CharacterRigController` so `patch4Enabled` cannot bypass art approval.
- [x] Added `Patch4PrefabReadinessBinder` so regenerated prefabs cannot omit the gate.
- [x] Kept automated draft metadata permanently set to `activationAllowed: false`.

### Visual project status

- [x] Created an editable Canva P4.0-C production-status report.
- [x] Updated GitHub design links with Adobe, Figma and Canva sources.
- [ ] Figma P4.0-C status panel was not added because the Starter MCP call limit was reached; the failed call was atomic and the existing file is intact.

## 2026-07-28 — P4.0-D CI, compilation and smoke verification

### GitHub automation completed

- [x] Added `Assets/GameWorkPatch4/CI/validate_patch4.py`.
- [x] Added `.github/workflows/patch4-static-guard.yml`.
- [x] Static guard validates contract counts, uniqueness, master SHA, JSON manifests, readiness lock and protected paths.
- [x] Static guard clearly does not claim Unity compilation.

### Unity verification tools completed

- [x] Added `Patch4CompilationMonitor`.
- [x] Compilation reports are written to `Library/GameWorkPatch4Reports/patch4-compilation-report.json`.
- [x] Added `Patch4EditorSmokeValidator`.
- [x] Editor smoke reports are written to `Library/GameWorkPatch4Reports/patch4-editor-smoke-report.json`.
- [x] Added compilation and smoke-report status to the Production Dashboard.
- [x] Extended the Production Pipeline to seven ordered steps.

### Automated tests completed

- [x] Added isolated EditMode test assembly.
- [x] Added tests for contract counts, uniqueness, critical entries and readiness SHA matching.
- [x] Added isolated PlayMode test assembly.
- [x] Added tests proving unapproved art cannot activate Patch 4.
- [x] Added tests proving an incomplete skeleton cannot be bypassed.
- [x] Added tests proving an approved complete rig switches visibility and restores rollback when disabled.
- [x] Added `Docs/Patch4/P4_0_D_VERIFICATION.md`.

### Current honest status

The verification source code is committed, but Unity has not physically compiled or run the tests yet. No passing compilation, EditMode or PlayMode result is claimed. Patch 4 remains disabled.

### Immediate manual tasks

- [ ] Open branch `patch-4.0` in Unity `6000.3.19f1`.
- [ ] Wait for compilation and inspect `patch4-compilation-report.json`.
- [ ] Run the Production Dashboard from step 1 through step 6.
- [ ] Run all EditMode tests.
- [ ] Run all PlayMode tests.
- [ ] Download Adobe sources and bake the draft pack.
- [ ] Inspect `layer-bake-report.json`.
- [ ] Repaint hidden neck, shoulder, elbow, wrist, hip, knee, ankle, belly and shirt-hem continuations.
- [ ] Paint real open-mouth, smile, eyelid, iris and cheek layers.
- [ ] Reassemble the neutral pose and compare it with the approved master.
- [ ] Re-run validation and rebuild the locked prefab.
- [ ] Test all ten animations in the actual room.
- [ ] Paint Sprite Skin weights after rigid-layer continuity passes.
- [ ] Approve `Patch4ArtReadiness.asset` only after the exact master and every test pass.

### Do not touch

- `MainMenuLoop.mp4`
- main-menu scenes and prefabs
- music and audio configuration
- settings UI and persistence

## 2026-07-30 — P4.0-H exact quality-master replacement

- [x] Verified the previous embedded source was only `96 × 144`.
- [x] Re-rendered and cut out the character with Adobe Photoshop/Firefly.
- [x] Produced a real `1024 × 1536` transparent RGBA master.
- [x] Committed the exact master bytes under the isolated Patch 4 art root.
- [x] Replaced GPU upscaling with SHA-checked byte-for-byte local restoration.
- [x] Removed `Patch4EmbeddedArtSource`.
- [x] Kept all Adobe URLs out of the Unity execution path.
- [x] Made one pull automatically restore masks, rebake all 40 layers, rebuild
  the locked prefab, validate and run EditMode plus PlayMode tests.
- [x] Excluded the runtime-only ground shadow from master pixel comparison while
  preserving it in the runtime layer pack.
- [x] Kept `productionArtApproved` false and Patch 3.5 visible.
- [x] Confirmed the new quality pass in Unity `6000.3.19f1` with the automatic
  `4/4` run, zero warnings/errors and three-panel screenshot.
- [x] User confirmed the master and assembled pose were many times sharper.

## 2026-07-30 — P4.0-I joint and face candidates

- [x] Replaced radius-five joint disks with texture-preserving elliptical
  continuations for every required moving seam.
- [x] Raised ordinary overlap validation to 180 pixels and belly/shirt overlap
  to 360 pixels.
- [x] Added deterministic skin underlay beneath independent eyes and mouth.
- [x] Restored exact neutral eye and closed-mouth patches from the quality
  master.
- [x] Added painted closed-lid, open-mouth and smile candidate layers.
- [x] Corrected blink direction and hid lids in the neutral runtime state.
- [x] Added an automatic four-panel neutral / blink / open-mouth / smile review.
- [x] Kept `productionArtApproved` false and Patch 3.5 visible.
- [x] Confirmed automatic Unity verification: EditMode `4 passed`, PlayMode
  `4 passed`, zero warnings/errors and the face close-up opened.
- [x] Rejected the candidate during human visual review because blink,
  open-mouth and smile exposed straight-edged rectangular skin backings.

## 2026-07-30 — P4.0-J seamless face replacements

- [x] Replaced rectangular face fills with elliptical boundary-driven
  inpainting inside `Head/HeadBase`.
- [x] Made alternate lid and mouth PNGs transparent feature-only layers.
- [x] Removed replaceable eye/mouth pixels from overlapping cheek layers.
- [x] Bound eye whites and irises to the blink controller for true replacement.
- [x] Rebuilt the four review poses from mutually exclusive runtime-order layer
  sets instead of stacking alternates over the neutral face.
- [x] Added a blocking alpha-density, border and outside-region check for every
  alternate face layer.
- [x] Added QA and smoke-report proof that replacement composition was used
  and every alternate layer passed sparse-alpha seam checks.
- [x] Reduced the open mouth and changed the smile to a smaller painted closed
  expression.
- [x] Kept `productionArtApproved` false and Patch 3.5 visible.
- [x] Fixed the one compile-time collection-interface mismatch found by real
  Unity import.
- [x] Confirmed P4.0-J in Unity `6000.3.19f1`: automatic EditMode `4 passed`,
  PlayMode `4 passed`, zero warnings/errors and the face close-up opened.
- [x] Rejected P4.0-J during human visual review because smaller light
  rectangles remained around closed eyes, open mouth and smile.

## 2026-07-30 — P4.0-K feathered face transitions

- [x] Replaced rectangular neutral eye/mouth copies with transparent
  high-detail extraction against the deterministic inpainted skin field.
- [x] Constrained every neutral feature with a soft ellipse that reaches zero
  alpha before the old rectangular patch border.
- [x] Replaced hard rectangular cheek clearing with feathered elliptical alpha
  removal.
- [x] Extended sparse-alpha validation to all nine neutral and alternate
  swappable facial feature layers.
- [x] Added blocking hard-alpha-cut checks along both eye regions and the mouth
  region in both cheek layers.
- [x] Added `faceTransitionLayersFeathered` to locked neutral QA and Editor
  smoke validation.
- [x] Advanced automatic continuation to the P4.0-K run id.
- [x] Kept `productionArtApproved` false and Patch 3.5 visible.
- [ ] Confirm P4.0-K in Unity `6000.3.19f1` with automatic `4/4` verification
  and a close-up with no rectangular or elliptical halo.
- [ ] Complete Sprite Skin weight painting after the candidate art passes
  visual review.
