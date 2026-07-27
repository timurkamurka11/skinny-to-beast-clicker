# GameWork Patch 4.0 — One-click Windows verification

## Purpose

The repository root contains:

- `RUN_PATCH4_VERIFY.bat`
- `RUN_PATCH4_VERIFY.ps1`

The launcher automates the maximum work possible without remotely controlling the developer's computer.

## User action

1. Close Unity Editor.
2. Double-click `RUN_PATCH4_VERIFY.bat`.
3. Wait for the reports folder to open.

The user does not need to type Git, Unity command-line or Test Runner commands.

## Automated operations

The launcher:

1. requires a clean working tree;
2. fetches and switches to `patch-4.0`;
3. pulls the latest remote commits using fast-forward only;
4. verifies project version `6000.3.19f1`;
5. locates the matching Unity Editor installation;
6. downloads the approved Adobe master, rigging reference and valid masks;
7. verifies the master SHA-256;
8. opens Unity in batch mode;
9. compiles scripts and imports assets;
10. bakes the draft layer pack;
11. runs draft pixel and joint validation;
12. rebuilds the locked Animator and character prefab;
13. runs protected-path and Editor smoke validation;
14. runs Patch 4 EditMode tests;
15. runs Patch 4 PlayMode tests;
16. copies JSON and XML reports;
17. creates a ZIP archive;
18. opens the report folder in Windows Explorer.

## Reports

Reports are written under:

`Patch4VerificationResults/<timestamp>/`

The directory is ignored by Git.

Typical files:

- `SUMMARY.txt`
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

- The script never approves `Patch4ArtReadiness.asset`.
- The script never enables Patch 4 automatically.
- Patch 3.5 remains the visible rollback character.
- A master SHA mismatch stops the run.
- A dirty working tree stops the run instead of overwriting local work.
- Running Unity Editor instances must be closed before execution.
- Protected menu, video, music and settings files are not changed.

## Important limitation

`git push` cannot launch Unity on a local Windows computer. The launcher reduces the local requirement to one double-click, but the actual Unity executable and license must exist on that computer.

Final hidden-joint painting, face-pose painting, visual animation review and readiness approval remain human-review operations.
