using System.Collections.Generic;
using UnityEngine;

/// <summary>Marks a stationary or manually moved object as blocking wireless signal.</summary>
[DisallowMultipleComponent]
public sealed class SignalBlocker : MonoBehaviour
{
    private static readonly HashSet<SignalBlocker> Active = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Active.Clear();

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    internal static void Collect(Grid grid, HashSet<Vector3Int> cells)
    {
        foreach (var blocker in Active)
        {
            if (blocker.gameObject.scene != grid.gameObject.scene) continue;
            var parentGrid = blocker.GetComponentInParent<Grid>();
            if (parentGrid == null)
            {
                foreach (var root in blocker.gameObject.scene.GetRootGameObjects())
                    foreach (var candidate in root.GetComponentsInChildren<Grid>(true))
                    {
                        if (parentGrid != null) { parentGrid = null; goto resolved; }
                        parentGrid = candidate;
                    }
            }
            resolved:
            if (parentGrid != grid) continue;
            cells.Add(grid.WorldToCell(blocker.transform.position));
        }
    }
}
