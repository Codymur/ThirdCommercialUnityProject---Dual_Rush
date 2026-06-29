using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Shooter Enemy
/// — Once activated, moves into firing range, then strafes and shoots in bursts
/// — Backs away if player gets too close
/// — Dies to anything via TakeDamage(amount, hitDirection)
/// </summary>
public class ShooterEnemy : EnemyBase
{
    [Header("Shooting")]
    public float attackRange = 12f;   // Preferred firing distance (not detection)
    public float backupRange = 4f;    // Backs up if player closer than this
    public float damage = 10f;
    public int bulletsPerBurst = 3;
    public float timeBetweenShots = 0.15f; // Within a burst
    public float timeBetweenBursts = 2f;

    [Header("Lateral Movement")]
    public float strafeSpeed = 3f;
    public float strafeChangeTime = 1.2f;  // How often strafe direction flips

    [Header("Bullet")]
    public GameObject bulletPrefab;         // Assign a simple sphere prefab
    public Transform firePoint;             // Assign an empty on the capsule
    public float bulletSpeed = 20f;

    // ── Internal ──────────────────────────────────────────────────
    float burstTimer = 0f;
    float strafeTimer = 0f;
    int strafeDirection = 1;   // 1 or -1
    bool isBursting = false;
    int shotsRemaining = 0;
    float shotTimer = 0f;

    // ──────────────────────────────────────────────────────────────
    protected override void Start()
    {
        base.Start();
        agent.speed = strafeSpeed;
        agent.stoppingDistance = backupRange;
        strafeDirection = Random.value > 0.5f ? 1 : -1;
        burstTimer = 1.5f;
    }

    // ──────────────────────────────────────────────────────────────
    protected override void Update()
    {
        base.Update(); // handles Idle -> Alert transition on activation
        if (isDead || player == null || !isActivated) return;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case EnemyState.Idle:
                // Pre-activation only
                agent.ResetPath();
                break;

            case EnemyState.Alert:
                // Move toward player until in firing range
                agent.SetDestination(player.position);
                if (dist <= attackRange)
                    state = EnemyState.Attack;
                break;

            case EnemyState.Attack:
                HandleAttackMovement(dist);
                HandleShooting(dist);
                break;
        }
    }

    // ──────────────────────────────────────────────────────────────
    void HandleAttackMovement(float dist)
    {
        // Always face the player
        Vector3 lookDir = (player.position - transform.position).normalized;
        lookDir.y = 0f;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(lookDir),
            Time.deltaTime * 8f
        );

        // Back up if player is too close
        if (dist < backupRange)
        {
            Vector3 backupPos = transform.position - lookDir * 3f;
            agent.SetDestination(backupPos);
            return;
        }

        // Strafe laterally — change direction periodically
        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            strafeDirection *= -1;
            strafeTimer = strafeChangeTime + Random.Range(-0.3f, 0.3f);
        }

        Vector3 strafeTarget = transform.position + transform.right * strafeDirection * 3f;
        agent.SetDestination(strafeTarget);
    }

    // ──────────────────────────────────────────────────────────────
    void HandleShooting(float dist)
    {
        if (dist > attackRange) return;

        if (isBursting)
        {
            // Fire individual shots within burst
            shotTimer -= Time.deltaTime;
            if (shotTimer <= 0f && shotsRemaining > 0)
            {
                FireBullet();
                shotsRemaining--;
                shotTimer = timeBetweenShots;

                if (shotsRemaining <= 0)
                {
                    isBursting = false;
                    burstTimer = timeBetweenBursts + Random.Range(-0.3f, 0.5f);
                }
            }
        }
        else
        {
            // Countdown to next burst
            burstTimer -= Time.deltaTime;
            if (burstTimer <= 0f)
            {
                isBursting = true;
                shotsRemaining = bulletsPerBurst;
                shotTimer = 0f;
            }
        }
    }

    // ──────────────────────────────────────────────────────────────
    void FireBullet()
    {
        if (firePoint == null || bulletPrefab == null)
        {
            // No prefab set up yet — raycast fallback for early testing
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f,
                                (player.position - transform.position).normalized,
                                out hit, attackRange))
            {
                Target t = hit.collider.GetComponentInParent<Target>();
                if (t != null) t.TakeDamage(damage);
            }
            return;
        }

        // Spawn bullet and send it toward player's center
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Vector3 dir = (player.position + Vector3.up * 0.5f - firePoint.position).normalized;
        Rigidbody bRb = bullet.GetComponent<Rigidbody>();
        if (bRb != null)
        {
            bRb.isKinematic = false;
            bRb.linearVelocity = dir * bulletSpeed;
        }

        Destroy(bullet, 4f);
    }
}