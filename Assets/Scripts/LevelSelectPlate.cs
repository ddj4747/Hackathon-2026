using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A level-select "pressure plate". It snaps to a cell of the scene's Grid and highlights while the
/// player stands on it. Pressing the shared Confirm action (Space) while the player is standing
/// on it loads the scene assigned in the inspector.
/// </summary>
[DisallowMultipleComponent]
public class LevelSelectPlate : MonoBehaviour
{
    [Header("Level")]
    [Tooltip("Number displayed on the plate.")]
    [SerializeField, Min(0)] private int levelNumber = 1;

#if UNITY_EDITOR
    [Tooltip("Scene to load. Pick it from the list or drag it in. The scene must be in File > Build Profiles > Scene List.")]
    [SerializeField] private SceneAsset sceneAsset;
#endif

    // Baked from sceneAsset in OnValidate so it also works in builds, where SceneAsset does not exist.
    [SerializeField, HideInInspector] private string scenePath;

    [Header("Progression")]
    [Tooltip("The preceding level, which must be beaten before this plate can be selected.")]
    [SerializeField] private LevelSelectPlate previousLevel;
    [Tooltip("Allow selection if the preceding plate does not have a scene yet.")]
    [SerializeField] private bool allowWhenPreviousUnassigned;

    [Header("Look")]
    [SerializeField] private SpriteRenderer plateRenderer;
    [SerializeField] private TextMesh numberLabel;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.2f, 1f);

    private Grid grid;
    private PlayerGridMovement player;
    private Vector3Int cell;
    private InputAction selectAction;
    private bool loading;

    public int LevelNumber => levelNumber;
    public bool IsHighlighted { get; private set; }
    public bool HasAssignedScene => !string.IsNullOrEmpty(scenePath);
    public bool IsBeaten => GameManager.Instance != null && GameManager.Instance.IsLevelBeaten(scenePath);
    public bool IsUnlocked => previousLevel == null ||
        (!previousLevel.HasAssignedScene && allowWhenPreviousUnassigned) || previousLevel.IsBeaten;

    private void Awake()
    {
        ApplyLabel();
    }

    private void Start()
    {
        grid = FindAnyObjectByType<Grid>();
        if (grid == null)
        {
            Debug.LogError("LevelSelectPlate: no Grid found in the scene. Add the MapGrid prefab.", this);
            enabled = false;
            return;
        }

        // Snap onto the grid cell this plate was placed on.
        cell = grid.WorldToCell(transform.position);
        Vector3 center = grid.GetCellCenterWorld(cell);
        center.z = transform.position.z;
        transform.position = center;

        player = FindAnyObjectByType<PlayerGridMovement>();
        PlaceReturningPlayer();
        SetVisual(false);
    }

    // Coming back from this plate's level through an exit: start the menu with the player standing on it.
    private void PlaceReturningPlayer()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || player == null || string.IsNullOrEmpty(scenePath))
            return;

        if (manager.ReturnedFromScenePath != scenePath)
            return;

        player.TeleportToCell(cell);
        manager.ClearReturn();
    }

    private void OnEnable()
    {
        InputActionAsset actions = InputSystem.actions;
        selectAction = actions != null ? actions.FindAction(GameManager.ConfirmActionPath) : null;
        if (selectAction == null)
        {
            Debug.LogError($"LevelSelectPlate: action '{GameManager.ConfirmActionPath}' not found in the project-wide input actions.", this);
            return;
        }

        selectAction.performed += OnSelect;
        if (!selectAction.enabled)
            selectAction.Enable();
    }

    private void OnDisable()
    {
        if (selectAction != null)
            selectAction.performed -= OnSelect;
        selectAction = null;
    }

    private void Update()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerGridMovement>();

        // Highlighted as soon as the player's centre is inside this cell (i.e. halfway through the step onto it).
        bool onPlate = player != null && grid.WorldToCell(player.transform.position) == cell;
        SetVisual(onPlate);
    }

    private void SetVisual(bool highlighted)
    {
        IsHighlighted = highlighted;
        if (plateRenderer == null)
            return;

        plateRenderer.color = highlighted ? highlightColor : normalColor;
    }

    private void OnSelect(InputAction.CallbackContext context)
    {
        if (loading || player == null)
            return;

        // The player must be standing still on this plate, not just passing over it.
        if (player.IsMoving || player.CurrentCell != cell)
            return;

        if (!IsUnlocked)
            return;

        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogWarning($"LevelSelectPlate: level {levelNumber} has no scene assigned.", this);
            return;
        }

        if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
        {
            Debug.LogError($"LevelSelectPlate: '{scenePath}' is not in the build's scene list. Add it in File > Build Profiles > Scene List.", this);
            return;
        }

        loading = true;
        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(SoundManager.Effect.Select);
        SceneManager.LoadScene(scenePath);
    }

    private void ApplyLabel()
    {
        if (numberLabel != null)
            numberLabel.text = levelNumber.ToString();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        scenePath = sceneAsset != null ? AssetDatabase.GetAssetPath(sceneAsset) : string.Empty;
#endif
        ApplyLabel();
    }
}
