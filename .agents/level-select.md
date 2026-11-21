# Level select (Menu scene)

## Status
- `Menu` scene with level plates implemented; first in the build's scene list.
- Plate 0 loads the tutorial `Levels/Level-1`; plates 1-5 load `Levels/Level0` through `Levels/Level4`, respectively. Plates 6-9 are unassigned placeholders; Tutorial and Coming soon labels remain in the room.
- Levels return to `Menu` through a `LevelExit` ([LEVEL_EXIT.md](level-exit.md)); no keyboard shortcut.

## Entry files
- `Assets/Scenes/Menu.unity` (camera, Global Light 2D, `MapGrid`, `Player`, ten `LevelPlate` instances in the reference layout)
- `Assets/Prefabs/LevelPlate.prefab`, `Assets/Scripts/LevelSelectPlate.cs`, `Assets/Scripts/LevelSelectCable.cs`, `Assets/Sprites/LevelEntryBlock.png`, `Assets/Resources/WiresGround.png`
- `Assets/Sprites/MenuBackground/`: four 352 x 256 background frames, looping animation and Animator controller.
- `Assets/InputSystem_Actions.inputactions`: shared `Confirm` action (Space, gamepad south) in the `Player` map
- `ProjectSettings/EditorBuildSettings.asset`: `Menu`, `SampleScene`, `Level-1`, and `Level0` through `Level4` are enabled.

## Behavior and rules
- The supplied four-frame map fills the camera view as an animated background, above the dual-grid terrain artwork and below menu objects. The terrain defines walkable land and blocked water, while its artwork is covered by the background. Frames use point filtering at 16 pixels per unit; frame 0 holds longer than the other frames.
- A plate snaps to its grid cell and uses the supplied 16 x 16 block sprite at 16 PPU. It changes tint immediately while the player's centre is inside its cell; it does not grow or animate.
- Plates follow the reference path: 0-1-2-3-4, branching through 5-7 and 6 to 8, then 9. Their cells are 0 (-7,-3), 1 (-6,-4), 2 (-4,-2), 3 (-2,-2), 4 (0,-3), 5 (2,-2), 6 (0,0), 7 (4,0), 8 (2,2), and 9 (1,4). The dual-grid floor follows the background coastline at tile resolution: a cell is walkable when its centre falls on the dark land pixels; other cells are blocked. The smaller islands are disconnected from the main landmass.
- Confirm (Space, gamepad south) loads the plate's scene only while the player stands still on it and its prerequisite is met. Plate 0 has no prerequisite; plate 1 unlocks after the tutorial is marked beaten. The tutorial currently has no receivers, so the game manager cannot mark it beaten automatically; its completion condition is unresolved. Plates 2-7 retain their existing preceding-plate requirements. Plates 8 and 9 have no prerequisites assigned yet; progression for the new branches remains unresolved.
- Ground cables use `WiresGround.png` sprites and show whether their source plate's scene is beaten. Cables follow 0-1-2-3-4, 4-5-7-8, 4-6-8 and 8-9. The shared junction below 8 uses an inactive static sprite; its power rule is unresolved.
- Inspector fields on `LevelSelectPlate`: `Level Number` (text on the plate), `Scene Asset` (scene picker; its path is baked into a hidden `scenePath` in `OnValidate` so it works in builds), plus normal and highlight colours.
- On returning from a level, the plate whose scene matches `GameManager.ReturnedFromScenePath` teleports the player onto itself in `Start`, then clears the marker. A level with no matching plate leaves the player at the scene's start position.
- A scene must be in the build's scene list to be loadable; otherwise the plate logs an error. Selecting an unassigned placeholder logs a warning and does not load a scene.
- The number is a legacy `TextMesh` with `nokiafc22`, sorted above the plate (order 2) but below blocks (5) and the player (10), so the player covers the number; font and pixel rules in [PIXEL_PERFECT_AND_FONT.md](pixel-perfect-and-font.md).

## Public interfaces and dependencies
- Valid level selection plays a placeholder effect through the [sound manager](sound-manager.md).
- `LevelSelectPlate` uses `Grid`, `PlayerGridMovement` ([PLAYER_MOVEMENT.md](player-movement.md), `TeleportToCell`), `GameManager` and the project-wide actions (`InputSystem.actions`, action `Player/Confirm`, constant `GameManager.ConfirmActionPath`).
- `PlayerGridMovement` itself still reads the keyboard directly; only Confirm uses the input actions asset.

## Setup and verification
- Unity CLI preview-scene checks compared all 352 viewport cells with the background, verified the player can reach all ten plates, and checked the plate sprite and stationary highlight. Appearance and movement have not been checked in Play mode.
- New level plate: drag `LevelPlate.prefab` into a scene with `MapGrid` and `Player`, set `Level Number`, assign `Scene Asset`, and add that scene to the build's scene list.
- Unity preview-scene checks verified the plate assignments, cable sprites, completion-based unlocking and cable state changes. Selection through player input has not been tested in Play mode.
- Unity CLI scene checks verified all ten plate labels and cells, existing assignments and prerequisites, and valid cable sprite references.
