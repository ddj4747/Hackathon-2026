using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Turns every cell in a circular area into a wireless signal obstacle. Walls and other blockers stop it and are not covered.</summary>
[DisallowMultipleComponent]
public sealed class SignalJammer : MonoBehaviour, ISignalRangeSource
{
    [SerializeField, Min(0), Tooltip("Radius of the jammed area in grid cells.")]
    private int range = 3;

    private static readonly HashSet<SignalJammer> Active = new();
    private readonly List<Vector3Int> coverage = new();
    private Grid grid;
    private Scene gridScene;

    Grid ISignalRangeSource.RangeGrid => grid;
    IReadOnlyList<Vector3Int> ISignalRangeSource.RangeCells => coverage;
    bool ISignalRangeSource.HasVisibleSignal => isActiveAndEnabled && grid != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Active.Clear();

    private void OnEnable()
    {
        gridScene = default;
        Active.Add(this);
        SignalRangeSources.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
        SignalRangeSources.Remove(this);
        coverage.Clear();
        grid = null;
    }

    private void OnTransformParentChanged() => gridScene = default;

    private void Update()
    {
        ResolveGrid();
        coverage.Clear();
        if (grid != null) AddCoverage(grid, new SignalObstacles(grid, false), coverage, null);
    }

    // Build from current transforms at the time of each obstacle snapshot, so a jammer
    // takes effect independently of the relative Update order of signal sources.
    internal static void Collect(Grid target, HashSet<Vector3Int> cells)
    {
        // Jammers do not stop each other, so share one snapshot of the other blockers.
        SignalObstacles obstacles = null;
        foreach (var jammer in Active)
        {
            jammer.ResolveGrid();
            if (jammer.grid != target) continue;
            obstacles ??= new SignalObstacles(target, false);
            jammer.AddCoverage(target, obstacles, null, cells);
        }
    }

    private void ResolveGrid()
    {
        var scene = gameObject.scene;
        if (scene == gridScene && grid != null) return;
        gridScene = scene;
        grid = null;
        if (!scene.isLoaded) return;
        grid = GetComponentInParent<Grid>();
        if (grid != null) return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<Grid>(true))
            {
                if (grid != null) { grid = null; return; }
                grid = candidate;
            }
    }

    private void AddCoverage(Grid target, SignalObstacles obstacles, List<Vector3Int> list, HashSet<Vector3Int> cells)
    {
        var position = transform.position;
        var center = target.WorldToCell(position);
        var origin = (Vector2)target.LocalToCellInterpolated(target.transform.InverseTransformPoint(position));
        var centerOffset = (Vector2)target.GetLayoutCellCenter();
        var radius = Mathf.Max(0, range);
        var roundedRadius = radius + 0.4f;
        var radiusSquared = roundedRadius * roundedRadius;
        for (var y = -radius; y <= radius; y++)
            for (var x = -radius; x <= radius; x++)
            {
                var cell = center + new Vector3Int(x, y, 0);
                if (x != 0 || y != 0)
                {
                    var offset = new Vector2(cell.x, cell.y) + centerOffset - origin;
                    if (offset.sqrMagnitude > radiusSquared) continue;
                    // Walls and blockers are not covered, and nothing behind them is. The range overlay
                    // outlines the exposed edge facing them.
                    if (!obstacles.HasLineOfSight(origin, center, cell, centerOffset)) continue;
                }
                list?.Add(cell);
                cells?.Add(cell);
            }
    }
}
