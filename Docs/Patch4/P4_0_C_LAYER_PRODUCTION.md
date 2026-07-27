# GameWork Patch 4.0 — P4.0-C Painted Layer Production

## Purpose

P4.0-C converts the approved neutral front master into an actual layered character suitable for the new Patch 4 skeleton.

The automated pipeline is intentionally conservative. It can download Adobe sources, split visible pixels into a complete contract-shaped draft pack, measure coverage and identify missing overlap. It cannot invent final hidden anatomy or approve production art.

## Sources

### Approved neutral master

- Canvas: `1024 × 1536`
- Format: RGBA PNG with real transparency
- Adobe asset: `urn:aaid:sc:AP:aa1abfc7-66c2-4260-a320-6781833d46cb`
- Adobe source URL: `https://at.adobe.com/SGSnfFAvaBd9wjrT`
- Approved SHA-256: `5873cf6df0df2b5ebd4947b687693162d4b34899202326d1b1ae62df9f50587c`

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

### Step 1 — Download Adobe sources

Menu:

`Tools → GameWork → Patch 4.0 → Pipeline → 1. Download Adobe Sources`

Downloads:

- approved neutral master to `Source/`
- valid Adobe masks to `Masks/Downloaded/`
- Firefly rigging reference to `References/`

Invalid whole-subject masks are skipped.

### Step 2 — Bake draft layers

Menu:

`Tools → GameWork → Patch 4.0 → Pipeline → 2. Bake Draft Layers`

The baker creates every path from `Patch4RigContract.RequiredLayerPaths` as a full-canvas `1024 × 1536` PNG.

File naming replaces `/` with `_`:

- `Body/TorsoBase` → `Body_TorsoBase.png`
- `ArmL/Upper` → `ArmL_Upper.png`
- `Face/MouthSmile` → `Face_MouthSmile.png`

The draft baker:

- intersects source alpha with Adobe masks when valid;
- splits bilateral masks at the master center line;
- applies controlled normalized regions when Adobe detection failed;
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
