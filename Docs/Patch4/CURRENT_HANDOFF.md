# GameWork Patch 4.0 — Current Cross-Chat Handoff

Last updated: **2026-08-08**

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
- The user installed CoplayDev MCP locally. It can provide live Console,
  Hierarchy and Play Mode inspection once its tools are exposed to the active
  assistant session; they were not visible during the P4.0-S source pass. No
  second Unity MCP installation is required.

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

## P4.0-Q exclusive-cutout result and rejection

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

Unity `6000.3.19f1` ran the regenerated P4.0-Q candidate. It removed the
multiply-owned duplicate limbs from P4.0-P, but the fresh actual-room sheet
still failed human review:

- shoulders, elbows and hands visibly looked like cropped rectangular pieces
  instead of one smoothly deforming painted body;
- moving frames still displaced the head/face relationship;
- `FatMan_Walk_InRoom` did not read as a real walk;
- the review correctly reported failure after two Console errors;
- the Console showed `EditorSceneManager.NewScene` being called while the
  separate review Play Mode was already active, followed by the Test Runner's
  `An unexpected error happened while running tests.` message.

Rigidly transforming source rectangles is therefore rejected as the visible
runtime architecture. The P4.0-Q technical sheet must not be accepted.

## P4.0-R real Unity result and visual rejection

Unity `6000.3.19f1` completed the fresh P4.0-R review with zero Console errors.
The cutout rectangles and duplicated body pieces were gone, but human review
still rejected the character:

- the neutral face lost its eyes, irises and closed mouth;
- `TapReact_02` and especially `UpgradeReact` pulled the outer shirt into wide
  vacuum-like wings;
- arm and leg articulation remained weak and the walk did not read clearly;
- the technical review incorrectly printed `PASSED` because it enforced only
  minimum silhouette size and total changed pixels.

The causes are now confirmed in the isolated Patch 4 code. The continuous body
was deliberately inpainted over all three neutral face regions, then depended
on sparse color extraction to reconstruct them. The deformer classified broad
horizontal strips as arms, including outer shirt pixels. The review had no
maximum-expansion check and no walk-specific limb-motion check.

## P4.0-S exact-face and anatomical-warp correction

- The neutral runtime stack is exactly one layer: the untouched full-quality
  `Body/TorsoBase` master. Original eyes, irises and closed mouth are preserved
  directly and no longer reconstructed from extracted fragments.
- Blink, open-mouth and smile use complete elliptical replacement overlays:
  unchanged pixels match the master exactly, the edited center is inpainted,
  and the boundary fades before the region edge.
- Neutral eye and mouth reference layers remain in the 40-layer catalog but
  start hidden and are no longer bound as live neutral face objects.
- The intact body grid increases from `32 × 48` to `64 × 96`.
- Arm weights follow curved shoulder/elbow/wrist centerlines and stop at a
  torso boundary; leg weights follow separate hip/knee/foot centerlines and
  fade at the crotch. Broad shirt-pulling horizontal strips are removed.
- Extreme reaction scale and arm values are reduced while the walk receives a
  readable step sway, knee bend, foot rotation and counter-swing.
- Actual-room QA now rejects both silhouette collapse and excessive width,
  height or area expansion.
- `FatMan_Walk_InRoom` must additionally pass a focused arm/leg changed-pixel
  ratio; a body bob with static limbs can no longer pass.
- Automatic continuation advances to
  `anatomical-warp-face-review-v11`.
- Readiness remains locked, Patch 3.5 remains active and protected paths remain
  unchanged.

## P4.0-S real Unity test stop

Unity `6000.3.19f1` imported and rebuilt P4.0-S, but the actual-room animation
review correctly did not start because the fourth PlayMode test failed first.
The Console identified the failed leaf as
`Patch4RuntimeInstallationPlayModeTests.LivingGameplayRoomGetsLockedRollbackInstance`
with `Expected: not null` / `But was: null`.

This is a stale test contract, not evidence that the new body rig or animation
curves failed. P4.0-S intentionally binds `eyeWhiteLeft`, `eyeWhiteRight`, both
irises and `mouthClosed` as `null` because those neutral pixels now live inside
the one untouched exact-master body. The PlayMode test updated the layer
visibility checks but accidentally retained the older P4.0-R `Assert.NotNull`
requirements for the two eye-white fields. Test Runner therefore stopped before
the separate ten-clip room session.

## P4.0-T exact-master face-binding test correction

