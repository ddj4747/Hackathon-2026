# Cables

## Status

- `WireTilemap` uses directional, signal-aware wire tiles that become pushable pieces in Play mode. The older path-based cable prefab remains available separately.

## Entry files

- `Assets/Scripts/SignalCable.cs`, `Assets/Scripts/CableAdapter.cs`
- `Assets/Prefabs/SignalCable.prefab`, `Assets/Resources/Wires.png`, `Assets/Resources/RedWires.png`, `Assets/Resources/WiresGround.png`, `Assets/Resources/RedWiresGround.png`, `Assets/WireTiles/`
- `Assets/Scripts/WireTile.cs`, `Assets/DirectionalWireTiles/` and `Assets/DirectionalWireTiles/Red/` contain the 11 directional tiles in both signal types.
- `Assets/Scripts/WireNetwork.cs`, `Assets/Scripts/MovableWire.cs`, `Assets/Prefabs/MovableWire.prefab` and `Assets/Prefabs/WireTilemap.prefab` implement and demonstrate the movable tilemap network.
- `Assets/TilePalettes/Wire Palette.prefab` and `Assets/TilePalettes/Signal Palette.prefab` include all 11 directional wire tiles in both types.
- [Connection manager](connection-manager.md), [Map grid](map-grid.md)

## Behavior and rules

- A valid path contains at least two distinct endpoints and uses orthogonally adjacent cells. Intermediate cells define the route but do not receive signal from the cable.
- The endpoint cells connect bidirectionally at unchanged quality for the `SignalCable`?s configured signal type when each contains an active adapter. Links are removed when the cable is disabled or the path or endpoints are invalid.
- An adapter's optional `Test Signal` toggle contributes quality 1 at its cell during Play mode; switching it off, disabling it, moving it, or resetting signals updates the contribution.
- The cable resolves an explicitly assigned Grid or its nearest parent Grid in the same scene. Coordinates are scene-grid cell coordinates. Adapters are found by their world position in that grid.
- Directional wire tiles record up, right, down and left ports. Neighboring pieces connect only when their types match and both ports face their shared edge, including rotated/flipped tile transforms. In Play mode each tile creates a pushable piece; movement follows the existing chain-pushing rules. Signal uses the destination cell during a push and updates when the layout changes. See [Pushable blocks](pushable-blocks.md).
- An unoccupied neighboring wire port acts as an adapter when a matching-type enabled emitter or an enabled, supplied repeater occupies that adjacent cell on the same Grid. Signal reaches every tile in that connected component; green shows supplied, blue shows unsupplied. Changes are recalculated during Play mode. A wire of either type in the adjacent cell prevents that port being an adapter, even if its direction does not match.
- Tilemap wire power is separate from `ConnectionManager` floor signals and the older path-based cable links. An enabled receiver or collector in an adjacent cell is supplied by a matching-type powered open wire port facing it. A repeater next to an open port is supplied too (Play mode): `ConnectionManager` includes registered `WireNetwork`s in its repeater recalculation, starting only from emitters (wires touching an emitter, then wires touching each newly supplied repeater), so a wire and a repeater cannot keep each other powered. `WireNetwork` registers itself with its scene manager and calls `WireLayoutChanged()` when its wire cells, ports, types or adjacent emitters change. Unity MCP verified in Level0 Play mode (manual stepping): emitter, repeaters, antenna, wire, then the repeater at the wire end supplied; emitter off turns all of them off; moving that repeater off the wire end and back removes and restores its supply.

## Public interfaces and dependencies

- `SignalCable` serializes its signal type, Grid, ordered path and optional LineRenderer; `CableAdapter` has a type for its optional test signal. `CableAdapter` marks an endpoint; the scene-scoped `ConnectionManager` owns signal propagation.
- `WireTile.AreConnected` checks untransformed neighboring tiles. `WireNetwork.IsPowerSupplied` reports whether any tile is powered; `IsPowered(cell)` reports individual tiles. `SuppliesAdjacent(worldPosition, grid)` tests a powered open port facing a neighboring position on the same Grid. `Recalculate()` updates the network immediately.
- The existing manager keys signals by cell coordinate rather than Grid identity; levels using cables should use one signal Grid per scene.

## Setup and verification

- Parent the prefab under the level Grid. Set the path to the intended absolute Grid cells and move its adapter children into the first and last cells. The default path connects (0,0) and (1,0) when the prefab is at the Grid origin.
- Paint the desired 16-pixel `WireTiles` on the prefab's `Cable artwork` Tilemap to match the route. The tilemap is initially empty; the path and artwork are configured separately. Sprites use point filtering and 16 pixels per unit.
- `SampleScene` contains a `WireTilemap` under `MapGrid` with a modified sample layout.
- Unity CLI verified compilation, strongest-signal propagation in both directions, source removal in a loop, link removal and prefab adapter enable/disable behavior. Visual placement remains a level-authoring step.
- Unity CLI also verified propagation through the three example cables and removal when the test toggle is turned off.
- Parent `WireTilemap` beneath the scene Grid and paint `DirectionalWireTiles` on it. Original and Red wire tiles can be painted on the same tilemap; their tile asset determines their type. For compatibility, setting `Signal Type` to Red on `WireNetwork` also treats original wire tiles on that map as Red. A tilemap without `WireNetwork` treats original tiles as Original. Red wire previews and movable sprites use matching sprites from `Assets/Resources/RedWires.png`. The Red artwork preserves the Original background and replaces `#257179` with `#5D275D`, `#38B764` with `#B13E53`, and `#A7F070` with `#EF7D57`. It was regenerated from the current `Wires.png` (dark grid lines and corner dots in the background), so only those three colors differ. `Assets/Resources/RedWiresGround.png` applies the same recolor to `WiresGround.png` (transparent ground cables with an outline, sprites named `GroundWire_*` like the original); nothing references it yet, as the level-select cables use the green `WiresGround.png`. Both sheets were generated by a script and not viewed in Unity. The prefab contains four horizontal sample tiles; place an emitter or supplied repeater on the adjacent cell to the left or right. The Inspector's `Is Power Supplied` shows whether any tile is powered in Play mode.
- Select `Wire Palette` or `Signal Palette` in Unity's Tile Palette window and paint directional wires on a Tilemap beneath the level Grid. Existing painted directional wires also become movable in Play mode. A network is created automatically if the Grid has none.
- Unity MCP previously verified repeater placement and direction, matching and mismatched neighboring tiles, power removal, and blue/green sprites.

- Unity MCP verified an emitter supplying a wire and receiver, a mismatched input port, reconnection and source disablement in an isolated preview scene.

- Unity MCP verified runtime tile spawning, a push that disconnects signal, reconnection, a two-piece chain push, and delivery through a powered open port in an isolated play-mode scene.

- Unity MCP preview-scene checks invoked the movable-network calculation to verify a Red source, mismatched wire isolation, matching wire propagation and source removal.

- Unity MCP verified both palettes and the saved `Assets/Scenes/SignalTypesTest.unity` by instantiating its tiles in an isolated scene and invoking signal and wire updates. Matching paths powered their receivers; a mismatched wire broke only the Red path. A separate Unity 6000.5.0f1 Play-mode run also verified painted wire instantiation, both powered receivers, wrong-type interruption, reconnection and source removal.
