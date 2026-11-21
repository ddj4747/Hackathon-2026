using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Terrain of one logical cell of a <see cref="DualGridLayer"/>. Values are used in the corner codes.</summary>
public enum DualGridTerrain
{
    Floor = 0,
    Wall = 1,
    Water = 2,
}

/// <summary>
/// Tile painted on a DualGridLayer's logical tilemap. It only says which terrain the cell is; its sprite is a
/// palette preview and is drawn only as the faint editor guide. Any other tile counts as Floor, an empty cell as Wall.
/// </summary>
[CreateAssetMenu(fileName = "DualGridTerrainTile", menuName = "2D/Tiles/Dual Grid Terrain Tile")]
public class DualGridTerrainTile : Tile
{
    [Tooltip("What this tile paints on a DualGridLayer.")]
    public DualGridTerrain terrain = DualGridTerrain.Floor;
}
