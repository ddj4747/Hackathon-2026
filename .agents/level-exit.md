# Level exit

## Status
- `LevelExit` prefab implemented. Not yet verified in the editor or in Play mode.
- No level contains an exit yet: `SampleScene` has no walls, so the prefab still has to be placed by whoever builds the level walls.

## Entry files
- `Assets/Prefabs/LevelExit.prefab` (dark one-cell "hole" sprite, trigger `BoxCollider2D` 0.5 x 0.5), `Assets/Scripts/LevelExit.cs`, `Assets/Sprites/LevelExitSprite.png`

## Behavior and rules
- Snaps to the grid cell it is placed on. Its collider is always a trigger.
- When the player's collider overlaps it (about halfway into the cell), it calls `GameManager.ReturnToMenu()`; the menu then starts with the player standing on the plate of the level they left ([LEVEL_SELECT.md](level-select.md)).

## Public interfaces and dependencies
- Needs a `GameManager` ([GAME_MANAGER.md](GAME_MANAGER.md)) and the player's kinematic `Rigidbody2D` + small `BoxCollider2D` ([PLAYER_MOVEMENT.md](player-movement.md)); trigger events need a rigidbody on one side.

## Setup and verification
- Drag `LevelExit.prefab` into a level, on the wall cell that should be the hole (or leave the wall tile out and put the exit there). Wall collision itself is not implemented.
