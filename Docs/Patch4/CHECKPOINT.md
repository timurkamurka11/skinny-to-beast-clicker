# GameWork Patch 4.0 — Durable Checkpoint

Last updated: 2026-08-02
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

The user installed CoplayDev MCP locally. It is useful for later live Unity
inspection, but its commands were not exposed to the assistant session during
the P4.0-S source correction. A second Unity MCP is not required.

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

Unity `6000.3.19f1` initially exposed a single compile-time collection-interface
mismatch in the new QA call. Commit `06e63fa` fixed that signature without
changing behavior. The automatic continuation then completed with:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- zero Console warnings and errors;
- the four-panel face review opened automatically.

The result was substantially better than P4.0-I, but still failed human visual
review. Smaller light rectangles remained around closed eyes, the open mouth
and the smile. Alternate lid and mouth layers were already feature-only. The
remaining seams came from rectangular neutral eye/mouth master copies and hard
rectangular alpha cuts in both cheek layers. Patch 4 remained locked.

## P4.0-K feathered face transitions

The next corrective candidate pass removes both remaining rectangular
operations:

- neutral eye and closed-mouth layers are extracted as transparent high-detail
  features by comparing the exact master against the deterministic inpainted
  skin field;
- extraction is constrained by a softly feathered ellipse, so no neutral
  feature reaches the old rectangular region border;
- both cheek layers remove neutral features with an elliptical alpha feather
  instead of clearing a rectangle;
- all nine swappable neutral and alternate feature layers are checked for
  sparse alpha, rectangular-border contact and paint outside their face region;
- draft and neutral-pose QA count abrupt transparent-to-opaque pixels along
  every former eye/mouth rectangle and block more than six;
- the neutral-pose and Editor smoke reports now require
  `faceTransitionLayersFeathered: true`;
- the automatic run id is advanced so one pull performs the complete rebake,
  safety validation and `4/4` test sequence;
- readiness and runtime activation remain locked.

Unity `6000.3.19f1` completed P4.0-K with:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- zero Console warnings and errors;
- the four-expression review opened automatically;
- no remaining rectangular or elliptical halo around either eye, the open
  mouth or the smile in the supplied final close-up;
- Patch 4 still locked and Patch 3.5 still active.

The face-transition candidate therefore passed this human close-up review. The
readiness asset was not approved.

## P4.0-L Canvas weight maps and actual-room animation review

The visible production path uses Screen Space Overlay `UI.Image` objects, not
SpriteRenderers. Standard Sprite Skin would therefore deform only the disabled
fallback renderers. P4.0-L adds an equivalent skinning path for the real Canvas:

- `Patch4CanvasSkinDeformer` derives from `BaseMeshEffect`;
- all 40 images receive deterministic bind poses;
- rigid details keep a single-bone four-vertex mesh;
- torso, belly, chest, neck, head, cheeks, shirts, bottoms, shoes, arms, legs
  and impact fold use subdivided grids with distance-painted multi-bone
  weights;
- each vertex applies standard current-bone × bind-pose matrices in image-local
  space;
- bind poses are captured again after the approved master is fitted to the
  existing `720 × 1280` room character root;
- Editor smoke and the existing fourth PlayMode test require all 40 bindings,
  at least 20 weighted layers and weighted critical body/clothing/limb layers;
- the SpriteRenderer fallback set remains present but disabled.

The same pass adds a fully automatic locked motion review:

- a passing automatic `4/4` schedules a second, Editor-only Play Mode session;
- `GameplayWindowController.Show()` creates the actual
  `LivingGameplayScene` through the existing runtime path;
- `Patch4RuntimeInstaller` binds the locked generated character beside the
  actual legacy rig;
- an Editor-only driver temporarily presents Patch 4 for review without
  calling `SetPatch4Enabled(true)` or changing the readiness asset;
- all ten required clips play in sequence with Canvas skinning and secondary
  motion active;
- one actual-room frame per clip is captured into
  `Library/GameWorkPatch4Reports/patch4-animation-room-review.png`;
- a JSON report records all ten clips, skinning counts, locked gate,
  `humanReviewRequired: true` and `activationAllowed: false`;
- Patch 4 is hidden and Patch 3.5 is restored before Play Mode exits;
- neutral, face and 5 × 2 animation review windows then open automatically.

This is a review mechanism, not production activation. The driver is enclosed
by `UNITY_EDITOR` and is absent from player builds.

### P4.0-L real Unity rejection

