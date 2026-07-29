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
- The approved `1024 × 1536` neutral master and ten deterministic masks restore
  locally.
- The draft baker produces all 40 canonical layers.
- Hidden joint scaffolds and the synthetic shadow satisfy the pixel/joint
  validator.
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
- `83eabc1` — automatic EditMode and PlayMode test runner.

## Real Unity test result

The user ran the automatic test continuation successfully.

Confirmed Console result:

```text
Patch 4 automated verification PASSED.
EditMode: 4 passed; PlayMode: 3 passed.
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

## Current GitHub implementation awaiting local verification

The next integration is implemented entirely inside `Assets/GameWorkPatch4/`:

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

## Exact next action

After the integration commit is present on `patch-4.0`, run only:

```bat
git pull origin patch-4.0
```

Keep Unity open and wait. `Patch4AutoContinuation` will automatically:

1. rebuild the resource-loadable locked prefab;
2. run pixel, rig, compilation and Editor smoke validation;
3. run all EditMode tests;
4. enter Play Mode and run all PlayMode tests;
5. exit Play Mode and open Console.

Expected final count:

```text
EditMode: 4 passed; PlayMode: 4 passed.
```

No Dashboard or Test Runner click is required.

## Do not do yet

- Do not approve `Patch4ArtReadiness.asset`.
- Do not force `productionArtApproved = true`.
- Do not activate Patch 4 in runtime.
- Do not merge `patch-4.0` into `main`.
- Do not modify protected menu/audio/settings files.

## Work after the 4/4 runtime-installation test

- Add and validate a Canvas-compatible presentation for the painted Patch 4
  layers inside `LivingGameplayScene`.
- Keep that presentation hidden behind the readiness gate.
- Complete/review hidden joint artwork and final facial poses.
- Complete Sprite Skin weight painting.
- Review all ten animations in the actual room.
- Approve readiness only after technical and human visual review.
