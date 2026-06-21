using UnityEngine;

public class FirstDoorCloser : MonoBehaviour
{
    public FirstDoorShutting firstDoorScript;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            firstDoorScript.LerpZToZero();
            Destroy(gameObject);
        }
    }
}
