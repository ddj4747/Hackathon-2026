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
    private TilemapRenderer tilemapRenderer;
    private Tile indicator;
    private Sprite indicatorSprite;
    private Tile jammerIndicator;
    private Sprite jammerSprite;
    private Material originalMaterial;
    private Material rangeMaterial;
    private bool showingJammer;

    private static readonly Color ActiveOriginal = new Color32(0x38, 0xb7, 0x64, 255);
    private static readonly Color InactiveOriginal = new Color32(0x25, 0x71, 0x79, 255);
    private static readonly Color ActiveRed = new Color32(0xb1, 0x3e, 0x53, 255);
    private static readonly Color InactiveRed = new Color32(0x5d, 0x27, 0x5d, 255);

    private void OnEnable()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
        originalMaterial = tilemapRenderer.sharedMaterial;
        if (rangeMaterial == null)
        {
            var shader = Resources.Load<Shader>("SignalRangeOverlay");
            if (shader != null) rangeMaterial = new Material(shader);
            else Debug.LogError("Signal range overlay shader is missing.", this);
        }
        if (indicator != null) return;

        var white = Texture2D.whiteTexture;
        indicatorSprite = Sprite.Create(white, new Rect(0f, 0f, white.width, white.height),
            new Vector2(0.5f, 0.5f), white.width);
        indicator = ScriptableObject.CreateInstance<Tile>();
        indicator.sprite = indicatorSprite;
        indicator.colliderType = Tile.ColliderType.None;
        indicator.flags = TileFlags.None;
        jammerSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f), 2f);
        jammerIndicator = ScriptableObject.CreateInstance<Tile>();
        jammerIndicator.sprite = jammerSprite;
        jammerIndicator.colliderType = Tile.ColliderType.None;
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
        var jammer = source is SignalJammer;
        if (jammer != showingJammer)
        {
            tilemap.ClearAllTiles();
            displayed.Clear();
            showingJammer = jammer;
        }

        tilemapRenderer.sharedMaterial = jammer || rangeMaterial == null ? originalMaterial : rangeMaterial;
        tilemap.color = jammer ? color : Color.white;
        if (!jammer && rangeMaterial != null)
        {
            var tint = source switch
            {
                Repeater repeater when repeater.Type == SignalType.Red => repeater.IsSignalSupplied ? ActiveRed : InactiveRed,
                Repeater repeater => repeater.IsSignalSupplied ? ActiveOriginal : InactiveOriginal,
                SignalEmitter emitter when emitter.Type == SignalType.Red => ActiveRed,
                _ => ActiveOriginal
            };
            rangeMaterial.SetColor("_OverlayColor", tint);
        }

        desired.Clear();
        if (source != null && source.HasVisibleSignal)
            foreach (var cell in source.RangeCells) desired.Add(cell);

        foreach (var cell in displayed)
            if (!desired.Contains(cell)) tilemap.SetTile(cell, null);
        foreach (var cell in desired)
        {
            if (!displayed.Contains(cell)) tilemap.SetTile(cell, jammer ? jammerIndicator : indicator);
            if (!jammer)
            {
                // The shader uses vertex color as four exposed-edge flags (left, right, bottom, top).
                tilemap.SetColor(cell, new Color(
                    desired.Contains(cell + Vector3Int.left) ? 0f : 1f,
                    desired.Contains(cell + Vector3Int.right) ? 0f : 1f,
                    desired.Contains(cell + Vector3Int.down) ? 0f : 1f,
                    desired.Contains(cell + Vector3Int.up) ? 0f : 1f));
            }
        }

        displayed.Clear();
        displayed.UnionWith(desired);
    }

    private void OnDisable()
    {
        if (tilemap != null) tilemap.ClearAllTiles();
        if (tilemapRenderer != null) tilemapRenderer.sharedMaterial = originalMaterial;
        displayed.Clear();
        desired.Clear();
        showingJammer = false;
    }

    private void OnDestroy()
    {
        Destroy(indicator);
        Destroy(indicatorSprite);
        Destroy(jammerIndicator);
        Destroy(jammerSprite);
        Destroy(rangeMaterial);
    }
}
