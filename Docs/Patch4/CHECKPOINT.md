# GameWork Patch 4.0 — Durable Checkpoint

Last updated: 2026-07-28
Branch: `patch-4.0`
Repository: `timurkamurka11/skinny-to-beast-clicker`

This file is the canonical continuation point for future Patch 4 work.

## User goal

Replace the Patch 3.5 procedural/basic-shape character with an original hand-drawn overweight adult man, a completely new named skeleton, separated facial artwork and new animations.

The following must remain unchanged:

- `MainMenuLoop.mp4`
- main-menu scenes, prefabs, transitions and button logic
- music, ambient audio and audio mixers
- settings UI, persistence, language, vibration and notifications

## Approved visual source

- Original neutral front master: transparent PNG, 1024 × 1536, RGBA.
- Character: overweight adult man, heavy belly, thick arms and thighs, short dark hair, gray sleeveless shirt, dark shorts/pants and gray shoes.
- Figma file: `tZSr9vinRs9EbZzgatxjda`
- Concept board: node `4:3`
- Rig blueprint board: node `6:3`
- Adobe Creative Cloud source: `urn:aaid:sc:AP:5086d367-0290-430e-b9a7-39e5392bdbde`
- Adobe vector trace: `https://to.adobe.com/aN0OeN9oa589DR97`

The master is approved as the visual source, but it is not a final one-piece Unity sprite.

## Completed P4.0-A — art and rig foundation

- Five-view directional concept reference.
- Clean neutral front master on transparent background.
- Manual layer-cut and 24 px overlap guide.
- New skeleton blueprint and canonical bone names.
- Layer contract for body, head, face, arms, legs, clothes and FX.
- Adobe vector trace and local art-foundation package.

## Completed P4.0-B — runtime and editor pipeline

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

- accepted tap count from `CharacterRigController`
- movement state and facing
- idle/routine action state
- current skin stage from `CharacterSkinController`

It mirrors those signals into the new Patch 4 Animator while Patch 3.5 stays available as rollback.

## Current activation state

Patch 4 must remain **disabled**.

It may activate only when:

1. all required bones exist;
2. all canonical painted sprites exist in the layer catalog;
3. all ten animation clips exist;
4. the generated prefab passes validator checks;
5. only one character body is visible;
6. Unity compilation and Play Mode tests pass.

Until then, Patch 3.5 remains visible.

## Layer export convention

Place full-canvas transparent PNGs in:

`Assets/GameWorkPatch4/Art/Character/FatMan/Layers/`

File names replace the contract slash with one underscore:

- `Body_TorsoBase.png`
- `Face_MouthClosed.png`
- `ArmL_Upper.png`
- `LegR_Foot.png`
- `FX_Shadow.png`

Every draft layer remains 1024 × 1536 so the import postprocessor can apply an exact skeleton-aligned pivot. Transparent trimming/atlas optimization comes only after the reassembled neutral pose is verified.

## Immediate next work

### P4.0-C — actual painted layer production

1. Cut the approved master into the canonical PNG layer set.
2. Manually redraw hidden artwork beneath every moving joint.
3. Create independent eye whites, irises, lids, brows, cheeks and three mouth poses.
4. Reassemble all layers in the neutral pose and compare pixel-for-pixel against the approved master.
5. Import layers into Unity and rebuild the catalog.
6. Run `Tools/GameWork/Patch 4.0/Build/Rebuild Character Prefab`.
7. Install beside the selected legacy character in rollback mode.
8. Run contract and protected-path validation.
9. Compile and test all ten animations in the room.

## Known limitations

- Unity compilation has not yet been executed in the actual editor.
- Final transparent layer PNGs are not yet in the repository.
- Sprite Skin weight painting has not yet been completed.
- Figma Starter reached its three-page limit; the attempted new P4.0-B page was rejected without modifying the file. Existing concept and rig pages remain intact.
