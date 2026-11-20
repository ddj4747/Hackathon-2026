using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// A starting signal source. Parent it under the level Grid when a scene has multiple grids.
[DisallowMultipleComponent]
public sealed class SignalEmitter : MonoBehaviour
{
    [SerializeField, Min(0), Tooltip("Radius of the signal in grid cells.")]
    private int range = 3;

    private readonly List<Vector3Int> coveredCells = new();
    private Grid grid;
    private Scene gridScene;
    private ConnectionManager manager;
    private uint resetVersion;
    private Vector3Int lastCenter;
    private Vector2 lastOrigin;
    private int lastRange;

    private void OnEnable() => gridScene = default;
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
        if (manager == target && resetVersion == target.ResetVersion && center == lastCenter &&
            origin.Equals(lastOrigin) && radius == lastRange)
            return;

        RemoveCoverage();
        manager = target;
        resetVersion = target.ResetVersion;
        lastCenter = center;
        lastOrigin = origin;
        lastRange = radius;

        AddCoverage(center);
        var roundedRadius = radius + 0.4f;
        var radiusSquared = roundedRadius * roundedRadius;
        var centerOffset = (Vector2)grid.GetLayoutCellCenter();
        for (var y = -radius; y <= radius; y++)
            for (var x = -radius; x <= radius; x++)
            {
                if (x == 0 && y == 0) continue;
                var cell = center + new Vector3Int(x, y, 0);
                var offset = new Vector2(cell.x, cell.y) + centerOffset - origin;
                if (offset.sqrMagnitude <= radiusSquared) AddCoverage(cell);
            }
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

    private void OnDisable() => RemoveCoverage();

    private void RemoveCoverage()
    {
        if (manager != null && resetVersion == manager.ResetVersion)
            foreach (var cell in coveredCells) manager.RemoveSignal(cell, 1f);
        coveredCells.Clear();
        manager = null;
    }
}
