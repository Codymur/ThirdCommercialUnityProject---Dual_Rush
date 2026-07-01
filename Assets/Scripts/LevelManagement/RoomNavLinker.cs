using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Builds a runtime NavMeshLink bridging two adjacent rooms' isolated NavMesh
/// islands across their shared doorway. Each room bakes "Current Object
/// Hierarchy" only, so without a link the meshes don't connect and enemies
/// can't cross the threshold.
///
/// Endpoints are snapped onto each room's actual baked mesh height so the link
/// binds surface-to-surface even when the two rooms' meshes sit at slightly
/// different heights at the doorway.
/// </summary>
public static class RoomNavLinker
{
    private const float EndpointInset = 0.2f;
    private const float LinkWidth = 0.2f;

    // Vertical search range when snapping an endpoint onto a room's mesh. Needs to
    // be tall enough to find the mesh whether it's above or below the anchor.
    private const float SampleHeight = 3f;

    public static void Link(Room fromRoom, Room toRoom)
    {
        if (fromRoom == null || toRoom == null) return;
        if (fromRoom.exitAnchor == null || toRoom.entranceAnchor == null) return;

        Transform doorway = fromRoom.exitAnchor;
        Vector3 doorPos = doorway.position;
        Vector3 through = doorway.forward;
        through.y = 0f;
        if (through.sqrMagnitude < 0.0001f) through = Vector3.forward;
        through.Normalize();

        // Raw endpoint guesses, inset into each room from the doorway.
        Vector3 startGuess = doorPos - through * EndpointInset;
        Vector3 endGuess = doorPos + through * EndpointInset;

        // Snap each endpoint onto whatever mesh actually exists at that spot —
        // this captures each room's real surface height independently, so the
        // link binds flush on both sides even if the two meshes differ in Y.
        if (!NavMesh.SamplePosition(startGuess, out NavMeshHit startHit, SampleHeight, NavMesh.AllAreas))
        {
            Debug.LogWarning($"[RoomNavLinker] No mesh found near start endpoint for '{fromRoom.name}'->'{toRoom.name}'. Link skipped.", fromRoom);
            return;
        }
        if (!NavMesh.SamplePosition(endGuess, out NavMeshHit endHit, SampleHeight, NavMesh.AllAreas))
        {
            Debug.LogWarning($"[RoomNavLinker] No mesh found near end endpoint for '{fromRoom.name}'->'{toRoom.name}'. Link skipped.", fromRoom);
            return;
        }

        Vector3 startWorld = new Vector3(startHit.position.x, startHit.position.y - 0.2f, startHit.position.z);
        Vector3 endWorld = new Vector3(endHit.position.x, endHit.position.y - 0.2f, endHit.position.z + 0.01f);

        // Host the link on a child of toRoom so it shares that room's lifetime.
        // Place the link ORIGIN at the snapped start height so its local Y math
        // starts from the real mesh, not the (possibly raised) anchor.
        GameObject linkGO = new GameObject($"NavLink_{fromRoom.name}->{toRoom.name}");
        linkGO.transform.SetParent(toRoom.transform, worldPositionStays: true);
        linkGO.transform.position = startWorld;
        linkGO.transform.rotation = Quaternion.LookRotation(through, Vector3.up);

        NavMeshLink link = linkGO.AddComponent<NavMeshLink>();
        link.startPoint = linkGO.transform.InverseTransformPoint(startWorld);
        link.endPoint = linkGO.transform.InverseTransformPoint(endWorld);
        link.width = LinkWidth;
        link.bidirectional = true;
        link.area = 0;
        link.UpdateLink();
    }
}