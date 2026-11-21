using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// A starting signal source. Parent it under the level Grid when a scene has multiple grids.
[DisallowMultipleComponent]
public sealed class SignalEmitter : MonoBehaviour, ISignalRangeSource
{
    [SerializeField, Min(0), Tooltip("Radius of the signal in grid cells.")]
    private int range = 3;

    private readonly List<Vector3Int> coveredCells = new();
    private readonly List<Vector3Int> nextCoverage = new();
    private Grid grid;
    private Scene gridScene;
    private ConnectionManager manager;
    private uint resetVersion;

    Grid ISignalRangeSource.RangeGrid => grid;
    IReadOnlyList<Vector3Int> ISignalRangeSource.RangeCells => coveredCells;
    bool ISignalRangeSource.HasVisibleSignal => isActiveAndEnabled && grid != null &&
        manager != null && resetVersion == manager.ResetVersion;

    internal bool IsEmittingOn(Grid targetGrid) => isActiveAndEnabled && grid == targetGrid &&
        manager != null && resetVersion == manager.ResetVersion;

    private void OnEnable()
    {
        gridScene = default;
        SignalRangeSources.Add(this);
    }
    private void OnTransformParentChanged() => gridScene = default;

    private void Update()
    {
        var scene = gameObject.scene;
        if (scene.isLoaded && scene != gridScene)
        {
            gridScene = scene;
            grid = FindGrid(scene);
            if (grid == null)
                Debug.LogWarning("SignalEmitter needs a parent Grid or exactly one Grid in its scene.", this);
        }

        if (grid == null || !scene.isLoaded)
        {
            RemoveCoverage();
            return;
        }

        var target = ConnectionManager.ForScene(scene);
        var position = transform.position;
        var center = grid.WorldToCell(position);
        var origin = (Vector2)grid.LocalToCellInterpolated(grid.transform.InverseTransformPoint(position));
        var radius = Mathf.Max(0, range);
        var obstacles = new SignalObstacles(grid);
        var centerOffset = (Vector2)grid.GetLayoutCellCenter();
        nextCoverage.Clear();
        if (obstacles.HasLineOfSight(origin, center, center, centerOffset)) nextCoverage.Add(center);
        var roundedRadius = radius + 0.4f;
        var radiusSquared = roundedRadius * roundedRadius;
        for (var y = -radius; y <= radius; y++)
            for (var x = -radius; x <= radius; x++)
            {
                if (x == 0 && y == 0) continue;
                var cell = center + new Vector3Int(x, y, 0);
                var offset = new Vector2(cell.x, cell.y) + centerOffset - origin;
                if (offset.sqrMagnitude <= radiusSquared && obstacles.HasLineOfSight(origin, center, cell, centerOffset))
                    nextCoverage.Add(cell);
            }
        if (manager == target && resetVersion == target.ResetVersion && SameCoverage()) return;
        RemoveCoverage();
        manager = target;
        resetVersion = target.ResetVersion;
        foreach (var cell in nextCoverage) AddCoverage(cell);
    }

    private bool SameCoverage()
    {
        if (coveredCells.Count != nextCoverage.Count) return false;
        for (var i = 0; i < coveredCells.Count; i++)
            if (coveredCells[i] != nextCoverage[i]) return false;
        return true;
    }

    private Grid FindGrid(Scene scene)
    {
        var parentGrid = GetComponentInParent<Grid>();
        if (parentGrid != null) return parentGrid;
        Grid found = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<Grid>(true))
            {
                if (found != null) return null;
                found = candidate;
            }
        return found;
    }

    private void AddCoverage(Vector3Int cell)
    {
        manager.AddSignal(cell, 1f);
        coveredCells.Add(cell);
    }

    private void OnDisable()
    {
        SignalRangeSources.Remove(this);
        RemoveCoverage();
    }

    private void RemoveCoverage()
    {
        if (manager != null && resetVersion == manager.ResetVersion)
            foreach (var cell in coveredCells) manager.RemoveSignal(cell, 1f);
        coveredCells.Clear();
        manager = null;
    }
}
