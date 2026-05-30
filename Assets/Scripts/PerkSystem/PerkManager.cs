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

    private PlayerMovementTutorial playerMovement;
    private PlayerDive playerDive;
    private PlayerHealth playerHealth;

    // ── Derived stats (recalculated on each perk pickup) ─────────────────
    public float MoveSpeedBonus      { get; private set; }
    public float JumpForceBonus      { get; private set; }
    public float DamageMult          { get; private set; } = 1f;
    public float DiveCooldownMult    { get; private set; } = 1f;
    public float HealthRegenOnKill   { get; private set; }
    public float FireRateMult        { get; private set; } = 1f;
    public float DamageTakenMult     { get; private set; } = 1f;

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
            baseMoveSpeed  = playerMovement.moveSpeed;
            baseJumpForce  = playerMovement.jumpForce;
        }

        if (playerDive != null)
            baseDiveCooldown = playerDive.diveCooldown;
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

    // ── Internal ─────────────────────────────────────────────────────────

    private void RecalculateStats()
    {
        MoveSpeedBonus    = 0f;
        JumpForceBonus    = 0f;
        DamageMult        = 1f;
        DiveCooldownMult  = 1f;
        HealthRegenOnKill = 0f;
        FireRateMult      = 1f;
        DamageTakenMult   = 1f;

        foreach (PerkSO p in activePerks)
        {
            MoveSpeedBonus    += p.moveSpeedAdd;
            JumpForceBonus    += p.jumpForceAdd;
            DamageMult        *= p.damageMult;
            DiveCooldownMult  *= p.diveCooldownMult;
            HealthRegenOnKill += p.healthRegenOnKill;
            FireRateMult      *= p.fireRateMult;
            DamageTakenMult   *= p.damageTakenMult;
        }
    }

    private void ApplyToPlayer()
    {
        if (playerMovement != null)
        {
            playerMovement.moveSpeed  = baseMoveSpeed  + MoveSpeedBonus;
            playerMovement.jumpForce  = baseJumpForce  + JumpForceBonus;
        }

        if (playerDive != null)
            playerDive.diveCooldown = Mathf.Max(0.1f, baseDiveCooldown * DiveCooldownMult);
    }

    /// <summary>Restores player health by the given amount, clamped to max health.</summary>
    public void HealPlayer(float amount)
    {
        if (playerHealth != null)
            playerHealth.Heal(amount);
    }
}
