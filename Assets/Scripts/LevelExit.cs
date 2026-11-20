using UnityEngine;

/// <summary>
/// A hole in a level's wall. Its collider is a trigger: when the player walks into it, the game returns
/// to the menu and the player is placed on the plate of the level they just left.
/// Snaps to the grid cell it is placed on, like the level plates.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class LevelExit : MonoBehaviour
{
    private void Awake()
    {
        // The exit must never block the player, whatever the inspector says.
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        Grid grid = FindAnyObjectByType<Grid>();
        if (grid == null)
            return;

        Vector3 center = grid.GetCellCenterWorld(grid.WorldToCell(transform.position));
        center.z = transform.position.z;
        transform.position = center;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerGridMovement>() == null)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("LevelExit: no GameManager available.", this);
            return;
        }

        GameManager.Instance.ReturnToMenu();
    }
}
