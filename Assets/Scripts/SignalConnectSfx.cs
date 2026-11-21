using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays a sound when a component in this object's scene gets connected: a repeater (including the antenna)
/// becomes supplied or a receiver / collector starts receiving. Several connections in the same check play the
/// sound once. Connections made right after the scene starts (the level's initial state) are silent.
/// </summary>
public class SignalConnectSfx : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
    [Tooltip("Connections during this time after start-up make no sound.")]
    [SerializeField, Min(0f)] private float startupSilence = 0.5f;
    [Tooltip("How often (seconds) the connection states are checked.")]
    [SerializeField, Min(0.02f)] private float checkInterval = 0.05f;

    private AudioSource source;
    private HashSet<Component> connected = new HashSet<Component>();
    private HashSet<Component> scratch = new HashSet<Component>();
    private float startTime;
    private float nextCheck;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        startTime = Time.time;
        connected.Clear();
    }

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + checkInterval;

        scratch.Clear();
        bool newConnection = false;

        foreach (Repeater repeater in FindObjectsByType<Repeater>())
        {
            if (repeater.gameObject.scene != gameObject.scene || !repeater.IsSignalSupplied) continue;
            scratch.Add(repeater);
            newConnection |= !connected.Contains(repeater);
        }

        foreach (SignalReceiver receiver in FindObjectsByType<SignalReceiver>())
        {
            if (receiver.gameObject.scene != gameObject.scene || !receiver.IsSignalSupplied) continue;
            scratch.Add(receiver);
            newConnection |= !connected.Contains(receiver);
        }

        (connected, scratch) = (scratch, connected);

        if (newConnection && clip != null && Time.time - startTime >= startupSilence)
            source.PlayOneShot(clip, volume);
    }
}