Unity `6000.3.19f1` completed the automatic room cycle, but the 5 × 2 contact
sheet showed unusable stretched skin rectangles and collapsed body fragments
in all ten clips. The Console also repeated:

```text
Character stage 4 was selected but did not produce a visible rig.
The next Sync will retry it.
```

The prior `PASSED` line meant only that ten screenshots were written. It did
not validate their visual contents or Console errors and is rejected.

The failure came from expanding a Tight sprite's opaque outer-UV crop across
the complete transparent layer rectangle. Separately, the review set the
legacy visual root inactive, so the existing Stage 4 visibility check retried
continuously. Production activation stayed locked and Patch 3.5 was restored.

## P4.0-M FullRect UV and room-silhouette correction

The corrective pass remains isolated under Patch 4:

- `Patch4LayerImportPostprocessor` forces `SpriteMeshType.FullRect`;
- every Canvas `Image` disables `useSpriteMesh`;
- `Patch4CanvasSkinDeformer` derives its UV range from the full `Sprite.rect`
  and source texture dimensions rather than `DataUtility.GetOuterUV`;
- Editor smoke and the fourth PlayMode test require 40 FullRect source sprites,
  40 full-canvas UV mappings and no Tight image meshes;
- the review leaves the Patch 3.5 hierarchy active and temporarily hides only
  its rendering through a reversible `CanvasGroup`;
- a clean gameplay-room background is captured before Patch 4 appears;
- each of the ten captured frames must contain a sufficiently wide, tall and
  filled silhouette inside the expected character canvas;
- any Error, Exception or Assert logged during the room review blocks the
  technical result;
- the driver restores the previous CanvasGroup state and Patch 3.5 before
  leaving Play Mode;
- human review and the production-art gate remain mandatory.

### P4.0-M real Unity test stop

Unity `6000.3.19f1` completed the FullRect rebake and Editor validations. The
FullRect importer state, all 40 full-canvas UV mappings and disabled
`Image.useSpriteMesh` contract passed before testing. EditMode passed, but the
fourth PlayMode test returned `Failed(Child)`, so the actual-room review
correctly did not begin.

The PlayMode test had one extra assertion that was not part of Editor smoke:
it required every imported source Sprite to expose exactly four entries through
`Sprite.vertices`. The visible presentation does not consume those source
vertices: `Patch4CanvasSkinDeformer` clears the mesh and generates its own
weighted grid. Exact internal source-array cardinality is therefore not the
runtime safety contract.

## P4.0-N corrected runtime contract and child diagnostics

- FullRect import, full-canvas UVs and source-mesh bypass remain mandatory.
- All 40 deformers must still be bound and at least 20 must remain multi-bone.
- The PlayMode test no longer assumes an exact internal `Sprite.vertices`
  array length that the custom deformer does not use.
- The automated runner now traverses failed test leaves and writes their names,
  states and messages to its JSON report.
- Console now includes the first actual assertion failure instead of only the
  parent state `Failed(Child)`.
- A new continuation run id restarts the complete locked pipeline after pull.
- Readiness remains false and the room review still cannot pass a collapsed
  silhouette or any Console error.

### P4.0-N real Unity motion rejection

Unity `6000.3.19f1` completed the corrected run with:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- zero Console errors;
- all ten actual-room captures written.

Human review rejected the motion despite the technical `PASSED`. Most captures
were nearly static, `FatMan_Turn` collapsed to a vertical line and the old
synthetic/robot-like walking footstep was audible.

The principal motion bug was double transform cancellation. Each Canvas layer
follower copied its live bone transform every `LateUpdate`, while the custom
skin deformer applied the same bone matrix relative to that moving follower.
The primary influence therefore cancelled. Separately, the authored turn clip
explicitly reduced `CharacterRoot` horizontal scale to `0.12`. The prior
silhouette gate compared against an expected rectangle that collapsed along
with the character, so it could not reject that frame.

## P4.0-O frozen bind anchors and measurable visible motion

- Canvas followers align once before bind-pose capture and stay frozen after
  capture; the live bones now deform the real UI mesh instead of being
  cancelled.
- `BindAnchorsFrozen` is required by Canvas readiness, Editor smoke, PlayMode
  and the room-review setup.
- All ten clips now contain readable body motion with neutral start poses and
  intentionally selected action-peak review times.
- `FatMan_Turn` uses pelvis/spine/head/arm counter-motion and only mild
  `0.94–1.02` scale change; the near-zero squash is forbidden.
- The review stores a neutral silhouette before the clips and requires every
  peak to retain its width, height and filled area against that fixed
  reference.
