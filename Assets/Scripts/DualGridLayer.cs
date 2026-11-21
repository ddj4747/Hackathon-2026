using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Dual grid for the floor, the walls and the water.
///
/// The Tilemap on this object is the LOGICAL layer: you paint terrain on it. A <see cref="DualGridTerrainTile"/>
/// paints its terrain (Floor / Wall / Water), any other tile paints Floor, an empty cell is Wall. The logical layer
/// is drawn only as a faint editor guide. The visible art is drawn on a hidden child Tilemap shifted by half a cell:
/// every corner of the logical grid gets the ONE tileset tile whose four corners match the four cells around it.
/// Tiles are never assembled from pieces: a combination no tileset has is left empty, marked in the Scene view and
/// reported in the console.
///
/// Tilesets: square tiles (tileSize px) in rows and columns; animation frames side by side, left to right, each frame
/// the same width. Which combination a tile is, is read from its four corner pixels in frame 0 (nearest of the floor,
/// wall and water colours), so tiles can be in any order and new rows / columns are picked up automatically.
/// Frame 0 is held longer (stillFrameHold), the other frames play once each. Textures need Read/Write enabled.
///
/// Everything else in the game (player, blocks, Terrain tilemap...) keeps using the normal Grid; this layer does not
/// block movement.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap))]
public class DualGridLayer : MonoBehaviour
{
    [System.Serializable]
    public class Sheet
    {
        [Tooltip("Tileset texture. Import with Read/Write on, Filter Mode Point, Compression None.")]
        public Texture2D texture;

        [Tooltip("Animation frames placed side by side, left to right (1 = not animated).")]
        [Min(1)] public int frames = 1;
    }

    private const string DisplayName = "~DualGridDisplay";
    private const int ComboCount = 81;          // 3 terrains ^ 4 corners
    private const string TerrainLetters = "FWA"; // index = DualGridTerrain value
    private static readonly int[] Pow3 = { 1, 3, 9, 27 };

    // Logical cells around a corner (x,y), in corner order TL, TR, BL, BR.
    private static readonly Vector3Int[] CornerOffsets =
    {
        new Vector3Int(-1,  0, 0),
        new Vector3Int( 0,  0, 0),
        new Vector3Int(-1, -1, 0),
        new Vector3Int( 0, -1, 0),
    };

    [Header("Tilesets")]
    [Tooltip("Searched in order; if two tiles have the same corners, the first one is used.")]
    [SerializeField] private List<Sheet> tilesets = new List<Sheet>();

    [Tooltip("Size of one tile in pixels. One tile = one cell of the grid.")]
    [SerializeField, Min(2)] private int tileSize = 16;

    [Tooltip("Colours used to recognise the corners of each tile (corner pixels of frame 0).")]
    [SerializeField] private Color floorColor = new Color32(26, 28, 44, 255);
    [SerializeField] private Color wallColor = new Color32(51, 60, 87, 255);
    [SerializeField] private Color waterColor = new Color32(41, 54, 111, 255);

    [Header("Paint tiles (used by SetTerrain from code)")]
    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase waterTile;

    [Header("Look")]
    [Tooltip("Sorting order of the visible tiles. Keep it below the player and the objects.")]
    [SerializeField] private int sortingOrder = -10;

    [Tooltip("Plain wall drawn this many cells around everything you paint, so no empty background shows next to the level.")]
    [SerializeField, Min(0)] private int margin = 3;

    [Header("Animation")]
    [Tooltip("Seconds per animation frame.")]
    [SerializeField, Min(0.01f)] private float frameDuration = 0.15f;

    [Tooltip("How many frames the first frame (no outline) lasts. 4 = four times longer than the others.")]
    [SerializeField, Min(1)] private int stillFrameHold = 4;

    [Header("Editor")]
    [Tooltip("In the editor, show the painted cells as a faint overlay. Never shown in Play mode.")]
    [SerializeField] private bool showGuideInEditor = true;

