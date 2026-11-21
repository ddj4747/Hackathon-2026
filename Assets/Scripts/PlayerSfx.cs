using UnityEngine;

/// <summary>
/// Player sound effects. Plays one footstep each time a step (or an undo of one) starts, and a looping
/// scrape while the player is pushing something, fading it out when pushing stops.
/// The source clips are longer recordings; the footstep and the scrape loop are cut from them at start-up
/// (the clips must use the Decompress On Load setting so their samples can be read).
/// </summary>
[RequireComponent(typeof(PlayerGridMovement))]
public class PlayerSfx : MonoBehaviour
{
    [Header("Footstep")]
    [SerializeField] private AudioClip footsteps;
    [Tooltip("Start (seconds) of the single footstep cut from the clip.")]
    [SerializeField, Min(0f)] private float footstepStart = 0.03f;
    [SerializeField, Min(0.01f)] private float footstepLength = 0.17f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 1f;
    [Tooltip("Random pitch variation (+/-) per footstep.")]
    [SerializeField, Range(0f, 0.3f)] private float footstepPitchJitter = 0.05f;

    [Header("Push")]
    [SerializeField] private AudioClip push;
    [Tooltip("Part of the clip (seconds) used as the scrape loop.")]
    [SerializeField, Min(0f)] private float pushLoopStart = 1f;
    [SerializeField, Min(0f)] private float pushLoopEnd = 13.5f;
    [SerializeField, Range(0f, 1f)] private float pushVolume = 1f;
    [SerializeField, Min(0.01f)] private float pushFadeTime = 0.1f;

    private PlayerGridMovement movement;
    private AudioSource stepSource;
    private AudioSource pushSource;
    private AudioClip stepClip;
    private AudioClip pushClip;
    private bool pushPlaying;   // own flag: AudioSource.isPlaying is false while the game is paused

    private void Awake()
    {
        movement = GetComponent<PlayerGridMovement>();
        stepSource = CreateSource(false);
        pushSource = CreateSource(true);
        stepClip = Slice(footsteps, footstepStart, footstepLength, "Footstep");
        pushClip = Slice(push, pushLoopStart, pushLoopEnd - pushLoopStart, "PushLoop");
        pushSource.clip = pushClip;
    }

    private void OnEnable() => movement.StepStarted += OnStepStarted;

    private void OnDisable()
    {
        movement.StepStarted -= OnStepStarted;
        if (pushSource != null) pushSource.Stop();
        pushPlaying = false;
    }

    private void OnStepStarted(bool pushing)
    {
        if (stepClip == null) return;
        stepSource.pitch = 1f + Random.Range(-footstepPitchJitter, footstepPitchJitter);
        stepSource.PlayOneShot(stepClip, footstepVolume);
    }

    private void Update()
    {
        if (pushClip == null) return;

        bool pushing = movement.IsPushing;
        if (pushing && !pushPlaying)
        {
            pushPlaying = true;
            pushSource.volume = 0f;
            pushSource.time = Random.Range(0f, pushClip.length * 0.9f);   // vary where the scrape starts
            pushSource.Play();
        }

        float target = pushing ? pushVolume : 0f;
        pushSource.volume = Mathf.MoveTowards(pushSource.volume, target, Time.deltaTime / pushFadeTime);
        if (!pushing && pushPlaying && pushSource.volume <= 0f)
        {
            pushSource.Stop();
            pushPlaying = false;
        }
    }

    private AudioSource CreateSource(bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    /// <summary>Copies a part of a clip into a new clip. Returns the whole clip if its samples cannot be read.</summary>
    private static AudioClip Slice(AudioClip clip, float start, float length, string sliceName)
    {
        if (clip == null) return null;

        int channels = clip.channels;
        int from = Mathf.Clamp(Mathf.RoundToInt(start * clip.frequency), 0, clip.samples - 1);
        int count = Mathf.Clamp(Mathf.RoundToInt(length * clip.frequency), 1, clip.samples - from);

        var data = new float[count * channels];
        if (!clip.GetData(data, from))
            return clip;

        AudioClip slice = AudioClip.Create(clip.name + "_" + sliceName, count, channels, clip.frequency, false);
        slice.SetData(data, 0);
        return slice;
    }
}
