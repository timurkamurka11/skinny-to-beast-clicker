# Living Gameplay Rig V2

This patch keeps the approved main menu, Settings popup, room, HUD, tap effects,
economy and upgrade flow. It replaces the old whole-PNG character animation with
one exclusive skin, a movable 2D transform rig, facial controls, directional walk
art and autonomous room routines.

## Entry flow

`START` now follows this order:

```text
Main menu
→ GameEntryScreen
→ saved body stage is resolved
→ character walks toward the door
→ room assets are preloaded
→ `GameEntry.unity` reaches 90% with scene activation blocked
→ black transition curtain closes
→ scene activation is allowed
→ gameplay is built and synchronized behind the curtain
→ entry screen fades out
→ room becomes visible
```

The entry screen is visible for more than 1.5 seconds and blocks repeated input.
It lives on a temporary `DontDestroyOnLoad` canvas while
`SceneManager.LoadSceneAsync()` loads `GameEntry.unity` with
`allowSceneActivation = false`. `GameplayWindowController.Show()` finishes its
build and first `Refresh()` before the entry overlay becomes transparent, so a
stale body stage cannot flash for one frame. Its back-facing walk frames use the
same center and foot calibration as the room, keeping the approaching character
aligned with its shadow and clear of the status panel.

## Runtime hierarchy

```text
GameplayWindow
├── LivingGameplayScene
│   ├── RoomStage01
│   ├── RoomStage02
│   ├── AmbientLayer
│   ├── UpgradeProps
│   ├── CharacterActors
│   │   ├── Anchor.Center
│   │   ├── Anchor.Training
│   │   ├── Anchor.Sofa
│   │   ├── Anchor.Window
│   │   ├── Anchor.Mirror
│   │   └── CharacterRoot
│   │       ├── CharacterShadow
│   │       ├── Skeleton.Root
│   │       │   ├── Bone.Root / Pelvis / Spine / Chest / Neck / Head
│   │       │   ├── Bone.Shoulder / UpperArm / Forearm / Hand (L/R)
│   │       │   └── Bone.Thigh / Shin / Foot (L/R)
│   │       └── DirectionalWalkRenderer
│   └── DumbbellRoot
├── TapEffects
└── SafeArea
```

## Exclusive skin switching

`CharacterSkinController` is the only class allowed to apply body art.

- There are no four enabled stage GameObjects.
- The controller owns the exclusive `Body`, `Head`, `HairBack`, `HairFront`,
  `Top`, `Bottom`, `Shoes`, `Face` and `Accessory` slots.
- Each slot is stored in a one-value dictionary; a duplicate selection aborts
  the swap before the actor is shown.
- The actor starts hidden and does not receive a temporary stage-1 skin while
  saved progress is loading.
- Every visible front-facing rig part receives the same single texture.
- Stage changes fade the current rig to zero, replace the texture, then fade the
  same rig back in.
- Interrupted transitions are stopped and normalized before another skin applies.
- `CharacterRigValidator` verifies all required bones, checks slot exclusivity
  and exposes `Run 50 Skin Swaps` as a play-mode stress test.

The seven progression stages still map to four art stages:

| Gameplay stage | Active art |
|---|---|
| `Skinny` | `character_stage_01.png` |
| `Beginner` | `character_stage_02.png` |
| `Fit`, `Athletic` | `character_stage_03.png` |
| `Big`, `Beast`, `Gym Legend` | `character_stage_04.png` |

## Skeletal rendering

`RigPartGraphic` renders polygonal UV regions of the current stage texture. Each
region is attached to its own pivot instead of displaying a second whole
character. The active rig prefab contains 20 named bones:

- root, pelvis, spine, chest, neck and head;
- left/right shoulders, upper arms, forearms and hands;
- left/right thighs, shins and feet.

