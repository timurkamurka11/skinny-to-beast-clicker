# GameWork Patch 4.0 — Durable Checkpoint

Last updated: 2026-07-30
Branch: `patch-4.0`
Repository: `timurkamurka11/skinny-to-beast-clicker`

This file is the canonical continuation point for all future Patch 4 work.

## User goal

Replace the Patch 3.5 procedural/basic-shape character with an original hand-drawn overweight adult man, a completely new named skeleton, separated facial artwork, soft-body deformation and new animations.

The following must remain unchanged:

- `MainMenuLoop.mp4`
- main-menu scenes, prefabs, transitions and button logic
- music, ambient audio and audio mixers
- settings UI, persistence, language, vibration and notifications

## Approved visual source

- Neutral front master: transparent PNG, `1024 × 1536`, RGBA.
- Approved SHA-256: `5873cf6df0df2b5ebd4947b687693162d4b34899202326d1b1ae62df9f50587c`.
- Character: overweight adult man, heavy belly, thick arms and thighs, short dark hair, dirty gray sleeveless shirt, dark pants and gray shoes.
- Figma file: `tZSr9vinRs9EbZzgatxjda`.
- Concept board: node `4:3`.
- Rig blueprint board: node `6:3`.
- Adobe source currently used by P4.0-C: `urn:aaid:sc:AP:aa1abfc7-66c2-4260-a320-6781833d46cb`.
- Adobe source URL: `https://at.adobe.com/SGSnfFAvaBd9wjrT`.
- Earlier Creative Cloud copy: `urn:aaid:sc:AP:5086d367-0290-430e-b9a7-39e5392bdbde`.
- Adobe vector trace: `https://to.adobe.com/aN0OeN9oa589DR97`.
- Adobe rigging-parts reference: `https://photoshop-api.adobe.io/v2/short-url/urn:aaid:ps:US:5b427aac-252e-45c2-9a79-272568e505b8`.

The master is approved as the visual source, but it is not a final one-piece Unity sprite. The Firefly rigging sheet is reference-only and may not replace the exact approved master.

## Completed P4.0-A — art and rig foundation

- Five-view directional concept reference.
- Clean neutral front master on transparent background.
- Manual layer-cut and 24 px overlap guide.
- New skeleton blueprint and canonical bone names.
- Layer contract for body, head, face, arms, legs, clothes and FX.
- Adobe vector trace and local art-foundation package.

## Completed P4.0-B — runtime and editor foundation

### Runtime

- `Patch4RigContract`
- `Patch4CharacterRigController`
- `Patch4CharacterStateMachine`
- `Patch4FaceController`
- `Patch4SecondaryMotionController`
- `Patch4CharacterVisibilityGuard`
- `Patch4LayerCatalog`
- `Patch4LayerRenderer`
- `Patch4LegacySignalBridge`

### Editor automation

- `Patch4RigContractValidator`
- `Patch4LayerImportPostprocessor`
- `Patch4LayerPlacement`
- `Patch4LayerCatalogBuilder`
- `Patch4AnimationLibraryBuilder`
- `Patch4AnimatorControllerSanitizer`
- `Patch4PrefabBuilder`
- `Patch4SceneInstaller`

### Generated animation contract

1. `FatMan_Idle_Breathe`
2. `FatMan_Idle_ShiftWeight`
3. `FatMan_Blink_Random`
4. `FatMan_LookAround`
5. `FatMan_TapReact_01`
6. `FatMan_TapReact_02`
7. `FatMan_Walk_InRoom`
8. `FatMan_Turn`
9. `FatMan_SitOrLean`
10. `FatMan_UpgradeReact`

### Existing gameplay integration

Patch 4 does not edit the existing gameplay controller. `Patch4LegacySignalBridge` observes:

- accepted tap count from `CharacterRigController`;
- movement state and facing;
- idle/routine action state;
- current skin stage from `CharacterSkinController`.

It mirrors those signals into the new Patch 4 Animator while Patch 3.5 stays available as rollback.

## Completed P4.0-C automation — Adobe masks and layer production pipeline

### Adobe work completed

The transparent master was uploaded and visually inspected in Adobe.

Valid selection masks were produced for:

- hair;
- face base;
- eyebrows;
- nose;
- ears;
- neck;
- upper clothes;
- lower clothes;
- hands;
- shoes.

Adobe did not reliably detect stylized pupils, mouth, arms or legs. Those failed requests returned the full subject or an implausibly small selection. They are marked invalid in the manifest and are never treated as production masks.

Manifest:

`Assets/GameWorkPatch4/Art/Character/FatMan/Masks/adobe-mask-manifest.json`

### P4.0-C runtime safety