    private readonly TileBase[] comboTiles = new TileBase[ComboCount];   // null = no tileset has this combination
    private readonly List<string> duplicateTiles = new List<string>();
    private readonly List<Object> createdObjects = new List<Object>();
    private readonly HashSet<Vector3Int> pending = new HashSet<Vector3Int>();
    private readonly HashSet<Vector3Int> missingCorners = new HashSet<Vector3Int>();
    private readonly List<Vector3Int> batchPositions = new List<Vector3Int>();
    private readonly List<TileBase> batchTiles = new List<TileBase>();

    private Tilemap dataTilemap;
    private Tilemap displayTilemap;
    private bool needsRebuild;

    /// <summary>The logical layer (one tile per painted cell).</summary>
    public Tilemap Data => dataTilemap != null ? dataTilemap : (dataTilemap = GetComponent<Tilemap>());

    /// <summary>Terrain of a logical cell: DualGridTerrainTile = its terrain, other tile = Floor, empty = Wall.</summary>
    public DualGridTerrain GetTerrain(Vector3Int cell)
    {
        TileBase tile = Data.GetTile(cell);
        if (tile == null)
            return DualGridTerrain.Wall;
        if (tile is DualGridTerrainTile terrainTile)
            return terrainTile.terrain;
        return DualGridTerrain.Floor;
    }

    /// <summary>Paints a cell from code with the floor / water tile assigned in the Inspector (Wall clears the cell).</summary>
    public void SetTerrain(Vector3Int cell, DualGridTerrain terrain)
    {
        TileBase tile = null;
        if (terrain == DualGridTerrain.Floor)
            tile = floorTile;
        else if (terrain == DualGridTerrain.Water)
            tile = waterTile;

        if (terrain != DualGridTerrain.Wall && tile == null)
        {
            Debug.LogError("DualGridLayer: no paint tile assigned for " + terrain + ".", this);
            return;
        }

        Data.SetTile(cell, tile);
        AddAffected(cell);
        ApplyAffected();
    }

    private void OnEnable()
    {
        dataTilemap = GetComponent<Tilemap>();

        // The logical layer is never drawn in the game; in the editor it can be a faint guide just above the art.
        TilemapRenderer dataRenderer = GetComponent<TilemapRenderer>();
        if (dataRenderer != null)
        {
            bool guide = showGuideInEditor && !Application.isPlaying;
            if (dataRenderer.enabled != guide)
                dataRenderer.enabled = guide;
            if (guide)
            {
                Color guideColor = new Color(1f, 1f, 1f, 0.4f);
                if (dataTilemap.color != guideColor)
                    dataTilemap.color = guideColor;
                if (dataRenderer.sortingOrder != sortingOrder + 1)
                    dataRenderer.sortingOrder = sortingOrder + 1;
            }
        }

        Tilemap.tilemapTileChanged += OnTilemapTileChanged;
        Rebuild();
    }

    private void OnDisable()
    {
        Tilemap.tilemapTileChanged -= OnTilemapTileChanged;
        DestroyDisplay();
        ReleaseCreated();
    }

    private void OnValidate()
    {
        needsRebuild = true;
    }

    private void Update()
    {
        if (needsRebuild)
        {
            needsRebuild = false;
            Rebuild();
            return;
        }

#if UNITY_EDITOR
        // Safety net in the editor: redraw if the painted cells changed without a tile-changed event.
        if (!Application.isPlaying && displayTilemap != null && ComputeDataHash() != lastDataHash)
            RefreshAll();
#endif
    }

#if UNITY_EDITOR
    private int lastDataHash;

    private int ComputeDataHash()
    {
        unchecked
        {
            int hash = 17;
            BoundsInt bounds = Data.cellBounds;
            foreach (Vector3Int pos in bounds.allPositionsWithin)
            {
                if (Data.HasTile(pos))
                    hash = hash * 31 + pos.x * 73856093 + pos.y * 19349663 + (int)GetTerrain(pos) * 83492791 + 1;
            }
            return hash;
        }
    }

