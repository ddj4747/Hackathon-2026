using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
public sealed class WireNetwork : MonoBehaviour
{
    [SerializeField, Tooltip("True when at least one wire in this tilemap is supplied. Updated during Play mode.")]
    private bool isPowerSupplied;

    private readonly HashSet<Vector3Int> powered = new();
    private Tilemap tilemap;
    private static readonly Vector3Int[] Steps = { Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left };

    public bool IsPowerSupplied => isPowerSupplied;
    public bool IsPowered(Vector3Int cell) => powered.Contains(cell);

    private void Awake() => tilemap = GetComponent<Tilemap>();

    private void Update() => Recalculate();

    public void Recalculate()
    {
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        var grid = GetComponentInParent<Grid>();
        var nextPowered = new HashSet<Vector3Int>();
        var wires = new Dictionary<Vector3Int, WireTile>();
        foreach (var cell in tilemap.cellBounds.allPositionsWithin)
            if (tilemap.GetTile(cell) is WireTile wire) wires[cell] = wire;

        if (grid != null && gameObject.scene.isLoaded)
        {
            var antennaCells = new HashSet<Vector3Int>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var antenna in root.GetComponentsInChildren<Antenna>())
                {
                    if (!antenna.IsSignalSupplied) continue;
                    var parentGrid = antenna.GetComponentInParent<Grid>();
                    if (parentGrid != null && parentGrid != grid) continue;
                    antennaCells.Add(tilemap.WorldToCell(antenna.transform.position));
                }

            var queue = new Queue<Vector3Int>();
            foreach (var pair in wires)
                foreach (var step in Steps)
                {
                    var end = pair.Key + step;
                    if (!wires.ContainsKey(end) && antennaCells.Contains(end) && HasPort(pair.Key, pair.Value, step))
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
                        HasPort(neighbor, other, -step) && nextPowered.Add(neighbor))
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
        if (tilemap != null) foreach (var cell in previous) tilemap.RefreshTile(cell);
    }
}
