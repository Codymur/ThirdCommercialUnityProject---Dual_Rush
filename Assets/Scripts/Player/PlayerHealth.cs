using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : Target
{
    [Header("Player Health")]
    public float maxHealth = 100f;

    [Header("Invincibility Frames")]
    [Tooltip("Seconds of invincibility after taking a hit. Prevents one-shot combos.")]
    public float invincibilityDuration = 0.4f;

    [Header("Damage Screen Flash")]
    [Tooltip("Full-screen red UI Image. Set its alpha to 0 in the Inspector.")]
    public Image damageOverlay;
    public float damageFlashAlpha = 0.35f;
    public float damageFlashFadeSpeed = 4f;

    [Header("Death")]
    [Tooltip("How long to wait before restarting after death.")]
    public float respawnDelay = 2f;

    [Header("UI")]
    [Tooltip("Optional health bar � fill image, 0 to 1.")]
    public Image healthBarFill;
    [Tooltip("Optional health text label.")]
    public Text healthText;

    private bool isInvincible = false;
    private float currentDamageFlashAlpha = 0f;
    private PlayerMovementTutorial movement;

    protected override void Awake()
    {
        base.Awake();
        health = maxHealth;
    }

    private void Start()
    {
        movement = GetComponent<PlayerMovementTutorial>();
        UpdateHealthUI();
    }

    private void Update()
    {
        if (currentDamageFlashAlpha > 0f)
        {
            currentDamageFlashAlpha -= Time.deltaTime * damageFlashFadeSpeed;
            currentDamageFlashAlpha = Mathf.Max(currentDamageFlashAlpha, 0f);
            SetOverlayAlpha(currentDamageFlashAlpha);
        }
    }

    public override void TakeDamage(float amount) => TakeDamage(amount, Vector3.zero);
    public override void TakeDamage(float amount, Vector3 hitDirection)
    {
        if (isInvincible) return;

        PerkManager pm = PerkManager.Instance;
        if (pm != null) amount *= pm.DamageTakenMult;

        health -= amount;
        health = Mathf.Max(health, 0f);

        TriggerDamageFlash();
        UpdateHealthUI();

        if (health <= 0f)
            Die(hitDirection);
        else
            StartCoroutine(InvincibilityCoroutine());
    }

    /// <summary>Restores health by amount, clamped to maxHealth. Has no effect when dead.</summary>
    public void Heal(float amount)
    {
        if (IsDead) return;
        health = Mathf.Min(health + amount, maxHealth);
        UpdateHealthUI();
    }

    protected override void Die(Vector3 hitDirection)
    {
        if (movement != null) movement.enabled = false;
        StartCoroutine(RestartCoroutine());
    }

    private IEnumerator RestartCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    private void TriggerDamageFlash()
    {
        currentDamageFlashAlpha = damageFlashAlpha;
        SetOverlayAlpha(currentDamageFlashAlpha);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (damageOverlay == null) return;
        Color c = damageOverlay.color;
        c.a = alpha;
        damageOverlay.color = c;
    }

    private void UpdateHealthUI()
    {
        float fraction = health / maxHealth;
        if (healthBarFill != null) healthBarFill.fillAmount = fraction;
        if (healthText != null) healthText.text = Mathf.CeilToInt(health).ToString();
    }

    public float GetHealthFraction() => health / maxHealth;
    public bool IsDead => health <= 0f;
}