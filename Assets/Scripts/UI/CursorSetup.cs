using UnityEngine;

public class CursorSetup : MonoBehaviour
{
    [Header("Follow")]
    [Tooltip("How quickly the cursor catches up to the mouse. Lower = floatier trail.")]
    [SerializeField] private float followSpeed = 18f;

    [Header("Sway / Tilt")]
    [Tooltip("How much the cursor tilts toward its movement direction.")]
    [SerializeField] private float tiltAmount = 0.35f;
    [Tooltip("Max tilt in degrees.")]
    [SerializeField] private float maxTilt = 22f;
    [Tooltip("How fast the tilt settles back.")]
    [SerializeField] private float tiltSmooth = 12f;

    [Header("Squash & Stretch")]
    [Tooltip("How much the cursor stretches when moving fast.")]
    [SerializeField] private float stretchAmount = 0.0015f;
    [Tooltip("Max stretch scale.")]
    [SerializeField] private float maxStretch = 0.35f;
    [Tooltip("How fast the scale settles back.")]
    [SerializeField] private float scaleSmooth = 14f;

    [Header("Click Press")]
    [Tooltip("How far the cursor shrinks while the button is held. 0.2 = 80% size.")]
    [SerializeField] private float clickShrink = 0.25f;
    [Tooltip("How much it overshoots (pops bigger) on release.")]
    [SerializeField] private float clickPop = 0.18f;
    [Tooltip("Spring stiffness of the click bounce. Higher = snappier.")]
    [SerializeField] private float clickStiffness = 220f;
    [Tooltip("Spring damping. Lower = bouncier.")]
    [SerializeField] private float clickDamping = 14f;

    [Header("Idle Bob")]
    [Tooltip("Gentle breathing motion when the cursor is still.")]
    [SerializeField] private float idleBobAmount = 0.04f;
    [SerializeField] private float idleBobSpeed = 2.5f;

    private Vector3 _pos;          // smoothed cursor position
    private Vector2 _vel;          // smoothed velocity (px/sec)
    private float _tilt;           // current tilt angle
    private Vector3 _scale;        // current squash/stretch scale
    private Transform _visual;     // child Image to tilt/scale

    private float _clickScale = 1f; // spring value: 1 = rest
    private float _clickVel;        // spring velocity

    private void Start()
    {
        Cursor.visible = false;

        _pos = Input.mousePosition;
        _scale = Vector3.one;

        _visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;

        Vector3 target = Input.mousePosition;

        // ── Spring follow ─────────────────────────────────────────
        Vector3 prev = _pos;
        _pos = Vector3.Lerp(_pos, target, 1f - Mathf.Exp(-followSpeed * dt));
        transform.position = _pos;

        // ── Velocity (smoothed) ───────────────────────────────────
        Vector2 instVel = (Vector2)(_pos - prev) / dt;
        _vel = Vector2.Lerp(_vel, instVel, 1f - Mathf.Exp(-12f * dt));
        float speed = _vel.magnitude;

        // ── Tilt toward horizontal motion ─────────────────────────
        float targetTilt = Mathf.Clamp(-_vel.x * tiltAmount, -maxTilt, maxTilt);
        _tilt = Mathf.Lerp(_tilt, targetTilt, 1f - Mathf.Exp(-tiltSmooth * dt));

        // ── Click press spring ────────────────────────────────────
        // While held → settle toward a shrunken size.
        // On release → kick the spring so it pops back past 1 and bounces.
        float clickTarget = Input.GetMouseButton(0) ? (1f - clickShrink) : 1f;

        if (Input.GetMouseButtonUp(0))
            _clickVel += clickPop * clickStiffness * 0.06f; // release pop

        float clickAccel = (clickTarget - _clickScale) * clickStiffness
                         - _clickVel * clickDamping;
        _clickVel += clickAccel * dt;
        _clickScale += _clickVel * dt;

        // ── Squash & stretch along movement ───────────────────────
        float stretch = Mathf.Min(speed * stretchAmount, maxStretch);
        Vector3 targetScale = new Vector3(1f + stretch, 1f - stretch * 0.6f, 1f);

        // idle bob when nearly still
        float idle = (speed < 30f) ? Mathf.Sin(Time.unscaledTime * idleBobSpeed) * idleBobAmount : 0f;
        targetScale += new Vector3(idle, idle, 0f);

        _scale = Vector3.Lerp(_scale, targetScale, 1f - Mathf.Exp(-scaleSmooth * dt));

        // ── Apply (movement scale × click spring) ─────────────────
        _visual.localRotation = Quaternion.Euler(0f, 0f, _tilt);
        _visual.localScale = _scale * _clickScale;
    }
}