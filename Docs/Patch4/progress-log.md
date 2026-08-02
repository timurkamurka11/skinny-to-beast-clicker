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
- [x] Confirmed P4.0-K in Unity `6000.3.19f1` with automatic EditMode
  `4 passed`, PlayMode `4 passed`, zero warnings/errors and a close-up with no
  rectangular or elliptical eye/mouth halo.
- [x] Accepted the feathered face-transition close-up while keeping production
  approval locked.

## 2026-07-30 — P4.0-L Canvas weights and actual-room animation review

- [x] Confirmed that standard Sprite Skin would affect only the disabled
  SpriteRenderer fallbacks, not the actual Screen Space Overlay Canvas.
- [x] Added a Canvas-compatible weight-painted grid deformer for `UI.Image`.
- [x] Bound all 40 canonical layers to the Patch 4 skeleton.
- [x] Added multi-bone grids for torso, belly, chest, neck, head, cheeks,
  clothes, arms, legs, shoes and impact fold.
- [x] Added bind-pose recapture after the master is fitted to the real room.
- [x] Extended Editor smoke and the existing PlayMode integration test to
  require all 40 bindings and at least 20 multi-bone layers.
- [x] Added an Editor-only locked driver that creates the actual
  `LivingGameplayScene`, cycles all ten clips and captures a room contact
  sheet.
- [x] Kept the review driver out of player builds and prevented any call to
  `SetPatch4Enabled(true)`.
- [x] Made the automatic flow restore Patch 3.5 and exit Play Mode before
  opening the neutral, face and animation review windows.
- [x] Kept `productionArtApproved` false.
- [x] Ran P4.0-L in Unity `6000.3.19f1` and produced all ten room captures.
- [x] Rejected the result: every capture contained stretched/collapsed skin
  fragments instead of an assembled character.
- [x] Rejected the old technical `PASSED`: the review also emitted repeated
  Stage 4 invisible-rig Console errors.
- [x] Identified Tight outer-UV crop expansion and deactivation of the legacy
  visual root as the two isolated causes.

## 2026-07-30 — P4.0-M FullRect UV and honest room validation

- [x] Forced every regenerated layer import to `SpriteMeshType.FullRect`.
- [x] Disabled tight sprite meshes on the 40 Canvas Images.
- [x] Replaced cropped outer-UV mapping with full `Sprite.rect` texture UVs.
- [x] Extended Editor smoke and PlayMode checks for 40 FullRect sources,
  full-canvas mappings and source-mesh bypass.
- [x] Replaced legacy-root deactivation with reversible CanvasGroup hiding so
  Stage 4 remains logically visible.
- [x] Added a clean-background comparison and per-frame character silhouette
  thresholds.
- [x] Made any Console Error, Exception or Assert block the room-review result.
- [x] Kept the readiness asset locked and Patch 3.5 restoration mandatory.
- [x] Confirmed the P4.0-M FullRect rebake and Editor checks in Unity
  `6000.3.19f1`; EditMode passed.
- [x] Stopped before room review because the fourth PlayMode test returned
  `Failed(Child)`.
- [x] Isolated the only PlayMode-only new assertion: an irrelevant exact
  `Sprite.vertices.Length == 4` requirement on source geometry that the custom
  Canvas deformer clears and replaces.

## 2026-07-30 — P4.0-N runtime test contract and diagnostics

- [x] Kept FullRect importer, full-canvas UV, bind-pose, 40-layer and weighted
  grid requirements intact.
- [x] Removed only the unused source Sprite vertex-array cardinality assertion.
- [x] Added recursive failed-leaf collection to the automatic Test Runner.
- [x] Added the first child test name and assertion text to the Console failure.
- [x] Advanced the automatic continuation to a new run id.
- [x] Kept the readiness gate locked and Patch 3.5 active.
- [x] Confirmed P4.0-N in Unity `6000.3.19f1`: EditMode `4 passed`, PlayMode
  `4 passed`, ten captures and zero room-review errors.
- [x] Rejected P4.0-N after human review: most clips were nearly static,
  `FatMan_Turn` collapsed to a vertical line and the old robot-like footstep
  remained audible.
- [x] Isolated live follower updates as the primary-motion cancellation and
  the authored `0.12` turn scale as the collapse source.

## 2026-07-30 — P4.0-O visible motion and silent locked review

- [x] Freeze Canvas layer anchors after bind-pose capture.
- [x] Require frozen anchors in readiness, Editor smoke and PlayMode.
- [x] Expand all ten clips with readable body, head, arm and leg motion.
- [x] Replace the near-zero turn squash with a safe pivot/counter-motion pose.
- [x] Capture each clip at an authored action peak.
- [x] Compare every peak with its own start pose and block weak visible motion.
- [x] Compare every silhouette with a fixed neutral reference and block width,
  height or area collapse.
- [x] Pause and restore the legacy walk routine and signal bridge during the
  isolated review.
