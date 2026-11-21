using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A bridge cell over water. While its group is open, the player can walk on the cell and pushed chains can enter it,
/// even though a <see cref="DualGridLayer"/> marks the cell as Water. Bridges whose cells touch (up, down, left, right)
/// form a group: if any bridge in the group has <see cref="Walkable"/> set, every bridge in the group is walkable.
/// Walls are not affected. Place it with the Bridge tiles of the Signal Palette or as the Bridge prefab.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class Bridge : MonoBehaviour
{
    [Tooltip("Lets the player walk on this bridge and on every bridge touching it (directly or through other bridges).")]
    [SerializeField] private bool walkable;

    [Tooltip("Sprite alpha while the group is not walkable.")]
    [SerializeField, Range(0f, 1f)] private float inactiveAlpha = 0.35f;

    [Tooltip("Unity Grid (e.g. the MapGrid prefab) that defines the cells. Found automatically if empty.")]
    [SerializeField] private Grid grid;

    private static readonly List<Bridge> All = new List<Bridge>();
    private static int version;

    private SpriteRenderer spriteRenderer;
    private int cachedVersion = -1;
    private bool cachedOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        All.Clear();
        version = 0;
    }

    /// <summary>This bridge's own flag. The cell is walkable if this or any touching bridge's flag is set.</summary>
    public bool Walkable
    {
        get => walkable;
        set
        {
            if (walkable == value)
                return;
            walkable = value;
            version++;
        }
    }

    /// <summary>Set this bridge's flag from a UnityEvent, such as a signal receiver event.</summary>
    public void SetWalkable(bool value) => Walkable = value;

    /// <summary>True if the player can walk on this bridge: its own flag or a touching bridge's flag is set.</summary>
    public bool IsOpen
    {
        get
        {
            if (cachedVersion != version)
            {
                cachedOpen = ComputeOpen();
                cachedVersion = version;
            }
            return cachedOpen;
        }
    }

    /// <summary>Cell this bridge occupies.</summary>
    public Vector3Int Cell => grid != null ? grid.WorldToCell(transform.position) : default;

    /// <summary>True if an open (walkable) bridge occupies the cell of <paramref name="grid"/>.</summary>
    public static bool IsWalkableAt(Grid grid, Vector3Int cell)
    {
        for (int i = 0; i < All.Count; i++)
        {
            Bridge bridge = All[i];
            if (bridge.grid == grid && bridge.Cell == cell && bridge.IsOpen)
                return true;
        }
        return false;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        if (grid == null)
            grid = GetComponentInParent<Grid>();
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();
        if (grid == null)
        {
            Debug.LogError("Bridge: no Grid found in the scene. Add the MapGrid prefab.", this);
            return;
        }

        if (!All.Contains(this))
            All.Add(this);
        version++;
    }

    private void OnDisable()
    {
        if (All.Remove(this))
            version++;
    }

    private void OnValidate()
    {
        version++;
    }

    private void Update()
    {
        if (spriteRenderer == null)
            return;

        Color color = spriteRenderer.color;
        float alpha = IsOpen ? 1f : inactiveAlpha;
        if (!Mathf.Approximately(color.a, alpha))
        {
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }

    // Breadth-first search over touching bridges: open if any bridge in the group has its own flag set.
    private bool ComputeOpen()
    {
        if (grid == null)
            return walkable;

        var visited = new HashSet<Bridge> { this };
        var queue = new Queue<Bridge>();
        queue.Enqueue(this);

        while (queue.Count > 0)
        {
            Bridge current = queue.Dequeue();
            if (current.walkable)
                return true;

            Vector3Int cell = current.Cell;
            for (int i = 0; i < All.Count; i++)
            {
                Bridge other = All[i];
                if (other.grid != grid || visited.Contains(other))
                    continue;

                Vector3Int d = other.Cell - cell;
                if (d.z == 0 && Mathf.Abs(d.x) + Mathf.Abs(d.y) == 1)
                {
                    visited.Add(other);
                    queue.Enqueue(other);
                }
            }
        }
        return false;
    }
}
