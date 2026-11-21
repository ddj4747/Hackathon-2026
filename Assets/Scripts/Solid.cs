using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks an object as solid: it occupies a grid cell, and neither the player nor a pushed chain
/// can enter that cell. Add it to walls and other obstacles. Snaps to the cell it is placed on.
/// </summary>
[DisallowMultipleComponent]
public class Solid : MonoBehaviour
{
    [Tooltip("Unity Grid (e.g. the MapGrid prefab) that defines the cells. Found automatically if empty.")]
    [SerializeField] private Grid grid;

    private static readonly List<Solid> All = new List<Solid>();

    /// <summary>Cell this object occupies.</summary>
    public Vector3Int Cell { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => All.Clear();

    /// <summary>True if a solid object occupies the cell.</summary>
    public static bool IsAt(Vector3Int cell)
    {
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].Cell == cell)
                return true;
        }
        return false;
    }

    internal static bool IsAt(Grid targetGrid, Vector3Int cell)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].grid == targetGrid && All[i].Cell == cell)
                return true;
        return false;
    }

    /// <summary>
    /// True if the player or a pushed chain cannot enter the cell: a <see cref="Solid"/> object is there, or the
    /// cell is Wall or Water on a <see cref="DualGridLayer"/>, or an active laser covers it.
    /// </summary>
    public static bool IsBlocked(Grid grid, Vector3Int cell)
    {
        return IsTerrainBlocked(grid, cell) || LaserEmitter.IsBeamAt(grid, cell);
    }

    // A pushed object can enter a beam and be destroyed, but cannot enter walls or water.
    internal static bool IsTerrainBlocked(Grid grid, Vector3Int cell) =>
        IsAt(cell) || DualGridLayer.IsBlocked(grid, cell);

    private void Awake()
    {
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();

        if (grid == null)
        {
            Debug.LogError("Solid: no Grid found in the scene. Add the MapGrid prefab.", this);
            enabled = false;
            return;
        }

        Cell = grid.WorldToCell(transform.position);
        Vector3 p = grid.GetCellCenterWorld(Cell);
        p.z = transform.position.z;
        transform.position = p;
    }

    private void OnEnable()
    {
        if (grid != null && !All.Contains(this))
            All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }
}
