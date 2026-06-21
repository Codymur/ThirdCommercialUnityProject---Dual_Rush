using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that owns the player's active perks for the current run and
/// exposes the final stat values after all modifiers are stacked.
/// </summary>
public class PerkManager : MonoBehaviour
{
    public static PerkManager Instance { get; private set; }

    private readonly List<PerkSO> activePerks = new List<PerkSO>();

    // ── Cached base values ────────────────────────────────────────────────
    private float baseMoveSpeed;
    private float baseJumpForce;
    private float baseDiveCooldown;
    private float baseAirMultiplier;
    private float baseMaxHealth;
    private float baseInvincibilityDuration;

    private PlayerMovementTutorial playerMovement;
    private PlayerDive playerDive;
    private PlayerHealth playerHealth;

    // ── Derived stats (recalculated on each perk pickup) ─────────────────
    public float MoveSpeedBonus         { get; private set; }
    public float JumpForceBonus         { get; private set; }
    public float DamageMult             { get; private set; } = 1f;
    public float DiveCooldownMult       { get; private set; } = 1f;
    public float HealthRegenOnKill      { get; private set; }
    public float FireRateMult           { get; private set; } = 1f;
    public float DamageTakenMult        { get; private set; } = 1f;
    public float AirControlMult         { get; private set; } = 1f;
    public float PostHitSpeedBoost      { get; private set; }
    public float PostHitSpeedDuration   { get; private set; }
    public float FinisherDamageMult     { get; private set; } = 1f;
    public float KillFireRateBuffMult   { get; private set; } = 1f;
    public float KillFireRateBuffDuration { get; private set; }
    public float LowHealthDamageMult    { get; private set; } = 1f;
    public float MaxHealthAdd           { get; private set; }
    public float InvincibilityDurationAdd { get; private set; }

    // ── Runtime buff state ────────────────────────────────────────────────
    /// <summary>Active Bloodrush fire rate multiplier — 1f when no buff is running.</summary>
    public float ActiveKillFireRateMult { get; private set; } = 1f;

    private Coroutine _killFireRateCoroutine;
    private Coroutine _postHitSpeedCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Cache player references — player is expected to exist in the scene.
        playerMovement = FindAnyObjectByType<PlayerMovementTutorial>();
        playerDive     = FindAnyObjectByType<PlayerDive>();
        playerHealth   = FindAnyObjectByType<PlayerHealth>();

        if (playerMovement != null)
        {
            baseMoveSpeed      = playerMovement.moveSpeed;
            baseJumpForce      = playerMovement.jumpForce;
            baseAirMultiplier  = playerMovement.airMultiplier;
        }

        if (playerDive != null)
            baseDiveCooldown = playerDive.diveCooldown;

