# Circuit aura

## Status
- Visual effect only: soft white glow around a whole circuit, meant for the win moment (input connected to output). The win condition itself does not exist yet; nothing calls the aura automatically.
- Not used by any existing scene or prefab; demo in `Assets/Scenes/ConnectionParticlesTest.unity` (`playOnStart` on).

## Entry files
- `Assets/Scripts/CircuitAura.cs`: component; `Assets/Prefabs/CircuitAura.prefab` has both materials assigned.
- `Assets/Shaders/CircuitGlowSprite.shader` (`Hackathon/CircuitGlowSprite`) + `Assets/Materials/CircuitGlowSprite.mat`.
- `Assets/Shaders/CircuitGlowBeam.shader` (`Hackathon/CircuitGlowBeam`) + `Assets/Materials/CircuitGlowBeam.mat`.

## Behavior and rules
- Sprites: on `Play()` each listed `SpriteRenderer` gets a child `CircuitAuraGlow` (padded quad, sorting order = sprite order - 1). The shader blurs the sprite alpha (Gaussian, 11x11 taps, `spriteGlowRadius` in sprite px) and adds the glow colour; snapped to sprite pixels. Handles atlases (texture rect), pivot, flipX/flipY; rebuilt when the sprite changes.
- Beams: each listed `ConnectionParticles` gets a `CircuitAuraBeamGlow` quad (child of the aura, `beamGlowSortingOrder`, default -2) with a Gaussian band of `beamGlowHalfWidth` px around the A-B segment, snapped to the world pixel grid. Beam particle rate is multiplied by `beamRateMultiplier` (default 2.5) via `ConnectionParticles.RateMultiplier`.
- Animation: fade in (`fadeInTime`), then pulse between `pulseMin`..`pulseMax` at `pulseSpeed` Hz; `Stop()` fades out, removes glow objects and resets the beam rate. Uses unscaled time by default.
- Glow objects use `HideFlags.DontSave` and are never saved into scenes.
- Additive blending (`Blend SrcAlpha One`): looks right on dark backgrounds; on white floors the glow is invisible.

## Public interfaces and dependencies
- `Play()`, `Stop()`, `IsPlaying`, `SetCircuit(IEnumerable<SpriteRenderer>, IEnumerable<ConnectionParticles>)`; context menu Play/Stop in the Inspector.
- Depends on [CONNECTION_PARTICLES.md](CONNECTION_PARTICLES.md) (`EndpointA/B`, `PixelsPerUnit`, `RateMultiplier`).
- Undecided: who calls `Play()` (win-condition logic), and which objects form the circuit.

## Setup and verification
- Verified via Unity MCP: shaders compile without errors; scripts compile; no console errors.
- Play mode, offscreen camera render read back numerically (red channel, background 24): aura off = 24 everywhere; aura on: 1.5 px from a sprite edge 138, beam centre 246, 4 px from beam centre 144, far from the circuit 24.
- `Stop()` resets state and beam rate; glow object removal happens at end of frame and was not observed (editor unfocused, frames did not advance). Not visually checked.
