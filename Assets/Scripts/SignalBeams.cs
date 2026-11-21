using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows a <see cref="ConnectionParticles"/> beam for every wireless link on this Grid: from a transmitting source
/// (an enabled <see cref="SignalEmitter"/> or a supplied <see cref="Repeater"/>) to each repeater or
/// <see cref="SignalReceiver"/> of the same signal type whose cell is inside the source's coverage.
/// One beam per pair (pixels fly both ways).
/// Read-only: it does not change signal propagation. Lost links stop emitting and are removed once their pixels land.
/// Lives on the MapGrid prefab; cables are not shown.
/// </summary>
[DisallowMultipleComponent]
public sealed class SignalBeams : MonoBehaviour
{
    [Tooltip("Beam prefab (ConnectionParticles).")]
    [SerializeField] private ConnectionParticles beamPrefab;

    [Tooltip("Seconds a lost link keeps its already flying pixels before it is removed.")]
    [SerializeField, Min(0f)] private float fadeOutTime = 2.5f;

    [Tooltip("How often the list of receivers in the scene is refreshed (seconds).")]
    [SerializeField, Min(0.1f)] private float receiverRefreshInterval = 1f;

    [Tooltip("Pixel colour of Original signal links (default #38B764, the active Original range overlay colour).")]
    [SerializeField] private Color originalColor = new Color32(0x38, 0xB7, 0x64, 0xFF);

    [Tooltip("Pixel colour of Red signal links (default #B13E53, the active Red range overlay colour).")]
    [SerializeField] private Color redColor = new Color32(0xB1, 0x3E, 0x53, 0xFF);

    private readonly Dictionary<PairKey, ConnectionParticles> beams = new Dictionary<PairKey, ConnectionParticles>();
    private readonly Dictionary<PairKey, (Component a, Component b, SignalType type)> wanted = new Dictionary<PairKey, (Component, Component, SignalType)>();
    private readonly List<PairKey> removeKeys = new List<PairKey>();
    private readonly List<ISignalRangeSource> sources = new List<ISignalRangeSource>();
    private readonly List<Repeater> repeaters = new List<Repeater>();
    private readonly List<SignalReceiver> receivers = new List<SignalReceiver>();
    private Grid grid;
    private float nextReceiverRefresh;

    /// <summary>Beams currently shown (e.g. for <see cref="CircuitAura"/>).</summary>
    public IEnumerable<ConnectionParticles> ActiveBeams => beams.Values;

    private void Awake()
    {
        grid = GetComponent<Grid>();
        if (grid == null)
            grid = GetComponentInParent<Grid>();
    }

    private void OnDisable()
    {
        foreach (var beam in beams.Values)
            if (beam != null) Destroy(beam.gameObject);
        beams.Clear();
    }

    private void LateUpdate()
    {
        if (beamPrefab == null || grid == null)
            return;

        if (Time.unscaledTime >= nextReceiverRefresh)
        {
            nextReceiverRefresh = Time.unscaledTime + receiverRefreshInterval;
            receivers.Clear();
            foreach (var receiver in FindObjectsByType<SignalReceiver>())
                if (receiver.gameObject.scene == gameObject.scene)
                    receivers.Add(receiver);
        }

        CollectSourcesAndRepeaters();
        wanted.Clear();

        foreach (var source in sources)
        {
            var from = (Component)source;
            var sourceType = source is Repeater transmittingRepeater ? transmittingRepeater.Type : ((SignalEmitter)source).Type;
            var cells = source.RangeCells;
            if (cells == null || cells.Count == 0)
                continue;

            foreach (var repeater in repeaters)
                if (repeater != from && repeater.Type == sourceType && Covers(cells, repeater.transform))
                    Want(from, repeater, sourceType);

            foreach (var receiver in receivers)
                if (receiver != null && receiver.isActiveAndEnabled && receiver.Type == sourceType && Covers(cells, receiver.transform))
                    Want(from, receiver, sourceType);
        }

        // Fade out links that are gone.
        removeKeys.Clear();
        foreach (var pair in beams)
            if (!wanted.ContainsKey(pair.Key) || pair.Value == null)
                removeKeys.Add(pair.Key);
        foreach (PairKey key in removeKeys)
        {
            var beam = beams[key];
            beams.Remove(key);
            if (beam == null) continue;
            beam.Emitting = false;
            Destroy(beam.gameObject, fadeOutTime);
        }

        // Create new links.
        foreach (var pair in wanted)
        {
            if (beams.ContainsKey(pair.Key))
                continue;
            var beam = Instantiate(beamPrefab, transform);
            beam.name = "SignalBeam " + pair.Value.a.name + " - " + pair.Value.b.name;
            beam.transform.localPosition = Vector3.zero;
            beam.SetEndpoints(pair.Value.a.transform, pair.Value.b.transform);
            beam.Color = pair.Value.type == SignalType.Red ? redColor : originalColor;
            beams.Add(pair.Key, beam);
        }
    }

    private void CollectSourcesAndRepeaters()
    {
        sources.Clear();
        repeaters.Clear();
        foreach (var source in SignalRangeSources.Active)
        {
            var component = source as Component;
            if (component == null || source.RangeGrid != grid)
                continue;

            if (source is Repeater repeater)
            {
                repeaters.Add(repeater);
                if (repeater.IsSignalSupplied)
                    sources.Add(source);
            }
            else if (source is SignalEmitter emitter && emitter.IsEmittingOn(grid))
            {
                sources.Add(source);   // jammers also have a range but transmit nothing
            }
        }
    }

    private bool Covers(IReadOnlyList<Vector3Int> cells, Transform target)
    {
        var cell = grid.WorldToCell(target.position);
        for (int i = 0; i < cells.Count; i++)
            if (cells[i] == cell) return true;
        return false;
    }

    private void Want(Component a, Component b, SignalType type)
    {
        var key = new PairKey(a, b);
        if (!wanted.ContainsKey(key))
            wanted.Add(key, (a, b, type));
    }

    /// <summary>Unordered pair of endpoints: (a, b) equals (b, a).</summary>
    private readonly struct PairKey : System.IEquatable<PairKey>
    {
        private readonly Component a;
        private readonly Component b;

        public PairKey(Component a, Component b)
        {
            this.a = a;
            this.b = b;
        }

        public bool Equals(PairKey other) =>
            (a == other.a && b == other.b) || (a == other.b && b == other.a);

        public override bool Equals(object obj) => obj is PairKey other && Equals(other);

        public override int GetHashCode() => RuntimeHash(a) ^ RuntimeHash(b);

        private static int RuntimeHash(Component c) =>
            ReferenceEquals(c, null) ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(c);
    }
}
