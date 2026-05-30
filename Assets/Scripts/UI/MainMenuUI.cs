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
    private const string FovKey          = "FieldOfView";
    private const string VsyncKey        = "VSync";
    private const string AntiAliasingKey = "AntiAliasing";
    private const string ShowFpsKey      = "FPSDisplayVisible";

    private const float DefaultSensitivity      = 300f;
    private const float MinSensitivity          = 50f;
    private const float MaxSensitivity          = 1000f;
    private const float DefaultVolume           = 0.75f;
    private const float DefaultFov              = 70f;
    private const float MinFov                  = 60f;
    private const float MaxFov                  = 120f;
    private const int   DefaultQualityIndex     = 0;
    private const int   DefaultTextureLimit     = 0;
    private const int   DefaultAntiAliasingIndex = 0;
    private const bool  DefaultFullscreen        = true;

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

    // ── Field of View ─────────────────────────────────────────────────────────
    [Header("Field of View")]
    public Slider fovSlider;
    public TextMeshProUGUI fovValueText;

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

    // ── VSync & Anti-Aliasing ─────────────────────────────────────────────────
    [Header("VSync & Anti-Aliasing")]
    public Toggle       vsyncToggle;
    public TMP_Dropdown antiAliasingDropdown;

    // ── FPS Display ───────────────────────────────────────────────────────────
    [Header("FPS Display")]
    [Tooltip("Toggle that shows or hides the FPS counter.")]
    public Toggle showFpsToggle;

    // ── Reset ─────────────────────────────────────────────────────────────────
    [Header("Reset")]
    [Tooltip("Button that resets all settings to their default values.")]
    public Button resetButton;

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
        InitializeFov();
        InitializeResolution();
        InitializeFullscreen();
        InitializeQuality();
        InitializeTexture();
        InitializeVsync();
        InitializeAntiAliasing();
        InitializeAudio();
        InitializeShowFps();
        InitializeResetButton();
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

    // ── Field of View ─────────────────────────────────────────────────────────
    private void InitializeFov()
    {
        if (fovSlider == null) return;

        fovSlider.minValue    = MinFov;
        fovSlider.maxValue    = MaxFov;
        fovSlider.wholeNumbers = true;
        fovSlider.value       = PlayerPrefs.GetFloat(FovKey, DefaultFov);

        fovSlider.onValueChanged.AddListener(OnFovChanged);
        UpdateFovText(fovSlider.value);
    }

    private void OnFovChanged(float value)
    {
        PlayerPrefs.SetFloat(FovKey, value);
        PlayerPrefs.Save();
        UpdateFovText(value);
    }

    private void UpdateFovText(float value)
    {
        if (fovValueText != null)
            fovValueText.text = Mathf.RoundToInt(value).ToString();
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

    // ── VSync ─────────────────────────────────────────────────────────────────
    private void InitializeVsync()
    {
        if (vsyncToggle == null) return;

        int saved = PlayerPrefs.GetInt(VsyncKey, 0);
        QualitySettings.vSyncCount = saved;
        vsyncToggle.isOn = saved == 1;
        vsyncToggle.onValueChanged.AddListener(OnVsyncChanged);
    }

    private void OnVsyncChanged(bool value)
    {
        QualitySettings.vSyncCount = value ? 1 : 0;
        PlayerPrefs.SetInt(VsyncKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ── Anti-Aliasing ─────────────────────────────────────────────────────────
    private static readonly int[] AntiAliasingValues = { 0, 2, 4, 8 };

    private void InitializeAntiAliasing()
    {
        if (antiAliasingDropdown == null) return;

        antiAliasingDropdown.ClearOptions();
        antiAliasingDropdown.AddOptions(new List<string> { "Off", "2x MSAA", "4x MSAA", "8x MSAA" });

        int savedIndex = PlayerPrefs.GetInt(AntiAliasingKey, 0);
        antiAliasingDropdown.value = savedIndex;
        antiAliasingDropdown.RefreshShownValue();

        QualitySettings.antiAliasing = AntiAliasingValues[savedIndex];
        antiAliasingDropdown.onValueChanged.AddListener(OnAntiAliasingChanged);
    }

    /// <summary>Applies the selected anti-aliasing level and saves the preference.</summary>
    private void OnAntiAliasingChanged(int index)
    {
        QualitySettings.antiAliasing = AntiAliasingValues[index];
        PlayerPrefs.SetInt(AntiAliasingKey, index);
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

    // ── FPS Display ───────────────────────────────────────────────────────────

    /// <summary>Loads saved FPS display preference and registers the toggle listener.</summary>
    private void InitializeShowFps()
    {
        if (showFpsToggle == null) return;

        showFpsToggle.isOn = PlayerPrefs.GetInt(ShowFpsKey, 0) == 1;
        showFpsToggle.onValueChanged.AddListener(OnShowFpsChanged);
    }

    /// <summary>Shows or hides the FPS counter and saves the preference.</summary>
    public void OnShowFpsChanged(bool value)
    {
        PlayerPrefs.SetInt(ShowFpsKey, value ? 1 : 0);
        PlayerPrefs.Save();
        FPSDisplay.Instance?.SetVisible(value);
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    /// <summary>Registers the reset button click listener.</summary>
    private void InitializeResetButton()
    {
        if (resetButton == null) return;

        resetButton.onClick.AddListener(ResetSettings);
    }

    /// <summary>Resets all settings to their default values and applies them immediately.</summary>
    public void ResetSettings()
    {
        // Gameplay
        if (sensitivitySlider    != null) sensitivitySlider.value    = DefaultSensitivity;
        if (invertXToggle        != null) invertXToggle.isOn         = false;
        if (invertYToggle        != null) invertYToggle.isOn         = false;
        if (fovSlider            != null) fovSlider.value            = DefaultFov;

        // Display
        if (fullscreenToggle     != null) fullscreenToggle.isOn      = DefaultFullscreen;
        if (qualityDropdown      != null) qualityDropdown.value      = DefaultQualityIndex;
        if (textureDropdown      != null) textureDropdown.value      = DefaultTextureLimit;
        if (vsyncToggle          != null) vsyncToggle.isOn           = false;
        if (antiAliasingDropdown != null) antiAliasingDropdown.value = DefaultAntiAliasingIndex;

        // Resolution: revert to the native screen resolution
        if (resolutionDropdown != null)
        {
            int idx = FindCurrentResolutionIndex();
            if (resolutionDropdown.value != idx)
                resolutionDropdown.value = idx;
            else
                OnResolutionChanged(idx);
        }

        // Audio
        if (masterVolumeSlider   != null) masterVolumeSlider.value   = DefaultVolume;
        if (sfxVolumeSlider      != null) sfxVolumeSlider.value      = DefaultVolume;
        if (musicVolumeSlider    != null) musicVolumeSlider.value    = DefaultVolume;

        // FPS Display
        if (showFpsToggle        != null) showFpsToggle.isOn         = false;
    }
}
