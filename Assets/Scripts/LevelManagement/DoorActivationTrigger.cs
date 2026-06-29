using UnityEngine;

/// <summary>
/// Attach to the exit door GameObject (the one with the HingeJoint + Rigidbody).
/// When the door rotates past <see cref="openAngleThreshold"/> degrees from its
/// rest position, the linked <see cref="targetRoom"/>'s enemies are activated.
///
/// Rooms only have exit doors, so this door belongs to the room the player is
/// leaving, and targetRoom is the room the player is about to enter.
/// RoomManager wires targetRoom after instantiation.
/// </summary>
[RequireComponent(typeof(HingeJoint))]
public class DoorActivationTrigger : MonoBehaviour
{
    [Tooltip("Door angle (absolute degrees from rest) at which the target room's enemies activate.")]
    public float openAngleThreshold = 15f;

    [Tooltip("Room whose enemies activate when this door opens. Set by RoomManager.")]
    public Room targetRoom;

    private HingeJoint hinge;
    private bool hasActivated = false;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint>();
    }

    private void Update()
    {
        if (hasActivated || targetRoom == null || hinge == null) return;

        // HingeJoint.angle is current angle relative to the joint's rest position.
        // Mathf.Abs handles doors that swing in either direction.
        if (Mathf.Abs(hinge.angle) >= openAngleThreshold)
        {
            hasActivated = true;
            targetRoom.ActivateEnemies();
        }
    }

    /// <summary>
    /// Assigns the room whose enemies activate when this door opens.
    /// Resets the one-shot guard so the trigger fires once per assignment.
    /// </summary>
    public void SetTargetRoom(Room room)
    {
        targetRoom = room;
        hasActivated = false;
    }
}