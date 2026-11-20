using UnityEngine;

// Place an enabled adapter on each cable endpoint to connect it to the floor signal.
[DisallowMultipleComponent]
public sealed class CableAdapter : MonoBehaviour
{
    [SerializeField, Tooltip("Debug signal for testing cables in Play mode.")]
    private bool testSignal;

    private ConnectionManager manager;
    private Vector3Int cell;
    private uint resetVersion;

    private void Update()
    {
        var grid = GetComponentInParent<Grid>();
        if (!testSignal || grid == null || grid.gameObject.scene != gameObject.scene || !gameObject.scene.isLoaded)
        {
            RemoveTestSignal();
            return;
        }

        var target = ConnectionManager.ForScene(gameObject.scene);
        var targetCell = grid.WorldToCell(transform.position);
        if (manager == target && cell == targetCell && resetVersion == target.ResetVersion)
            return;

        RemoveTestSignal();
        manager = target;
        cell = targetCell;
        resetVersion = target.ResetVersion;
        manager.AddSignal(cell, 1f);
    }

    private void OnDisable() => RemoveTestSignal();

    private void RemoveTestSignal()
    {
        if (manager != null && resetVersion == manager.ResetVersion)
            manager.RemoveSignal(cell, 1f);
        manager = null;
    }
}
