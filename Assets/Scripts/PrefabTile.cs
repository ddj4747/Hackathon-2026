using UnityEngine;
using UnityEngine.Tilemaps;

// A palette tile that creates a functional prefab when the level is played.
[CreateAssetMenu(menuName = "Tiles/Prefab Tile")]
public sealed class PrefabTile : TileBase
{
    [SerializeField] private Sprite previewSprite;
    [SerializeField] private Color previewColor = Color.white;
    [SerializeField] private GameObject prefab;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = Application.isPlaying ? null : previewSprite;
        tileData.color = previewColor;
        tileData.gameObject = prefab;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockAll | TileFlags.InstantiateGameObjectRuntimeOnly;
        tileData.colliderType = Tile.ColliderType.None;
    }
}
