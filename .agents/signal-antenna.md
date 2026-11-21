# Signal antenna

## Status
- Implemented as a [repeater](repeater.md) variant with placeholder art. Collects wireless signal and hands it to an adjacent wire; transmits no wireless range of its own.

## Entry files
- `Assets/Prefabs/Repeaters/SignalAntenna.prefab` (variant of `Assets/Prefabs/Repeater.prefab`), `Assets/Prefabs/RedSignalVariants/Red_SignalAntenna.prefab` (variant of `SignalAntenna`, `signalType` Red).
- `Assets/RepeaterTiles/SignalAntenna.asset`, `Assets/RepeaterTiles/Red/Red_SignalAntenna.asset` (`RepeaterTile`; red preview tint 1, 0.45, 0.45), placed in `Assets/TilePalettes/Signal Palette.prefab` at cells (6,-2) and (15,-2), next to the enhancer.
- `Assets/Sprites/SignalAntenna_Unsupplied.png`, `SignalAntenna_Supplied.png`: placeholder 16x16 sprites (PPU 16, Point, uncompressed) in the repeater colours.

## Behavior and rules
- `Repeater` with `range` 0, `sectorAngle` 360 and `ignoreEnhancers` on: its coverage is only its own cell, adjacent enhancers add nothing.
- It is supplied like any repeater (a source's coverage reaches its cell). While supplied, an adjacent matching-type wire with an open port facing it is powered, because wire networks treat supplied repeaters as sources (see [cables](cables.md)).
- Pushable (inherited from `Repeater.prefab`). Blue sprite unsupplied, green supplied (`RepeaterVisual`); red type tinted by `SignalTypeVisual`.

## Public interfaces and dependencies
- No own script. `Repeater.ignoreEnhancers` (serialized, default off) was added for it; other repeaters are unchanged.

## Setup and verification
- Paint the antenna tile from `Signal Palette` on a Tilemap under the level Grid, or place the prefab under the Grid, next to an open wire port.
- Unity MCP, temporary edit-mode scene with manually invoked updates: antenna supplied by an emitter 2 cells away; coverage 1 cell with an enhancer adjacent (a plain range-0 repeater there got 5 cells); adjacent `WireTilemap` powered; a repeater 3 cells from the antenna and outside the emitter range stayed unsupplied (supplied when a normal repeater replaced the antenna); emitter off unpowered both. Not verified in Play mode or visually.
- Unity MCP preview-scene check verified the Red antenna uses the Red shader fallback, while the Original antenna keeps its normal material and a Red repeater uses its matching artwork.