- The fourth PlayMode test now requires all five obsolete neutral face-object
  bindings to be `null`.
- It separately requires both feathered lid replacements, the open mouth and
  the smile to remain bound and non-null.
- The static guard enforces the same split, preventing a future neutral-face
  architecture change from leaving stale PlayMode assertions behind.
- Automatic continuation advances to
  `exact-master-face-binding-review-v12`, so the complete locked pipeline and
  ten-clip review restart after one pull without a button click.
- P4.0-S artwork, constrained `64 × 96` deformation, motion curves and strict
  room-review gates are unchanged.
- Readiness remains locked, Patch 3.5 remains active and protected paths remain
  unchanged.

## P4.0-T real Unity result and visual rejection

Unity `6000.3.19f1` completed the fresh token-matched P4.0-T room review. The
exact neutral face was restored and the earlier chopped rectangular body parts
did not return, but human review rejected the motion:

- the arms and especially the legs still did not produce a readable stride;
- the character mostly rocked from side to side in place;
- reaction frames still pulled skin/shirt pixels into vacuum-like side wedges;
- the technical review incorrectly reported a pass because its combined limb
  region counted whole-character translation and background pixels as limb
  motion.

Source inspection and row measurements against the exact `1024 × 1536` master
confirmed that the P4.0-S arm centerline remained about 20–40 pixels too far
inside the shirt and its feather band was nearly twice the painted arm width.
The leg centerlines were likewise biased toward the crotch. A later endpoint
trace corrected the provisional rotation diagnosis: because the authored left
and right bind chains are mirrored in X, matching raw rotation signs are needed
to produce opposite anatomical endpoint motion in a frontal stride.

## P4.0-U anatomical limb and stride correction

- The intact full-master body remains the sole neutral artwork; no anatomical
  cutout layers are re-enabled.
- The continuous mesh increases from `64 × 96` to `96 × 144` so shoulder,
  elbow, hip, knee and ankle transitions resolve at roughly 10.7 source pixels
  instead of 16.
- Arm centerlines and radii now follow the measured skin silhouette. The inner
  boundary starts near `x = 0.36` at the shoulder and reaches `x = 0.30` at the
  hand, explicitly excluding the tank top from arm ownership.
- Leg centerlines move outward from hip to shoe and use a narrow center seam;
  most painted limb pixels receive full limb motion instead of a broad blend
  back to the torso.
- Walk root sway is reduced from `±0.22` to `±0.04`. Both thigh curves now use
  the same alternating sign in frontal space, while knees, feet and both arms
  receive distinct counter-motion, so the peak is a stride rather than a
  symmetric squat/sway.
- Tap and upgrade arm extremes are reduced; the stricter mesh ownership carries
  the actual arms without pulling a wide strip of shirt.
- Motion QA first aligns the peak to the start-pose foreground centroid and
  then compares only start-pose foreground pixels. Pure whole-body translation
  can no longer count as deformation.
- Walk QA measures left arm, right arm, left leg and right leg separately and
  requires every region to pass. A combined changed-pixel total can no longer
  hide one or more static limbs.
- Maximum allowed width, height and area expansion are tightened to
  `1.16 / 1.12 / 1.20` of neutral.
- Automatic continuation advances to
  `anatomical-limb-stride-review-v13`.
- Readiness remains locked, Patch 3.5 remains active and protected paths remain
  unchanged.

## P4.0-U real Unity result and visual rejection

Unity `6000.3.19f1` completed the fresh P4.0-U run with zero reported review
errors, but the contact sheet and live motion failed human review:

- the exact body and face remained intact, but the character mostly twitched or
  rocked in place;
- the arms and legs did not show a readable shoulder/elbow/hip/knee gait;
- `FatMan_Walk_InRoom` did not read as walking;
- small body and texture changes inside broad screen regions were still enough
  for the technical review to report `PASSED`.

The P4.0-U result is rejected. A green technical message is not production
approval and Patch 4 remains locked.

## Current P4.0-V explicit joint-gait correction

- Preserve the exact `1024 × 1536` repository master and the existing
  `96 × 144` continuous Canvas grid; no generated or redrawn raster parts are
  substituted for the approved source.
- Remove the remaining full-body horizontal walk curve. Walking must come from
  the articulated skeleton, not from translating the complete character.
- Author four explicit gait phases with alternating thigh lift, knee bend,
  foot plant, shoulder counter-swing, elbow follow-through and hand motion.
