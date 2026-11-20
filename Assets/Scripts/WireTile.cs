using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[Flags]
public enum WireDirection
{
    None = 0,
    Up = 1,
    Right = 2,
    Down = 4,
    Left = 8
}

[CreateAssetMenu(menuName = "Tiles/Wire Tile")]
public sealed class WireTile : TileBase
{
    [SerializeField] private WireDirection connections;
    [SerializeField] private Sprite inactiveSprite;
    [SerializeField] private Sprite activeSprite;

    public WireDirection Connections => connections;
    public Sprite InactiveSprite => inactiveSprite;
    public Sprite ActiveSprite => activeSprite;

    public bool Connects(WireDirection direction) => (connections & direction) == direction;

    public static bool AreConnected(WireTile first, WireTile second, Vector3Int offset)
    {
        if (first == null || second == null) return false;
        if (offset == Vector3Int.up) return first.Connects(WireDirection.Up) && second.Connects(WireDirection.Down);
        if (offset == Vector3Int.right) return first.Connects(WireDirection.Right) && second.Connects(WireDirection.Left);
        if (offset == Vector3Int.down) return first.Connects(WireDirection.Down) && second.Connects(WireDirection.Up);
        if (offset == Vector3Int.left) return first.Connects(WireDirection.Left) && second.Connects(WireDirection.Right);
        return false;
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        var network = tilemap.GetComponent<WireNetwork>();
        tileData.sprite = network != null && network.IsPowered(position) ? activeSprite : inactiveSprite;
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockAll;
        tileData.colliderType = Tile.ColliderType.None;
    }
}
