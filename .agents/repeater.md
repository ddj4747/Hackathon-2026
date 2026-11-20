# Repeater

## Status

- Repeaters contribute coverage when signal reaches their cell. A [signal emitter](signal-emitter.md) can provide the starting signal.
- Fifteen visual variants each use the top-row blue sprite when unsupplied and the matching bottom-row green sprite when supplied.

## Entry files

- `Assets/Scripts/Repeater.cs`, `Assets/Scripts/RepeaterVisual.cs`, `Assets/Prefabs/Repeater.prefab`
- `Assets/Resources/Repeaters.png`, `Assets/Prefabs/Repeaters/`, `Assets/RepeaterTiles/`
- `Assets/Scripts/RepeaterTile.cs`, `Assets/TilePalettes/Signal Palette.prefab`, `Assets/Prefabs/RepeaterTilemap.prefab`
- [Connection manager](connection-manager.md), [Cables](cables.md), [Map grid](map-grid.md)

## Behavior and rules

- A repeater supplied by a positive signal contributes quality 1 to its own cell and configured sector. It stops contributing if its source is lost. Cycles cannot supply themselves without an external source.
- Range, sector angle, start angle, position and local +X facing determine the coverage described in the root project instructions. The start angle defaults to 0 degrees and the sector extends counterclockwise. Grid lookup uses the nearest parent Grid or the only Grid in the scene.
- In Play mode the visual follows `IsSignalSupplied`; blue is unsupplied and green is supplied. In edit mode tiles preview blue. A painted repeater tile instantiates its matching functional prefab.
- The player can push standalone and painted repeaters using the existing push and chain rules. Coverage and supply are recalculated from the repeater's moving position. See [Pushable blocks](pushable-blocks.md).

## Public interfaces and dependencies

- Coverage uses the scene-scoped `ConnectionManager`. Sector angle, start angle and range are serialized on the repeater.
- `IsSignalSupplied` exposes whether the repeater is enabled and receives signal; [wire tilemaps](cables.md) use it for adjacent open-port adapters. Wire tilemaps do not supply repeaters.

## Setup and verification

- Select `Signal Palette` and paint on a Tilemap beneath the level Grid. It contains 15 repeater variants, four emitter orientations and four receiver orientations. `RepeaterTilemap` contains a sample row of all 15 variants; `SampleScene` contains a modified instance with six repeater tiles.
- Standalone variants can also be placed under a Grid. Place a signal emitter so that its coverage reaches a repeater's cell; tile-created instances can be inspected in Play mode.
- Unity CLI previously verified palette references, edit previews, all 15 instantiated prefabs and both sprite mappings. Unity MCP verified sector boundaries, start-angle changes, wraparound, rotation, and zero- and full-width coverage. Unity MCP also verified signal activation, chaining and shutdown without a source.

- Unity verified that all 15 variants inherit Pushable and checked a painted repeater in Play mode: the player pushed it out of supply, removing its coverage, and pushing it back restored coverage.
