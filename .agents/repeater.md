# Repeater

## Status

- Repeaters contribute coverage when signal reaches their cell. A [signal emitter](signal-emitter.md) can provide the starting signal.
- Fifteen visual variants each use the top-row blue sprite when unsupplied and the matching bottom-row green sprite when supplied.

## Entry files

- `Assets/Scripts/Repeater.cs`, `Assets/Scripts/RepeaterVisual.cs`, `Assets/Prefabs/Repeater.prefab`
- `Assets/Resources/Repeaters.png`, `Assets/Prefabs/Repeaters/`, `Assets/Prefabs/RedSignalVariants/`, `Assets/RepeaterTiles/` and `Assets/RepeaterTiles/Red/`
- `Assets/Scripts/RepeaterTile.cs`, `Assets/TilePalettes/Signal Palette.prefab`, `Assets/Prefabs/RepeaterTilemap.prefab`
- [Connection manager](connection-manager.md), [Cables](cables.md), [Map grid](map-grid.md)

## Behavior and rules

- A repeater supplied by a positive signal of its configured type contributes quality 1 to its own cell and configured sector or line. It stops contributing if its source is lost. Cycles cannot supply themselves without an external source.
- Walls and marked objects obstruct its line of sight; the hover overlay shows the unobstructed potential coverage. See [Connection manager](connection-manager.md).
- Range, sector angle, start angle, position and local +X facing determine the coverage described in the root project instructions. The start angle defaults to 0 degrees and the sector extends counterclockwise. Grid lookup uses the nearest parent Grid or the only Grid in the scene.
- Variants `Repeater_04` through `Repeater_15` use a one-cell-wide line along the center of their former 90-degree sector. The line extends Range cells, rotates with the prefab, and is extended by adjacent enhancers. Other repeater prefabs retain sector coverage.
- In Play mode the visual follows `IsSignalSupplied`; blue is unsupplied and green is supplied. In edit mode Original tiles preview blue and Red tiles use a red tint. A painted repeater tile instantiates its matching functional prefab.
- The player can push standalone and painted repeaters using the existing push and chain rules. Coverage and supply are recalculated from the repeater's moving position. See [Pushable blocks](pushable-blocks.md).
- Adjacent pushable [enhancers](repeater-enhancer.md) increase the repeater range by one each; diagonal cells do not count.
- Hovering a repeater shows its potential coverage through the [connection overlay](connection-manager.md), even while it is unsupplied.

## Public interfaces and dependencies

- Coverage uses the scene-scoped `ConnectionManager`. Signal type, sector angle, start angle, range and the straight-line option are serialized on the repeater. Original is the default. The Red type uses `Assets/Resources/RedSignal.shader` on its sprite.
- `IsSignalSupplied` exposes whether the repeater is enabled and receives signal; [wire tilemaps](cables.md) use it for adjacent open-port adapters. Wire tilemaps do not supply repeaters.

## Setup and verification

- Select `Signal Palette` and paint on a Tilemap beneath the level Grid. It contains 15 repeater variants, four emitter orientations, four receiver orientations and four collectors for each signal type, plus wires and other signal tiles. Red variants are to the right of the originals. `RepeaterTilemap` contains a sample row of all 15 variants; `SampleScene` contains a modified instance with six repeater tiles.
- Standalone variants can also be placed under a Grid and configured as Original or Red per instance. Painted repeater tiles use the type configured on their referenced prefab. Place a signal emitter so that its coverage reaches a repeater's cell; tile-created instances can be inspected in Play mode.
- Unity CLI previously verified palette references, edit previews, all 15 instantiated prefabs and both sprite mappings. Unity MCP verified sector boundaries, start-angle changes, wraparound, rotation, and zero- and full-width coverage. Unity MCP also verified signal activation, chaining and shutdown without a source.

- Unity verified that all 15 variants inherit Pushable and checked a painted repeater in Play mode: the player pushed it out of supply, removing its coverage, and pushing it back restored coverage.
- Unity Play-mode checks in an isolated scene verified the unsupplied range preview and all 15 painted variants switching sprites when signal was supplied and removed.
- An isolated Unity preview-scene check verified all 12 line prefabs, their directions and ranges, activation and shutdown, rotation and enhancer extension; it also verified that circular coverage is unchanged.

- Unity MCP preview-scene checks verified independent supply and removal for overlapping Original and Red repeaters.

- Unity MCP verified all Red repeater tile references and the matching and mismatched repeaters in `Assets/Scenes/SignalTypesTest.unity` through isolated update calls.
