using UnityEngine;

/// <summary>
/// Marker component for the enemy's head bone collider.
/// EnemyBase.setColliderState skips any collider on a GameObject carrying this component,
/// keeping it enabled throughout the enemy's lifetime:
///   - Alive: receives headshot raycasts from GunController
///   - Dead:  participates in ragdoll physics
/// The Rigidbody is initialized kinematic here so the Animator drives bone position
/// while alive. EnemyBase.setRigidbodyState(false) flips it to non-kinematic on death.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyHeadShot : MonoBehaviour
{
    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;
    }
}

