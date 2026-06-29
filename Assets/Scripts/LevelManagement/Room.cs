using System;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("Anchors")]
    public Transform entranceAnchor;
    public Transform exitAnchor;

    [Header("Doors")]
    public Door exitDoor;

    [Header("Spawn Points")]
    public Transform[] enemySpawnPoints;

    [Header("Enemy Prefabs")]
    public GameObject shooterEnemyPrefab;
    public GameObject rusherEnemyPrefab;
    public GameObject miniBossPrefab;
    public GameObject bossPrefab;

    [Header("Perk Trigger")]
    public GameObject perkTriggerObject;

    public event Action OnRoomCleared;
    public event Action OnPlayerExited;

    private List<EnemyBase> aliveEnemies = new List<EnemyBase>();
    private RoomType roomType;
    private bool isPerkRoom = false;

    public void Initialise(RoomType type, int difficulty)
    {
        roomType = type;
        isPerkRoom = (type == RoomType.Perk);

        // Always keep the perk trigger hidden at init time.
        // For perk rooms it is revealed in WakeEnemies() once the room is reached.
        if (perkTriggerObject != null)
            perkTriggerObject.SetActive(false);

        if (exitDoor != null)
            exitDoor.SetLocked(true);

        SpawnEnemies(difficulty);
    }

    void SpawnEnemies(int difficulty)
    {
        // Perk rooms have no enemies.
        if (isPerkRoom) return;

        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
        {
            Debug.LogWarning($"[Room] No enemy spawn points on '{name}'.");
            CheckRoomCleared();
            return;
        }

        switch (roomType)
        {
            case RoomType.Normal: SpawnNormalEnemies(difficulty); break;
            case RoomType.MiniBoss: SpawnMiniBoss(); break;
            case RoomType.Boss: SpawnBoss(); break;
        }
    }

    void SpawnNormalEnemies(int difficulty)
    {
        int count = Mathf.Min(2 + difficulty, enemySpawnPoints.Length);
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = (i % 2 == 1 && rusherEnemyPrefab != null)
                ? rusherEnemyPrefab
                : shooterEnemyPrefab;
            SpawnAt(prefab, enemySpawnPoints[i]);
        }
    }

    void SpawnMiniBoss()
    {
        if (enemySpawnPoints.Length > 0)
            SpawnAt(miniBossPrefab != null ? miniBossPrefab : shooterEnemyPrefab, enemySpawnPoints[0]);
        for (int i = 1; i < Mathf.Min(3, enemySpawnPoints.Length); i++)
            SpawnAt(shooterEnemyPrefab, enemySpawnPoints[i]);
    }

    void SpawnBoss()
    {
        if (enemySpawnPoints.Length > 0)
            SpawnAt(bossPrefab != null ? bossPrefab : rusherEnemyPrefab, enemySpawnPoints[0]);
    }

    void SpawnAt(GameObject prefab, Transform point)
    {
        if (prefab == null) return;
        GameObject go = Instantiate(prefab, point.position, point.rotation, transform);
        EnemyBase enemy = go.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            aliveEnemies.Add(enemy);
            enemy.OnDeath += HandleEnemyDeath;
        }
    }

    void HandleEnemyDeath(EnemyBase enemy)
    {
        enemy.OnDeath -= HandleEnemyDeath;
        aliveEnemies.Remove(enemy);
        CheckRoomCleared();
    }

    void CheckRoomCleared()
    {
        if (aliveEnemies.Count > 0) return;
        Debug.Log($"[Room] '{name}' cleared.");

        OnRoomCleared?.Invoke();

        // Perk rooms unlock the door only after the player picks a perk (OnPerkTaken).
        // Normal and mini-boss rooms open the exit immediately.
        if (!isPerkRoom && exitDoor != null)
            exitDoor.Open();
    }

    public void PlayerExited()
    {
        OnPlayerExited?.Invoke();
    }

    /// <summary>
    /// Called by the previous room's <see cref="DoorActivationTrigger"/> when the
    /// player pushes that room's exit door open. All alive enemies in this room
    /// transition from forced Idle to active detection and can now attack.
    /// For the very first room of a run, RoomManager calls this directly on
    /// batch load since there is no preceding door.
    /// </summary>
    public void ActivateEnemies()
    {
        foreach (EnemyBase enemy in aliveEnemies)
        {
            if (enemy != null)
                enemy.Activate();
        }

        Debug.Log($"[Room] '{name}' enemies activated.");
    }

    public void OnPerkTaken()
    {
        if (exitDoor != null)
            exitDoor.Open();
    }

    /// <summary>For perk rooms, reveals the perk pickup and fires OnRoomCleared
    /// to keep the cascade chain intact.</summary>
    public void WakeEnemies()
    {
        if (!isPerkRoom) return;

        if (perkTriggerObject != null)
            perkTriggerObject.SetActive(true);
        // Perk rooms have no enemies — fire OnRoomCleared immediately
        // so the cascade chain stays intact.
        OnRoomCleared?.Invoke();
    }
}