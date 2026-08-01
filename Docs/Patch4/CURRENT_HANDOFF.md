# GameWork Patch 4.0 — Current Cross-Chat Handoff

Last updated: **2026-08-01**

Repository: `timurkamurka11/skinny-to-beast-clicker`

Working branch: `patch-4.0`

Canonical long-form history: `Docs/Patch4/CHECKPOINT.md`

This file is the latest operational continuation point. Read it before doing
more Patch 4 work.

## User workflow preference

The assistant edits and commits Patch 4 files directly to GitHub.

The user should normally need only:

```bat
git pull origin patch-4.0
```

Do not ask the user to edit code or operate Patch 4 Dashboard/Test Runner
buttons. Continue the workflow through `Patch4AutoContinuation` whenever a safe
automatic step can replace a manual click.

Do not introduce Unity installers, Unity Hub installers, branch switching
automation, stash/reset workflows or unrelated local-file synchronization
unless the user explicitly requests them.

## Protected scope

Do not change:

- `MainMenuLoop.mp4`;
- menu scenes, prefabs, transitions or button logic;
- music, ambient audio or audio mixers;
- settings UI, persistence, language, vibration or notifications.

Patch 4 work remains isolated under:

- `Assets/GameWorkPatch4/`
- `Docs/Patch4/`

## Confirmed Unity environment

- Unity Editor: `6000.3.19f1`
- Project opens and compiles outside Safe Mode.
- `Assets/Scenes/SampleScene.unity` is only a bootstrap scene.
- The full gameplay room is created dynamically at runtime.

## Completed art and validation pipeline

- Expired Adobe links were replaced by repository-owned local source
  restoration.
- The former embedded source was only `96 × 144` and was being enlarged to
  `1024 × 1536`; this was the confirmed cause of the weak/pixelated review.
- A real `1024 × 1536` transparent Photoshop/Firefly quality master is now
  committed at
  `Assets/GameWorkPatch4/Art/Character/FatMan/FatMan_NeutralFront_Master.png`.
- Button 1/local automation restores the exact committed bytes only after
  checking SHA-256, dimensions and RGBA format; no Adobe/network access is
  required in Unity.
- Ten deterministic masks restore locally from that quality master.
- The baker produces all 40 canonical layers.
- The former five-pixel joint scaffolds have been replaced by wide,
  texture-preserving hidden continuations sized independently for neck,
  shoulders, elbows, wrists, hips, knees, ankles, belly and shirt hem.
- The face baker now creates neutral eye/mouth restoration patches, painted
  closed lids, a real open mouth and a smile over a deterministic skin
  underlay; these remain human-review candidates, not approved production art.
- The Animator Controller contains all ten required clips, including
  `FatMan_Blink_Random`.
- Rig contract validation passes.
- Compilation and Editor prefab smoke validation pass.
- The readiness gate stays locked and Patch 3.5 remains active.

Important commits in that path include:

- `a921ea1` — embedded approved draft art source;
- `12e37f2` — local source/mask restoration;
- `0794670` — hidden joint scaffolds and shadow;
- `9e5e295` — correct FX leakage validation;
- `e0fd371` — blink clip bound into the Animator Controller;
- `524b449` — automatic animation-library verification;
- `83eabc1` — automatic EditMode and PlayMode test runner;
- `c596512` — locked runtime installation in `LivingGameplayScene`;
- `0386784` — Canvas-compatible 40-layer room presentation;
- `5ea24ef` — locked neutral-pose review and automatic `4/4` verification;
- `e25763d` — exact 1024 × 1536 repository quality master;
- `20c4e43` — feathered feature-only face transitions;
- `38f885f` — token-matched fresh actual-room review lifecycle.

## Real Unity test result

The user ran the automatic test continuation successfully.

Confirmed Console result:

```text
Patch 4 automated verification PASSED.
EditMode: 4 passed; PlayMode: 4 passed.
```

Reports:

