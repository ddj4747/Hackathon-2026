# Pixel-perfect rendering and font

## Status
- Configured, not yet verified in the editor or in Play mode (no visual check was possible). Tweak the values below after looking at the Game view.

## Entry files
- `Assets/Resources/nokiafc22.ttf` (+ `.meta`): the project font (Nokia Cellphone FC, native 8 px design).
- `Assets/Prefabs/LevelPlate.prefab`: level number `TextMesh` using the font.

## Behavior and rules
- Camera: `PixelPerfectCamera` was tried (PPU 16, 512 x 352, Pixel Snapping) and removed: the editor warned about the resolution and the player ended up far from the view centre. Cameras are back to plain orthographic size 5 (1 tile = 1 world unit). Pixel-perfect camera setup is undecided; revisit with the user.
- Font importer: Font Size 16, Rendering Mode Smooth (Unity defaults). Size 8 + Hinted Raster was tried first and the editor logged `Failed to load ... nokiafc22.ttf`; cause not confirmed, so it was reverted. Use only multiples of 8 for the font size (8, 16, ...); other sizes give fractional advances and blur.
- `TextMesh` rule: world units per font pixel = `characterSize * 0.1`. With font size 16 and `characterSize` 0.3125 one font pixel is 1/8 unit (a glyph pixel is 2 screen pixels; native 8 px design doubled) . Font Style must stay Normal (bold/italic are synthesized and break pixel alignment).
- All game text must use `nokiafc22.ttf` (assign `Resources/nokiafc22` font and its material, same size/characterSize values). At size 16 digits are 12 px wide ("1" is 8 px), so numbers of any length have an even pixel width and stay centred on the tile.

## Public interfaces and dependencies
- `LevelSelectPlate` only sets `TextMesh.text`; font settings live in the prefab ([LEVEL_SELECT.md](level-select.md)).
- Camera follow: [PLAYER_MOVEMENT.md](player-movement.md).

## Setup and verification
- Open `Menu` in Play mode and check that the number is crisp and vertically centred on the plate. If it sits half a pixel off, move the `Number` child by multiples of 1/16 unit.
- Undecided: how to make the view pixel perfect at 512 x 352 (Pixel Perfect Camera settings, crop/letterbox, or fixed orthographic size 11).
