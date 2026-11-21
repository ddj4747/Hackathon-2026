using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/Repeater Tile")]
public sealed class RepeaterTile : TileBase
{
    [SerializeField] private Sprite previewSprite;
    [SerializeField] private GameObject repeaterPrefab;

    public Sprite PreviewSprite => previewSprite;
    public GameObject RepeaterPrefab => repeaterPrefab;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        // The prefab renders the live state; the tile sprite is only the editing preview.
        tileData.sprite = Application.isPlaying ? null : previewSprite;
        tileData.gameObject = repeaterPrefab;
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockAll;
        tileData.colliderType = Tile.ColliderType.None;
    }
}
