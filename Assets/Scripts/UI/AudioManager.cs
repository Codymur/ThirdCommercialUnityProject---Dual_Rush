using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Persistent singleton that applies and exposes audio volume controls.
/// Auto-created before any scene loads using the Master AudioMixer from Resources.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ── PlayerPrefs keys (must match the AudioMixer's exposed parameter names) ──
    private const string MasterVolumeKey = "MasterVolume";
    private const string SFXVolumeKey    = "SFXVolume";
    private const string MusicVolumeKey  = "MusicVolume";

    private const float DefaultVolume = 0.75f;

    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioMixer _audioMixer;

    // ── Bootstrap ─────────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        var mixer = Resources.Load<AudioMixer>("Master");
        if (mixer == null)
        {
            Debug.LogError("[AudioManager] Could not load 'Master' AudioMixer from Resources.");
            return;
        }

        var go = new GameObject("[AudioManager]");
        DontDestroyOnLoad(go);

        var manager = go.AddComponent<AudioManager>();
        manager._audioMixer = mixer;
        Instance = manager;

        manager.ApplySavedSettings();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_audioMixer != null)
            ApplySavedSettings();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Sets and persists master volume from a 0–1 linear value.</summary>
    public void SetMasterVolume(float linearValue)
    {
        ApplyToMixer(MasterVolumeKey, linearValue);
        PlayerPrefs.SetFloat(MasterVolumeKey, linearValue);
        PlayerPrefs.Save();
    }

    /// <summary>Sets and persists SFX volume from a 0–1 linear value.</summary>
    public void SetSFXVolume(float linearValue)
    {
        ApplyToMixer(SFXVolumeKey, linearValue);
        PlayerPrefs.SetFloat(SFXVolumeKey, linearValue);
        PlayerPrefs.Save();
    }

    /// <summary>Sets and persists music volume from a 0–1 linear value.</summary>
    public void SetMusicVolume(float linearValue)
    {
        ApplyToMixer(MusicVolumeKey, linearValue);
        PlayerPrefs.SetFloat(MusicVolumeKey, linearValue);
        PlayerPrefs.Save();
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private void ApplySavedSettings()
    {
        ApplyToMixer(MasterVolumeKey, PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume));
        ApplyToMixer(SFXVolumeKey,    PlayerPrefs.GetFloat(SFXVolumeKey,    DefaultVolume));
        ApplyToMixer(MusicVolumeKey,  PlayerPrefs.GetFloat(MusicVolumeKey,  DefaultVolume));
    }

    private void ApplyToMixer(string parameterName, float linearValue)
    {
        if (_audioMixer == null) return;

        if (!_audioMixer.SetFloat(parameterName, LinearToDecibel(linearValue)))
            Debug.LogWarning($"[AudioManager] AudioMixer has no exposed parameter '{parameterName}'. " +
                             "Expose it in the Audio Mixer window.");
    }

    private static float LinearToDecibel(float linear) =>
        Mathf.Log10(Mathf.Max(linear, 0.0001f)) * 20f;
}
