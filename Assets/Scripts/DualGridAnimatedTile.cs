using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// One whole dual-grid tile, created at runtime by <see cref="DualGridLayer"/> (never saved as an asset).
/// One sprite = static; several sprites = looping tile animation.
/// </summary>
public class DualGridAnimatedTile : TileBase
{
    public Sprite[] frames;

    /// <summary>Animation frames per second (Tilemap.animationFrameRate is kept at 1).</summary>
    public float speed = 1f;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockAll;
        tileData.colliderType = Tile.ColliderType.None;
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        if (frames == null || frames.Length < 2)
            return false;

        tileAnimationData.animatedSprites = frames;
        tileAnimationData.animationSpeed = speed;
        tileAnimationData.animationStartTime = 0f;
        return true;
    }
}
