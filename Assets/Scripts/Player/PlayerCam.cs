using UnityEngine;
using DG.Tweening;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;
    public Transform orientation;
    public Transform CamHolder;

    float xRotation;
    float yRotation;

    private float moveTiltX, moveTiltZ;
    private float diveTiltX, diveTiltZ;
    private Tween tiltTween;

    // ?????????????????????????????????????????????
    [Header("Head Bob")]
    public bool headBobEnabled = true;
    public float bobFrequency = 16f;
    public float bobVerticalAmp = 0.045f;
    public float bobHorizontalAmp = 0.02f;
    public float bobSmoothSpeed = 14f;
    public float bobStopSpeed = 28f;
    public float bobSpeedThreshold = 0.4f;

    // ?????????????????????????????????????????????
    [Header("Jump Spring")]
    public float jumpStretchKick = 0.35f;
    public float jumpSpringStiffness = 120f;
    public float jumpSpringDamping = 7f;

    // ?????????????????????????????????????????????
    [Header("Landing Spring")]
    public float minLandingFallSpeed = 3f;

    public float landingSquashMult = 0.08f;
    public float landingSquashMax = 0.40f;
    public float landingSpringStiffness = 130f;
    public float landingSpringDamping = 7f;

    // ?????????????????????????????????????????????
    [Header("Jump Camera Kick")]
    public float jumpFovZoomOut = 4f;
    public float jumpPitchUp = 2f;
    public float jumpKickDuration = 0.35f;
    public float jumpRecoverDuration = 0.6f;

    // ?????????????????????????????????????????????
    [Header("Landing Camera Kick")]
    public float landFovZoomIn = 3f;
    public float landPitchDown = 2.5f;
    public float landKickDuration = 0.25f;
    public float landRecoverDuration = 0.7f;

    // ?????????????????????????????????????????????
    [Header("Kick Shake")]
    public float kickShakeStrength = 0.06f;
    public float kickShakeStiffness = 180f;
    public float kickShakeDamping = 14f;

    // ?????????????????????????????????????????????
    [Header("Base FOV")]
    public float baseFov = 90f;

    // ?????????????????????????????????????????????
    [Header("References")]
    public Rigidbody playerRb;
    public PlayerMovementTutorial playerMovement;
    public PlayerDive diveScript; // Assign in Inspector

    // Bob state
    private float bobTimer;
    private float bobAmplitude;
    private Vector3 bobCurrentOffset;
    private Vector3 bobOriginLocalPos;

    // Spring state
    private float jumpSpringPos, jumpSpringVel;
    private float squashSpringPos, squashSpringVel;

    // Kick shake spring state
    private float shakeXPos, shakeXVel;
    private float shakeYPos, shakeYVel;

    // Pitch kick state
    private float pitchKickOffset = 0f;
    private Tween pitchKickTween;
    private Tween fovTween;

    private bool wasGrounded = true;
    private float previousYVel;

    private const string SensitivityKey = "MouseSensitivity";
    private const float DefaultSensitivity = 300f;
    private const string InvertXKey = "InvertX";
    private const string InvertYKey = "InvertY";
    private const string FovKey = "FieldOfView";
    private const float DefaultFov = 90f;

    private bool invertX;
    private bool invertY;

    // ?????????????????????????????????????????????
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        bobOriginLocalPos = transform.localPosition;
        LoadSensitivity();
        LoadFov();
    }

    /// <summary>Reads mouse sensitivity and invert flags from PlayerPrefs and applies them.</summary>
    public void LoadSensitivity()
    {
        float saved = PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
        sensX = saved;
        sensY = saved;

        invertX = PlayerPrefs.GetInt(InvertXKey, 0) == 1;
        invertY = PlayerPrefs.GetInt(InvertYKey, 0) == 1;
    }

    /// <summary>Reads the saved field of view from PlayerPrefs and applies it to the camera and baseFov.</summary>
    public void LoadFov()
    {
        float savedFov = PlayerPrefs.GetFloat(FovKey, DefaultFov);
        baseFov = savedFov;
        Camera cam = GetComponent<Camera>();
        if (cam != null)
            cam.fieldOfView = savedFov;
    }

    private void Update()
    {
        HandleMouseLook();
        if (headBobEnabled) HandleHeadBob();
    }

    // ?? Mouse Look ???????????????????????????????
    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        if (invertX) mouseX = -mouseX;
        if (invertY) mouseY = -mouseY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        CamHolder.rotation = Quaternion.Euler(xRotation + pitchKickOffset, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }

    // ?? Head Bob + Springs ???????????????????????
    private void HandleHeadBob()
    {
        if (playerRb == null || playerMovement == null) return;

        bool isDiving = diveScript != null && diveScript.isDiving;

        bool grounded = playerMovement.GetGrounded();
        float hSpeed = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z).magnitude;
        float dt = Time.deltaTime;

        // ?? JUMP ?????????????????????????????????
        if (!isDiving && !grounded && wasGrounded)
        {
            jumpSpringVel += jumpStretchKick;
            KickFov(baseFov + jumpFovZoomOut, jumpKickDuration, jumpRecoverDuration);
            KickPitch(-jumpPitchUp, jumpKickDuration, jumpRecoverDuration);
        }

        // ?? LAND ?????????????????????????????????
        if (!isDiving && grounded && !wasGrounded)
        {
            float fallSpeed = Mathf.Abs(previousYVel);
            if (fallSpeed >= minLandingFallSpeed)
            {
                float squashKick = Mathf.Clamp(
                    fallSpeed * landingSquashMult, 0.05f, landingSquashMax);
                squashSpringVel -= squashKick;

                KickFov(baseFov - landFovZoomIn, landKickDuration, landRecoverDuration);
                KickPitch(landPitchDown, landKickDuration, landRecoverDuration);
            }
        }

        // ?? Tick springs ??????????????????????????
        StepSpring(ref jumpSpringPos, ref jumpSpringVel,
                   jumpSpringStiffness, jumpSpringDamping, dt);
        StepSpring(ref squashSpringPos, ref squashSpringVel,
                   landingSpringStiffness, landingSpringDamping, dt);
        StepSpring(ref shakeXPos, ref shakeXVel,
                   kickShakeStiffness, kickShakeDamping, dt);
        StepSpring(ref shakeYPos, ref shakeYVel,
                   kickShakeStiffness, kickShakeDamping, dt);

        // ?? Walking bob — suppressed while diving ???
        // Force targetAmplitude to 0 during dive so bob fades out naturally
        // via bobStopSpeed rather than snapping off.
        bool isMoving = !isDiving && grounded && hSpeed > bobSpeedThreshold;
        float targetAmplitude = isMoving ? Mathf.Clamp01(hSpeed / 8f) : 0f;
        float ampLerpSpeed = isMoving ? bobSmoothSpeed : bobStopSpeed;
        bobAmplitude = Mathf.Lerp(bobAmplitude, targetAmplitude, dt * ampLerpSpeed);

        if (bobAmplitude > 0.001f)
        {
            bobTimer += dt * bobFrequency * bobAmplitude;

            bobCurrentOffset = new Vector3(
                Mathf.Sin(bobTimer) * bobHorizontalAmp * bobAmplitude,
                Mathf.Abs(Mathf.Sin(bobTimer)) * bobVerticalAmp * bobAmplitude,
                0f
            );
        }
        else
        {
            bobCurrentOffset = Vector3.zero;
            bobTimer = 0f;
        }

        // ?? Apply everything ??????????????????????
        transform.localPosition = bobOriginLocalPos + new Vector3(
            bobCurrentOffset.x + shakeXPos,
            bobCurrentOffset.y + jumpSpringPos + squashSpringPos + shakeYPos,
            0f
        );

        wasGrounded = grounded;
        previousYVel = playerRb.linearVelocity.y;
    }

    // ?? FOV kick then recover ?????????????????????
    private void KickFov(float target, float kickDuration, float recoverDuration)
    {
        Camera cam = GetComponent<Camera>();
        fovTween?.Kill();
        fovTween = cam.DOFieldOfView(target, kickDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                fovTween = cam.DOFieldOfView(baseFov, recoverDuration)
                    .SetEase(Ease.InOutSine);
            });
    }

    // ?? Pitch kick then recover ???????????????????
    private void KickPitch(float degrees, float kickDuration, float recoverDuration)
    {
        pitchKickTween?.Kill();
        DOTween.To(() => pitchKickOffset, x => pitchKickOffset = x, degrees, kickDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                pitchKickTween = DOTween.To(
                    () => pitchKickOffset,
                    x => pitchKickOffset = x,
                    0f, recoverDuration)
                    .SetEase(Ease.InOutSine);
            });
    }

    // ?? Harmonic oscillator ???????????????????????
    private void StepSpring(ref float pos, ref float vel,
                            float stiffness, float damping, float dt)
    {
        float force = -stiffness * pos - damping * vel;
        vel += force * dt;
        pos += vel * dt;
    }

    // ?? Public API ????????????????????????????????
    public void DoFov(float endValue)
    {
        GetComponent<Camera>().DOFieldOfView(endValue, 0.25f);
    }

    public void DoTilt(float zTilt)
    {
        moveTiltZ = zTilt;
        ApplyTilt(0.25f);
    }

    public void DoTiltX(float xTilt)
    {
        moveTiltX = xTilt;
        ApplyTilt(0.25f);
    }

    public void DoDiveTiltX(float xTilt, float duration = 0.25f)
    {
        diveTiltX = xTilt;
        ApplyTilt(duration);
    }

    public void DoDiveTiltZ(float zTilt, float duration = 0.25f)
    {
        diveTiltZ = zTilt;
        ApplyTilt(duration);
    }

    public void TriggerFovKick(float targetFov, float kickDuration, float recoverDuration)
        => KickFov(targetFov, kickDuration, recoverDuration);

    public void TriggerPitchKick(float degrees, float kickDuration, float recoverDuration)
        => KickPitch(degrees, kickDuration, recoverDuration);

    public void TriggerKickShake(float strength)
    {
        float s = strength * kickShakeStrength;
        shakeXVel += s * (Random.value > 0.5f ? 1f : -1f);
        shakeYVel += s * (Random.value > 0.5f ? 1f : -1f);
    }

    private void ApplyTilt(float duration)
    {
        if (tiltTween != null && tiltTween.IsActive()) tiltTween.Kill();
        Vector3 target = new Vector3(moveTiltX + diveTiltX, 0f, moveTiltZ + diveTiltZ);
        tiltTween = transform.DOLocalRotate(target, duration).SetEase(Ease.OutSine);
    }
}