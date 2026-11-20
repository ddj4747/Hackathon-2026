using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ConnectionManager
{
    private static readonly Dictionary<Scene, ConnectionManager> ManagersByScene = new();

    private readonly Dictionary<Vector3Int, List<float>> signalsByCell = new();

    internal event Action<Vector3Int> SignalChanged;
    internal event Action SignalsCleared;
    internal IEnumerable<Vector3Int> SignalCells => signalsByCell.Keys;
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
        if (!signalsByCell.TryGetValue(cell, out var signals))
            return 0f;

        var strongest = signals[0];
        for (var index = 1; index < signals.Count; index++)
            strongest = Mathf.Max(strongest, signals[index]);

        return strongest;
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
        SignalChanged?.Invoke(cell);
    }

    public bool RemoveSignal(Vector3Int cell, float quality)
    {
        if (!signalsByCell.TryGetValue(cell, out var signals) || !signals.Remove(quality))
            return false;

        if (signals.Count == 0)
            signalsByCell.Remove(cell);

        SignalChanged?.Invoke(cell);
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
