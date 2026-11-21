# Signal emitter

## Status

- A signal emitter is a starting source for the scene's connection manager.

## Entry files

- `Assets/Scripts/SignalEmitter.cs`, `Assets/Prefabs/SignalEmitter.prefab`, `Assets/Prefabs/RedSignalEmitter.prefab`
- `Assets/Scripts/PrefabTile.cs`, `Assets/SignalTiles/SignalEmitter.asset`, `Assets/TilePalettes/Signal Palette.prefab`
- [Repeater](repeater.md), [Connection manager](connection-manager.md)

## Behavior and rules

- While enabled, an emitter supplies its configured signal type at quality 1 to its own cell and a full circular range using the repeater's rounded radius rule. Disabling, moving, changing its range, or resetting signals updates coverage.
- Walls and marked objects obstruct its line of sight and the hover overlay reflects the resulting coverage. See [Connection manager](connection-manager.md).
- An enabled emitter can supply an adjacent [wire network](cables.md) through an open wire port facing its cell.
- Uses its nearest parent Grid, or the only Grid in its scene. With multiple grids, parent it under the intended Grid.
- Hovering an enabled emitter shows its own coverage through the [connection overlay](connection-manager.md).

## Public interfaces and dependencies

- Set `Signal Type` and `Range` on the component; defaults are Original and 3 cells. Red uses `Assets/Resources/RedSignal.shader` on the existing sprite. Coverage is contributed to the scene-scoped `ConnectionManager`.
- `PrefabTile` displays its sprite and tint in edit mode and creates its referenced prefab in Play mode. The emitter tile references the emitter prefab.

## Setup and verification

- Place an emitter prefab under the level Grid, or paint an Original or Red emitter from `Signal Palette` onto a Tilemap under the Grid. Four sprite orientations share the same circular signal behavior. Set the range on the prefab. Place `RedSignalEmitter` for a ready-configured Red source; painted tiles use the type of their referenced prefab; Red palette variants sit to the right of the originals.
- Unity MCP verified circular range, activation of a repeater chain, empty cells and shutdown when the source is removed. In Play mode, a painted emitter activated a neighboring repeater and removing its tile removed the signal; the edit-mode preview was also checked.

- Unity MCP preview-scene checks invoked signal updates and verified separate Original and Red sources, matching receivers, and jammer blocking and recovery for both types.

- `Assets/Scenes/SignalTypesTest.unity` contains both types. Unity MCP verified its tile references and signal paths with isolated update calls.
- Can be placed in a wall cell (as in Level0): `SignalObstacles` does not treat the dual-grid wall in an emitter's or receiver's own cell as blocking. Other walls, blocks and jammers still block. Unity MCP verified in a temporary scene: emitter and receiver on wall cells at both ends of a floor strip, repeater between; the receiver cell got signal and a plain wall cell beyond it stayed blocked.
