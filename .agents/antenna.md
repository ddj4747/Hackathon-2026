# Antenna

## Status

- Antennas contribute coverage to the scene's connection manager. An Inspector toggle controls supply.

## Entry files

- `Assets/Scripts/Antenna.cs`, `Assets/Prefabs/Antenna.prefab`
- [Connection manager](connection-manager.md), [Cables](cables.md)

## Behavior and rules

- `Is Signal Supplied` defaults to true. When off, the antenna removes its signal contributions; when on, it supplies quality 1 to its own cell and configured sector. Changes take effect during Play mode.
- Range, angle, position and local +X facing determine the coverage described in the root project instructions. Grid lookup uses the nearest parent Grid or the only Grid in the scene.

## Public interfaces and dependencies

- Coverage uses the scene-scoped `ConnectionManager`. Sector angle and range are serialized on the antenna.
- `IsSignalSupplied` exposes whether the antenna is enabled and supplies signal; [wire tilemaps](cables.md) use it for adjacent open-port adapters.

## Setup and verification

- Place an antenna under a Grid when the scene contains more than one. Set `Is Signal Supplied` in the Inspector to control its contribution.
- Unity MCP verified supply on and off, preservation of another contribution, restoration and recovery after a signal reset.
