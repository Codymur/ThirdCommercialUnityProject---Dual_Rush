using UnityEngine;
using System.Collections;

public class Target : MonoBehaviour
{
    public float health = 50f;

    /// <summary>Health value at spawn time, used to calculate HealthFraction for conditional perks.</summary>
    protected float maxHealth;

    /// <summary>Returns current health as a 0–1 fraction of the starting health.</summary>
    public float HealthFraction => maxHealth > 0f ? health / maxHealth : 0f;

    [Header("Damage Visual Feedback")]
    public Material[][] defaultMaterial;
    public Material flashMaterial;
    public Material deathMaterial;
    public float flashDuration = 0.5f;

    private Renderer[] renderers;
    private bool isFlashing = false;

    [Header("Death Settings")]
    public float deathFlashTime = 0.1f;
    public float deathForce = 5f;
    public float deathUpwardForce = 2f;

    protected virtual void Awake()
    {
        maxHealth = health;
        renderers = GetComponentsInChildren<Renderer>();
        defaultMaterial = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
            defaultMaterial[i] = renderers[i].materials;
    }

    public virtual void TakeDamage(float amount)
    {
        TakeDamage(amount, Vector3.zero);
    }

    public virtual void TakeDamage(float amount, Vector3 hitDirection)
    {
        health -= amount;

        if (!isFlashing && flashMaterial != null)
            StartCoroutine(FlashMaterialCoroutine());

        if (health <= 0)
            Die(hitDirection);
    }

    private IEnumerator FlashMaterialCoroutine()
    {
        isFlashing = true;

        foreach (Renderer rend in renderers)
        {
            if (rend.TryGetComponent<EnemyHeadShot>(out _)) continue;   // ADDED
            Material[] mats = new Material[rend.materials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = flashMaterial;
            rend.materials = mats;
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            if (renderers[i].TryGetComponent<EnemyHeadShot>(out _)) continue;
            renderers[i].materials = defaultMaterial[i];

        }
            

        isFlashing = false;
    }

    // ?? Virtual so EnemyBase can override with ragdoll / AI cleanup ?
    protected virtual void Die(Vector3 hitDirection)
    {
        Destroy(gameObject);
    }

    // Keeps original call working on plain Target objects
    void Die() => Die(Vector3.zero);
}