- Reduce whole-body and belly scaling in tap/upgrade reactions so those clips
  cannot recreate vacuum-like expansion.
- Measure binary foreground-silhouette change after global alignment in four
  narrower independent arm/leg regions. Texture shimmer and shirt-colour
  changes no longer count as limb motion.
- Additionally require both hands to move relative to their clavicles and both
  feet to move relative to the pelvis. Root bob or body sway cannot satisfy
  this joint-space gate.
- Add an EditMode regression test that requires thigh lift, counter-swinging
  arms and the absence of whole-body horizontal walk translation.
- Automatic continuation advances to
  `articulated-gait-silhouette-review-v14`.
- Readiness remains locked, Patch 3.5 remains active and protected paths remain
  unchanged.

## P4.0-V real Unity result and state-entry diagnosis

Unity `6000.3.19f1` completed a fresh P4.0-V review and the stricter joint gate
correctly rejected it. The walk reported only `0.170` total arm/leg coverage,
left/right arm coverage `0.190 / 0.114`, left/right leg coverage
`0.231 / 0.136`, hand endpoint displacement `0.18 / 0.17` and foot endpoint
displacement `0.15 / 0.17`. The contact sheet remained visually close to Idle.

The authored gait curves were present, but the room driver called
`Animator.Play(clip.name, 0, 0f)`. Unity Animator states are addressed by their
full layer path; the generated controller keeps the layer name `Base Layer`.
The short-name request therefore did not prove or reliably enter the requested
state. The default Idle/Shift chain continued while the independent face
controller changed expressions, which explains the static body and changing
face in the fresh sheet.

## P4.0-W verified Animator-state correction

- Resolve every review state as `<actual layer name>.<clip name>` and convert
  that path to a hash.
- Require `Animator.HasState(0, hash)` before sampling and require the current
  `fullPathHash` to equal that hash both at the start and at the peak.
- Hold the controller parameters that would otherwise immediately return Walk,
  Look or Sit to Idle.
- Play each clip live, then deterministically resample its exact authored peak
  before the screenshot and relative-joint measurements.
- Record requested and observed state hashes in the JSON report. A missing or
  lost state now blocks the whole technical review.
- Extend the existing runtime PlayMode installation test to enter
  `Base Layer.FatMan_Walk_InRoom`, sample `0.00` and `0.25`, and require both
  hands to move relative to their clavicles and both feet relative to the
  pelvis.
- Automatic continuation advances to
  `verified-full-path-motion-review-v15`.
- The exact master, layer art, mesh weights and authored v14 curves are
  unchanged so this run isolates state execution from art changes.
- Readiness remains locked, Patch 3.5 remains active and protected menu, video,
  music and settings paths remain unchanged.

## P4.0-W real Unity test stop

Unity `6000.3.19f1` imported P4.0-W and ran the automatic tests. EditMode
passed, but PlayMode stopped at
`Patch4RuntimeInstallationPlayModeTests.LivingGameplayRoomGetsLockedRollbackInstance`
before the separate animation-room review began. The first assertion was:

```text
The runtime controller does not expose Base Layer.FatMan_Walk_InRoom.
Expected: True
But was: False
```

This is fresh evidence that the v15 state-entry guard worked and that no new
animation frame was sampled. Repository inspection then found the exact
controller construction error: Unity creates layer `Base Layer` and a root
state machine with the same name, but `Patch4AnimationLibraryBuilder` renamed
only that root state machine to `Patch 4 Locomotion`. Full-path hashes begin
with the top-level state-machine path, while the runtime correctly composed the
request from the actual layer name. The two names no longer described the same
state path, so `HasState` correctly returned false.

## Current P4.0-X canonical Animator root-path correction

- Build the generated root state machine with exactly the owning layer name.
- Repair any already-generated mismatched controller in the existing
  sanitizer before the prefab is saved.
- Make Editor smoke validation reject a layer/root-name mismatch and reject any
  required clip that is not exposed as a direct root state.
- Extend the existing EditMode contract test without changing the `4`-test
  count: all ten states must be direct children and the root name must equal the
  layer name.
- Retain the P4.0-W PlayMode `HasState`, `fullPathHash` and relative-limb
  regression unchanged; it now exercises the canonical
  `Base Layer.FatMan_Walk_InRoom` path.
- Advance automatic continuation to
  `normalized-controller-state-path-review-v16`.
