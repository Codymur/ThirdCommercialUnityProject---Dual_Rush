using System.Collections;
using UnityEngine;
using Unity.AI.Navigation;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    [Header("Room Prefabs")]
    public GameObject[] normalRoomPrefabs;
    public GameObject[] miniBossRoomPrefabs;
    public GameObject[] perkRoomPrefabs;
    public GameObject bossRoomPrefab;

    [Header("First Room")]
    public Transform firstRoomExitAnchor;

    [Header("Door Settings")]
    [Tooltip("How long the exit door lingers in the world after its room is destroyed (seconds).")]
    public float doorLingerDuration = 3f;

    // One full cycle = 4 normal + 1 mini-boss + 1 perk.
    private const int RoomSlots = 6;

    private readonly Room[] rooms = new Room[RoomSlots];
    private readonly GameObject[] roomInstances = new GameObject[RoomSlots];

    // How many rooms at the front of the array have already been cleared and the
    // player has moved past. Their instances are stashed here until the player
    // physically enters the next room (handled by RoomEnteredTrigger).
    private GameObject pendingDestroyInstance;
    private int pendingDestroySlot = -1;

    // Index into rooms[] pointing at the room the player is currently in.
    private int activeSlot = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Instantiates the first 6 rooms and bakes their NavMeshes.</summary>
    public void LoadFirstBatch()
    {
        StartCoroutine(LoadBatchRoutine(startSlot: 0, tailRoom: null));
    }

    /// <summary>
    /// Called by PerkPickup after the player picks a perk.
    /// Destroys all rooms in the current batch (except ones still in use) and
    /// instantiates the next full batch of 6.
    /// </summary>
    public void LoadNextBatch()
    {
        StartCoroutine(LoadNextBatchRoutine());
    }

    /// <summary>
    /// Called by <see cref="FirstDoorCloser"/> when the player crosses out of the
    /// FirstLevel tutorial room into slot 0 of the first procedural batch.
    /// Slot 0 spawns its enemies on batch load but keeps them in forced Idle
    /// until this call so they don't activate while the player is still looting.
    /// </summary>
    public void ActivateFirstProceduralRoom()
    {
        if (rooms[0] != null)
            rooms[0].ActivateEnemies();
    }

    /// <summary>
    /// Called by RunManager when the player exits a non-perk room.
    /// Stashes the just-exited room for deferred destruction and wakes the
    /// enemies in the room the player is now entering.
    /// </summary>
    public void ShiftRoom()
    {
        // activeSlot was already incremented by HandlePlayerExited before
        // AdvanceRoom→ShiftRoom is called, so activeSlot-1 is the exited room
        // and activeSlot-2 is the room before it — the one we want to destroy.
        int exitedSlot = activeSlot - 2;
        if (exitedSlot >= 0 && exitedSlot < RoomSlots)
        {
            pendingDestroyInstance = roomInstances[exitedSlot];
            pendingDestroySlot = exitedSlot;
        }
    }

    /// <summary>
    /// Destroys the previous room instance. Called by RoomEnteredTrigger once
    /// the player has physically stepped into the next room.
    /// Deferred by one frame and guarded by slot index to guarantee the player
    /// has fully cleared the boundary before the old room is torn down.
    /// </summary>
    public void DestroyPreviousRoom()
    {
        if (pendingDestroyInstance == null) return;

        // Safety: never destroy the room the player is currently inside.
        if (pendingDestroySlot >= activeSlot)
        {
            Debug.LogWarning($"[RoomManager] DestroyPreviousRoom blocked — pending slot {pendingDestroySlot} is not behind active slot {activeSlot}.");
            return;
        }

        StartCoroutine(DestroyPreviousRoomRoutine(pendingDestroyInstance));
        pendingDestroyInstance = null;
        pendingDestroySlot = -1;
    }

    IEnumerator DestroyPreviousRoomRoutine(GameObject instance)
    {
        yield return null; // Wait one frame so the player is fully inside the new room.
        if (instance == null) yield break;

        Debug.Log($"[RoomManager] Destroying previous room '{instance.name}'.");
        DetachAndLingerDoor(instance);
        Destroy(instance);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Startup batch
    // ──────────────────────────────────────────────────────────────────────────

    IEnumerator LoadBatchRoutine(int startSlot, Room tailRoom)
    {
        for (int i = startSlot; i < RoomSlots; i++)
        {
            int absoluteIndex = RunManager.Instance.CurrentRoomIndex + i;
            RoomType type = RunManager.Instance.GetRoomType(absoluteIndex);
            GameObject prefab = PickPrefab(type);
            if (prefab == null)
            {
                Debug.LogError($"[RoomManager] No prefab for slot {i} (type: {type}).");
                yield break;
            }

            roomInstances[i] = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            rooms[i] = roomInstances[i].GetComponent<Room>();
        }

        // Align slot 0 to firstRoomExitAnchor (or to the provided tail room).
        if (tailRoom == null)
        {
            if (firstRoomExitAnchor != null && rooms[0] != null && rooms[0].entranceAnchor != null)
            {
                Quaternion rotOffset = firstRoomExitAnchor.rotation
                    * Quaternion.Inverse(rooms[0].entranceAnchor.rotation);
                rooms[0].transform.rotation = rotOffset * rooms[0].transform.rotation;
                rooms[0].transform.position += firstRoomExitAnchor.position
                    - rooms[0].entranceAnchor.position;
            }
            // Chain-align the rest.
            for (int i = 1; i < RoomSlots; i++)
                AlignRooms(rooms[i - 1], rooms[i]);
        }
        else
        {
            // New batch: align first new room to the tail of the previous batch.
            AlignRooms(tailRoom, rooms[startSlot]);
            for (int i = startSlot + 1; i < RoomSlots; i++)
                AlignRooms(rooms[i - 1], rooms[i]);
        }

        // Bake all NavMeshes.
        for (int i = startSlot; i < RoomSlots; i++)
        {
            NavMeshSurface surface = roomInstances[i]?.GetComponentInChildren<NavMeshSurface>();
            if (surface != null)
            {
                surface.BuildNavMesh();
                yield return null;
            }
        }

        // Initialise all rooms — all spawn active immediately.
        for (int i = 0; i < RoomSlots; i++)
            InitialiseRoom(i);

        // Wire each exit door to activate the NEXT room in the chain.
        // Slot 0 of the first batch is intentionally left in forced Idle until
        // FirstDoorCloser → ActivateFirstProceduralRoom() fires.
        WireDoorTriggers(tailRoom);

        activeSlot = 0;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Next-batch load (triggered by perk pickup)
    // ──────────────────────────────────────────────────────────────────────────

    IEnumerator LoadNextBatchRoutine()
    {
        // Stash the perk room — it stays in the scene until the player physically
        // crosses into the first room of the new batch.
        int perkSlot = RoomSlots - 1;
        Room perkRoom = rooms[perkSlot];
        GameObject perkRoomInstance = roomInstances[perkSlot];

        // Remove it from the tracked arrays but do NOT destroy it yet.
        rooms[perkSlot] = null;
        roomInstances[perkSlot] = null;

        // Destroy rooms 0–4 (already passed through) and any stale pending instance.
        for (int i = 0; i < perkSlot; i++)
        {
            if (roomInstances[i] != null)
            {
                DetachAndLingerDoor(roomInstances[i]);
                Destroy(roomInstances[i]);
                roomInstances[i] = null;
                rooms[i] = null;
            }
        }
        if (pendingDestroyInstance != null)
        {
            DetachAndLingerDoor(pendingDestroyInstance);
            Destroy(pendingDestroyInstance);
            pendingDestroyInstance = null;
            pendingDestroySlot = -1;
        }

        yield return null; // Let Unity process the Destroys.

        // Advance the room index past the perk room so GetRoomType resolves
        // correctly for the new cycle (slot 0 → absoluteIndex 6 → Normal, etc.).
        RunManager.Instance.AdvancePastPerkRoom();

        // Build and align the new batch, chaining off the perk room's exitAnchor.
        // LoadBatchRoutine's WireDoorTriggers call uses the perk room as tailRoom,
        // wiring its exit door to the new batch's slot 0 so the perk room's
        // door activation handover keeps working.
        yield return StartCoroutine(LoadBatchRoutine(startSlot: 0, tailRoom: perkRoom));

        // Hand the perk room to the deferred-destroy pipeline.
        // It will be torn down by RoomEnteredTrigger of the new batch's first room
        // once the player physically walks through. pendingDestroySlot = -1 always
        // passes the slot guard (activeSlot resets to 0 after LoadBatchRoutine).
        pendingDestroyInstance = perkRoomInstance;
        pendingDestroySlot = -1;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Room cleared
    // ──────────────────────────────────────────────────────────────────────────

    void HandleRoomCleared(int slotIndex)
    {
        // For normal/mini-boss rooms the exit door is opened directly by Room itself.
        // Reveal perk pickup in the next perk room if applicable.
        int nextSlot = slotIndex + 1;
        if (nextSlot < RoomSlots && rooms[nextSlot] != null)
            rooms[nextSlot].WakeEnemies();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Player exited a room
    // ──────────────────────────────────────────────────────────────────────────

    void HandlePlayerExited(int slotIndex)
    {
        // Guard against stale subscriptions firing from a room the player
        // already passed through.
        if (slotIndex != activeSlot)
        {
            Debug.LogWarning($"[RoomManager] Stale OnPlayerExited from slot {slotIndex} (active slot is {activeSlot}). Ignored.");
            return;
        }

        activeSlot++;
        RunManager.Instance.AdvanceRoom();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    void InitialiseRoom(int slotIndex)
    {
        Room room = rooms[slotIndex];
        if (room == null) return;

        int absoluteIndex = RunManager.Instance.CurrentRoomIndex + slotIndex;
        room.Initialise(
            RunManager.Instance.GetRoomType(absoluteIndex),
            RunManager.Instance.GetDifficulty(absoluteIndex)
        );

        // Capture slotIndex so each room's exit fires with the correct slot,
        // preventing stale subscriptions from interfering.
        int capturedSlot = slotIndex;
        room.OnPlayerExited += () => HandlePlayerExited(capturedSlot);

        // Cascade: reveal perk pickup in the next room when this room is cleared.
        if (slotIndex < RoomSlots - 1)
            room.OnRoomCleared += () => HandleRoomCleared(slotIndex);
    }

    /// <summary>
    /// Wires each exit door's <see cref="DoorActivationTrigger"/> to the room it
    /// leads into. Within a batch, slot i's exit door → slot i+1. If a tailRoom
    /// is provided (the perk room from the previous batch), its exit door is
    /// wired to slot 0 of this batch.
    /// The new batch's perk room (slot RoomSlots-1) is left unwired; the next
    /// LoadNextBatchRoutine wires it when it runs as tailRoom.
    /// </summary>
    void WireDoorTriggers(Room tailRoom)
    {
        // Tail handover: previous batch's perk room door → this batch's slot 0.
        if (tailRoom != null && tailRoom.exitDoor != null && rooms[0] != null)
        {
            DoorActivationTrigger tailTrigger = tailRoom.exitDoor.GetComponent<DoorActivationTrigger>();
            if (tailTrigger != null)
                tailTrigger.SetTargetRoom(rooms[0]);
            else
                Debug.LogWarning($"[RoomManager] Tail room '{tailRoom.name}' exit door is missing a DoorActivationTrigger.", tailRoom);

            // Bridge the perk room's mesh to the new batch's slot 0 so enemies
            // can cross the handover doorway too.
            RoomNavLinker.Link(tailRoom, rooms[0]);
        }

        // Within-batch wiring: slot i's door → slot i+1's room.
        for (int i = 0; i < RoomSlots - 1; i++)
        {
            if (rooms[i] == null || rooms[i].exitDoor == null) continue;
            if (rooms[i + 1] == null) continue;

            DoorActivationTrigger trigger = rooms[i].exitDoor.GetComponent<DoorActivationTrigger>();
            if (trigger != null)
                trigger.SetTargetRoom(rooms[i + 1]);
            else
                Debug.LogWarning($"[RoomManager] Room '{rooms[i].name}' exit door is missing a DoorActivationTrigger.", rooms[i]);

            // Bridge this room's mesh to the next room's mesh across the doorway.
            RoomNavLinker.Link(rooms[i], rooms[i + 1]);
        }
    }

    void AlignRooms(Room current, Room next)
    {
        if (current == null || next == null) return;
        if (current.exitAnchor == null || next.entranceAnchor == null)
        {
            Debug.LogError($"[RoomManager] AlignRooms: missing anchor on '{current.name}' or '{next.name}'.");
            return;
        }

        Quaternion rotOffset = current.exitAnchor.rotation
            * Quaternion.Inverse(next.entranceAnchor.rotation);
        next.transform.rotation = rotOffset * next.transform.rotation;
        next.transform.position += current.exitAnchor.position - next.entranceAnchor.position;
    }

    GameObject PickPrefab(RoomType type)
    {
        return type switch
        {
            RoomType.Boss => bossRoomPrefab,
            RoomType.MiniBoss => miniBossRoomPrefabs.Length > 0
                ? miniBossRoomPrefabs[Random.Range(0, miniBossRoomPrefabs.Length)]
                : null,
            RoomType.Perk => perkRoomPrefabs.Length > 0
                ? perkRoomPrefabs[Random.Range(0, perkRoomPrefabs.Length)]
                : null,
            _ => normalRoomPrefabs.Length > 0
                ? normalRoomPrefabs[Random.Range(0, normalRoomPrefabs.Length)]
                : null,
        };
    }

    /// <summary>
    /// Detaches the room's exit door frame (the parent of the Door component) so it
    /// survives the room's destruction, then schedules its own cleanup after
    /// <see cref="doorLingerDuration"/> seconds.
    /// </summary>
    void DetachAndLingerDoor(GameObject roomInstance)
    {
        Room room = roomInstance.GetComponent<Room>();
        if (room == null || room.exitDoor == null) return;

        // ExitDoor (frame) is the direct parent of DoorObject (Door component).
        // Detach the frame so both the door and its surround survive room destruction.
        Transform doorRoot = room.exitDoor.transform.parent != null
            ? room.exitDoor.transform.parent
            : room.exitDoor.transform;

        doorRoot.SetParent(null, worldPositionStays: true);
        room.exitDoor.LerpZToZero();
        Destroy(doorRoot.gameObject, doorLingerDuration);
    }
}