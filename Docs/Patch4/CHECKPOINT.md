# GameWork Patch 4.0 — Durable Checkpoint

Last updated: 2026-07-30
Branch: `patch-4.0`
Repository: `timurkamurka11/skinny-to-beast-clicker`

This file is the canonical continuation point for all future Patch 4 work.

## User goal

Replace the Patch 3.5 procedural/basic-shape character with an original hand-drawn overweight adult man, a completely new named skeleton, separated facial artwork, soft-body deformation and new animations.

The following must remain unchanged:

- `MainMenuLoop.mp4`
- main-menu scenes, prefabs, transitions and button logic
- music, ambient audio and audio mixers
- settings UI, persistence, language, vibration and notifications

## Current visual source

- Neutral front master: transparent PNG, `1024 × 1536`, RGBA.
- Current exact SHA-256: `7b151f1ded93f3852bc8a7218ab26f94298b7f822094304bbcea9c076cad72a3`.
- Repository source:
  `Assets/GameWorkPatch4/Art/Character/FatMan/FatMan_NeutralFront_Master.png`.
- Character: overweight adult man, heavy belly, thick arms and thighs, short dark hair, dirty gray sleeveless shirt, dark pants and gray shoes.
- Figma file: `tZSr9vinRs9EbZzgatxjda`.
- Concept board: node `4:3`.
- Rig blueprint board: node `6:3`.
- Historical Adobe source: `urn:aaid:sc:AP:aa1abfc7-66c2-4260-a320-6781833d46cb`.
- Historical Adobe source URL: `https://at.adobe.com/SGSnfFAvaBd9wjrT`.
- Exact 1024 quality-pass reference:
  `https://photoshop-api.adobe.io/v2/short-url/urn:aaid:ps:US:72e5364f-ba61-4f62-96f5-51c0d8ac09bf`.
- Earlier Creative Cloud copy: `urn:aaid:sc:AP:5086d367-0290-430e-b9a7-39e5392bdbde`.
- Adobe vector trace: `https://to.adobe.com/aN0OeN9oa589DR97`.
- Adobe rigging-parts reference: `https://photoshop-api.adobe.io/v2/short-url/urn:aaid:ps:US:5b427aac-252e-45c2-9a79-272568e505b8`.

The repository owns the exact master bytes and Unity has no Adobe/network
dependency. This master is the current quality source, but it is not final
production art and is not approved by the readiness gate. The Firefly rigging
sheet is reference-only and may not replace the exact repository master.

## Completed P4.0-A — art and rig foundation

- Five-view directional concept reference.
- Clean neutral front master on transparent background.
- Manual layer-cut and 24 px overlap guide.
- New skeleton blueprint and canonical bone names.
- Layer contract for body, head, face, arms, legs, clothes and FX.
- Adobe vector trace and local art-foundation package.

## Completed P4.0-B — runtime and editor foundation

### Runtime

- `Patch4RigContract`
- `Patch4CharacterRigController`
- `Patch4CharacterStateMachine`
- `Patch4FaceController`
- `Patch4SecondaryMotionController`
- `Patch4CharacterVisibilityGuard`
- `Patch4LayerCatalog`
- `Patch4LayerRenderer`
- `Patch4LegacySignalBridge`

### Editor automation

- `Patch4RigContractValidator`
- `Patch4LayerImportPostprocessor`
- `Patch4LayerPlacement`
- `Patch4LayerCatalogBuilder`
- `Patch4AnimationLibraryBuilder`
- `Patch4AnimatorControllerSanitizer`
- `Patch4PrefabBuilder`
- `Patch4SceneInstaller`

### Generated animation contract

1. `FatMan_Idle_Breathe`
2. `FatMan_Idle_ShiftWeight`
3. `FatMan_Blink_Random`
4. `FatMan_LookAround`
5. `FatMan_TapReact_01`
6. `FatMan_TapReact_02`
7. `FatMan_Walk_InRoom`
8. `FatMan_Turn`
9. `FatMan_SitOrLean`
10. `FatMan_UpgradeReact`

