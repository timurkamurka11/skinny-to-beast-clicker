# MVP Plan

## Goal

Build the smallest playable version of Skinny to Beast Clicker in Unity.

## MVP version 0.1

### Screen

One vertical mobile screen:

- Character in the center
- Tap zone over the character
- Strength counter
- Coins counter
- Current body stage
- Upgrade buttons at the bottom

### Gameplay

- Player taps the character.
- Each tap adds reps.
- Reps increase strength.
- Strength unlocks body stages.
- Coins are earned from training milestones.
- Coins buy upgrades.
- Upgrades increase tap power or passive training.

### First upgrades

1. Dumbbells — increases tap strength.
2. Protein — increases coins from training.
3. Coach — increases auto reps.
4. Better Gym — increases all gains.

### First body stages

1. Skinny
2. Beginner
3. Fit
4. Athletic

### First scripts

- `GameManager.cs` — initializes the game.
- `PlayerStats.cs` — stores strength, coins, reps, body stage.
- `TapTrainingController.cs` — handles tap input.
- `UpgradeData.cs` — upgrade config.
- `UpgradeManager.cs` — purchase and scaling logic.
- `MainHudController.cs` — updates UI text.
- `NumberFormatter.cs` — short number formatting.

## Not in MVP

- Ads
- In-app purchases
- Online saves
- Leaderboards
- Complex character customization
- Multiple gyms
- Bosses/challenges

## Development order

1. Create Unity 2D project.
2. Set Android portrait orientation.
3. Build main scene.
4. Add UI text and tap button.
5. Add PlayerStats and TapTrainingController.
6. Add upgrades.
7. Add body stage changes.
8. Test on Android.
9. Add polish: animations, particles, sound.
10. Only after that add ads and purchases.