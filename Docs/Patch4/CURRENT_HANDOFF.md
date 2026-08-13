# GameWork Patch 4.0 — Current Cross-Chat Handoff

Last updated: **2026-08-13**

Repository: `timurkamurka11/skinny-to-beast-clicker`

Working branch: `patch-4.0`

Canonical long-form history: `Docs/Patch4/CHECKPOINT.md`

This file is the latest operational continuation point. Read it before doing
more Patch 4 work.

## Current P4.0-AM / V35 deterministic live-motion review

The first V34 Unity run compiled, completed a `30.9 s` live gameplay preview
with `288` continuous samples and no frame swaps, then failed only the paused
evidence pass. Blink measured `0.014` against `0.015` because the time-driven
blink had already begun reopening before screenshot capture. Walk showed valid
hand trajectories (`0.334/0.342`) and nearly valid feet (`0.225/0.096`), but
continuity was `0.000` because review called `Animator.Play` anew at each of its
sixteen samples, resetting the stateful planted-foot solver.

V35 fixes the evidence path without weakening its thresholds. The face
controller exposes an Editor-only exact closed-lid review pose. Walk sampling
keeps one uninterrupted Animator/IK state and verifies it in place rather than
restarting the state. The existing range-normalized continuity formula and all
of its thresholds stay unchanged, so a one-frame reset/jump remains blocked.
Automatic token: `deterministic-motion-review-v35`. Production gameplay
motion, art, menus, video, music/audio, settings, readiness lock and Patch 3.5
rollback remain unchanged.

## Current P4.0-AL / V34 front-facing contract repair

The first V33 Unity run compiled and completed PlayMode, but the combined test
run failed in EditMode because `Patch4ContractEditModeTests` still required the
superseded strings `CharacterFacing.SideLeft` and
`SetEditorWalkFacingSign(walkFacingSign)`. V33 intentionally replaced both with
the stable front-depth route: `CharacterFacing.Front`, `KeepFrontFacingRig()`
and `SetEditorWalkFacingSign(1)`. V34 updates that regression contract without
weakening it and advances the automatic continuation token to
`front-facing-contract-repair-v34`, so the next pull repeats the complete
rebuild and EditMode/PlayMode run automatically. The V33 motion implementation,
art, menus, video, music/audio, settings, readiness lock and Patch 3.5 rollback
remain unchanged.

## Current P4.0-AK / V33 continuous layered motion

The user's first V32 import proved that the sixteen-image atlas was both
technically and architecturally wrong for this target. Unity reported two
`CS0120` calls to the instance-only row resolver, rejected
`FatMan_WalkRight_16_V32.png` as unreadable, and entered a camera-less
`InitTestScene`. More importantly, the user correctly rejected the visible
result as a faster slideshow: a complete-body `RawImage` still replaced the
character pose by pose instead of animating one rig.

V33 removes that path globally:

- the corrupt `V32Smooth` PNG and the obsolete
  `Patch4V22WalkCyclePresentation` component are deleted;
- `Patch4V23FullFramePresentation` keeps the old sheets only as disabled
  reference/QA sources and can no longer assign a live texture, show the
  `RawImage`, or hide the layered character;
- the one persistent Canvas character now consists of torso, rigid head,
  head-bound independent face and four continuous whole-limb sprites; its
  bones are interpolated every rendered frame by clamped-auto curves;
- Walk is a `1.6 s` heavy eight-pose control curve sampled at sixteen
  continuous review times. The finalizer preserves the phase-four support pose
  and closes only the loop seam; the former forced half-cycle neutral reset
  that caused a visible hitch is gone;
- arm, forearm, hand, thigh, shin and foot all carry opposing motion. The
  planted-foot solver now follows the actual 2D travel vector, not only X;
- Idle, shift, look, taps, turn, sit and upgrade keep their natural authored
  durations. Persistent state transitions use `0.12-0.18 s` blends and tap/
  trigger entries use `0.10 s` blends;
- the locked normal-game route is no longer a single horizontal line. It uses
  a narrow central X range, distinct safe Y depths and continuously interpolated
  scale, so the front-authored rig walks mainly into/out of the room instead of
  sliding sideways;
