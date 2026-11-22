using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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
    [SerializeField] private SignalType signalType;
    [SerializeField] private WireDirection connections;
    [SerializeField] private Sprite inactiveSprite;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private GameObject movablePrefab;
    private static Dictionary<string, Sprite> redSprites;

    public SignalType Type => signalType;
    public SignalType TypeOn(WireNetwork network) => signalType == SignalType.Red ||
        (network != null && network.Type == SignalType.Red) ? SignalType.Red : SignalType.Original;
    public WireDirection Connections => connections;
    public Sprite InactiveSprite => inactiveSprite;
    public Sprite ActiveSprite => activeSprite;
    public GameObject MovablePrefab => movablePrefab;

    public Sprite SpriteFor(bool powered, SignalType type)
    {
        var sprite = powered ? activeSprite : inactiveSprite;
        if (type != SignalType.Red || sprite == null) return sprite;
        if (redSprites == null)
        {
            redSprites = new Dictionary<string, Sprite>();
            foreach (var candidate in Resources.LoadAll<Sprite>("RedWires"))
                redSprites[candidate.name] = candidate;
        }
        return redSprites.TryGetValue(sprite.name, out var redSprite) ? redSprite : sprite;
    }

    // Saved Tilemaps contain the tile data cached in the editor, including the edit-mode preview sprite.
    // A player build uses that cache as is, so the preview of every wire / repeater / prefab tile stayed visible
    // at its start cell next to the spawned movable object ("cloned" objects in builds). Refreshing those cells
    // after the scene loads runs GetTileData again, which hides the preview while playing.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneRefresh()
    {
        SceneManager.sceneLoaded -= RefreshSceneWires;
        SceneManager.sceneLoaded += RefreshSceneWires;
    }

    private static void RefreshSceneWires(Scene scene, LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var map in root.GetComponentsInChildren<Tilemap>(true))
            {
                var cells = new List<Vector3Int>();
                foreach (var cell in map.cellBounds.allPositionsWithin)
                {
                    TileBase tile = map.GetTile(cell);
                    if (tile is WireTile || ((tile is RepeaterTile || tile is PrefabTile) && map.GetSprite(cell) != null))
                        cells.Add(cell);
                }
                foreach (var cell in cells) map.RefreshTile(cell);
            }
    }

    public bool Connects(WireDirection direction) => (connections & direction) == direction;

    public static bool AreConnected(WireTile first, WireTile second, Vector3Int offset)
    {
        if (first == null || second == null || first.Type != second.Type) return false;
        if (offset == Vector3Int.up) return first.Connects(WireDirection.Up) && second.Connects(WireDirection.Down);
        if (offset == Vector3Int.right) return first.Connects(WireDirection.Right) && second.Connects(WireDirection.Left);
        if (offset == Vector3Int.down) return first.Connects(WireDirection.Down) && second.Connects(WireDirection.Up);
        if (offset == Vector3Int.left) return first.Connects(WireDirection.Left) && second.Connects(WireDirection.Right);
        return false;
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        var network = tilemap.GetComponent<WireNetwork>();
        tileData.sprite = Application.isPlaying && movablePrefab != null ? null :
            SpriteFor(network != null && network.IsPowered(position), TypeOn(network));
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.gameObject = movablePrefab;
        tileData.flags = TileFlags.LockAll | TileFlags.InstantiateGameObjectRuntimeOnly;
        tileData.colliderType = Tile.ColliderType.None;
    }

    public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
    {
        if (go != null && go.TryGetComponent<MovableWire>(out var wire))
            wire.Initialize(this);
        return true;
    }
}
