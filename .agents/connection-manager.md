# Connection manager

## Status

- A loaded scene gets its own manager on first access. Future scenes need no component setup.

## Entry files

- `Assets/Scripts/ConnectionManager.cs`, `Assets/Scripts/ConnectionOverlay.cs`, `Assets/Prefabs/ConnectionOverlay.prefab`
- [Map grid](map-grid.md)

## Behavior and rules

- Each cell stores a list of finite signal qualities. Its quality is the largest value, or `0` for an empty cell.
- Adding the same value twice creates two contributions. Removal matches one stored value exactly and reports whether it was found.
- Cable-linked cells share the strongest directly contributed signal throughout their connected component. Links do not themselves add contributions, so loops cannot sustain a signal after its source is removed. See [Cables](cables.md).
- Signal sources activate repeaters whose cells they reach. Active repeaters extend signal to their coverage; activation is recomputed from sources so repeater cycles cannot sustain themselves. See [Repeater](repeater.md) and [Signal emitter](signal-emitter.md).
- Unloading a scene clears and drops its manager. Starting a new play session starts with fresh managers.
- The overlay shows only the hovered enabled emitter or repeater's own range. An unsupplied repeater shows its potential coverage without transmitting signal. Nothing is shown off an object; the overlay does not affect signal propagation.

## Public interfaces and dependencies

- `ConnectionManager.Instance` resolves the active scene. Use `ForScene(gameObject.scene)` for an object that can live in an additively loaded scene.
- `AddSignal(cell, quality)`, `RemoveSignal(cell, quality)` and `GetSignalQuality(cell)` accept Unity `Vector3Int` cell coordinates. `GetSignalQuality(worldPosition, grid)` converts through the supplied grid.
- `HasSignal(cell)` and `HasSignal(worldPosition, grid)` report whether the cell's strongest signal quality is positive, including signals received over cable links. Cells without a contribution have no signal.
- `ConnectionManager.ResetState()` clears contributions from all managers while keeping their instances.

## Setup and verification

- Verified maximum selection, duplicate removal, empty cells, world-to-cell lookup, scene isolation and global reset in Unity.
- Verified cable propagation, competing sources, disconnection and source removal in a loop with Unity CLI.
- Parent `ConnectionOverlay.prefab` under the level Grid. Hover uses the main camera, the Input System mouse and each signal object's sprite bounds. An isolated Unity preview-scene check verified hover selection, per-source coverage, clearing, loss of supply and unchanged signal propagation.