        if (playerHealth != null)
        {
            baseMaxHealth              = playerHealth.maxHealth;
            baseInvincibilityDuration  = playerHealth.invincibilityDuration;
        }
    }

    /// <summary>Adds a perk and immediately applies its stat changes to the player.</summary>
    public void AddPerk(PerkSO perk)
    {
        if (perk == null) return;
        activePerks.Add(perk);
        RecalculateStats();
        ApplyToPlayer();
        Debug.Log($"[PerkManager] Picked up: {perk.perkName}");
    }

    /// <summary>Returns a read-only view of all currently active perks.</summary>
    public IReadOnlyList<PerkSO> ActivePerks => activePerks;

    /// <summary>
    /// Called by EnemyBase on death. Activates the Bloodrush fire rate buff if the perk is active.
    /// Calling with no Bloodrush perks equipped is a safe no-op.
    /// </summary>
    public void TriggerKillFireRateBuff()
    {
        if (KillFireRateBuffDuration <= 0f || KillFireRateBuffMult <= 1f) return;

        if (_killFireRateCoroutine != null)
            StopCoroutine(_killFireRateCoroutine);

        _killFireRateCoroutine = StartCoroutine(KillFireRateBuffCoroutine());
    }

    /// <summary>
    /// Called by PlayerHealth when the player survives a hit.
    /// Activates the Adrenaline speed boost if the perk is active.
    /// Calling with no Adrenaline perks equipped is a safe no-op.
    /// </summary>
    public void TriggerPostHitSpeedBoost()
    {
        if (PostHitSpeedBoost <= 0f || PostHitSpeedDuration <= 0f) return;

        if (_postHitSpeedCoroutine != null)
            StopCoroutine(_postHitSpeedCoroutine);

        _postHitSpeedCoroutine = StartCoroutine(PostHitSpeedCoroutine());
    }

    /// <summary>Restores player health by the given amount, clamped to max health.</summary>
    public void HealPlayer(float amount)
    {
        if (playerHealth != null)
            playerHealth.Heal(amount);
    }

    // ── Internal ─────────────────────────────────────────────────────────

    private void RecalculateStats()
    {
        MoveSpeedBonus          = 0f;
        JumpForceBonus          = 0f;
        DamageMult              = 1f;
        DiveCooldownMult        = 1f;
        HealthRegenOnKill       = 0f;
        FireRateMult            = 1f;
        DamageTakenMult         = 1f;
        AirControlMult          = 1f;
        PostHitSpeedBoost       = 0f;
        PostHitSpeedDuration    = 0f;
        FinisherDamageMult      = 1f;
        KillFireRateBuffMult    = 1f;
        KillFireRateBuffDuration = 0f;
        LowHealthDamageMult     = 1f;
        MaxHealthAdd            = 0f;
        InvincibilityDurationAdd = 0f;

        foreach (PerkSO p in activePerks)
        {
            MoveSpeedBonus          += p.moveSpeedAdd;
            JumpForceBonus          += p.jumpForceAdd;
            DamageMult              *= p.damageMult;
            DiveCooldownMult        *= p.diveCooldownMult;
            HealthRegenOnKill       += p.healthRegenOnKill;
            FireRateMult            *= p.fireRateMult;
            DamageTakenMult         *= p.damageTakenMult;
            AirControlMult          *= p.airControlMult;
            PostHitSpeedBoost       += p.postHitSpeedBoost;
            PostHitSpeedDuration    += p.postHitSpeedDuration;
            FinisherDamageMult      *= p.finisherDamageMult;
            KillFireRateBuffMult    *= p.killFireRateBuffMult;
            KillFireRateBuffDuration += p.killFireRateBuffDuration;
            LowHealthDamageMult     *= p.lowHealthDamageMult;
            MaxHealthAdd            += p.maxHealthAdd;
            InvincibilityDurationAdd += p.invincibilityDurationAdd;
        }
    }

    private void ApplyToPlayer()
    {
        if (playerMovement != null)
        {
            playerMovement.moveSpeed   = baseMoveSpeed + MoveSpeedBonus;
            playerMovement.jumpForce   = baseJumpForce + JumpForceBonus;
            playerMovement.airMultiplier = baseAirMultiplier * AirControlMult;
        }

        if (playerDive != null)
            playerDive.diveCooldown = Mathf.Max(0.1f, baseDiveCooldown * DiveCooldownMult);

        if (playerHealth != null)
        {
            float prevMax = playerHealth.maxHealth;
            playerHealth.maxHealth = baseMaxHealth + MaxHealthAdd;

            // Heal by the difference so picking Toughness immediately grants the extra HP.
            float healthGained = playerHealth.maxHealth - prevMax;
            if (healthGained > 0f)
                playerHealth.Heal(healthGained);

            playerHealth.invincibilityDuration = baseInvincibilityDuration + InvincibilityDurationAdd;
        }
    }

    private IEnumerator KillFireRateBuffCoroutine()
    {
        ActiveKillFireRateMult = KillFireRateBuffMult;
        yield return new WaitForSeconds(KillFireRateBuffDuration);
        ActiveKillFireRateMult = 1f;
        _killFireRateCoroutine = null;
    }

    private IEnumerator PostHitSpeedCoroutine()
    {
        if (playerMovement != null)
            playerMovement.moveSpeed = baseMoveSpeed + MoveSpeedBonus + PostHitSpeedBoost;

        yield return new WaitForSeconds(PostHitSpeedDuration);

        if (playerMovement != null)
            playerMovement.moveSpeed = baseMoveSpeed + MoveSpeedBonus;

        _postHitSpeedCoroutine = null;
    }
}
