# Level exit

## Status
- `LevelExit` is an always-open exit back to the menu. Levels 0 through 4 each have an exit in their left wall.

## Entry files
- `Assets/Prefabs/LevelExit.prefab`, `Assets/Scripts/LevelExit.cs`, `Assets/Sprites/LevelExitSprite.png`.

## Behavior and rules
- Exits are open independently of signal. The visible hole has a trigger collider; walking into it returns the player to the menu, including when the exit becomes active while the player is already on its cell.
- A wall exit allows player entry but does not let pushables through or override solid objects, lasers, or water. The exit snaps to its parent Grid, or a scene Grid if unparented.
- The menu places the player on the plate for the level they left when one is assigned. See [Game manager](GAME_MANAGER.md).

## Public interfaces and dependencies
- `IsOpen` always returns true. Trigger events require the player's kinematic `Rigidbody2D` and collider; see [Player movement](player-movement.md).

## Setup and verification
- Place `LevelExit.prefab` on a wall cell next to unobstructed walkable floor under the level Grid. Add `Menu` to the build scene list.
- Grid cells: Level0 and Level1 `(-15, -2)`, Level2 and Level4 `(-16, 3)`, Level3 `(-16, -3)`.
- Unity preview-scene checks verified open state, wall-cell registration, clear routes from the existing player spawns, and camera framing in Levels 0-4. These changes have not been tested in Play mode.
