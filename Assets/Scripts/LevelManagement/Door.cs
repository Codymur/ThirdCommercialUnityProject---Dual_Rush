using System.Collections;
using UnityEngine;

[RequireComponent(typeof(HingeJoint))]
[RequireComponent(typeof(Rigidbody))]
public class Door : MonoBehaviour
{
    [Header("Trigger")]
    public PassageTrigger passageTrigger;

    [Header("Linger")]
    [Tooltip("Duration in seconds for the door to swing back to its closed angle after the room is destroyed. " +
             "Keep this below RoomManager.doorLingerDuration so the door finishes before it is cleaned up.")]
    public float lingerLerpDuration = 1.5f;

    private Rigidbody rb;
    private HingeJoint hinge;
    private bool isLocked = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        hinge = GetComponent<HingeJoint>();
        rb.isKinematic = true; // frozen until unlocked

        if (passageTrigger != null)
            passageTrigger.SetActiving(false);
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    public void Open()
    {
        if (this == null || rb == null) return;
        isLocked = false;
        rb.isKinematic = false;
        Invoke(nameof(EnablePassage), 0.5f);
    }

    /// <summary>
    /// Freezes physics and smoothly rotates the door back to its closed position
    /// by unwinding the hinge angle to zero. Called when the room is destroyed.
    /// </summary>
    public void LerpZToZero()
    {
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Freeze the frame Rigidbody too so the whole doorframe doesn't fall.
        if (transform.parent != null)
        {
            Rigidbody frameRb = transform.parent.GetComponent<Rigidbody>();
            if (frameRb != null)
            {
                frameRb.isKinematic = true;
                frameRb.linearVelocity = Vector3.zero;
                frameRb.angularVelocity = Vector3.zero;
            }
        }

        StartCoroutine(HingeLerpRoutine());
    }

    IEnumerator HingeLerpRoutine()
    {
        // The hinge rotates the door around its world-space axis.
        // We capture the starting angle and lerp it back to 0 (closed).
        float startAngle = hinge.angle;

        // World-space axis the hinge rotates around, anchored at the hinge pivot.
        Vector3 hingeWorldAxis = transform.TransformDirection(hinge.axis);
        Vector3 hingeWorldAnchor = transform.TransformPoint(hinge.anchor);

        Quaternion startRot = transform.rotation;
        // Target: undo the hinge rotation entirely.
        Quaternion targetRot = Quaternion.AngleAxis(-startAngle, hingeWorldAxis) * startRot;

        float elapsed = 0f;
        while (elapsed < lingerLerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lingerLerpDuration;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.rotation = targetRot;
    }

    void EnablePassage()
    {
        if (passageTrigger != null)
            passageTrigger.SetActiving(true);
    }
}