using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ConnectionManager
{
    private static readonly Dictionary<Scene, ConnectionManager> ManagersByScene = new();

    private readonly Dictionary<(Vector3Int cell, SignalType type), List<float>> signalsByCell = new();

    private readonly Dictionary<object, (Vector3Int a, Vector3Int b, SignalType type)> cableLinks = new();
    private readonly Dictionary<Repeater, (Vector3Int center, List<Vector3Int> coverage)> repeaters = new();
    private readonly HashSet<Repeater> poweredRepeaters = new();
    private readonly HashSet<(Vector3Int cell, SignalType type)> repeaterSignalCells = new();

    internal event Action<Vector3Int> SignalChanged;
    internal event Action SignalsCleared;
    internal IEnumerable<Vector3Int> SignalCells
    {
        get
        {
            var cells = new HashSet<Vector3Int>();
            foreach (var key in signalsByCell.Keys) cells.Add(key.cell);
            foreach (var link in cableLinks.Values) { cells.Add(link.a); cells.Add(link.b); }
            foreach (var key in repeaterSignalCells) cells.Add(key.cell);
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

    public float GetSignalQuality(Vector3Int cell, SignalType type = SignalType.Original)
    {
        var strongest = 0f;
        var found = false;
        foreach (var reachable in ConnectedCells(cell, type))
        {
            if (signalsByCell.TryGetValue((reachable, type), out var signals))
                foreach (var signal in signals)
                {
                    strongest = found ? Mathf.Max(strongest, signal) : signal;
                    found = true;
                }
            if (repeaterSignalCells.Contains((reachable, type)))
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
            if (GetSignalQuality(pair.Value.center, pair.Key.Type) > 0f && poweredRepeaters.Add(pair.Key))
                pending.Enqueue(pair.Key);

        while (pending.Count > 0)
        {
            var repeater = pending.Dequeue();
            foreach (var cell in repeaters[repeater].coverage)
            {
                repeaterSignalCells.Add((cell, repeater.Type));
                var connected = ConnectedCells(cell, repeater.Type);
                foreach (var pair in repeaters)
                    if (pair.Key.Type == repeater.Type && connected.Contains(pair.Value.center) && poweredRepeaters.Add(pair.Key))
                        pending.Enqueue(pair.Key);
            }
        }

        affected.UnionWith(SignalCells);
        foreach (var cell in affected) SignalChanged?.Invoke(cell);
    }

    private HashSet<Vector3Int> ConnectedCells(Vector3Int cell, SignalType type)
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
                if (link.type != type) continue;
                if (link.a == current) next = link.b;
                else if (link.b == current) next = link.a;
                else continue;
                if (visited.Add(next)) pending.Enqueue(next);
            }
        }
        return visited;
    }

    internal void SetCableLink(object owner, Vector3Int a, Vector3Int b, SignalType type = SignalType.Original)
    {
        var affected = AllSignalCells();
        RemoveCableLink(owner);
        cableLinks[owner] = (a, b, type);
        RecalculateRepeaters(affected);
    }

    internal void RemoveCableLink(object owner)
    {
        if (!cableLinks.TryGetValue(owner, out var link)) return;
        var affected = AllSignalCells();
        cableLinks.Remove(owner);
        RecalculateRepeaters(affected);
    }

    public float GetSignalQuality(Vector3 worldPosition, GridLayout grid, SignalType type = SignalType.Original)
    {
        return GetSignalQuality(grid.WorldToCell(worldPosition), type);
    }

    public bool HasSignal(Vector3Int cell, SignalType type = SignalType.Original)
    {
        return GetSignalQuality(cell, type) > 0f;
    }

    public bool HasSignal(Vector3 worldPosition, GridLayout grid, SignalType type = SignalType.Original)
    {
        return HasSignal(grid.WorldToCell(worldPosition), type);
    }

    public void AddSignal(Vector3Int cell, float quality, SignalType type = SignalType.Original)
    {
        if (float.IsNaN(quality) || float.IsInfinity(quality))
            throw new ArgumentOutOfRangeException(nameof(quality), "Signal quality must be finite.");

        var affected = AllSignalCells();
        if (!signalsByCell.TryGetValue((cell, type), out var signals))
        {
            signals = new List<float>();
            signalsByCell.Add((cell, type), signals);
        }

        signals.Add(quality);
        RecalculateRepeaters(affected);
    }

    public bool RemoveSignal(Vector3Int cell, float quality, SignalType type = SignalType.Original)
    {
        var affected = AllSignalCells();
        if (!signalsByCell.TryGetValue((cell, type), out var signals) || !signals.Remove(quality))
            return false;

        if (signals.Count == 0)
            signalsByCell.Remove((cell, type));

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
