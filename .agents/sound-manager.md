# Sound manager

## Status
- Implemented persistent music and placeholder sound effects.

## Entry files
- `Assets/Scripts/SoundManager.cs`
- `Assets/Resources/Sound/Music.wav` and `Step`, `Push`, `Undo`, `Select`, `Restart`, `Exit` WAV files.

## Behavior and rules
- The manager creates itself before the first scene and persists across scene changes. It plays the supplied music on a looping 2D source, and effects on a separate 2D source. Music continues across level selection, restart and return to menu.
- Placeholder effects play for successful player steps, pushes and undo, valid level selection, restart and return to menu. Blocked movement and rejected actions are silent. The effects are synthesized placeholders that can be replaced with files of the same names.

## Public interfaces and dependencies
- `SoundManager.Instance.Play(SoundManager.Effect)` plays an effect; `MusicVolume` and `EffectsVolume` independently set clamped 0–1 source volumes (defaults 0.4 and 0.7). A scene camera needs an AudioListener.
- Uses `Resources.Load<AudioClip>`; keep clip names and resource paths when replacing audio. No scene placement is needed.

## Setup and verification
- Play any game scene to start music; walk, push, undo, select, restart or exit to hear the respective effects.
- Unity imported all clips and Play mode verified singleton creation, resource loading, and successful movement/undo calls. Audible playback remains unverified: during the check the editor's audio clock was not advancing and the source did not report playing.
