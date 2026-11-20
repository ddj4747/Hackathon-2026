# Repeater

## Status

- Repeaters contribute coverage to the scene's connection manager. Their supplied state is controlled by an Inspector toggle; cable-to-repeater supply is not implemented.
- Fifteen visual variants each use the top-row blue sprite when unsupplied and the matching bottom-row green sprite when supplied.

## Entry files

- `Assets/Scripts/Repeater.cs`, `Assets/Scripts/RepeaterVisual.cs`, `Assets/Prefabs/Repeater.prefab`
- `Assets/Resources/Repeaters.png`, `Assets/Prefabs/Repeaters/`, `Assets/RepeaterTiles/`
- `Assets/Scripts/RepeaterTile.cs`, `Assets/TilePalettes/Repeater Palette.prefab`, `Assets/Prefabs/RepeaterTilemap.prefab`
- [Connection manager](connection-manager.md), [Cables](cables.md), [Map grid](map-grid.md)

## Behavior and rules

- `Is Signal Supplied` defaults to true. When off, the repeater removes its signal contributions; when on, it supplies quality 1 to its own cell and configured sector. Changes take effect during Play mode.
- Range, sector angle, start angle, position and local +X facing determine the coverage described in the root project instructions. The start angle defaults to 0 degrees and the sector extends counterclockwise. Grid lookup uses the nearest parent Grid or the only Grid in the scene.
- In Play mode the visual follows `IsSignalSupplied`; blue is unsupplied and green is supplied. In edit mode tiles preview blue. A painted repeater tile instantiates its matching functional prefab.

## Public interfaces and dependencies

- Coverage uses the scene-scoped `ConnectionManager`. Sector angle, start angle and range are serialized on the repeater.
- `IsSignalSupplied` exposes whether the repeater is enabled and supplies signal; [wire tilemaps](cables.md) use it for adjacent open-port adapters. Wire tilemaps do not supply repeaters.

## Setup and verification

- Select `Repeater Palette` and paint on a Tilemap beneath the level Grid. `RepeaterTilemap` contains a sample row of all 15 variants; `SampleScene` has an instance at cells x=-7 through 7, y=-4.
- Standalone variants can also be placed under a Grid. Set `Is Signal Supplied` on a prefab to control its supply; tile-created instances can be inspected in Play mode.
- Unity CLI verified palette references, edit previews, all 15 instantiated prefabs and both sprite mappings, plus automatic changes when toggling supply in Play mode. Unity MCP verified sector boundaries, start-angle changes, wraparound, rotation, and zero- and full-width coverage.
