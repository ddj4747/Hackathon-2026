using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Global game manager. It creates itself when the game starts and survives scene changes, so it
/// does not need to be placed in any scene.
///
/// For now it owns the trip back to the menu: a level exit calls <see cref="ReturnToMenu"/>, and the
/// manager remembers which level was left so the menu can put the player back on that level's plate.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>Shared "confirm / activate" action (Space, gamepad south). Use it for all confirm-style input.</summary>
    public const string ConfirmActionPath = "Player/Confirm";

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
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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

    /// <summary>Loads the menu and remembers the current level so the player lands on its plate.</summary>
    public void ReturnToMenu()
    {
        Scene active = SceneManager.GetActiveScene();
        if (loading || active.name == MenuSceneName)
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
