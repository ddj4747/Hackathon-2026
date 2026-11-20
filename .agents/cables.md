# Cables

## Status

- `WireTilemap` is a single reusable Tilemap object with directional, signal-aware wire tiles. The older path-based cable prefab remains available separately.

## Entry files

- `Assets/Scripts/SignalCable.cs`, `Assets/Scripts/CableAdapter.cs`
- `Assets/Prefabs/SignalCable.prefab`, `Assets/Resources/Wires.png`, `Assets/WireTiles/`
- `Assets/Scripts/WireTile.cs`, `Assets/DirectionalWireTiles/` contain 11 directional tile assets with blue and green sprites.
- `Assets/Scripts/WireNetwork.cs`, `Assets/Prefabs/WireTilemap.prefab` implement and demonstrate the tilemap network.
- `Assets/TilePalettes/Wire Palette.prefab` is a Unity Tile Palette containing all 11 directional wire tiles.
- [Connection manager](connection-manager.md), [Map grid](map-grid.md)

## Behavior and rules

- A valid path contains at least two distinct endpoints and uses orthogonally adjacent cells. Intermediate cells define the route but do not receive signal from the cable.
- The endpoint cells connect bidirectionally at unchanged quality when each contains an active adapter. Links are removed when the cable is disabled or the path or endpoints are invalid.
- An adapter's optional `Test Signal` toggle contributes quality 1 at its cell during Play mode; switching it off, disabling it, moving it, or resetting signals updates the contribution.
- The cable resolves an explicitly assigned Grid or its nearest parent Grid in the same scene. Coordinates are scene-grid cell coordinates. Adapters are found by their world position in that grid.
- Directional wire tiles record up, right, down and left ports. Neighboring tiles connect only when both ports face their shared edge, including rotated/flipped tile transforms.
- An unoccupied neighboring wire port acts as an adapter when an enabled, supplied repeater occupies that adjacent cell. Signal reaches every tile in that connected component; green shows supplied, blue shows unsupplied. Changes are recalculated during Play mode. A wire in the adjacent cell prevents that port being an adapter, even if its direction does not match.
- Tilemap wire power is separate from `ConnectionManager` floor signals and the older path-based cable links; output from the tilemap to other objects is not implemented.

## Public interfaces and dependencies

- `SignalCable` serializes the Grid, ordered path and optional LineRenderer. `CableAdapter` marks an endpoint; the scene-scoped `ConnectionManager` owns signal propagation.
- `WireTile.AreConnected` checks untransformed neighboring tiles. `WireNetwork.IsPowerSupplied` reports whether any tile is powered; `IsPowered(cell)` reports individual tiles. `Recalculate()` updates the network immediately.
- The existing manager keys signals by cell coordinate rather than Grid identity; levels using cables should use one signal Grid per scene.

## Setup and verification

- Parent the prefab under the level Grid. Set the path to the intended absolute Grid cells and move its adapter children into the first and last cells. The default path connects (0,0) and (1,0) when the prefab is at the Grid origin.
- Paint the desired 16-pixel `WireTiles` on the prefab's `Cable artwork` Tilemap to match the route. The tilemap is initially empty; the path and artwork are configured separately. Sprites use point filtering and 16 pixels per unit.
- `SampleScene` contains a `WireTilemap` under `MapGrid`, with four horizontal tiles adjacent to the root repeater. In Play mode that repeater's `Is Signal Supplied` toggle controls their power.
- Unity CLI verified compilation, strongest-signal propagation in both directions, source removal in a loop, link removal and prefab adapter enable/disable behavior. Visual placement remains a level-authoring step.
- Unity CLI also verified propagation through the three example cables and removal when the test toggle is turned off.
- Parent `WireTilemap` beneath the scene Grid and paint `DirectionalWireTiles` on it. The prefab contains four horizontal sample tiles; place a supplied repeater on the adjacent cell to the left or right. The Inspector's `Is Power Supplied` shows whether any tile is powered in Play mode.
- Select `Wire Palette` in Unity's Tile Palette window and paint onto the `WireTilemap` object. Unity recognizes the palette and its 11 distinct directional tile assets.
- Unity MCP verified repeater placement and direction, matching and mismatched neighboring tiles, power removal, blue/green sprites, and the sample scene's repeater-to-wire supply.
