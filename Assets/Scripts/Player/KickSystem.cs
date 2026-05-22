using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// First-person melee kick. Attach to the Player root (same object as PlayerMovementTutorial).
/// Performs a short-range raycast from the FPS camera and applies damage + physics force
/// to whatever is hit. Drives the FootModel DOTween animation and integrates with
/// PlayerCam's pitch kick for camera feel.
/// </summary>
public class KickSystem : MonoBehaviour
{
    // ── Input ────────────────────────────────────────────────────────────────
    [Header("Input")]
    public KeyCode kickKey = KeyCode.F;

    // ── Kick Stats ───────────────────────────────────────────────────────────
    [Header("Kick Stats")]
    public float kickRange = 2.5f;
    public float kickDamage = 25f;
    public float kickForce = 600f;
    public float kickUpwardForce = 150f;
    public float kickCooldown = 0.8f;

    // ── Foot Animation ───────────────────────────────────────────────────────
    [Header("Foot Animation")]
    public Transform footModel;

    [Tooltip("How far the foot travels forward along its local Z axis.")]
    public float kickExtendZ = 0.35f;

    [Tooltip("How far the foot rises along its local Y axis during the kick.")]
    public float kickRiseY = 0.08f;

    [Tooltip("Additional X rotation (degrees) applied during the kick swing.")]
    public float kickRotationX = -35f;

    [Tooltip("Duration of the forward swing phase.")]
    public float kickExtendDuration = 0.12f;

    [Tooltip("Duration of the retraction phase back to rest.")]
    public float kickRetractDuration = 0.22f;

    [Tooltip("When during the extend phase the hit registers (seconds). Should be less than kickExtendDuration. " +
             "Lower values match OutQuint's visual peak which arrives well before the full extend time.")]
    public float kickHitDelay = 0.07f;

    // ── Hands Pullback ───────────────────────────────────────────────────────
    [Header("Hands Pullback")]
    public GunWallAvoidance wallAvoidance;

    [Tooltip("How far the hands pull back along their local Z axis during the kick.")]
    public float handsPullbackZ = 0.18f;

    [Tooltip("Duration for the hands to lerp back to rest after the kick. " +
             "GunWallAvoidance.moveSpeed controls the actual blend speed.")]
    public float handsPullbackHoldDuration = 0.3f;
    [Header("Camera Feel")]
    public float kickPitchDegrees = 6f;
    public float kickPitchDuration = 0.08f;
    public float kickPitchRecoverDuration = 0.25f;

    // ── Audio ────────────────────────────────────────────────────────────────
    [Header("Audio")]
    public AudioSource kickAudioSource;
    public AudioClip[] kickSoundClips;
    public AudioClip[] kickHitSoundClips;
    public float kickVolumeMin = 0.85f;
    public float kickVolumeMax = 1.0f;
    public float kickPitchMin = 0.9f;
    public float kickPitchMax = 1.1f;

    // ── Kickable Layers ──────────────────────────────────────────────────────
    [Header("Kickable Layers")]
    [Tooltip("Only objects on these layers can be hit by the kick raycast.")]
    public LayerMask kickableLayers = Physics.AllLayers;

    // ── References ───────────────────────────────────────────────────────────
    [Header("References")]
    public Camera fpsCam;
    public PlayerCam playerCam;

    // ── Private State ────────────────────────────────────────────────────────
    private bool isReady = true;
    private int lastKickSoundIndex = -1;
    private int lastHitSoundIndex = -1;

    // Cached rest pose — captured once in Start so retraction always returns here.
    private Vector3 footRestPosition;
    private Vector3 footRestRotation;

    // Active DOTween sequences so overlapping kicks are handled cleanly.
    private Sequence footSequence;
    private Coroutine handsPullbackCoroutine;

    private void Start()
    {
        if (footModel != null)
        {
            footRestPosition = footModel.localPosition;
            footRestRotation = footModel.localEulerAngles;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(kickKey) && isReady)
        {
            StartCoroutine(PerformKick());
        }
    }

