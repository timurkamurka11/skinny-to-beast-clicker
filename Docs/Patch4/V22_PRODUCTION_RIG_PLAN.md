# Patch 4 V22 — Production Character Plan

Last updated: 2026-08-08

Status: **locked candidate work; not approved for production activation**

## Why V22 exists

The v17 room review proved that the current visible actor is not a production
2D rig. `Patch4CanvasSkinDeformer` bends one flattened master with procedural
screen zones. The result alternates between rubber/vacuum stretching and
paper-cut joints, regardless of how the curves are tuned.

The repository metadata already states the relevant contract:

- the master is `runtimeReady: false`;
- a flattened runtime sprite is forbidden;
- hidden joint artwork needs at least 24 real painted overlap pixels;
- human art and motion review are mandatory.

V22 therefore separates complete painted motion from experimental mesh motion.
It does not reinterpret another technical `PASS` as art approval.

## V22-A — complete-frame Walk candidate

Current repository asset:

`Assets/GameWorkPatch4/Art/Character/FatMan/V22Candidates/FatMan_WalkCycle_V22.png`

Contract:

- eight RGBA frames in four columns and two rows;
- one complete body per frame;
- common centre, ground line and scale;
- actual contact/down/passing/up leg poses and opposing arm swing;
- opposing contact poses exceed `0.140` alpha-silhouette difference in each
  arm and leg region, and all adjacent pairs exceed `0.075` so repeated poses
  cannot pass;
- no detached face, duplicate underlay, sliced limbs or deforming rectangle;
- runtime and room review select frames from the existing
  `FatMan_Walk_InRoom` Animator state;
- gameplay traversal remains owned by the existing room routine; the separate
  root travel exists only in the silent locked review;
- the old Canvas body is hidden as one whole group while a Walk frame is shown;
- Patch 4 remains disabled and Patch 3.5 remains the rollback character.

This asset is a review candidate. It may be replaced after human review without
changing the master or unlocking readiness.

## V22-B — production layered source

A real skeletal implementation requires a layered PSB/PSD (or equivalent
lossless source) with painted hidden continuation, not masks cut from the
flattened master. Minimum deliverable:

- torso and pelvis with shoulder/hip sockets painted behind limbs;
- upper/lower arms and hands with at least 24 px overlap at shoulder, elbow and
  wrist in source resolution;
- thighs, shins and feet with at least 24 px overlap at hip, knee and ankle;
- head/neck overlap plus separate eyelids, open mouth and smile shapes with no
  rectangular skin backing;
- stable front and three-quarter references that preserve the approved identity,
  clothing and proportions;
- no layer may rely on transparent holes in another layer to look complete.

Do not synthesize missing underpaint in C# and do not mark an automatically
inpainted joint as approved production art.

## V22-C — native rig architecture

After the layered source passes art review:

1. Import through Unity 2D PSD Importer.
2. Use native `SpriteRenderer` + `SpriteSkin` with an authored mesh and weights.
3. Add 2D IK limb solvers for hands/feet and explicit pole targets for knees and
   elbows.
4. Render the isolated native actor through a dedicated camera to a
   `RenderTexture`, then present that texture in the existing room Canvas. This
   keeps the menu and gameplay-room UI architecture unchanged.
5. Use Sprite Library/Resolver swaps for eyelids, mouth and hand poses.
6. Keep Walk foot contacts event-driven. Root travel must match stride length,
   stance feet must remain planted in world space and swing feet must clear the
   ground.
7. Use dedicated complete frames or authored pose swaps for Turn and
   UpgradeReact if the single front mesh cannot produce a clean silhouette.

The old `Patch4CanvasSkinDeformer` remains review-only until this replacement
exists; it must not become the approved production renderer.

## Mandatory validation order

1. **Neutral bind invariance** — enabling the renderer at neutral may not change
   width, height, area, pivot or face placement beyond one pixel at source
   scale.
2. **Joint smoke** — rotate one joint at a time and reject gaps, double edges,
   shirt wedges or foreign-pixel exposure.
3. **Walk physics** — require eight ordered phases, alternating hand/foot
   landmarks, monotonic room travel, planted-foot stability and swing-foot
   clearance.
4. **Technical room review** — all ten verified Animator states, retained
   silhouettes, independent facial motion and zero relevant Console errors.
5. **Human review** — judge identity, smoothness, weight, foot slide, seams and
   whether each action reads correctly at gameplay scale.
6. **Activation** — only after the exact source hash and all art/motion evidence
   are manually approved.

For complete-frame motion, validation follows the visible alpha silhouette,
not the hidden experimental bones. Native SpriteSkin motion must later restore
joint-space landmark, contact and displacement gates once the real layered
source exists.

## Automation and Unity MCP

`Patch4AutoContinuation` may rebuild candidates, run tests and open read-only
evidence. It may never approve readiness.

CoplayDev MCP is useful when its tools are exposed to the active assistant
session: it can read the live Console, enter Play Mode and inspect the actual
scene without asking the user to click. If it is unavailable, the repository
automation remains the fallback and the user only pulls the branch.

## Protected scope

V22 must not change `MainMenuLoop.mp4`, menu assets or logic, music, ambient
audio, mixers, settings UI or settings persistence.
