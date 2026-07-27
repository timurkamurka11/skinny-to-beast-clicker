# GameWork Patch 4.0 — One-command Windows workflow

## Purpose

Patch 4 can be synchronized, generated, compiled and tested without manually switching branches, pulling the repository, opening the Production Dashboard or running the Unity Test Runner.

The project root contains:

- `RUN_PATCH4_VERIFY.bat`
- `RUN_PATCH4_VERIFY.ps1`

The batch launcher downloads the latest PowerShell launcher directly from `origin/patch-4.0` before every run.

## Permanent user action

After the latest launcher has been synchronized once, the user only needs to close Unity Editor and double-click:

`RUN_PATCH4_VERIFY.bat`

For a machine that still has an older launcher, one PowerShell command can fetch and execute the newest remote launcher without `git pull` or `git switch`.

## Automated operations

The launcher:

1. records the current Git working tree without rejecting unrelated Unity changes;
2. creates a backup copy of any modified tracked Patch 4 files;
3. fetches `origin/patch-4.0`;
4. extracts only Patch 4-managed files from a temporary Git archive;
5. copies those managed files into the project without switching branches, stashing, resetting or deleting unrelated files;
6. preserves untracked Unity `.meta` files and locally generated game content;
7. verifies Unity version `6000.3.19f1`;
8. locates the matching Unity Editor installation;
9. downloads the approved Adobe master, rigging reference and valid masks;
10. verifies the exact master SHA-256;
11. opens Unity in batch mode;
12. compiles scripts and imports assets;
13. bakes the complete draft layer pack;
14. runs pixel and joint validation;
15. rebuilds the locked Animator and Patch 4 prefab;
16. runs protected-path and Editor smoke validation;
17. runs Patch 4 EditMode tests;
18. runs Patch 4 PlayMode tests;
19. copies JSON, XML and Unity log reports;
20. creates a ZIP archive;
21. opens the report folder;
22. opens the Unity project normally after automation finishes.

## Local-file safety

The synchronizer manages only:

- `Assets/GameWorkPatch4/`
- `Docs/Patch4/`
- the Patch 4 launcher files;
- the Patch 4 static GitHub workflow.

It does not use:

- `git reset`;
- `git clean`;
- `git stash`;
- branch switching;
- whole-repository checkout.

It does not delete or overwrite unrelated local scenes, menus, audio, videos, settings, prefabs, scripts or generated Unity metadata.

All detected local changes are recorded in:

`Patch4VerificationResults/<timestamp>/LOCAL_CHANGES_PRESERVED.txt`

Modified tracked Patch 4 files are copied to:

`Patch4VerificationResults/<timestamp>/ManagedBackup/`

before the remote Patch 4 version is synchronized.

## Reports

Reports are written under:

`Patch4VerificationResults/<timestamp>/`

Typical files:

- `SUMMARY.txt`
- `LOCAL_CHANGES_PRESERVED.txt`
- `01-prepare-and-smoke.log`
- `02-editmode.log`
- `03-playmode.log`
- `editmode-results.xml`
- `playmode-results.xml`
- `patch4-compilation-report.json`
- `patch4-editor-smoke-report.json`
- `patch4-batch-summary.json`

A ZIP copy is created beside the timestamped directory.

## Safety behavior

- The launcher never approves `Patch4ArtReadiness.asset`.
- The launcher never enables unfinished Patch 4 art automatically.
- Patch 3.5 remains the visible rollback character.
- A master SHA mismatch stops the run.
- A currently open Unity Editor is not force-closed, preventing loss of unsaved work.
- Protected menu, video, music and settings files are not modified.

## Remaining human-only work

The launcher performs all deterministic Git, Adobe download, Unity generation and test operations. Final hidden-joint painting, face-pose painting, animation-quality review and readiness approval still require visual judgment and are intentionally not auto-approved.