The chest and abdomen are separate deforming regions. Breathing affects the
chest, abdomen and shoulders. Weight shifting affects the pelvis, chest and head
in opposite directions, while compensating foot rotation keeps the soles planted.
Walk cycles move opposite arms and legs and bend the knees. Three different tap
lifts, scratch, yawn, stretch, flex, clothes adjustment, shoulder warm-up and
three-phase sitting animate the relevant bones with eased recovery rather than
moving the full PNG as one rectangle.

The generated `LivingCharacter.controller` mirrors the procedural rig state with
four layers: `Base`, `UpperBody`, `Face` and `FullBodyAction`. It contains the
named idle, walk, sit and `TapLift_A/B/C` states and is rebuilt automatically in
the Editor when an older controller is detected.

## Directional movement

Each art stage has a generated four-frame directional sheet:

| Sheet row | Frames |
|---|---|
| top | left-facing side contact and passing poses |
| bottom | back-facing contact and passing poses |

Moving right mirrors the side animation. Moving toward the foreground uses the
front transform rig, so legs and arms remain independently animated. Scale and
shadow opacity change with room depth. Every generated directional frame has an
individual center, scale and foot-baseline calibration, preventing the character
from jumping sideways or changing height when the walk frame alternates.

`CharacterRoutineController` chooses among five authored anchors. After
approximately 10–25 seconds including the previous action, the character can:

- walk to the window and face away;
- sit at the sofa;
- flex at the mirror;
- stretch or yawn in the center;
- scratch, shift weight, adjust clothes, warm up shoulders or return to the
  dumbbell.

A tap awards gameplay progress immediately. The dumbbell reacts immediately. If
the character is elsewhere, the routine is interrupted and the character returns
quickly to the training anchor before playing the queued lift reaction. Rapid taps
are capped to a six-reaction visual queue and never create an unbounded coroutine
stack. After the tap burst, the actor walks back to the interrupted anchor and
resumes the interrupted walk or action. If sitting was interrupted, the sofa
sequence is entered again through `SitDown` rather than snapping directly into
the seated pose.
The sofa action eases into the seated pose, holds it, then eases back to standing
instead of briefly crouching and immediately snapping upright.

## Face rig

The face controller owns separate eye whites, pupils, eyelids, brows, cheeks,
sweat and seven procedural mouth configurations.

- blink interval: 2.5–6 seconds;
- optional double blink: 18% chance;
- eased pupil look targets;
- tired, relaxed, focused, happy, strain and yawn expressions;
- stage-specific default expression;
- downward focus and strain during a lift;
- a short smile after the lift completes.

Brows, mouth shapes, cheeks and sweat fade between expressions instead of
switching visibility on a single frame.

## Generated assets

- `game_entry_door.png`
- `Rig/walk_stage_01.png`
- `Rig/walk_stage_02.png`
- `Rig/walk_stage_03.png`
- `Rig/walk_stage_04.png`
- `CharacterRig2D.prefab`
- `GameEntry.unity`
- generated `Animations/LivingCharacter.controller`

All directional sheets have real alpha after chroma-key cleanup and are imported
without mipmaps.

## Validation checklist

- Press `START` repeatedly and confirm only one entry flow starts.
- Confirm the room never appears before the door animation.
- Reload saves at each body threshold and confirm one correct skin is visible.
- Trigger repeated stage changes and confirm no old body remains underneath.
- Run `Validate Character Rig` and `Run 50 Skin Swaps` on `CharacterRoot`.
- Wait at least one minute and confirm multiple room actions occur.
- Confirm side and back walk frames alternate and moving right mirrors correctly.
- Confirm front walking moves opposite arms and legs.
- Confirm blinking, pupil movement and expression changes are visible.
- Tap rapidly while the character is at the window or sofa and confirm gameplay
  totals update immediately without locking the animation.
- Confirm the dumbbell and bottom navigation remain in front of the moving actor.
- Return to the menu and confirm the menu video and approved Settings layout are
  unchanged.
