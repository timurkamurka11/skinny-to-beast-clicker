# Living Gameplay Screen

This patch replaces the single baked gameplay illustration with independent runtime
layers. The main menu and Settings popup are not rebuilt or restyled.

## Runtime hierarchy

```text
GameplayWindow
├── LivingGameplayScene
│   ├── RoomStage01
│   ├── RoomStage02
│   ├── AmbientLayer
│   │   ├── WindowNightGlow
│   │   ├── LampWarmGlow
│   │   └── FloatingDust (18 pooled particles)
│   ├── UpgradeProps
│   ├── CharacterRoot
│   │   ├── AnimatorLayer
│   │   ├── BellyJiggleMask
│   │   └── Blink overlays
│   └── DumbbellRoot
├── TapEffects (pooled text and sparks)
└── SafeArea (existing HUD, tap hotspot, navigation and upgrades)
```

## Character animation states

The Editor automatically creates
`Assets/Resources/UI/Gameplay/Living/Animations/LivingCharacter.controller`
after the scripts compile.

| State | Trigger | Duration | Behavior |
|---|---|---:|---|
| `Idle_Breathe` | default | 2.4 s loop | Breathing, slight vertical movement and sway |
| `Blink` | scheduler | 0.12–0.22 s | Independent eyelid overlay, optional double blink |
| `TapReact_A` | `TapA` | 0.28 s | Upward squash, recoil and recovery |
| `TapReact_B` | `TapB` | 0.34 s | Side recoil, squash and recovery |
| `Idle_LookDown` | `RareLook` | 1.15 s | Tired downward slump |
| `Idle_Scratch` | `RareScratch` | 1.4 s | Small torso and belly fidget |
| `UpgradeReact` | `Upgrade` | 0.9 s | Excited bounce |
| `StageChange` | `StageChange` | 1.1 s | Anticipation, scale burst and recovery |

Blink intervals are randomized between 2.5 and 6 seconds. A rare idle is requested
after 8–16 seconds without a tap. Every tap resets the rare-idle timer.

All states have a procedural fallback. The character still breathes, blinks and
reacts if the generated Animator Controller is temporarily unavailable during an
Editor reimport.

## Tap response

Every accepted tap performs these operations independently:

1. `TapTrainingController` adds the exact power, reps and coins.
2. The dumbbell jumps, squashes and rotates.
3. The character receives alternating `TapA` / `TapB` triggers.
4. The belly overlay receives a short damped impulse.
5. A pooled `+POWER` label appears.
6. Between 8 and 13 pooled sparks are emitted.
7. The procedural impact sound plays with a small random pitch variation.
8. A short haptic pulse runs when `settings.vibration` is enabled.

The visual reaction is never used to decide whether a tap counts. Fast tapping can
restart or accumulate visual impulses without dropping gameplay input.

## Visual progression

The seven gameplay body stages are mapped to four generated character sprites:

| Gameplay stage | Art |
|---|---|
| `Skinny` | `character_stage_01.png` |
| `Beginner` | `character_stage_02.png` |
| `Fit`, `Athletic` | `character_stage_03.png` |
| `Big`, `Beast`, `Gym Legend` | `character_stage_04.png` |

Upgrade visuals:

| Upgrade | Visible change |
|---|---|
| `Dumbbells` level 0 | Worn starter dumbbell |
| `Dumbbells` levels 1–3 | Modern adjustable dumbbell |
| `Dumbbells` level 4+ | Maximum heavy dumbbell |
| `Protein` level 1+ | Protein tub and shaker appear |
| `Coach` level 1+ | Training board and coach phone appear |
| `Better Gym` level 1+ | Room crossfades to the renovated home gym |

Character, dumbbell and room replacements use crossfades and burst feedback.

## Performance limits

- 18 ambient dust particles.
- 18 pooled floating-text objects.
- 54 pooled spark objects.
- No `Instantiate` or `Destroy` call is performed for individual taps.
- Animation uses unscaled time so the UI remains stable during time-scale changes.
- Source sprites are imported without mipmaps and with a 2048 maximum texture size.

## Validation checklist

- Open `MainMenu` and press `START`.
- Confirm the character immediately breathes and sways.
- Wait at least 6 seconds and confirm blinking occurs.
- Tap 10 times and confirm the stored totals increase exactly 10 times.
- Confirm the dumbbell reacts to all 10 taps even if character reactions overlap.
- Disable vibration in Settings and confirm tap haptics stop.
- Purchase every upgrade once and confirm its visual appears.
- Cross a body threshold and confirm the character sprite crossfades.
- Return to the menu and confirm the menu video resumes.
- Confirm the Settings popup still uses its previously approved layout.

## Technical references

- Unity Animator triggers:
  https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Animator.SetTrigger.html
- Unity animation parameters:
  https://docs.unity3d.com/6000.3/Documentation/Manual/AnimationParameters.html
- Unity unscaled Animator updates:
  https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AnimatorUpdateMode.html
- Unity Android vibration:
  https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Handheld.Vibrate.html