- `Library/GameWorkPatch4Reports/patch4-test-report.json`
- `Library/GameWorkPatch4Reports/patch4-editmode-results.xml`
- `Library/GameWorkPatch4Reports/patch4-playmode-results.xml`

This is a real Unity result from `6000.3.19f1`, not a static inference.

## Actual gameplay-room discovery

There is no authored scene containing a persistent
`CharacterRigController`.

The runtime path is:

```text
GameplayWindow
└── LivingGameplayScene
    └── CharacterActors
        └── CharacterRoot (CharacterRigController)
```

`GameplayVisualStageController.BuildCharacter()` instantiates:

```text
Resources/UI/Gameplay/Living/CharacterRig2D.prefab
```

The separate `GameEntryScreen` also creates a temporary legacy character, but
Patch 4 must not attach there during this integration step.

## Verified runtime-room integration

Unity `6000.3.19f1` confirmed the isolated integration entirely inside
`Assets/GameWorkPatch4/`:

- `Patch4PrefabBuilder` now generates the locked prefab under the isolated
  Patch 4 `Resources` folder so runtime code can load it.
- `Patch4RuntimeInstaller` waits for a `CharacterRigController` specifically
  below `LivingGameplayScene`.
- It instantiates `FatMan_Patch4_Instance` beside the real runtime character.
- It binds rollback visibility and legacy gameplay signals.
- It explicitly calls `SetPatch4Enabled(false)`.
- Patch 3.5 remains visible and the production-art approval flag is untouched.
- Editor smoke validation now confirms that the runtime resource is loadable.
- A fourth PlayMode test verifies installation, binding, hidden Patch 4 visuals
  and visible Patch 3.5 rollback.
- The static guard verifies that runtime installation cannot enable Patch 4.

The user's Console showed the installation message, zero warnings/errors and:

```text
Patch 4 automated verification PASSED.
EditMode: 4 passed; PlayMode: 4 passed.
```

## Verified Canvas room presentation

Unity `6000.3.19f1` confirmed the Canvas integration without changes to the
legacy room, menu, audio or settings code:

- `Patch4CanvasPresentation` converts all 40 full-canvas painted sprites into
  non-interactive `UI.Image` layers.
- The images live in one flat hierarchy with deterministic canonical ordering.
- Each image captures a frozen bind anchor; live motion comes only from its
  Canvas skin matrix so the transform is never applied twice or cancelled.
- The approved `1024 × 1536` master is fitted to the existing `720 × 1280`
  character room at the legacy `0.74` presentation scale.
- The painted pelvis is aligned to the existing gameplay character origin.
- SpriteRenderer fallbacks are disabled so they cannot compete with the
  Screen Space Overlay Canvas.
- Eyelids and mouth poses are rebound to the Canvas images.
- Editor smoke validation checks all 40 images and disabled fallbacks.
- The fourth PlayMode test now also verifies the room Canvas, image count,
  scale, pelvis alignment and locked rollback visibility.
- `Patch4CanvasPresentation` has no activation API and never changes readiness.
- `Patch4VisualRoot` remains inactive and Patch 3.5 remains visible.

The user's Console showed the Canvas-ready installation message, zero
warnings/errors and:

```text
Patch 4 automated verification PASSED.
EditMode: 4 passed; PlayMode: 4 passed.
```

## Neutral-pose QA verified in real Unity

The user completed the automatic neutral-pose run in Unity `6000.3.19f1`:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- zero Console warnings and errors;
- the three-panel approved / assembled / difference window opened
  automatically;
- Patch 4 remained locked and Patch 3.5 remained visible.

The comparison proved that the split/reassembly preserved the old source, but
also revealed that the old source itself was visibly low-resolution. Repository
inspection found the exact cause: a `96 × 144` indexed preview was being
bilinearly enlarged to `1024 × 1536`.

## Quality-master pass verified in real Unity

The user verified the replacement pass in Unity `6000.3.19f1`:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- zero Console warnings and errors;
- the quality master and assembled neutral pose were both sharp;
- the user explicitly confirmed the result was “many times better”;
- the difference panel showed only the expected thin silhouette contour and no
  false ground-shadow block;
