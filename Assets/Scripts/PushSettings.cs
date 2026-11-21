using UnityEngine;

/// <summary>
/// Legacy push settings retained for existing assets. Chain length no longer limits pushing.
/// </summary>
[CreateAssetMenu(fileName = "PushSettings", menuName = "Game/Push Settings")]
public class PushSettings : ScriptableObject
{
    [Tooltip("Legacy setting; push chains have no length limit.")]
    [Min(0)] public int maxChainLength;

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
