using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// White soft-glow aura around a whole circuit (input, repeaters, output and the beams between
/// them), meant to play when the connection is completed. Call <see cref="Play"/> / <see cref="Stop"/>
/// from the circuit logic; nothing triggers it automatically (except <c>playOnStart</c>, for testing).
/// For every sprite it creates a padded quad behind it using the CircuitGlowSprite shader; for every
/// <see cref="ConnectionParticles"/> beam a glow band (CircuitGlowBeam shader) and denser particles.
/// The aura fades in, then pulses until stopped, then fades out and removes its glow objects.
/// </summary>
[DisallowMultipleComponent]
public class CircuitAura : MonoBehaviour
{
    [Header("Circuit")]
    [Tooltip("Sprites that get a glow (input, repeaters, output...).")]
    [SerializeField] private List<SpriteRenderer> sprites = new List<SpriteRenderer>();

    [Tooltip("Beams between the circuit parts. They get a glow band and denser particles.")]
    [SerializeField] private List<ConnectionParticles> beams = new List<ConnectionParticles>();

    [Header("Materials")]
    [SerializeField] private Material spriteGlowMaterial;
    [SerializeField] private Material beamGlowMaterial;

    [Header("Look")]
    [SerializeField] private Color color = Color.white;

    [Tooltip("Glow radius around sprites, in sprite pixels.")]
    [SerializeField, Range(0.5f, 12f)] private float spriteGlowRadius = 4f;

    [Tooltip("Half width of the glow band along beams, in pixels.")]
    [SerializeField, Range(0.5f, 16f)] private float beamGlowHalfWidth = 5f;

    [Tooltip("Particle rate multiplier on the beams while the aura is on.")]
    [SerializeField, Min(0f)] private float beamRateMultiplier = 2.5f;

