using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Keeps this object on the centre of a cell of the scene's Grid (the MapGrid prefab).
/// In the editor it snaps whenever the object is moved, so everything you drag around ends up on the grid.
/// In play mode it snaps once when the object is enabled.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class GridSnap : MonoBehaviour
{
    private Grid grid;

    private void OnEnable()
    {
        SnapNow();
    }

#if UNITY_EDITOR
    // In edit mode Update runs whenever the scene changes, e.g. while dragging with the Move tool.
    private void Update()
    {
        if (Application.isPlaying || !transform.hasChanged)
            return;

        transform.hasChanged = false;
        SnapNow();
    }
#endif

    private void SnapNow()
    {
        if (grid == null)
            grid = FindAnyObjectByType<Grid>();
        if (grid == null)
            return;

        Vector3 center = grid.GetCellCenterWorld(grid.WorldToCell(transform.position));
        center.z = transform.position.z;

        if ((transform.position - center).sqrMagnitude < 1e-8f)
            return;

        transform.position = center;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // Keep the snapped position when this object is a prefab instance in a scene.
            PrefabUtility.RecordPrefabInstancePropertyModifications(transform);
            EditorUtility.SetDirty(transform);
        }
#endif
    }
}
