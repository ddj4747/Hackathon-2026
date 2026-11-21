using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class Repeater : MonoBehaviour, ISignalRangeSource
{
    public bool IsSignalSupplied => isActiveAndEnabled && manager != null && manager.IsRepeaterPowered(this);
    Grid ISignalRangeSource.RangeGrid => grid;
    IReadOnlyList<Vector3Int> ISignalRangeSource.RangeCells => coveredCells;
    bool ISignalRangeSource.HasVisibleSignal => isActiveAndEnabled && grid != null &&
        manager != null && resetVersion == manager.ResetVersion;

    [SerializeField, Min(0), Tooltip("Radius in grid cells measured from this repeater's position.")]
    private int range = 3;

    [SerializeField, Range(0f, 360f), Tooltip("Coverage sector width in degrees.")]
    private float sectorAngle = 360f;

    [SerializeField, Range(0f, 360f), Tooltip("Start of the coverage sector in degrees counterclockwise from local +X.")]
    private float startAngle;

    [SerializeField, Tooltip("Use a one-cell-wide line in the direction at the center of the sector.")]
    private bool straightLine;

    private readonly List<Vector3Int> coveredCells = new();
    private readonly List<Vector3Int> nextCoverage = new();
    private Grid grid;
    private Scene gridScene;
    private ConnectionManager manager;
    private uint resetVersion;
    private Vector3Int registeredCenter;

    private void OnEnable()
    {
        gridScene = default;
        SignalRangeSources.Add(this);
    }

    private void OnTransformParentChanged()
    {
        gridScene = default;
    }

    private void Update()
    {
        var scene = gameObject.scene;
        if (scene.isLoaded && scene != gridScene)
        {
            gridScene = scene;
            grid = FindGrid(scene);
            if (grid == null)
                Debug.LogWarning("Repeater needs a parent Grid or exactly one Grid in its scene.", this);
        }

        if (grid == null || !scene.isLoaded)
        {
            RemoveCoverage();
            return;
        }

        var sceneManager = ConnectionManager.ForScene(scene);
        var position = transform.position;
        var center = grid.WorldToCell(position);
        var origin = (Vector2)grid.LocalToCellInterpolated(grid.transform.InverseTransformPoint(position));
        var radius = Mathf.Max(0, range) + CountAdjacentEnhancers(center);
        var angle = Mathf.Clamp(sectorAngle, 0f, 360f);
        var direction = GetDirection(angle);
        var obstacles = new SignalObstacles(grid);
        var centerOffset = (Vector2)grid.GetLayoutCellCenter();
        nextCoverage.Clear();
        if (obstacles.HasLineOfSight(origin, center, center, centerOffset)) nextCoverage.Add(center);
        if (straightLine)
        {
            // Sample one cell per step along the major axis to rasterize a thin line.
            var step = direction / Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
            for (var i = 1; i <= radius; i++)
            {
                var cell = center + new Vector3Int(Mathf.RoundToInt(step.x * i), Mathf.RoundToInt(step.y * i), 0);
                if (!obstacles.HasLineOfSight(origin, center, cell, centerOffset)) break;
                nextCoverage.Add(cell);
            }
            ApplyCoverage(sceneManager, center);
            return;
        }
        if (angle <= 0f)
        {
            ApplyCoverage(sceneManager, center);
            return;
        }

        // Round the boundary outward to create the intended stepped pixel-circle.
        var roundedRadius = radius + 0.4f;
        var radiusSquared = roundedRadius * roundedRadius;
        var minimumDot = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);
        for (var y = -radius; y <= radius; y++)
        {
            for (var x = -radius; x <= radius; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                var cell = center + new Vector3Int(x, y, 0);
                var offset = new Vector2(cell.x, cell.y) + centerOffset - origin;
                var distanceSquared = offset.sqrMagnitude;
                if (distanceSquared > radiusSquared)
                    continue;

                // Include cells whose centers lie on the sector boundary.
                if (angle < 360f && Vector2.Dot(direction, offset) /
                    Mathf.Sqrt(distanceSquared) < minimumDot - 0.00001f)
                    continue;

                if (obstacles.HasLineOfSight(origin, center, cell, centerOffset)) nextCoverage.Add(cell);
            }
        }
        ApplyCoverage(sceneManager, center);
    }

    private void ApplyCoverage(ConnectionManager target, Vector3Int center)
    {
        bool same = manager == target && resetVersion == target.ResetVersion && registeredCenter == center &&
            coveredCells.Count == nextCoverage.Count;
        for (var i = 0; same && i < coveredCells.Count; i++) same = coveredCells[i] == nextCoverage[i];
        if (same) return;
        RemoveCoverage();
        manager = target;
        resetVersion = target.ResetVersion;
        registeredCenter = center;
        coveredCells.AddRange(nextCoverage);
        manager.SetRepeater(this, center, coveredCells);
    }

    private Grid FindGrid(Scene scene)
    {
        var parentGrid = GetComponentInParent<Grid>();
        if (parentGrid != null)
            return parentGrid;

        // Root-level repeaters can use the level's grid when it is unambiguous.
        Grid sceneGrid = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var candidate in root.GetComponentsInChildren<Grid>(true))
            {
                if (sceneGrid != null)
                    return null;

                sceneGrid = candidate;
            }
        }

        return sceneGrid;
    }

    private int CountAdjacentEnhancers(Vector3Int center)
    {
        var count = 0;
        foreach (var enhancer in RepeaterEnhancer.Active)
        {
            if (!enhancer.UsesGrid(grid)) continue;
            var offset = grid.WorldToCell(enhancer.transform.position) - center;
            if (offset.z == 0 && Mathf.Abs(offset.x) + Mathf.Abs(offset.y) == 1)
                count++;
        }
        return count;
    }

    private Vector2 GetDirection(float angle)
    {
        if (!straightLine && (angle <= 0f || angle >= 360f))
            return Vector2.right;

        var centerAngle = (Mathf.Repeat(startAngle, 360f) + angle * 0.5f) * Mathf.Deg2Rad;
        var localDirection = new Vector3(Mathf.Cos(centerAngle), Mathf.Sin(centerAngle), 0f);
        var direction = (Vector2)grid.transform.InverseTransformDirection(transform.TransformDirection(localDirection));
        return direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
    }

    private void OnDisable()
    {
        SignalRangeSources.Remove(this);
        RemoveCoverage();
    }

    private void RemoveCoverage()
    {
        if (manager != null && resetVersion == manager.ResetVersion)
        {
            manager.RemoveRepeater(this);
        }

        coveredCells.Clear();
        manager = null;
    }
}
