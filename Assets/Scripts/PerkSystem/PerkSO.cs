using UnityEngine;

[CreateAssetMenu(fileName = "NewPerk", menuName = "ThirdCommercial/Perk")]
public class PerkSO : ScriptableObject
{
    public string perkName;
    [TextArea] public string description;
    public Sprite icon;

    public enum PerkType { Movement, Combat, Survival }
    public PerkType category;

    // ── Existing stat modifiers ───────────────────────────────────────────────
    public float moveSpeedAdd;
    public float jumpForceAdd;
    public float diveCooldownMult = 1f;     // 0.8 = 20% faster cooldown
    public float damageMult = 1f;           // 1.25 = 25% more damage
    public float healthRegenOnKill;         // flat HP healed on each kill
    public float fireRateMult = 1f;         // 1.5 = 50% faster fire rate
    public float damageTakenMult = 1f;      // 0.6 = 40% less damage taken

    // ── New stat modifiers ────────────────────────────────────────────────────

    /// <summary>Parkour — multiplies the player's air movement control (airMultiplier).</summary>
    public float airControlMult = 1f;

    /// <summary>Adrenaline — flat move speed added temporarily after taking damage.</summary>
    public float postHitSpeedBoost = 0f;

    /// <summary>Adrenaline — seconds the post-hit speed boost lasts.</summary>
    public float postHitSpeedDuration = 0f;

    /// <summary>Executioner — damage multiplier applied when the target is below the finisher HP threshold.</summary>
    public float finisherDamageMult = 1f;

    /// <summary>Bloodrush — fire rate multiplier activated for a short window after each kill.</summary>
    public float killFireRateBuffMult = 1f;

    /// <summary>Bloodrush — seconds the kill fire rate buff lasts.</summary>
    public float killFireRateBuffDuration = 0f;

    /// <summary>Last Stand — damage multiplier applied when the player is at or below the low-health threshold.</summary>
    public float lowHealthDamageMult = 1f;

    /// <summary>Toughness — flat amount added to the player's maximum health.</summary>
    public float maxHealthAdd = 0f;

    /// <summary>Iron Will — seconds added to the post-hit invincibility window.</summary>
    public float invincibilityDurationAdd = 0f;
}