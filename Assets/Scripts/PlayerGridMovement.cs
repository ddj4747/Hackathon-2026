using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Grid-based player movement. While a direction key is held the player keeps stepping
/// from tile to tile at <see cref="tilesPerSecond"/>. The player always finishes the tile
/// it is travelling to, so it never rests between cells.
/// Walking into a <see cref="Pushable"/> shoves it (and any pushables lined up behind it) one cell ahead;
/// if that is not possible, or the target cell is <see cref="Solid"/>, wall or water, the player stays put.
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
    private readonly List<Pushable> pushed = new List<Pushable>();   // chain being shoved during the current step

    /// <summary>Undo action (Q), see <see cref="Undo"/>.</summary>
    public const string UndoActionPath = "Player/Undo";

    [Tooltip("How many steps can be undone (oldest are dropped).")]
    [SerializeField, Min(1)] private int maxUndoSteps = 1000;

    [Tooltip("Undo animation speed relative to a normal step (1 = same speed).")]
    [SerializeField, Min(0.1f)] private float undoSpeedMultiplier = 1.5f;

    // Undo animation in progress.
    private float undoProgress = 1f;
    private Vector3 undoFrom;
    private Vector3 undoTo;
    private Vector3Int undoEndFacing;
    private Pushable[] undoObjects = new Pushable[0];

    // One entry per step: where the player and the shoved chain were before it.
    private struct UndoStep
    {
        public Vector3Int playerCell;
        public Vector3Int facing;
        public Pushable[] objects;
        public Vector3Int[] cells;
    }

    private readonly List<UndoStep> undoSteps = new List<UndoStep>();
    private InputAction undoAction;

    /// <summary>Number of steps that can currently be undone.</summary>
    public int UndoCount => undoSteps.Count;

    public Vector3Int CurrentCell => currentCell;
    public bool IsMoving => stepProgress < 1f || IsUndoing;

    /// <summary>True while the current step (or the step being undone) moves at least one <see cref="Pushable"/>.</summary>
    public bool IsPushing => (stepProgress < 1f && pushed.Count > 0) || (IsUndoing && undoObjects.Length > 0);

    /// <summary>True while an undo is animating back to the previous cell.</summary>
    public bool IsUndoing => undoProgress < 1f;

    /// <summary>Direction of the last step actually started (not changed by blocked or queued input). Starts facing up.</summary>
    public Vector3Int Facing { get; private set; } = Vector3Int.up;

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

        FinishPush();
        FinishUndoAnimation();
        undoSteps.Clear();   // a teleport is not a step; older steps would no longer match
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

        undoAction = InputSystem.actions != null ? InputSystem.actions.FindAction(UndoActionPath) : null;
        if (undoAction == null)
            Debug.LogWarning($"PlayerGridMovement: action '{UndoActionPath}' not found; undo uses the Q key directly.", this);
    }

    /// <summary>
    /// Reverts the last step: the player walks back to its previous cell (still facing the undone step's direction)
    /// and every object it shoved slides back with it, with the walk / push animation. A step still in progress is
    /// reversed from where it is. Logical cells change at once; only the motion is animated.
    /// Returns false when there is nothing to undo.
    /// </summary>
    public bool Undo()
    {
        if (undoSteps.Count == 0)
            return false;

        FinishUndoAnimation();   // a previous undo still animating ends at its target first

        UndoStep step = undoSteps[undoSteps.Count - 1];
        undoSteps.RemoveAt(undoSteps.Count - 1);

        pushed.Clear();
        int count = 0;
        for (int i = 0; i < step.objects.Length; i++)
            if (step.objects[i] != null) count++;
        undoObjects = new Pushable[count];
        count = 0;
        for (int i = 0; i < step.objects.Length; i++)
        {
            if (step.objects[i] == null) continue;
            step.objects[i].BeginMoveTo(step.cells[i]);
            undoObjects[count++] = step.objects[i];
        }

        // Face the direction of the undone step while moving back, then restore the facing from before it.
        Vector3Int back = currentCell == step.playerCell ? targetCell - step.playerCell : currentCell - step.playerCell;
        if (back != Vector3Int.zero) Facing = back;
        undoEndFacing = step.facing;

        undoFrom = transform.position;
        undoTo = CellCenter(step.playerCell);
        currentCell = step.playerCell;
        targetCell = step.playerCell;
        stepProgress = 1f;
        undoProgress = 0f;
        return true;
    }

    private void UpdateUndoAnimation()
    {
        undoProgress = Mathf.Min(1f, undoProgress + tilesPerSecond * undoSpeedMultiplier * Time.deltaTime);
        transform.position = Vector3.Lerp(undoFrom, undoTo, undoProgress);
        for (int i = 0; i < undoObjects.Length; i++)
            if (undoObjects[i] != null) undoObjects[i].SetPushProgress(undoProgress);
        if (undoProgress >= 1f)
            FinishUndoAnimation();
    }

    private void FinishUndoAnimation()
    {
        if (undoObjects.Length == 0 && undoProgress >= 1f)
            return;
        undoProgress = 1f;
        transform.position = undoTo;
        for (int i = 0; i < undoObjects.Length; i++)
            if (undoObjects[i] != null) undoObjects[i].SetPushProgress(1f);
        undoObjects = new Pushable[0];
        Facing = undoEndFacing;
    }

    private bool UndoPressed()
    {
        if (undoAction != null)
            return undoAction.WasPressedThisFrame();
        Keyboard kb = Keyboard.current;
        return kb != null && kb.qKey.wasPressedThisFrame;
    }

    private void Update()
    {
        if (UndoPressed() && Undo())
            lastHeldDirection = Vector3Int.zero;   // do not walk straight back with a key still held

        if (IsUndoing)
        {
            UpdateUndoAnimation();
            return;   // no new steps until the undo has landed
        }

        // A beam can be switched on while a step is already in progress.
        if (stepProgress < 1f && LaserEmitter.IsBeamAt(grid, targetCell))
        {
            FinishPush();
            targetCell = currentCell;
            stepProgress = 1f;
            transform.position = CellCenter(currentCell);
            return;
        }

        Vector3Int input = ReadDirection();

        if (!IsMoving && input != Vector3Int.zero && TryBeginStep(input))
            stepProgress = 0f;

        if (!IsMoving)
            return;

        stepProgress += tilesPerSecond * Time.deltaTime;

        // Chain steps while a key is held; leftover progress is carried so speed stays exact.
        while (stepProgress >= 1f)
        {
            currentCell = targetCell;
            stepProgress -= 1f;
            FinishPush();

            if (input != Vector3Int.zero && TryBeginStep(input))
            {
                // Step started; leftover progress is carried into it.
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

        // The shoved block moves in lock-step with the player, one cell ahead.
        float pushProgress = IsMoving ? stepProgress : 1f;
        for (int i = 0; i < pushed.Count; i++)
            if (pushed[i] != null) pushed[i].SetPushProgress(pushProgress);
    }

    /// <summary>
    /// Sets the next target cell. Pushables standing there are shoved along as a chain;
    /// returns false (no step) when the cell is solid, wall or water, or the chain cannot move.
    /// </summary>
    private bool TryBeginStep(Vector3Int direction)
    {
        Vector3Int target = currentCell + direction;

        if (Solid.IsBlocked(grid, target))   // solid object, wall or water
            return false;
        if (!Pushable.TryBeginChain(target, direction, pushed))
            return false;

        // TryBeginChain already moved the chain one cell logically; remember where it was.
        var step = new UndoStep
        {
            playerCell = currentCell,
            facing = Facing,
            objects = pushed.ToArray(),
            cells = new Vector3Int[pushed.Count],
        };
        for (int i = 0; i < pushed.Count; i++)
            step.cells[i] = pushed[i].Cell - direction;
        undoSteps.Add(step);
        if (undoSteps.Count > maxUndoSteps)
            undoSteps.RemoveAt(0);

        targetCell = target;
        Facing = direction;   // turn only when the step really starts
        return true;
    }

    private void FinishPush()
    {
        for (int i = 0; i < pushed.Count; i++)
            if (pushed[i] != null) pushed[i].SetPushProgress(1f);
        pushed.Clear();
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