### Existing gameplay integration

Patch 4 does not edit the existing gameplay controller. `Patch4LegacySignalBridge` observes:

- accepted tap count from `CharacterRigController`;
- movement state and facing;
- idle/routine action state;
- current skin stage from `CharacterSkinController`.

It mirrors those signals into the new Patch 4 Animator while Patch 3.5 stays available as rollback.

## Completed P4.0-C automation — mask and layer production pipeline

### Adobe work completed

The transparent master was uploaded and visually inspected in Adobe.

Valid selection masks were produced for:

- hair;
- face base;
- eyebrows;
- nose;
- ears;
- neck;
- upper clothes;
- lower clothes;
- hands;
- shoes.

Adobe did not reliably detect stylized pupils, mouth, arms or legs. Those failed requests returned the full subject or an implausibly small selection. They are marked invalid in the manifest and are never treated as production masks.

Manifest:

`Assets/GameWorkPatch4/Art/Character/FatMan/Masks/adobe-mask-manifest.json`

### P4.0-C runtime safety

- `Patch4ArtReadinessAsset` is the explicit human-approval gate.
- `Patch4CharacterRigController` requires readiness approval for the exact master SHA-256.
- Setting `patch4Enabled = true` cannot bypass the gate.
- If art is not approved, Patch 4 stays hidden and Patch 3.5 remains visible.
- Automated tools never set `productionArtApproved`.

### P4.0-C editor tools

- `Patch4AdobeMaskDownloader`
- `Patch4MaskDrivenLayerBaker`
- `Patch4DraftLayerValidator`
- `Patch4ArtReadinessAssetBuilder`
- `Patch4PrefabReadinessBinder`
- `Patch4ProductionPipeline`
- `Patch4ProductionDashboard`

### Draft layer behavior

The baker creates the complete canonical full-canvas layer set in:

`Assets/GameWorkPatch4/Art/Character/FatMan/Layers/`

Every draft remains `1024 × 1536`. Filenames replace `/` with `_`:

- `Body_TorsoBase.png`
- `Face_MouthClosed.png`
- `ArmL_Upper.png`
- `LegR_Foot.png`
- `FX_Shadow.png`

The current baker prefers the ten locally regenerated repository masks and uses
bounded geometric fallback regions when a dedicated mask is unavailable. It
writes `layer-draft-status.json` with `activationAllowed: false`.

### Pixel and joint QA

`Patch4DraftLayerValidator` creates:

`Assets/GameWorkPatch4/Art/Character/FatMan/layer-bake-report.json`

It checks:

- all canonical files exist;
- every canvas is exactly `1024 × 1536`;
- each layer contains meaningful alpha pixels;
- union coverage of the approved master;
- alpha leakage outside the approved master;
- local overlap at neck, shoulders, elbows, wrists, hips, knees, ankles and belly/shirt hem;
- draft metadata keeps activation disabled.

Technical passing does not equal human art approval.

## Completed P4.0-D automation — compile, CI and smoke verification

### GitHub static guard

Workflow:

`.github/workflows/patch4-static-guard.yml`

Validator:

`Assets/GameWorkPatch4/CI/validate_patch4.py`

It checks the 31-bone, 40-layer and 10-clip contracts, uniqueness, approved master SHA, JSON manifests, readiness lock and protected paths. It does not claim Unity compilation.

### Unity compilation report

`Patch4CompilationMonitor` writes:

`Library/GameWorkPatch4Reports/patch4-compilation-report.json`

The report records all compiler errors and warnings, assembly, source path, line, column and Patch 4-specific counts. It is deliberately outside `Assets` to prevent re-import loops.

### Editor prefab smoke report

`Patch4EditorSmokeValidator` writes:

`Library/GameWorkPatch4Reports/patch4-editor-smoke-report.json`

It checks prefab existence, complete skeleton, readiness binding, exact SHA, Animator Controller, all ten clips, the complete layer catalog and the initially hidden Patch 4 visual root.

### EditMode tests

Assembly:

