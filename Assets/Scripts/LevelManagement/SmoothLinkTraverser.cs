using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SmoothLinkTraverser : MonoBehaviour
{
    private NavMeshAgent agent;
    private bool isTraversing = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // FIX 1: Turn off Unity's default "hopping" traversal behavior
        agent.autoTraverseOffMeshLink = false;
    }

    void Update()
    {
        // FIX 2: Intercept the moment the agent reaches the RoomNavLinker
        if (agent.isOnOffMeshLink && !isTraversing)
        {
            StartCoroutine(TraverseLinkSmoothly());
        }
    }

    private IEnumerator TraverseLinkSmoothly()
    {
        isTraversing = true;
        OffMeshLinkData data = agent.currentOffMeshLinkData;

        Vector3 startPos = agent.transform.position;
        // Keep the agent's baseOffset in mind so it doesn't sink into the floor
        Vector3 endPos = data.endPos + (Vector3.up * agent.baseOffset);

        // Calculate how long the crossing should take based on the enemy's walk speed
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / Mathf.Max(agent.speed, 0.1f);
        float timePassed = 0f;

        // FIX 3: Manually glide the enemy in a perfectly straight, flat line
        while (timePassed < duration)
        {
            timePassed += Time.deltaTime;
            float percent = timePassed / duration;
            agent.transform.position = Vector3.Lerp(startPos, endPos, percent);
            yield return null;
        }

        // Snap precisely to the endpoint and resume normal pathfinding
        agent.transform.position = endPos;
        agent.CompleteOffMeshLink();
        isTraversing = false;
    }
}