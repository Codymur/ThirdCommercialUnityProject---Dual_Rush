using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Rusher Enemy
/// - Charges directly at the player once activated
/// - No shooting — pure melee threat
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
    public float chargeWindup = 0.6f;  // Brief pause before full charge — readable tell

    // Internal state
    float meleeTimer = 0f;
    float windupTimer = 0f;
    bool isWindingUp = false;

    // Radius used to search for the nearest NavMesh point when the agent is off-mesh.
    private const float NavMeshWarpSearchRadius = 3f;

    // How far above/below the player we still accept a valid NavMesh point when
    // projecting their position down onto the mesh (covers an airborne player).
    private const float DestinationSampleHeight = 4f;

    protected override void Start()
    {
        base.Start();

        // Rushers are tankier — override default Target health
        health = 20f;
        agent.speed = chargeSpeed;
        agent.angularSpeed = 360f;           // Turns fast — hard to juke
        agent.stoppingDistance = meleeRange * 0.8f;

        // Charger must never ease toward its destination. autoBraking (on by
        // default) decelerates the agent as it nears its target, and since the
        // target is the player, it crawls whenever it catches up.
        agent.autoBraking = false;

        // Stop the rusher's solid capsule from physically grinding against the
        // player's capsule at melee range. IgnoreCollision removes only the
        // depenetration contact between this pair — raycasts and spherecasts are
        // unaffected, so kick/dive detection and melee knockback still work.
        if (player != null)
        {
            Collider playerCol = player.GetComponent<Collider>();
            if (playerCol != null && col != null)
                Physics.IgnoreCollision(col, playerCol, true);
        }
    }

    protected override void Update()
    {
        base.Update(); // handles Idle -> Alert transition on activation
        if (isDead || player == null || !isActivated) return;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case EnemyState.Idle:
                // Pre-activation only — base.Update flips us to Alert
                // the moment the door opens.
                agent.ResetPath();
                isWindingUp = false;
                windupTimer = 0f;
                break;

            case EnemyState.Alert:
                // Brief windup before first charge — visual tell for the player
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
                HandleCharge();
                HandleMelee(dist);
                break;
        }
    }

    /// <summary>
    /// Drives the agent straight at the player every frame.
    /// Re-pathing each frame (no throttle, no pathPending gate) keeps the agent
    /// continuously moving — throttling the destination is what caused the agent
    /// to reach a stale endpoint and stop while the player was moving.
    /// </summary>
    void HandleCharge()
    {
        // Guard: if the agent fell off the NavMesh, warp it back to the nearest
        // valid point. SetDestination silently fails when isOnNavMesh is false.
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit warpHit, NavMeshWarpSearchRadius, NavMesh.AllAreas))
            {
                agent.Warp(warpHit.position);
                Debug.LogWarning($"[RusherEnemy] '{name}' was off NavMesh — warped to {warpHit.position}. " +
                                 $"Check that the NavMeshSurface covers spawn point {transform.position}.", this);
            }
            else
            {
                Debug.LogError($"[RusherEnemy] '{name}' is off NavMesh and no point found within {NavMeshWarpSearchRadius}m. " +
                               $"Spawn position: {transform.position}. Expand the NavMeshSurface bounds.", this);
                return;
            }
        }

        // Project the player's position onto the NavMesh so an airborne player
        // still gives a reachable target point on the floor below them.
        Vector3 targetPoint = player.position;
        if (NavMesh.SamplePosition(player.position, out NavMeshHit destHit, DestinationSampleHeight, NavMesh.AllAreas))
            targetPoint = destHit.position;

        agent.SetDestination(targetPoint);

        // Face player
        Vector3 lookDir = player.position - transform.position;
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