- Patch 4 stayed locked and Patch 3.5 stayed visible.

The verified pass remains isolated and read-only:

- the committed exact source is `1024 × 1536`, 8-bit RGBA;
- SHA-256 is
  `7b151f1ded93f3852bc8a7218ab26f94298b7f822094304bbcea9c076cad72a3`;
- the former embedded preview class was removed;
- local restoration refuses a mismatched checksum, size or PNG format;
- `Patch4NeutralPoseValidator` composites only the canonical 18-layer neutral
  runtime stack; required reference/duplicate layers are excluded;
- alternate lids, open/smile mouths, sweat, impact FX and the runtime-only
  ground shadow are excluded from the neutral master comparison;
- the ground shadow still exists in the runtime layer pack, but no longer
  creates a false magenta difference beneath the shoes;
- it compares the assembled pose pixel-by-pixel with the current master;
- it writes composite, difference, three-panel review PNG and JSON metrics to
  `Library/GameWorkPatch4Reports/`.
- the report always records `humanReviewRequired: true` and
  `activationAllowed: false`.
- Editor smoke validation requires a complete neutral composite and confirms
  that activation remains blocked.
- After automatic EditMode/PlayMode verification,
  `Patch4NeutralPoseReviewWindow` opens with the locked quality master,
  assembled neutral pose and pixel difference.
- No review button, Dashboard command or Test Runner click is required.

## P4.0-I real Unity result and visual rejection

The user completed the automatic P4.0-I run in Unity `6000.3.19f1`:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- zero Console warnings and errors;
- the four-expression review opened automatically.

The technical pass did not pass human visual review. Neutral remained sharp,
but blink, open-mouth and smile showed large straight-edged skin rectangles.
The cause was architectural: alternate features were composited on top of the
already-active open eyes and closed mouth, so each alternate carried an opaque
skin backing to erase the previous feature. Patch 4 remained locked.

## P4.0-J real Unity result and visual rejection

The user completed P4.0-J in Unity `6000.3.19f1`.

The first import exposed one compile-time `IDictionary` /
`IReadOnlyDictionary` mismatch in `Patch4NeutralPoseValidator`. Commit
`06e63fa` corrected only that signature. The subsequent automatic run produced:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- zero Console warnings and errors;
- the four-expression review opened automatically.

P4.0-J was a major visual improvement over P4.0-I, but human review still
rejected it. Small light rectangular seams remained around both closed eyes,
the open mouth and the smile. The alternate layers were already transparent;
the remaining seam came from two neutral-state operations:

- `Face/EyeWhiteL`, `Face/EyeWhiteR` and `Face/MouthClosed` still copied their
  whole rectangular master regions;
- `Face/CheekL` and `Face/CheekR` removed those regions with a hard rectangular
  alpha cut.

Technical `4/4` therefore did not approve the art. Patch 4 stayed locked.

## P4.0-K feathered face-transition pass verified

The corrective pass:

- keeps the verified joint continuations from P4.0-I;
- keeps the elliptical boundary-driven inpainting inside `Head/HeadBase`;
- extracts neutral open eyes and the closed mouth as sparse transparent
  high-detail features by comparing the master against the inpainted skin
  field instead of copying rectangular master patches;
- removes neutral eye and mouth details from overlapping cheek layers with a
  soft elliptical alpha feather rather than a hard rectangle;
- keeps lid, open-mouth and smile PNGs feature-only and transparent;
- hides both eye whites and both irises during the closed phase of a blink;
- restores all four open-eye layers when the blink opens;
- continues to rebuild every preview pose from mutually exclusive canonical
  layers in runtime order;
- validates all nine swappable neutral/alternate feature layers for sparse
  alpha, border contact and paint outside the allowed face region;
- counts abrupt transparent-to-opaque pixels along every former rectangular
  cheek boundary and blocks the layer pack if more than six remain;
