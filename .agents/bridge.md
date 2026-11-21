# Bridge

## Status
- `Bridge`, its prefabs and palette tiles are implemented. Bridge behavior has not yet been verified in Play mode.

## Entry files
- `Assets/Scripts/Bridge.cs`: component on each bridge cell
- `Assets/Prefabs/Bridge.prefab` (`walkable` off) and `Assets/Prefabs/BridgeWalkable.prefab` (`walkable` on), sorting order 1, `GridSnap`
- `Assets/EntityTiles/BridgeTile.asset` and `BridgeWalkableTile.asset`: `PrefabTile` assets, placed at cells (8,-8) and (9,-8) of `Assets/TilePalettes/Signal Palette.prefab`
- `Assets/Sprites/Bridge.png`: placeholder 16 x 16 plank sprite
- `Assets/Prefabs/RedBridgeController.prefab` and `Assets/Scripts/SignalBridgeController.cs`: red signal receiver controlling a selected bridge.
- Integration: `DualGridLayer.IsBlocked` (used by `Solid.IsBlocked`, the player and pushing)

## Behavior and rules
- Each bridge has a `walkable` bool (Inspector, or `Bridge.Walkable` from code, for example from a signal receiver event).
- Bridges whose cells touch up, down, left or right form a group. If any bridge in the group has `walkable` set, every bridge in the group is walkable. Diagonal neighbours are not in the group. Groups are computed on demand and cached until a bridge is enabled, disabled or toggled.
- A walkable bridge on a Water cell lets the player walk on it and lets a pushed chain enter it. A bridge on a Wall cell changes nothing. A bridge that is not walkable leaves the water blocking, and is drawn faded (`inactiveAlpha`).
- A bridge only matters where the cell is Water. Place it on water tiles.

## Public interfaces and dependencies
- `Bridge.IsWalkableAt(grid, cell)`, `Bridge.Walkable`, `Bridge.SetWalkable(bool)` for Inspector UnityEvents, `Bridge.IsOpen` (group state), `Bridge.Cell`.
- Needs a `Grid` (the `MapGrid` prefab); it uses the parent Grid or the first Grid in the scene.

## Known limits
- Closing a bridge while the player or a pushable stands on it is not handled: they stay in place and the cell blocks entering again.
- Assign a directly placed bridge instance to the controller; painted bridge tiles are instantiated only at runtime and cannot be assigned as scene references. The controller sets the assigned bridge closed when signal is lost or the controller is disabled. Other walkable bridges in the same group can still keep the group open.

## Setup and verification
- Paint Bridge tiles from the Signal Palette over water cells, tick `walkable` on one of them in Play mode and check that the player can walk over all touching bridges.
- To control a bridge from any [signal receiver](signal-receiver.md), place the receiver and a bridge in the desired group as scene prefab instances. Add the bridge to both receiver events and select `Bridge.SetWalkable`. For a bridge open with signal, use true for received and false for lost, and clear its initial `walkable` flag. To close it with signal, reverse those values and enable its initial `walkable` flag. Receiver events only fire on transitions; match the initial flag to the no-signal state. Every other bridge in the group must have its own `walkable` flag off for the group to close. The existing `SignalBridgeController` also writes the flag each frame; do not use it to control the same bridge with these events.
- Unity MCP verified receiver event bindings open and close an adjacent bridge group in both configurations in an isolated preview scene; another walkable bridge kept the group open.
- `Assets/Scenes/SignalControlsTest.unity` demonstrates a receiver-controlled two-cell bridge over water; see [Signal receiver](signal-receiver.md) for the test steps.

- Unity MCP checked the red controller turns its assigned bridge on with supply and off on signal loss or controller disable in an isolated preview scene; not tested in Play mode.
