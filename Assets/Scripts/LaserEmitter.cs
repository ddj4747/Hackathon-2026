using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>A wall-mounted, switchable laser along a cardinal grid direction.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class LaserEmitter : MonoBehaviour
{
    public enum Direction { Left, Up, Right, Down }

    [SerializeField] private Direction direction = Direction.Left;
    [SerializeField, Tooltip("Whether the laser is firing.")] private bool isOn = true;
    [SerializeField] private Sprite activeEmitter;
    [SerializeField] private Sprite inactiveEmitter;
    [SerializeField] private Sprite beamSprite;
    [SerializeField, Tooltip("Pixel sparks emitted along the active beam.")] private ParticleSystem beamParticles;
    [SerializeField, Tooltip("HDR sprite material used by URP Bloom.")] private Material beamMaterial;
    [SerializeField, Min(0f), Tooltip("Beam fade duration in seconds; gameplay switches immediately.")] private float fadeDuration = 0.2f;

    private const int MaxWallDistance = 1024;
    private static readonly List<LaserEmitter> All = new();
    private readonly HashSet<Vector3Int> cells = new();
    private readonly List<SpriteRenderer> segments = new();
    private readonly List<Vector3Int> nextCells = new();
    private Grid grid;
    private SpriteRenderer emitterRenderer;
    private bool warnedMissingWall;
    private int lastRefreshFrame = -1;
    private float beamOpacity;
    private bool beamVisible;
    private Tween fadeTween;
    private ParticleSystem wallParticles;
    private float wallParticleAccumulator;
    private float beamParticleAccumulator;

    /// <summary>Switch the beam on or off without disabling the emitter GameObject.</summary>
    public bool IsOn
    {
        get => isOn;
        set
        {
            if (isOn == value) return;
            isOn = value;
            lastRefreshFrame = -1;
            if (isActiveAndEnabled && Application.isPlaying) Refresh();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => All.Clear();

    private void Awake()
    {
        emitterRenderer = GetComponent<SpriteRenderer>();
        grid = GetComponentInParent<Grid>();
        if (grid == null)
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var candidate in root.GetComponentsInChildren<Grid>())
                {
                    if (grid != null && grid != candidate)
                    {
                        Debug.LogError("LaserEmitter needs a parent Grid when the scene has multiple grids.", this);
                        enabled = false;
                        return;
                    }
                    grid = candidate;
                }
        }
        if (grid == null)
        {
            Debug.LogError("LaserEmitter needs a Grid in its scene.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
        lastRefreshFrame = -1;
    }

    private void OnDisable()
    {
        All.Remove(this);
        cells.Clear();
        fadeTween?.Kill();
        fadeTween = null;
        beamOpacity = 0f;
        beamVisible = false;
        foreach (var segment in segments) if (segment != null) segment.gameObject.SetActive(false);
        StopParticles();
    }

    private void Update()
    {
        Refresh();
        if (!isOn || grid == null) return;
        // Deactivation unregisters each object immediately, including its signal contribution.
        foreach (var cell in cells)
        {
            Pushable item;
            while ((item = Pushable.GetAt(grid, cell)) != null)
            {
                SpawnDestructionParticles(item);
                item.DestroyInLaser();
            }
        }
    }

    public static bool IsBeamAt(Grid targetGrid, Vector3Int cell)
    {
        if (targetGrid == null) return false;
        foreach (var laser in All)
        {
            if (laser == null || !laser.isActiveAndEnabled || laser.grid != targetGrid) continue;
            laser.Refresh();
            if (laser.cells.Contains(cell)) return true;
        }
        return false;
    }

    internal static bool DestroyIfInBeam(Grid targetGrid, Pushable item)
    {
        if (targetGrid == null || item == null) return false;
        foreach (var laser in All)
        {
            if (laser == null || !laser.isActiveAndEnabled || laser.grid != targetGrid) continue;
            laser.Refresh();
            if (!laser.cells.Contains(item.Cell)) continue;
            laser.SpawnDestructionParticles(item);
            item.DestroyInLaser();
            return true;
        }
        return false;
    }

    private void SpawnDestructionParticles(Pushable item)
    {
        if (beamParticles == null) return;
        var position = grid.GetCellCenterWorld(item.Cell);
        position.z = item.transform.position.z;
        var burst = Instantiate(beamParticles, position, grid.transform.rotation);
        burst.name = "Laser destruction sparks";
        burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = burst.main;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.075f, 0.15f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var shape = burst.shape;
        shape.enabled = false;
        var emission = burst.emission;
        emission.enabled = false;
        burst.Play();
        const int count = 36;
        float rotation = Random.Range(0f, Mathf.PI * 2f);
        for (int i = 0; i < count; i++)
        {
            float angle = rotation + i * (Mathf.PI * 2f / count);
            var outward = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            var particle = new ParticleSystem.EmitParams
            {
                position = position + outward * Random.Range(0f, 0.25f),
                velocity = outward * Random.Range(0.9f, 2.2f),
                applyShapeToPosition = false
            };
            if (i % 3 == 0)
                particle.startColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            burst.Emit(particle, 1);
        }
        Destroy(burst.gameObject, 1f);
    }

    private Vector3Int Step => direction switch
    {
        Direction.Up => Vector3Int.up,
        Direction.Right => Vector3Int.right,
        Direction.Down => Vector3Int.down,
        _ => Vector3Int.left
    };

    private void Refresh()
    {
        if (lastRefreshFrame == Time.frameCount) return;
        lastRefreshFrame = Time.frameCount;
        if (emitterRenderer == null) emitterRenderer = GetComponent<SpriteRenderer>();
        emitterRenderer.sprite = isOn ? activeEmitter : inactiveEmitter;
        cells.Clear();
        nextCells.Clear();
        if (isOn && grid != null)
        {
            Vector3Int origin = grid.WorldToCell(transform.position);
            Vector3Int step = Step;
            bool foundWall = false;
            for (int i = 1; i <= MaxWallDistance; i++)
            {
                var cell = origin + step * i;
                if (DualGridLayer.IsWall(grid, cell) || Solid.IsAt(grid, cell))
                {
                    foundWall = true;
                    break;
                }
                nextCells.Add(cell);
            }
            if (foundWall)
            {
                warnedMissingWall = false;
                cells.Add(origin);
                foreach (var cell in nextCells) cells.Add(cell);
                DrawBeam(origin, step);
                return;
            }
            if (!warnedMissingWall)
            {
                Debug.LogWarning("LaserEmitter found no wall within " + MaxWallDistance + " cells; beam is off.", this);
                warnedMissingWall = true;
            }
        }
        FadeBeam(false);
        StopParticles();
    }

    private void DrawBeam(Vector3Int origin, Vector3Int step)
    {
        // The nozzle occupies the back quarter of its cell. Fill the rest up to the cell edge.
        int count = nextCells.Count + 1;
        for (int i = 0; i < count; i++)
        {
            if (i == segments.Count)
            {
                var child = new GameObject("Laser beam");
                child.hideFlags = HideFlags.DontSave;
                child.transform.SetParent(transform, false);
                segments.Add(child.AddComponent<SpriteRenderer>());
            }
            var renderer = segments[i];
            renderer.gameObject.SetActive(true);
            renderer.sprite = beamSprite;
            if (beamMaterial != null) renderer.sharedMaterial = beamMaterial;
            var color = renderer.color;
            color.a = beamOpacity;
            renderer.color = color;
            renderer.sortingLayerID = emitterRenderer.sortingLayerID;
            renderer.sortingOrder = emitterRenderer.sortingOrder - 1;
            var center = grid.GetCellCenterWorld(i == 0 ? origin : nextCells[i - 1]);
            center.z = transform.position.z;
            if (i == 0) center += (grid.CellToWorld(step) - grid.CellToWorld(Vector3Int.zero)) * 0.125f;
            renderer.transform.position = center;
            renderer.transform.rotation = grid.transform.rotation;
            renderer.transform.localScale = i == 0
                ? (step.x != 0 ? new Vector3(0.75f, 1f, 1f) : new Vector3(1f, 0.75f, 1f))
                : Vector3.one;
        }
        HideSegmentsFrom(count);
        FadeBeam(true);
        UpdateParticles(origin, step);
    }

    private void FadeBeam(bool visible)
    {
        if (beamVisible == visible) return;
        beamVisible = visible;
        fadeTween?.Kill();
        float target = visible ? 1f : 0f;
        if (fadeDuration <= 0f)
        {
            SetBeamOpacity(target);
            if (!visible) HideBeamVisuals();
            return;
        }
        fadeTween = DOTween.To(() => beamOpacity, SetBeamOpacity, target, fadeDuration)
            .SetEase(Ease.Linear).SetTarget(this).OnComplete(() =>
            {
                fadeTween = null;
                if (!beamVisible) HideBeamVisuals();
            });
    }

    private void SetBeamOpacity(float value)
    {
        beamOpacity = value;
        foreach (var segment in segments)
        {
            if (segment == null) continue;
            var color = segment.color;
            color.a = value;
            segment.color = color;
        }
    }

    private void HideBeamVisuals()
    {
        HideSegmentsFrom(0);
    }

    private void UpdateParticles(Vector3Int origin, Vector3Int step)
    {
        if (beamParticles == null) return;
        Vector3 cellStep = grid.CellToWorld(step) - grid.CellToWorld(Vector3Int.zero);
        Vector3 start = grid.GetCellCenterWorld(origin) + cellStep * 0.25f;
        Vector3 end = grid.GetCellCenterWorld(origin + step * (nextCells.Count + 1)) - cellStep * 0.5f;
        start.z = end.z = transform.position.z;
        beamParticles.transform.position = (start + end) * 0.5f;
        beamParticles.transform.rotation = grid.transform.rotation;
        float length = Vector3.Distance(start, end);
        var main = beamParticles.main;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.075f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var shape = beamParticles.shape;
        shape.enabled = false;
        var emission = beamParticles.emission;
        emission.enabled = false;
        var renderer = beamParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerID = emitterRenderer.sortingLayerID;
        renderer.sortingOrder = emitterRenderer.sortingOrder;
        if (!beamParticles.isPlaying) beamParticles.Play();
        beamParticleAccumulator += Mathf.Max(2f, length * 3f) * Time.deltaTime;
        int count = Mathf.FloorToInt(beamParticleAccumulator);
        beamParticleAccumulator -= count;
        Vector3 along = cellStep.normalized;
        Vector3 sideways = new Vector3(-along.y, along.x, 0f);
        for (int i = 0; i < count; i++)
        {
            float sign = Random.value < 0.5f ? -1f : 1f;
            var particle = new ParticleSystem.EmitParams
            {
                position = Vector3.Lerp(start, end, Random.value) + sideways * (sign * Random.Range(0.04f, 0.12f)),
                velocity = along * Random.Range(0.6f, 1.2f),
                applyShapeToPosition = false
            };
            beamParticles.Emit(particle, 1);
        }
        UpdateWallParticles(end, cellStep);
    }

    private void UpdateWallParticles(Vector3 position, Vector3 cellStep)
    {
        if (wallParticles == null)
        {
            wallParticles = Instantiate(beamParticles, transform);
            wallParticles.name = "Laser wall sparks";
            wallParticles.gameObject.hideFlags = HideFlags.DontSave;
            wallParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = wallParticles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            var shape = wallParticles.shape;
            shape.enabled = false;
            var emission = wallParticles.emission;
            emission.enabled = false;
        }

        var renderer = wallParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerID = emitterRenderer.sortingLayerID;
        renderer.sortingOrder = emitterRenderer.sortingOrder;
        if (!wallParticles.isPlaying) wallParticles.Play();
        wallParticleAccumulator += 48f * Time.deltaTime;
        int count = Mathf.FloorToInt(wallParticleAccumulator);
        wallParticleAccumulator -= count;
        Vector3 back = -cellStep.normalized;
        Vector3 sideways = new Vector3(-back.y, back.x, 0f);
        for (int i = 0; i < count; i++)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            var particle = new ParticleSystem.EmitParams
            {
                position = position + back * 0.02f + sideways * (side * Random.Range(0.15f, 0.25f)),
                velocity = back * Random.Range(0.5f, 1.5f) + sideways * Random.Range(-1.4f, 1.4f),
                applyShapeToPosition = false
            };
            wallParticles.Emit(particle, 1);
        }
    }

    private void StopParticles()
    {
        if (beamParticles != null)
            beamParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (wallParticles != null)
            wallParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        wallParticleAccumulator = 0f;
        beamParticleAccumulator = 0f;
    }

    private void HideSegmentsFrom(int index)
    {
        for (int i = index; i < segments.Count; i++)
            if (segments[i] != null) segments[i].gameObject.SetActive(false);
    }
}
