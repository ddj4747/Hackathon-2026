using UnityEngine;

/// <summary>Opens a selected bridge while this receiver is supplied.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SignalReceiver))]
public sealed class SignalBridgeController : MonoBehaviour
{
    [Tooltip("Bridge cell to control. Its connected bridge group opens with it.")]
    [SerializeField] private Bridge bridge;

    private SignalReceiver receiver;

    private void Awake() => receiver = GetComponent<SignalReceiver>();

    private void LateUpdate()
    {
        if (bridge != null)
            bridge.Walkable = receiver != null && receiver.isActiveAndEnabled && receiver.IsSignalSupplied;
    }

    private void OnDisable()
    {
        if (bridge != null) bridge.Walkable = false;
    }
}
