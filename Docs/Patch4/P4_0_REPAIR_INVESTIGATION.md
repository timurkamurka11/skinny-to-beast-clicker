# Patch 4.0 Repair Investigation

## Defect contract

Observed in the supplied Unity screenshots:

- `GameplayVisualStageController` logs `Character stage 4 was selected but did not produce a visible rig. The next Sync will retry it.` once per refresh cycle.
- The locked animation-room review rejects the required clips for collapsed or over-expanded silhouette retention.
- Walk poses show a detached/floating actor, overextended legs, and travel into the right-side furniture area.

Expected:

- one structurally valid selected legacy rig remains active while one Patch 4 renderer owns the visible pixels;
- one owner writes each animated transform channel;
- the gameplay character root is the sole room-space travel owner;
- all required Animator states preserve a bounded, grounded silhouette and return cleanly to Idle.

## Baseline

- Unity: `6000.3.19f1`
- Scene: runtime-created `LivingGameplayScene` from `Assets/Scenes/SampleScene.unity`
- Commit: `dd8f878a2f2d0415a68df5980ee40be1b903773c`
- Static guard: passes before repair
- Runtime access in this workspace: Unity Editor executable and Unity MCP provider are unavailable; supplied screenshots and the committed Unity code/tests are the available runtime evidence.

## Hypothesis log

| Rank | Hypothesis | Evidence | Predicted observation | Discriminating check | Status |
|---:|---|---|---|---|---|
| 1 | Walk leg transforms have two simultaneous owners. | `BuildWalk` and `Patch4WalkV18Finalizer` author all six leg rotations; `Patch4V21HybridRigInstaller` also installed `Patch4V21FootPlantController`, whose `LateUpdate` overwrote those same rotations. Existing tests required both paths. | Removing the procedural leg writer while retaining the authored eight-phase clip removes per-frame pose competition and the exit snap. | Assert the generated prefab has no procedural leg writer while the real Walk clip retains all six articulated curves; sample the clip in PlayMode. | confirmed and repaired; static/EditMode regression added, Unity execution pending |
| 2 | Locked review moves the Patch 4 child instead of the authoritative gameplay character root. | The installer parents `FatMan_Patch4_Instance` below the legacy character root. Production `CharacterRoutineController` moves the legacy `RectTransform`, but review wrote `rigController.transform.localPosition` using a mostly vertical `(0.30, 0.954)` route. | Patch 4 detaches from the logical actor/shadow/depth scale and appears to float or enter furniture, matching the screenshots. | Verify review travel changes the legacy root and never the Patch 4 child; assert restoration of position and scale. | confirmed and repaired; root, shallow route, depth scale and exact restore are guarded |
| 3 | Core torso motion also has competing owners. | Animation clips key `SpineUpper`; `Patch4SecondaryMotionController` wrote a configured `SpineUpper` channel later each frame. | Core rotation depends on execution order and can inflate silhouette variance across otherwise subtle clips. | Check generated secondary channels against Animator-owned transform bindings, then remove only overlapping channels. | confirmed and repaired; generated-channel/clip overlap regression added |
| 4 | The Stage 4 retry was caused by preview deactivating the legacy `VisualRoot`; V37 fixed hierarchy activation but the real `Sync` path still retried any readiness failure. | `GameplayVisualStageController` reset `currentCharacterArt` to `-1` and reapplied on every refresh; target-application failure also cleared any would-be latch. | A durable selected stage plus one failure latch removes repeated retries and can finalize passively when Animator readiness returns. | Exercise all art stages, disable/re-enable Animator, then force target-application failure and call public `Sync` five times per episode. | confirmed and repaired; PlayMode integration regression added |
| 5 | Silhouette failure is a capture/ownership problem rather than extreme authored scale. | Finalizers remove whole/core scale curves; reported expansion affects nearly every clip, which is inconsistent with the small per-clip authored amplitudes. Prior V36 rendered two character generations. | Once renderer and transform ownership are exclusive, neutral-retention failures fall without lowering thresholds. | Fresh actual-room review after H1-H4/H6; compare one-body count and neutral bounds. | code paths repaired; fresh Unity review pending |
| 6 | The hybrid mesh binds against an arbitrary prior gameplay pose. | Patch 4 remains hidden while its Animator follows legacy signals. `PrepareLockedReview` activated the visual without first resetting the Animator; `Patch4V21HybridPuppetController` captures its bind the first frame it becomes visible. | If Walk/Tap/Sit was active, every later clip deforms from the wrong baseline, explaining broad silhouette failures despite restrained curves. | Pause signal owners, reset and verify Idle at time zero while hidden, activate visual, explicitly recapture hybrid bindings in that order. | confirmed by lifecycle tracing and repaired; ordering regression added, fresh Unity review pending |

## Confirmed execution-path findings

- Body stages `>= 4` resolve to art index `3`; `GameplayVisualStageController` owns selection of the persistent `CharacterRig2D`.
- `Patch4RuntimeInstaller` creates exactly one named Patch 4 child per legacy rig and binds the legacy `VisualRoot` as rollback.
- Patch 4 uses `Animator.applyRootMotion = false`; room translation is scripted.
- V23 full-frame sheets are disabled reference/QA data; the visible character is the generated continuous uGUI rig.
- The automatic pipeline generates `.anim`, controller, prefab and layer assets locally, so builder/finalizer code is the source of truth.

## Validation record

- `python3 Assets/GameWorkPatch4/CI/validate_patch4.py`: passes after each red/green repair; now detects transform-writer overlap, unbounded Stage 4 retries, wrong travel ownership/route and missing neutral rebind.
- `git diff --check`: passes after the repairs.
- Unity compilation, EditMode, PlayMode, Console and actual-room visual review: not runnable in the current workspace; must not be reported as passed until executed in Unity.
