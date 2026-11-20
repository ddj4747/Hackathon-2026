using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Editor menu item that builds a Tile Palette out of the project's placeable prefabs
/// (Floor, Wall, LevelPlate, LevelExit, Player), so they can be painted on the grid like tiles.
/// Tools > Grid > Create GameObject Palette
/// </summary>
public static class GameObjectPaletteCreator
{
    private const string PaletteFolder = "Assets/Palettes";
    private const string PaletteName = "GameObjectTiles";

    private static readonly string[] PrefabPaths =
    {
        "Assets/Prefabs/Tiles/Floor.prefab",
        "Assets/Prefabs/Tiles/Wall.prefab",
        "Assets/Prefabs/LevelPlate.prefab",
        "Assets/Prefabs/LevelExit.prefab",
        "Assets/Prefabs/Player.prefab",
    };

    [MenuItem("Tools/Grid/Create GameObject Palette")]
    private static void Create()
    {
        string palettePath = $"{PaletteFolder}/{PaletteName}.prefab";

        if (!AssetDatabase.IsValidFolder(PaletteFolder))
            AssetDatabase.CreateFolder("Assets", "Palettes");

        if (AssetDatabase.LoadAssetAtPath<GameObject>(palettePath) != null)
        {
            if (!EditorUtility.DisplayDialog("Create GameObject Palette",
                    $"{palettePath} already exists. Replace it?", "Replace", "Cancel"))
                return;

            AssetDatabase.DeleteAsset(palettePath);
        }

        GameObject created = GridPaletteUtility.CreateNewPalette(
            PaletteFolder, PaletteName,
            GridLayout.CellLayout.Rectangle,
            GridPalette.CellSizing.Automatic,
            new Vector3(1f, 1f, 0f),
            GridLayout.CellSwizzle.XYZ);

        if (created == null)
        {
            Debug.LogError("GameObjectPaletteCreator: could not create the palette asset.");
            return;
        }

        string path = AssetDatabase.GetAssetPath(created);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        int added = 0;

        try
        {
            Grid grid = root.GetComponent<Grid>();
            Tilemap layer = root.GetComponentInChildren<Tilemap>();
            if (grid == null || layer == null)
            {
                Debug.LogError("GameObjectPaletteCreator: the new palette has no Grid or Tilemap.");
                return;
            }

            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPaths[i]);
                if (prefab == null)
                {
                    Debug.LogWarning($"GameObjectPaletteCreator: missing prefab {PrefabPaths[i]}, skipped.");
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, layer.transform);
                instance.transform.position = grid.GetCellCenterWorld(new Vector3Int(added, 0, 0));
                added++;
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Debug.Log($"GameObjectPaletteCreator: created {path} with {added} prefabs. Open Window > 2D > Tile Palette and pick '{PaletteName}'.");
    }
}