- the preview no longer mirrors that frontal artwork from legacy
  `SideLeft`/`SideRight` signals. All projected anchors request `Front`, and the
  continuous rig keeps one stable facing while it travels through depth;
- actual-room QA no longer grades atlas silhouettes. It measures live hand and
  foot trajectories, their continuity, one persistent visible body and
  monotonic 2D room travel. Body twitch, frozen limbs and frame swapping cannot
  pass;
- the existing gameplay mapping remains authoritative: idle, routine actions,
  accepted taps, movement, facing changes and purchases select their matching
  Animator states through `Patch4CharacterStateMachine` and
  `Patch4LegacySignalBridge`.

Automatic continuation token: `continuous-layered-motion-v33`. Repository
static validation, whitespace checks and C# syntax parsing pass. Unity
`6000.3.19f1` compilation, automated EditMode/PlayMode tests and fresh human
motion review run automatically after the next pull. Readiness remains locked
and Patch 3.5 remains the rollback owner.

No protected menu, `MainMenuLoop.mp4`, music/audio, settings or production
readiness asset was changed. A true side-profile layered rig remains a separate
art deliverable: generating or slicing one flat profile image would recreate
the already rejected cut-joint/vacuum artifacts, so V33 deliberately uses the
clean continuous front rig with depth travel rather than faking a paper doll.

## Superseded P4.0-AJ / V32 sixteen-phase smooth locomotion

The user's fresh normal-game observation confirms that V31 routes Walk, but
rejects the visible result: V29 only accelerated an eight-image whole-body
sequence, so the character still read as a fast slideshow and sideward slide.
Inspection of `FatMan_WalkRight_V23.png` confirmed that its cells were not
adjacent gait phases and included near-duplicate high-knee poses. Runtime
cross-dissolve and optical-flow experiments were rejected before commit because
they produced doubled arms, blurred feet and the vacuum artifacts already
rejected in V17-V21.

V32 changes the source motion rather than increasing playback speed:

- a versioned repository-owned `1536 x 1024` RGBA atlas under `V32Smooth/`
  contains sixteen distinct complete-body profile-right phases in a 4x4 grid;
- every cell is normalized to one body scale and common shoe line while V23
  remains untouched as rollback art;
- Walk playback is slowed from `0.56 s` to `1.28 s`, yielding a readable
  12.5-frame-per-second cycle instead of a rapid eight-pose flash;
- the presentation supports the existing 4x2 pose sheets and the new 4x4 Walk
  sheet without displaying or blending two bodies;
- room review and PlayMode contracts now require all sixteen phases, including
  adjacent-frame, silhouette, profile direction, clipping and ground-line QA;
- V31 public action routing and V29 reversible left/right mirroring remain the
  owners of state and travel direction;
- automatic continuation advances to
  `smooth-sixteen-phase-locomotion-v32`.

V32 does not change menus, scenes, video, music, audio, settings, readiness or
Patch 3.5 rollback behavior. Static repository validation passes; Unity
`6000.3.19f1` compilation, automated tests and visual pacing remain pending the
user's next pull.

## Current P4.0-AI / V31 direct locomotion action routing

The user's first V30 automatic run reached the actual-room live preview and
returned the new bounded diagnostics instead of an ambiguous timeout:

```text
Gameplay action did not enter Base Layer.FatMan_Walk_InRoom after 27
observed frame(s): expected hash -1614043475, current hash -212395280,
transition False, next hash 0, Speed 1.000, review API ready True.
```

The observed full-path hash `-212395280` resolves exactly to
`Base Layer.FatMan_Idle_Breathe` (and the expected hash resolves to Walk).
This proves that the public action bridge accepted `Speed = 1`, the review API
was enabled and the layer was not blocked by a one-shot or transition: the
generated controller simply remained in Idle. The blank review cells and
camera-less view are consequences of that early technical stop, not missing
art.

V31 moves persistent locomotion ownership into the public gameplay bridge:

