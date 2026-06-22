using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PerkHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Idle Settings")]
    float idleRotationSpeed;
    float idleRotationAmount;

    [Header("Hover Settings")]
    public float maxTilt = 15f;

    [Header("Spring Settings")]
    [Tooltip("How strongly the card snaps toward the target rotation.")]
    public float springStiffness = 200f;
    [Tooltip("How much the spring resists overshooting. Lower = bouncier.")]
    public float springDamping = 15f;

    [Header("Scale Settings")]
    public float hoverScale = 1.08f;
    public float scaleInDuration = 0.3f;
    public float scaleOutDuration = 0.2f;

    [Header("Holo FX")]
    [Tooltip("The card panel Image whose material has the HolographicFoil shader. Leave empty to auto-find a child Graphic.")]
    public Graphic fxTarget;
    [Tooltip("How strongly tilt feeds the shader's rainbow shift.")]
    public float tiltToShader = 0.06f;
    [Tooltip("How strongly flick speed feeds the shine flare.")]
    public float flareGain = 0.012f;
    [Tooltip("How fast the flick flare fades back to 0.")]
    public float flareDecay = 4f;
    [Tooltip("How fast the click burst fades back to 0.")]
    public float clickDecay = 3.5f;

    // The card root — one level up from this button child.
    private RectTransform cardRect;
    private bool isHovered;

    // Spring state — kept in Euler space (small angles, no gimbal concern).
    private Vector3 targetEuler;
    private Vector3 currentEuler;
    private Vector3 rotationVelocity;

    // Holo material driving.
    private Material _fxMat;
    private float _flare;
    private float _click;

    static readonly int ID_Tilt = Shader.PropertyToID("_Tilt");
    static readonly int ID_Flare = Shader.PropertyToID("_Flare");
    static readonly int ID_Click = Shader.PropertyToID("_Click");

    private void Start()
    {
        idleRotationSpeed = Random.Range(8, 12);
        idleRotationAmount = Random.Range(4, 8);

        // This script sits on the button child; the visual card to tilt is the parent.
        cardRect = transform.parent.GetComponent<RectTransform>();
        targetEuler = Vector3.zero;
        currentEuler = Vector3.zero;
        rotationVelocity = Vector3.zero;

        // Resolve the holo material instance. PerkCardFX clones the material in
        // Awake, so grab the live instance off the graphic here in Start.
        if (fxTarget == null)
            fxTarget = GetComponentInChildren<Graphic>();
        if (fxTarget != null)
            _fxMat = fxTarget.materialForRendering;   // the per-card cloned instance
    }

    private void Update()
    {
        if (isHovered)
        {
            // Map mouse position into the card's local space (-1 … +1 on each axis).
            Vector2 localPos = cardRect.InverseTransformPoint(Input.mousePosition);
            float normX = Mathf.Clamp(localPos.x / (cardRect.rect.width * 0.5f), -1f, 1f);
            float normY = Mathf.Clamp(localPos.y / (cardRect.rect.height * 0.5f), -1f, 1f);

            // Pitch: mouse above centre → top tilts away (negative X rotation in Unity UI).
            // Roll:  mouse right of centre → right edge tilts away (positive Y rotation).
            targetEuler = new Vector3(-normY * maxTilt, normX * maxTilt, 0f);
        }
        else
        {
            // Idle wobble — unscaled so it works while timeScale == 0.
            float t = Time.unscaledTime * idleRotationSpeed;
            targetEuler = new Vector3(
                Mathf.Sin(t) * idleRotationAmount,
                Mathf.Cos(t) * idleRotationAmount,
                0f
            );
        }

        // Critically-damped spring integration (semi-implicit Euler, unscaled).
        float dt = Time.unscaledDeltaTime;
        Vector3 displacement = targetEuler - currentEuler;
        Vector3 springForce = displacement * springStiffness;
        Vector3 dampForce = rotationVelocity * springDamping;

        rotationVelocity += (springForce - dampForce) * dt;
        currentEuler += rotationVelocity * dt;

        cardRect.localRotation = Quaternion.Euler(currentEuler);

        // ── Feed the holo shader ──────────────────────────────────
        if (_fxMat != null)
        {
            // Real tilt → rainbow shift (Y rotation = roll/left-right, X = pitch/up-down).
            Vector2 tilt = new Vector2(currentEuler.y, -currentEuler.x) * tiltToShader;
            _fxMat.SetVector(ID_Tilt, tilt);

            // Flick flare from how fast the card is currently rotating.
            float angSpeed = rotationVelocity.magnitude;
            _flare = Mathf.Max(_flare, Mathf.Clamp01(angSpeed * flareGain));
            _flare = Mathf.MoveTowards(_flare, 0f, flareDecay * dt);
            _fxMat.SetFloat(ID_Flare, _flare);

            // Click burst decay.
            _click = Mathf.MoveTowards(_click, 0f, clickDecay * dt);
            _fxMat.SetFloat(ID_Click, _click);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        // Kill any in-progress scale tween, then pop forward with an overshoot bounce.
        cardRect.DOKill(false);
        cardRect.DOScale(hoverScale, scaleInDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);   // SetUpdate(true) = unscaled, works at timeScale 0.
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        cardRect.DOKill(false);
        cardRect.DOScale(1f, scaleOutDuration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Bright holo burst on select.
        _click = 1f;
        if (_fxMat != null) _fxMat.SetFloat(ID_Click, 1f);
    }
}