- Keep the exact master, all layer art, mesh weights and authored v14 gait
  curves unchanged so v16 still isolates state execution.
- Keep readiness locked, Patch 3.5 active and protected menu, video, music,
  audio and settings paths unchanged.

## P4.0-X real Unity result and human rejection

Unity `6000.3.19f1` reached the fresh v16 room review with the canonical state
paths and reported a technical pass with zero room-review Console errors. The
user rejected the result after watching it live:

- `FatMan_Walk_InRoom` still read as a character standing in one place and
  twitching/rocking;
- the single captured walk peak did not prove an actual step sequence;
- arms and legs did not alternate clearly over time;
- the earlier hip and shoulder position curves pulled weighted texture regions
  and contributed to vacuum-like deformation.

Repository tracing found that production room traversal and the review were
using different motion owners. `CharacterRoutineController.WalkTo` moves the
legacy character root, and the Patch 4 instance inherits that parent movement
in normal gameplay. The locked review deliberately disables the legacy routine
and old one-shot footstep, but v16 supplied no silent replacement traversal.
It then judged Walk from only its start and `0.25` peak. A static twitch could
therefore pass without ever showing a complete walk cycle.

The v16 technical result is rejected. Patch 4 remains locked.

## Current P4.0-Y complete gait and room-travel evidence

- Keep the exact `1024 × 1536` master, the one-piece `96 × 144` Canvas body and
  all existing face bindings unchanged.
- Remove local-position curves from both thighs and both upper arms. Hip and
  shoulder bind anchors remain fixed, so weighted texture regions cannot be
  dragged like rectangular rubber patches.
- Reduce pelvis/spine rocking to a small secondary balance motion.
- Author a mirror-correct two-step gait. Matching raw rotation signs are used
  on the mirrored chains, with asymmetric thigh/knee/foot and
  shoulder/elbow/hand bends so the leading side swaps after half a cycle.
- Keep normal runtime traversal owned by the existing
  `CharacterRoutineController`; no second gameplay movement system is added.
- During the silent locked review only, move the generated Patch 4 root through
  the room while the disabled legacy routine stays paused, then restore the
  exact bind position before leaving the clip and before returning to Edit
  Mode.
- Sample eight consecutive Walk phases (`0.000` through `0.875`) instead of one
  peak and write `patch4-walk-cycle-review.png`.
- Require monotonic room travel, measurable travel distance and opposing
  anatomical hand/foot endpoint deltas after mirroring the left limb into the
  right limb's coordinate frame. Standing sway cannot pass this gate.
- Extend the existing PlayMode installation test from one Walk peak to both
  `0.25 / 0.75` peaks. One planted foot may move less at a contact pose, but
  both arms and both legs must reverse by the required amount across the full
  cycle.
- Show the eight-phase strip above the existing ten-clip contact sheet in the
  read-only review window.
- Advance automatic continuation to
  `opposing-gait-room-travel-review-v17`.
- Keep readiness locked, Patch 3.5 active and protected menu, video, music,
  audio and settings paths unchanged.

## P4.0-Y real Unity result and root rejection

Unity `6000.3.19f1` completed the fresh v17 eight-phase review. The new
validator correctly rejected the result instead of repeating a false pass:

- every legacy deformed clip exceeded the neutral width/height retention
  limits; typical peak retention was about `1.40 × 1.25–1.43` while area stayed
  close to `1.00`, proving that the one-piece image was being spread rather
  than revealing new painted anatomy;
- Walk did produce eight room-travel frames, but the displayed character still
  read as a standing body with weak limb motion;
- Walk reported `arm dot -1.000` and `leg dot -0.612`, both already inside the
  required `<= -0.200` direction limit. The failure message omitted the actual
  four displacement magnitudes, so it hid which amplitude condition failed;
- human review again reported rubber/vacuum deformation and no convincing
  footfall.

Tracing the runtime confirmed the structural cause: the visible actor is still
one flattened `1024 × 1536` PNG deformed by
`Patch4CanvasSkinDeformer` using code-defined zones. The repository metadata
already marks that flattened master as `runtimeReady: false` and forbids it as
final runtime art. More angle/weight tuning cannot turn that source into a
production skeletal rig.

The v17 result is rejected. Patch 4 remains locked.

## Current P4.0-Z / V22 complete-frame walk candidate

- Add a new project-owned RGBA candidate at
  `Assets/GameWorkPatch4/Art/Character/FatMan/V22Candidates/FatMan_WalkCycle_V22.png`.
  It contains eight equal `4 × 2` cells and keeps one complete painted body in
  every cell.