- `SetWalkSpeed` still writes the `Speed` float used by contextual exits and
  the serialized controller, but an Idle movement request now uses one
  fixed-time `CrossFadeInFixedTime` into the full-path Walk state;
- repeated real-game `Speed = 1` ticks cannot restart the gait because the
  router does nothing when Walk already owns or is entering the layer;
- `Speed = 0` explicitly leaves current/pending Walk for Idle, while an active
  tap, blink, turn or upgrade one-shot is allowed to finish and use its
  existing context exit;
- the existing PlayMode installation test now clears every persistent intent,
  proves the public action reaches Walk, then issues another movement tick and
  proves normalized Walk time advances instead of resetting;
- static guards require the direct action route and the non-restart
  regression; the automatic token advances to
  `direct-locomotion-action-routing-v31`.

V31 changes no artwork, room layout, menu, scene, video, music, audio,
settings, readiness or Patch 3.5 rollback behavior. Repository static
validation must pass before publication. Unity `6000.3.19f1` runtime proof is
the automatic run after the user's next pull.

## Current P4.0-AH / V30 frame-observed gameplay routing hotfix

The user's first V29 automatic run reached the actual-room live preview but
stopped before any evidence frame was captured. Unity reported the exact first
failure:

```text
Gameplay action did not enter Base Layer.FatMan_Walk_InRoom.
The uninterrupted real-time gameplay preview did not complete every
calibrated full-frame state.
```

The empty contact-sheet cells and `InitTestScene / No cameras rendering` view
are downstream symptoms of that early exit; they are not evidence that the
committed Patch 4 art or gameplay room was deleted. Source inspection found a
frame-timing hole in `RouteGameplayActionToState`: its `0.4 s` real-time loop
could expire during one heavy Editor frame and return without observing the
Animator update which occurred during that frame. Its reset also assumed that
`Animator.Update(0)` had already cleared the preceding one-shot transition.

V30 corrects the review contract without forcing a state or weakening the
gate:

- the driver resets to Idle and observes a real player update before routing
  the next public gameplay action;
- the requested state is checked before the real-time deadline, with minimum
  and maximum frame bounds plus a `1.25 s` ceiling;
- a failure now records expected/current/next hashes, transition state,
  `Speed` and `Patch4CharacterStateMachine.IsReady` instead of returning the
  ambiguous one-line error;
- the existing PlayMode installation test now proves the real
  `SetWalkSpeed(1) -> Idle -> Walk` transition before it uses direct
  `Animator.Play` for pose sampling;
- automatic continuation advances to
  `frame-observed-gameplay-routing-v30` and reruns the complete locked flow.

V29's faster cadence, adjacent Idle ping-pong, reversible safe corridor and
left/right Walk mirroring remain intact. V30 changes no artwork, menu, scene,
video, music, audio, settings, readiness or Patch 3.5 rollback behavior.

## Previous P4.0-AG / V29 safe-room cadence and facing correction

The user's first successful V28 normal-game evidence proves that Patch 4 now
binds and renders in the interactive room. It also rejects three visible
behaviors:

- the four-frame standing states advance slowly enough to read as individual
  slides;
- the only authored Walk atlas faces screen-right and was never mirrored when
  the legacy routine moved left;
- the complete-frame Patch 4 body inherited the smaller Patch 3.5 `Sofa`,
  `Window` and `Mirror` destinations, visibly standing on the sofa and the
  right dumbbell rack.

V29 corrects those exact causes inside the locked Editor preview:

- target durations are reduced for every complete-frame state; Idle uses the
  adjacent sequence `0,1,2,3,2,1`, avoiding the old last-to-first loop jump;
- the preview driver reads `CharacterRigController.Facing` each frame and
  mirrors only `FatMan_Walk_InRoom` for `SideLeft`; all front poses retain their
  authored orientation and fixed shoe line;
- the driver snapshots the five legacy `RoomAnchor` values, stops the legacy
  routine, projects them into a short horizontal central corridor
  (`x = 0.07 .. 0.09`, `y = 0.515`, scale `0.70`) between the sofa and
  right rack, then restarts that same routine;
