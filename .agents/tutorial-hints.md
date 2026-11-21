# Tutorial hints

## Status
- Implemented. `Level-1` (tutorial scene) has a `Tutorial` object with three placeholder hints; the final texts are still to be supplied.

## Entry files
- `Assets/Scripts/TutorialHints.cs`
- `Assets/Scenes/Levels/Level-1.unity`, object `Tutorial`

## Behavior and rules
- Each hint is text written on the floor at `position` (grid cells, text centre; 0.5 steps reach cell borders) and stays there once it has appeared. Position and text are re-read every frame (by list index), so Inspector edits in Play mode move or change a shown hint at once; Unity discards Play-mode edits when Play mode ends. Unity MCP verified a Play-mode position edit moving the shown label. Line breaks come from Enter in the text; there is no automatic wrapping.
- Hints appear in list order. Only the next hint's trigger is checked, and only after the previous hint appeared; then `delay` runs. Several hints with met triggers appear in the same frame.
- Triggers: `Immediately` (first hint: level start), `PlayerEntersArea` (player standing in `area`, grid cells, x/y = bottom-left cell), `PlayerMoves`, `PlayerPushes`, `PlayerUndoes` (counted only after the previous hint appeared; from `PlayerGridMovement.StepStarted`), `CellSupplied` (a repeater, antenna or receiver on `cell` is supplied).
- Look: world-space `TextMesh` with `nokiafc22`, rasterised at font size 48 and scaled so letters are `size` game pixels high (default 6; 8 = the font's native pixel size), with a one-font-pixel dark shadow. Sorting orders 2 (shadow) and 3 (text): above the floor and signal overlay, below blocks (5) and the player (10). Text appears whole at once (`charactersPerSecond` 0; a value above 0 turns on a typewriter that keeps the centred layout fixed); fade in (`fadeTime`, 0.3 s). Rich text `<color>` tags work. Positions snap to the 1/16 world grid. The font reports all Polish diacritics.
- Scene view: each hint position is drawn with its number and text; selecting the object also draws `PlayerEntersArea` areas (yellow) and `CellSupplied` cells (green).

## Public interfaces and dependencies
- `Show(int index)`, `ShownCount`. Uses [Player movement](player-movement.md) (`StepStarted`, `IsUndoing`), `Repeater.IsSignalSupplied`, `SignalReceiver.IsSignalSupplied` and the scene `Grid`. Font rules: [Pixel-perfect rendering and font](pixel-perfect-and-font.md).

## Setup and verification
- Add `TutorialHints` to any object in a level and fill `Hints` in the Inspector.
- Unity MCP Play-mode checks in `Level-1` (manual stepping): hint 0 after its 0.5 s delay at its position, line height 7.5 game px at size 6, fade complete; hint 1 on the first push while hint 0 stays; an earlier version verified `CellSupplied` and the typewriter. `PlayerEntersArea`, `PlayerMoves`, `PlayerUndoes` and the on-screen look were not checked.
- Not indexed in root `AGENTS.md` (left unchanged on request).
