using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tutorial captions written on the floor of a level. Place one in the scene (any GameObject) and fill the hint list
/// in the Inspector. Each hint is drawn at its grid position (between the floor and the objects) and stays there once
/// it has appeared. Hints appear in list order: a hint's trigger is only checked after the previous hint appeared.
/// Text uses the project font (letter height set by Size, in game pixels), with a dark shadow and a typewriter effect.
/// </summary>
public class TutorialHints : MonoBehaviour
{
    public enum Trigger
    {
        [Tooltip("As soon as it is its turn (the first hint: when the level starts).")]
        Immediately,
        [Tooltip("The player stands in the Area (grid cells).")]
        PlayerEntersArea,
        [Tooltip("The player completes any step.")]
        PlayerMoves,
        [Tooltip("The player pushes something.")]
        PlayerPushes,
        [Tooltip("The player undoes a move.")]
        PlayerUndoes,
        [Tooltip("The repeater / antenna / receiver on Cell becomes supplied with signal.")]
        CellSupplied,
    }

    [System.Serializable]
    public class Hint
    {
        [Tooltip("Use Enter for line breaks; rich text <color> tags work.")]
        [TextArea(2, 5)] public string text = "";
        [Tooltip("Grid position of the text centre, in cells (0.5 steps put it on a cell border).")]
        public Vector2 position;
        public Trigger trigger = Trigger.Immediately;
        [Tooltip("Extra wait (seconds) after the trigger before the hint appears.")]
        [Min(0f)] public float delay;
        [Tooltip("PlayerEntersArea: grid cells (x, y = bottom-left cell; width, height in cells).")]
        public RectInt area = new RectInt(0, 0, 1, 1);
        [Tooltip("CellSupplied: grid cell of the repeater, antenna or receiver.")]
        public Vector2Int cell;
    }

    /// <summary>The glyphs are rasterised at this size so small text stays sharp at window resolution;
    /// the world size comes from <see cref="size"/> (TextMesh em height = fontSize * characterSize * 0.1 units).</summary>
    private const int FontSize = 48;
    private const float PixelsPerUnit = 16f;

    [SerializeField] private List<Hint> hints = new List<Hint>();

