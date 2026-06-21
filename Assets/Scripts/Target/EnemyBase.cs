using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.AI;

/// <summary>
/// Inherits Target (health + damage flash).
/// Adds: NavMeshAgent control, death physics, ragdoll hook.
/// ShooterEnemy and RusherEnemy both inherit from this.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class EnemyBase : Target
{
    [Header("Detection")]
    public float detectionRange = 15f;
    public float losePlayerRange = 20f;

    [Header("References")]
    public Transform player;              // Assign in Inspector or auto-found in Start

    // ?? Internal refs ??????????????????????????????????????????????
    protected NavMeshAgent agent;
    protected Rigidbody rb;
    protected CapsuleCollider col;
    protected bool isDead = false;

    public event Action<EnemyBase> OnDeath;

    // ?? State ??????????????????????????????????????????????????????
    public enum EnemyState { Idle, Alert, Attack }
    protected EnemyState state = EnemyState.Idle;

    // Whether this enemy's room has been entered by the player.
    // Enemies stay in Idle and cannot attack until this is true.
    protected bool isActivated = false;

    // ?? Events � hook score system / perk triggers later ???????????
    public System.Action<EnemyBase> OnEnemyDeath;

    // ??????????????????????????????????????????????????????????????
    protected override void Awake()
    {
        base.Awake(); // runs Target.Awake() � saves renderer materials

        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        rb.isKinematic = true; // NavMeshAgent drives movement while alive
    }

    protected virtual void Start()
    {
        // Auto-find player if not assigned
        if (player == null)
        {
            player = PlayerMainChecking.Instance;

            if (player == null)
                Debug.LogWarning($"[EnemyBase] '{name}' could not find player via PlayerMainChecking.Instance. Enemy will be inactive.", this);
        }

        // Warn early if the agent failed to land on the NavMesh.
        if (!agent.isOnNavMesh)
            Debug.LogWarning($"[EnemyBase] '{name}' NavMeshAgent is not on a NavMesh after Start. " +
                             $"Check that NavMeshSurface covers spawn point {transform.position}.", this);
    }

    // ──────────────────────────────────────────────────────────────────────────
    protected virtual void Update()
    {
        if (isDead || player == null || !isActivated) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case EnemyState.Idle:
                if ( distToPlayer <= detectionRange)
                    state = EnemyState.Alert;
                break;

            case EnemyState.Alert:
                if (distToPlayer > losePlayerRange)
                    state = EnemyState.Idle;
                break;

            case EnemyState.Attack:
                if (distToPlayer > losePlayerRange)
                    state = EnemyState.Idle;
                break;
        }
    }

    // ??????????????????????????????????????????????????????????????
    protected override void Die(Vector3 hitDirection)
    {
        if (isDead) return;
        isDead = true;

        OnEnemyDeath?.Invoke(this);
        OnDeath?.Invoke(this);

        // Trigger on-kill perk effects
        PerkManager pm = PerkManager.Instance;
        if (pm != null)
        {
            if (pm.HealthRegenOnKill > 0f)
                pm.HealPlayer(pm.HealthRegenOnKill);

            pm.TriggerKillFireRateBuff(); // Bloodrush
        }

        agent.enabled = false;
        col.enabled = false;

        // Disable all child colliders (e.g. the Head SphereCollider) so they
        // cannot receive raycasts during the post-death Destroy delay.
        foreach (Collider childCol in GetComponentsInChildren<Collider>())
            childCol.enabled = false;

        // ?? RAGDOLL HOOK ??????????????????????????????????????????
        // When you have a rigged model:
        //   1. Enable all Rigidbodies in the rig
        //   2. Disable the Animator
        //   3. Apply hitDirection force to the nearest bone
        //   Replace PlaceholderDeathPhysics() with EnableRagdoll()
        // ?????????????????????????????????????????????????????????
        PlaceholderDeathPhysics(hitDirection);

        Destroy(gameObject, deathFlashTime + 3f);
    }

    void PlaceholderDeathPhysics(Vector3 hitDirection)
    {
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;

        Vector3 force = hitDirection == Vector3.zero
            ? (Vector3.back + Vector3.up) * deathForce
            : hitDirection.normalized * deathForce + Vector3.up * deathUpwardForce;

        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(UnityEngine.Random.insideUnitSphere * 2f, ForceMode.Impulse);
    }

    // ?? Future ragdoll method ??????????????????????????????????????
    // void EnableRagdoll(Vector3 hitDirection)
    // {
    //     GetComponent<Animator>().enabled = false;
    //     foreach (Rigidbody bone in GetComponentsInChildren<Rigidbody>())
    //     {
    //         bone.isKinematic = false;
    //         bone.AddForce(hitDirection.normalized * deathForce, ForceMode.Impulse);
    //     }
    // }

    public bool IsDead => isDead;

    /// <summary>
    /// Called by <see cref="Room.ActivateEnemies"/> when the player enters the room trigger.
    /// Until this is called the enemy stays permanently in Idle and will not detect or attack.
    /// </summary>
    public void Activate()
    {
        isActivated = true;
    }

}