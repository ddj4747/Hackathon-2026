# Level select (Menu scene)

## Status
- `Menu` scene with level plates implemented; first in the build's scene list.
- Plate 1 loads `Levels/Level0`, plate 2 loads `Levels/Level1`; plate 3 is an unassigned placeholder and logs a warning when selected.
- Levels return to `Menu` through a `LevelExit` ([LEVEL_EXIT.md](level-exit.md)); no keyboard shortcut.

## Entry files
- `Assets/Scenes/Menu.unity` (camera, Global Light 2D, `MapGrid`, `Player`, three `LevelPlate` instances at cells (3,0), (5,0), (7,0))
- `Assets/Prefabs/LevelPlate.prefab`, `Assets/Scripts/LevelSelectPlate.cs`, `Assets/Sprites/LevelPlateSprite.png`
- `Assets/InputSystem_Actions.inputactions`: shared `Confirm` action (Space, gamepad south) in the `Player` map
- `ProjectSettings/EditorBuildSettings.asset`: `Menu`, `SampleScene`, `Level1` and `Level0` are enabled.

## Behavior and rules
- A plate snaps to the grid cell it is placed on. It highlights (yellow tint, grows 1.3x) while the player's centre is inside its cell.
- Confirm (Space, gamepad south) loads the plate's scene, only while the player stands still on that plate.
- Inspector fields on `LevelSelectPlate`: `Level Number` (text on the plate), `Scene Asset` (scene picker; its path is baked into a hidden `scenePath` in `OnValidate` so it works in builds), plus colours and highlight scale.
- On returning from a level, the plate whose scene matches `GameManager.ReturnedFromScenePath` teleports the player onto itself in `Start`, then clears the marker. A level with no matching plate leaves the player at the scene's start position.
- A scene must be in the build's scene list to be loadable; otherwise the plate logs an error.
- The number is a legacy `TextMesh` with `nokiafc22`, sorted above the plate (order 2) but below blocks (5) and the player (10), so the player covers the number; font and pixel rules in [PIXEL_PERFECT_AND_FONT.md](pixel-perfect-and-font.md).

## Public interfaces and dependencies
- `LevelSelectPlate` uses `Grid`, `PlayerGridMovement` ([PLAYER_MOVEMENT.md](player-movement.md), `TeleportToCell`), `GameManager` and the project-wide actions (`InputSystem.actions`, action `Player/Confirm`, constant `GameManager.ConfirmActionPath`).
- `PlayerGridMovement` itself still reads the keyboard directly; only Confirm uses the input actions asset.

## Setup and verification
- New level plate: drag `LevelPlate.prefab` into a scene with `MapGrid` and `Player`, set `Level Number`, assign `Scene Asset`, and add that scene to the build's scene list.
- Unity verified both scene assets and their baked paths on the saved Menu scene; selection through player input has not been tested for these assignments.