- records `facePoseUsesReplacementComposition: true` and
  `faceReplacementLayersClean: true`, plus
  `faceTransitionLayersFeathered: true`, in the locked QA report;
- keeps `humanReviewRequired: true` and `activationAllowed: false`.

Unity `6000.3.19f1` then confirmed:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- zero Console warnings and errors;
- neutral, blink, open-mouth and smile all retained the same face;
- the former rectangular and elliptical halos around eyes and mouths were no
  longer visible in the close-up supplied by the user;
- Patch 4 remained locked and Patch 3.5 remained active.

This completes the face-transition correction. It does not approve production
art.

## P4.0-L real Unity result and visual rejection

The user ran P4.0-L in Unity `6000.3.19f1`. The automatic room session reached
all ten clips and restored rollback mode, but the result failed human review:

- every contact-sheet frame showed stretched/collapsed skin fragments instead
  of the assembled character;
- `FatMan_Turn` collapsed almost to a vertical line;
- the Console repeatedly logged
  `Character stage 4 was selected but did not produce a visible rig`;
- the old driver still printed a technical `PASSED` because it checked only
  that ten screenshots existed.

The root causes were isolated to Patch 4:

- the layer importer left sparse full-canvas PNGs as `SpriteMeshType.Tight`;
- `DataUtility.GetOuterUV` therefore supplied the small opaque crop while the
  deformer spread that crop across the complete `1024 × 1536` image rectangle;
- the review disabled the legacy visual root with `SetActive(false)`, causing
  the existing stage controller to regard Stage 4 as invisible and retry it.

P4.0-L is rejected. Its technical message is not accepted as evidence of a
valid character or motion pass. Readiness remains locked.

## P4.0-M full-canvas UV correction — Unity result

The corrective pass:

- forces all regenerated layer sprites to `SpriteMeshType.FullRect`;
- disables `Image.useSpriteMesh` for the full transparent canvas;
- maps the deformer grid from `Sprite.rect` and source texture dimensions
  instead of a Tight opaque outer-UV crop;
- extends Editor smoke and PlayMode checks to require all 40 FullRect sprites,
  uncropped UVs and source-mesh bypass on every Canvas Image;
- keeps the Patch 3.5 visual hierarchy logically active during the temporary
  review and hides it only with a reversible `CanvasGroup`;
- captures a clean room background before Patch 4 is shown;
- compares every animation frame with that background and rejects collapsed,
  undersized or missing character silhouettes;
- records and blocks any Console error emitted during the room review;
- cannot print technical `PASSED` unless ten captures, all silhouette checks,
  zero review errors, locked readiness and rollback restoration all succeed;
- never calls `SetPatch4Enabled(true)` and never changes readiness.

Unity `6000.3.19f1` confirmed that the Editor-side rebuild and validations
passed, including all FullRect importer, full-canvas UV and `useSpriteMesh`
checks. EditMode completed successfully. The fourth PlayMode test then stopped
the workflow with `Failed(Child)` before the actual-room review began.

The only newly added PlayMode-only condition not already covered by the passing
Editor smoke report required `Sprite.vertices.Length == 4`. That value is
Unity's imported source-mesh representation. It is not consumed by the visible
Patch 4 presentation because `Patch4CanvasSkinDeformer.ModifyMesh` clears the
source geometry and constructs its own weighted full-canvas grid. Exact source
vertex-array cardinality was therefore an implementation-detail assertion, not
a safety or visual contract.

## P4.0-N runtime-contract correction — Unity result

The corrective pass:

- retains FullRect import, full-canvas UV mapping and `Image.useSpriteMesh =
  false`;
- retains the runtime requirement that all 40 deformers report full-canvas UVs
  and valid bind poses;
- removes only the irrelevant exact `Sprite.vertices` array-length assertion;
- continues to validate the custom generated Canvas grid through deformer
  counts, weighted-layer counts and bind-pose state;
- recursively records every failed leaf test name, result and assertion message
  in `patch4-test-report.json`;
- prints the first real child failure directly in Console instead of only
  `Failed(Child)`;