`SkinnyToBeast.GameWorkPatch4.EditModeTests`

The tests verify contract counts, uniqueness, critical entries and exact readiness SHA behavior.

### PlayMode tests

Assembly:

`SkinnyToBeast.GameWorkPatch4.PlayModeTests`

The tests verify:

1. complete skeleton without art approval remains on Patch 3.5;
2. exact approved SHA plus complete skeleton can activate Patch 4;
3. disabling Patch 4 restores rollback visibility;
4. approval cannot bypass an incomplete skeleton.

The tests use reflection to stay isolated from the predefined `Assembly-CSharp` assembly.

Detailed instructions:

`Docs/Patch4/P4_0_D_VERIFICATION.md`

### Real Unity verification completed

Unity `6000.3.19f1` produced a passing automatic verification result:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- compilation snapshot: passed;
- rig contract: passed;
- Editor prefab smoke: passed;
- readiness gate: correctly locked.

`Patch4AutomatedTestRunner` writes the combined JSON and two NUnit XML reports
under `Library/GameWorkPatch4Reports/`.

## P4.0-E runtime room integration

Repository inspection confirmed that the playable room is not an authored scene
containing `CharacterRigController`. `GameplayWindowController` builds
`LivingGameplayScene` dynamically, and `GameplayVisualStageController`
instantiates:

`Resources/UI/Gameplay/Living/CharacterRig2D.prefab`

The Patch 4 integration therefore stays isolated:

- `Patch4PrefabBuilder` generates the locked prefab under
  `Assets/GameWorkPatch4/Resources/`;
- `Patch4RuntimeInstaller` scans only below `LivingGameplayScene`;
- `GameEntryScreen` is excluded;
- the Patch 4 instance is parented beside the real legacy rig;
- rollback root and legacy gameplay signals are bound at runtime;
- `SetPatch4Enabled(false)` is applied explicitly;
- Patch 3.5 remains visible;
- production-art approval is never changed.

An additional PlayMode test verifies the real runtime-resource installation
contract. Unity `6000.3.19f1` confirmed:

- EditMode: `4 passed`;
- PlayMode: `4 passed`.

The user's Console also confirmed that Patch 4 installed only below
`LivingGameplayScene`, stayed in locked rollback mode and left Patch 3.5
visible, with zero warnings and zero errors.

## P4.0-F Canvas room presentation

The painted PNG layers now have an isolated Canvas-compatible presentation:

- `Patch4CanvasPresentation` builds 40 non-interactive `UI.Image` objects;
- one flat image hierarchy preserves canonical global layer order;
- `LateUpdate` mirrors each image pivot to its assigned Patch 4 bone;
- the `1024 × 1536` source is fitted to the legacy `720 × 1280` character
  root using the existing `0.74` presentation scale;
- the source pelvis is aligned to the legacy room origin;
- SpriteRenderer fallbacks are disabled;
- eyelid and mouth bindings are moved to the Canvas images;
- the runtime installer requires successful Canvas binding before accepting
  the hidden Patch 4 instance;
- the Editor smoke report validates all 40 images and disabled fallbacks;
- the runtime PlayMode integration test validates Canvas binding, image count,
  room scale, pelvis alignment and locked rollback state.

This implementation does not approve art, does not enable Patch 4 and does not
modify the legacy gameplay-room builder.

Unity `6000.3.19f1` confirmed the Canvas-ready installation with zero warnings
and zero errors:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- Patch 4 stayed hidden;
- Patch 3.5 stayed visible.

## P4.0-G locked neutral-pose QA

The review step was verified in Unity `6000.3.19f1` without changing readiness:

- `Patch4NeutralPoseValidator` originally composited 36 neutral-state layers in canonical
  order;
- open/smile mouths, sweat and impact FX are excluded from the neutral pose;
- the same four state layers now start hidden in the runtime Canvas;
- the assembled pose is compared pixel-by-pixel with the approved master;
- coverage, leakage, silhouette IoU, mean color error and close-color match are
  recorded;
