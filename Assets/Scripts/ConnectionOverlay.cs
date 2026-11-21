using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public interface ISignalRangeSource
{
    Grid RangeGrid { get; }
    IReadOnlyList<Vector3Int> RangeCells { get; }
    bool HasVisibleSignal { get; }
}

internal static class SignalRangeSources
{
    internal static readonly HashSet<ISignalRangeSource> Active = new();
    internal static void Add(ISignalRangeSource source) => Active.Add(source);
    internal static void Remove(ISignalRangeSource source) => Active.Remove(source);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => Active.Clear();
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
public sealed class ConnectionOverlay : MonoBehaviour
{
    [SerializeField] private Color color = new(0.2f, 0.85f, 1f, 0.45f);

    private readonly HashSet<Vector3Int> displayed = new();
    private readonly HashSet<Vector3Int> desired = new();
    private Tilemap tilemap;
    private Tile indicator;
    private Sprite indicatorSprite;

    private void OnEnable()
    {
        tilemap = GetComponent<Tilemap>();
        tilemap.color = color;
        if (indicator != null) return;

        indicatorSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f), 2f);
        indicator = ScriptableObject.CreateInstance<Tile>();
        indicator.sprite = indicatorSprite;
        indicator.colliderType = Tile.ColliderType.None;
    }

    private void LateUpdate()
    {
        var camera = Camera.main;
        var mouse = Mouse.current;
        var grid = GetComponentInParent<Grid>();
        ISignalRangeSource hovered = null;
        if (camera != null && mouse != null && grid != null)
        {
            var screenPosition = mouse.position.ReadValue();
            if (camera.pixelRect.Contains(screenPosition))
                hovered = FindHoveredSource(grid, camera.ScreenPointToRay(screenPosition));
        }
        ShowCoverage(hovered);
    }

    private ISignalRangeSource FindHoveredSource(Grid grid, Ray ray)
    {
        ISignalRangeSource selected = null;
        var selectedLayer = int.MinValue;
        var selectedOrder = int.MinValue;
        foreach (var source in SignalRangeSources.Active)
        {
            if (source.RangeGrid != grid || source is not MonoBehaviour behaviour ||
                behaviour.gameObject.scene != gameObject.scene) continue;
            var renderer = behaviour.GetComponent<SpriteRenderer>();
            if (renderer == null || !renderer.enabled || renderer.sprite == null) continue;

            var plane = new Plane(grid.transform.forward, behaviour.transform.position);
            if (!plane.Raycast(ray, out var distance)) continue;
            var point = ray.GetPoint(distance);
            var bounds = renderer.bounds;
            if (point.x < bounds.min.x || point.x > bounds.max.x ||
                point.y < bounds.min.y || point.y > bounds.max.y) continue;
            var layer = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
            var order = renderer.sortingOrder;
            if (selected != null && (layer < selectedLayer || (layer == selectedLayer && order <= selectedOrder))) continue;
            selected = source;
            selectedLayer = layer;
            selectedOrder = order;
        }
        return selected;
    }

    private void ShowCoverage(ISignalRangeSource source)
    {
        desired.Clear();
        if (source != null && source.HasVisibleSignal)
            foreach (var cell in source.RangeCells) desired.Add(cell);

        foreach (var cell in displayed)
            if (!desired.Contains(cell)) tilemap.SetTile(cell, null);
        foreach (var cell in desired)
            if (!displayed.Contains(cell)) tilemap.SetTile(cell, indicator);

        displayed.Clear();
        displayed.UnionWith(desired);
    }

    private void OnDisable()
    {
        if (tilemap != null) tilemap.ClearAllTiles();
        displayed.Clear();
        desired.Clear();
    }

    private void OnDestroy()
    {
        Destroy(indicator);
        Destroy(indicatorSprite);
    }
}
