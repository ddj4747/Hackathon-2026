# Player movement

## Status
- Grid-based player implemented: hold a direction key to keep stepping tile to tile at a set speed.
- `SampleScene` contains a player prefab instance.
- Not yet verified in the Unity editor (files were written outside it; check the console after import).

## Entry files
- `Assets/Scripts/PlayerGridMovement.cs`
- `Assets/Scripts/PlayerSpriteAnimator.cs`
- `Assets/Prefabs/Player.prefab` (SpriteRenderer + `PlayerGridMovement` + `PlayerSpriteAnimator` + `PlayerSfx` + `SignalConnectSfx`, sorting order 10)
- `Assets/Scripts/PlayerSfx.cs`, `Assets/Scripts/SignalConnectSfx.cs`; clips in `Assets/SFX/` (footsteps, cardboard-box push, 8-bit connect sound)
- `Assets/Sprites/Player.png`: sprite sheet, 4 x 2 cells of 16 x 16 px, 16 PPU, point filter, no compression. Frames `Player_0`-`Player_3` (top row) = pushing, `Player_4`-`Player_7` (bottom row) = walking. The sprites face up only.
- `Assets/Sprites/PlayerSprite.png`: old placeholder square, no longer used (can be deleted).

## Behavior and rules
- Movement is 4-way (WASD or arrow keys, Input System keyboard). The most recently pressed key wins; if it is released, any other held key takes over.
- Speed is `tilesPerSecond` (default 6). The player always completes the tile it is travelling to, then chains to the next while a key is held, so it never stops between cells.
- On start the player snaps to the centre of the nearest cell.
- Camera: not part of the player. The camera is fixed and shows 352 x 256 px (22 x 16 tiles) ([PIXEL_PERFECT_AND_FONT.md](pixel-perfect-and-font.md)). `Assets/Scripts/PlayerCameraFollow.cs` is no longer on any prefab or scene and can be deleted.
- Physics: the prefab has a kinematic `Rigidbody2D` (no gravity) and a 0.5 x 0.5 `BoxCollider2D`, only so trigger zones such as `LevelExit` detect the player. Movement is still script-driven; `PlayerGridMovement.Awake` re-adds them if removed. Level walls are not `Solid` objects: wall and water terrain blocking comes from the dual grid (below).
- Pushing: stepping into a `Pushable` shoves it and any number of consecutive pushables one cell ahead; a `Solid` cell or a chain that cannot move blocks the player ([PUSHABLE_BLOCKS.md](pushable-blocks.md)). The player also cannot enter Wall or Water cells of a `DualGridLayer` (unpainted cells count as wall once any terrain is painted; an empty layer blocks nothing), see [DUAL_GRID.md](../Claude%20outputs/DUAL_GRID.md). Water under a walkable `Bridge` can be walked on ([bridge.md](bridge.md)). Additional collision and walkability rules remain undecided (see root `AGENTS.md`).
- Active laser beam cells also block the player. If a beam switches on during a normal step, the player returns to the starting cell and a pushed chain finishes its movement. Undo animations are processed separately; see [Laser](laser.md).

- Undo (Q, action `Player/Undo`, keyboard only): every started step records the player cell, its facing and the cells of the chain it shoved; Q reverts the last step with an animation: logical cells change at once, then the player walks back to its previous cell at `tilesPerSecond * undoSpeedMultiplier` (1.5) still facing the undone step (walk frames, or push frames when objects come back) and the shoved objects slide back with it (`Pushable.BeginMoveTo`); at the end the facing from before the step is restored. A step in progress is reversed from where it is; Q during an undo animation lands it at once and starts the next undo. No new steps while undoing. `IsUndoing` is public; `IsMoving` / `IsPushing` include the undo. History is per level load (R / scene reload clears it), up to `maxUndoSteps` (1000). Signal, wires and beams follow the restored positions. Unity MCP verified in a temporary scene (manual stepping): chain push, steps, cancelling a half-finished push and undoing back to the start; empty history returns false; animation positions halfway, push flag while a block comes back, chained Q, final facing. Not checked in Play mode.
- Animation (`PlayerSpriteAnimator`): walking plays frames 4-7, pushing (a step that shoves a `Pushable`) plays frames 0-3, at `framesPerSecond` (12). When the player stands still the last frame stays. The player is rotated (root transform, z) to face its last direction: up 0, left 90, down 180, right -90 degrees; `rotationOffset` corrects it if the art faces another way. Facing changes only when a step actually starts in that direction (not on blocked moves and not while a step is still finishing). Not yet verified visually.

- Sound (`PlayerSfx`): each `StepStarted` (step or undo) plays one footstep cut from the footsteps clip (`footstepStart` 0.03 s, `footstepLength` 0.17 s, small random pitch). While `IsPushing`, a scrape loop cut from the push clip (1-13.5 s) plays, fading in and out over `pushFadeTime` (0.1 s). Clips must be Decompress On Load so their samples can be read; otherwise the whole clip is used.
- Sound (`SignalConnectSfx`, on the player prefab only because every level has a player): every 0.05 s it checks repeaters (antenna included) and receivers / collectors in the player's scene and plays the connect clip once (volume 0.5) when any of them became supplied. Connections in the first 0.5 s after start (the level's initial state) are silent.
- Unity MCP Play-mode check of the sounds (manual stepping, `LevelClaude'a`): step / push / undo events with the push flag, push loop fade-in, slices created (0.17 s, 12.5 s), a new connection detected for each repeater placed, none at start. Not listened to.

## Public interfaces and dependencies
- Uses the `Grid` from the `MapGrid` prefab (see [MAP_GRID.md](map-grid.md)); the `grid` field is auto-found with `FindFirstObjectByType<Grid>()` if empty.
- Public: `CurrentCell`, `IsMoving`, `IsPushing`, `Facing` (read-only), event `StepStarted(bool pushing)`, `TeleportToCell(Vector3Int)` (instant move to a cell centre, cancels a step in progress, clears the undo history), `Undo()`, `UndoCount`, constant `UndoActionPath`.
- Requires Input System (`activeInputHandler: 1` in ProjectSettings).

## Setup and verification
- Add `MapGrid` and `Player` to a scene, press Play, hold WASD/arrows.
- To tune speed change `tilesPerSecond` (on the prefab).
