# Map grid

## Status

- `MapGrid` is a reusable rectangular Unity Grid with an empty `Terrain` Tilemap.

## Entry files

- `Assets/Prefabs/MapGrid.prefab`
- `Assets/Scenes/SampleScene.unity` contains a linked instance and a separate sample `RepeaterTilemap` child; see [Repeater](repeater.md).

## Behavior and rules

- Each cell spans one Unity unit along X and Y. Import 16 × 16 tile sprites at 16 pixels per unit to fit one cell.
- The level determines the map dimensions. Dual-grid tile mapping is pending tileset rules.

## Public interfaces and dependencies

- Unity `Grid` provides cell coordinates. The `Terrain` child uses `Tilemap` and `TilemapRenderer`.

## Setup and verification

- Add the prefab to a level and target `Terrain` in the Tile Palette.
- Verified the prefab link, cell dimensions, tile placement, and world-to-cell conversion through Unity CLI.
