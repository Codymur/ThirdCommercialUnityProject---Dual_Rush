using UnityEngine;

[CreateAssetMenu(fileName = "NewPerk", menuName = "ThirdCommercial/Perk")]
public class PerkSO : ScriptableObject
{
    public string perkName;
    [TextArea] public string description;
    public Sprite icon;

    public enum PerkType { Movement, Combat, Survival }
    public PerkType category;

    // Stat Modifiers
    public float moveSpeedAdd;
    public float jumpForceAdd;
    public float diveCooldownMult = 1f; // 0.8 = 20% faster
    public float damageMult = 1f;
    public float healthRegenOnKill;
    public float fireRateMult = 1f;       // 1.5 = 50% faster fire rate
    public float damageTakenMult = 1f;    // 0.6 = 40% less damage taken
}