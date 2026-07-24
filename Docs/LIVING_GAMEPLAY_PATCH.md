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
→ door opens
→ gameplay is built and synchronized behind the entry screen
→ entry screen fades out
→ room becomes visible
```

The entry screen is visible for more than 1.5 seconds and blocks repeated input.
`GameplayWindowController.Show()` finishes its synchronous build and first
`Refresh()` before the entry overlay becomes transparent, so a stale body stage
cannot flash for one frame. Its back-facing walk frames use the same center and
foot calibration as the room, keeping the approaching character aligned with its
shadow and clear of the status panel.

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
│   │       │   ├── Bone.Pelvis / Spine / Chest / Neck / Head
│   │       │   ├── Bone.UpperArm / Forearm / Hand (L/R)
│   │       │   └── Bone.Thigh / Shin / Foot (L/R)
│   │       └── DirectionalWalkRenderer
│   └── DumbbellRoot
├── TapEffects
└── SafeArea
```

## Exclusive skin switching

`CharacterSkinController` is the only class allowed to apply body art.

- There are no four enabled stage GameObjects.
- Every visible front-facing rig part receives the same single texture.
- Stage changes fade the current rig to zero, replace the texture, then fade the
  same rig back in.
- Interrupted transitions are stopped and normalized before another skin applies.
- `CharacterRigValidator` verifies all required bones and rejects more than one
  distinct front texture.

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
character. The active rig contains 17 named bones:

- pelvis, spine, chest, neck and head;
- left/right upper arms, forearms and hands;
- left/right thighs, shins and feet.

Breathing affects the chest and spine. Weight shifting affects the pelvis, chest
and head in opposite directions. Walk cycles move opposite arms and legs, bend
the knees and preserve independent feet. Tap, scratch, yawn, stretch, flex and sit
poses animate the relevant bones with eased recovery rather than moving the full
PNG as one rectangle.

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
- scratch, shift weight or return to the dumbbell.

A tap awards gameplay progress immediately. The dumbbell reacts immediately. If
the character is elsewhere, the routine is interrupted and the character returns
quickly to the training anchor before playing the queued lift reaction. Rapid taps
are capped to a small visual queue and never create an unbounded coroutine stack.
The sofa action eases into the seated pose, holds it, then eases back to standing
instead of briefly crouching and immediately snapping upright.

## Face rig

The face controller owns separate eye whites, pupils, eyelids, brows and five
procedural mouth configurations.

- blink interval: 2.5–6 seconds;
- optional double blink: 18% chance;
- eased pupil look targets;
- tired, neutral, focused, happy, strain and yawn expressions;
- stage-specific default expression;
- downward focus and strain during a lift.

## Generated assets

- `game_entry_door.png`
- `Rig/walk_stage_01.png`
- `Rig/walk_stage_02.png`
- `Rig/walk_stage_03.png`
- `Rig/walk_stage_04.png`

All directional sheets have real alpha after chroma-key cleanup and are imported
without mipmaps.

## Validation checklist

- Press `START` repeatedly and confirm only one entry flow starts.
- Confirm the room never appears before the door animation.
- Reload saves at each body threshold and confirm one correct skin is visible.
- Trigger repeated stage changes and confirm no old body remains underneath.
- Wait at least one minute and confirm multiple room actions occur.
- Confirm side and back walk frames alternate and moving right mirrors correctly.
- Confirm front walking moves opposite arms and legs.
- Confirm blinking, pupil movement and expression changes are visible.
- Tap rapidly while the character is at the window or sofa and confirm gameplay
  totals update immediately without locking the animation.
- Confirm the dumbbell and bottom navigation remain in front of the moving actor.
- Return to the menu and confirm the menu video and approved Settings layout are
  unchanged.
