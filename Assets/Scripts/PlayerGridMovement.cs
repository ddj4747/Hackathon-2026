using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Grid-based player movement. While a direction key is held the player keeps stepping
/// from tile to tile at <see cref="tilesPerSecond"/>. The player always finishes the tile
/// it is travelling to, so it never rests between cells.
/// Controls (not final): WASD or arrow keys, 4 directions, the most recently pressed key wins.
/// </summary>
[DisallowMultipleComponent]
public class PlayerGridMovement : MonoBehaviour
{
    [Tooltip("Unity Grid (e.g. the MapGrid prefab) that defines the cells. Found automatically if empty.")]
    [SerializeField] private Grid grid;

    [Tooltip("Movement speed while a direction is held, in tiles per second.")]
    [SerializeField, Min(0.01f)] private float tilesPerSecond = 6f;

    private Vector3Int currentCell;
    private Vector3Int targetCell;
    private float stepProgress = 1f;   // 0..1 between currentCell and targetCell; 1 = standing still
    private Vector3Int lastHeldDirection;

    public Vector3Int CurrentCell => currentCell;
    public bool IsMoving => stepProgress < 1f;

    private void Awake()
    {
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();

        EnsurePhysics();
    }

    /// <summary>
    /// Trigger events (level exits) need a Rigidbody2D and a collider on the player. The prefab has both;
    /// this only fills them in if they were removed. The body is kinematic: movement is driven by this script.
    /// </summary>
    private void EnsurePhysics()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        if (GetComponent<Collider2D>() == null)
            gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.5f;
    }

    /// <summary>Instantly places the player at the centre of a cell and cancels any step in progress.</summary>
    public void TeleportToCell(Vector3Int cell)
    {
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();
        if (grid == null)
            return;

        currentCell = cell;
        targetCell = cell;
        stepProgress = 1f;
        lastHeldDirection = Vector3Int.zero;
        transform.position = CellCenter(cell);
    }

    private void Start()
    {
        if (grid == null)
        {
            Debug.LogError("PlayerGridMovement: no Grid found in the scene. Add the MapGrid prefab.", this);
            enabled = false;
            return;
        }

        // Snap onto the nearest cell at start.
        currentCell = grid.WorldToCell(transform.position);
        targetCell = currentCell;
        transform.position = CellCenter(currentCell);
    }

    private void Update()
    {
        Vector3Int input = ReadDirection();

        if (!IsMoving && input != Vector3Int.zero)
            BeginStep(input);

        if (!IsMoving)
            return;

        stepProgress += tilesPerSecond * Time.deltaTime;

        // Chain steps while a key is held; leftover progress is carried so speed stays exact.
        while (stepProgress >= 1f)
        {
            currentCell = targetCell;
            stepProgress -= 1f;

            if (input != Vector3Int.zero)
            {
                targetCell = currentCell + input;
            }
            else
            {
                stepProgress = 1f;
                break;
            }
        }

        Vector3 from = CellCenter(currentCell);
        Vector3 to = CellCenter(targetCell);
        transform.position = IsMoving ? Vector3.Lerp(from, to, stepProgress) : from;
    }

    private void BeginStep(Vector3Int direction)
    {
        targetCell = currentCell + direction;
        stepProgress = 0f;
    }

    private Vector3 CellCenter(Vector3Int cell)
    {
        Vector3 p = grid.GetCellCenterWorld(cell);
        p.z = transform.position.z;
        return p;
    }

    /// <summary>
    /// Returns the held direction (4-way). If several keys are held, the one pressed last wins.
    /// </summary>
    private Vector3Int ReadDirection()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return Vector3Int.zero;

        bool up = kb.wKey.isPressed || kb.upArrowKey.isPressed;
        bool down = kb.sKey.isPressed || kb.downArrowKey.isPressed;
        bool left = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
        bool right = kb.dKey.isPressed || kb.rightArrowKey.isPressed;

        bool upNew = kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
        bool downNew = kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame;
        bool leftNew = kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame;
        bool rightNew = kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;

        if (upNew) lastHeldDirection = Vector3Int.up;
        else if (downNew) lastHeldDirection = Vector3Int.down;
        else if (leftNew) lastHeldDirection = Vector3Int.left;
        else if (rightNew) lastHeldDirection = Vector3Int.right;

        if (IsHeld(lastHeldDirection, up, down, left, right))
            return lastHeldDirection;

        // The last-pressed key was released; fall back to any key that is still held.
        if (up) return lastHeldDirection = Vector3Int.up;
        if (down) return lastHeldDirection = Vector3Int.down;
        if (left) return lastHeldDirection = Vector3Int.left;
        if (right) return lastHeldDirection = Vector3Int.right;

        lastHeldDirection = Vector3Int.zero;
        return Vector3Int.zero;
    }

    private static bool IsHeld(Vector3Int dir, bool up, bool down, bool left, bool right)
    {
        if (dir == Vector3Int.up) return up;
        if (dir == Vector3Int.down) return down;
        if (dir == Vector3Int.left) return left;
        if (dir == Vector3Int.right) return right;
        return false;
    }
}
