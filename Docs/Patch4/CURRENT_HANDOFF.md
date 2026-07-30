# GameWork Patch 4.0 — Current Cross-Chat Handoff

Last updated: **2026-07-30**

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
- `20c4e43` — feathered feature-only face transitions.

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
- Each image follows its assigned Patch 4 bone in `LateUpdate`.
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
- `Patch4NeutralPoseValidator` now composites 33 neutral comparison layers;
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

## Current P4.0-N runtime-contract and failure-reporting correction

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

## Exact next action

After the P4.0-N correction is present on `patch-4.0`, run only:

```bat
git pull origin patch-4.0
```

Keep Unity open and wait. `Patch4AutoContinuation` will automatically:

1. verify and restore the exact repository master;
2. regenerate all ten masks and all 40 candidate layers;
3. preserve the verified hidden continuations and feathered face states;
4. import every layer as FullRect and rebuild the locked Canvas-weighted
   prefab;
5. assemble and compare the locked neutral pose;
6. write the neutral and four-expression review images;
7. validate all 40 skin bindings, full-canvas UVs and multi-bone weight maps;
8. run pixel, rig, compilation and Editor smoke validation;
9. run all EditMode tests;
10. enter Play Mode and run all PlayMode tests;
11. after `4/4`, enter Play Mode again and create the actual gameplay room;
12. capture a clean background, then play all ten clips and block any
    collapsed silhouette or Console error;
13. restore Patch 3.5, exit Play Mode and open all three read-only review
    windows.

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

## Work after the P4.0-N automatic room review

- Inspect the actual-room contact sheet and the live ten-clip cycle while
  keeping production activation locked.
- Reject and revise any exposed joint, detached layer, excessive stretch,
  overlap, foot slide or collapse at an animation extreme.
- Keep the Canvas weight maps locked until the ten motions pass human review.
- Approve readiness only after technical and human visual review.