- `Training` remains the gameplay-owned tap destination. The four other route
  signals become standing `Center` actions during this preview, so the larger
  body neither climbs the sofa/rack nor performs an unsupported sit in empty
  floor space;
- ending the preview restores every original anchor kind, position, scale,
  facing and stay time before Patch 3.5 becomes visible again.

The continuation token is `safe-room-cadence-direction-v29`. This change does
not unlock Patch 4 and does not edit legacy gameplay, menu, scene, video, music,
audio or settings assets. Repository validation is required before publishing;
Unity `6000.3.19f1` visual confirmation remains pending the user's next pull.

## Current P4.0-AF / V28 interactive-preview assembly hotfix

The user's first V27 normal-game attempt did not display Patch 4. Unity emitted
the deterministic first error:

```text
Can't add script behaviour 'Patch4InteractiveGameplayPreviewDriver' because
it is an editor script. To attach a script it needs to be outside of the
'Editor' folder.
```

The following `NullReferenceException` at
`Patch4InteractiveGameplayPreview.cs:346` was secondary: Unity returned no
component and V27 immediately called `Begin` on that missing driver. The old
Patch 3.5 body therefore remained visible and the room binding timed out.

V28 fixes that exact assembly-boundary defect without changing gameplay:

- the transient `MonoBehaviour` moved from `Assets/GameWorkPatch4/Editor/` to
  `Assets/GameWorkPatch4/Runtime/`, preserving its `.meta` GUID;
- the complete driver source is wrapped in `#if UNITY_EDITOR`, so Unity can
  attach it during an Editor Play Mode preview while player builds exclude it;
- the Editor orchestrator checks the `AddComponent` result and exits with one
  explicit binding error instead of producing a cascading null exception;
- EditMode and repository guards require the attachable path, forbid the old
  Editor-folder path and retain the readiness-lock assertions;
- automatic continuation advances to
  `interactive-preview-assembly-boundary-v28`, forcing a clean rerun after the
  next pull.

No menu, scene, video, music, audio, settings, room-anchor or production
readiness behavior changes in V28. Patch 4 remains locked and Patch 3.5 remains
the rollback owner.

## Previous P4.0-AE / V27 locked interactive normal-game preview

The user's fresh V26 run closes the Test Runner ownership checkpoint. Unity
`6000.3.19f1` reached the token-matched actual-room review after the automatic
tests, emitted zero review warnings/errors and reported:

- two uninterrupted action-routed gameplay passes;
- `14.8` seconds and `88` visible whole-frame advances;
- corrected Walk runway travel from `180.0 px` to `324.9 px`;
- a final locked animation-room technical `PASSED` result.

The user reports that the animation direction is now much better and wants to
inspect the character through the normal interactive game before changing its
room navigation. V27 adds that exact continuation without approving Patch 4:

- after a fresh passing technical room review, the read-only report windows
  are deferred and a separate Editor-only Play Mode session is queued;
- the generated legacy Animator is synchronously preflighted before Play Mode
  so its old asset transaction cannot cancel the preview;
- `GameplayWindowController.Show()` creates the real normal gameplay room and
  `Patch4RuntimeInstaller` binds the locked Patch 4 instance beside the real
  character;
- Patch 3.5 remains logically active and continues to own input, accepted taps,
  purchases, routine actions, room travel and its existing room anchors; only
  its character pixels are hidden by a reversible `CanvasGroup`;
- the existing `Patch4LegacySignalBridge` remains enabled, so real gameplay
  signals drive the Patch 4 Animator instead of a clip tour;
- the complete-frame surface gains a `UNITY_EDITOR`-only display override that
  follows the live Animator while `SetPatch4Enabled(false)` and the production
  readiness lock remain unchanged;
- Unity focuses the Game view and deliberately leaves Play Mode running until
  the user stops it; stopping restores the rollback view and then opens the
  deferred evidence windows;
- Test Runner preflight clears any stale interactive-preview ownership before
  starting a later automated run.

