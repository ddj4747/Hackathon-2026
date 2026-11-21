using UnityEngine;

/// <summary>Persistent, non-spatial music and sound effects for the whole game.</summary>
[DisallowMultipleComponent]
public class SoundManager : MonoBehaviour
{
    public enum Effect { Step, Push, Undo, Select, Restart, Exit }

    public static SoundManager Instance { get; private set; }

    private AudioSource musicSource;
    private AudioSource effectsSource;
    private AudioClip[] effects;

    public float MusicVolume
    {
        get => musicSource.volume;
        set => musicSource.volume = Mathf.Clamp01(value);
    }

    public float EffectsVolume
    {
        get => effectsSource.volume;
        set => effectsSource.volume = Mathf.Clamp01(value);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
            new GameObject("SoundManager").AddComponent<SoundManager>();
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
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = 0.4f;
        effectsSource = gameObject.AddComponent<AudioSource>();
        effectsSource.playOnAwake = false;
        effectsSource.spatialBlend = 0f;
        effectsSource.volume = 0.7f;

        musicSource.clip = Resources.Load<AudioClip>("Sound/Music");
        if (musicSource.clip != null)
            musicSource.Play();
        else
            Debug.LogWarning("SoundManager: missing Resources/Sound/Music audio clip.");

        effects = new AudioClip[System.Enum.GetValues(typeof(Effect)).Length];
        for (int i = 0; i < effects.Length; i++)
        {
            string name = ((Effect)i).ToString();
            effects[i] = Resources.Load<AudioClip>("Sound/" + name);
            if (effects[i] == null)
                Debug.LogWarning("SoundManager: missing Resources/Sound/" + name + " audio clip.");
        }
    }

    /// <summary>Play a placeholder effect, allowing effects to overlap.</summary>
    public void Play(Effect effect)
    {
        int index = (int)effect;
        if (effects != null && index >= 0 && index < effects.Length && effects[index] != null)
            effectsSource.PlayOneShot(effects[index]);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
