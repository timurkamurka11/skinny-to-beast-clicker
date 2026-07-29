# GameWork Patch 4.0 — P4.0-C Painted Layer Production

## Purpose

P4.0-C converts the approved neutral front master into an actual layered character suitable for the new Patch 4 skeleton.

The automated pipeline is intentionally conservative. It restores the exact
repository source, splits visible pixels into a complete contract-shaped draft
pack, measures coverage and identifies missing overlap. It cannot invent final
hidden anatomy or approve production art.

## Sources

### Approved neutral master

- Canvas: `1024 × 1536`
- Format: RGBA PNG with real transparency
- Repository path:
  `Assets/GameWorkPatch4/Art/Character/FatMan/FatMan_NeutralFront_Master.png`
- Approved SHA-256: `7b151f1ded93f3852bc8a7218ab26f94298b7f822094304bbcea9c076cad72a3`
- Photoshop/Firefly quality-pass reference:
  `https://photoshop-api.adobe.io/v2/short-url/urn:aaid:ps:US:72e5364f-ba61-4f62-96f5-51c0d8ac09bf`

The former compact source was only `96 × 144` and was enlarged in Unity. It
has been removed. The current workflow uses the exact committed 1024 source and
has no Adobe/network dependency.

### Adobe rigging reference

- URL: `https://photoshop-api.adobe.io/v2/short-url/urn:aaid:ps:US:5b427aac-252e-45c2-9a79-272568e505b8`
- Purpose: visual guidance for independent facial and clothing elements
- Status: reference only, not an exact replacement for the approved master

### Adobe mask manifest

`Assets/GameWorkPatch4/Art/Character/FatMan/Masks/adobe-mask-manifest.json`

Valid Adobe masks:

- hair
- face base
- eyebrows
- nose
- ears
- neck
- upper clothes
- lower clothes
- hands
- shoes

Rejected masks:

- pupils
- mouth
- arms
- legs

The rejected requests returned the entire subject or an implausibly tiny region. They are retained in the manifest for traceability but are never downloaded as valid production masks.

## Unity production dashboard

Open:

`Tools → GameWork → Patch 4.0 → Open Production Dashboard`

The dashboard reports:

- Adobe manifest availability
- master download state
- number of valid downloaded masks
- number of canonical layer PNGs
- pixel/joint report state
- signed art-readiness state
- Animator Controller state
- generated prefab state

## Ordered Unity pipeline

### Step 1 — Restore repository sources

Menu:

`Tools → GameWork → Patch 4.0 → Pipeline → 1. Download Adobe Sources`

The legacy menu name is retained for compatibility, but the command performs no
download. It:

- verifies the committed master SHA-256, size and RGBA format;
- copies the exact bytes to `Source/` and `References/`;
- regenerates ten deterministic masks in `Masks/Downloaded/`.

### Step 2 — Bake draft layers

Menu:

`Tools → GameWork → Patch 4.0 → Pipeline → 2. Bake Draft Layers`

The baker creates every path from `Patch4RigContract.RequiredLayerPaths` as a full-canvas `1024 × 1536` PNG.

File naming replaces `/` with `_`:

- `Body/TorsoBase` → `Body_TorsoBase.png`
- `ArmL/Upper` → `ArmL_Upper.png`
- `Face/MouthSmile` → `Face_MouthSmile.png`

The draft baker:

- intersects source alpha with the locally generated masks when available;
- splits bilateral masks at the master center line;
- applies controlled normalized regions when a dedicated mask is unavailable;
- never trims the canvas;
- records missing masks and manual redraw reasons;
- writes `layer-draft-status.json` with `activationAllowed: false`.

### Step 3 — Manual hidden-art reconstruction

Every moving connection needs at least 24 px of correctly painted hidden continuation at the target 1024 × 1536 resolution.

Required joint work:

