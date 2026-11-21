# Level exit

## Status
- `LevelExit` is a reusable signal-controlled exit. Level0 and Level1 have exits in the right wall linked to their adjacent collectors.

## Entry files
- `Assets/Prefabs/LevelExit.prefab`, `Assets/Scripts/LevelExit.cs`, `Assets/Sprites/LevelExitSprite.png`.

## Behavior and rules
- The exit opens while every assigned collector receives its matching signal; losing any signal or disabling any collector closes it. An empty list or an unassigned entry keeps it closed and logs a warning.
- The hole sprite is visible only while open; its collider is always a trigger. In a wall, the cell blocks the player until the exit opens; on a floor cell it remains walkable while closed. A wall exit does not let pushables through or override solid objects, lasers, or water. An open exit returns the player to the menu on overlap, including if it opens while the player is already standing there. The menu places the player on the plate for the level they left when one is assigned.
- The exit snaps to its parent Grid, or a scene Grid if unparented.

## Public interfaces and dependencies
- Assign each scene collector's `SignalReceiver` component to `Collectors` on `LevelExit`. `IsOpen` exposes the current state. See [Signal receiver](signal-receiver.md) and [Game manager](GAME_MANAGER.md).
- Trigger events require the player's kinematic `Rigidbody2D` and collider; see [Player movement](player-movement.md).

## Setup and verification
- Place `LevelExit.prefab` on a floor cell or a wall cell next to walkable floor, under the level Grid. Place one or more collector prefab instances under the Grid and assign their `SignalReceiver` components to the exit. A collector painted onto a Tilemap is spawned at runtime and cannot be assigned as a scene reference in the Inspector.
- Level0: collector at grid cell (4, -1), exit immediately above at (4, 0). Level1: collector at (4, 0), exit at (4, 1). Add `Menu` to the build scene list.
- Unity MCP preview-scene checks covered one and two collectors, empty and missing entries, signal-type mismatch, disabling a collector, signal loss and the hole sprite.
- A Level0 preview-scene check verified the saved collector reference, closed-wall blocking, entry when supplied, and cancellation of a step when signal is lost. A Play-mode check using a temporary powered repeater verified the collector becomes supplied, the exit renderer is enabled and in the camera view, and walking into it loads Menu. The wall appearance has not been inspected visually.
- A Level1 preview-scene check verified the collector reference and wall blocking before, during and after signal.
