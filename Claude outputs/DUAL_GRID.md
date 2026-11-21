# Dual grid (floor, walls, water)

## Status
- Implemented; animation confirmed working in Play mode by the user. The no-merging / auto-detect version is not yet verified in the editor.
- One dual-grid layer (`MapGrid/Ground`) draws floor (dark), walls (light) and animated water. Everything else (player, blocks, level exits, plates, `Solid`, pushing, the `Terrain` tilemap used for placing antennas/blocks) stays on the normal `MapGrid` Grid.
- Rule from the user: never assemble one tile from pieces of others. A corner combination with no tile is left empty and reported.
- The tilesets cover all 81 combinations (checked against the art; no duplicates). If art changes, context menu `Log missing tiles` on `Ground` lists any gaps.
- The layer does not block movement; whether walls or water are solid is undecided.
- Once terrain is painted, logical wall cells (including unpainted cells) block wireless signal. A completely empty layer does not block signal; see [Connection manager](../.agents/connection-manager.md).

## Entry files
- `Assets/Scripts/DualGridLayer.cs`: the component (on the `Ground` Tilemap).
- `Assets/Scripts/DualGridTerrainTile.cs`: `DualGridTerrain` enum (Floor 0, Wall 1, Water 2) and the paintable terrain tile (menu `Create > 2D > Tiles > Dual Grid Terrain Tile`).
- `Assets/Scripts/DualGridAnimatedTile.cs`: runtime-only display tile (static or animated).
- `Assets/Tiles/DualGridFloor.asset`, `DualGridWater.asset`, `DualGridWall.asset`: the tiles to paint with.
- `Assets/Sprites/Tileset.png` (floor + wall, 4 x 4, 1 frame), `TilesetWater.png` (floor + water, 4 x 4, 4 frames), `TilesetAll.png` (floor + wall + water, 8 x 4 per frame, 4 frames), `TilesetWaterWall.png` (wall + water and a few 3-terrain tiles, 5 x 4, 1 frame): 16 px tiles, 16 PPU, Point filter, no compression, Read/Write on. The importers' own sprite slicing is not used by the dual grid (only the paint tiles' previews use `Tileset_12`, `Tileset_6`, `TilesetWater_18`).
- Empty leftovers that can be deleted: `Assets/Editor/DualGridSetup.cs`, `Assets/Scripts/DualGridQuarterTile.cs`.

## Behavior and rules
- `Ground`'s Tilemap is the logical layer: `DualGridTerrainTile` = its terrain, any other tile = Floor. Empty cell = nothing, except an empty cell within `Margin` cells (default 3, square / Chebyshev distance, so also diagonally) of floor or water, which is an automatic wall. Walls can also be painted by hand (kept until erased). Auto walls are recomputed after every edit, so erasing floor/water removes the walls it created. In the editor the painted cells (not auto walls) are drawn as a faint guide (`Show Guide In Editor`) one sorting order BELOW the art, so it only shows where no tile is drawn; never in Play mode. (Drawn above, its 40 % sprites tinted tile edges with colours not in the tileset.)
- Corners: a void cell counts as wall in the corner code, so wall cells are drawn whole (half a tile of plain wall shows past the outer wall cells); a corner whose four cells are all void gets no tile. Erasing floor inside a floor area makes that cell an auto wall (it still touches floor).
- Display: hidden, unsaved child Tilemap `~DualGridDisplay` shifted by half a cell. Corner (x, y) looks at cells (x-1,y), (x,y), (x-1,y-1), (x,y-1) and gets the one tile whose corners match.
- Tile recognition: for every tile slot in frame 0 of each sheet in `Tilesets` (in list order), the four corner pixels are matched to the nearest of `Floor/Wall/Water Color`. Transparent corner = empty slot. First tile per combination wins; repeats inside one sheet are logged. Columns per frame = width / frames / tileSize, rows = height / tileSize, so rows/columns can be added freely.
- Missing combinations: corner left empty, pink crossed square in the Scene view, warning in the console after each rebuild listing the combinations the level needs (`F/W/A`, written `TL TR / BL BR`).
- Animation: frame 0 (no outline) repeated `Still Frame Hold` times (default 4), then frames 1..n-1; `Frame Duration` 0.15 s. Tilemap tile animation; the Scene view may show frame 0 only.
- Painting, erasing and undo update the display automatically (each change redraws the corners within `Margin` + 1 cells of it); sorting order -10. `Clear walls (keep floor and water)` (component context menu, undoable) removes hand-painted Wall cells; the auto wall stays.

## Public interfaces and dependencies
- `DualGridLayer.GetTerrain(cell)` (auto walls and void report Wall; `SignalObstacles` uses this so they block wireless signal), `IsVoid(cell)`, `IsAutoWall(cell)`, `SetTerrain(cell, terrain)` (uses `Floor Tile` / `Water Tile` / `Wall Tile`), `ClearCell(cell)`, `ClearWalls()`, `RefreshAll()`, `Rebuild()`, `LogMissingTiles()`, `Data`.
- Needs a parent `Grid` (`MapGrid`); one tile = one parent cell. Takes the material from the `TilemapRenderer` on the same object.

## Setup and verification
- Auto-wall rules verified via Unity MCP in a temporary scene with the `MapGrid` configuration (see the latest check below for the current margin): empty map draws nothing; floor gets an auto-wall ring; water moves the walls out and erasing it moves them back; a lone hand-painted wall draws and erasing it leaves nothing; erasing all floor leaves nothing; `RefreshAll` gives the same result. Not checked visually in the Scene view.
- `MapGrid.prefab` has the four tilesets (frames 1, 4, 4, 1) and the paint tiles (floor, water, wall) assigned on `Ground`. Re-slicing a sheet in the Sprite Editor resets Read/Write; turn it back on.
- Drag the three `Assets/Tiles/DualGrid*.asset` tiles into a Tile Palette, set Active Tilemap = `Ground`, paint. Never paint floor/water on `Terrain`.
- New tiles: add them anywhere in an existing sheet (keep frames side by side, equal width) or add a new sheet to `Tilesets`; then `Rebuild dual grid` from the component's context menu.
- Unresolved: whether walls/water block movement.
