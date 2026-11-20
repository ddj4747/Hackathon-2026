using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ConnectionManager
{
    private static readonly Dictionary<Scene, ConnectionManager> ManagersByScene = new();

    private readonly Dictionary<Vector3Int, List<float>> signalsByCell = new();

    private readonly Dictionary<object, (Vector3Int a, Vector3Int b)> cableLinks = new();
    private readonly Dictionary<Repeater, (Vector3Int center, List<Vector3Int> coverage)> repeaters = new();
    private readonly HashSet<Repeater> poweredRepeaters = new();
    private readonly HashSet<Vector3Int> repeaterSignalCells = new();

    internal event Action<Vector3Int> SignalChanged;
    internal event Action SignalsCleared;
    internal IEnumerable<Vector3Int> SignalCells
    {
        get
        {
            var cells = new HashSet<Vector3Int>(signalsByCell.Keys);
            foreach (var link in cableLinks.Values) { cells.Add(link.a); cells.Add(link.b); }
            cells.UnionWith(repeaterSignalCells);
            return cells;
        }
    }
    internal uint ResetVersion { get; private set; }

    static ConnectionManager()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private ConnectionManager()
    {
    }

    public static ConnectionManager Instance => ForScene(SceneManager.GetActiveScene());

    public static ConnectionManager ForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            throw new ArgumentException("The scene must be loaded.", nameof(scene));

        if (!ManagersByScene.TryGetValue(scene, out var manager))
        {
            manager = new ConnectionManager();
            ManagersByScene.Add(scene, manager);
        }

        return manager;
    }

    public static void ResetState()
    {
        foreach (var manager in ManagersByScene.Values)
            manager.ClearSignals();
    }

    public float GetSignalQuality(Vector3Int cell)
    {
        var strongest = 0f;
        var found = false;
        foreach (var reachable in ConnectedCells(cell))
        {
            if (signalsByCell.TryGetValue(reachable, out var signals))
                foreach (var signal in signals)
                {
                    strongest = found ? Mathf.Max(strongest, signal) : signal;
                    found = true;
                }
            if (repeaterSignalCells.Contains(reachable))
            {
                strongest = found ? Mathf.Max(strongest, 1f) : 1f;
                found = true;
            }
        }
        return strongest;
    }

    internal bool IsRepeaterPowered(Repeater repeater) => poweredRepeaters.Contains(repeater);

    internal void SetRepeater(Repeater repeater, Vector3Int center, List<Vector3Int> coverage)
    {
        var affected = AllSignalCells();
        repeaters[repeater] = (center, new List<Vector3Int>(coverage));
        RecalculateRepeaters(affected);
    }

    internal void RemoveRepeater(Repeater repeater)
    {
        if (!repeaters.ContainsKey(repeater)) return;
        var affected = AllSignalCells();
        repeaters.Remove(repeater);
        RecalculateRepeaters(affected);
    }

    private HashSet<Vector3Int> AllSignalCells() => new(SignalCells);

    // Start from external contributions, then expand only from reachable repeaters.
    // Recomputing from scratch prevents a loop of repeaters from powering itself.
    private void RecalculateRepeaters(HashSet<Vector3Int> affected)
    {
        poweredRepeaters.Clear();
        repeaterSignalCells.Clear();
        var pending = new Queue<Repeater>();
        foreach (var pair in repeaters)
            if (GetSignalQuality(pair.Value.center) > 0f && poweredRepeaters.Add(pair.Key))
                pending.Enqueue(pair.Key);

        while (pending.Count > 0)
        {
            var repeater = pending.Dequeue();
            foreach (var cell in repeaters[repeater].coverage)
            {
                repeaterSignalCells.Add(cell);
                var connected = ConnectedCells(cell);
                foreach (var pair in repeaters)
                    if (connected.Contains(pair.Value.center) && poweredRepeaters.Add(pair.Key))
                        pending.Enqueue(pair.Key);
            }
        }

        affected.UnionWith(SignalCells);
        foreach (var cell in affected) SignalChanged?.Invoke(cell);
    }

    private HashSet<Vector3Int> ConnectedCells(Vector3Int cell)
    {
        var visited = new HashSet<Vector3Int> { cell };
        var pending = new Queue<Vector3Int>();
        pending.Enqueue(cell);
        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            foreach (var link in cableLinks.Values)
            {
                Vector3Int next;
                if (link.a == current) next = link.b;
                else if (link.b == current) next = link.a;
                else continue;
                if (visited.Add(next)) pending.Enqueue(next);
            }
        }
        return visited;
    }

    internal void SetCableLink(object owner, Vector3Int a, Vector3Int b)
    {
        var affected = AllSignalCells();
        RemoveCableLink(owner);
        cableLinks[owner] = (a, b);
        RecalculateRepeaters(affected);
    }

    internal void RemoveCableLink(object owner)
    {
        if (!cableLinks.TryGetValue(owner, out var link)) return;
        var affected = AllSignalCells();
        cableLinks.Remove(owner);
        RecalculateRepeaters(affected);
    }

    public float GetSignalQuality(Vector3 worldPosition, GridLayout grid)
    {
        return GetSignalQuality(grid.WorldToCell(worldPosition));
    }

    public bool HasSignal(Vector3Int cell)
    {
        return GetSignalQuality(cell) > 0f;
    }

    public bool HasSignal(Vector3 worldPosition, GridLayout grid)
    {
        return HasSignal(grid.WorldToCell(worldPosition));
    }

    public void AddSignal(Vector3Int cell, float quality)
    {
        if (float.IsNaN(quality) || float.IsInfinity(quality))
            throw new ArgumentOutOfRangeException(nameof(quality), "Signal quality must be finite.");

        var affected = AllSignalCells();
        if (!signalsByCell.TryGetValue(cell, out var signals))
        {
            signals = new List<float>();
            signalsByCell.Add(cell, signals);
        }

        signals.Add(quality);
        RecalculateRepeaters(affected);
    }

    public bool RemoveSignal(Vector3Int cell, float quality)
    {
        var affected = AllSignalCells();
        if (!signalsByCell.TryGetValue(cell, out var signals) || !signals.Remove(quality))
            return false;

        if (signals.Count == 0)
            signalsByCell.Remove(cell);

        RecalculateRepeaters(affected);
        return true;
    }

    private void ClearSignals()
    {
        signalsByCell.Clear();
        repeaters.Clear();
        poweredRepeaters.Clear();
        repeaterSignalCells.Clear();
        ResetVersion++;
        SignalsCleared?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        ResetState();
        ManagersByScene.Clear();

        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        if (ManagersByScene.Remove(scene, out var manager))
            manager.ClearSignals();
    }
}
