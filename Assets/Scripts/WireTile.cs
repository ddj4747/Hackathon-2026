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

    public SignalType Type => signalType;
    public SignalType TypeOn(WireNetwork network) => signalType == SignalType.Red ||
        (network != null && network.Type == SignalType.Red) ? SignalType.Red : SignalType.Original;
    public WireDirection Connections => connections;
    public Sprite InactiveSprite => inactiveSprite;
    public Sprite ActiveSprite => activeSprite;
    public GameObject MovablePrefab => movablePrefab;

    // Saved Tilemaps can contain cached tile data from before movable prefabs were assigned.
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
                    if (map.GetTile(cell) is WireTile) cells.Add(cell);
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
            (network != null && network.IsPowered(position) ? activeSprite : inactiveSprite);
        tileData.color = TypeOn(network) == SignalType.Red ? new Color(1f, 0.55f, 0.55f, 1f) : Color.white;
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
