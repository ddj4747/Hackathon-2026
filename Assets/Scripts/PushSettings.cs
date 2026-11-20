using UnityEngine;

/// <summary>
/// Global push rules shared by every <see cref="Pushable"/>. Lives as one asset,
/// <c>Assets/Resources/PushSettings.asset</c>, so the values are edited in a single place.
/// </summary>
[CreateAssetMenu(fileName = "PushSettings", menuName = "Game/Push Settings")]
public class PushSettings : ScriptableObject
{
    [Tooltip("Maximum number of objects the player can push in one chain. 0 = unlimited.")]
    [Min(0)] public int maxChainLength = 3;

    private static PushSettings instance;

    /// <summary>The settings asset (Resources/PushSettings). Falls back to defaults if the asset is missing.</summary>
    public static PushSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<PushSettings>("PushSettings");
                if (instance == null)
                {
                    Debug.LogWarning("PushSettings asset not found in Resources; using defaults.");
                    instance = CreateInstance<PushSettings>();
                }
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;
}
