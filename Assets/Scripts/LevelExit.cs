using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An always-open hole that returns the player to the menu. The exit snaps to its grid cell.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
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

    /// <summary>Exits are always open, independent of signal.</summary>
    public bool IsOpen => true;

    private void Awake()
    {
        // The exit must never block the player, whatever the inspector says.
        GetComponent<Collider2D>().isTrigger = true;
        var exitSprite = GetComponent<SpriteRenderer>();
        if (exitSprite != null) exitSprite.enabled = true;
    }

    private void Start()
    {
        Grid grid = GetComponentInParent<Grid>();
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();
        if (grid == null)
            return;

        Vector3 center = grid.GetCellCenterWorld(grid.WorldToCell(transform.position));
        center.z = transform.position.z;
        transform.position = center;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryExit(other);

    // Also handle the player already standing on the cell when the exit becomes active.
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