- Each peak is also compared with that clip's captured start pose and must pass
  a clip-specific minimum visible changed-pixel ratio.
- The legacy `CharacterRoutineController` and Patch 3.5 signal bridge pause
  only during the isolated Patch 4 review. Existing non-loop one-shot audio is
  stopped, preventing the old robot-like footstep from contaminating the
  review. Both controllers are restored afterward.
- Ambient audio and all protected menu/video/music/settings code remain
  unchanged.
- Readiness and activation remain locked.

### P4.0-O Unity handoff race and stale artifact rejection

Unity `6000.3.19f1` compiled this source and completed EditMode `4 passed` plus
PlayMode `4 passed`. The subsequent locked room review did not complete.
`StartAfterTests()` began tracking the second session before the Test Runner had
fully exited its Play Mode session, so the normal test-session exit was
misclassified as an interrupted review.

Console correctly exposed the lifecycle failure:

`Play Mode ended before the locked room review completed.`

The review window nevertheless loaded the previous PNG because it did not
validate artifact freshness. The displayed nearly static poses and vertical
turn were the stale P4.0-N contact sheet, not a capture of the corrected P4.0-O
curves. That displayed result is rejected.

## P4.0-P token-matched fresh room review

- The review uses `waiting-for-test-play-mode-exit` until Unity has fully
  returned to Edit Mode after PlayMode tests.
- Only then does it enter the separate actual-room Play Mode session.
- The waiting stage survives domain reload through `SessionState`.
- Previous room-review JSON and PNG artifacts are cleared before the run.
- A unique run token is passed into the driver and written to the report.
- Completion opens the animation window only when both artifacts exist, the
  report is complete and its token matches the current run.
- The window independently repeats the token/completion check and shows clear
  failure text for a fresh failed report.
- An old contact sheet can no longer masquerade as the current review.
- Automatic continuation advances to `fresh-room-review-handoff-v8`.
- P4.0-O motion, Canvas, rollback and audio isolation remain unchanged.
- Readiness remains false and protected paths remain unchanged.

### P4.0-P real Unity rejection

Unity `6000.3.19f1` completed the fresh token-matched second Play Mode session.
The new sheet and JSON were current, and the honest validator failed
`FatMan_Blink_Random` at `0.001` visible whole-character motion against the old
`0.003` threshold. Human review also rejected the actual frames: neutral hid
coincident copies, while moving clips separated duplicate heads, arms and legs
and detached the Eye/Jaw-bound face from the head.

The root cause was upstream of the animation curves. Repository masks are
conservative rectangular source regions, and all required reference layers
were initially visible. The same master pixels therefore existed in several
independently weighted layers. Coarse multi-bone grids compounded the split on
parts that should have behaved as rigid cutouts.

## P4.0-Q exclusive cutout ownership and rigid face/limbs

- `Patch4RigContract` defines the canonical 18-layer neutral runtime stack,
  the 15 exclusive source-art owners and the 23 controlled rigid layers.
- The baker resolves every visible master pixel to one live body owner and
  re-adds overlap only inside named joint continuations.
- The draft validator rejects multiply-owned runtime pixels outside those
  joints and measures master coverage from the actual 18-layer neutral stack.
- Required reference copies remain available in the 40-layer catalog but are
  hidden by both catalog metadata and runtime defensive visibility.
- Head, painted face states, arms and legs use a one-bone four-vertex cutout;
  only the central shirt retains soft multi-bone Canvas deformation.
- The Canvas mesh effect dirties rigid and soft layers every frame, so a
  one-bone cutout visibly follows its animated bone instead of staying at the
  captured bind pose.
- All face replacements share the Head pivot, separate Iris copies stay hidden,
  and animation clips no longer apply a second Eye-bone blink/look transform.
- Blink QA now requires both non-zero character motion and a focused face-region
  motion ratio, so a real lid swap is measured without pretending it is a
  whole-body action.
- Editor smoke and PlayMode contracts require all rigid runtime bindings and
  the soft shirt grid.
- Automatic continuation advances to
  `exclusive-cutout-rig-review-v9`.
- Production readiness remains locked; Patch 3.5 remains active.

### P4.0-Q real Unity rejection

Unity `6000.3.19f1` rebuilt and displayed the P4.0-Q runtime candidate. The
one-owner pass removed the duplicated full limbs from P4.0-P, but the visible
architecture still failed human review:

- shoulder, elbow, hand and leg motion exposed cropped rectangular cutout
  boundaries rather than a smooth painted body;