- `Patch4ArtReadinessAsset` is the explicit human-approval gate.
- `Patch4CharacterRigController` requires readiness approval for the exact master SHA-256.
- Setting `patch4Enabled = true` cannot bypass the gate.
- If art is not approved, Patch 4 stays hidden and Patch 3.5 remains visible.
- Automated tools never set `productionArtApproved`.

### P4.0-C editor tools

- `Patch4AdobeMaskDownloader`
- `Patch4MaskDrivenLayerBaker`
- `Patch4DraftLayerValidator`
- `Patch4ArtReadinessAssetBuilder`
- `Patch4PrefabReadinessBinder`
- `Patch4ProductionPipeline`
- `Patch4ProductionDashboard`

### Draft layer behavior

The baker creates the complete canonical full-canvas layer set in:

`Assets/GameWorkPatch4/Art/Character/FatMan/Layers/`

Every draft remains `1024 × 1536`. Filenames replace `/` with `_`:

- `Body_TorsoBase.png`
- `Face_MouthClosed.png`
- `ArmL_Upper.png`
- `LegR_Foot.png`
- `FX_Shadow.png`

The baker prefers valid Adobe masks and uses bounded geometric fallback regions only when Adobe detection failed. It writes `layer-draft-status.json` with `activationAllowed: false`.

### Pixel and joint QA

`Patch4DraftLayerValidator` creates:

`Assets/GameWorkPatch4/Art/Character/FatMan/layer-bake-report.json`

It checks:

- all canonical files exist;
- every canvas is exactly `1024 × 1536`;
- each layer contains meaningful alpha pixels;
- union coverage of the approved master;
- alpha leakage outside the approved master;
- local overlap at neck, shoulders, elbows, wrists, hips, knees, ankles and belly/shirt hem;
- draft metadata keeps activation disabled.

Technical passing does not equal human art approval.

## Completed P4.0-D automation — compile, CI and smoke verification

### GitHub static guard

Workflow:

`.github/workflows/patch4-static-guard.yml`

Validator:

`Assets/GameWorkPatch4/CI/validate_patch4.py`

It checks the 31-bone, 40-layer and 10-clip contracts, uniqueness, approved master SHA, JSON manifests, readiness lock and protected paths. It does not claim Unity compilation.

### Unity compilation report

`Patch4CompilationMonitor` writes:

`Library/GameWorkPatch4Reports/patch4-compilation-report.json`

The report records all compiler errors and warnings, assembly, source path, line, column and Patch 4-specific counts. It is deliberately outside `Assets` to prevent re-import loops.

### Editor prefab smoke report

`Patch4EditorSmokeValidator` writes:

`Library/GameWorkPatch4Reports/patch4-editor-smoke-report.json`

It checks prefab existence, complete skeleton, readiness binding, exact SHA, Animator Controller, all ten clips, the complete layer catalog and the initially hidden Patch 4 visual root.

### EditMode tests

Assembly:

`SkinnyToBeast.GameWorkPatch4.EditModeTests`

The tests verify contract counts, uniqueness, critical entries and exact readiness SHA behavior.

### PlayMode tests

Assembly:

`SkinnyToBeast.GameWorkPatch4.PlayModeTests`

The tests verify:

1. complete skeleton without art approval remains on Patch 3.5;
2. exact approved SHA plus complete skeleton can activate Patch 4;
3. disabling Patch 4 restores rollback visibility;
4. approval cannot bypass an incomplete skeleton.

The tests use reflection to stay isolated from the predefined `Assembly-CSharp` assembly.

Detailed instructions:

`Docs/Patch4/P4_0_D_VERIFICATION.md`

### Real Unity verification completed

Unity `6000.3.19f1` produced a passing automatic verification result:

- EditMode: `4 passed`;
- PlayMode: `4 passed`;
- compilation snapshot: passed;
- rig contract: passed;
- Editor prefab smoke: passed;
- readiness gate: correctly locked.

`Patch4AutomatedTestRunner` writes the combined JSON and two NUnit XML reports
under `Library/GameWorkPatch4Reports/`.

## P4.0-E runtime room integration

Repository inspection confirmed that the playable room is not an authored scene
containing `CharacterRigController`. `GameplayWindowController` builds
`LivingGameplayScene` dynamically, and `GameplayVisualStageController`
instantiates:

`Resources/UI/Gameplay/Living/CharacterRig2D.prefab`

The Patch 4 integration therefore stays isolated:

- `Patch4PrefabBuilder` generates the locked prefab under
  `Assets/GameWorkPatch4/Resources/`;
- `Patch4RuntimeInstaller` scans only below `LivingGameplayScene`;
- `GameEntryScreen` is excluded;
- the Patch 4 instance is parented beside the real legacy rig;
- rollback root and legacy gameplay signals are bound at runtime;
- `SetPatch4Enabled(false)` is applied explicitly;
- Patch 3.5 remains visible;
- production-art approval is never changed.

