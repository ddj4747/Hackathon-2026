# Repeater enhancer

## Status
- Implemented as a pushable range modifier, with a tinted block sprite as placeholder artwork.

## Entry files
- `Assets/Scripts/RepeaterEnhancer.cs`, `Assets/Prefabs/RepeaterEnhancer.prefab`, `Assets/SignalTiles/RepeaterEnhancer.asset`, `Assets/TilePalettes/Signal Palette.prefab`
- [Repeater](repeater.md), [Pushable blocks](pushable-blocks.md)

## Behavior and rules
- Each enabled enhancer directly above, below, left or right of a repeater adds one cell to its range. Bonuses stack; diagonals do not count. Moving or disabling an enhancer updates coverage. The repeater must still receive signal to contribute coverage.
- An enhancer affects repeaters on the same Grid in its scene. It uses its nearest parent Grid or the only Grid in its scene.

## Public interfaces and dependencies
- `RepeaterEnhancer` requires `Pushable`; repeaters count active adjacent enhancers when calculating coverage.

## Setup and verification
- Paint the cyan enhancer tile from `Signal Palette` onto a Tilemap beneath the level Grid, or place the prefab under that Grid. Its sprite is a placeholder.
- Unity verified base and extended coverage, stacking, diagonal exclusion, moving away and back, and disabling in an isolated preview scene. Unity compiled without console errors.
