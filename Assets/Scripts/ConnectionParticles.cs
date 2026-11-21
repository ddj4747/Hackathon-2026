using UnityEngine;

/// <summary>
/// Visualises a connection between two endpoints (e.g. repeaters) as single white pixels flying
/// between them in both directions. The ParticleSystem on this object only handles emission, lifetime
/// and rendering; this script places every particle on the straight line between the endpoints each
/// frame, adds a small sideways offset (whole pixels) and snaps it to the pixel grid, so the effect
/// stays pixel-art crisp and follows the endpoints when they move.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
[DisallowMultipleComponent]
public class ConnectionParticles : MonoBehaviour
{
    [Tooltip("First end of the connection.")]
    [SerializeField] private Transform endpointA;

    [Tooltip("Second end of the connection.")]
    [SerializeField] private Transform endpointB;

    [Tooltip("Pixels emitted per second in each direction.")]
    [SerializeField, Min(0f)] private float ratePerDirection = 10f;

    [Tooltip("Flight speed in world units (tiles) per second.")]
    [SerializeField, Min(0.01f)] private float speed = 4f;

    [Tooltip("Emit in both directions (A to B and B to A). Off: only A to B.")]
    [SerializeField] private bool bothDirections = true;

    [Tooltip("Distance from each endpoint centre where pixels appear and disappear (0.5 = edge of a 16 px tile).")]
    [SerializeField, Min(0f)] private float endpointInset = 0.375f;

    [Tooltip("Maximum sideways offset from the line, in whole pixels.")]
    [SerializeField, Range(0, 8)] private int maxSideOffsetPixels = 3;

    [Tooltip("Pixels per world unit (sprite import PPU).")]
    [SerializeField, Min(1)] private int pixelsPerUnit = 16;

    private ParticleSystem system;
    private ParticleSystem.Particle[] buffer;
    private float emitAccumulator;

    public Transform EndpointA => endpointA;
    public Transform EndpointB => endpointB;
    public int PixelsPerUnit => pixelsPerUnit;

    /// <summary>Multiplies the emission rate (e.g. CircuitAura makes beams denser while active). 1 = normal.</summary>
    public float RateMultiplier { get; set; } = 1f;

    /// <summary>False stops new pixels; the ones already flying finish their trip (used to fade a beam out).</summary>
    public bool Emitting { get; set; } = true;

    /// <summary>Sets both ends of the connection at runtime.</summary>
    public void SetEndpoints(Transform a, Transform b)
    {
        endpointA = a;
        endpointB = b;
    }

    private void Awake()
    {
        system = GetComponent<ParticleSystem>();
        ConfigureSystem();
    }

    private void OnEnable()
    {
        emitAccumulator = 0f;
        if (system != null && !system.isPlaying)
            system.Play();
    }

    /// <summary>Forces the ParticleSystem settings this script relies on.</summary>
    private void ConfigureSystem()
    {
        var main = system.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.startSize = 1f / pixelsPerUnit;
        main.startRotation = 0f;
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.loop = true;
        main.playOnAwake = true;
        main.maxParticles = Mathf.Max(main.maxParticles, 256);

        var emission = system.emission;
        emission.enabled = false; // emission is driven from Update

        var shape = system.shape;
        shape.enabled = false;

        buffer = new ParticleSystem.Particle[main.maxParticles];
    }

    private void Update()
    {
        if (!Emitting || !TryGetLine(out Vector3 a, out Vector3 b, out float travel))
        {
            emitAccumulator = 0f;
            return;
        }

        int directions = bothDirections ? 2 : 1;
        emitAccumulator += ratePerDirection * Mathf.Max(0f, RateMultiplier) * directions * Time.deltaTime;
        int count = Mathf.FloorToInt(emitAccumulator);
        if (count <= 0)
            return;
        emitAccumulator -= count;

        var emitParams = new ParticleSystem.EmitParams
        {
            position = a,
            velocity = Vector3.zero,
            startLifetime = travel / speed,
            startSize = 1f / pixelsPerUnit,
            startColor = Color.white,
            applyShapeToPosition = false,
        };

        for (int i = 0; i < count; i++)
        {
            // Bit 0 of the seed: direction. The rest: sideways offset.
            uint seed = (uint)Random.Range(0, int.MaxValue);
            seed = bothDirections ? seed : (seed & ~1u);
            emitParams.randomSeed = seed;
            system.Emit(emitParams, 1);
        }
    }

    private void LateUpdate()
    {
        if (!TryGetLine(out Vector3 a, out Vector3 b, out _))
        {
            if (system.particleCount > 0)
                system.Clear();
            return;
        }

        if (buffer == null || buffer.Length < system.main.maxParticles)
            buffer = new ParticleSystem.Particle[system.main.maxParticles];

        int alive = system.GetParticles(buffer);
        if (alive == 0)
            return;

        Vector3 dir = (b - a).normalized;
        Vector3 side = new Vector3(-dir.y, dir.x, 0f);
        float pixel = 1f / pixelsPerUnit;
        int offsetChoices = maxSideOffsetPixels * 2 + 1;

        for (int i = 0; i < alive; i++)
        {
            ref ParticleSystem.Particle p = ref buffer[i];
            float t = p.startLifetime > 0f ? 1f - p.remainingLifetime / p.startLifetime : 0f;

            bool reverse = (p.randomSeed & 1u) != 0;
            int offset = (int)((p.randomSeed >> 1) % (uint)offsetChoices) - maxSideOffsetPixels;

            Vector3 pos = reverse ? Vector3.Lerp(b, a, t) : Vector3.Lerp(a, b, t);
            pos += side * (offset * pixel);
            p.position = SnapToPixel(pos);
        }

        system.SetParticles(buffer, alive);
    }

    /// <summary>Line between the endpoints, shortened by the inset on both ends.</summary>
    private bool TryGetLine(out Vector3 a, out Vector3 b, out float travel)
    {
        a = b = Vector3.zero;
        travel = 0f;
        if (endpointA == null || endpointB == null)
            return false;

        Vector3 pa = endpointA.position;
        Vector3 pb = endpointB.position;
        pa.z = pb.z = transform.position.z;

        float distance = Vector3.Distance(pa, pb);
        travel = distance - 2f * endpointInset;
        if (travel <= 0.0001f)
            return false;

        Vector3 dir = (pb - pa) / distance;
        a = pa + dir * endpointInset;
        b = pb - dir * endpointInset;
        return true;
    }

    /// <summary>Puts the 1 px quad exactly on a pixel cell (pixel edges at multiples of 1/PPU).</summary>
    private Vector3 SnapToPixel(Vector3 pos)
    {
        float ppu = pixelsPerUnit;
        float half = 0.5f / ppu;
        pos.x = Mathf.Floor(pos.x * ppu) / ppu + half;
        pos.y = Mathf.Floor(pos.y * ppu) / ppu + half;
        return pos;
    }

    private void OnDrawGizmosSelected()
    {
        if (endpointA == null || endpointB == null)
            return;
        Gizmos.color = Color.white;
        Gizmos.DrawLine(endpointA.position, endpointB.position);
    }
}
