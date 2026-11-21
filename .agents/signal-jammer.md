# Signal jammer

## Status

- A reusable jammer blocks wireless signal in a circular area while enabled.

## Entry files

- `Assets/Scripts/SignalJammer.cs`, `Assets/Prefabs/SignalJammer.prefab`, `Assets/SignalTiles/SignalJammer.asset`, `Assets/TilePalettes/Signal Palette.prefab`
- [Connection manager](connection-manager.md), [Signal emitter](signal-emitter.md), [Repeater](repeater.md)

## Behavior and rules

- For both Original and Red signals, the jammer's own cell and all cells in its radius act as wall cells for wireless line of sight: they block signal and cast shadows. The radius is not stopped by terrain or other blockers. Overlapping jammers work independently.
- Radius uses the emitter's stepped circular rule, measured from the exact position, including the center cell. Moving, disabling or changing the range updates blocking; range defaults to 3 and can be zero.
- Hovering the enabled jammer shows its full blocking area using the existing connection overlay. Its red-tinted emitter sprite is a placeholder.
- Like walls, the jammer does not change the separate cable connections or wire networks.

## Public interfaces and dependencies

- The serialized `Range` controls the radius. It resolves the nearest parent Grid, or the only Grid in its scene. Parent it under the intended Grid if there are multiple grids.
- `SignalObstacles` collects enabled jammers when signal sources calculate coverage. The jammer does not need signal to operate.

## Setup and verification

- Place `SignalJammer.prefab` under the level Grid and configure Range, or paint the red jammer from `Signal Palette` onto a Tilemap under the Grid. Painted jammers use the prefab's Range. Parent `ConnectionOverlay.prefab` under that Grid to show the area on hover.
- Unity CLI verified the palette cell, preview sprite and color, prefab reference and runtime instantiation flag; painted runtime instantiation has not been separately tested. Isolated Unity preview-scene checks exercised the rounded radius, emitter blocking, repeater shutdown, movement, zero range, disable restoration, shadowing and overlapping jammers. These checks invoked lifecycle methods directly; a full Play-mode check has not been performed.

- Unity MCP preview-scene checks invoked source and receiver updates and verified that one jammer blocks and restores both signal types.
