# Repeater

## Status

- Repeaters contribute coverage when signal reaches their cell. A [signal emitter](signal-emitter.md) can provide the starting signal.
- Fifteen visual variants each use the top-row unsupplied sprite and matching bottom-row supplied sprite, with separate Original and Red artwork.
- The Original sheet has an additional leftmost column that is not sliced; the existing 15 variants use the following columns.

## Entry files

- `Assets/Scripts/Repeater.cs`, `Assets/Scripts/RepeaterVisual.cs`, `Assets/Prefabs/Repeater.prefab`
- `Assets/Resources/Repeaters.png`, `Assets/Resources/RedRepeaters.png`, `Assets/Prefabs/Repeaters/`, `Assets/Prefabs/RedSignalVariants/`, `Assets/RepeaterTiles/` and `Assets/RepeaterTiles/Red/`
- `Assets/Scripts/RepeaterTile.cs`, `Assets/TilePalettes/Signal Palette.prefab`, `Assets/Prefabs/RepeaterTilemap.prefab`
- [Connection manager](connection-manager.md), [Cables](cables.md), [Map grid](map-grid.md)

## Behavior and rules

- A repeater supplied by a positive signal of its configured type contributes quality 1 to its own cell and configured sector or line. It stops contributing if its source is lost. Cycles cannot supply themselves without an external source.
- Walls and marked objects obstruct its line of sight; the hover overlay shows the unobstructed potential coverage. See [Connection manager](connection-manager.md).
- Range, sector angle, start angle, position and local +X facing determine the coverage described in the root project instructions. The start angle defaults to 0 degrees and the sector extends counterclockwise. Grid lookup uses the nearest parent Grid or the only Grid in the scene.
- Variants `Repeater_04` through `Repeater_15` use a one-cell-wide line along the center of their former 90-degree sector. The line extends Range cells, rotates with the prefab, and is extended by adjacent enhancers. Other repeater prefabs retain sector coverage.
- In Play mode the visual follows `IsSignalSupplied`; Original uses blue when unsupplied and green when supplied, while Red uses purple and red. Red artwork replaces `#257179` with `#5D275D`, `#38B764` with `#B13E53`, and `#A7F070` with `#EF7D57`. Edit-mode tiles preview the matching unsupplied artwork. A painted repeater tile instantiates its matching functional prefab.
- The player can push standalone and painted repeaters using the existing push and chain rules. Coverage and supply are recalculated from the repeater's moving position. See [Pushable blocks](pushable-blocks.md).
- Adjacent pushable [enhancers](repeater-enhancer.md) increase the repeater range by one each; diagonal cells do not count.
- Prefab ranges (normal and Red): circular `Repeater_01`-`03` = 2, 3, 4; line variants `Repeater_04`-`15` = 3, 4, 5 per direction (04-06, 07-09, 10-12, 13-15). Base `Repeater.prefab` stays 3.
- `ignoreEnhancers` (serialized, default off) makes a repeater ignore adjacent enhancers; used by the [signal antenna](signal-antenna.md) (range 0, feeds cables only).
- Hovering a repeater shows its potential coverage through the [connection overlay](connection-manager.md), even while it is unsupplied.

## Public interfaces and dependencies

- Coverage uses the scene-scoped `ConnectionManager`. Signal type, sector angle, start angle, range and the straight-line option are serialized on the repeater. Original is the default. Red repeaters resolve matching sprite names from `Assets/Resources/RedRepeaters.png`, falling back to the shared Red shader when no matching artwork exists.
- `IsSignalSupplied` exposes whether the repeater is enabled and receives signal; [wire tilemaps](cables.md) use it for adjacent open-port adapters. In Play mode a matching-type wire also supplies a repeater next to its open port when the wire is fed by an emitter or by another supplied repeater; see [Cables](cables.md).

## Setup and verification

- Select `Signal Palette` and paint on a Tilemap beneath the level Grid. It contains 15 repeater variants, four emitter orientations, four receiver orientations and four collectors for each signal type, plus wires and other signal tiles. Red variants are to the right of the originals. `RepeaterTilemap` contains a sample row of all 15 variants; `SampleScene` contains a modified instance with six repeater tiles.
- Standalone variants can also be placed under a Grid and configured as Original or Red per instance. Painted repeater tiles use the type configured on their referenced prefab. Place a signal emitter so that its coverage reaches a repeater's cell; tile-created instances can be inspected in Play mode.
- Unity CLI previously verified palette references, edit previews, all 15 instantiated prefabs and both sprite mappings. Unity MCP verified sector boundaries, start-angle changes, wraparound, rotation, and zero- and full-width coverage. Unity MCP also verified signal activation, chaining and shutdown without a source.

- Unity verified that all 15 variants inherit Pushable and checked a painted repeater in Play mode: the player pushed it out of supply, removing its coverage, and pushing it back restored coverage.
- Unity Play-mode checks in an isolated scene verified the unsupplied range preview and all 15 painted variants switching sprites when signal was supplied and removed.
- An isolated Unity preview-scene check verified all 12 line prefabs, their directions and ranges, activation and shutdown, rotation and enhancer extension; it also verified that circular coverage is unchanged.

- Unity MCP preview-scene checks verified independent supply and removal for overlapping Original and Red repeaters.

- Unity MCP verified all Red repeater tile references and the matching and mismatched repeaters in `Assets/Scenes/SignalTypesTest.unity` through isolated update calls.
