# Cables

## Status

- `WireTilemap` uses directional, signal-aware wire tiles that become pushable pieces in Play mode. The older path-based cable prefab remains available separately.

## Entry files

- `Assets/Scripts/SignalCable.cs`, `Assets/Scripts/CableAdapter.cs`
- `Assets/Prefabs/SignalCable.prefab`, `Assets/Resources/Wires.png`, `Assets/WireTiles/`
- `Assets/Scripts/WireTile.cs`, `Assets/DirectionalWireTiles/` contain 11 directional tile assets with blue and green sprites.
- `Assets/Scripts/WireNetwork.cs`, `Assets/Scripts/MovableWire.cs`, `Assets/Prefabs/MovableWire.prefab` and `Assets/Prefabs/WireTilemap.prefab` implement and demonstrate the movable tilemap network.
- `Assets/TilePalettes/Wire Palette.prefab` and `Assets/TilePalettes/Signal Palette.prefab` include all 11 directional wire tiles.
- [Connection manager](connection-manager.md), [Map grid](map-grid.md)

## Behavior and rules

- A valid path contains at least two distinct endpoints and uses orthogonally adjacent cells. Intermediate cells define the route but do not receive signal from the cable.
- The endpoint cells connect bidirectionally at unchanged quality when each contains an active adapter. Links are removed when the cable is disabled or the path or endpoints are invalid.
- An adapter's optional `Test Signal` toggle contributes quality 1 at its cell during Play mode; switching it off, disabling it, moving it, or resetting signals updates the contribution.
- The cable resolves an explicitly assigned Grid or its nearest parent Grid in the same scene. Coordinates are scene-grid cell coordinates. Adapters are found by their world position in that grid.
- Directional wire tiles record up, right, down and left ports. Neighboring pieces connect only when both ports face their shared edge, including rotated/flipped tile transforms. In Play mode each tile creates a pushable piece; movement follows the existing chain-pushing rules. Signal uses the destination cell during a push and updates when the layout changes. See [Pushable blocks](pushable-blocks.md).
- An unoccupied neighboring wire port acts as an adapter when an enabled emitter or an enabled, supplied repeater occupies that adjacent cell on the same Grid. Signal reaches every tile in that connected component; green shows supplied, blue shows unsupplied. Changes are recalculated during Play mode. A wire in the adjacent cell prevents that port being an adapter, even if its direction does not match.
- Tilemap wire power is separate from `ConnectionManager` floor signals and the older path-based cable links. An enabled receiver or collector in an adjacent cell is supplied by a powered open wire port facing it; other output is not implemented.

## Public interfaces and dependencies

- `SignalCable` serializes the Grid, ordered path and optional LineRenderer. `CableAdapter` marks an endpoint; the scene-scoped `ConnectionManager` owns signal propagation.
- `WireTile.AreConnected` checks untransformed neighboring tiles. `WireNetwork.IsPowerSupplied` reports whether any tile is powered; `IsPowered(cell)` reports individual tiles. `SuppliesAdjacent(worldPosition, grid)` tests a powered open port facing a neighboring position on the same Grid. `Recalculate()` updates the network immediately.
- The existing manager keys signals by cell coordinate rather than Grid identity; levels using cables should use one signal Grid per scene.

## Setup and verification

- Parent the prefab under the level Grid. Set the path to the intended absolute Grid cells and move its adapter children into the first and last cells. The default path connects (0,0) and (1,0) when the prefab is at the Grid origin.
- Paint the desired 16-pixel `WireTiles` on the prefab's `Cable artwork` Tilemap to match the route. The tilemap is initially empty; the path and artwork are configured separately. Sprites use point filtering and 16 pixels per unit.
- `SampleScene` contains a `WireTilemap` under `MapGrid` with a modified sample layout.
- Unity CLI verified compilation, strongest-signal propagation in both directions, source removal in a loop, link removal and prefab adapter enable/disable behavior. Visual placement remains a level-authoring step.
- Unity CLI also verified propagation through the three example cables and removal when the test toggle is turned off.
- Parent `WireTilemap` beneath the scene Grid and paint `DirectionalWireTiles` on it. The prefab contains four horizontal sample tiles; place an emitter or supplied repeater on the adjacent cell to the left or right. The Inspector's `Is Power Supplied` shows whether any tile is powered in Play mode.
- Select `Wire Palette` or `Signal Palette` in Unity's Tile Palette window and paint directional wires on a Tilemap beneath the level Grid. Existing painted directional wires also become movable in Play mode. A network is created automatically if the Grid has none.
- Unity MCP previously verified repeater placement and direction, matching and mismatched neighboring tiles, power removal, and blue/green sprites.

- Unity MCP verified an emitter supplying a wire and receiver, a mismatched input port, reconnection and source disablement in an isolated preview scene.

- Unity MCP verified runtime tile spawning, a push that disconnects signal, reconnection, a two-piece chain push, and delivery through a powered open port in an isolated play-mode scene.
