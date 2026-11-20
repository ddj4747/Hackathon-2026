# Project
- Unity **6000.5.0f1** (Unity 6.5). Source: `ProjectSettings/ProjectVersion.txt`.
- 2D project; URP **17.5.0**, Input System **1.19.0**.
- Cinemachine **3.1.7**, Unity Pipeline **1.0.0**, DOTween (free) installed under `Assets/Plugins/Demigiant/DOTween`.
- Check the version file and `Packages/manifest.json` before changing dependencies. Do not upgrade Unity or packages without asking.

# Game concept (planned)
- A **2D top-down game** about establishing a connection between two antennas.
- The tileset will use a **dual-grid** layout. A rectangular Unity Grid prefab provides the map grid. Dual-grid tile mapping and rendering setup remain undecided.
- Visual style: **pixel art**, with a **512 × 352 pixel** target screen resolution and **16 × 16 pixel** tiles. A full viewport at this resolution spans **32 × 22 tiles**; this does not define the total map size.
- The player moves around the map and manages the placement of objects in the room to make the connection work.
- Player movement is grid-based: the player jumps from one tile to another. Allowed directions, controls, jump timing/animation and collision rules remain undecided.
- Input decisions: Space (action `Player/Confirm`) is the shared confirm/activate key, since there is no jump mechanic. Movement is WASD/arrows. Leaving a level is done by walking into a `LevelExit` hole, not by a key.
- The core gameplay is rearranging objects that affect the connection: some block it, others extend its reach, and additional effects may be defined later.
- The game should have several levels and many gameplay variants through different room layouts, object arrangements and combinations of connection effects.
- Exact connection rules, object effects, player controls, interaction mechanics and level completion conditions remain undecided. Ask the user before implementing those details; do not treat this concept as an implemented feature.

# Unity interaction
- **Computer/screen control is forbidden.** Do not use GUI automation, browser UI automation, mouse/keyboard control, or screen capture. Use commands and APIs only, with **Unity CLI first**, or **Unity MCP** when available.
- CLI currently: `C:/Users/adamb/Downloads/unity-windows-x64.exe`. Quote the executable path in shell commands.
- Run `unity status --json`; target this project's absolute path with `--project-path` on editor commands. Discover commands with `unity commands --grep <term>` or command help.
- Pipeline enables CLI/MCP access. MCP client registration is not yet verified; do not assume MCP is connected.
- CoplayDev MCP for Unity **10.3.0** is installed separately as `com.coplaydev.unity-mcp`, pinned to Git tag `v10.3.0`. Package installation does not prove a connected MCP client.
- Edit C# and Markdown directly. Use Unity APIs through CLI/MCP for scenes, prefabs, components and serialized assets; preserve `.meta` files and GUIDs.
- If a tool fails, inspect its error and the relevant logs. Report blockers; never switch to computer/screen control or request access to it.

# Decisions
- Inspect the relevant code and instructions before making changes. Do not guess gameplay rules, controls, architecture or intended behavior.
- If the answer is absent or requirements conflict, ask the user before implementing the dependent work. Continue only independent, unambiguous work.
- Never describe a planned feature as implemented.

# Module notes and context
- Store all module notes together in the root `.agents/` folder as `.agents/<module-name>.md`. Do not create per-module `AGENTS.md` files in source directories.
- Create a module note when implementing an actual module, not in advance for examples or hypothetical features. Create `.agents/` when the first note is needed.
- Read this root file first, then use the module index below to load only notes relevant to the task and its dependencies. Notes in `.agents/` must be read explicitly; do not assume they are loaded automatically.
- Keep each note short, with sections for status, entry files, behavior/rules, public interfaces/dependencies, and setup/verification. Record verified facts and explicit unresolved decisions; link to related notes instead of duplicating them.
- Split a large note into focused files within the same `.agents/` folder and index each one below. Keep the root index to module name, scope and note path.
- **Every agent must update affected module notes in the same task as a behavior, interface, setup or ownership change.** Add, rename or remove index entries whenever notes change. Update shared facts in this root file when needed. Remove stale claims; do not use notes as a work log.

# Module index
| Module | Scope | Note |
| --- | --- | --- |
| Release builds | Tag-triggered desktop builds and GitHub releases | [.github/RELEASING.md](.github/RELEASING.md) (temporary location; `.agents/` writes blocked) |
| Unity MCP | CoplayDev editor package and connection prerequisites | [.github/UNITY_MCP.md](.github/UNITY_MCP.md) (temporary location; `.agents/` writes blocked) |
| Map grid | Level grid prefab and terrain tilemap | [.github/MAP_GRID.md](.github/MAP_GRID.md) (temporary location; `.agents/` writes blocked) |
| Player movement | Grid-based player prefab, sprite and input | [Claude outputs/PLAYER_MOVEMENT.md](Claude%20outputs/PLAYER_MOVEMENT.md) (temporary location; `.agents/` and `.github/` writes blocked) |
| Level select | Menu scene, level plates, Select input action | [Claude outputs/LEVEL_SELECT.md](Claude%20outputs/LEVEL_SELECT.md) (temporary location; `.agents/` and `.github/` writes blocked) |
| Game manager | Persistent manager, return to menu, shared Confirm action | [Claude outputs/GAME_MANAGER.md](Claude%20outputs/GAME_MANAGER.md) (temporary location; `.agents/` and `.github/` writes blocked) |
| Level exit | Trigger hole in level walls that returns to the menu | [Claude outputs/LEVEL_EXIT.md](Claude%20outputs/LEVEL_EXIT.md) (temporary location; `.agents/` and `.github/` writes blocked) |

# Verification
- Verify compilation and relevant behavior through CLI/MCP; read console errors without clearing the user's console. Run checks appropriate to the change.
- Report what changed, what was verified and any remaining blockers. Do not claim success for checks not performed.