    private IEnumerator PerformKick()
    {
        isReady = false;

        PlayKickSound(kickSoundClips, ref lastKickSoundIndex);
        TriggerCameraKick();
        AnimateFootKick();
        PullbackHands();

        // Wait for the visual apex (earlier than full extend due to OutQuint front-loading).
        yield return new WaitForSeconds(kickHitDelay);

        RaycastHit hit;
        if (Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out hit, kickRange, kickableLayers))
        {
            Debug.Log(hit.transform.gameObject.name);
            ApplyDamage(hit);
            ApplyForce(hit);
            PlayKickSound(kickHitSoundClips, ref lastHitSoundIndex);
        }

        yield return new WaitForSeconds(kickCooldown - kickHitDelay);
        isReady = true;
    }

    private void PullbackHands()
    {
        if (wallAvoidance == null) return;

        if (handsPullbackCoroutine != null)
            StopCoroutine(handsPullbackCoroutine);

        handsPullbackCoroutine = StartCoroutine(HandsPullbackCoroutine());
    }

    private IEnumerator HandsPullbackCoroutine()
    {
        // Apply the offset immediately — GunWallAvoidance.Update lerps toward it each frame.
        wallAvoidance.kickOffset = new Vector3(0f, 0f, -handsPullbackZ);

        yield return new WaitForSeconds(handsPullbackHoldDuration);

        // Clear the offset; GunWallAvoidance.Update lerps back to rest naturally.
        wallAvoidance.kickOffset = Vector3.zero;
    }

    private void AnimateFootKick()
    {
        if (footModel == null) return;

        // Kill any in-flight sequence before starting a new one.
        footSequence?.Kill();
        footModel.localPosition = footRestPosition;
        footModel.localEulerAngles = footRestRotation;

        Vector3 extendedPosition = footRestPosition + new Vector3(0f, kickRiseY, kickExtendZ);
        Vector3 extendedRotation = footRestRotation + new Vector3(kickRotationX, 0f, 0f);

        footSequence = DOTween.Sequence();

        // Phase 1 — swing forward.
        footSequence.Append(
            footModel.DOLocalMove(extendedPosition, kickExtendDuration)
                     .SetEase(Ease.OutQuint)
        );
        footSequence.Join(
            footModel.DOLocalRotate(extendedRotation, kickExtendDuration, RotateMode.Fast)
                     .SetEase(Ease.OutQuint)
        );

        // Phase 2 — retract to rest.
        footSequence.Append(
            footModel.DOLocalMove(footRestPosition, kickRetractDuration)
                     .SetEase(Ease.InOutSine)
        );
        footSequence.Join(
            footModel.DOLocalRotate(footRestRotation, kickRetractDuration, RotateMode.Fast)
                     .SetEase(Ease.InOutSine)
        );
    }

    private void ApplyDamage(RaycastHit hit)
    {
        Target target = hit.collider.GetComponentInParent<Target>();
        if (target != null)
        {
            // Pass forward direction so the enemy tips away from the kick
            target.TakeDamage(kickDamage, fpsCam.transform.forward);
        }
    }

    private void ApplyForce(RaycastHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null) return;

        Vector3 direction = (hit.point - fpsCam.transform.position).normalized;
        rb.AddForce(direction * kickForce + Vector3.up * kickUpwardForce, ForceMode.Impulse);
    }

    private void TriggerCameraKick()
    {
        if (playerCam == null) return;
        playerCam.TriggerPitchKick(kickPitchDegrees, kickPitchDuration, kickPitchRecoverDuration);
        playerCam.TriggerKickShake(1f);
    }

    private void PlayKickSound(AudioClip[] clips, ref int lastIndex)
    {
        if (kickAudioSource == null || clips == null || clips.Length == 0) return;

        int index;
        do
        {
            index = Random.Range(0, clips.Length);
        } while (index == lastIndex && clips.Length > 1);

        lastIndex = index;

        kickAudioSource.volume = Random.Range(kickVolumeMin, kickVolumeMax);
        kickAudioSource.pitch = Random.Range(kickPitchMin, kickPitchMax);
        kickAudioSource.PlayOneShot(clips[index]);
    }

    private void OnDestroy()
    {
        footSequence?.Kill();
    }
}
