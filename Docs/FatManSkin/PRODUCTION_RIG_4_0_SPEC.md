# Production Fat Man Rig 4.0

## Why this patch exists

The current flat turnaround PNG cannot become a Lamar-quality character by
runtime cropping or by stretching it over the old procedural mannequin. Strong
motion tears the image; weak motion looks like a static sticker. Patch 4.0 ends
that approach.

The final character must be an authored Unity 2D Animation asset with its own
bones, meshes, weights, pivots and animation clips. The old red/procedural rig
may remain hidden only as temporary gameplay logic until all gameplay signals
are moved to the production Animator. It must never render the character.

## Required source asset

Preferred input: one layered Photoshop **PSB** file for every body stage, or one
master PSB with compatible Sprite Libraries.

Minimum delivery:

- `FatMan_Stage01.psb`
- `FatMan_Stage02.psb`
- `FatMan_Stage03.psb`
- `FatMan_Stage04.psb`

Each PSB must contain three authored view groups:

- `FrontView`
- `SideView`
- `BackView`

The side view may be mirrored only when clothing, hair and lighting are truly
symmetrical.

## Required art layers

Every view must contain clean transparent layers with painted overlap under all
joints. Do not cut exactly on the elbow, knee, shoulder, wrist, ankle or neck.

### Body

- Pelvis / shorts
- Belly
- ShirtHem
- Chest / shirt
- Neck
- Head
- Hair
- ChinSoft

### Arms

- UpperArm_L
- Forearm_L
- Hand_L
- UpperArm_R
- Forearm_R
- Hand_R

### Legs

- Thigh_L
- Shin_L
- Foot_L
- Thigh_R
- Shin_R
- Foot_R

### Face

- Brow_L
- Brow_R
- Eye_L_Open
- Eye_L_Closed
- Eye_R_Open
- Eye_R_Closed
- Mouth_Neutral
- Mouth_Open
- Mouth_Strain
- Mouth_Yawn
- optional Mouth_Smile

The Back view does not need face layers.

## Mandatory skeleton

The authored skeleton must be built for this fat character, not copied from the
old mannequin.

- Root
- Pelvis
- Spine
- Chest
- Belly
- ShirtHem
- Neck
- Head
- ChinSoft
- UpperArm_L
- Forearm_L
- Hand_L
- UpperArm_R
- Forearm_R
- Hand_R
- Thigh_L
- Shin_L
- Foot_L
- Thigh_R
- Shin_R
- Foot_R

Recommended additional soft bones:

- ChestSoft_L / ChestSoft_R
- BellySoft_L / BellySoft_R
- ShirtSoft_L / ShirtSoft_R
- Cheek_L / Cheek_R
- HairFront / HairBack

## Weight-paint rules

- Head vertices must never be influenced by leg or pelvis bones.
- Foot vertices must not receive torso weights.
- Every limb joint needs a two-bone blend and enough painted overlap.
- Belly vertices should primarily follow Belly and Pelvis, with a smaller Chest
  influence near the top.
- ShirtHem follows Belly and Pelvis and may use its own soft bones.
- ChinSoft follows Head and Neck with limited secondary motion.
- Use a local mesh around every deforming part. Never deform one full-body
  rectangular texture.

## Animator contract

The prefab Animator must expose these parameters:

### Integers

- `Facing`: 0 Front, 1 Side, 2 Back
- `Stage`: 0..3

### Float

- `Speed`

### Bool

- `Walking`

### Triggers

- `Tap`
- `Yawn`
- `Scratch`
- `Stretch`
- `Flex`
- `Sit`
- `Stand`
- `Upgrade`

## Required animation clips

- Idle breathing
- Idle weight shift
- Random blink
- Look left / right
- Scratch
- Yawn
- Stretch
- Flex
- Tap reaction A
- Tap reaction B
- Walk loop
- Turn Front to Side
- Turn Side to Back
- Turn Back to Side
- Sit
- Stand
- Upgrade reaction

The stomach, chest, shirt hem and chin must have controlled secondary motion.
No clip may separate a body part, invert a mesh or reveal an unpainted gap.

## Unity import path

The project already uses Unity 2D Animation and 2D PSD Importer. Import the PSB
with character rigging enabled, preserve layer hierarchy, generate SpriteSkin
components and weight-paint the meshes in the Sprite Editor.

Create the final prefab at:

`Assets/Resources/Characters/FatManProduction/FatManRig.prefab`

The prefab root must contain:

- `ProductionFatManRigContract`
- an `Animator`
- a `Skeleton` child
- a `Visual` child
- `FrontView`, `SideView`, `BackView`
- SpriteRenderer and SpriteSkin components

Do not add:

- CharacterMeshGraphic
- uGUI Image body layers
- runtime-cropped turnaround pieces
- the old CharacterRigController

## Rendering inside the current game

The game room is uGUI/ScreenSpaceOverlay, while SpriteSkin uses SpriteRenderer.
`ProductionFatManRenderHost` renders the authored character with a transparent
orthographic camera into a RenderTexture shown by a RawImage. This avoids
forcing SpriteSkin into uGUI and keeps the new skeleton independent from the
old robot.

When the production prefab is valid, `ProductionFatManRigAutoInstaller` attaches
that host to both the entry character and the gameplay character and hides the
legacy render surfaces.

## Verification

Run:

`Tools > Skinny to Beast > Production Rig 4.0 > Validate Fat Man Rig`

The patch is accepted only when:

- the validator passes;
- no CharacterMeshGraphic exists in the production prefab;
- all SpriteSkin objects have bound bones;
- Front, Side and Back work;
- Stage 1..4 work;
- blink, mouth and brows work;
- arms, legs, torso and belly visibly animate;
- the entry walk has no white blocks or detached pieces;
- no old robot is visible;
- settings and audio remain unchanged;
- the room always opens even if character validation fails.

## What ChatGPT can and cannot do

ChatGPT can implement the Unity integration, Animator contract, validation,
RenderTexture host, gameplay state bridge and code fixes. It can also help
prepare or edit a layered source when the actual layers are supplied.

A single flattened PNG does not contain the hidden artwork, joint overlap,
separate meshes or correct weight data needed for commercial-quality skeletal
animation. A layered PSB/PSD or a ready Spine/Live2D/Unity rig must be supplied
or created by an artist before the final character can honestly be completed.
