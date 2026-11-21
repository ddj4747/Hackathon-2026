using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Detects positive signal at this object's cell and raises an event on connection.
[DisallowMultipleComponent]
public sealed class SignalReceiver : MonoBehaviour
{
    [SerializeField] private UnityEvent onSignalReceived = new();

    public UnityEvent OnSignalReceived => onSignalReceived;
    public bool IsSignalSupplied { get; private set; }

    private Grid grid;
    private Scene gridScene;

    private void OnEnable() => gridScene = default;
    private void OnTransformParentChanged() => gridScene = default;

    private void Update()
    {
        var scene = gameObject.scene;
        if (scene.isLoaded && scene != gridScene)
        {
            gridScene = scene;
            grid = FindGrid(scene);
            if (grid == null)
                Debug.LogWarning("SignalReceiver needs a parent Grid or exactly one Grid in its scene.", this);
        }

        var supplied = grid != null && scene.isLoaded &&
            (ConnectionManager.ForScene(scene).HasSignal(transform.position, grid) || HasWireSignal(scene));
        var connected = supplied && !IsSignalSupplied;
        IsSignalSupplied = supplied;
        if (connected)
        {
            Debug.Log("Signal receiver connected.", this);
            onSignalReceived.Invoke();
        }
    }

    private void OnDisable() => IsSignalSupplied = false;

    private bool HasWireSignal(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var network in root.GetComponentsInChildren<WireNetwork>())
                if (network.SuppliesAdjacent(transform.position, grid)) return true;
        return false;
    }

    private Grid FindGrid(Scene scene)
    {
        var parentGrid = GetComponentInParent<Grid>();
        if (parentGrid != null) return parentGrid;
        Grid found = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<Grid>(true))
            {
                if (found != null) return null;
                found = candidate;
            }
        return found;
    }
}