- the head/face relationship still shifted in animated poses;
- the walk capture did not read as walking;
- the fresh review failed after two Console errors;
- Test Runner post-build cleanup called `EditorSceneManager.NewScene` after the
  separate animation review had already entered Play Mode, which Unity rejects.

The rigid anatomical cutout approach is therefore not a production candidate.
Its reference layers may remain in the 40-layer contract, but they may not be
the visible runtime body.

## P4.0-R intact full-body Canvas deformation

- `Body/TorsoBase` now copies the complete locked master silhouette and keeps
  the verified inpainted eye/mouth underlay.
- The runtime neutral stack contains exactly four layers: the intact body, two
  sparse neutral eye features and the sparse closed mouth.
- Segmented head, arms, legs, shirt and other required candidates remain hidden
  reference layers; they cannot produce chopped or doubled runtime limbs.
- The visible body builds a `32 × 48` full-canvas grid with smooth anatomical
  weighting for Head/Neck, spine/chest/belly, both three-part arms and both
  three-part legs.
- The full-body face pixels and every facial replacement use the same Head
  matrix. The extra LookAround head translation is removed.
- The in-room walk receives a stronger stride, knee/foot articulation, arm
  counter-swing and body bounce.
- The room-review handoff requires 30 consecutive idle Editor updates and at
  least 1.25 seconds after Test Runner Play Mode before starting its own Play
  Mode. Compile, import or play transitions reset that timer.
- Editor smoke, PlayMode and static guards require the intact dense body and
  forbid anatomical cutouts from the neutral runtime stack.
- Automatic continuation advances to
  `continuous-body-rig-review-v10`.
- Readiness remains false, Patch 3.5 remains active and protected paths are
  unchanged.

### P4.0-R real Unity rejection

Unity `6000.3.19f1` completed the fresh P4.0-R room review without Console
errors. It confirmed that one intact body removed the chopped rectangles, but
the visual candidate still failed:

- the face had no readable neutral eyes or mouth;
- reaction clips stretched the outer shirt sideways like a vacuum pull;
- arms and legs barely articulated and the walk stayed visually static;
- minimum-only silhouette and motion thresholds incorrectly allowed the sheet
  to report a technical pass.

Repository inspection confirmed that P4.0-R inpainted the master face before
runtime and relied on sparse feature extraction to rebuild it. Its continuous
mesh also assigned arm influence to broad x/y strips rather than the actual arm
centerlines, so shirt pixels followed large arm rotations.

## P4.0-S exact neutral face and constrained anatomical warp

- `Body/TorsoBase` now returns the quality master clone without eye or mouth
  inpainting. It is the only neutral runtime layer.
- Eye-white, iris and closed-mouth candidates remain hidden references.
- Closed lids, open mouth and smile copy only a feathered elliptical master
  patch, inpaint the feature center and paint the alternate expression. The
  unchanged feather matches the underlying master exactly.
- The blink controller fades those full replacement patches instead of scaling
  a thin lid line over an open eye.
- The continuous grid becomes `64 × 96` and includes both clavicles.
- Arm influence is constrained around curved shoulder-to-hand centerlines and
  fades before the torso; leg influence follows separate hip-to-foot
  centerlines and fades at the center seam.
- Large whole-body/arm reaction values are reduced; walk stride, knee/foot
  articulation and arm counter-swing remain clearly authored.
- Actual-room validation adds maximum neutral width, height and area expansion
  limits, plus a focused arm/leg motion gate for `FatMan_Walk_InRoom`.
- Static, EditMode, PlayMode, smoke and neutral/face QA contracts are updated
  for the exact one-layer neutral stack and the denser constrained grid.
- Automatic continuation advances to
  `anatomical-warp-face-review-v11`.
- Production readiness stays false, Patch 3.5 stays active and menu, video,
  music, audio and settings paths remain untouched.

### P4.0-S real Unity test stop

Unity `6000.3.19f1` imported the P4.0-S source and reached Test Runner. EditMode
passed, but PlayMode returned `Failed(Child)` before the separate actual-room
review could begin. The first failed leaf was
`Patch4RuntimeInstallationPlayModeTests.LivingGameplayRoomGetsLockedRollbackInstance`
with a bare `Expected: not null` / `But was: null` assertion.

The failure was isolated to two stale assertions. P4.0-S deliberately passes
`null` for the old left/right eye-white bindings because the untouched master
body now owns the complete neutral eyes, irises and closed mouth. The test had
already changed those layer objects to start hidden, but still asserted that
the two private eye-white bindings were non-null. No animation clip ran, so
this result says nothing yet about the P4.0-S motion or deformation quality.