- composite, difference and three-panel review PNGs are written under
  `Library/GameWorkPatch4Reports/`;
- the JSON report always requires human review and blocks activation;
- Editor smoke validation checks the diagnostic gate;
- the read-only review window opens automatically after the existing `4/4`
  test sequence.

The user's real Unity run completed with zero warnings/errors and:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- the three-panel review window opened automatically;
- Patch 4 remained hidden and Patch 3.5 remained visible.

The review established that splitting/reassembly was technically sound, but the
left and middle panels were both visibly pixelated. The old embedded repository
source was then decoded and measured at only `96 × 144`; Unity had been
bilinearly enlarging it to `1024 × 1536`.

## P4.0-H repository quality-master replacement

- Photoshop/Firefly produced a cleaner character while preserving the neutral
  pose, silhouette, clothes and skeleton placement.
- Background removal and exact resize produced a real `1024 × 1536` 8-bit RGBA
  master.
- The subject alignment remains close to the old target: the new visible bbox
  is `580 × 1075` at `(222, 156)`.
- The exact PNG is committed under the isolated Patch 4 art directory.
- SHA-256 is
  `7b151f1ded93f3852bc8a7218ab26f94298b7f822094304bbcea9c076cad72a3`.
- `Patch4EmbeddedArtSource` and its `96 × 144` payload were removed.
- `Patch4AdobeMaskDownloader` now validates SHA, dimensions and RGBA format,
  copies the exact bytes and generates masks locally.
- The static guard independently validates the committed PNG header and hash.
- Neutral comparison now excludes the runtime-only ground shadow in addition
  to alternate mouths, sweat and impact FX: 35 comparison layers.
- The shadow still exists at runtime, but it no longer appears as a deliberate
  magenta mismatch under the shoes.
- `Patch4AutoContinuation` performs restore, mask creation, full 40-layer bake,
  locked rebuild, safety validation and all tests after one pull.
- Production activation remains locked and still requires later human review.

Unity `6000.3.19f1` then verified this exact quality pass:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- zero Console warnings and errors;
- sharp master and assembled-neutral panels;
- only a thin silhouette contour in the difference panel;
- no false ground-shadow difference;
- the user confirmed the visual quality was many times better;
- Patch 4 remained hidden and Patch 3.5 remained visible.

## P4.0-I hidden-continuation and independent-face candidates

The next isolated candidate pass replaces the remaining technical stand-ins:

- the former radius-five solid joint disks are removed;
- neck, shoulders, elbows, wrists, hips, knees, ankles, belly and shirt hem now
  receive texture-preserving elliptical overlaps copied from the exact master;
- each overlap may feather only three pixels outside the master silhouette;
- ordinary joint overlap validation rises from 24 to 180 pixels;
- belly/shirt overlap validation rises to 360 pixels;
- the head base receives a deterministic skin underlay beneath both eyes and
  the mouth;
- exact master patches restore neutral eyes and the closed mouth;
- closed lids, open mouth and smile are independently painted candidate
  layers;
- closed lids start hidden and the blink controller now grows them from open
  to fully closed before retracting and hiding them;
- neutral comparison excludes both alternate lids as well as alternate mouths
  and FX, leaving 33 comparison layers;
- neutral / blink / open-mouth / smile close-ups are written to
  `Library/GameWorkPatch4Reports/patch4-face-pose-review.png`;
- `Patch4FacePoseReviewWindow` opens automatically after the existing neutral
  review window;
- Editor smoke validation requires the new face preview;
- static checks require the new joint and facial painting paths and reject the
  former scaffold method.

This pass is still a human-review candidate. It does not approve art, enable
Patch 4 or change the exact master SHA.

Unity `6000.3.19f1` completed this pass with:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- zero Console warnings and errors;
- the four-panel face review opened automatically.

Human review rejected the alternate expressions. Blink, open-mouth and smile
showed visible straight-edged skin backing rectangles even though all technical
tests passed. This is a visual blocker, so Patch 4 stayed locked.

## P4.0-J seamless face replacements

