# Unity Setup

## Recommended project type

Create a Unity 2D mobile project.

## Basic settings

### Platform

- Build target: Android first
- Orientation: Portrait
- Target aspect: 9:16 mobile screen

### Scene

Create the first scene:

`Assets/Scenes/Main.unity`

Suggested hierarchy:

```text
Main Camera
Canvas
  TopHud
    StrengthText
    CoinsText
    BodyStageText
  CharacterArea
    CharacterImage
    TapButton
  UpgradePanel
    DumbbellsButton
    ProteinButton
    CoachButton
    BetterGymButton
EventSystem
GameManager
```

## Script setup

Attach scripts like this:

- `GameManager.cs` → `GameManager` object
- `PlayerStats.cs` → `GameManager` object
- `TapTrainingController.cs` → tap button or character object
- `UpgradeManager.cs` → `GameManager` object
- `MainHudController.cs` → Canvas or HUD object

## Mobile UI rule

Use large buttons and big text. The game should be playable with one thumb.

## Git rule

Commit Unity `.meta` files. Do not commit `Library/`, `Temp/`, `Obj/`, or build files.