- advances the automatic continuation run id so the corrected `4/4` and locked
  room review start without a manual click;
- keeps production readiness locked and Patch 3.5 as the active character.

Unity `6000.3.19f1` completed P4.0-N with EditMode `4 passed`, PlayMode
`4 passed`, zero Console errors and a generated actual-room contact sheet.
Technical validation printed `PASSED`, but human motion review rejected it:

- most clips showed almost no visible movement;
- `FatMan_Turn` still collapsed to a near-vertical line;
- the old synthetic/robot-like walking footstep was audible during the Patch 4
  review.

The static appearance was not an art problem. `Patch4CanvasPresentation`
updated every layer follower to the current bone transform in `LateUpdate`,
while `Patch4CanvasSkinDeformer` applied that same bone transform relative to
the moving follower. The primary transform therefore cancelled itself.
`FatMan_Turn` separately authored a horizontal scale of `0.12`, directly
creating the line. P4.0-N is technically complete but fails human motion
review and remains locked.

## Current P4.0-O visible-motion and silent-review correction

The corrective pass:

- aligns every Canvas follower once for bind-pose capture and freezes it
  afterward, allowing live bone matrices to produce visible deformation;
- exposes and validates `BindAnchorsFrozen` in Editor smoke and PlayMode;
- replaces the `0.12` turn squash with a safe body pivot using pelvis, spine,
  head, arm, position and mild `0.94–1.02` scale motion;
- expands breathing, weight shift, looking, two tap reactions, walking,
  sitting/leaning and upgrade motion across the body, arms, legs and head;
- captures each clip at its authored action peak instead of sampling several
  clips at a neutral crossing;
- compares each peak against that clip's start pose and requires a
  clip-specific minimum changed-pixel ratio;
- compares every animated silhouette against the frozen neutral silhouette, so
  a moving expected rectangle cannot hide another collapse;
- pauses the legacy room routine and Patch 3.5 signal bridge only during the
  isolated review, stops the old one-shot footstep, then restores both;
- keeps ambient room audio, protected audio code, menu, video, music and
  settings untouched;
- keeps readiness false and Patch 3.5 active outside the temporary review.

Unity `6000.3.19f1` compiled P4.0-O and again completed EditMode `4 passed`
and PlayMode `4 passed`. The corrected room review itself did **not** run.
`StartAfterTests()` was invoked while the Test Runner was still leaving its own
Play Mode session. The review marked itself in progress too early, then treated
the Test Runner's normal `EnteredEditMode` event as an interrupted review.
Console recorded `Play Mode ended before the locked room review completed`.

The animation window then loaded the previous contact-sheet PNG without
checking whether it belonged to the current run. That stale sheet still showed
the old nearly static poses and the old vertical `FatMan_Turn`. It is rejected
and is not evidence that the P4.0-O animation curves ran.

## P4.0-P fresh room-review result and rejection

The P4.0-P correction:

- adds an explicit waiting stage between Test Runner Play Mode and the separate
  locked room-review Play Mode;
- persists that waiting state across Unity domain reloads;
- clears the previous room report and PNG before a new review begins;
- gives every review a unique run token written into its JSON report;
- opens the animation window only when the report and PNG match the current
  token and the report is complete;
- labels a fresh technical failure as failed instead of displaying unconditional
  success text;
- blocks an old contact sheet even if stale files somehow remain;
- advances the automatic continuation id to rerun the complete pipeline after
  pull;
- leaves the P4.0-O frozen-anchor animation and silent-review corrections
  intact;
- keeps readiness locked and all protected paths unchanged.

Unity `6000.3.19f1` then completed the separate token-matched room review. The
new sheet was fresh and the validator correctly rejected it instead of showing
an old pass:

- `FatMan_Blink_Random` changed only `0.001` of the neutral silhouette against
  the old `0.003` whole-body minimum;
- neutral looked plausible only because overlapping copies occupied the same
  pixels;
- LookAround, tap, turn, sit and upgrade frames exposed duplicate heads, arms
  and legs;
