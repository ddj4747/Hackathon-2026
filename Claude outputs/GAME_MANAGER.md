# Game manager

## Status
- `GameManager` implemented: back-to-menu prompt on Esc. Not yet verified in the editor or in Play mode.
- Nothing else is managed yet (no pause, level progress or saving).

## Entry files
- `Assets/Scripts/GameManager.cs`
- `Assets/InputSystem_Actions.inputactions`: `Player/Confirm` (Space, gamepad south) and `Player/Back` (Esc, gamepad start)

## Behavior and rules
- Creates itself at startup (`RuntimeInitializeOnLoadMethod`, after the first scene loads) and persists across scenes; do not place it in scenes.
- Esc on any scene except `Menu` opens a window in the bottom-right corner; Esc again closes it. Confirm (Space) while it is open loads `Menu`.
- The window is a runtime-built uGUI canvas (reference resolution 512 x 352, legacy `Text` with the built-in font). Temporary look.
- Space is the shared confirm/activate input: bind new confirm-style interactions to `Player/Confirm` instead of adding keys. `Jump` also uses Space in the asset but is unused (no jump mechanic).
- The game keeps running while the window is open (no pause).

## Public interfaces and dependencies
- `GameManager.Instance`, `IsPromptOpen` (other confirm handlers should ignore Confirm while it is true), constants `ConfirmActionPath`, `BackActionPath`, `MenuSceneName` (`"Menu"`).
- Uses the project-wide input actions (`InputSystem.actions`) and uGUI (`com.unity.ugui`).
- `LevelSelectPlate` shares the Confirm action ([LEVEL_SELECT.md](LEVEL_SELECT.md)).

## Setup and verification
- `Menu` must be in the build's scene list (it is, at index 0). Run any level scene, press Esc, then Space.