    [Header("Look")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color shadowColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
    [Tooltip("Letter height in game pixels (a tile is 16). 8 = the font's native pixel size.")]
    [SerializeField, Range(3f, 16f)] private float size = 6f;
    [Tooltip("Sorting order: above the floor and signal overlay (1), below blocks and repeaters.")]
    [SerializeField] private int sortingOrder = 2;
    [Tooltip("Typewriter speed in characters per second; 0 shows the text at once.")]
    [SerializeField, Min(0f)] private float charactersPerSecond = 0f;
    [SerializeField, Min(0.01f)] private float fadeTime = 0.3f;

    [Tooltip("Grid used for positions, areas and cells. Found automatically if empty.")]
    [SerializeField] private Grid grid;

    private class Label
    {
        public TextMesh text;
        public TextMesh shadow;
        public int index;
        public float shownTime;
    }

    private readonly List<Label> labels = new List<Label>();
    private int next;                 // index of the hint waiting for its trigger
    private float pendingTime = -1f;  // when >= 0, the waiting hint appears at this time (delay running)
    private bool moved, pushed, undone;
    private PlayerGridMovement player;
    private Font font;

    /// <summary>Number of hints already shown.</summary>
    public int ShownCount => next;

    private void Awake()
    {
        if (grid == null) grid = FindAnyObjectByType<Grid>();
        font = Resources.Load<Font>("nokiafc22");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void OnDisable()
    {
        if (player != null) player.StepStarted -= OnStepStarted;
        player = null;
    }

    private void Update()
    {
        BindPlayer();

        // Several hints whose triggers are already met appear in the same frame.
        while (next < hints.Count)
        {
            if (pendingTime < 0f && IsTriggered(hints[next]))
                pendingTime = Time.time + hints[next].delay;
            if (pendingTime < 0f || Time.time < pendingTime)
                break;
            Show(next);
        }

        foreach (Label label in labels)
            UpdateLabel(label);
    }

    /// <summary>Shows hint <paramref name="index"/> now (and keeps it); later hints continue from there.</summary>
    public void Show(int index)
    {
        if (index < 0 || index >= hints.Count) return;

        Hint hint = hints[index];
        if (!string.IsNullOrEmpty(hint.text))
            labels.Add(CreateLabel(hint, index));

        next = Mathf.Max(next, index + 1);
        pendingTime = -1f;
        moved = pushed = undone = false;   // the next hint reacts only to actions after this one appeared
    }

    private bool IsTriggered(Hint hint)
    {
        switch (hint.trigger)
        {
            case Trigger.Immediately: return true;
            case Trigger.PlayerMoves: return moved;
            case Trigger.PlayerPushes: return pushed;
            case Trigger.PlayerUndoes: return undone;
            case Trigger.PlayerEntersArea:
                if (player == null || grid == null || player.IsMoving) return false;
                Vector3Int c = grid.WorldToCell(player.transform.position);
                return hint.area.Contains(new Vector2Int(c.x, c.y));
            case Trigger.CellSupplied:
                return IsCellSupplied(new Vector3Int(hint.cell.x, hint.cell.y, 0));
        }
        return false;
    }

    private bool IsCellSupplied(Vector3Int cell)
    {
        if (grid == null) return false;
        foreach (Repeater repeater in FindObjectsByType<Repeater>())
            if (repeater.IsSignalSupplied && grid.WorldToCell(repeater.transform.position) == cell) return true;
        foreach (SignalReceiver receiver in FindObjectsByType<SignalReceiver>())
            if (receiver.IsSignalSupplied && grid.WorldToCell(receiver.transform.position) == cell) return true;
        return false;
    }

    private void BindPlayer()
    {
        if (player != null) return;
        player = FindAnyObjectByType<PlayerGridMovement>();   // a painted player tile spawns at runtime
        if (player != null) player.StepStarted += OnStepStarted;
    }

    private void OnStepStarted(bool pushing)
    {
        if (player != null && player.IsUndoing)
        {
            undone = true;
            return;
        }
        moved = true;
        if (pushing) pushed = true;
    }

    // ---------- Text on the floor ----------

    private Label CreateLabel(Hint hint, int index)
    {
        var root = new GameObject("Hint " + index);
        root.transform.SetParent(transform, false);

        float pixel = size / 8f / PixelsPerUnit;   // shadow offset: one font pixel
        var label = new Label
        {
            index = index,
            shownTime = Time.time,
            shadow = CreateTextMesh(root.transform, "Shadow", new Vector3(pixel, -pixel, 0f), sortingOrder),
            text = CreateTextMesh(root.transform, "Text", Vector3.zero, sortingOrder + 1),
        };
        UpdateLabel(label);
        return label;
    }

    private TextMesh CreateTextMesh(Transform parent, string objectName, Vector3 offset, int order)
    {
        var go = new GameObject(objectName, typeof(MeshRenderer), typeof(TextMesh));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = offset;

        TextMesh mesh = go.GetComponent<TextMesh>();
        mesh.font = font;
        mesh.fontSize = FontSize;
        mesh.characterSize = size / PixelsPerUnit / (FontSize * 0.1f);
        mesh.fontStyle = FontStyle.Normal;   // bold / italic break pixel alignment
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.richText = true;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = font.material;
        renderer.sortingOrder = order;
        return mesh;
    }

    private void UpdateLabel(Label label)
    {
        float age = Time.time - label.shownTime;
        float alpha = Mathf.Clamp01(age / fadeTime);
        if (label.index >= hints.Count) return;
        Hint hint = hints[label.index];   // by index: Inspector edits may replace the Hint objects
        string content = hint.text ?? "";

        // Position and text are read every frame, so Inspector edits in Play mode show up at once.
        label.text.transform.parent.position = WorldPosition(hint.position);

        int shown = charactersPerSecond > 0f
            ? Mathf.Min(content.Length, Mathf.FloorToInt(age * charactersPerSecond))
            : content.Length;

        // The hidden rest stays in the text (transparent) so the centred text does not shift while typing.
        string visible = shown >= content.Length
            ? content
            : content.Substring(0, shown) + "<color=#00000000>" + content.Substring(shown) + "</color>";

        label.text.text = visible;
        label.shadow.text = visible;
        label.text.color = WithAlpha(textColor, alpha);
        label.shadow.color = WithAlpha(shadowColor, alpha);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a *= alpha;
        return color;
    }

    /// <summary>World position of a cell-space point, snapped to the 1/16 pixel grid.</summary>
    private Vector3 WorldPosition(Vector2 cellPosition)
    {
        Vector3 p = grid != null
            ? grid.CellToWorld(Vector3Int.zero) + Vector3.Scale(new Vector3(cellPosition.x + 0.5f, cellPosition.y + 0.5f, 0f), grid.cellSize)
            : new Vector3(cellPosition.x, cellPosition.y, 0f);
        p.x = Mathf.Round(p.x * PixelsPerUnit) / PixelsPerUnit;
        p.y = Mathf.Round(p.y * PixelsPerUnit) / PixelsPerUnit;
        p.z = 0f;
        return p;
    }

    // ---------- Scene view help ----------

    private void OnDrawGizmos()
    {
        if (Application.isPlaying) return;
        Grid g = grid != null ? grid : FindAnyObjectByType<Grid>();
        if (g == null) return;
        grid = grid != null ? grid : g;

        for (int i = 0; i < hints.Count; i++)
        {
            Hint hint = hints[i];
            Vector3 p = WorldPosition(hint.position);
            Gizmos.color = new Color(1f, 1f, 1f, 0.6f);
            Gizmos.DrawWireCube(p, new Vector3(0.25f, 0.25f, 0f));
#if UNITY_EDITOR
            UnityEditor.Handles.Label(p, i + ": " + hint.text);
#endif
        }
    }

    private void OnDrawGizmosSelected()
    {
        Grid g = grid != null ? grid : FindAnyObjectByType<Grid>();
        if (g == null) return;

        for (int i = 0; i < hints.Count; i++)
        {
            Hint hint = hints[i];
            if (hint.trigger == Trigger.PlayerEntersArea)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
                Vector3 a = g.CellToWorld(new Vector3Int(hint.area.xMin, hint.area.yMin, 0));
                Vector3 b = g.CellToWorld(new Vector3Int(hint.area.xMax, hint.area.yMax, 0));
                Gizmos.DrawCube((a + b) * 0.5f, new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), 0.01f));
            }
            else if (hint.trigger == Trigger.CellSupplied)
            {
                Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.8f);
                Gizmos.DrawWireCube(g.GetCellCenterWorld(new Vector3Int(hint.cell.x, hint.cell.y, 0)), g.cellSize);
            }
        }
    }
}