- eyes and mouths separated from the head as their Eye/Jaw bones moved;
- the fresh P4.0-P sheet therefore fails both technical and human review.

The source cause was the combination of conservative rectangular repository
masks, every reference layer being visible, and multi-bone grids on cutout
segments. Several required PNGs owned the same source pixels. Bone motion then
separated those coincident copies.

## Current P4.0-Q exclusive cutout and rigid-face correction

- The baker assigns every neutral source pixel to exactly one live body layer.
- Only small named neck, shoulder, elbow, wrist, hip, knee and ankle
  continuations may overlap.
- Duplicate/reference torso, belly, chest, ears, brows, irises, nose, cheeks,
  shirt overlay, bottoms and shoes remain in the 40-layer catalog but start
  hidden.
- Head, all face replacements and every arm/leg segment follow exactly one
  parent bone; only `Clothes/ShirtBase` keeps a soft multi-bone grid.
- The Canvas deformer refreshes both rigid one-bone cutouts and the soft shirt
  every frame; rigid head/face/limb meshes can no longer remain frozen while
  their bones animate.
- All face replacement sprites use the Head pivot. Animation clips no longer
  transform Eye bones in addition to the independent painted blink controller.
- Draft QA blocks any multiply-owned live pixel outside an authorized joint and
  measures neutral coverage from only the 18 layers that actually render.
- Blink keeps a non-zero whole-character requirement and additionally must pass
  a stricter focused face-region changed-pixel check.
- Automatic continuation advances to
  `exclusive-cutout-rig-review-v9`.
- Readiness stays locked and Patch 3.5 remains the active rollback character.

## Exact next action

After the P4.0-Q correction is present on `patch-4.0`, run only:

```bat
git pull origin patch-4.0
```

Keep Unity open and wait. `Patch4AutoContinuation` will automatically:

1. verify and restore the exact repository master;
2. regenerate all ten masks and all 40 candidate layers;
3. enforce one live owner per neutral body pixel and restore only named joint
   continuations;
4. import every layer as FullRect and rebuild the locked hybrid Canvas prefab
   with rigid head/face/limbs and a soft central shirt;
5. assemble and compare the locked 18-layer neutral runtime pose;
6. write the neutral and four-expression review images;
7. validate all 40 skin bindings, frozen bind anchors, full-canvas UVs,
   exclusive live-pixel ownership and every required rigid cutout;
8. run pixel, rig, compilation and Editor smoke validation;
9. run all EditMode tests;
10. enter Play Mode and run all PlayMode tests;
11. after `4/4`, wait for Test Runner to return fully to Edit Mode, then enter
    a separate Play Mode session and create the actual gameplay room;
12. capture a clean background and neutral reference, then play all ten clips
    and block weak start-to-peak motion, weak focused blink motion, a collapsed
    silhouette or any Console error while the legacy robot-like footstep stays
    paused;
13. write a token-matched fresh report and contact sheet, restore Patch 3.5,
    exit Play Mode and open the read-only review windows.

Expected final count:

```text
EditMode: 4 passed; PlayMode: 4 passed.
```

No Dashboard, Test Runner, Play button or review-window click is required.
Unity will briefly show the real room while it cycles the clips. Inspect the
automatically focused 5 × 2 animation contact sheet after Unity returns to Edit
Mode. The face and neutral windows remain open behind it.

## Do not do yet

- Do not approve `Patch4ArtReadiness.asset`.
- Do not force `productionArtApproved = true`.
- Do not activate Patch 4 in runtime.
- Do not merge `patch-4.0` into `main`.
- Do not modify protected menu/audio/settings files.

## Work after the P4.0-Q automatic room review

- Inspect the actual-room contact sheet and the live ten-clip cycle while
  keeping production activation locked.
- Reject and revise any exposed joint, detached layer, excessive stretch,
  overlap, foot slide or collapse at an animation extreme.
- Keep the exclusive cutout ownership and rigid bindings locked until the ten
  motions pass human review.
- Approve readiness only after technical and human visual review.
