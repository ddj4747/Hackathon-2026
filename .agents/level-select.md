# Level select (Menu scene)

## Status
- `Menu` scene with level plates implemented; first in the build's scene list.
- Plates 1-5 load `Levels/Level0` through `Levels/Level4`, respectively. Plates 0 and 6-9 are unassigned placeholders with the same gray normal tint; Tutorial and Coming soon labels remain in the room.
- Levels return to `Menu` through a `LevelExit` ([LEVEL_EXIT.md](level-exit.md)); no keyboard shortcut.

## Entry files
- `Assets/Scenes/Menu.unity` (camera, Global Light 2D, `MapGrid`, `Player`, ten `LevelPlate` instances in the reference layout)
- `Assets/Prefabs/LevelPlate.prefab`, `Assets/Scripts/LevelSelectPlate.cs`, `Assets/Scripts/LevelSelectCable.cs`, `Assets/Sprites/LevelPlateSprite.png`, `Assets/Resources/WiresGround.png`
- `Assets/InputSystem_Actions.inputactions`: shared `Confirm` action (Space, gamepad south) in the `Player` map
- `ProjectSettings/EditorBuildSettings.asset`: `Menu`, `SampleScene`, and `Level0` through `Level4` are enabled.

## Behavior and rules
- A plate snaps to the grid cell it is placed on. It highlights (yellow tint, grows 1.3x) while the player's centre is inside its cell.
- Plates follow the reference path: 0-1-2-3-4, branching through 5-7 and 6 to 8, then 9. Their cells are 0 (-7,-3), 1 (-6,-4), 2 (-4,-2), 3 (-2,-2), 4 (0,-3), 5 (2,-2), 6 (0,0), 7 (4,0), 8 (2,2), and 9 (1,4). The dual-grid floor forms an 18 by 12 room; automatic walls around the floor keep the player inside.
- Confirm (Space, gamepad south) loads the plate's scene only while the player stands still on it and its prerequisite is met. Plates 2-7 retain their existing preceding-plate requirements. Plates 8 and 9 have no prerequisites assigned yet; progression for the new branches, the unassigned tutorial, and its level-1 rule remain unresolved.
- Ground cables use `WiresGround.png` sprites and show whether their source plate's scene is beaten. Cables follow 0-1-2-3-4, 4-5-7-8, 4-6-8 and 8-9. The shared junction below 8 uses an inactive static sprite; its power rule is unresolved.
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
- Unity CLI scene checks verified all ten plate labels and cells, existing assignments and prerequisites, valid cable sprite references, and a walkable route from the player start to every plate. The new layout and walking have not been checked in Play mode.
