# Level select (Menu scene)

## Status
- `Menu` scene with level plates implemented; first in the build's scene list.
- Plate 0 loads the tutorial `Levels/Level-1`; plates 1-6 load `Levels/Level0` through `Levels/Level5`, respectively. Plates 7-9 are unassigned placeholders; Tutorial and Coming soon labels remain in the room.
- Levels return to `Menu` through a `LevelExit` ([LEVEL_EXIT.md](level-exit.md)); no keyboard shortcut.

## Entry files
- `Assets/Scenes/Menu.unity` (camera, Global Light 2D, `MapGrid`, `Player`, ten `LevelPlate` instances in the reference layout)
- `Assets/Prefabs/LevelPlate.prefab`, `Assets/Scripts/LevelSelectPlate.cs`, `Assets/Scripts/LevelSelectCable.cs`, `Assets/Sprites/LevelEntryBlock.png`, `Assets/Resources/WiresGround.png`
- `Assets/Sprites/MenuBackground/`: four 352 x 256 background frames, looping animation and Animator controller.
- `Assets/InputSystem_Actions.inputactions`: shared `Confirm` action (Space, gamepad south) in the `Player` map
- `ProjectSettings/EditorBuildSettings.asset`: `Menu`, `SampleScene`, `Level-1`, and `Level0` through `Level5` are enabled.

## Behavior and rules
- The supplied four-frame map fills the camera view as an animated background, above the dual-grid terrain artwork and below menu objects. The terrain defines walkable land and blocked water, while its artwork is covered by the background. Frames use point filtering at 16 pixels per unit; frame 0 holds longer than the other frames.
- A plate snaps to its grid cell. Its background comes from `Assets/Sprites/LevelButton.png` (32 x 16, sliced at 16 PPU, Point, uncompressed): `LevelButton_Normal` (left half) while not selected, `LevelButton_Selected` (right half) while the player's centre is inside its cell (`normalSprite` / `highlightSprite` on the prefab; tints `normalColor` / `highlightColor` are white). It does not grow or animate. Unity MCP Play-mode check verified the sprite and number colour swap on and off a plate.
- Plates follow the reference path: 0-1-2-3-4, branching through 5-7 and 6 to 8, then 9. Their cells are 0 (-7,-3), 1 (-6,-4), 2 (-4,-2), 3 (-2,-2), 4 (0,-3), 5 (2,-2), 6 (0,0), 7 (4,0), 8 (2,2), and 9 (1,4). The dual-grid floor follows the background coastline at tile resolution: a cell is walkable when its centre falls on the dark land pixels; other cells are blocked. The smaller islands are disconnected from the main landmass.
- Confirm (Space, gamepad south) loads the plate's scene only while the player stands still on it and its prerequisite is met. Plate 0 has no prerequisite; plate 1 unlocks after the tutorial is marked beaten. Plates 2-4 require the preceding plate's level. Plates 5 and 6 both require plate 4's level; plate 7 requires plate 6's level. Plates 8 and 9 have no prerequisites assigned yet; their progression remains unresolved.
- Ground cables use `WiresGround.png` sprites and show whether their source plate's scene is beaten. Cables follow 0-1-2-3-4, 4-5-7-8, 4-6-8 and 8-9. The shared junction below 8 uses an inactive static sprite; its power rule is unresolved.
- Inspector fields on `LevelSelectPlate`: `Level Number` (text on the plate), `Scene Asset` (scene picker; its path is baked into a hidden `scenePath` in `OnValidate` so it works in builds), plus normal and highlight colours.
- On returning from a level, the plate whose scene matches `GameManager.ReturnedFromScenePath` teleports the player onto itself in `Start`, then clears the marker. A level with no matching plate leaves the player at the scene's start position.
- A scene must be in the build's scene list to be loadable; otherwise the plate logs an error. Selecting an unassigned placeholder logs a warning and does not load a scene.
- The number is a legacy `TextMesh` with `nokiafc22`, colour #333C57 (set on the `LevelPlate` prefab, no scene overrides), #566C86 while the player stands on the plate (`numberHighlightColor`; Unity MCP Play-mode check verified both colours), sorted above the plate (order 2) but below blocks (5) and the player (10), so the player covers the number; font and pixel rules in [PIXEL_PERFECT_AND_FONT.md](pixel-perfect-and-font.md).

## Public interfaces and dependencies
- Valid level selection plays a placeholder effect through the [sound manager](sound-manager.md).
- `LevelSelectPlate` uses `Grid`, `PlayerGridMovement` ([PLAYER_MOVEMENT.md](player-movement.md), `TeleportToCell`), `GameManager` and the project-wide actions (`InputSystem.actions`, action `Player/Confirm`, constant `GameManager.ConfirmActionPath`).
- `PlayerGridMovement` itself still reads the keyboard directly; only Confirm uses the input actions asset.

## Setup and verification
- Unity CLI preview-scene checks compared all 352 viewport cells with the background, verified the player can reach all ten plates, and checked the plate sprite and stationary highlight. Appearance and movement have not been checked in Play mode.
- New level plate: drag `LevelPlate.prefab` into a scene with `MapGrid` and `Player`, set `Level Number`, assign `Scene Asset`, and add that scene to the build's scene list.
- Unity preview-scene checks verified the plate assignments, cable sprites, completion-based unlocking and cable state changes. Selection through player input has not been tested in Play mode.
- Unity CLI scene checks verified all ten plate labels and cells, existing assignments and prerequisites, and valid cable sprite references.
- Unity MCP Play-mode check: with `Level-1` saved as beaten, plate 0 reports beaten, plate 1 is unlocked and `Cable 0 to 1` is powered.
- Unity CLI Play-mode checks verified that supplying both receivers in the level on plate 4 saves its completion and unlocks plates 5 and 6, while supplying only one does not. The test restored the existing saved completion state. Plate 6's scene loaded with an active player and exit; selection through player input was not tested.
