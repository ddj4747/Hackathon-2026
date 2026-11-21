# Pixel-perfect rendering and font

## Status
- Configured, not yet verified in the editor or in Play mode (no visual check was possible). Tweak the values below after looking at the Game view.
- Fixed 352 x 256 camera implemented (files edited outside the editor; check the console after import).

## Entry files
- `Assets/Scripts/FixedCamera.cs`: on the `Main Camera` of `Menu` and `SampleScene`.
- `Assets/Resources/nokiafc22.ttf` (+ `.meta`): the project font (Nokia Cellphone FC, native 8 px design).
- `Assets/Prefabs/LevelPlate.prefab`: level number `TextMesh` using the font.

## Behavior and rules
- Camera: fixed, never follows the player. `FixedCamera` sets orthographic size `referenceHeight / (2 * PPU)` = 128 / 16 = **8** and places the camera at `center` (default (0, 0), z unchanged), so the view is exactly 352 x 256 px = 22 x 16 tiles (x -11..11, y -8..8 with the default centre; edges fall on cell borders). Both scenes use it with these values, and the cameras are serialized with orthographic size 8.
- Other window aspect ratios: the viewport is letterboxed/pillarboxed to 352:256 (11:8) and a child camera `LetterboxBars` (created at runtime, solid `barColor`, default the wall colour #333C57 = `DualGridLayer.wallColor` in `MapGrid.prefab` and the wall pixels of `Tileset.png`; if the wall colour changes, update `Bar Color` on both cameras) fills the bars, so the picture is never stretched or cropped. No integer-scale snapping: at a window size that is not a multiple of 352 x 256 pixels are unevenly sized. `PixelPerfectCamera` was tried earlier (PPU 16, 512 x 352) and removed; it is not used.
- Font importer: Font Size 16, Rendering Mode Smooth (Unity defaults). Size 8 + Hinted Raster was tried first and the editor logged `Failed to load ... nokiafc22.ttf`; cause not confirmed, so it was reverted. Use only multiples of 8 for the font size (8, 16, ...); other sizes give fractional advances and blur.
- `TextMesh` rule: world units per font pixel = `characterSize * 0.1`. With font size 16 and `characterSize` 0.3125 one font pixel is 1/8 unit (a glyph pixel is 2 screen pixels; native 8 px design doubled) . Font Style must stay Normal (bold/italic are synthesized and break pixel alignment).
- All game text must use `nokiafc22.ttf` (assign `Resources/nokiafc22` font and its material, same size/characterSize values). At size 16 digits are 12 px wide ("1" is 8 px), so numbers of any length have an even pixel width and stay centred on the tile.

## Public interfaces and dependencies
- `LevelSelectPlate` only sets `TextMesh.text`; font settings live in the prefab ([LEVEL_SELECT.md](level-select.md)).
- Camera follow: [PLAYER_MOVEMENT.md](player-movement.md).

## Setup and verification
- Open `Menu` in Play mode and check that the number is crisp and vertically centred on the plate. If it sits half a pixel off, move the `Number` child by multiples of 1/16 unit.
- To frame a level differently change `Center` on the `FixedCamera` component (use whole numbers so cell borders stay on pixel borders). Undecided: integer-scale snapping of the window.
