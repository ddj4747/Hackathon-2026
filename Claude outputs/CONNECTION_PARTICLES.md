# Connection particles

## Status
- 1 px particles flying between two endpoints, coloured by signal type when shown by `SignalBeams` (Original `originalColor` #A7F070, Red `redColor` #EF7D57; fields on `SignalBeams`; a standalone beam uses its own `color`, default white). `SignalBeams` on the `MapGrid` prefab shows them automatically for every wireless signal link in a level. Visual only; does not change signal propagation.

## Entry files
- `Assets/Scripts/ConnectionParticles.cs`: one beam between two Transforms.
- `Assets/Scripts/SignalBeams.cs`: creates / removes beams for the signal links on its Grid; on the root of `Assets/Prefabs/MapGrid.prefab` (field `beamPrefab` = `ConnectionParticles.prefab`).
- `Assets/Prefabs/ConnectionParticles.prefab`: ParticleSystem + `ConnectionParticles`, material `Assets/Materials/ConnectionPixel.mat` (URP Particles/Unlit, white), sorting order 5 (signal objects use 2, the player 10).
- `Assets/Sprites/RepeaterPlaceholder.png`: temporary 16x16 repeater sprite used only by the test scene.
- `Assets/Scenes/ConnectionParticlesTest.unity`: standalone demo (camera ortho 5, Global Light 2D, Repeater A/B/C, beams A-B and B-C, CircuitAura demo). Not in build settings.

## Behavior and rules
- Pixels emitted in both directions (A->B and B->A; `bothDirections` toggle), on the straight line between endpoint centres, shortened by `endpointInset` (default 0.375 = 6 px) at each end.
- Each pixel has a fixed sideways offset of -`maxSideOffsetPixels`..+`maxSideOffsetPixels` whole pixels (default 3, so the beam is 7 px wide; slider range 0-8).
- Every particle position is set each `LateUpdate` from its lifetime progress and snapped to the 1/16 pixel grid, so the beam follows endpoints that move (pushed repeaters).
- Lifetime = travel length / `speed` (default 4 tiles/s); rate `ratePerDirection` (default 10/s). `Emitting = false` stops new pixels while flying ones finish.
- `SignalBeams` (each `LateUpdate`, same Grid only): sources are enabled `SignalEmitter`s (`IsEmittingOn`) and supplied `Repeater`s (`IsSignalSupplied`); a link exists when a repeater's or `SignalReceiver`'s cell is in a source's `RangeCells` (coverage already accounts for walls and blockers). One beam per unordered pair. Jammers and cables get no beams. A lost link stops emitting and is destroyed after `fadeOutTime` (2.5 s). Receivers are re-scanned every `receiverRefreshInterval` (1 s). Each new beam gets the colour of its link's signal type (`ConnectionParticles.Color`); pixels already flying keep theirs.

## Public interfaces and dependencies
- `ConnectionParticles`: `SetEndpoints(a, b)`, `EndpointA`, `EndpointB`, `PixelsPerUnit`, `RateMultiplier` (used by [CIRCUIT_AURA.md](CIRCUIT_AURA.md)), `Emitting`.
- `SignalBeams.ActiveBeams`. Reads `SignalRangeSources.Active`, `Repeater.IsSignalSupplied`, `SignalEmitter.IsEmittingOn` (see `.agents/repeater.md`, `.agents/signal-emitter.md`, `.agents/connection-manager.md`); does not modify them.

## Setup and verification
- `ConnectionParticles`: verified via Unity MCP in Play mode, stepping manually: particles on the pixel grid, both directions, 1 px size, offsets within range, none inside the insets.
- `SignalBeams`: compiles with no console errors; `MapGrid.prefab` diff checked (only the new component; editor-guide values written by `DualGridLayer.OnEnable` were reverted). Play-mode behaviour NOT yet verified (the user was working in the editor). Not visually checked.
- Not yet added to the root `AGENTS.md` module index (user asked not to edit it for now).
- Unity MCP Play-mode check in `SignalTypesTest`: the Original link's pixels are #A7F070 and the Red link's #EF7D57. Not visually checked.