The corrective candidate pass removes the source of the rectangles:

- `Head/HeadBase` now uses elliptical boundary-driven inpainting beneath the
  eyes and mouth instead of a rectangular color field;
- lid, open-mouth and smile layers contain only their painted feature with
  transparent surroundings;
- overlapping cheek layers are cleared inside replaceable eye and mouth
  regions;
- the blink controller binds and hides both eye whites and both irises only
  during the closed phase, then restores them;
- the review compositor builds neutral, blink, open-mouth and smile from
  mutually exclusive canonical layer sets in runtime order;
- draft validation rejects dense rectangular backing alpha, border contact and
  any paint outside the allowed face region;
- the QA and Editor smoke reports require
  `facePoseUsesReplacementComposition: true` and
  `faceReplacementLayersClean: true`;
- the open mouth is smaller and the smile is a restrained painted closed smile;
- readiness and runtime activation remain locked.

## Production dashboard

Open in Unity:

`Tools → GameWork → Patch 4.0 → Open Production Dashboard`

Ordered commands:

1. Restore repository sources.
2. Bake draft layers.
3. Validate draft layers.
4. Rebuild locked runtime assets.
5. Run safety validation.
6. Run compilation and Editor smoke reports.
7. Open Unity Test Runner and run EditMode plus PlayMode tests.

## Canva visual status

Editable design:

`https://www.canva.com/d/Zwy2RkpL4DJRJYs`

View-only design:

`https://www.canva.com/d/VESyL19jdqnHkif`

Canva is a visual status copy. This GitHub checkpoint remains the source of truth.

## Figma status

Figma concept and rig pages remain intact.

A new P4.0-C status panel could not be written because the Figma Starter MCP call limit was reached. The failed write was atomic and did not modify or damage the existing file.

## Current activation state

Patch 4 must remain **disabled**.

It may activate only when:

1. all required bones exist;
2. all canonical painted sprites exist in the layer catalog;
3. all ten animation clips exist;
4. the pixel/joint report passes;
5. the compilation report succeeds;
6. the Editor smoke report passes;
7. all EditMode tests pass;
8. all PlayMode tests pass;
9. all hidden joint artwork has been manually reconstructed;
10. independent face poses have been manually reviewed;
11. the exact master SHA is approved in `Patch4ArtReadiness.asset`;
12. only one character body is visible;
13. the ten animations pass review in the actual room;
14. protected paths remain unchanged.

Until every condition passes, Patch 3.5 remains visible.

## Immediate next work

1. Pull the P4.0-J seamless-replacement commit into Unity `6000.3.19f1`.
2. Let `Patch4AutoContinuation` verify/restore the exact source, regenerate
   masks and all 40 layers, paint the joint/face replacements, assemble the
   neutral pose, write both review previews and run all validations without
   manual clicks.
3. Confirm `EditMode: 4 passed; PlayMode: 4 passed`.
4. Inspect the automatically focused neutral / blink / open-mouth / smile
   close-ups and the neutral comparison behind them.
5. Revise any visible facial seam or moving-joint exposure while keeping the
   readiness gate locked.
6. Complete Sprite Skin weight painting.
7. Review all ten animations in the actual room.
8. Only after successful technical and human review, approve the readiness
    asset for the exact master SHA.

Detailed art instructions:

`Docs/Patch4/P4_0_C_LAYER_PRODUCTION.md`

Detailed verification instructions:

`Docs/Patch4/P4_0_D_VERIFICATION.md`

## Known limitations

- Generated PNG layers and generated runtime assets exist locally in Unity and
  are not committed as binary repository assets.
- P4.0-I passed local Unity `4/4`, but its face close-up failed human review
  because alternate expressions contained visible rectangular skin backings.
- P4.0-J has not yet received its local Unity `4/4` run and close-up review.
- Sprite Skin weight painting has not yet been completed.
- The ten clips have not yet received final visual review with the production
  character visible in the actual room.
- The Canvas presentation remains hidden behind readiness.
- Figma Starter MCP limit currently prevents additional write calls.
