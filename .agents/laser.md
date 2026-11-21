# Laser

## Status
- Four directional laser presets use the supplied active and inactive emitter art and beam sprites.

## Entry files
- `Assets/Scripts/LaserEmitter.cs`, `Assets/Prefabs/Lasers/`, `Assets/Sprites/Laser.png`.
- `Assets/EntityTiles/Laser*Tile.asset`, `Assets/TilePalettes/Signal Palette.prefab`.

## Behavior and rules
- An enabled beam runs from its emitter along the preset direction to the nearest logical dual-grid Wall or `Solid` cell. Water does not stop the beam. A beam with no wall within 1024 cells is disabled and logs a warning.
- The player cannot enter active beam cells. Any `Pushable`, including a repeater or movable wire, is destroyed when it enters or is already in a beam. Pushing can move objects into a beam. Switching the beam off removes its movement blocking and shows the inactive emitter artwork.
- Beam sprites fade in and out with DOTween; `Fade Duration` defaults to 0.2 seconds. Beam blocking and destruction still switch immediately. Disabling the component clears visuals immediately.
- The beam and sparks use HDR materials with Unity URP's built-in Bloom post-processing. Each laser prefab carries a global volume using `Assets/Settings/LaserBloom.asset` (intensity 0.08); the level camera has HDR and post-processing enabled. Bloom is scene-wide and may affect other sufficiently bright objects.
- Active beams emit small, sparse sparks moving along the beam, plus denser impact sparks from both beam edges directed away from the terminating wall. Destroying a pushable produces a 36-particle burst spreading in all 2D directions from its destination cell, including dark particles; this works for objects already in a beam and objects pushed into one. All three effects use randomized sizes. Beam and wall sparks stop when the beam is off or has no terminating wall.
- The emitter belongs to its parent Grid, or to the only Grid in its scene. It must be parented to the intended Grid when a scene has several.

## Public interfaces and dependencies
- `LaserEmitter.IsOn` switches the beam at runtime; `SetOn(bool)` provides the same control to Inspector UnityEvents. `Is On` sets the initial state in the Inspector. `LaserEmitter.IsBeamAt(grid, cell)` reports whether a cell is covered.
- The four directional prefabs include a particle system using `Assets/Materials/LaserSparks.mat`, an HDR beam material `Assets/Materials/LaserBeamHDR.mat`, and a Bloom volume. Spark transparency fades over particle lifetime. `LaserEmitter` controls the effects, including per-destruction bursts.
- Uses the wall and movement rules in [Pushable blocks](pushable-blocks.md) and the dual-grid terrain described in [Dual grid](../Claude%20outputs/DUAL_GRID.md).

## Setup and verification
- Paint Left, Up, Right or Down from `Signal Palette` onto a Tilemap under the level Grid, or place the corresponding prefab. Mount the emitter in a wall cell, pointing into the room toward another wall.
- To control a laser from a [signal receiver](signal-receiver.md), place both as scene prefab instances. Add the laser to each receiver event, select `LaserEmitter.SetOn`, and set its boolean for that event. For a beam that turns on with signal, use true for received and false for lost, and clear the laser's initial `Is On`; reverse those values and enable `Is On` to turn it off with signal. The receiver emits transitions only, so match the initial laser state to the no-signal state.
- Unity CLI verified receiver event bindings switch the laser state on signal acquisition and loss in both configurations in an isolated preview scene.
- `Assets/Scenes/SignalControlsTest.unity` demonstrates a receiver switching a laser off and opening a bridge when a signal-blocking crate is moved. See [Signal receiver](signal-receiver.md) for the test steps.
- Unity MCP Play-mode checks covered each direction, wall termination, water transmission, switching, player blocking, pushing a movable into the beam, destroying a repeater already in the beam, and spawning a painted laser tile.
- Unity MCP verified particle-system and material references on all four prefabs, fade-in midpoint, immediate beam shutdown, and completed fade-out. Play-mode checks verified the burst travels in all four directions and wall sparks travel away from the wall and clear when stopped. A subsequent editor check verified beam particles move forward and beam/wall sizes vary; the latest dark-particle and size changes have not been visually inspected or tested in Play mode.
- A Play-mode check verified wall sparks originate on both sides of horizontal and vertical beams; their placement has not been visually inspected.
- Unity MCP verified the HDR camera settings, Bloom profile, four laser volumes and HDR material references. Built-in Bloom appearance has not been visually inspected.
