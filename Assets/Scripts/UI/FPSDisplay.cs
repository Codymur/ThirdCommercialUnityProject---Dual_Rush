using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton FPS counter that persists across scene loads (DontDestroyOnLoad).
/// Call <see cref="SetVisible"/> or toggle via <see cref="ToggleVisibility"/> from any UI.
/// Visibility preference is persisted with PlayerPrefs.
/// </summary>
public class FPSDisplay : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static FPSDisplay Instance { get; private set; }

    // ── PlayerPrefs ───────────────────────────────────────────────────────────
    private const string VisibilityKey = "FPSDisplayVisible";

    // ── Settings ──────────────────────────────────────────────────────────────
    private const float UpdateInterval  = 0.25f;
    private const int   FontSize        = 20;
    private const float PanelWidth      = 90f;
    private const float PanelHeight     = 36f;
    private const float Padding         = 8f;

    // ── Runtime references ────────────────────────────────────────────────────
    private Canvas          _canvas;
    private GameObject      _panel;
    private TextMeshProUGUI _fpsText;

    // ── FPS tracking ──────────────────────────────────────────────────────────
    private float _elapsed;
    private int   _frames;
    private int   _currentFps;

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

        BuildUI();

        bool visible = PlayerPrefs.GetInt(VisibilityKey, 0) == 1;
        SetVisible(visible);
    }

    private void Update()
    {
        if (_panel == null || !_panel.activeSelf) return;

        _elapsed += Time.unscaledDeltaTime;
        _frames++;

        if (_elapsed >= UpdateInterval)
        {
            _currentFps = Mathf.RoundToInt(_frames / _elapsed);
            _fpsText.text = $"{_currentFps} FPS";
            _fpsText.color = GetFpsColor(_currentFps);
            _elapsed = 0f;
            _frames  = 0;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Shows or hides the FPS display and saves the preference.</summary>
    public void SetVisible(bool visible)
    {
        if (_panel != null)
            _panel.SetActive(visible);

        PlayerPrefs.SetInt(VisibilityKey, visible ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>Flips the current visibility state.</summary>
    public void ToggleVisibility()
    {
        bool next = _panel != null && !_panel.activeSelf;
        SetVisible(next);
    }

    /// <summary>Returns whether the display is currently visible.</summary>
    public bool IsVisible => _panel != null && _panel.activeSelf;

    // ── UI construction ───────────────────────────────────────────────────────
    private void BuildUI()
    {
        // Screen-space overlay canvas that lives on this GameObject
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 999;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // Semi-transparent background panel — top-left corner
        _panel = new GameObject("FPS_Panel");
        _panel.transform.SetParent(_canvas.transform, false);

        RectTransform panelRect = _panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot     = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(Padding, -Padding);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        Image bg = _panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f);

        // TMP label inside the panel
        GameObject textObj = new GameObject("FPS_Text");
        textObj.transform.SetParent(_panel.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin        = Vector2.zero;
        textRect.anchorMax        = Vector2.one;
        textRect.offsetMin        = Vector2.zero;
        textRect.offsetMax        = Vector2.zero;

        _fpsText                  = textObj.AddComponent<TextMeshProUGUI>();
        _fpsText.fontSize         = FontSize;
        _fpsText.fontStyle        = FontStyles.Bold;
        _fpsText.alignment        = TextAlignmentOptions.Center;
        _fpsText.text             = "-- FPS";
        _fpsText.color            = Color.white;
        _fpsText.raycastTarget    = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Color GetFpsColor(int fps)
    {
        if (fps >= 60) return Color.green;
        if (fps >= 30) return Color.yellow;
        return Color.red;
    }
}
