# Connection particles

## Status
- Visual effect only: white 1 px particles flying between two endpoints (repeaters). Not wired into any gameplay; connection rules remain undecided.
- Lives in the shared asset folders with its own test scene; no existing scene, prefab or script references it yet.

## Entry files
- `Assets/Scripts/ConnectionParticles.cs`: component driving the effect.
- `Assets/Prefabs/ConnectionParticles.prefab`: ParticleSystem + `ConnectionParticles`, material `Assets/Materials/ConnectionPixel.mat` (URP Particles/Unlit, white), sorting order 10.
- `Assets/Sprites/RepeaterPlaceholder.png`: temporary 16x16 repeater sprite (PPU 16, point filter, uncompressed).
- `Assets/Scenes/ConnectionParticlesTest.unity`: test scene (camera ortho 5, Global Light 2D, Repeater A/B/C, beams A-B and B-C, CircuitAura demo). Not in build settings.

## Behavior and rules
- Pixels emitted in both directions (A->B and B->A; `bothDirections` toggle), on the straight line between endpoint centres, shortened by `endpointInset` (default 0.375 = 6 px) at each end.
- Each pixel has a fixed sideways offset of -`maxSideOffsetPixels`..+`maxSideOffsetPixels` whole pixels (default 3, so the beam is 7 px wide; slider range 0-8).
- The script sets every particle position each `LateUpdate` from its lifetime progress and snaps it to the 1/16 pixel grid, so the line follows endpoints that move (e.g. pushed blocks).
- Lifetime = travel length / `speed` (default 4 tiles/s); rate `ratePerDirection` (default 10/s).
- Awake forces the ParticleSystem settings it relies on (world space, speed 0, size 1/PPU, emission and shape off). Direction and offset are encoded in `Particle.randomSeed`.

## Public interfaces and dependencies
- `SetEndpoints(Transform a, Transform b)`, `EndpointA`, `EndpointB`, `PixelsPerUnit`, `RateMultiplier` (runtime emission multiplier, used by [CIRCUIT_AURA.md](CIRCUIT_AURA.md)).
- No dependency on other modules.

## Setup and verification
- Verified via Unity MCP: compiles with no console errors. In Play mode, stepping manually (60 frames): all particles on the pixel grid, both directions present, size 1 px, sideways distance <= ~1.3 px, none inside the endpoint insets.
- Not visually checked (no screen capture).
- Not yet added to the root `AGENTS.md` module index (user asked not to edit it for now).
