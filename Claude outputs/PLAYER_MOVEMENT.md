# Player movement and camera

## Status
- Grid-based player implemented: hold a direction key to keep stepping tile to tile at a set speed.
- Not placed in `SampleScene`; drag `Assets/Prefabs/Player.prefab` into a level.
- Not yet verified in the Unity editor (files were written outside it; check the console after import).

## Entry files
- `Assets/Scripts/PlayerGridMovement.cs`
- `Assets/Scripts/PlayerCameraFollow.cs`
- `Assets/Prefabs/Player.prefab` (SpriteRenderer + `PlayerGridMovement` + `PlayerCameraFollow`, sorting order 10)
- `Assets/Sprites/PlayerSprite.png` (16 x 16 px, 16 PPU = one cell, point filter, dark outline + orange fill)

## Behavior and rules
- Movement is 4-way (WASD or arrow keys, Input System keyboard). The most recently pressed key wins; if it is released, any other held key takes over.
- Speed is `tilesPerSecond` (default 6). The player always completes the tile it is travelling to, then chains to the next while a key is held, so it never stops between cells.
- On start the player snaps to the centre of the nearest cell.
- Camera: `PlayerCameraFollow` moves `Camera.main` in `LateUpdate` with `Vector3.SmoothDamp` (`smoothTime` 0.15 s, optional `offset`), keeps the camera's Z, jumps onto the player on the first `LateUpdate` (after the player has snapped to its cell), so the camera is already centred when the scene appears. No bounds clamping, no pixel-perfect snapping, camera size unchanged (orthographic size 5; the 512 x 352 target would need 11 at 16 PPU).
- No collision or walkability checks yet. Diagonals, controls, jump animation and collision rules are still undecided (see root `AGENTS.md`).

## Public interfaces and dependencies
- Uses the `Grid` from the `MapGrid` prefab (see [MAP_GRID.md](../.github/MAP_GRID.md)); the `grid` field is auto-found with `FindFirstObjectByType<Grid>()` if empty.
- Public read-only: `CurrentCell`, `IsMoving`.
- Requires Input System (`activeInputHandler: 1` in ProjectSettings).

## Setup and verification
- Add `MapGrid` and `Player` to a scene, press Play, hold WASD/arrows.
- To tune speed change `tilesPerSecond`; for camera feel change `smoothTime`. Both are on the prefab.
- The camera follow needs a camera tagged `MainCamera` (or assign `targetCamera`).