V27 does not add a second movement controller, free roaming, colliders or new
room paths. The preview inherits the existing five bounded legacy anchors
(`Center`, `Training`, `Sofa`, `Window`, `Mirror`). The next room-safety pass
must be based on this normal-game observation: either reduce the allowed anchor
set and emphasize standing actions, or author Patch 4-specific safe zones that
respect the visible size of the complete-frame body. No menu, scene, video,
music, audio or settings asset changes in V27.

## Current P4.0-AD / V26 Test Runner PlayMode ownership

The user's first V25 automatic Unity run reached PlayMode tests but Unity
reported:

```text
Playmode tests were aborted because the player was stopped.
An unexpected error happened while running tests.
```

This is a lifecycle failure, not an animation assertion. Source tracing found
two Editor owners capable of controlling the same PlayMode session:

- the legacy `LivingGameplayAnimatorAssetBuilder` intentionally cancels a
  normal Play request with `EditorApplication.isPlaying = false` whenever its
  generated Patch 3 Animator requires an Edit Mode transaction, then queues a
  separate non-test Play resume;
- a stale Patch 4 room-review session can retain delayed enter/exit callbacks
  across script reloads.

Either owner is invalid while Unity Test Runner controls PlayMode. V26 keeps
all changes inside Patch 4 and establishes one owner before starting tests:

- `Patch4AutomatedTestRunner` exposes its active ownership state;
- any stale room-review callbacks and SessionState are cleared in stable Edit
  Mode before EditMode tests begin;
- immediately before PlayMode tests, the generated Patch 3 Animator is
  synchronously validated, and the obsolete legacy non-test resume flag is
  cleared before and after that validation;
- room-review enter, bind and exit paths refuse to enter or stop PlayMode while
  Test Runner owns it;
- the separate actual-room review still starts only after a completed passing
  PlayMode result and the normal Test Runner return to Edit Mode;
- automatic continuation advances to
  `test-runner-playmode-ownership-v26` so the next pull reruns the full flow.

This correction does not alter gameplay animation mappings, artwork, Patch 3
runtime behavior, readiness, menu, video, music, audio or settings. The user's
fresh V26 result now confirms that Test Runner completed and the separate room
review entered, played and exited without either lifecycle owner aborting it.
Patch 4 remains locked and Patch 3.5 remains active outside the review.

## P4.0-AC / V25 gameplay-action routing foundation

The user's fresh V24 actual-room review is the current human evidence. The
single-body complete-frame architecture is now substantially better and the
animations visibly play, but the user correctly reported that their cadence
still hitches and that they must be connected to real gameplay actions rather
than remain a clip demonstration. The same review exposed two deterministic
technical failures:

- `FatMan_Walk_InRoom` travelled the hard-coded maximum of `180 px`, while the
  validator simultaneously required `313.5 px`; that gate was impossible to
  satisfy;
- the corrected V24 upgrade sheet was already authored at neutral scale, but
  runtime still multiplied it by the old `1.135` compensation, producing the
  reported `1.133 / 1.136 / 1.204` width, height and area expansion.

P4.0-AC/V25 corrects the event architecture and those measured failures:

- `Patch4LegacySignalBridge` now maps actual gameplay state to Patch 4:
  movement → Walk, `ShiftWeight` → Shift, `LookAround` → Look, the sit action
  family → Sit, facing changes → Turn and accepted taps → alternating Tap 1/2;
- successful `UpgradeManager.Purchase` notifications trigger exactly one
  `UpgradeReact`; a simultaneous art-stage change is debounced rather than
  restarting the celebration;
- full-frame blink is now an Animator trigger scheduled only during free idle,
  never while walking, reacting or performing a routine action;
- the Animator controller has explicit `Shift` and `Blink` parameters. The old
  unconditional Idle → Shift loop is removed, so every non-idle state now has
  a gameplay owner;
- one-shots return directly to the current Walk/Sit/Look/Shift intent instead
  of always flashing through Idle;
- each Animator state speed is calibrated to the same target duration used by
  the visible complete-frame presentation. The former second source/target
  time multiplication is removed, eliminating the early final-frame hold;
- fixed-duration transitions are short (`0.05–0.08 s`) and do not reactivate
  cross-faded duplicate bodies;
