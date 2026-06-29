using UnityEngine;

public class FirstDoorCloser : MonoBehaviour
{
    public FirstDoorShutting firstDoorScript;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            firstDoorScript.LerpZToZero();

            // Player has just crossed out of FirstLevel into the first procedural
            // room. Activate its enemies now — they were sitting idle on batch
            // load so combat wouldn't start while the player was still looting.
            if (RoomManager.Instance != null)
                RoomManager.Instance.ActivateFirstProceduralRoom();

            Destroy(gameObject);
        }
    }
}