    // Pink crossed squares on corners whose tile combination is missing from every tileset.
    private void OnDrawGizmos()
    {
        if (missingCorners.Count == 0 || dataTilemap == null)
            return;

        Grid grid = GetComponentInParent<Grid>();
        Vector3 cell = grid != null ? grid.cellSize : Vector3.one;
        Vector3 half = new Vector3(cell.x * 0.45f, cell.y * 0.45f, 0f);
        Gizmos.color = new Color(1f, 0.1f, 0.7f, 1f);
        foreach (Vector3Int corner in missingCorners)
        {
            Vector3 center = dataTilemap.CellToWorld(corner);   // corner (x,y) = bottom-left of cell (x,y)
            Gizmos.DrawWireCube(center, half * 2f);
            Gizmos.DrawLine(center - half, center + half);
            Gizmos.DrawLine(center + new Vector3(-half.x, half.y, 0f), center + new Vector3(half.x, -half.y, 0f));
        }
    }
#endif

    /// <summary>Re-reads the tilesets, recreates the display layer and redraws everything.</summary>
    [ContextMenu("Rebuild dual grid")]
    public void Rebuild()
    {
        if (!isActiveAndEnabled)
            return;

        needsRebuild = false;
        DestroyDisplay();
        ReleaseCreated();
        missingCorners.Clear();

#if UNITY_EDITOR
        // Do not create the hidden display layer inside prefab contents / Prefab Mode.
        if (!Application.isPlaying && EditorSceneManager.IsPreviewScene(gameObject.scene))
            return;
#endif

        if (!BuildTiles())
            return;

        CreateDisplay();
        RefreshAll();

        if (duplicateTiles.Count > 0)
            Debug.Log("DualGridLayer: tiles with the same corners as an earlier tile in the same tileset (ignored): " +
                      string.Join(", ", duplicateTiles) + ".", this);
        ReportMissingInLevel();
    }

