# Signal receiver

## Status
- Four receiver orientations and four collector orientations detect signal. Their events have no listeners configured by default.

## Entry files
- `Assets/Scripts/SignalReceiver.cs`, `Assets/Prefabs/RedSignalReceiver.prefab`, `Assets/Prefabs/SignalCollector*.prefab`, `Assets/SignalTiles/SignalCollector*.asset`, `Assets/Prefabs/SignalReceiver1.prefab` through `SignalReceiver4.prefab`, `Assets/SignalTiles/SignalReceiver1.asset` through `SignalReceiver4.asset`.
- `Assets/Sprites/EmitterReceiver.png`, `Assets/TilePalettes/Signal Palette.prefab`.

## Behavior and rules
- While enabled, it checks for positive matching-type floor signal at its cell or a matching-type powered wire with an open port facing its adjacent cell. It invokes the received or lost event once on each corresponding transition; it does not report loss while either input remains supplied. It logs `Signal receiver connected.` on acquisition and `Signal receiver disconnected.` on loss. Disabling resets its state without invoking the loss event.
- Uses its nearest parent Grid, or the only Grid in its scene. With multiple grids, parent it under the intended Grid.

## Public interfaces and dependencies
- `Signal Type` chooses Original (default) or Red; Red uses `Assets/Resources/RedSignal.shader` on the existing sprite. `IsSignalSupplied` reports its current sampled state; `OnSignalReceived` and `OnSignalLost` expose the serialized UnityEvents. No listeners are configured by default.
- Uses the scene-scoped [connection manager](connection-manager.md) and adjacent [wire networks](cables.md). Palette tiles instantiate their matching prefab in Play mode.

## Setup and verification
- Place `RedSignalReceiver` for a ready-configured Red receiver. Paint an Original or Red receiver or collector from `Signal Palette` on a regular Tilemap such as `MapGrid/Terrain`, not the dual-grid `Ground` terrain layer, or place a receiver or collector prefab beneath the Grid. A painted tile spawns its prefab in Play mode, so use a directly placed prefab instance for per-instance Inspector event bindings to scene objects.
- Unity MCP verified palette tile references and receiver connection, sustained signal, disconnection and reconnection in an isolated preview scene.
- Unity MCP verified a powered repeater-to-wire-to-receiver path, a closed wire port, sustained signal, reconnection and source removal in an isolated preview scene.
- Unity CLI verified the collector's event transitions for manager signal, powered wire, overlapping inputs, reconnection and a closed wire port in an isolated preview scene.
- Unity MCP verified the collector palette tile's prefab and sprite references, and one event and console log for each acquisition and loss in an isolated preview scene.
- Unity CLI verified all four collector palette tiles reference their matching prefabs and sprites.

- Unity MCP preview-scene checks invoked receiver updates and verified matching-type reception, isolation and recovery after a jammer is removed.

- `Assets/Scenes/SignalTypesTest.unity` contains both types; Unity MCP verified its receiver paths with isolated update calls.
