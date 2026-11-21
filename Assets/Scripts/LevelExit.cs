using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A signal-controlled exit. When all assigned collectors are supplied, the hole opens and the player
/// can walk into it to return to the menu. The exit snaps to the grid cell it is placed on.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    [Tooltip("Scene collectors that control this exit. All entries must receive matching signal to open it.")]
    [SerializeField] private SignalReceiver[] collectors = System.Array.Empty<SignalReceiver>();

    private SpriteRenderer exitSprite;
    private static readonly List<LevelExit> Exits = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetExits() => Exits.Clear();

    private void OnEnable()
    {
        if (!Exits.Contains(this)) Exits.Add(this);
    }

    private void OnDisable() => Exits.Remove(this);

    // Only an open exit on this Grid permits the player to enter its wall cell.
    internal static bool IsOpenAt(Grid grid, Vector3Int cell)
    {
        if (grid == null) return false;
        foreach (var exit in Exits)
        {
            if (exit == null || !exit.isActiveAndEnabled || !exit.IsOpen || exit.gameObject.scene != grid.gameObject.scene)
                continue;
            Grid exitGrid = exit.GetComponentInParent<Grid>();
            if (exitGrid == null) exitGrid = FindAnyObjectByType<Grid>();
            if (exitGrid == grid && grid.WorldToCell(exit.transform.position) == cell)
                return true;
        }
        return false;
    }

    /// <summary>True only while every assigned collector is enabled and receiving signal.</summary>
    public bool IsOpen
    {
        get
        {
            if (collectors == null || collectors.Length == 0)
                return false;
            foreach (var collector in collectors)
                if (collector == null || !collector.isActiveAndEnabled || !collector.IsSignalSupplied)
                    return false;
            return true;
        }
    }

    private void Awake()
    {
        // The exit must never block the player, whatever the inspector says.
        GetComponent<Collider2D>().isTrigger = true;
        exitSprite = GetComponent<SpriteRenderer>();
        RefreshAppearance();
    }

    private void Start()
    {
        if (collectors == null || collectors.Length == 0 || System.Array.Exists(collectors, item => item == null))
            Debug.LogWarning("LevelExit: assign every collector in the Inspector; the exit will remain closed until configured.", this);

        Grid grid = GetComponentInParent<Grid>();
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();
        if (grid == null)
            return;

        Vector3 center = grid.GetCellCenterWorld(grid.WorldToCell(transform.position));
        center.z = transform.position.z;
        transform.position = center;
    }

    private void LateUpdate() => RefreshAppearance();

    private void RefreshAppearance()
    {
        if (exitSprite != null)
            exitSprite.enabled = IsOpen;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryExit(other);

    // The last collector can receive signal while the player is already standing on the exit cell.
    private void OnTriggerStay2D(Collider2D other) => TryExit(other);

    private void TryExit(Collider2D other)
    {
        if (!IsOpen || other.GetComponentInParent<PlayerGridMovement>() == null)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("LevelExit: no GameManager available.", this);
            return;
        }

        GameManager.Instance.ReturnToMenu();
    }
}
