using UnityEngine;
using UnityEngine.Audio;

public class BulletShellSound : MonoBehaviour
{
    public AudioClip[] shellSoundClips;
    public float volumeMin = 0.4f;
    public float volumeMax = 0.7f;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.2f;

    [Tooltip("Optional audio mixer group to route the shell sound through (e.g. SFX).")]
    public AudioMixerGroup outputMixerGroup;

    private bool hasPlayed = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasPlayed) return;
        if (shellSoundClips == null || shellSoundClips.Length == 0) return;

        hasPlayed = true;

        AudioClip clip = shellSoundClips[Random.Range(0, shellSoundClips.Length)];
        PlayClipAtPointWithMixer(clip, transform.position, Random.Range(volumeMin, volumeMax), Random.Range(pitchMin, pitchMax));
    }

    /// <summary>Plays a one-shot clip at the given position, routed through <see cref="outputMixerGroup"/> when assigned.</summary>
    private void PlayClipAtPointWithMixer(AudioClip clip, Vector3 position, float volume, float pitch)
    {
        GameObject tempGO = new GameObject("ShellSound_OneShot");
        tempGO.transform.position = position;

        AudioSource source = tempGO.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.spatialBlend = 1f;
        source.outputAudioMixerGroup = outputMixerGroup;
        source.Play();

        Destroy(tempGO, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)));
    }

    /// <summary>Resets state so a pooled shell can be reused.</summary>
    public void ResetShell()
    {
        hasPlayed = false;
    }
}
