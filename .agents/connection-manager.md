# Connection manager

## Status

- A loaded scene gets its own manager on first access. Future scenes need no component setup.

## Entry files

- `Assets/Scripts/ConnectionManager.cs`, `Assets/Scripts/SignalType.cs`, `Assets/Scripts/ConnectionOverlay.cs`, `Assets/Resources/SignalRangeOverlay.shader`, `Assets/Prefabs/ConnectionOverlay.prefab`
- [Map grid](map-grid.md)

## Behavior and rules

- Each cell stores separate lists of finite signal qualities for Original and Red. Each type?s quality is its largest value, or `0` when absent; both may coexist.
- Adding the same value twice creates two contributions. Removal matches one stored value exactly and reports whether it was found.
- Cable-linked cells share the strongest directly contributed signal of the cable?s type throughout their connected component. Links do not themselves add contributions, so loops cannot sustain a signal after its source is removed. See [Cables](cables.md).
- Signal sources activate same-type repeaters whose cells they reach. Active repeaters extend signal to their coverage; activation is recomputed from sources so repeater cycles cannot sustain themselves. See [Repeater](repeater.md) and [Signal emitter](signal-emitter.md).
- Wireless coverage uses direct line of sight: walls, marked objects and jammer coverage block their cells and cast a shadow away from the source. A tile loses signal if any positive area lies in shadow; two blockers meeting at a corner also block a diagonal line. Signal updates when blockers move or terrain changes. Cables retain their existing separate connections.
- Unloading a scene clears and drops its manager. Starting a new play session starts with fresh managers.
- The overlay shows only the hovered enabled emitter, repeater or jammer's own range. An unsupplied repeater shows its potential coverage without transmitting signal. Nothing is shown off an object; the overlay does not affect signal propagation.
- Emitter and repeater previews have a one-pixel dashed outer boundary with no fill. Dashes move clockwise in one-pixel steps at six pixels per second. Original uses `#38B764` when supplied and `#257179` when unsupplied; Red uses `#B13E53` and `#5D275D`. Emitters use the supplied color. Jammer previews retain their cyan squares.

## Public interfaces and dependencies

- `ConnectionManager.Instance` resolves the active scene. Use `ForScene(gameObject.scene)` for an object that can live in an additively loaded scene.
- `AddSignal(cell, quality)`, `RemoveSignal(cell, quality)` and `GetSignalQuality(cell)` accept Unity `Vector3Int` cell coordinates and an optional `SignalType` (defaults to Original). `GetSignalQuality(worldPosition, grid)` converts through the supplied grid.
- `HasSignal(cell)` and `HasSignal(worldPosition, grid)` accept an optional type and report whether that type?s strongest signal quality is positive, including signals received over cable links. Cells without a contribution have no signal.
- [Signal jammers](signal-jammer.md) turn every cell in their radius into a wireless obstacle. `SignalBlocker` marks a GameObject as an obstacle; `Pushable.stopsSignal` marks a movable object. Once a `DualGridLayer` contains terrain tiles, its logical wall cells (including unpainted cells) also block wireless signal. An entirely empty layer does not block signal.
- `ConnectionManager.ResetState()` clears contributions from all managers while keeping their instances.

## Setup and verification

- Verified maximum selection, duplicate removal, empty cells, world-to-cell lookup, scene isolation and global reset in Unity.
- Verified cable propagation, competing sources, disconnection and source removal in a loop with Unity CLI.
- `LevelTemplate` and `Level0` include a linked `ConnectionOverlay.prefab` under `MapGrid`. For other levels, add one overlay under each Grid that needs range previews, keeping its local position at zero; the overlay and the signal objects must resolve to the same Grid. Keep the overlay enabled and the scene camera tagged `MainCamera`. Hover works in Play mode using the Input System mouse and each signal object's sprite bounds; the overlay renderer must sort above the ground. Existing levels do not inherit changes made to the template.
- In Level0 Play mode, Unity verified camera-ray selection of all three repeaters, populated their coverage tiles, and cleared the overlay. An isolated Unity preview-scene check also verified per-source coverage, loss of supply and unchanged signal propagation.

- Unity MCP preview-scene checks verified coexisting types, matching repeater activation and shutdown, and cable type isolation.

- `Assets/Scenes/SignalTypesTest.unity` has two working signal paths and a mismatched repeater near each emitter. It is a standalone test scene, not a level-select entry. A separate Unity 6000.5.0f1 Play-mode run verified the two paths, mismatched repeaters, wire interruption, reconnection and source removal.
