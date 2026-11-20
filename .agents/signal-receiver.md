# Signal receiver

## Status
- Four receiver orientations and palette tiles detect signal. The level-ending action is not yet selected or configured.

## Entry files
- `Assets/Scripts/SignalReceiver.cs`, `Assets/Prefabs/SignalReceiver1.prefab` through `SignalReceiver4.prefab`, `Assets/SignalTiles/SignalReceiver1.asset` through `SignalReceiver4.asset`.
- `Assets/Sprites/EmitterReceiver.png`, `Assets/TilePalettes/Signal Palette.prefab`.

## Behavior and rules
- While enabled, the receiver checks for positive floor signal at its cell or a powered wire with an open port facing its adjacent cell, and logs `Signal receiver connected.` and invokes its event on each transition from disconnected to connected. It resets its supplied state when disabled.
- Uses its nearest parent Grid, or the only Grid in its scene. With multiple grids, parent it under the intended Grid.

## Public interfaces and dependencies
- `IsSignalSupplied` reports its current sampled state; `OnSignalReceived` exposes the serialized UnityEvent. No listener is configured by default.
- Uses the scene-scoped [connection manager](connection-manager.md) and adjacent [wire networks](cables.md). Palette tiles instantiate their matching prefab in Play mode.

## Setup and verification
- Paint a blue receiver from `Signal Palette` on a Tilemap under the level Grid, or place its prefab beneath the Grid.
- Unity MCP verified palette tile references and receiver connection, sustained signal, disconnection and reconnection in an isolated preview scene.
- Unity MCP verified a powered repeater-to-wire-to-receiver path, a closed wire port, sustained signal, reconnection and source removal in an isolated preview scene.
