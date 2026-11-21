# Player movement

## Status
- Grid-based player implemented: hold a direction key to keep stepping tile to tile at a set speed.
- `SampleScene` contains a player prefab instance.
- Not yet verified in the Unity editor (files were written outside it; check the console after import).

## Entry files
- `Assets/Scripts/PlayerGridMovement.cs`
- `Assets/Scripts/PlayerSpriteAnimator.cs`
- `Assets/Prefabs/Player.prefab` (SpriteRenderer + `PlayerGridMovement` + `PlayerSpriteAnimator`, sorting order 10)
- `Assets/Sprites/Player.png`: sprite sheet, 4 x 2 cells of 16 x 16 px, 16 PPU, point filter, no compression. Frames `Player_0`-`Player_3` (top row) = pushing, `Player_4`-`Player_7` (bottom row) = walking. The sprites face up only.
- `Assets/Sprites/PlayerSprite.png`: old placeholder square, no longer used (can be deleted).

## Behavior and rules
- Movement is 4-way (WASD or arrow keys, Input System keyboard). The most recently pressed key wins; if it is released, any other held key takes over.
- Speed is `tilesPerSecond` (default 6). The player always completes the tile it is travelling to, then chains to the next while a key is held, so it never stops between cells.
- On start the player snaps to the centre of the nearest cell.
- Camera: not part of the player. The camera is fixed and shows 352 x 256 px (22 x 16 tiles) ([PIXEL_PERFECT_AND_FONT.md](pixel-perfect-and-font.md)). `Assets/Scripts/PlayerCameraFollow.cs` is no longer on any prefab or scene and can be deleted.
- Physics: the prefab has a kinematic `Rigidbody2D` (no gravity) and a 0.5 x 0.5 `BoxCollider2D`, only so trigger zones such as `LevelExit` detect the player. Movement is still script-driven; `PlayerGridMovement.Awake` re-adds them if removed. Level walls are not `Solid` objects: wall and water terrain blocking comes from the dual grid (below).
- Pushing: stepping into a `Pushable` shoves it (and the chain behind it) one cell ahead; a `Solid` cell or a chain that cannot move blocks the player ([PUSHABLE_BLOCKS.md](pushable-blocks.md)). The player also cannot enter Wall or Water cells of a `DualGridLayer` (unpainted cells count as wall once any terrain is painted; an empty layer blocks nothing), see [DUAL_GRID.md](../Claude%20outputs/DUAL_GRID.md). No other collision or walkability checks yet. Additional collision and walkability rules remain undecided (see root `AGENTS.md`).

- Animation (`PlayerSpriteAnimator`): walking plays frames 4-7, pushing (a step that shoves a `Pushable`) plays frames 0-3, at `framesPerSecond` (12). When the player stands still the last frame stays. The player is rotated (root transform, z) to face its last direction: up 0, left 90, down 180, right -90 degrees; `rotationOffset` corrects it if the art faces another way. Facing changes only when a step actually starts in that direction (not on blocked moves and not while a step is still finishing). Not yet verified visually.

## Public interfaces and dependencies
- Uses the `Grid` from the `MapGrid` prefab (see [MAP_GRID.md](map-grid.md)); the `grid` field is auto-found with `FindFirstObjectByType<Grid>()` if empty.
- Public: `CurrentCell`, `IsMoving`, `IsPushing`, `Facing` (read-only) and `TeleportToCell(Vector3Int)` (instant move to a cell centre, cancels a step in progress).
- Requires Input System (`activeInputHandler: 1` in ProjectSettings).

## Setup and verification
- Add `MapGrid` and `Player` to a scene, press Play, hold WASD/arrows.
- To tune speed change `tilesPerSecond` (on the prefab).
