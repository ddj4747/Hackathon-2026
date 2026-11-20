# GameObject tile palette and grid snapping

## Status
- `GridSnap`, placeholder `Floor`/`Wall` prefabs and the palette creator are implemented. Not yet verified in the editor.
- `Assets/Palettes/GameObjectTiles.prefab` is included; the creator can regenerate it.
- Floor and Wall are visual placeholders only: no collider, no blocking. Collision rules are still undecided. The real tileset is planned as a dual-grid Tilemap, so these are temporary.

## Entry files
- `Assets/Scripts/GridSnap.cs`: `[ExecuteAlways]` snap-to-cell component
- `Assets/Editor/GameObjectPaletteCreator.cs`: menu `Tools > Grid > Create GameObject Palette`
- `Assets/Prefabs/Tiles/Floor.prefab` (sorting -10), `Assets/Prefabs/Tiles/Wall.prefab` (sorting 0), sprites in `Assets/Sprites/`

## Behavior and rules
- `GridSnap` moves its object to the centre of the nearest cell of the scene's `Grid` whenever it is moved in the editor, and once when enabled in play mode. It is intended for placeable prefabs; inspect the prefab before relying on automatic snapping.
- The creator builds `Assets/Palettes/GameObjectTiles.prefab` (rectangular, cell size 1) containing Floor, Wall, LevelPlate, LevelExit and Player, via `GridPaletteUtility.CreateNewPalette`.
- To paint: Window > 2D > Tile Palette, pick `GameObjectTiles`, choose the target (the `Terrain` child of `MapGrid`, or Paint on Scene Root), select an element, paint. Each painted prefab lands on a cell centre.
- Add a new placeable prefab: put `GridSnap` on it, add its path to `PrefabPaths` in the creator and run the menu item again (it offers to replace the palette).

## Public interfaces and dependencies
- Needs `MapGrid` in the scene (one unit per cell with a half-cell centre offset at the grid origin). Uses 2D Tilemap Editor / Extras.

## Setup and verification
- Select the palette and paint in a scene with `MapGrid`; check its behavior in the editor.