1. Neck under head and shirt.
2. Both shoulder sockets under torso and upper arms.
3. Both elbows under upper arms and forearms.
4. Both wrists under forearms and hands.
5. Both hips under torso/pants and thighs.
6. Both knees under thighs and shins.
7. Both ankles under shins and feet.
8. Belly beneath the shirt belly overlay.
9. Shirt hem beneath neighboring torso and pants layers.

Do not create visible circles, capsules or rectangular body pieces. Hidden overlap art must continue the original painted contour, shading, fabric folds and skin rendering.

## Face production requirements

The following cannot remain geometric crops:

- `Face/EyeWhiteL`
- `Face/EyeWhiteR`
- `Face/IrisL`
- `Face/IrisR`
- `Face/LidL`
- `Face/LidR`
- `Face/MouthOpen`
- `Face/MouthSmile`
- `Face/CheekL`
- `Face/CheekR`

Requirements:

- Eyes preserve the original tired expression.
- Eyelids close without scaling the whole head.
- Irises stay inside the eye whites during look-around motion.
- Closed, open and smiling mouth layers share the same anchor.
- Cheek overlays contain no duplicated nose, mouth or jaw lines.
- All facial layers remain parented beneath `Head`.

## Pixel and joint validation

Menu:

`Tools → GameWork → Patch 4.0 → Pipeline → 3. Validate Draft Layers`

Report:

`Assets/GameWorkPatch4/Art/Character/FatMan/layer-bake-report.json`

Checks:

- every canonical PNG exists;
- every PNG is exactly `1024 × 1536`;
- each layer has meaningful alpha content;
- union coverage is at least 96.5% of master alpha;
- leakage outside master alpha is no more than 0.25%;
- local overlap exists at neck, shoulders, elbows, wrists, hips, knees, ankles and belly/shirt hem;
- draft metadata explicitly keeps activation disabled.

Technical validation does not equal human art approval.

## Production-art readiness gate

Asset:

`Assets/GameWorkPatch4/Art/Patch4ArtReadiness.asset`

`Patch4CharacterRigController` now requires all three conditions:

1. `patch4Enabled == true`
2. complete valid skeleton
3. readiness asset approved for the exact master SHA-256

If any condition fails:

- Patch 4 visual root is disabled;
- Patch 3.5 rollback remains visible;
- a warning is logged;
- draft art cannot become visible accidentally.

Automated tools never set `productionArtApproved`.

## Runtime rebuild

After manual art corrections:

`Tools → GameWork → Patch 4.0 → Pipeline → 4. Rebuild Runtime Assets`

This rebuilds:

- layer catalog
- ten animation clips
- Animator Controller
- Patch 4 prefab
- readiness binding

The prefab remains locked.

## Final safety validation

Run:

`Tools → GameWork → Patch 4.0 → Pipeline → 5. Run Safety Validation`

The command checks:

- pixel and joint report
- protected Git paths
- skeleton contract
- animation clip contract
- readiness lock state

Then test in Play Mode:

1. Idle breathing for at least 30 seconds.
2. Random blink without face detachment.
3. Look-around with irises contained.
4. At least 100 rapid taps with both reactions.
5. Walk across every room path.
6. Turn left/right and front/back presentation.
7. Sit/lean loop and recovery.
8. Upgrade reaction and stage change.
9. No double body visibility.
10. No sprite gaps at maximum deformation.
11. No menu, video, music or settings regressions.

## Canva production-status report

Editable design:

`https://www.canva.com/d/Zwy2RkpL4DJRJYs`

View-only design:

`https://www.canva.com/d/VESyL19jdqnHkif`

Canva is a visual status copy. GitHub `CHECKPOINT.md` remains the canonical continuation source.

## Protected scope

Never modify during Patch 4 character work:

- `MainMenuLoop.mp4`
- main-menu scenes, prefabs, transitions or button logic
- music, ambient audio or audio mixers
- settings UI, persistence, language, vibration or notifications
