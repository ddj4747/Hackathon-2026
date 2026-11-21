# Pushable blocks

## Status
- Implemented, not yet verified in the editor or in Play mode (files were written outside Unity; check the console after import).
- Test blocks in `SampleScene` (level 1) around the player start cell (0,0): single blocks at (0,3) and (0,-3); a chain of 3 to the right at (3..5,0); a chain of 4 to the left at (-3..-6,0), which exceeds the default limit of 3 and cannot be pushed.

## Entry files
- `Assets/Scripts/Pushable.cs`: add to any object to make it pushable.
- `Assets/Scripts/Solid.cs`: add to walls/obstacles; nothing can enter their cell.
- `Assets/Scripts/PushSettings.cs` + `Assets/Resources/PushSettings.asset`: global push rules (one place for all objects).
- `Assets/Prefabs/PushableBlock.prefab` (SpriteRenderer sorting order 5 + `Pushable`), `Assets/Sprites/Block.png` (16 x 16 px, 16 PPU, point filter, no compression; the old placeholder `BlockSprite.png` is unused and can be deleted).
- `Assets/Scripts/PlayerGridMovement.cs`: does the pushing (see [PLAYER_MOVEMENT.md](PLAYER_MOVEMENT.md)).

## Behavior and rules
- A pushable snaps to the grid cell it is placed on and occupies it.
- When the player steps toward a cell holding a pushable, the object is shoved one cell in the same direction; both move at the player's speed, one cell apart. Holding the key keeps pushing it along.
- Chains: all pushables lined up in the pushed direction move together, one cell, at the player's speed. The chain is limited to `PushSettings.maxChainLength` objects (default 3, 0 = unlimited; edit `Resources/PushSettings.asset`). Over the limit, or if the cell after the chain is `Solid`, nothing moves and the player stays put.
- `Solid` objects block the player and chains. Pushables themselves have no collider. Tilemap tiles, level exits and other colliders are not treated as solid; which things count as walls is still undecided (see root `AGENTS.md`). No `Solid` prefab exists yet.
- `Pushable.stopsSignal` (public bool, default false, shown in the Inspector): meant to say whether the object stops the signal, i.e. whether the connection range passes through it. It is only the flag; nothing reads it yet, and the signal/range mechanic itself is undecided.
- Pulling and diagonal pushes are not implemented.

## Public interfaces and dependencies
- `Pushable.GetAt(cell)`, `Pushable.TryBeginChain(startCell, direction, list)`, `Cell`, `SetPushProgress(t)` (animated by the player each frame); `Solid.IsAt(cell)`; `PushSettings.Instance.maxChainLength`.
- Uses the scene's `Grid` (`MapGrid`), found automatically if the `grid` field is empty.

## Setup and verification
- Add `PushableBlock.prefab` to a level (position anywhere; it snaps to the nearest cell centre), or add `Pushable` to any other object.
- Press Play in `SampleScene` and walk into the blocks with WASD/arrows; change `maxChainLength` in the asset to see the limit move.
