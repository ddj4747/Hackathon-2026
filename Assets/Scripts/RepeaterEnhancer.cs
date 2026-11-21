using System.Collections.Generic;
using UnityEngine;

// A movable modifier; repeaters inspect enabled enhancers in their scene.
[DisallowMultipleComponent]
[RequireComponent(typeof(Pushable))]
public sealed class RepeaterEnhancer : MonoBehaviour
{
    private static readonly List<RepeaterEnhancer> active = new();

    internal static IReadOnlyList<RepeaterEnhancer> Active => active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => active.Clear();

    private void OnEnable()
    {
        if (!active.Contains(this)) active.Add(this);
    }

    private void OnDisable() => active.Remove(this);

    internal bool UsesGrid(Grid target)
    {
        if (gameObject.scene != target.gameObject.scene) return false;
        var parent = GetComponentInParent<Grid>();
        if (parent != null) return parent == target;

        Grid onlyGrid = null;
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<Grid>(true))
            {
                if (onlyGrid != null) return false;
                onlyGrid = candidate;
            }
        return onlyGrid == target;
    }
}
