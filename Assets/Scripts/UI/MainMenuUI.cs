using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages main menu panel transitions and the in-menu settings screen.
/// Attach to the Canvas GameObject in the MainMenu scene.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    // ── PlayerPrefs keys ─────────────────────────────────────────────────────
    private const string SensitivityKey  = "MouseSensitivity";
    private const string InvertXKey      = "InvertX";
    private const string InvertYKey      = "InvertY";
    private const string ResolutionKey   = "ResolutionIndex";
    private const string FullscreenKey   = "Fullscreen";
    private const string QualityKey      = "QualityIndex";
    private const string TextureKey      = "TextureLimit";
    private const string MasterVolumeKey = "MasterVolume";
    private const string SFXVolumeKey    = "SFXVolume";
    private const string MusicVolumeKey  = "MusicVolume";

    private const float DefaultSensitivity = 300f;
    private const float MinSensitivity     = 50f;
    private const float MaxSensitivity     = 1000f;
    private const float DefaultVolume      = 0.75f;

    // ── Panels ────────────────────────────────────────────────────────────────
    [Header("Panels")]
    [Tooltip("Parent containing the Play / Options / Quit buttons.")]
    public GameObject mainButtonsPanel;

    [Tooltip("The settings panel to show when Options is clicked.")]
    public GameObject settingsPanel;

    public GameObject GameplayPanel;
    public GameObject AudioPanel;
    public GameObject DisplayPanel;

    // ── Sensitivity ───────────────────────────────────────────────────────────
    [Header("Sensitivity")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;

    // ── Invert ────────────────────────────────────────────────────────────────
    [Header("Invert")]
    [Tooltip("Toggle that inverts the horizontal (X) mouse axis.")]
    public Toggle invertXToggle;

    [Tooltip("Toggle that inverts the vertical (Y) mouse axis.")]
    public Toggle invertYToggle;

    // ── Display ───────────────────────────────────────────────────────────────
    [Header("Display")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle       fullscreenToggle;
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown textureDropdown;

    // ── Audio ─────────────────────────────────────────────────────────────────
    [Header("Audio")]
    public Slider masterVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider musicVolumeSlider;

    // ── Internal state ────────────────────────────────────────────────────────
    private Resolution[] _resolutions;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            GameplayPanel.SetActive(false);
            AudioPanel.SetActive(false);
            DisplayPanel.SetActive(true);
        }
            


        InitializeSensitivity();
        InitializeInvertToggles();
        InitializeResolution();
        InitializeFullscreen();
        InitializeQuality();
        InitializeTexture();
        InitializeAudio();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void GameplayPanelActivate()
    {
        GameplayPanel.SetActive(true);
        AudioPanel.SetActive(false);
        DisplayPanel.SetActive(false);
    }

    public void AudioPanelActivate()
    {
        GameplayPanel.SetActive(false);
        AudioPanel.SetActive(true);
        DisplayPanel.SetActive(false);
    }

    public void DisplayPanelActivate()
    {
        GameplayPanel.SetActive(false);
        AudioPanel.SetActive(false);
        DisplayPanel.SetActive(true);
    }

    /// <summary>Shows the settings panel and hides the main buttons.</summary>
    public void OpenSettings()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (settingsPanel != null)    settingsPanel.SetActive(true);
    }

    /// <summary>Hides the settings panel and restores the main buttons.</summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)    settingsPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    // ── Sensitivity ───────────────────────────────────────────────────────────
    private void InitializeSensitivity()
    {
        if (sensitivitySlider == null) return;

        sensitivitySlider.minValue = MinSensitivity;
        sensitivitySlider.maxValue = MaxSensitivity;
        sensitivitySlider.value    = PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);

        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        UpdateSensitivityText(sensitivitySlider.value);
    }

    private void OnSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat(SensitivityKey, value);
        PlayerPrefs.Save();
        UpdateSensitivityText(value);
    }

    private void UpdateSensitivityText(float value)
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = Mathf.RoundToInt(value).ToString();
    }

    // ── Invert ────────────────────────────────────────────────────────────────
    private void InitializeInvertToggles()
    {
        if (invertXToggle != null)
        {
            invertXToggle.isOn = PlayerPrefs.GetInt(InvertXKey, 0) == 1;
            invertXToggle.onValueChanged.AddListener(OnInvertXChanged);
        }

        if (invertYToggle != null)
        {
            invertYToggle.isOn = PlayerPrefs.GetInt(InvertYKey, 0) == 1;
            invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
        }
    }

    private void OnInvertXChanged(bool value)
    {
        PlayerPrefs.SetInt(InvertXKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnInvertYChanged(bool value)
    {
        PlayerPrefs.SetInt(InvertYKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ── Resolution ────────────────────────────────────────────────────────────
    private const float AspectRatio16x9 = 16f / 9f;
    private const float AspectRatioTolerance = 0.02f;

    private static bool Is16x9(Resolution r) =>
        Mathf.Abs((float)r.width / r.height - AspectRatio16x9) <= AspectRatioTolerance;

    private void InitializeResolution()
    {
        if (resolutionDropdown == null) return;

        // Only 16:9 resolutions, deduplicated by width x height
        var allResolutions = Screen.resolutions;
        var seen           = new HashSet<string>();
        var dedupedList    = new List<Resolution>();
        var options        = new List<TMP_Dropdown.OptionData>();

        foreach (Resolution r in allResolutions)
        {
            if (!Is16x9(r)) continue;

            string key = $"{r.width}x{r.height}";
            if (seen.Add(key))
            {
                dedupedList.Add(r);
                options.Add(new TMP_Dropdown.OptionData($"{r.width} x {r.height}"));
            }
        }

        _resolutions = dedupedList.ToArray();
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);

        int saved   = PlayerPrefs.GetInt(ResolutionKey, -1);
        int current = FindCurrentResolutionIndex();
        if (saved >= 0 && saved < _resolutions.Length)
            current = saved;

        resolutionDropdown.value = current;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    private int FindCurrentResolutionIndex()
    {
        for (int i = 0; i < _resolutions.Length; i++)
        {
            if (_resolutions[i].width  == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
                return i;
        }
        return 0;
    }

    private void OnResolutionChanged(int index)
    {
        Resolution r = _resolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
        PlayerPrefs.SetInt(ResolutionKey, index);
        PlayerPrefs.Save();
    }

    // ── Fullscreen ────────────────────────────────────────────────────────────
    private void InitializeFullscreen()
    {
        if (fullscreenToggle == null) return;

        bool saved           = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen    = saved;
        fullscreenToggle.isOn = saved;
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    private void OnFullscreenChanged(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ── Quality ───────────────────────────────────────────────────────────────
    private void InitializeQuality()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.value = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        qualityDropdown.RefreshShownValue();
        qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    private void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index, applyExpensiveChanges: true);
        PlayerPrefs.SetInt(QualityKey, index);
        PlayerPrefs.Save();
    }

    // ── Texture ───────────────────────────────────────────────────────────────
    private void InitializeTexture()
    {
        if (textureDropdown == null) return;

        textureDropdown.ClearOptions();
        textureDropdown.AddOptions(new List<string> { "Full", "Half", "Quarter", "Eighth" });
        textureDropdown.value = PlayerPrefs.GetInt(TextureKey, QualitySettings.globalTextureMipmapLimit);
        textureDropdown.RefreshShownValue();
        textureDropdown.onValueChanged.AddListener(OnTextureChanged);
    }

    private void OnTextureChanged(int index)
    {
        QualitySettings.globalTextureMipmapLimit = index;
        PlayerPrefs.SetInt(TextureKey, index);
        PlayerPrefs.Save();
    }

    // ── Audio ─────────────────────────────────────────────────────────────────

    /// <summary>Loads saved volume preferences and registers slider listeners.</summary>
    private void InitializeAudio()
    {
        if (AudioManager.Instance == null) return;

        SetupVolumeSlider(masterVolumeSlider, MasterVolumeKey, OnMasterVolumeChanged);
        SetupVolumeSlider(sfxVolumeSlider,    SFXVolumeKey,    OnSFXVolumeChanged);
        SetupVolumeSlider(musicVolumeSlider,  MusicVolumeKey,  OnMusicVolumeChanged);
    }

    private void SetupVolumeSlider(Slider slider, string prefsKey, UnityEngine.Events.UnityAction<float> onChange)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = PlayerPrefs.GetFloat(prefsKey, DefaultVolume);
        slider.onValueChanged.AddListener(onChange);
        onChange(slider.value);
    }

    private void OnMasterVolumeChanged(float value) => AudioManager.Instance?.SetMasterVolume(value);
    private void OnSFXVolumeChanged(float value)    => AudioManager.Instance?.SetSFXVolume(value);
    private void OnMusicVolumeChanged(float value)  => AudioManager.Instance?.SetMusicVolume(value);
}
