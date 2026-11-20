using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SignalCable : MonoBehaviour
{
    [Tooltip("Grid used by the endpoint cells and path. Defaults to the nearest parent Grid.")]
    [SerializeField] private Grid grid;
    [Tooltip("Floor cells from one endpoint to the other, including both endpoints.")]
    [SerializeField] private List<Vector3Int> path = new();
    [Tooltip("Optional visual renderer. Its positions follow the configured path.")]
    [SerializeField] private LineRenderer cableRenderer;

    private ConnectionManager manager;
    private Vector3Int registeredA, registeredB;
    private bool registered;

    private void Update()
    {
        var selectedGrid = grid != null ? grid : GetComponentInParent<Grid>();
        if (selectedGrid == null || selectedGrid.gameObject.scene != gameObject.scene || path.Count < 2 || !ValidPath())
        {
            Disconnect();
            return;
        }

        Draw(selectedGrid);
        var a = path[0];
        var b = path[path.Count - 1];
        var connected = HasAdapter(selectedGrid, a) && HasAdapter(selectedGrid, b);
        var currentManager = ConnectionManager.ForScene(gameObject.scene);
        if (!connected) { Disconnect(); return; }
        if (registered && manager == currentManager && a == registeredA && b == registeredB) return;
        Disconnect();
        manager = currentManager;
        registeredA = a;
        registeredB = b;
        manager.SetCableLink(this, a, b);
        registered = true;
    }

    private bool ValidPath()
    {
        for (int i = 1; i < path.Count; i++)
        {
            var d = path[i] - path[i - 1];
            if (d.z != 0 || Mathf.Abs(d.x) + Mathf.Abs(d.y) != 1) return false;
        }
        return path[0] != path[path.Count - 1];
    }

    private bool HasAdapter(Grid selectedGrid, Vector3Int cell)
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var adapter in root.GetComponentsInChildren<CableAdapter>())
                if (adapter.isActiveAndEnabled && selectedGrid.WorldToCell(adapter.transform.position) == cell)
                    return true;
        return false;
    }

    private void Draw(Grid selectedGrid)
    {
        if (cableRenderer == null) return;
        cableRenderer.useWorldSpace = true;
        cableRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
            cableRenderer.SetPosition(i, selectedGrid.GetCellCenterWorld(path[i]));
    }

    private void OnDisable() => Disconnect();
    private void Disconnect()
    {
        if (registered) manager.RemoveCableLink(this);
        registered = false;
        manager = null;
    }
}
