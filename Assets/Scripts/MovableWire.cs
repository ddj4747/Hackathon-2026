using UnityEngine;
using UnityEngine.Tilemaps;

// Runtime instance created by a WireTile. Pushable owns its logical cell and movement.
[DisallowMultipleComponent]
[RequireComponent(typeof(Pushable), typeof(SpriteRenderer))]
public sealed class MovableWire : MonoBehaviour
{
    public WireTile Tile { get; private set; }
    public Pushable Pushable { get; private set; }
    public Grid Grid { get; private set; }
    private SpriteRenderer spriteRenderer;
    public SignalType Type => Tile != null ? Tile.TypeOn(GetComponentInParent<WireNetwork>()) : SignalType.Original;

    public void Initialize(WireTile tile)
    {
        Tile = tile;
        Pushable = GetComponent<Pushable>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        Grid = GetComponentInParent<Grid>();
        if (Grid == null) Grid = FindAnyObjectByType<Grid>();
        SetPowered(false);
    }

    private void Start()
    {
        if (Grid == null) return;
        // A palette can be painted onto any tilemap. Give it a network if needed.
        foreach (var root in gameObject.scene.GetRootGameObjects())
            foreach (var network in root.GetComponentsInChildren<WireNetwork>())
                if (network.GetComponentInParent<Grid>() == Grid) return;
        var map = GetComponentInParent<Tilemap>();
        if (map != null) map.gameObject.AddComponent<WireNetwork>();
        else
        {
            var holder = new GameObject("Wire Network");
            holder.transform.SetParent(Grid.transform, false);
            holder.AddComponent<Tilemap>();
            holder.AddComponent<WireNetwork>();
        }
    }

    public void SetPowered(bool supplied)
    {
        if (spriteRenderer != null && Tile != null)
        {
            SignalTypeVisual.Restore(spriteRenderer);
            spriteRenderer.sprite = Tile.SpriteFor(supplied, Type);
        }
    }

    private void OnDisable() => SignalTypeVisual.Restore(spriteRenderer);

    public bool HasPort(Vector3Int gridStep)
    {
        if (Tile == null || Grid == null) return false;
        var worldStep = Grid.CellToWorld(gridStep) - Grid.CellToWorld(Vector3Int.zero);
        var local = transform.InverseTransformVector(worldStep).normalized;
        if (Vector3.Distance(local, Vector3.up) < 0.01f) return Tile.Connects(WireDirection.Up);
        if (Vector3.Distance(local, Vector3.right) < 0.01f) return Tile.Connects(WireDirection.Right);
        if (Vector3.Distance(local, Vector3.down) < 0.01f) return Tile.Connects(WireDirection.Down);
        if (Vector3.Distance(local, Vector3.left) < 0.01f) return Tile.Connects(WireDirection.Left);
        return false;
    }
}