- the corrected V24 upgrade atlas now renders at `1.0` artwork scale;
- Walk review target travel is now a bounded `0.48` reference-body width, and
  its required minimum is a compatible `0.35` width;
- the uninterrupted room preview resets to Idle, requests every state through
  `Patch4CharacterStateMachine`, verifies the expected full-path Animator state
  and records `gameplayActionRoutingPassed` before showing each motion;
- EditMode/static contracts require all nine action parameters, their state
  destinations, both tap variants, calibrated state speeds and the absence of
  an unconditional Shift transition;
- automatic continuation advances to `gameplay-action-routing-v25`.

Repository static validation passed for the P4.0-AC/V25 source set. Its first
Unity run reached Test Runner but the Player was externally stopped before a
PlayMode result or fresh room review could be accepted. V26 supersedes that
lifecycle. Readiness remains locked, Patch 3.5 remains active, and no protected
menu, video, music, audio or settings file changed.

## Latest compile hotfix

- The first P4.0-AB publish (`caa9e98`) added a direct compile-time reference
  from `Patch4.EditModeTests` to `Patch4PrefabBuilder` in the separate Editor
  assembly.
- Unity `6000.3.19f1` correctly rejected that reference with `CS0234` at
  `Patch4ContractEditModeTests.cs:544` because the test asmdef deliberately has
  no Editor assembly reference.
- The V24 path assertion remains active, but now resolves
  `Patch4PrefabBuilder.V23UpgradeSheetPath` through the same `RequireType` /
  reflection pattern already used by the surrounding Editor contract tests.
- No runtime, scene, menu, audio, settings or art behavior changed in this
  hotfix. It retained the V24 continuation token at that time; V25 now
  supersedes it.

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

## Superseded P4.0-AB / V24 cadence and scale correction

The user's fresh V23 Unity review is the current evidence. It confirmed that
the complete-frame architecture is substantially cleaner: Walk is a real
right-facing gait, all ten states use intact painted bodies, the old deforming
mesh stays hidden and the face remains attached. It also exposed two real
problems and one review-presentation misunderstanding:

- the room driver deliberately stops at capture phases, so the contact-sheet
  pass looks slower and more stepped than gameplay;
- normal V23 frame selection inherited long legacy clip lengths, making the
  whole-frame cadence too slow even outside screenshot pauses;
- source sheets use different body scales and shoe-line padding. The face,
  profile Walk, turn/tap art and upgrade art therefore pop in size or height;
- V23 upgrade frame 5 is an enlarged torso rather than a complete body, and
  the raised-arm frame does not share the standing shoe line.

P4.0-AB corrects these findings without cross-fading or reactivating the old
rig:

- `Patch4V23FullFramePresentation` now defines responsive per-state playback
  durations. Normal Animator playback and locked preview use the same cadence;
- every source frame is measured once from its alpha silhouette. A fixed
  per-pose-family scale and automatic shoe-line correction are applied to the
  single `RawImage`, so padding cannot resize or float the actor;
- `V24Corrections/FatMan_Upgrade_V24.png` preserves the accepted identity,
  outfit and painted style but restores the cropped arms-crossed frame to a
  complete head-to-shoes body;
- `Patch4AnimationRoomReviewDriver` first plays two uninterrupted real-time
  passes inside the actual `LivingGameplayScene` (about 13 seconds, no capture
  pauses), then performs the frozen technical screenshot pass;
- the review window explicitly states that its contact sheet is paused
  evidence and that timing must be judged from the preceding live Game view;
- reports require the live preview, frame calibration and zero atlas-edge
  clipping before a technical pass;
- automatic continuation advances to
  `calibrated-live-gameplay-preview-v24`;
- readiness remains locked, Patch 3.5 remains active, and no menu, video,
  music, audio or settings file is changed.

This V24 source description is retained as history. The user's fresh V24 room
review completed and is summarized by the V25 section at the top of this file.
Do not treat the V24 run token as the current continuation.

The historical V24 automatic flow used the same single-command pull:

