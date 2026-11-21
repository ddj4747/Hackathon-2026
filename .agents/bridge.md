# Bridge

## Status
- `Bridge`, its prefabs and palette tiles are implemented. Not yet verified in the editor.

## Entry files
- `Assets/Scripts/Bridge.cs`: component on each bridge cell
- `Assets/Prefabs/Bridge.prefab` (`walkable` off) and `Assets/Prefabs/BridgeWalkable.prefab` (`walkable` on), sorting order 1, `GridSnap`
- `Assets/EntityTiles/BridgeTile.asset` and `BridgeWalkableTile.asset`: `PrefabTile` assets, placed at cells (8,-8) and (9,-8) of `Assets/TilePalettes/Signal Palette.prefab`
- `Assets/Sprites/Bridge.png`: placeholder 16 x 16 plank sprite
- Integration: `DualGridLayer.IsBlocked` (used by `Solid.IsBlocked`, the player and pushing)

## Behavior and rules
- Each bridge has a `walkable` bool (Inspector, or `Bridge.Walkable` from code, for example from a signal receiver event).
- Bridges whose cells touch up, down, left or right form a group. If any bridge in the group has `walkable` set, every bridge in the group is walkable. Diagonal neighbours are not in the group. Groups are computed on demand and cached until a bridge is enabled, disabled or toggled.
- A walkable bridge on a Water cell lets the player walk on it and lets a pushed chain enter it. A bridge on a Wall cell changes nothing. A bridge that is not walkable leaves the water blocking, and is drawn faded (`inactiveAlpha`).
- A bridge only matters where the cell is Water. Place it on water tiles.

## Public interfaces and dependencies
- `Bridge.IsWalkableAt(grid, cell)`, `Bridge.Walkable`, `Bridge.IsOpen` (group state), `Bridge.Cell`.
- Needs a `Grid` (the `MapGrid` prefab); it uses the parent Grid or the first Grid in the scene.

## Known limits
- Closing a bridge while the player or a pushable stands on it is not handled: they stay in place and the cell blocks entering again.
- Nothing is connected to the bool yet; toggle it from the Inspector or your own script.

## Setup and verification
- Paint Bridge tiles from the Signal Palette over water cells, tick `walkable` on one of them in Play mode and check that the player can walk over all touching bridges.
