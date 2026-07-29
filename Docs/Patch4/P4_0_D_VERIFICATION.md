# GameWork Patch 4.0 — P4.0-D Verification

## Purpose

P4.0-D adds repeatable checks around the Patch 4 art, rig, animator and rollback system. These checks do not approve production art and do not enable the new character.

Unity compilation and Play Mode results must still be produced by opening the project in Unity `6000.3.19f1`.

## GitHub static guard

Workflow:

`.github/workflows/patch4-static-guard.yml`

Validator:

`Assets/GameWorkPatch4/CI/validate_patch4.py`

The guard checks:

- 31 unique required bones;
- 40 unique required painted layers;
- 10 unique required animation clips;
- the approved source SHA-256;
- `runtimeReady: false` before manual approval;
- Adobe mask-manifest validity metadata;
- absence of automatic production-art approval;
- runtime installation remains locked to rollback mode;
- protected menu, video, music, mixer and settings paths.

This is a source-level safety check. It is not a substitute for Unity compilation.

## Compilation report

Class:

`Patch4CompilationMonitor`

Report:

`Library/GameWorkPatch4Reports/patch4-compilation-report.json`

The monitor records:

- all compiler errors and warnings;
- assembly path;
- source path, line and column;
- whether the message belongs to `GameWorkPatch4`;
- total and Patch 4-specific counts;
- whether the completed compilation succeeded.

Because the report is written under `Library`, generating it cannot trigger another Asset Database import.

## Editor prefab smoke report

Class:

`Patch4EditorSmokeValidator`

Report:

`Library/GameWorkPatch4Reports/patch4-editor-smoke-report.json`

It checks:

- generated prefab existence;
- `Patch4CharacterRigController` existence;
- complete named skeleton;
- expected source SHA;
- bound readiness asset;
- Animator Controller existence;
- all ten required clips;
- layer catalog existence;
- all required sprites assigned;
- generated prefab loadable through the isolated Patch 4 Resources path;
- Patch 4 initially disabled and hidden.

During P4.0-C the smoke report is expected to fail on painted layers until the manual art pack is complete.

## EditMode tests

Assembly:

`SkinnyToBeast.GameWorkPatch4.EditModeTests`

Tests verify:

- contract counts;
- no duplicate contract names;
- critical face, rig and animation entries;
- default readiness rejection;
- exact case-insensitive SHA matching;
- rejection of a different SHA.

## PlayMode tests

Assembly:

`SkinnyToBeast.GameWorkPatch4.PlayModeTests`

Tests verify:

1. A complete skeleton without art approval cannot activate Patch 4.
2. Approved art with a complete skeleton can activate Patch 4 and hide rollback.
3. Disabling Patch 4 restores Patch 3.5 rollback visibility.
4. Approved art cannot bypass an incomplete skeleton.
5. The dynamically-created `LivingGameplayScene` character receives one
   resource-loaded Patch 4 instance with Patch 4 hidden and Patch 3.5 visible.

The tests use reflection so they remain isolated from the project's predefined `Assembly-CSharp` assembly.

## Unity execution order

After pulling a continuation commit, `Patch4AutoContinuation` performs the
current required sequence:

1. Rebuild the locked runtime Resources prefab.
2. Rebuild the 40-layer Canvas presentation.
3. Assemble the neutral pose and compare it with the approved master.
4. Run draft, rig, compilation and Editor smoke validation.
5. Run all EditMode tests.
6. Run all PlayMode tests.
7. Exit Play Mode and open the read-only neutral-pose review window.

Dashboard and Test Runner commands remain available for diagnostics, but the
user does not need to click them for the normal continuation workflow.

## Release blocking rules

Patch 4 remains blocked when any of the following is true:

- Unity has compiler errors;
- the static guard fails;
- the pixel/joint report fails;
- the Editor smoke report fails;
- an EditMode or PlayMode test fails;
- any required sprite is missing;
- hidden joint artwork is unfinished;
- face poses are fallback graphics;
- both Patch 3.5 and Patch 4 are visible;
- readiness is not signed for the exact approved SHA;
- a protected path changed.

## Current honest status

Unity `6000.3.19f1` has produced a real passing result for compilation, Editor
smoke, `4` EditMode tests and `4` PlayMode tests, including the Canvas-ready
locked installation inside the dynamically created `LivingGameplayScene`.
Neutral-pose QA now awaits the same automatic `4/4` local verification and
three-panel review. Until that result and the remaining visual-art review pass,
Patch 4 stays locked.