```bat
git pull origin patch-4.0
```

Keep Unity open and wait. `Patch4AutoContinuation` will automatically:

1. verify and restore the exact repository master;
2. regenerate all ten masks and all 40 candidate layers;
3. rebuild the locked prefab, bind the V23 sheets and the corrected V24
   upgrade sheet;
4. keep the exact master and all 40 legacy candidate layers available only as
   hidden rollback diagnostics;
5. run pixel, rig, compilation, Editor smoke, EditMode and PlayMode checks;
6. after Test Runner becomes quiescent, create the actual gameplay room in a
   separate Play Mode session;
7. focus Game view and play two uninterrupted final-cadence passes in the
   actual room with fixed scale and shoe line;
8. enter every clip through its verified full Animator path;
9. hide the entire experimental deformation stack during every clip and show
   exactly one calibrated full-body frame;
10. capture a fresh eight-phase right-profile Walk strip and ten-clip sheet,
   report all four visible limb-region differences, facial differences and the
   weakest adjacent-frame difference, and keep all technical failures visible;
11. restore Patch 3.5, exit Play Mode and open the fresh read-only review.

Expected final count:

```text
EditMode: 4 passed; PlayMode: 4 passed.
```

No Dashboard, Test Runner, Play button or review-window click is required.
Watch the automatically focused Game view first: that uninterrupted pass is
the real timing preview. After Unity returns to Edit Mode, inspect the frozen
evidence window for scale, ground alignment, identity, silhouette and pose
quality. The first strip must show a right-facing alternating step moving
left-to-right. The `5 × 2` sheet must show one clean complete body in all ten
cells, with no second body, scale pop, floating shoes, cropped upgrade pose,
old limb pieces, vacuum stretching or detached face.

## Do not do yet

- Do not approve `Patch4ArtReadiness.asset`.
- Do not force `productionArtApproved = true`.
- Do not activate Patch 4 in runtime.
- Do not merge `patch-4.0` into `main`.
- Do not modify protected menu/audio/settings files.

## Next automatic V30 run

Run only:

```bat
git pull origin patch-4.0
```

Leave Unity open. Do not click Dashboard, Test Runner, Play or any review
button. The V30 token makes `Patch4AutoContinuation` rebuild the generated
controller and prefab, claim exclusive Test Runner PlayMode ownership, run
safety plus `4/4 + 4/4`, and then start the separate locked room review. The
review must prove `SetWalkSpeed -> Base Layer.FatMan_Walk_InRoom` through real
Animator updates before capturing any frame. After that review passes, Unity
will automatically enter one more normal gameplay session and leave Play Mode
running.
The first live pass must visibly show the event-owned sequence: idle breathing,
weight shift, blink, look, both taps, a right-facing travelling walk, turn,
sit/lean and upgrade. The frozen report must contain
`gameplayActionRoutingPassed: true`, compatible Walk travel, and no upgrade
scale-expansion failure. In the final Game view, use the normal dumbbell and
upgrade controls and watch the existing room routine. Patch 4 must remain
locked throughout.

## Work after the V30 interactive gameplay preview

- Observe the actual visible footprint while the five existing routine signals
  are reversibly projected into the V29 central corridor; keep activation
  locked.
- Reject any route or pose that overlaps the sofa, dumbbell rack, laundry
  basket or room edge. The technical review's artificial runway is not the
  normal-game corridor.
- If both directions remain clear, retain the small central standing zone and
  standing-action emphasis instead of restoring the unsafe sofa/window/mirror
  destinations for Patch 4.
- Confirm that taps and purchases interrupt/return to the correct real routine
  action and that the live sequence does not hold its final frame or flash
  through Idle after a one-shot.
- Reject any identity drift, foot slide, duplicate underlay, inconsistent scale
  or non-alternating step before extending the approach.
- Reject any clip that reveals the hidden legacy mesh, facial drift, weak
  expression, doubled silhouette or a front-facing sideways Walk.
- Do not resume tuning broad code-defined weight zones as if they were
  production art; V23's complete frames are the current review surface.
- Approve readiness only after technical and human visual review.
