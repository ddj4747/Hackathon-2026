using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
public sealed class WireNetwork : MonoBehaviour
{
    [SerializeField] private SignalType signalType;
    public SignalType Type => signalType;

    [SerializeField, Tooltip("True when at least one wire in this tilemap is supplied. Updated during Play mode.")]
    private bool isPowerSupplied;

    private readonly HashSet<Vector3Int> powered = new();
    private readonly Dictionary<Vector3Int, MovableWire> movableWires = new();
    private Tilemap tilemap;
    private SignalType previewType;
    private bool hasPreviewType;
    private static readonly Vector3Int[] Steps = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };

    public bool IsPowerSupplied => isPowerSupplied;
    public bool IsPowered(Vector3Int cell)
    {
        if (!Application.isPlaying) return powered.Contains(cell);
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        var grid = GetComponentInParent<Grid>();
        return grid != null && powered.Contains(grid.WorldToCell(tilemap.GetCellCenterWorld(cell)));
    }

    // A powered open port can deliver signal to an object in the adjacent cell.
    public bool SuppliesAdjacent(Vector3 worldPosition, Grid targetGrid, SignalType type = SignalType.Original)
    {
        if (!isActiveAndEnabled || targetGrid == null || GetComponentInParent<Grid>() != targetGrid)
            return false;
        if (Application.isPlaying)
        {
            var cell = targetGrid.WorldToCell(worldPosition);
            if (movableWires.ContainsKey(cell)) return false;
            foreach (var step in Steps)
            {
                var neighbor = cell - step;
                if (powered.Contains(neighbor) && movableWires.TryGetValue(neighbor, out var movable) &&
                    movable != null && movable.Type == type && movable.HasPort(step)) return true;
            }
            return false;
        }
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        var target = tilemap.WorldToCell(worldPosition);
        if (tilemap.GetTile(target) is WireTile) return false;
        foreach (var step in Steps)
        {
            var wireCell = target - step;
            if (powered.Contains(wireCell) && tilemap.GetTile(wireCell) is WireTile wire &&
                wire.TypeOn(this) == type && HasPort(wireCell, wire, step))
                return true;
        }
        return false;
    }

    private void Awake() => tilemap = GetComponent<Tilemap>();

    private void Update()
    {
        if (!Application.IsPlaying(gameObject) && (!hasPreviewType || previewType != signalType))
        {
            if (tilemap == null) tilemap = GetComponent<Tilemap>();
            tilemap.RefreshAllTiles();
            previewType = signalType;
            hasPreviewType = true;
        }
        Recalculate();
    }

    public void Recalculate()
    {
        if (Application.isPlaying)
        {
            RecalculateMovable();
            return;
        }
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        var grid = GetComponentInParent<Grid>();
        var nextPowered = new HashSet<Vector3Int>();
        var wires = new Dictionary<Vector3Int, WireTile>();
        foreach (var cell in tilemap.cellBounds.allPositionsWithin)
            if (tilemap.GetTile(cell) is WireTile wire) wires[cell] = wire;

        if (grid != null && gameObject.scene.isLoaded)
        {
            var sourceCells = new HashSet<(Vector3Int cell, SignalType type)>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var repeater in root.GetComponentsInChildren<Repeater>())
                {
                    if (!repeater.IsSignalSupplied) continue;
                    var parentGrid = repeater.GetComponentInParent<Grid>();
                    if (parentGrid != null && parentGrid != grid) continue;
                    sourceCells.Add((tilemap.WorldToCell(repeater.transform.position), repeater.Type));
                }
                foreach (var emitter in root.GetComponentsInChildren<SignalEmitter>())
                    if (emitter.IsEmittingOn(grid))
                        sourceCells.Add((tilemap.WorldToCell(emitter.transform.position), emitter.Type));
            }

            var queue = new Queue<Vector3Int>();
            foreach (var pair in wires)
                foreach (var step in Steps)
                {
                    var end = pair.Key + step;
                    if (!wires.ContainsKey(end) && sourceCells.Contains((end, pair.Value.TypeOn(this))) && HasPort(pair.Key, pair.Value, step))
                    {
                        if (nextPowered.Add(pair.Key)) queue.Enqueue(pair.Key);
                        break;
                    }
                }

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in Steps)
                {
                    var neighbor = cell + step;
                    if (wires.TryGetValue(neighbor, out var other) && HasPort(cell, wires[cell], step) &&
                        other.TypeOn(this) == wires[cell].TypeOn(this) && HasPort(neighbor, other, -step) && nextPowered.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }
        }

        var changed = new HashSet<Vector3Int>(powered);
        changed.SymmetricExceptWith(nextPowered);
        powered.Clear();
        powered.UnionWith(nextPowered);
        isPowerSupplied = powered.Count > 0;
        foreach (var cell in changed) tilemap.RefreshTile(cell);
    }

    private void RecalculateMovable()
    {
        var grid = GetComponentInParent<Grid>();
        var sources = new HashSet<(Vector3Int cell, SignalType type)>();
        movableWires.Clear();
        if (grid != null && gameObject.scene.isLoaded)
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var wire in root.GetComponentsInChildren<MovableWire>())
                    if (wire.isActiveAndEnabled && wire.Grid == grid && wire.Tile != null && wire.Pushable != null && wire.Pushable.isActiveAndEnabled)
                        movableWires[wire.Pushable.Cell] = wire;
                foreach (var repeater in root.GetComponentsInChildren<Repeater>())
                {
                    if (!repeater.IsSignalSupplied) continue;
                    var parentGrid = repeater.GetComponentInParent<Grid>();
                    if (parentGrid != null && parentGrid != grid) continue;
                    sources.Add((grid.WorldToCell(repeater.transform.position), repeater.Type));
                }
                foreach (var emitter in root.GetComponentsInChildren<SignalEmitter>())
                    if (emitter.IsEmittingOn(grid)) sources.Add((grid.WorldToCell(emitter.transform.position), emitter.Type));
            }
        }

        powered.Clear();
        var queue = new Queue<Vector3Int>();
        foreach (var pair in movableWires)
            foreach (var step in Steps)
            {
                var end = pair.Key + step;
                if (!movableWires.ContainsKey(end) && sources.Contains((end, pair.Value.Type)) && pair.Value.HasPort(step))
                {
                    if (powered.Add(pair.Key)) queue.Enqueue(pair.Key);
                    break;
                }
            }
        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            foreach (var step in Steps)
            {
                var neighbor = cell + step;
                if (movableWires.TryGetValue(neighbor, out var other) && movableWires[cell].HasPort(step) &&
                    other.Type == movableWires[cell].Type && other.HasPort(-step) && powered.Add(neighbor)) queue.Enqueue(neighbor);
            }
        }
        isPowerSupplied = powered.Count > 0;
        foreach (var pair in movableWires) pair.Value.SetPowered(powered.Contains(pair.Key));
    }

    private bool HasPort(Vector3Int cell, WireTile tile, Vector3Int worldStep)
    {
        var local = tilemap.GetTransformMatrix(cell).inverse.MultiplyVector(worldStep);
        if (Vector3.Distance(local, Vector3.up) < 0.01f) return tile.Connects(WireDirection.Up);
        if (Vector3.Distance(local, Vector3.right) < 0.01f) return tile.Connects(WireDirection.Right);
        if (Vector3.Distance(local, Vector3.down) < 0.01f) return tile.Connects(WireDirection.Down);
        if (Vector3.Distance(local, Vector3.left) < 0.01f) return tile.Connects(WireDirection.Left);
        return false;
    }

    private void OnDisable()
    {
        var previous = new List<Vector3Int>(powered);
        powered.Clear();
        isPowerSupplied = false;
        foreach (var wire in movableWires.Values) if (wire != null) wire.SetPowered(false);
        movableWires.Clear();
        if (!Application.isPlaying && tilemap != null) foreach (var cell in previous) tilemap.RefreshTile(cell);
    }
}
