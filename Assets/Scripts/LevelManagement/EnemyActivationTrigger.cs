using UnityEngine;

/// <summary>
/// Attach to the ActivateEnemies child GameObject of every room prefab.
/// Requires a trigger BoxCollider on the same GameObject.
/// When the player enters the trigger, enemies in the parent Room are unlocked
/// and begin detecting and attacking.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EnemyActivationTrigger : MonoBehaviour
{
    private const string PlayerTag = "Player";

    private Room room;
    private bool hasActivated = false;

    private void Awake()
    {
        room = GetComponentInParent<Room>();

        if (room == null)
            Debug.LogWarning($"[EnemyActivationTrigger] No Room component found in parent hierarchy of '{name}'.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasActivated) return;
        if (!other.CompareTag(PlayerTag)) return;
        if (room == null) return;

        hasActivated = true;
        room.ActivateEnemies();
    }
}
