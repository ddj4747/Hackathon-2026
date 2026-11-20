# Game manager

## Status
- `GameManager` implemented: owns the return to the menu. Not yet verified in the editor or in Play mode.
- Nothing else is managed yet (no pause, level progress or saving). The earlier Esc prompt was removed.

## Entry files
- `Assets/Scripts/GameManager.cs`
- `Assets/InputSystem_Actions.inputactions`: shared `Player/Confirm` action (Space, gamepad south)

## Behavior and rules
- Creates itself at startup (`RuntimeInitializeOnLoadMethod`, after the first scene loads) and persists across scenes; do not place it in scenes.
- `ReturnToMenu()` records the current level's scene path in `ReturnedFromScenePath`, then loads `Menu`. It does nothing while already in `Menu`.
- The menu's level plates read `ReturnedFromScenePath` to put the player back on the right plate and then call `ClearReturn()`. Loading any non-menu scene clears a stale marker.
- Space is the shared confirm/activate input: bind new confirm-style interactions to `Player/Confirm` instead of adding keys. `Jump` also uses Space in the asset but is unused (no jump mechanic).

## Public interfaces and dependencies
- `GameManager.Instance`, `ReturnToMenu()`, `ReturnedFromScenePath`, `ClearReturn()`, constants `ConfirmActionPath` and `MenuSceneName` (`"Menu"`, must be in the build's scene list).
- Used by `LevelExit` ([LEVEL_EXIT.md](LEVEL_EXIT.md)) and `LevelSelectPlate` ([LEVEL_SELECT.md](LEVEL_SELECT.md)).

## Setup and verification
- `Menu` is in the build's scene list (index 0). Walk into a `LevelExit` in a level to return.
