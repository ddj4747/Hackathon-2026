using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ConnectionManager
{
    private static readonly Dictionary<Scene, ConnectionManager> ManagersByScene = new();

    private readonly Dictionary<Vector3Int, List<float>> signalsByCell = new();

    private readonly Dictionary<object, (Vector3Int a, Vector3Int b)> cableLinks = new();

    internal event Action<Vector3Int> SignalChanged;
    internal event Action SignalsCleared;
    internal IEnumerable<Vector3Int> SignalCells
    {
        get
        {
            var cells = new HashSet<Vector3Int>(signalsByCell.Keys);
            foreach (var link in cableLinks.Values) { cells.Add(link.a); cells.Add(link.b); }
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
            if (signalsByCell.TryGetValue(reachable, out var signals))
                foreach (var signal in signals)
                {
                    strongest = found ? Mathf.Max(strongest, signal) : signal;
                    found = true;
                }
        return strongest;
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
        RemoveCableLink(owner);
        cableLinks[owner] = (a, b);
        NotifyConnected(a);
    }

    internal void RemoveCableLink(object owner)
    {
        if (!cableLinks.TryGetValue(owner, out var link)) return;
        var affected = ConnectedCells(link.a);
        cableLinks.Remove(owner);
        foreach (var cell in affected) SignalChanged?.Invoke(cell);
    }

    private void NotifyConnected(Vector3Int cell)
    {
        foreach (var affected in ConnectedCells(cell)) SignalChanged?.Invoke(affected);
    }

    public float GetSignalQuality(Vector3 worldPosition, GridLayout grid)
    {
        return GetSignalQuality(grid.WorldToCell(worldPosition));
    }

    public void AddSignal(Vector3Int cell, float quality)
    {
        if (float.IsNaN(quality) || float.IsInfinity(quality))
            throw new ArgumentOutOfRangeException(nameof(quality), "Signal quality must be finite.");

        if (!signalsByCell.TryGetValue(cell, out var signals))
        {
            signals = new List<float>();
            signalsByCell.Add(cell, signals);
        }

        signals.Add(quality);
        NotifyConnected(cell);
    }

    public bool RemoveSignal(Vector3Int cell, float quality)
    {
        if (!signalsByCell.TryGetValue(cell, out var signals) || !signals.Remove(quality))
            return false;

        if (signals.Count == 0)
            signalsByCell.Remove(cell);

        NotifyConnected(cell);
        return true;
    }

    private void ClearSignals()
    {
        signalsByCell.Clear();
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