- [x] Stop only the legacy non-loop one-shot source so the robot-like footstep
  cannot contaminate Patch 4 motion review.
- [x] Keep ambient audio, menu, video, music and settings unchanged.
- [x] Keep readiness locked and Patch 3.5 active outside review.
- [x] Confirm P4.0-O source compiles with EditMode `4 passed` and PlayMode
  `4 passed` in Unity `6000.3.19f1`.
- [ ] Capture the corrected ten visibly distinct peaks; the P4.0-O room-review
  session did not start, so zero collapse and silent walking remain unverified.
- [ ] Complete human review of joints, stretch, clothing continuity and foot
  contact only after the corrected contact sheet is visually coherent.

## 2026-08-01 — P4.0-P fresh room-review handoff

- [x] Confirmed P4.0-O source compiled and completed EditMode `4 passed` plus
  PlayMode `4 passed` in Unity `6000.3.19f1`.
- [x] Rejected the opened animation window: Console reported that the locked
  room review did not complete, while the window showed the stale P4.0-N PNG.
- [x] Isolated the race between Test Runner leaving Play Mode and the separate
  actual-room review attempting to track its own Play Mode session.
- [x] Added a persistent waiting stage before the second Play Mode entry.
- [x] Clear previous room-review artifacts at the start of every run.
- [x] Add a unique run token to the driver report and require the current token
  before any contact sheet can open.
- [x] Make the review window distinguish a fresh pass, fresh failure and absent
  or stale evidence.
- [x] Advance automatic continuation to a new run id.
- [x] Keep P4.0-O motion fixes, rollback, readiness and protected paths intact.
- [ ] Confirm P4.0-P in Unity `6000.3.19f1`: `4/4`, a separate completed room
  review, a fresh ten-frame sheet with no vertical turn and no legacy step.

## 2026-08-01 — P4.0-Q exclusive cutout and rigid-face correction

- [x] Accepted the fresh P4.0-P technical failure instead of weakening its
  evidence: blink measured `0.001`, and moving frames visibly split the rig.
- [x] Isolated coincident source pixels in rectangular mask regions, default-
  visible reference layers and soft grids on cutout parts as the shared cause.
- [x] Added a canonical 18-layer neutral runtime stack and hid all required
  reference duplicates without removing them from the 40-layer contract.
- [x] Assigned each neutral body pixel to one exclusive live owner and restored
  only named joint continuations.
- [x] Made the head, face states, arms and legs rigid one-bone cutouts; kept
  soft multi-bone deformation only on the central shirt.
- [x] Refresh rigid Canvas cutouts every frame as well as the soft shirt, so
  visible parts cannot freeze while their bones move.
- [x] Reparented painted face states to Head, hid duplicate Iris layers and
  removed redundant Eye-bone transforms from blink/look clips.
- [x] Added blocking duplicate-ownership QA and focused face-region blink QA.
- [x] Updated Editor smoke, PlayMode and static guard contracts for the hybrid
  cutout/soft-shirt rig.
- [x] Advanced automatic continuation to
  `exclusive-cutout-rig-review-v9`.
- [x] Kept readiness locked, Patch 3.5 active and protected paths untouched.
- [x] Exercised P4.0-Q in Unity `6000.3.19f1`; duplicated full limbs were gone.
- [x] Rejected P4.0-Q after human review: rectangular shoulder/arm/leg cutouts
  remained visibly chopped, the face/head relationship shifted and the walk
  did not read as walking.
- [x] Accepted the two fresh Console errors as blockers and traced them to Test
  Runner post-build cleanup calling `EditorSceneManager.NewScene` after the
  separate room review had already entered Play Mode.

## 2026-08-01 — P4.0-R intact continuous-body correction

- [x] Replace the visible 18-piece anatomical stack with one intact
  `Body/TorsoBase` master plus three sparse neutral face features.
- [x] Keep all 40 required layers, but hide segmented head, limb, shirt and
  clothing candidates as reference artwork.
- [x] Build a dense `32 × 48` full-canvas deformation surface for the intact
  body.
- [x] Add smooth anatomical Head/Neck, torso/belly, shoulder/arm and hip/leg
  weight zones instead of rectangular cutout ownership.
- [x] Bind the base face region and all eye/lid/mouth replacements to the exact
  same Head matrix; remove the extra LookAround head translation.
- [x] Increase walk stride, knee bend, foot rotation, arm swing and body bounce.
- [x] Require 30 stable Editor updates and 1.25 seconds of quiescence after Test
  Runner cleanup before entering the separate review Play Mode.
- [x] Update draft, smoke, PlayMode and static guards for the continuous body.
- [x] Advance automatic continuation to
  `continuous-body-rig-review-v10`.
- [x] Keep readiness locked, Patch 3.5 active and protected paths untouched.
- [x] Exercise P4.0-R in Unity `6000.3.19f1`; reject its blank face,
  shirt-vacuum stretch and unreadable walk despite zero Console errors.

