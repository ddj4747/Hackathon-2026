# Signal emitter

## Status

- A signal emitter is a starting source for the scene's connection manager.

## Entry files

- `Assets/Scripts/SignalEmitter.cs`, `Assets/Prefabs/SignalEmitter.prefab`
- `Assets/Scripts/PrefabTile.cs`, `Assets/SignalTiles/SignalEmitter.asset`, `Assets/TilePalettes/Signal Palette.prefab`
- [Repeater](repeater.md), [Connection manager](connection-manager.md)

## Behavior and rules

- While enabled, an emitter supplies quality 1 to its own cell and a full circular range using the repeater's rounded radius rule. Disabling, moving, changing its range, or resetting signals updates coverage.
- An enabled emitter can supply an adjacent [wire network](cables.md) through an open wire port facing its cell.
- Uses its nearest parent Grid, or the only Grid in its scene. With multiple grids, parent it under the intended Grid.

## Public interfaces and dependencies

- Set `Range` on the component; the default is 3 cells. Coverage is contributed to the scene-scoped `ConnectionManager`.
- `PrefabTile` displays its sprite and tint in edit mode and creates its referenced prefab in Play mode. The emitter tile references the emitter prefab.

## Setup and verification

- Place an emitter prefab under the level Grid, or paint a green emitter from `Signal Palette` onto a Tilemap under the Grid. Four sprite orientations share the same circular signal behavior. Set the range on the prefab.
- Unity MCP verified circular range, activation of a repeater chain, empty cells and shutdown when the source is removed. In Play mode, a painted emitter activated a neighboring repeater and removing its tile removed the signal; the edit-mode preview was also checked.
