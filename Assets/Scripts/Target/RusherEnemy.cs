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

    // Cached player physics/health refs. The player rig splits these across
    // child objects (Rigidbody on Player, Collider on PlayerObject), so a plain
    // GetComponent on the player root returns null. Resolve them once here.
    Target playerTarget;
    Rigidbody playerRb;

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
        // default) decelerates the agent as it nears its target.
        agent.autoBraking = false;

        // Snap to full speed and recover instantly from any momentary avoidance
        // slowdown, instead of ramping back up over the default acceleration.
        agent.acceleration = 40f;

        // High avoidance priority (lower number = higher priority). A charger
        // should plow past a strafing shooter, not yield to it.
        agent.avoidancePriority = 30;

        // Resolve the player's split components once. The collider lives on the
        // PlayerObject child, the Rigidbody on the Player root, and Target may be
        // on either — search the whole hierarchy so none of them come back null.
        if (player != null)
        {
            playerTarget = player.GetComponentInChildren<Target>();
            playerRb = player.GetComponentInChildren<Rigidbody>();
        }

        // Stop the rusher's capsule from physically grinding against the player's
        // capsule at melee range. The player's collider is on a child object, so
        // we sweep every collider on both sides. Raycasts/spherecasts are
        // unaffected, so kick/dive detection and melee knockback still work.
        IgnoreCollisionsWithPlayer();
    }

    void IgnoreCollisionsWithPlayer()
    {
        if (player == null) return;

        Collider[] myCols = GetComponentsInChildren<Collider>();
        Collider[] playerCols = player.GetComponentsInChildren<Collider>();

        foreach (Collider mine in myCols)
        {
            if (mine == null) continue;
            foreach (Collider theirs in playerCols)
            {
                if (theirs == null) continue;
                Physics.IgnoreCollision(mine, theirs, true);
            }
        }
    }

    /// <summary>
    /// ResetPath throws if the agent is off the NavMesh or disabled (unlike
    /// SetDestination, which fails silently). Guard every call so a single
    /// off-mesh frame — at spawn or during room teardown — can't throw.
    /// </summary>
    void SafeResetPath()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.ResetPath();
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
                SafeResetPath();
                isWindingUp = false;
                windupTimer = 0f;
                break;

            case EnemyState.Alert:
                // Brief windup before first charge — visual tell for the player
                if (!isWindingUp)
                {
                    isWindingUp = true;
                    windupTimer = chargeWindup;
                    SafeResetPath(); // Stand still during windup
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
    /// continuously moving.
    /// </summary>
    void HandleCharge()
    {

        // If we're mid-doorway-link, let the manual traversal finish first.
        if (isTraversingLink) return;

        // Step onto a link this frame? Hand off to the walking traversal.
        if (agent.isOnNavMesh && agent.isOnOffMeshLink)
        {
            HandleLinkTraversal();
            return;
        }

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

        // Hit player (cached — resolved across the player hierarchy in Start)
        if (playerTarget != null)
            playerTarget.TakeDamage(meleeDamage);

        // Slight knockback on the player's Rigidbody
        if (playerRb != null)
        {
            Vector3 knockDir = (player.position - transform.position).normalized + Vector3.up * 0.3f;
            playerRb.AddForce(knockDir * 8f, ForceMode.Impulse);
        }

        meleeTimer = meleeCooldown;
    }
}