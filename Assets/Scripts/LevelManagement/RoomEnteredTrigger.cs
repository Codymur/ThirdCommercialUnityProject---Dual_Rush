using System.Collections;
using UnityEngine;

/// <summary>
/// Placed at the entrance of each room. Destroys the previous room once the
/// player has physically stepped past the threshold and remained inside for
/// at least <see cref="dwellTime"/> seconds, preventing premature teardown
/// when the player only briefly crosses the boundary then steps back.
/// </summary>
public class RoomEnteredTrigger : MonoBehaviour
{
    [Tooltip("Seconds the player must remain inside the trigger before the previous room is destroyed.")]
    [SerializeField] private float dwellTime = 0.75f;

    private Coroutine _pendingDestroy;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _pendingDestroy = StartCoroutine(DestroyAfterDwell());
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_pendingDestroy != null)
        {
            StopCoroutine(_pendingDestroy);
            _pendingDestroy = null;
        }
    }

    IEnumerator DestroyAfterDwell()
    {
        yield return new WaitForSeconds(dwellTime);
        RoomManager.Instance.DestroyPreviousRoom();
        GetComponent<Collider>().enabled = false;
    }
}