    [Tooltip("Sorting order of the beam glow bands (keep below the sprites).")]
    [SerializeField] private int beamGlowSortingOrder = -2;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float fadeInTime = 0.4f;
    [SerializeField, Min(0f)] private float fadeOutTime = 0.3f;
    [Tooltip("Pulses per second after the fade in.")]
    [SerializeField, Min(0f)] private float pulseSpeed = 1.2f;
    [SerializeField, Range(0f, 2f)] private float pulseMin = 0.65f;
    [SerializeField, Range(0f, 2f)] private float pulseMax = 1f;
    [Tooltip("Ignore Time.timeScale (keeps animating if the game is paused on win).")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Testing")]
    [SerializeField] private bool playOnStart;

    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int ColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int RadiusId = Shader.PropertyToID("_RadiusPx");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int SpriteRectId = Shader.PropertyToID("_SpriteRectPx");
    private static readonly int TexSizeId = Shader.PropertyToID("_TexSizePx");
    private static readonly int HalfWidthId = Shader.PropertyToID("_HalfWidthPx");
    private static readonly int PpuId = Shader.PropertyToID("_PixelsPerUnit");
    private static readonly int LineAId = Shader.PropertyToID("_LineA");
    private static readonly int LineBId = Shader.PropertyToID("_LineB");

    private class SpriteGlow
    {
        public SpriteRenderer source;
        public GameObject go;
        public MeshRenderer renderer;
        public Mesh mesh;
        public Sprite builtFor;
        public bool builtFlipX, builtFlipY;
    }

    private class BeamGlow
    {
        public ConnectionParticles source;
        public GameObject go;
        public MeshRenderer renderer;
    }

    private readonly List<SpriteGlow> spriteGlows = new List<SpriteGlow>();
    private readonly List<BeamGlow> beamGlows = new List<BeamGlow>();
    private MaterialPropertyBlock block;
    private Mesh unitQuad;

    private enum State { Off, FadingIn, On, FadingOut }
    private State state = State.Off;
    private float level;      // 0..1 fade
    private float pulseTime;

    public bool IsPlaying => state == State.FadingIn || state == State.On;

    /// <summary>Replaces the circuit parts. Takes effect immediately if the aura is playing.</summary>
    public void SetCircuit(IEnumerable<SpriteRenderer> circuitSprites, IEnumerable<ConnectionParticles> circuitBeams)
    {
        bool wasVisible = state != State.Off;
        DestroyGlows();
        sprites = new List<SpriteRenderer>(circuitSprites ?? new SpriteRenderer[0]);
        beams = new List<ConnectionParticles>(circuitBeams ?? new ConnectionParticles[0]);
        if (wasVisible)
            CreateGlows();
    }

    /// <summary>Shows the aura (fade in, then pulse).</summary>
    [ContextMenu("Play")]
    public void Play()
    {
        if (state == State.Off)
        {
            CreateGlows();
            level = 0f;
            pulseTime = 0f;
        }
        state = State.FadingIn;
        SetBeamMultiplier(beamRateMultiplier);
    }

    /// <summary>Hides the aura (fade out, then the glow objects are removed).</summary>
    [ContextMenu("Stop")]
    public void Stop()
    {
        if (state == State.Off)
            return;
        state = State.FadingOut;
        SetBeamMultiplier(1f);
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    private void OnDisable()
    {
        SetBeamMultiplier(1f);
        DestroyGlows();
        state = State.Off;
        level = 0f;
    }

    private void OnDestroy()
    {
        if (unitQuad != null)
            Destroy(unitQuad);
    }

    private void LateUpdate()
    {
        if (state == State.Off)
            return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        switch (state)
        {
            case State.FadingIn:
                level = fadeInTime > 0f ? Mathf.MoveTowards(level, 1f, dt / fadeInTime) : 1f;
                if (level >= 1f)
                    state = State.On;
                break;
            case State.FadingOut:
                level = fadeOutTime > 0f ? Mathf.MoveTowards(level, 0f, dt / fadeOutTime) : 0f;
                if (level <= 0f)
                {
                    DestroyGlows();
                    state = State.Off;
                    return;
                }
                break;
        }

        float pulse = pulseMax;
        if (state == State.On || state == State.FadingOut)
        {
            pulseTime += dt;
            float wave = 0.5f + 0.5f * Mathf.Cos(pulseTime * pulseSpeed * 2f * Mathf.PI);
            pulse = Mathf.Lerp(pulseMin, pulseMax, wave);
        }

        float intensity = level * pulse;
        UpdateSpriteGlows(intensity);
        UpdateBeamGlows(intensity);
    }

    private void CreateGlows()
    {
        if (block == null)
            block = new MaterialPropertyBlock();

        foreach (SpriteRenderer sr in sprites)
        {
            if (sr == null || spriteGlowMaterial == null)
                continue;
            var go = new GameObject("CircuitAuraGlow");
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(sr.transform, false);
            var mesh = new Mesh { name = "CircuitAuraGlowQuad", hideFlags = HideFlags.DontSave };
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = spriteGlowMaterial;
            spriteGlows.Add(new SpriteGlow { source = sr, go = go, renderer = mr, mesh = mesh });
        }

        foreach (ConnectionParticles beam in beams)
        {
            if (beam == null || beamGlowMaterial == null)
                continue;
            var go = new GameObject("CircuitAuraBeamGlow");
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = GetUnitQuad();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = beamGlowMaterial;
            beamGlows.Add(new BeamGlow { source = beam, go = go, renderer = mr });
        }
    }

    private void DestroyGlows()
    {
        foreach (SpriteGlow g in spriteGlows)
        {
            if (g.go != null) Destroy(g.go);
            if (g.mesh != null) Destroy(g.mesh);
        }
        foreach (BeamGlow g in beamGlows)
        {
            if (g.go != null) Destroy(g.go);
        }
        spriteGlows.Clear();
        beamGlows.Clear();
    }

    private void UpdateSpriteGlows(float intensity)
    {
        foreach (SpriteGlow g in spriteGlows)
        {
            if (g.source == null || g.go == null)
                continue;

            Sprite sprite = g.source.sprite;
            bool visible = sprite != null && g.source.enabled && g.source.gameObject.activeInHierarchy;
            g.renderer.enabled = visible;
            if (!visible)
                continue;

            if (sprite != g.builtFor || g.source.flipX != g.builtFlipX || g.source.flipY != g.builtFlipY)
                BuildSpriteQuad(g, sprite);

            g.renderer.sortingLayerID = g.source.sortingLayerID;
            g.renderer.sortingOrder = g.source.sortingOrder - 1;

            Rect rect = sprite.textureRect;
            g.renderer.GetPropertyBlock(block);
            block.Clear();
            block.SetTexture(MainTexId, sprite.texture);
            block.SetVector(SpriteRectId, new Vector4(rect.x, rect.y, rect.width, rect.height));
            block.SetVector(TexSizeId, new Vector4(sprite.texture.width, sprite.texture.height, 0f, 0f));
            block.SetColor(ColorId, color);
            block.SetFloat(RadiusId, spriteGlowRadius);
            block.SetFloat(IntensityId, intensity);
            g.renderer.SetPropertyBlock(block);
        }
    }

    /// <summary>Quad covering the sprite rect plus padding; UVs are sprite pixel coordinates.</summary>
    private void BuildSpriteQuad(SpriteGlow g, Sprite sprite)
    {
        float ppu = sprite.pixelsPerUnit;
        Rect rect = sprite.textureRect;
        float pad = Mathf.Ceil(spriteGlowRadius * 2f) + 1f; // Gaussian tail ~2 sigma past the radius

        // Local position of the rect's bottom-left corner (pivot is in pixels from that corner).
        Vector2 min = -sprite.pivot / ppu;
        Vector2 size = rect.size / ppu;
        float padW = pad / ppu;

        float x0 = min.x - padW, x1 = min.x + size.x + padW;
        float y0 = min.y - padW, y1 = min.y + size.y + padW;
        if (g.source.flipX) { float t = -x0; x0 = -x1; x1 = t; }
        if (g.source.flipY) { float t = -y0; y0 = -y1; y1 = t; }

        float u0 = -pad, u1 = rect.width + pad;
        float v0 = -pad, v1 = rect.height + pad;
        if (g.source.flipX) { float t = u0; u0 = u1; u1 = t; }
        if (g.source.flipY) { float t = v0; v0 = v1; v1 = t; }

        g.mesh.Clear();
        g.mesh.vertices = new[]
        {
            new Vector3(x0, y0, 0f), new Vector3(x1, y0, 0f),
            new Vector3(x0, y1, 0f), new Vector3(x1, y1, 0f),
        };
        g.mesh.uv = new[]
        {
            new Vector2(u0, v0), new Vector2(u1, v0),
            new Vector2(u0, v1), new Vector2(u1, v1),
        };
        g.mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        g.mesh.RecalculateBounds();

        g.builtFor = sprite;
        g.builtFlipX = g.source.flipX;
        g.builtFlipY = g.source.flipY;
    }

    private void UpdateBeamGlows(float intensity)
    {
        foreach (BeamGlow g in beamGlows)
        {
            if (g.source == null || g.go == null)
                continue;

            Transform a = g.source.EndpointA, b = g.source.EndpointB;
            bool visible = a != null && b != null && g.source.isActiveAndEnabled;
            g.renderer.enabled = visible;
            if (!visible)
                continue;

            float ppu = g.source.PixelsPerUnit;
            Vector3 pa = a.position, pb = b.position;
            Vector3 delta = pb - pa;
            delta.z = 0f;
            float length = delta.magnitude;
            float pad = (beamGlowHalfWidth * 2f + 1f) / ppu;

            Transform t = g.go.transform;
            t.position = new Vector3((pa.x + pb.x) * 0.5f, (pa.y + pb.y) * 0.5f, g.source.transform.position.z);
            t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(length + 2f * pad, 2f * pad, 1f);

            g.renderer.sortingLayerID = g.source.GetComponent<Renderer>().sortingLayerID;
            g.renderer.sortingOrder = beamGlowSortingOrder;

            g.renderer.GetPropertyBlock(block);
            block.Clear();
            block.SetColor(ColorId, color);
            block.SetFloat(IntensityId, intensity);
            block.SetFloat(HalfWidthId, beamGlowHalfWidth);
            block.SetFloat(PpuId, ppu);
            block.SetVector(LineAId, pa);
            block.SetVector(LineBId, pb);
            g.renderer.SetPropertyBlock(block);
        }
    }

    private void SetBeamMultiplier(float multiplier)
    {
        foreach (ConnectionParticles beam in beams)
        {
            if (beam != null)
                beam.RateMultiplier = multiplier;
        }
    }

    private Mesh GetUnitQuad()
    {
        if (unitQuad != null)
            return unitQuad;
        unitQuad = new Mesh { name = "CircuitAuraUnitQuad", hideFlags = HideFlags.DontSave };
        unitQuad.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f),
        };
        unitQuad.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
        unitQuad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        unitQuad.RecalculateBounds();
        return unitQuad;
    }
}
