using System.Collections.Generic;
using UnityEngine;

// Snapshot object blockers; terrain is queried as needed so tile changes are reflected immediately.
internal sealed class SignalObstacles
{
    private readonly HashSet<Vector3Int> cells = new();
    private readonly List<DualGridLayer> terrain = new();
    // Emitters and receivers (inputs / outputs) may sit in a wall; the wall does not block their own cell.
    private readonly HashSet<Vector3Int> wallMounted = new();
    private Vector2[] polygon = new Vector2[12];
    private Vector2[] scratch = new Vector2[12];
    private readonly Vector2[] corners = new Vector2[4];

    // Jammers compute their own coverage from walls and other blockers, so they pass includeJammers: false.
    internal SignalObstacles(Grid grid, bool includeJammers = true)
    {
        Pushable.CollectSignalBlockers(grid, cells);
        SignalBlocker.Collect(grid, cells);
        if (includeJammers) SignalJammer.Collect(grid, cells);
        foreach (var layer in grid.GetComponentsInChildren<DualGridLayer>())
            if (layer.isActiveAndEnabled && layer.GetComponentInParent<Grid>() == grid &&
                layer.Data.GetUsedTilesCount() > 0)
                terrain.Add(layer);
        if (terrain.Count > 0)
        {
            foreach (var emitter in grid.GetComponentsInChildren<SignalEmitter>())
                wallMounted.Add(grid.WorldToCell(emitter.transform.position));
            foreach (var receiver in grid.GetComponentsInChildren<SignalReceiver>())
                wallMounted.Add(grid.WorldToCell(receiver.transform.position));
        }
    }

    private bool Blocks(Vector3Int cell)
    {
        if (cells.Contains(cell)) return true;
        if (wallMounted.Contains(cell)) return false;
        foreach (var layer in terrain)
            if (layer.GetTerrain(cell) == DualGridTerrain.Wall) return true;
        return false;
    }

    // Trace from the exact source position to the target cell centre in grid coordinates.
    // At an exact corner, the two side cells block only when both are occupied.
    internal bool HasLineOfSight(Vector2 origin, Vector3Int source, Vector3Int target, Vector2 centerOffset)
    {
        if (Blocks(source)) return false;
        if (source == target) return true;
        var end = new Vector2(target.x, target.y) + centerOffset;
        var delta = end - origin;
        int sx = delta.x > 0 ? 1 : delta.x < 0 ? -1 : 0;
        int sy = delta.y > 0 ? 1 : delta.y < 0 ? -1 : 0;
        float tx = sx == 0 ? float.PositiveInfinity : ((sx > 0 ? source.x + 1 : source.x) - origin.x) / delta.x;
        float ty = sy == 0 ? float.PositiveInfinity : ((sy > 0 ? source.y + 1 : source.y) - origin.y) / delta.y;
        float dx = sx == 0 ? float.PositiveInfinity : 1f / Mathf.Abs(delta.x);
        float dy = sy == 0 ? float.PositiveInfinity : 1f / Mathf.Abs(delta.y);
        var cell = source;
        while (cell != target)
        {
            if (Mathf.Abs(tx - ty) < 0.000001f)
            {
                if (Blocks(cell + new Vector3Int(sx, 0, 0)) && Blocks(cell + new Vector3Int(0, sy, 0))) return false;
                cell += new Vector3Int(sx, sy, 0);
                tx += dx;
                ty += dy;
            }
            else if (tx < ty)
            {
                cell.x += sx;
                tx += dx;
            }
            else
            {
                cell.y += sy;
                ty += dy;
            }
            if (Blocks(cell)) return false;
        }
        // A cell is also blocked when any positive area of it lies in a blocker's shadow,
        // even if the ray to its centre narrowly misses the blocker.
        for (var y = Mathf.Min(source.y, target.y); y <= Mathf.Max(source.y, target.y); y++)
            for (var x = Mathf.Min(source.x, target.x); x <= Mathf.Max(source.x, target.x); x++)
            {
                var blocker = new Vector3Int(x, y, source.z);
                if (blocker != source && blocker != target && Blocks(blocker) &&
                    ShadowOverlaps(origin, blocker, target)) return false;
            }
        return true;
    }

    // Clip the target square against the two rays through the blocker's silhouette and
    // against its source-facing sides. A positive-area intersection is in shadow;
    // touching a shadow boundary at only an edge or a point is not.
    private bool ShadowOverlaps(Vector2 origin, Vector3Int blocker, Vector3Int target)
    {
        var min = new Vector2(blocker.x, blocker.y);
        var max = min + Vector2.one;
        corners[0] = min;
        corners[1] = new Vector2(max.x, min.y);
        corners[2] = max;
        corners[3] = new Vector2(min.x, max.y);
        var left = Vector2.zero;
        var right = Vector2.zero;
        foreach (var corner in corners)
        {
            var ray = corner - origin;
            if (ray.sqrMagnitude < 0.000000000001f) continue;
            if (left == Vector2.zero || Cross(left, ray) < 0f) left = ray;
            if (right == Vector2.zero || Cross(right, ray) > 0f) right = ray;
        }

        polygon[0] = new Vector2(target.x, target.y);
        polygon[1] = new Vector2(target.x + 1, target.y);
        polygon[2] = new Vector2(target.x + 1, target.y + 1);
        polygon[3] = new Vector2(target.x, target.y + 1);
        var count = 4;
        count = Clip(count, new Vector2(-left.y, left.x), origin);
        count = Clip(count, new Vector2(right.y, -right.x), origin);
        if (origin.x < min.x) count = Clip(count, Vector2.right, min);
        else if (origin.x > max.x) count = Clip(count, Vector2.left, max);
        if (origin.y < min.y) count = Clip(count, Vector2.up, min);
        else if (origin.y > max.y) count = Clip(count, Vector2.down, max);

        if (count < 3) return false;
        var area = 0f;
        for (var i = 1; i + 1 < count; i++)
            area += Cross(polygon[i] - polygon[0], polygon[i + 1] - polygon[0]);
        return Mathf.Abs(area) > 0.000001f;
    }

    // Keeps the side of the line for which dot(normal, point - linePoint) >= 0.
    private int Clip(int count, Vector2 normal, Vector2 linePoint)
    {
        if (count == 0) return 0;
        var output = 0;
        var previous = polygon[count - 1];
        var previousDistance = Vector2.Dot(normal, previous - linePoint);
        for (var i = 0; i < count; i++)
        {
            var current = polygon[i];
            var distance = Vector2.Dot(normal, current - linePoint);
            if ((distance >= 0f) != (previousDistance >= 0f))
                scratch[output++] = Vector2.LerpUnclamped(previous, current, previousDistance / (previousDistance - distance));
            if (distance >= 0f) scratch[output++] = current;
            previous = current;
            previousDistance = distance;
        }
        (polygon, scratch) = (scratch, polygon);
        return output;
    }

    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