## P4.0-T exact-master face-binding test correction

- The runtime installation test now requires `eyeWhiteLeft`, `eyeWhiteRight`,
  `irisLeft`, `irisRight` and `mouthClosed` to be null.
- The same test requires both feathered lid transforms, `mouthOpen` and
  `mouthSmile` to remain bound.
- The static guard checks both sides of that contract.
- Automatic continuation advances to
  `exact-master-face-binding-review-v12` so the complete test and room-review
  sequence reruns after pull.
- The P4.0-S exact master, constrained anatomical weights, animation values and
  over-stretch/focused-limb gates are unchanged.
- Production readiness remains locked, Patch 3.5 remains active and protected
  paths remain untouched.

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

1. Pull the P4.0-T face-binding test correction into Unity
   `6000.3.19f1`.
2. Do not click the Dashboard, Test Runner or Play button; keep Unity open.
3. Let `Patch4AutoContinuation` restore/rebake every layer as FullRect, create
   one exact intact visible full-master body, rebuild the locked constrained
   `64 × 96` grid prefab, validate and complete EditMode `4 passed`, PlayMode
   `4 passed`.
4. Let the Editor-only continuation wait for a genuinely quiescent Edit Mode
   after Test Runner cleanup, then enter a separate Play Mode session, create
   the real room, freeze the Canvas bind anchors, pause the old walk routine
   and cycle all ten corrected clips on the intact body.
5. Let the driver reject weak body, focused blink or focused walk-limb motion,
   silhouette loss or excessive expansion, capture the review sheet, restore
   Patch 3.5 and its routine, then return to Edit Mode.
6. Confirm that the Console has zero errors and inspect the automatically
   focused 5 × 2 room-animation contact sheet.
7. Revise any exposed joint, detached paint, extreme stretch, foot slide or
   collapsing body/clothing region while the readiness gate stays locked.
8. Only after successful technical and human motion review, consider approving
   the readiness asset for the exact master SHA.

Detailed art instructions:

`Docs/Patch4/P4_0_C_LAYER_PRODUCTION.md`

Detailed verification instructions:

`Docs/Patch4/P4_0_D_VERIFICATION.md`

## Known limitations

- Generated PNG layers and generated runtime assets exist locally in Unity and
  are not committed as binary repository assets.
- P4.0-I passed local Unity `4/4`, but its face close-up failed human review
  because alternate expressions contained visible rectangular skin backings.
- P4.0-J passed local Unity `4/4`, but its face close-up still failed human
  review because neutral feature copies and cheek exclusions left smaller
  rectangular seams.
- P4.0-K passed its local Unity `4/4` run and face close-up review.
- P4.0-L compiled and ran in Unity `6000.3.19f1`, but failed visual review
  because Tight opaque UV crops were stretched across full-canvas meshes; its
  screenshot-only technical `PASSED` is rejected.
- P4.0-M FullRect rebuild and EditMode checks passed in Unity `6000.3.19f1`,
  but an irrelevant exact source-vertex-array assertion stopped PlayMode with
  `Failed(Child)` before the room review.
- P4.0-N passed Unity `4/4` and wrote all ten frames, but human review rejected
  nearly static motion, the collapsed turn and the audible old footstep.
- P4.0-O compiled and passed Unity `4/4`, but its corrected room animations were
  not captured because the second Play Mode session raced the Test Runner exit;
  the window displayed the stale P4.0-N PNG.
- P4.0-P completed a genuine fresh room review and correctly failed; its sheet
  exposed multiply-owned limbs and a detached face.
- P4.0-Q was exercised in Unity and rejected: its anatomical rectangles still
  looked chopped, the face shifted, walking was unreadable and Test Runner
  cleanup collided with the separate review Play Mode.
- P4.0-R was exercised in Unity and rejected: its face reconstruction erased
  neutral features, broad arm weights pulled the shirt outward and walking
  remained weak despite the minimum-only technical pass.
- P4.0-S reached real Unity Test Runner, but a stale eye-white `NotNull`
  assertion stopped PlayMode before any room animation was sampled.
- P4.0-T source and static guards are complete, but its corrected PlayMode
  contract and the P4.0-S exact face/constrained warp have not yet completed
  the user's Unity `6000.3.19f1` room review.
- The ten clips have not yet received final visual review with the production
  character visible in the actual room.
- The Canvas presentation remains hidden behind readiness.
- Figma Starter MCP limit currently prevents additional write calls.
