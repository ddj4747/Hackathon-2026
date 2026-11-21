# Signal jammer

## Status

- A reusable jammer blocks wireless signal in a circular area while enabled.

## Entry files

- `Assets/Scripts/SignalJammer.cs`, `Assets/Prefabs/SignalJammer.prefab`, `Assets/SignalTiles/SignalJammer.asset`, `Assets/TilePalettes/Signal Palette.prefab`
- `Assets/Resources/Jammers.png` (sliced sprite sheet), `Assets/Prefabs/Jammers/SignalJammer_1-3.prefab`, `Assets/SignalTiles/SignalJammer_1-3.asset`
- [Connection manager](connection-manager.md), [Signal emitter](signal-emitter.md), [Repeater](repeater.md)

## Behavior and rules

- For both Original and Red signals, the jammer's own cell and all cells in its radius act as wall cells for wireless line of sight: they block signal and cast shadows. The radius is stopped by terrain walls, signal-blocking objects and pushables with `stopsSignal`, using the same line-of-sight and shadow rules as emitters; those cells and everything behind them are not covered. The hover outline runs along the edge facing the wall or blocker, not inside it. Jammers do not stop each other; overlapping jammers work independently.
- Radius uses the emitter's stepped circular rule, measured from the exact position, including the center cell. Moving, disabling or changing the range updates blocking; range defaults to 3 and can be zero.
- Hovering the enabled jammer shows its full blocking area using the existing connection overlay, drawn as the same animated dashed outline as other signal objects, in white. The base `SignalJammer.prefab` keeps its red-tinted emitter sprite as a placeholder.
- Radius variants are prefab variants of `SignalJammer.prefab` that override only name, `range`, sprite and an untinted white color: `SignalJammer_1`, `_2` and `_3` use range 1, 2 and 3 with `Jammers_0`, `Jammers_1` and `Jammers_2` (circular body, small, medium and large core, like `Repeater_01`-`03`). `Jammers_3`-`Jammers_14` are directional shapes, not yet used. Each variant has a `PrefabTile` asset in `Assets/SignalTiles/`.
- The player can push jammers (and all radius variants) using the existing push and chain rules; `Pushable` is on the base prefab and `stopsSignal` is off, since the jammer blocks through its own area. Its area and hover outline follow the jammer from its moving position. See [Pushable blocks](pushable-blocks.md).
- Like walls, the jammer does not change the separate cable connections or wire networks.

## Public interfaces and dependencies

- The serialized `Range` controls the radius. It resolves the nearest parent Grid, or the only Grid in its scene. Parent it under the intended Grid if there are multiple grids.
- `SignalObstacles` collects enabled jammers when signal sources calculate coverage. The jammer does not need signal to operate.

## Setup and verification

- `Signal Palette` holds the three radius tiles (`SignalJammer_1`-`3`) in the three cells right of the red jammer's original-signal neighbors (row of the base jammer, cells x 5-7). Alternatively place the variant prefabs from `Assets/Prefabs/Jammers/` directly under the level Grid. The variants and palette cells were written as files without opening Unity, so they have not been imported or run yet.
- Place `SignalJammer.prefab` under the level Grid and configure Range, or paint the red jammer from `Signal Palette` onto a Tilemap under the Grid. Painted jammers use the prefab's Range. Parent `ConnectionOverlay.prefab` under that Grid to show the area on hover.
- Unity CLI verified the palette cell, preview sprite and color, prefab reference and runtime instantiation flag; painted runtime instantiation has not been separately tested. Isolated Unity preview-scene checks exercised the rounded radius, emitter blocking, repeater shutdown, movement, zero range, disable restoration, shadowing and overlapping jammers. These checks invoked lifecycle methods directly; a full Play-mode check has not been performed.

- Unity MCP preview-scene checks invoked source and receiver updates and verified that one jammer blocks and restores both signal types.