## 2026-08-02 — P4.0-R rejection and P4.0-S correction

- [x] Exercise P4.0-R in real Unity and confirm that chopped rectangles are
  gone.
- [x] Reject P4.0-R: neutral eyes/mouth were erased, reaction poses pulled the
  shirt into vacuum-like wings, and walk limb motion remained unreadable.
- [x] Trace the face loss to inpainting the only continuous body plus fragile
  sparse feature extraction.
- [x] Trace the vacuum stretch to broad horizontal arm weights that included
  outer shirt pixels.
- [x] Preserve the exact master as the sole neutral runtime layer.
- [x] Build softly feathered full eye/mouth expression replacements over that
  exact master.
- [x] Replace broad arm/leg strips with curved anatomical centerline envelopes
  on a denser `64 × 96` grid.
- [x] Add maximum expansion gates and focused walk arm/leg motion QA.
- [x] Advance automatic continuation to
  `anatomical-warp-face-review-v11`.
- [x] Keep readiness locked, Patch 3.5 active and protected paths untouched.
- [x] Exercise P4.0-S in Unity `6000.3.19f1`; record that the fourth PlayMode
  test stopped before the actual-room animation review.
- [x] Isolate the failure to stale `Assert.NotNull` checks for neutral eye
  objects that P4.0-S intentionally removed from the live face binding.

## 2026-08-02 — P4.0-T face-binding PlayMode correction

- [x] Require neutral eye, iris and closed-mouth controller bindings to remain
  null because the exact master body owns those pixels.
- [x] Require both feathered lids, open mouth and smile replacements to remain
  bound.
- [x] Add a static regression guard for the exact-master face-binding split.
- [x] Advance automatic continuation to
  `exact-master-face-binding-review-v12`.
- [x] Keep the P4.0-S art, mesh weights, animation curves and validation gates
  unchanged.
- [x] Keep readiness locked, Patch 3.5 active and protected paths untouched.
- [x] Exercise P4.0-T in Unity `6000.3.19f1`; confirm the exact neutral face and
  fresh token-matched room sheet.
- [x] Reject P4.0-T: the walk remained a side sway, arms/legs lacked readable
  articulation and reaction poses retained vacuum-like wedges despite the
  technical pass.

## 2026-08-02 — P4.0-U anatomical limb and stride correction

- [x] Measure the exact master rows and isolate arm/leg envelopes that were
  still centered inside shirt/crotch pixels.
- [x] Increase the intact full-master body grid from `64 × 96` to `96 × 144`.
- [x] Fit narrow full-weight arm envelopes to the painted skin and explicitly
  stop them at the tank-top edge.
- [x] Shift leg envelopes outward and narrow the crotch seam so thigh, shin and
  shoe pixels follow their real bones.
- [x] Replace the symmetric spread/squat walk phase with alternating frontal
  thigh, knee, foot and counter-arm articulation; reduce root sway.
- [x] Reduce tap/upgrade arm extremes that amplified the old ownership error.
- [x] Align away whole-body translation before pixel comparison and count only
  start-pose foreground pixels.
- [x] Require separate passing motion coverage for both arms and both legs.
- [x] Tighten maximum width, height and area expansion limits.
- [x] Advance automatic continuation to
  `anatomical-limb-stride-review-v13`.
- [x] Keep readiness locked, Patch 3.5 active and protected paths untouched.
- [x] Exercise P4.0-U in Unity `6000.3.19f1`; confirm the exact face/body stays
  intact and the fresh review reports zero Console errors.
- [x] Reject P4.0-U: walk remains a mostly static twitch/rock, arms and legs do
  not read as an articulated gait, and broad region pixel changes falsely pass
  the technical check.

## 2026-08-02 — P4.0-V explicit joint gait and truthful motion QA

- [x] Remove horizontal movement of the complete walk Visual.
- [x] Author four explicit gait phases with alternating thigh lift, knee bend,
  foot plant, shoulder counter-swing, elbow follow-through and hand motion.
- [x] Reduce reaction and upgrade body/belly scaling that can create vacuum-like
  expansion.
- [x] Replace colour-change limb coverage with aligned binary foreground-
  silhouette XOR in four narrower independent limb regions.
- [x] Require both hands to move relative to their clavicles and both feet to
  move relative to the pelvis.
- [x] Add an EditMode regression check for thigh lift, opposing arm phases and
  absence of whole-body horizontal walk sway.
- [x] Advance automatic continuation to
  `articulated-gait-silhouette-review-v14`.
- [x] Keep the exact master, menu, video, music, settings, readiness lock and
  Patch 3.5 rollback state unchanged.
- [ ] Confirm P4.0-V in Unity `6000.3.19f1`: `4/4`, visibly articulated arms
  and legs, readable in-place walk cycle, stable face/body and zero Console
  errors.
