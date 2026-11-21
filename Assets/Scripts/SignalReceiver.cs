using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Detects wireless or wired signal and raises events when its supplied state changes.
[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class SignalReceiver : MonoBehaviour
{
    [SerializeField] private SignalType signalType;
    [SerializeField, Tooltip("Sprite shown while this receiver is supplied.")]
    private Sprite suppliedSprite;
    public SignalType Type => signalType;

    [SerializeField] private UnityEvent onSignalReceived = new();
    [SerializeField] private UnityEvent onSignalLost = new();

    public UnityEvent OnSignalReceived => onSignalReceived;
    public UnityEvent OnSignalLost => onSignalLost;
    public bool IsSignalSupplied { get; private set; }

    /// <summary>Raised when any receiver's supplied state changes, including when a supplied receiver is disabled.</summary>
    public static event System.Action<SignalReceiver, bool> SuppliedChanged;

    private Grid grid;
    private Scene gridScene;
    private SpriteRenderer spriteRenderer;
    private Sprite unsuppliedSprite;

    private void OnEnable()
    {
        gridScene = default;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) unsuppliedSprite = spriteRenderer.sprite;
    }
    private void OnTransformParentChanged() => gridScene = default;

    private void Update()
    {
        SignalTypeVisual.Apply(spriteRenderer, signalType);
        if (Application.IsPlaying(gameObject)) UpdateSignal();
        if (spriteRenderer != null && suppliedSprite != null)
            spriteRenderer.sprite = Application.IsPlaying(gameObject) && IsSignalSupplied ? suppliedSprite : unsuppliedSprite;
    }

    private void UpdateSignal()
    {
        var scene = gameObject.scene;
        if (scene.isLoaded && scene != gridScene)
        {
            gridScene = scene;
            grid = FindGrid(scene);
            if (grid == null)
                Debug.LogWarning("SignalReceiver needs a parent Grid or exactly one Grid in its scene.", this);
        }

        var supplied = grid != null && scene.isLoaded &&
            (ConnectionManager.ForScene(scene).HasSignal(transform.position, grid, signalType) || HasWireSignal(scene));
        if (supplied == IsSignalSupplied) return;
        IsSignalSupplied = supplied;
        if (supplied)
        {
            Debug.Log("Signal receiver connected.", this);
            onSignalReceived.Invoke();
        }
        else
        {
            Debug.Log("Signal receiver disconnected.", this);
            onSignalLost.Invoke();
        }
        SuppliedChanged?.Invoke(this, supplied);
    }

    private void OnDisable()
    {
        var wasSupplied = IsSignalSupplied;
        IsSignalSupplied = false;
        if (spriteRenderer != null && suppliedSprite != null) spriteRenderer.sprite = unsuppliedSprite;
        if (wasSupplied) SuppliedChanged?.Invoke(this, false);
        SignalTypeVisual.Restore(spriteRenderer);
    }

    private bool HasWireSignal(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
            foreach (var network in root.GetComponentsInChildren<WireNetwork>())
                if (network.SuppliesAdjacent(transform.position, grid, signalType)) return true;
        return false;
    }

    private Grid FindGrid(Scene scene)
    {
        var parentGrid = GetComponentInParent<Grid>();
        if (parentGrid != null) return parentGrid;
        Grid found = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<Grid>(true))
            {
                if (found != null) return null;
                found = candidate;
            }
        return found;
    }
}
