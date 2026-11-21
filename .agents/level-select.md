# Level select (Menu scene)

## Status
- `Menu` scene with level plates implemented; first in the build's scene list.
- Plates 1-5 load `Levels/Level0` through `Levels/Level4`, respectively; a Tutorial label sits above unassigned plate 0. Plates 0, 6 and 7 are unassigned placeholders; the Coming soon label sits below 6 and 7.
- Levels return to `Menu` through a `LevelExit` ([LEVEL_EXIT.md](level-exit.md)); no keyboard shortcut.

## Entry files
- `Assets/Scenes/Menu.unity` (camera, Global Light 2D, `MapGrid`, `Player`, eight `LevelPlate` instances, with plates 4 through 7 in a second row)
- `Assets/Prefabs/LevelPlate.prefab`, `Assets/Scripts/LevelSelectPlate.cs`, `Assets/Scripts/LevelSelectCable.cs`, `Assets/Sprites/LevelPlateSprite.png`, `Assets/Resources/WiresGround.png`
- `Assets/InputSystem_Actions.inputactions`: shared `Confirm` action (Space, gamepad south) in the `Player` map
- `ProjectSettings/EditorBuildSettings.asset`: `Menu`, `SampleScene`, and `Level0` through `Level4` are enabled.

## Behavior and rules
- A plate snaps to the grid cell it is placed on. It highlights (yellow tint, grows 1.3x) while the player's centre is inside its cell.
- The menu has centered rows of plates 0, 1, 2, 3 and 4, 5, 6, 7, with a starting position below them. Its dual-grid floor forms an 18 × 12 room; automatic walls around the floor keep the player inside.
- Confirm (Space, gamepad south) loads the plate's scene only while the player stands still on it and its prerequisite is met. Plates 2-7 require the preceding plate's scene to have been beaten; the unassigned tutorial and its level-1 rule are unresolved.
- Ground cables for plates 0-1, 1-2, 2-3, and 3-4 use `WiresGround.png` sprites and show whether the source scene is beaten. The cable between 3 and 4 routes around the right and above the lower row. Two parallel cables run between plates 4 and 5; the second cable's power rule is unresolved.
- Inspector fields on `LevelSelectPlate`: `Level Number` (text on the plate), `Scene Asset` (scene picker; its path is baked into a hidden `scenePath` in `OnValidate` so it works in builds), plus colours and highlight scale.
- On returning from a level, the plate whose scene matches `GameManager.ReturnedFromScenePath` teleports the player onto itself in `Start`, then clears the marker. A level with no matching plate leaves the player at the scene's start position.
- A scene must be in the build's scene list to be loadable; otherwise the plate logs an error. Selecting an unassigned placeholder logs a warning and does not load a scene.
- The number is a legacy `TextMesh` with `nokiafc22`, sorted above the plate (order 2) but below blocks (5) and the player (10), so the player covers the number; font and pixel rules in [PIXEL_PERFECT_AND_FONT.md](pixel-perfect-and-font.md).

## Public interfaces and dependencies
- Valid level selection plays a placeholder effect through the [sound manager](sound-manager.md).
- `LevelSelectPlate` uses `Grid`, `PlayerGridMovement` ([PLAYER_MOVEMENT.md](player-movement.md), `TeleportToCell`), `GameManager` and the project-wide actions (`InputSystem.actions`, action `Player/Confirm`, constant `GameManager.ConfirmActionPath`).
- `PlayerGridMovement` itself still reads the keyboard directly; only Confirm uses the input actions asset.

## Setup and verification
- New level plate: drag `LevelPlate.prefab` into a scene with `MapGrid` and `Player`, set `Level Number`, assign `Scene Asset`, and add that scene to the build's scene list.
- Unity preview-scene checks verified the plate assignments, cable sprites, completion-based unlocking and cable state changes. Selection through player input has not been tested in Play mode.
- The room and plate positions were saved through Unity CLI; walking against the boundary has not yet been tested in Play mode.
