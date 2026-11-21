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

    private readonly List<Vector3Int> coveredCells = new();
    private Grid grid;
    private Scene gridScene;
    private ConnectionManager manager;
    private uint resetVersion;
    private Vector3Int lastCenter;
    private Vector2 lastOrigin;
    private int lastRange;
    private float lastAngle;
    private Vector2 lastDirection;

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
        if (manager == sceneManager && resetVersion == sceneManager.ResetVersion && lastCenter == center && lastOrigin.Equals(origin) && lastRange == radius &&
            lastAngle == angle && lastDirection == direction)
            return;

        RemoveCoverage();
        manager = sceneManager;
        resetVersion = manager.ResetVersion;
        lastCenter = center;
        lastOrigin = origin;
        lastRange = radius;
        lastAngle = angle;
        lastDirection = direction;

        AddCoverage(center);
        if (angle <= 0f)
        {
            manager.SetRepeater(this, center, coveredCells);
            return;
        }

        // Round the boundary outward to create the intended stepped pixel-circle.
        var roundedRadius = radius + 0.4f;
        var radiusSquared = roundedRadius * roundedRadius;
        var minimumDot = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);
        var centerOffset = (Vector2)grid.GetLayoutCellCenter();
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

                AddCoverage(cell);
            }
        }
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
        if (angle <= 0f || angle >= 360f)
            return Vector2.right;

        var centerAngle = (Mathf.Repeat(startAngle, 360f) + angle * 0.5f) * Mathf.Deg2Rad;
        var localDirection = new Vector3(Mathf.Cos(centerAngle), Mathf.Sin(centerAngle), 0f);
        var direction = (Vector2)grid.transform.InverseTransformDirection(transform.TransformDirection(localDirection));
        return direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
    }

    private void AddCoverage(Vector3Int cell)
    {
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
        {
            manager.RemoveRepeater(this);
        }

        coveredCells.Clear();
        manager = null;
    }
}
