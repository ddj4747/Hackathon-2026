using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Global game manager. It creates itself when the game starts and survives scene changes, so it
/// does not need to be placed in any scene.
///
/// For now it owns the trip back to the menu: a level exit calls <see cref="ReturnToMenu"/>, and the
/// manager remembers which level was left so the menu can put the player back on that level's plate.
///
/// It also restarts the current level when the Restart action (R) is pressed, by reloading the same scene
/// behind a full-screen loading screen. Restarting does nothing in the menu.
///
/// A small controls reminder (bottom-right corner) is shown in every scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>Shared "confirm / activate" action (Space, gamepad south). Use it for all confirm-style input.</summary>
    public const string ConfirmActionPath = "Player/Confirm";

    /// <summary>Restart-level action (R).</summary>
    public const string RestartActionPath = "Player/Restart";

    /// <summary>The loading screen stays up at least this long (seconds), so it never just flickers.</summary>
    private const float MinLoadingScreenTime = 0.3f;

    private const string ControlsHintText = "SPACE - select\nWASD - movement\nR - reset";

    /// <summary>Name of the menu scene. It must be in the build's scene list.</summary>
    public const string MenuSceneName = "Menu";

    private const string MenuScenePath = "Assets/Scenes/" + MenuSceneName + ".unity";

    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Path of the level the player just left through an exit, or null. The menu reads this to place the
    /// player on the matching level plate, then calls <see cref="ClearReturn"/>.
    /// </summary>
    public string ReturnedFromScenePath { get; private set; }

    private bool loading;
    private bool restarting;
    private InputAction restartAction;
    private GameObject loadingScreen;

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
        BuildControlsHint();
        BuildLoadingScreen();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        restartAction = InputSystem.actions != null ? InputSystem.actions.FindAction(RestartActionPath) : null;
        if (restartAction == null)
            Debug.LogError($"GameManager: action '{RestartActionPath}' not found in the project-wide input actions.", this);
        else
            restartAction.performed += OnRestartPerformed;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (restartAction != null)
            restartAction.performed -= OnRestartPerformed;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        loading = false;

        // Entering any level makes an older "returned from" marker stale.
        if (scene.name != MenuSceneName)
            ReturnedFromScenePath = null;
    }

    private void OnRestartPerformed(InputAction.CallbackContext context)
    {
        RestartLevel();
    }

    /// <summary>
    /// Reloads the current level from scratch behind the loading screen. Does nothing in the menu or while
    /// another load is running.
    /// </summary>
    public void RestartLevel()
    {
        Scene active = SceneManager.GetActiveScene();
        if (loading || restarting || active.name == MenuSceneName)
            return;

        if (active.buildIndex < 0)
        {
            Debug.LogError($"GameManager: scene '{active.name}' is not in the build's scene list, cannot restart it.");
            return;
        }

        StartCoroutine(RestartRoutine(active.buildIndex));
    }

    private IEnumerator RestartRoutine(int buildIndex)
    {
        restarting = true;
        loadingScreen.SetActive(true);
        float start = Time.unscaledTime;

        yield return null;   // let the loading screen render before the load work starts

        AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex);
        while (operation != null && !operation.isDone)
            yield return null;

        float remaining = MinLoadingScreenTime - (Time.unscaledTime - start);
        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);

        loadingScreen.SetActive(false);
        restarting = false;
    }

    /// <summary>Creates the full-screen loading screen (black canvas with text). Hidden until needed.</summary>
    private void BuildLoadingScreen()
    {
        loadingScreen = new GameObject("LoadingScreen", typeof(Canvas));
        loadingScreen.transform.SetParent(transform, false);

        Canvas canvas = loadingScreen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        RectTransform background = CreateStretchedChild(loadingScreen.transform, "Background", typeof(Image));
        Image image = background.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        RectTransform label = CreateStretchedChild(loadingScreen.transform, "Text", typeof(Text));
        Text text = label.GetComponent<Text>();
        text.font = LoadFont();
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "Loading...";
        text.raycastTarget = false;

        loadingScreen.SetActive(false);
    }

    /// <summary>Creates the always-visible controls reminder in the bottom-right corner.</summary>
    private void BuildControlsHint()
    {
        GameObject hud = new GameObject("ControlsHint", typeof(Canvas), typeof(CanvasScaler));
        hud.transform.SetParent(transform, false);

        Canvas canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;   // above the game, below the loading screen

        CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 540f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform label = CreateStretchedChild(hud.transform, "Text", typeof(Text));
        label.offsetMin = new Vector2(16f, 16f);     // margin from the screen edges
        label.offsetMax = new Vector2(-16f, -16f);

        Text text = label.GetComponent<Text>();
        text.font = LoadFont();
        text.fontSize = 16;
        text.alignment = TextAnchor.LowerRight;
        text.color = Color.white;
        text.text = ControlsHintText;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1f, -1f);
    }

    /// <summary>The project font (Resources/nokiafc22), or Unity's built-in font if it is missing.</summary>
    private static Font LoadFont()
    {
        Font font = Resources.Load<Font>("nokiafc22");
        return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static RectTransform CreateStretchedChild(Transform parent, string childName, System.Type graphicType)
    {
        GameObject child = new GameObject(childName, typeof(RectTransform), graphicType);
        child.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)child.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    /// <summary>Loads the menu and remembers the current level so the player lands on its plate.</summary>
    public void ReturnToMenu()
    {
        Scene active = SceneManager.GetActiveScene();
        if (loading || restarting || active.name == MenuSceneName)
            return;

        if (SceneUtility.GetBuildIndexByScenePath(MenuScenePath) < 0)
        {
            Debug.LogError($"GameManager: scene '{MenuSceneName}' is not in the build's scene list.");
            return;
        }

        loading = true;
        ReturnedFromScenePath = active.path;
        SceneManager.LoadScene(MenuSceneName);
    }

    public void ClearReturn()
    {
        ReturnedFromScenePath = null;
    }
}