    /// <summary>Redraws the whole display layer from the logical layer.</summary>
    [ContextMenu("Refresh all")]
    public void RefreshAll()
    {
        if (displayTilemap == null)
            return;

        displayTilemap.ClearAllTiles();
        pending.Clear();
        missingCorners.Clear();

#if UNITY_EDITOR
        lastDataHash = ComputeDataHash();
#endif

        BoundsInt bounds = Data.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (Data.HasTile(pos))
                AddAffected(pos);
        }
        ApplyAffected();
    }

    /// <summary>Lists every corner combination that no tileset has.</summary>
    [ContextMenu("Log missing tiles")]
    public void LogMissingTiles()
    {
        List<string> missing = new List<string>();
        for (int code = 0; code < ComboCount; code++)
        {
            if (comboTiles[code] == null)
                missing.Add(CodeToText(code));
        }

        if (missing.Count == 0)
            Debug.Log("DualGridLayer: all 81 combinations have a tile.", this);
        else
            Debug.Log("DualGridLayer: " + missing.Count + " combinations have no tile: " + string.Join(", ", missing) +
                      ". " + LegendText, this);
    }

    private const string LegendText =
        "F = floor, W = wall, A = water, written top-left top-right / bottom-left bottom-right.";

    private void ReportMissingInLevel()
    {
        if (missingCorners.Count == 0)
            return;

        HashSet<int> codes = new HashSet<int>();
        foreach (Vector3Int corner in missingCorners)
            codes.Add(ComputeCode(corner));

        List<string> names = new List<string>();
        foreach (int code in codes)
            names.Add(CodeToText(code));

        Debug.LogWarning("DualGridLayer: the level needs " + codes.Count + " tile combination(s) that no tileset has " +
                         "(left empty, pink markers in the Scene view): " + string.Join(", ", names) + ". " + LegendText, this);
    }

    private static string CodeToText(int code)
    {
        StringBuilder sb = new StringBuilder(5);
        sb.Append(TerrainLetters[Corner(code, 0)]).Append(TerrainLetters[Corner(code, 1)]).Append('/')
          .Append(TerrainLetters[Corner(code, 2)]).Append(TerrainLetters[Corner(code, 3)]);
        return sb.ToString();
    }

    private static int Corner(int code, int corner) => code / Pow3[corner] % 3;

    private void OnTilemapTileChanged(Tilemap map, Tilemap.SyncTile[] changes)
    {
        if (map != dataTilemap || displayTilemap == null)
            return;

        for (int i = 0; i < changes.Length; i++)
            AddAffected(changes[i].position);
        ApplyAffected();

#if UNITY_EDITOR
        lastDataHash = ComputeDataHash();
#endif
    }

    // A logical cell (x,y) touches the corners (x..x+1, y..y+1); the margin adds a ring of plain wall around them.
    private void AddAffected(Vector3Int dataCell)
    {
        for (int dx = -margin; dx <= 1 + margin; dx++)
            for (int dy = -margin; dy <= 1 + margin; dy++)
                pending.Add(new Vector3Int(dataCell.x + dx, dataCell.y + dy, dataCell.z));
    }

    private void ApplyAffected()
    {
        if (displayTilemap == null)
        {
            pending.Clear();
            return;
        }

        batchPositions.Clear();
        batchTiles.Clear();
        foreach (Vector3Int corner in pending)
        {
            TileBase tile = comboTiles[ComputeCode(corner)];
            if (tile == null)
                missingCorners.Add(corner);
            else
                missingCorners.Remove(corner);

            batchPositions.Add(corner);
            batchTiles.Add(tile);
        }
        pending.Clear();

        if (batchPositions.Count > 0)
            displayTilemap.SetTiles(batchPositions.ToArray(), batchTiles.ToArray());
    }

    private int ComputeCode(Vector3Int corner)
    {
        int code = 0;
        for (int i = 0; i < 4; i++)
            code += (int)GetTerrain(corner + CornerOffsets[i]) * Pow3[i];
        return code;
    }

    // ------------------------------------------------------------------ tilesets

    /// <summary>Reads every tile of every tileset and creates one display tile per corner combination.</summary>
    private bool BuildTiles()
    {
        duplicateTiles.Clear();
        int[] comboSheet = new int[ComboCount];
        for (int i = 0; i < ComboCount; i++)
        {
            comboTiles[i] = null;
            comboSheet[i] = -1;
        }

        Color[] terrainColors = { floorColor, wallColor, waterColor };
        bool any = false;

        for (int s = 0; s < tilesets.Count; s++)
        {
            Sheet sheet = tilesets[s];
            if (sheet == null || sheet.texture == null)
                continue;

            Texture2D tex = sheet.texture;
            if (!tex.isReadable)
            {
                Debug.LogError("DualGridLayer: turn on Read/Write in the import settings of " + tex.name + ".", this);
                continue;
            }

            int frames = Mathf.Max(1, sheet.frames);
            int columns = tex.width / frames / tileSize;
            int rows = tex.height / tileSize;
            if (columns < 1 || rows < 1)
            {
                Debug.LogWarning("DualGridLayer: " + tex.name + " is smaller than one tile per frame.", this);
                continue;
            }

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int code = ReadCode(tex, col, row, terrainColors);
                    if (code < 0)
                        continue;   // empty / transparent slot

                    if (comboTiles[code] != null)
                    {
                        // Shared tiles across sheets (e.g. plain floor) are expected; repeats inside one sheet are reported.
                        if (comboSheet[code] == s)
                            duplicateTiles.Add(tex.name + " row " + (row + 1) + " col " + (col + 1) + " (" + CodeToText(code) + ")");
                        continue;
                    }

                    comboTiles[code] = CreateTile(tex, frames, columns, col, row, code);
                    comboSheet[code] = s;
                    any = true;
                }
            }
        }

        if (!any)
            Debug.LogWarning("DualGridLayer: no usable tileset assigned.", this);
        return any;
    }

    // Corner terrains of a tile from its four corner pixels in frame 0; -1 if any corner is transparent.
    private int ReadCode(Texture2D tex, int col, int row, Color[] terrainColors)
    {
        int left = col * tileSize;
        int right = left + tileSize - 1;
        int top = tex.height - 1 - row * tileSize;   // GetPixel counts rows from the bottom
        int bottom = top - tileSize + 1;
        int[] xs = { left, right, left, right };
        int[] ys = { top, top, bottom, bottom };

        int code = 0;
        for (int i = 0; i < 4; i++)
        {
            Color pixel = tex.GetPixel(xs[i], ys[i]);
            if (pixel.a < 0.5f)
                return -1;
            code += NearestTerrain(pixel, terrainColors) * Pow3[i];
        }
        return code;
    }

    private static int NearestTerrain(Color pixel, Color[] terrainColors)
    {
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int t = 0; t < terrainColors.Length; t++)
        {
            float dr = pixel.r - terrainColors[t].r;
            float dg = pixel.g - terrainColors[t].g;
            float db = pixel.b - terrainColors[t].b;
            float distance = dr * dr + dg * dg + db * db;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = t;
            }
        }
        return best;
    }

    private TileBase CreateTile(Texture2D tex, int frames, int columns, int col, int row, int code)
    {
        DualGridAnimatedTile tile = ScriptableObject.CreateInstance<DualGridAnimatedTile>();
        tile.name = "DualGrid_" + CodeToText(code).Replace('/', '_');
        tile.hideFlags = HideFlags.HideAndDontSave;

        if (frames <= 1)
        {
            tile.frames = new[] { CreateSprite(tex, columns, 0, col, row) };
        }
        else
        {
            // Frame 0 (no outline) is repeated so it lasts longer, then frames 1..n-1 play once each.
            Sprite[] sequence = new Sprite[stillFrameHold + frames - 1];
            Sprite still = CreateSprite(tex, columns, 0, col, row);
            for (int i = 0; i < stillFrameHold; i++)
                sequence[i] = still;
            for (int f = 1; f < frames; f++)
                sequence[stillFrameHold + f - 1] = CreateSprite(tex, columns, f, col, row);
            tile.frames = sequence;
            tile.speed = 1f / frameDuration;
        }

        createdObjects.Add(tile);
        return tile;
    }

    private Sprite CreateSprite(Texture2D tex, int columns, int frame, int col, int row)
    {
        Rect rect = new Rect((frame * columns + col) * tileSize, tex.height - (row + 1) * tileSize, tileSize, tileSize);

        // One tile = one cell of the parent grid.
        Grid grid = GetComponentInParent<Grid>();
        float cellWidth = grid != null && grid.cellSize.x > 0f ? grid.cellSize.x : 1f;

        Sprite sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), tileSize / cellWidth, 0, SpriteMeshType.FullRect);
        sprite.name = tex.name + "_r" + row + "_c" + col + "_f" + frame;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        createdObjects.Add(sprite);
        return sprite;
    }

    // ------------------------------------------------------------------ display layer

    private void CreateDisplay()
    {
        GameObject go = new GameObject(DisplayName);
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.SetParent(transform, false);

        // Shift by half a cell so each visible tile is centred on a corner of the logical grid.
        Grid grid = GetComponentInParent<Grid>();
        Vector3 cell = grid != null ? grid.cellSize : Vector3.one;
        go.transform.localPosition = new Vector3(-cell.x * 0.5f, -cell.y * 0.5f, 0f);

        displayTilemap = go.AddComponent<Tilemap>();
        displayTilemap.animationFrameRate = 1f;
        TilemapRenderer renderer = go.AddComponent<TilemapRenderer>();

        TilemapRenderer dataRenderer = GetComponent<TilemapRenderer>();
        if (dataRenderer != null)
        {
            if (dataRenderer.sharedMaterial != null)
                renderer.sharedMaterial = dataRenderer.sharedMaterial;
            renderer.sortingLayerID = dataRenderer.sortingLayerID;
        }
        renderer.sortingOrder = sortingOrder;
    }

    private void DestroyDisplay()
    {
        // Also removes a display child left over from before a domain reload.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == DisplayName)
                DestroyUnityObject(child.gameObject);
        }
        displayTilemap = null;
    }

    private void ReleaseCreated()
    {
        for (int i = 0; i < createdObjects.Count; i++)
            DestroyUnityObject(createdObjects[i]);
        createdObjects.Clear();
        for (int i = 0; i < ComboCount; i++)
            comboTiles[i] = null;
    }

    private static void DestroyUnityObject(Object obj)
    {
        if (obj == null)
            return;
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}
