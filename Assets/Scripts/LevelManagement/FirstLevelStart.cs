using UnityEngine;

public class FirstLevelStart : MonoBehaviour
{
    public RunManager RunManagerScript;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RunManagerScript.StartRun();
            //Destroy(gameObject);
        }
    }
}
