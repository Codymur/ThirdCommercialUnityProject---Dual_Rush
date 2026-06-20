using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Rusher Enemy
/// - Charges directly at the player on detection
/// - No shooting - pure melee threat
/// - Tankier than ShooterEnemy
/// - Kick, throw, dive impact all kill it via TakeDamage()
/// </summary>
public class RusherEnemy : EnemyBase
{
    [Header("Rusher Settings")]
    public float chargeSpeed = 6f;
    public float meleeRange = 1.8f;
    public float meleeDamage = 20f;
    public float meleeCooldown = 1.2f;
    public float chargeWindup = 0.6f;  // Brief pause before full charge - readable tell

    // Internal state
    float meleeTimer = 0f;
    float windupTimer = 0f;
    bool isWindingUp = false;

    // Radius used to search for the nearest NavMesh point when the agent is off-mesh.
    private const float NavMeshWarpSearchRadius = 3f;

    protected override void Start()
    {
        base.Start();

        // Rushers are tankier - override default Target health
        health = 30f;
        agent.speed = chargeSpeed;
        agent.angularSpeed = 360f;           // Turns fast - hard to juke
        agent.stoppingDistance = meleeRange * 0.8f;
    }

    protected override void Update()
    {
        base.Update(); // handles Idle -> Alert -> Attack transitions
        if (isDead || player == null || !isActivated) return;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case EnemyState.Idle:
                agent.ResetPath();
                // Clear windup state when returning to Idle so the next
                // detection cycle always starts a clean full-duration tell.
                isWindingUp = false;
                windupTimer = 0f;
                break;

            case EnemyState.Alert:
                // Brief windup before first charge - visual tell for the player
                if (!isWindingUp)
                {
                    isWindingUp = true;
                    windupTimer = chargeWindup;
                    agent.ResetPath(); // Stand still during windup
                }

                windupTimer -= Time.deltaTime;
                if (windupTimer <= 0f)
                {
                    isWindingUp = false;
                    state = EnemyState.Attack;
                }
                break;

            case EnemyState.Attack:
                HandleCharge(dist);
                HandleMelee(dist);
                break;
        }
    }

    /// <summary>
    /// Moves the agent toward the player each frame.
    /// Includes a NavMesh warp fallback for agents that spawned off-mesh
    /// (common when a spawn point sits outside the NavMeshSurface bounds).
    /// </summary>
    void HandleCharge(float dist)
    {
        // Guard: if the agent is not on any NavMesh surface, try to recover by
        // warping it to the nearest valid position. This is the most common
        // reason a rusher silently refuses to move - SetDestination returns false
        // without any console error when isOnNavMesh is false.
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NavMeshWarpSearchRadius, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                Debug.LogWarning($"[RusherEnemy] '{name}' was off NavMesh — warped to nearest point {hit.position}. " +
                                 $"Check that the NavMeshSurface in this room covers spawn point {transform.position}.", this);
            }
            else
            {
                Debug.LogError($"[RusherEnemy] '{name}' is off NavMesh and no point found within {NavMeshWarpSearchRadius}m. " +
                               $"Spawn position: {transform.position}. Expand the NavMeshSurface bounds to include all spawn points.", this);
                return;
            }
        }

        // Always charge directly at player - no strafing, no backing up.
        // This makes it readable and gives the player a fair chance to dive/kick.
        if (!agent.SetDestination(player.position))
        {
            Debug.LogWarning($"[RusherEnemy] '{name}' SetDestination failed. " +
                             $"The player may be on a disconnected NavMesh island — " +
                             $"check that room NavMeshSurfaces overlap at doorways.", this);
        }

        // Face player
        Vector3 lookDir = (player.position - transform.position).normalized;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDir),
                Time.deltaTime * 12f
            );
        }
    }

    void HandleMelee(float dist)
    {
        meleeTimer -= Time.deltaTime;
        if (meleeTimer > 0f) return;
        if (dist > meleeRange) return;

        // Hit player
        Target playerTarget = player.GetComponent<Target>();
        if (playerTarget != null)
            playerTarget.TakeDamage(meleeDamage);

        // Slight knockback on the player's Rigidbody
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 knockDir = (player.position - transform.position).normalized + Vector3.up * 0.3f;
            playerRb.AddForce(knockDir * 8f, ForceMode.Impulse);
        }

        meleeTimer = meleeCooldown;
    }
}
