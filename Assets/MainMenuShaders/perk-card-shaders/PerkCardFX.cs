using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Feeds cursor state into a Dual Rush perk-card hover shader.
/// Put this on the SAME GameObject as the card's background Image (the dark
/// panel) and assign one of the PerkCard_*.shader materials to that Image.
///
/// It does NOT touch transform — scale / position / rotation stay fully under
/// PerkHoverEffect. It only drives shader uniforms:
///   _Hover   (0..1, eased)        _Mouse  (cursor UV over the card, 0..1)
///   _FxTime  (unscaled time)      _Aspect (card width / height)
/// plus the category glow (_Category / _UseCategory) or _AccentColor / _BaseColor.
///
/// The per-card material clone is created LAZILY (EnsureMaterial) so SetCategory /
/// SetAccent work even when called before Awake — e.g. while the parent panel is
/// still inactive during PerkSelectionUI.Show(). This fixes wrong first-open colors.
///
/// _FxTime is fed from Time.unscaledTime so the effect keeps animating while
/// the perk screen pauses the game (Time.timeScale == 0).
/// </summary>
[RequireComponent(typeof(Graphic))]
[DisallowMultipleComponent]
public class PerkCardFX : MonoBehaviour
{
    [Header("Colors")]
    [Tooltip("Manual accent (only used when the shader's Use Category Color is OFF, " +
             "i.e. after SetAccent). Movement #5BD0E0 · Combat #EF6B3A · Survival #F2C14E · Ember #FF6A14")]
    public Color accentColor = new Color(1f, 0.416f, 0.078f, 1f);   // ember
    [Tooltip("Idle base color of the card panel.")]
    public Color baseColor = new Color(0.105f, 0.121f, 0.149f, 1f); // gunmetal-800

    [Header("Feel")]
    [Tooltip("How fast hover eases in/out (higher = snappier).")]
    public float hoverSpeed = 12f;

    [Tooltip("Camera used to map the cursor. Leave empty for Screen Space - Overlay; auto-filled for Screen Space - Camera.")]
    public Camera uiCamera;

    Graphic _graphic;
    RectTransform _rect;
    Material _mat;
    float _hover;

    static readonly int ID_Hover = Shader.PropertyToID("_Hover");
    static readonly int ID_Mouse = Shader.PropertyToID("_Mouse");
    static readonly int ID_Aspect = Shader.PropertyToID("_Aspect");
    static readonly int ID_FxTime = Shader.PropertyToID("_FxTime");
    static readonly int ID_Accent = Shader.PropertyToID("_AccentColor");
    static readonly int ID_Base = Shader.PropertyToID("_BaseColor");
    static readonly int ID_Category = Shader.PropertyToID("_Category");
    static readonly int ID_UseCategory = Shader.PropertyToID("_UseCategory");

    /// <summary>
    /// Clone the card material exactly once so each card animates independently.
    /// Safe to call before Awake and while the GameObject (or a parent) is inactive —
    /// GetComponent and material assignment both work on inactive objects.
    /// </summary>
    void EnsureMaterial()
    {
        if (_mat != null) return;
        if (_graphic == null) _graphic = GetComponent<Graphic>();
        if (_graphic != null && _graphic.material != null)
        {
            _mat = new Material(_graphic.material);
            _graphic.material = _mat;
        }
    }

    void Awake()
    {
        _graphic = GetComponent<Graphic>();
        _rect = (RectTransform)transform;

        EnsureMaterial();   // no-op if SetCategory/SetAccent already cloned it

        if (uiCamera == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = canvas.worldCamera;
        }

        PushColors();
    }

    void OnEnable()
    {
        _hover = 0f;
        if (_mat != null) _mat.SetFloat(ID_Hover, 0f);
    }

    void Update()
    {
        if (_mat == null) return;

        Vector2 screenPos = Input.mousePosition;
        bool inside = RectTransformUtility.RectangleContainsScreenPoint(_rect, screenPos, uiCamera);

        // Exponential easing — frame-rate independent, unscaled so it runs at timeScale 0.
        float target = inside ? 1f : 0f;
        _hover = Mathf.Lerp(_hover, target, 1f - Mathf.Exp(-hoverSpeed * Time.unscaledDeltaTime));

        _mat.SetFloat(ID_Hover, _hover);
        _mat.SetFloat(ID_FxTime, Time.unscaledTime);
        float h = _rect.rect.height;
        _mat.SetFloat(ID_Aspect, h > 0.0001f ? _rect.rect.width / h : 1f);

        if (inside)
        {
            Vector2 local;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screenPos, uiCamera, out local))
            {
                Rect r = _rect.rect;
                float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
                float v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
                _mat.SetVector(ID_Mouse, new Vector4(u, v, 0f, 0f));
            }
        }
    }

    /// <summary>
    /// Drives the shader's <c>_Category</c> so the glow color is picked from the
    /// category slots in the material, and turns category mode ON. Clones the
    /// material on demand, so it works even before Awake / while inactive.
    /// Mapped BY NAME — independent of PerkType's underlying int values, and matches
    /// the shader's locked index convention (0 Movement · 1 Combat · 2 Survival).
    /// </summary>
    public void SetCategory(PerkSO.PerkType category)
    {
        EnsureMaterial();
        if (_mat == null) return;
        _mat.SetFloat(ID_Category, CategoryIndex(category));
        _mat.SetFloat(ID_UseCategory, 1f);   // ensure category-driven glow is active
    }

    static float CategoryIndex(PerkSO.PerkType category)
    {
        switch (category)
        {
            case PerkSO.PerkType.Movement: return 0f;
            case PerkSO.PerkType.Combat: return 1f;
            case PerkSO.PerkType.Survival: return 2f;
            default: return 0f;
        }
    }

    /// <summary>
    /// Manually recolor the card and switch the shader to accent mode
    /// (Use Category Color OFF). Use SetCategory instead for the normal flow.
    /// </summary>
    public void SetAccent(Color c)
    {
        accentColor = c;
        EnsureMaterial();
        if (_mat == null) return;
        _mat.SetColor(ID_Accent, c);
        _mat.SetFloat(ID_UseCategory, 0f);   // switch to manual accent
    }

    void PushColors()
    {
        if (_mat == null) return;
        _mat.SetColor(ID_Accent, accentColor);
        _mat.SetColor(ID_Base, baseColor);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) PushColors();
    }
#endif
}