- The candidate was generated from the exact repository master as the identity,
  costume and style reference, keyed to transparency, then mechanically aligned
  to a common centre and ground line. It does not overwrite the exact master.
- `Patch4V22WalkCyclePresentation` displays one complete frame at a time for
  `FatMan_Walk_InRoom`. While it is visible, a `CanvasGroup` hides the entire
  experimental mesh stack, so no old face, sliced joint or stretched body can
  leak behind it.
- Runtime frame selection follows the real Animator state's normalized time.
  The locked room review explicitly selects frames `0…7` while retaining its
  separate monotonic room travel.
- The report now records whether the V22 sheet was ready/used and measures the
  visible alpha silhouette in both arm regions, both leg regions and every
  adjacent frame pair. Hidden legacy bones can no longer fail or falsely pass
  a complete-frame Walk.
- The accepted candidate must exceed `0.140` opposing-contact difference in
  each arm and leg region and `0.075` in the weakest adjacent-frame pair;
  duplicated poses and body-only twitch are rejected.
- The committed candidate measures arms `0.179 / 0.172`, legs
  `0.161 / 0.241` and weakest adjacent pair `0.085`; all five checks pass
  without lowering their thresholds.
- EditMode, PlayMode and the static guard require the eight-frame asset and its
  locked presentation. The candidate cannot approve art or enable Patch 4.
- Automatic continuation advances to
  `complete-frame-walk-cycle-review-v22`.
- The nine non-Walk motions still use the rejected experimental deformation
  surface. A clean V22 Walk is an isolated milestone, not final Patch 4
  approval. The production replacement plan is recorded in
  `Docs/Patch4/V22_PRODUCTION_RIG_PLAN.md`.
- Patch 3.5 remains active and no protected menu, video, music, audio or
  settings path changed.

## Exact next action

After the P4.0-Z/V22 correction is present on `patch-4.0`, run only:

```bat
git pull origin patch-4.0
```

Keep Unity open and wait. `Patch4AutoContinuation` will automatically:

1. verify and restore the exact repository master;
2. regenerate all ten masks and all 40 candidate layers;
3. rebuild the locked prefab and bind the new eight-frame V22 Walk sheet;
4. keep the exact master and all 40 legacy candidate layers available for the
   still-locked non-Walk review;
5. run pixel, rig, compilation, Editor smoke, EditMode and PlayMode checks;
6. after Test Runner becomes quiescent, create the actual gameplay room in a
   separate Play Mode session;
7. enter every clip through its verified full Animator path;
8. during Walk only, hide the whole experimental deformation stack and show
   V22 frames `0…7` as complete silhouettes while the review root moves
   silently and monotonically through the room;
9. capture the fresh eight-frame strip and the existing ten-clip sheet, report
   all four visible limb-region differences plus the weakest adjacent-frame
   difference, and keep all technical failures visible rather than
   manufacturing a pass;
10. restore Patch 3.5, exit Play Mode and open the fresh read-only review.

Expected final count:

```text
EditMode: 4 passed; PlayMode: 4 passed.
```

No Dashboard, Test Runner, Play button or review-window click is required.
Unity will briefly show the real room while it cycles the clips. Inspect the
automatically focused review after Unity returns to Edit Mode. The first strip
must now show a real alternating step: bent knees, lifted feet and arm swing,
moving from left to right, with no second body behind it. The `5 × 2` sheet
below may still reject the nine legacy deformed clips; that is an honest known
blocker, not permission to weaken the checks.

## Do not do yet

- Do not approve `Patch4ArtReadiness.asset`.
- Do not force `productionArtApproved = true`.
- Do not activate Patch 4 in runtime.
- Do not merge `patch-4.0` into `main`.
- Do not modify protected menu/audio/settings files.

## Work after the P4.0-Z/V22 automatic room review

- Inspect the eight complete Walk frames first and keep activation locked.
- Reject any identity drift, foot slide, duplicate underlay, inconsistent scale
  or non-alternating step before extending the approach.
- Preserve the failures from the other nine clips until real layered art/native
  SpriteSkin or dedicated complete frames replace the experimental mesh.
- Follow `Docs/Patch4/V22_PRODUCTION_RIG_PLAN.md`; do not resume tuning broad
  code-defined weight zones as if they were production art.
- Approve readiness only after technical and human visual review.