An additional PlayMode test verifies the real runtime-resource installation
contract. Unity `6000.3.19f1` confirmed:

- EditMode: `4 passed`;
- PlayMode: `4 passed`.

The user's Console also confirmed that Patch 4 installed only below
`LivingGameplayScene`, stayed in locked rollback mode and left Patch 3.5
visible, with zero warnings and zero errors.

## P4.0-F Canvas room presentation

The painted PNG layers now have an isolated Canvas-compatible presentation:

- `Patch4CanvasPresentation` builds 40 non-interactive `UI.Image` objects;
- one flat image hierarchy preserves canonical global layer order;
- `LateUpdate` mirrors each image pivot to its assigned Patch 4 bone;
- the `1024 × 1536` source is fitted to the legacy `720 × 1280` character
  root using the existing `0.74` presentation scale;
- the source pelvis is aligned to the legacy room origin;
- SpriteRenderer fallbacks are disabled;
- eyelid and mouth bindings are moved to the Canvas images;
- the runtime installer requires successful Canvas binding before accepting
  the hidden Patch 4 instance;
- the Editor smoke report validates all 40 images and disabled fallbacks;
- the runtime PlayMode integration test validates Canvas binding, image count,
  room scale, pelvis alignment and locked rollback state.

This implementation does not approve art, does not enable Patch 4 and does not
modify the legacy gameplay-room builder. It awaits the next automatic local
Unity verification.

## Production dashboard

Open in Unity:

`Tools → GameWork → Patch 4.0 → Open Production Dashboard`

Ordered commands:

1. Download Adobe sources.
2. Bake draft layers.
3. Validate draft layers.
4. Rebuild locked runtime assets.
5. Run safety validation.
6. Run compilation and Editor smoke reports.
7. Open Unity Test Runner and run EditMode plus PlayMode tests.

## Canva visual status

Editable design:

`https://www.canva.com/d/Zwy2RkpL4DJRJYs`

View-only design:

`https://www.canva.com/d/VESyL19jdqnHkif`

Canva is a visual status copy. This GitHub checkpoint remains the source of truth.

## Figma status

Figma concept and rig pages remain intact.

A new P4.0-C status panel could not be written because the Figma Starter MCP call limit was reached. The failed write was atomic and did not modify or damage the existing file.

## Current activation state

Patch 4 must remain **disabled**.

It may activate only when:

1. all required bones exist;
2. all canonical painted sprites exist in the layer catalog;
3. all ten animation clips exist;
4. the pixel/joint report passes;
5. the compilation report succeeds;
6. the Editor smoke report passes;
7. all EditMode tests pass;
8. all PlayMode tests pass;
9. all hidden joint artwork has been manually reconstructed;
10. independent face poses have been manually reviewed;
11. the exact master SHA is approved in `Patch4ArtReadiness.asset`;
12. only one character body is visible;
13. the ten animations pass review in the actual room;
14. protected paths remain unchanged.

Until every condition passes, Patch 3.5 remains visible.

## Immediate next work

1. Pull the Canvas-presentation commit into Unity `6000.3.19f1`.
2. Let `Patch4AutoContinuation` rebuild the Resources prefab, generate the 40
   Canvas images and run all validations without manual clicks.
3. Confirm `EditMode: 4 passed; PlayMode: 4 passed`.
4. Reassemble the neutral pose and compare it against the approved master
   while the readiness gate remains locked.
5. Refine hidden artwork beneath neck, shoulders, elbows, wrists, hips, knees,
   ankles, belly and shirt hem.
6. Replace geometric face fallbacks with final eye whites, irises, eyelids,
   cheeks, open mouth and smile.
7. Complete Sprite Skin weight painting.
8. Review all ten animations in the actual room.
9. Only after successful technical and human review, approve the readiness
    asset for the exact master SHA.

Detailed art instructions:

`Docs/Patch4/P4_0_C_LAYER_PRODUCTION.md`

Detailed verification instructions:

`Docs/Patch4/P4_0_D_VERIFICATION.md`

## Known limitations

- The Canvas presentation has not yet received its local Unity `4/4`
  confirmation.
- Generated PNG layers and generated runtime assets exist locally in Unity and
  are not committed as binary repository assets.
- Hidden joint artwork still requires manual painting.
- Final face poses still require manual painting.
- Sprite Skin weight painting has not yet been completed.
- The Canvas presentation remains hidden behind readiness and still requires a
  later controlled human visual review in `LivingGameplayScene`.
- Figma Starter MCP limit currently prevents additional write calls.
