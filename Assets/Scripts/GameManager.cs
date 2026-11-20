using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Global game manager. It creates itself when the game starts and survives scene changes, so it
/// does not need to be placed in any scene.
///
/// Esc on any scene except the menu opens a small window in the bottom-right corner asking whether
/// to go back to the menu. Confirm (Space) returns to the menu, Esc closes the window.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>Shared "confirm / activate" action (Space, gamepad south). Use it for all confirm-style input.</summary>
    public const string ConfirmActionPath = "Player/Confirm";

    /// <summary>"Back / open menu" action (Esc, gamepad start).</summary>
    public const string BackActionPath = "Player/Back";

    /// <summary>Name of the menu scene. It must be in the build's scene list.</summary>
    public const string MenuSceneName = "Menu";

    public static GameManager Instance { get; private set; }

    /// <summary>True while the "back to menu?" window is open. Other confirm handlers can check this.</summary>
    public bool IsPromptOpen { get; private set; }

    private InputAction confirmAction;
    private InputAction backAction;
    private GameObject promptWindow;
    private bool loading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
            new GameObject("GameManager").AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPromptWindow();
        SetPromptOpen(false);
    }

    private void OnEnable()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded += OnSceneLoaded;

        InputActionAsset actions = InputSystem.actions;
        confirmAction = Find(actions, ConfirmActionPath);
        backAction = Find(actions, BackActionPath);

        if (confirmAction != null) confirmAction.performed += OnConfirm;
        if (backAction != null) backAction.performed += OnBack;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (confirmAction != null) confirmAction.performed -= OnConfirm;
        if (backAction != null) backAction.performed -= OnBack;
        confirmAction = null;
        backAction = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private static InputAction Find(InputActionAsset actions, string path)
    {
        InputAction action = actions != null ? actions.FindAction(path) : null;
        if (action == null)
        {
            Debug.LogError($"GameManager: action '{path}' not found in the project-wide input actions.");
            return null;
        }

        if (!action.enabled)
            action.Enable();
        return action;
    }

    private static bool InMenuScene => SceneManager.GetActiveScene().name == MenuSceneName;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        loading = false;
        SetPromptOpen(false);
    }

    private void OnBack(InputAction.CallbackContext context)
    {
        if (loading || InMenuScene)
            return;

        // Esc toggles the window: a second press cancels.
        SetPromptOpen(!IsPromptOpen);
    }

    private void OnConfirm(InputAction.CallbackContext context)
    {
        if (loading || !IsPromptOpen)
            return;

        loading = true;
        SetPromptOpen(false);

        if (SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + MenuSceneName + ".unity") < 0)
        {
            Debug.LogError($"GameManager: scene '{MenuSceneName}' is not in the build's scene list.");
            loading = false;
            return;
        }

        SceneManager.LoadScene(MenuSceneName);
    }

    private void SetPromptOpen(bool open)
    {
        IsPromptOpen = open;
        if (promptWindow != null)
            promptWindow.SetActive(open);
    }

    // ---- Prompt window (built in code so there is nothing to set up in a scene) ----

    private void BuildPromptWindow()
    {
        var canvasObject = new GameObject("PromptCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // Same reference resolution as the pixel-art target (512 x 352) so it scales with the game view.
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(512f, 352f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        promptWindow = new GameObject("BackToMenuWindow", typeof(RectTransform), typeof(Image), typeof(Outline));
        promptWindow.transform.SetParent(canvasObject.transform, false);

        var rect = (RectTransform)promptWindow.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f); // bottom-right corner
        rect.anchoredPosition = new Vector2(-8f, 8f);
        rect.sizeDelta = new Vector2(176f, 54f);

        promptWindow.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.14f, 0.92f);

        var border = promptWindow.GetComponent<Outline>();
        border.effectColor = new Color(1f, 0.85f, 0.2f, 1f);
        border.effectDistance = new Vector2(1f, -1f);

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(promptWindow.transform, false);

        var textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6f, 4f);
        textRect.offsetMax = new Vector2(-6f, -4f);

        var text = textObject.GetComponent<Text>();
        text.font = LoadFont();
        text.fontSize = 11;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = "Go back to the menu?\nSPACE - confirm\nESC - cancel";
    }

    private static Font LoadFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Font.CreateDynamicFontFromOSFont("Arial", 16);
    }
}
