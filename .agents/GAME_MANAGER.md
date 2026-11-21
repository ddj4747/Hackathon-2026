# Game manager

## Status
- `GameManager` implemented: owns the return to the menu and level restart (R). Not yet verified in the editor or in Play mode.
- Tracks beaten levels persistently. No pause is managed. The earlier Esc prompt was removed.

## Entry files
- `Assets/Scripts/GameManager.cs`, `Assets/Editor/GameManagerEditor.cs`
- `Assets/InputSystem_Actions.inputactions`: shared `Player/Confirm` action (Space, gamepad south) and `Player/Restart` (R, keyboard only) and `Player/Undo` (Q, keyboard only; handled by `PlayerGridMovement`)

## Behavior and rules
- Creates itself at startup (`RuntimeInitializeOnLoadMethod`, after the first scene loads) and persists across scenes; do not place it in scenes.
- `ReturnToMenu()` records the current level's scene path in `ReturnedFromScenePath`, then loads `Menu`. It does nothing while already in `Menu`.
- In LateUpdate, a level with at least one `SignalReceiver` is marked beaten when all receivers and collectors in the active scene are enabled and simultaneously supplied, including either signal type. Disabled or inactive receivers count as unsupplied. Scenes without receivers are not automatically completed. Exiting alone does not complete a level.
- `MarkCurrentLevelBeaten()` saves the active scene path as beaten in `PlayerPrefs`; it ignores the menu and unsaved scenes. `IsLevelBeaten(scenePath)` reads that persistent status. `SetLevelBeaten(scenePath, beaten)` sets or clears it and ignores empty paths and the menu. Once saved, progress survives loss of signal, restarts and game sessions. Scene paths are the identifiers, so moving or renaming a scene changes its progress key.
- In Play mode, the runtime GameManager Inspector lists beaten scene assets and allows adding a scene or removing an entry to change saved progress. The menu scene cannot be added. The list discovers existing scene assets under Assets; deleted or moved scene paths cannot be shown. If the active level already satisfies its receivers, normal completion can mark it beaten again.
- The menu's level plates read `ReturnedFromScenePath` to put the player back on the right plate and then call `ClearReturn()`. Loading any non-menu scene clears a stale marker.
- `RestartLevel()` (also bound to `Player/Restart`, key R): reloads the active level scene by build index with `LoadSceneAsync`. A full-screen loading screen (black screen-space-overlay canvas, sorting order max, text "Loading..." in `nokiafc22`) is built in code as a child of the manager and shown while loading, for at least 0.3 s. Ignored in `Menu`, while another load is running, and for scenes not in the build's scene list. Everything in the level resets (player, blocks). The loading screen is not used for other scene changes yet.
- Controls reminder: a screen-space overlay canvas (sorting order 100, scales with the screen from a 960 x 540 reference) built in code as a child of the manager shows "SPACE - select / WASD - movement / Q - undo / R - reset" in the bottom-right corner of every scene, in `nokiafc22` with a black outline. The text is the constant `ControlsHintText`; update it when controls change. R is ignored in the menu but still listed.
- Space is the shared confirm/activate input: bind new confirm-style interactions to `Player/Confirm` instead of adding keys. `Jump` also uses Space in the asset but is unused (no jump mechanic).

## Public interfaces and dependencies
- Successful restart and return to menu play placeholder effects through the [sound manager](sound-manager.md).
- Completion uses `SignalReceiver.IsSignalSupplied`; see [Signal receiver](signal-receiver.md).
- `GameManager.Instance`, `ReturnToMenu()`, `RestartLevel()`, `MarkCurrentLevelBeaten()`, `IsLevelBeaten(scenePath)`, `SetLevelBeaten(scenePath, beaten)`, `ReturnedFromScenePath`, `ClearReturn()`, constants `ConfirmActionPath`, `RestartActionPath` and `MenuSceneName` (`"Menu"`, must be in the build's scene list).
- Used by `LevelExit` ([LEVEL_EXIT.md](level-exit.md)) and `LevelSelectPlate` ([LEVEL_SELECT.md](level-select.md)).

## Setup and verification
- `Menu` is in the build's scene list (index 0). Walk into a `LevelExit` in a level to return; press R in a level to restart it.
- Unity preview-scene checks verified setting and clearing a temporary progress key and adding/removing a temporary scene from the Inspector list; the Inspector control has not been tested interactively.
- Unity CLI preview-scene checks verified completion requires at least one receiver and all receivers to be enabled and supplied, including inactive and disabled cases. End-to-end saving has not been checked in Play mode.
