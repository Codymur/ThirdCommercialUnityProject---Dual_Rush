using UnityEngine;
using System;
using System.Collections;
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
    [Header("References")]
    public Transform player;              // Assign in Inspector or auto-found in Start

    // ── Internal refs ─────────────────────────────────────────────
    protected NavMeshAgent agent;
    protected Rigidbody rb;
    protected CapsuleCollider col;
    protected bool isDead = false;

    public event Action<EnemyBase> OnDeath;

    // ── State ─────────────────────────────────────────────────────
    public enum EnemyState { Idle, Alert, Attack }
    protected EnemyState state = EnemyState.Idle;

    // Whether this enemy's room door has been opened.
    // Enemies stay in Idle and cannot attack until this is true.
    protected bool isActivated = false;

    // Whether we're currently hand-walking the agent across an off-mesh link
    // (doorway NavMeshLink). While true, normal steering is paused so the agent
    // walks the link at its own speed instead of snapping across (teleport look).
    protected bool isTraversingLink = false;

    // ── Events ── hook score system / perk triggers later ─────────
    public System.Action<EnemyBase> OnEnemyDeath;

    // ──────────────────────────────────────────────────────────────



    public GameObject objectToExcludeForHeadShots;

    protected override void Awake()
    {
        base.Awake(); // runs Target.Awake() — saves renderer materials

        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        rb.isKinematic = true; // NavMeshAgent drives movement while alive

        setRigidbodyState(true);
        setColliderState(false);
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

    // ──────────────────────────────────────────────────────────────
    protected virtual void Update()
    {
        if (isDead || player == null || !isActivated) return;

        // Once the door opens, leave Idle immediately and never go back.
        // Subclasses drive their own Alert -> Attack transitions.
        if (state == EnemyState.Idle)
            state = EnemyState.Alert;
    }

    // ──────────────────────────────────────────────────────────────
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
        //foreach (Collider childCol in GetComponentsInChildren<Collider>())
        //childCol.enabled = false;

        // ── RAGDOLL HOOK ─────────────────────────────────────────
        // When you have a rigged model:
        //   1. Enable all Rigidbodies in the rig
        //   2. Disable the Animator
        //   3. Apply hitDirection force to the nearest bone
        //   Replace PlaceholderDeathPhysics() with EnableRagdoll()
        // ────────────────────────────────────────────────────────
        //PlaceholderDeathPhysics(hitDirection);
        //EnableRagdoll(hitDirection);
        // Disable animator so it stops fighting the ragdoll simulation
        Animator anim = GetComponent<Animator>();
        if (anim != null)
            anim.enabled = false;

        setRigidbodyState(false);
        setColliderState(true);

        // Apply directional impulse to all ragdoll bones
        if (hitDirection != Vector3.zero)
        {
            foreach (Rigidbody bone in GetComponentsInChildren<Rigidbody>())
            {
                if (bone == rb) continue; // root stays kinematic, skip
                bone.AddForce(hitDirection.normalized * deathForce * 2f, ForceMode.Impulse);
            }
        }

        Destroy(gameObject, deathFlashTime + 60f);

    }

    //void PlaceholderDeathPhysics(Vector3 hitDirection)
    //{
    //    rb.isKinematic = false;
    //    rb.constraints = RigidbodyConstraints.None;

    //    Vector3 force = hitDirection == Vector3.zero
    //        ? (Vector3.back + Vector3.up) * deathForce
    //        : hitDirection.normalized * deathForce + Vector3.up * deathUpwardForce;

    //    rb.AddForce(force, ForceMode.Impulse);
    //    rb.AddTorque(UnityEngine.Random.insideUnitSphere * 2f, ForceMode.Impulse);
    //}

    // ── Future ragdoll method ─────────────────────────────────────
    void EnableRagdoll(Vector3 hitDirection)
    {
        GetComponent<Animator>().enabled = false;
        foreach (Rigidbody bone in GetComponentsInChildren<Rigidbody>())
        {
            bone.isKinematic = false;
            bone.AddForce(hitDirection.normalized * deathForce, ForceMode.Impulse);
        }
    }

    void setRigidbodyState(bool state)
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();
        Rigidbody rootRb = gameObject.GetComponent<Rigidbody>();

        foreach (Rigidbody rigidbody in rigidbodies)
        {
            // Root is intentionally excluded:
            // alive → stays kinematic (set in Awake) so NavMeshAgent drives movement cleanly
            // dead  → stays kinematic as a stable skeleton anchor while bones ragdoll freely
            if (rigidbody == rootRb)
                continue;

            rigidbody.isKinematic = state;
        }
    }


    void setColliderState(bool state)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            // Head colliders (marked by EnemyHeadShot) are excluded from the alive/dead toggle.
            // They remain enabled at all times: headshot raycast detection when alive,
            // ragdoll physics participation when dead.
            if (collider.GetComponent<EnemyHeadShot>() != null)
                continue;

            collider.enabled = state;
        }

        // Root collider is always the inverse of the children.
        GetComponent<Collider>().enabled = !state;
    }


    public bool IsDead => isDead;

    /// <summary>
    /// Called by <see cref="Room.ActivateEnemies"/> when the room's door is opened
    /// past the activation angle. Until this is called the enemy stays permanently
    /// in Idle and will not attack.
    /// </summary>
    public void Activate()
    {
        isActivated = true;
    }

    // ── Off-mesh link (doorway) traversal ─────────────────────────

    /// <summary>
    /// Manually walks the agent across an off-mesh link / NavMeshLink at its own
    /// movement speed, instead of the default near-instant snap that reads as a
    /// teleport across short doorway links. Call from subclass movement code; it
    /// no-ops unless the agent is actually sitting on a link.
    /// Requires "Auto Traverse Off Mesh Link" to be UNCHECKED on the agent.
    /// </summary>
    protected void HandleLinkTraversal()
    {
        if (isTraversingLink) return;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        if (!agent.isOnOffMeshLink) return;

        StartCoroutine(TraverseLinkRoutine());
    }

    private IEnumerator TraverseLinkRoutine()
    {
        isTraversingLink = true;

        OffMeshLinkData link = agent.currentOffMeshLinkData;

        // Floors are level at the doorway; any height difference between the two
        // baked meshes is a small voxel-snap artifact, not a real step. Sample the
        // far endpoint to get its true mesh height, then walk a flat line to it —
        // no Y easing, which would only chase the few-cm bake difference and dip.
        Vector3 endPos = link.endPos;
        if (NavMesh.SamplePosition(link.endPos, out NavMeshHit endHit, 1f, NavMesh.AllAreas))
            endPos = endHit.position;
        endPos += Vector3.up * agent.baseOffset;

        while (Vector3.Distance(agent.transform.position, endPos) > 0.05f)
        {
            if (isDead || agent == null || !agent.enabled)
            {
                isTraversingLink = false;
                yield break;
            }

            agent.transform.position = Vector3.MoveTowards(
                agent.transform.position, endPos, agent.speed * Time.deltaTime);

            Vector3 faceDir = endPos - agent.transform.position;
            faceDir.y = 0f;
            if (faceDir.sqrMagnitude > 0.0001f)
                agent.transform.rotation = Quaternion.Slerp(
                    agent.transform.rotation,
                    Quaternion.LookRotation(faceDir),
                    Time.deltaTime * 12f);

            yield return null;
        }

        if (agent != null && agent.enabled && agent.isOnOffMeshLink)
            agent.CompleteOffMeshLink();

        isTraversingLink = false;
    }
}