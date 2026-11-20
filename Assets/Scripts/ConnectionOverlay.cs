using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
public sealed class ConnectionOverlay : MonoBehaviour
{
    [SerializeField] private Color color = new(0.2f, 0.85f, 1f, 0.45f);

    private ConnectionManager manager;
    private Tilemap tilemap;
    private Tile indicator;
    private Sprite indicatorSprite;

    private void OnEnable()
    {
        Connect();
    }

    private void Start()
    {
        Connect();
    }

    private void Connect()
    {
        if (manager != null || !gameObject.scene.isLoaded)
            return;

        tilemap = GetComponent<Tilemap>();
        tilemap.color = color;
        if (indicator == null)
        {
            // Make the indicator half a cell wide on the project's one-unit grid.
            var texture = Texture2D.whiteTexture;
            var rect = new Rect(0f, 0f, 1f, 1f);
            indicatorSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 2f);
            indicator = ScriptableObject.CreateInstance<Tile>();
            indicator.sprite = indicatorSprite;
            indicator.colliderType = Tile.ColliderType.None;
        }

        manager = ConnectionManager.ForScene(gameObject.scene);
        manager.SignalChanged += UpdateCell;
        manager.SignalsCleared += ClearTiles;
        foreach (var cell in manager.SignalCells)
            UpdateCell(cell);
    }

    private void OnDisable()
    {
        if (manager == null)
            return;

        manager.SignalChanged -= UpdateCell;
        manager.SignalsCleared -= ClearTiles;
        manager = null;
        ClearTiles();
    }

    private void OnDestroy()
    {
        Destroy(indicator);
        Destroy(indicatorSprite);
    }

    private void UpdateCell(Vector3Int cell)
    {
        var tile = manager.GetSignalQuality(cell) > 0f ? indicator : null;
        if (tilemap.GetTile(cell) != tile)
            tilemap.SetTile(cell, tile);
    }

    private void ClearTiles()
    {
        tilemap.ClearAllTiles();
    }
}
