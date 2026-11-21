# Pushable blocks

## Status
- Implemented, not yet verified in the editor or in Play mode (files were written outside Unity; check the console after import).
- Test blocks in `SampleScene` (level 1) around the player start cell (0,0): single blocks at (0,3) and (0,-3); a chain of 3 to the right at (3..5,0); a chain of 4 to the left at (-3..-6,0), which exceeds the default limit of 3 and cannot be pushed.

## Entry files
- `Assets/Scripts/Pushable.cs`: add to any object to make it pushable.
- `Assets/Scripts/Solid.cs`: add to walls/obstacles; nothing can enter their cell.
- `Assets/Scripts/PushSettings.cs` + `Assets/Resources/PushSettings.asset`: global push rules (one place for all objects).
- `Assets/Prefabs/PushableBlock.prefab` (SpriteRenderer sorting order 5 + `Pushable`), `Assets/Sprites/BlockSprite.png` (16 x 16 px, 16 PPU, placeholder crate).
- `Assets/Scripts/PlayerGridMovement.cs`: does the pushing (see [PLAYER_MOVEMENT.md](player-movement.md)).

## Behavior and rules
- A pushable snaps to the grid cell it is placed on and occupies it.
- When the player steps toward a cell holding a pushable, the object is shoved one cell in the same direction; both move at the player's speed, one cell apart. Holding the key keeps pushing it along.
- Chains: all pushables lined up in the pushed direction move together, one cell, at the player's speed. The chain is limited to `PushSettings.maxChainLength` objects (default 3, 0 = unlimited; edit `Resources/PushSettings.asset`). Over the limit, or if the cell after the chain is `Solid`, nothing moves and the player stays put.
- `Solid` objects block the player and chains. Wall and Water cells of a `DualGridLayer` block them too (`Solid.IsBlocked(grid, cell)`): the player cannot enter them and a chain cannot be shoved onto them (nothing moves, the player stays put). Pushables themselves have no collider. The `Terrain` tilemap, level exits and other colliders are not treated as solid. Water under a walkable `Bridge` is passable for the player and chains ([bridge.md](bridge.md)); walls are never. No `Solid` prefab exists yet.
- Pulling and diagonal pushes are not implemented.
- Enable `stopsSignal` on a Pushable to obstruct wireless signal at its logical occupied cell, including during a push. See [Connection manager](connection-manager.md).

## Public interfaces and dependencies
- `Pushable.GetAt(cell)`, `Pushable.TryBeginChain(startCell, direction, list)`, `Cell`, `TeleportToCell(cell)` (instant), `BeginMoveTo(cell)` (undo slide, animated by `SetPushProgress`), `SetPushProgress(t)` (animated by the player each frame); `Solid.IsAt(cell)`, `Solid.IsBlocked(grid, cell)` (solid objects plus wall/water terrain); `PushSettings.Instance.maxChainLength`.
- Uses the assigned `Grid`, or the nearest parent Grid, falling back to a scene Grid when empty. Directional wire pieces and repeaters also use `Pushable`; see [Cables](cables.md) and [Repeater](repeater.md).

## Setup and verification
- Add `PushableBlock.prefab` to a level (position anywhere; it snaps to the nearest cell centre), or add `Pushable` to any other object.
- Press Play in `SampleScene` and walk into the blocks with WASD/arrows; change `maxChainLength` in the asset to see the limit move.
- Placing in a level: the `Player` and `PushableBlock` tiles (`PrefabTile` assets `Assets/EntityTiles/PlayerTile.asset`, `PushableBlockTile.asset`) are in `Assets/TilePalettes/Signal Palette.prefab`, in the bottom row next to the signal emitters (cells (6,-8) and (7,-8)). Paint them on the level's `Terrain` tilemap like the signal tiles: the editor shows the preview sprite and the real prefab is instantiated when Play starts, at the cell centre, as a child of the Terrain tilemap. `Assets/Palettes/Entities.prefab` is an extra palette with the same two tiles. Not verified in the editor.
