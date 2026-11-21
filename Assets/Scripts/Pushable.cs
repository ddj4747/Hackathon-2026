using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Add this to any object to make it pushable by the player. The object snaps to a grid cell and
/// occupies it. When the player walks into that cell, the object is shoved one cell further in the
/// same direction, together with every pushable lined up behind it (a chain). The chain is limited by
/// <see cref="PushSettings"/> and stopped by <see cref="Solid"/> objects; otherwise the player is blocked.
/// The player drives the visual motion (see <see cref="PlayerGridMovement"/>) so everything stays in sync.
/// </summary>
[DisallowMultipleComponent]
public class Pushable : MonoBehaviour
{
    [Tooltip("Whether this object stops the signal (the connection range does not pass through it). Nothing reads this yet.")]
    public bool stopsSignal;

    [Tooltip("Unity Grid (e.g. the MapGrid prefab) that defines the cells. Found automatically if empty.")]
    [SerializeField] private Grid grid;

    private static readonly List<Pushable> All = new List<Pushable>();

    private Vector3 moveFrom;
    private Vector3 moveTo;

    /// <summary>Cell this object occupies. While it is being pushed this is already the destination cell.</summary>
    public Vector3Int Cell { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => All.Clear();

    /// <summary>Returns the pushable occupying a cell, or null.</summary>
    public static Pushable GetAt(Vector3Int cell)
    {
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].Cell == cell)
                return All[i];
        }
        return null;
    }

    private void Awake()
    {
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();

        if (grid == null)
        {
            Debug.LogError("Pushable: no Grid found in the scene. Add the MapGrid prefab.", this);
            enabled = false;
            return;
        }

        Cell = grid.WorldToCell(transform.position);
        transform.position = CellCenter(Cell);
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

    /// <summary>
    /// Tries to push everything in a straight line starting at <paramref name="startCell"/>. All
    /// consecutive pushables from there are shoved one cell in <paramref name="direction"/> as one chain.
    /// Returns false (nothing moves, <paramref name="pushed"/> is empty) when the chain is longer than
    /// <see cref="PushSettings.maxChainLength"/> or the cell after it is solid. Returns true with an
    /// empty list when there is nothing to push. On success each object logically moves to its new
    /// cell immediately; the caller animates them with <see cref="SetPushProgress"/>.
    /// </summary>
    public static bool TryBeginChain(Vector3Int startCell, Vector3Int direction, List<Pushable> pushed)
    {
        pushed.Clear();
        int max = PushSettings.Instance.maxChainLength;

        Vector3Int cell = startCell;
        Pushable block;
        while ((block = GetAt(cell)) != null)
        {
            pushed.Add(block);
            if (max > 0 && pushed.Count > max)
            {
                pushed.Clear();
                return false;
            }
            cell += direction;
        }

        // 'cell' is now the first cell behind the chain; it must be free to enter.
        if (pushed.Count > 0 && Solid.IsAt(cell))
        {
            pushed.Clear();
            return false;
        }

        for (int i = 0; i < pushed.Count; i++)
            pushed[i].BeginMove(direction);
        return true;
    }

    private void BeginMove(Vector3Int direction)
    {
        Vector3Int target = Cell + direction;
        moveFrom = CellCenter(Cell);
        moveTo = CellCenter(target);
        Cell = target;
    }

    /// <summary>Places the object between the push's start and end cell (0..1).</summary>
    public void SetPushProgress(float t)
    {
        transform.position = Vector3.Lerp(moveFrom, moveTo, Mathf.Clamp01(t));
    }

    private Vector3 CellCenter(Vector3Int cell)
    {
        Vector3 p = grid.GetCellCenterWorld(cell);
        p.z = transform.position.z;
        return p;
    }
}
