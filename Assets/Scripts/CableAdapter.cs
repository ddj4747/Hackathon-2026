using UnityEngine;

// Place an enabled adapter on each cable endpoint to connect it to the floor signal.
[DisallowMultipleComponent]
public sealed class CableAdapter : MonoBehaviour
{
    [SerializeField, Tooltip("Debug signal for testing cables in Play mode.")]
    private bool testSignal;

    [SerializeField] private SignalType signalType;
    private SignalType registeredType;
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
        if (manager == target && cell == targetCell && resetVersion == target.ResetVersion && registeredType == signalType)
            return;

        RemoveTestSignal();
        manager = target;
        cell = targetCell;
        resetVersion = target.ResetVersion;
        registeredType = signalType;
        manager.AddSignal(cell, 1f, registeredType);
    }

    private void OnDisable() => RemoveTestSignal();

    private void RemoveTestSignal()
    {
        if (manager != null && resetVersion == manager.ResetVersion)
            manager.RemoveSignal(cell, 1f, registeredType);
        manager = null;
    }
}
