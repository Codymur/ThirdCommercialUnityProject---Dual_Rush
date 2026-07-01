using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimbDismemberment : MonoBehaviour
{

    [SerializeField] LimbDismemberment[] childLimbs;

    [SerializeField] GameObject woundHole;

    private void Start()
    {
        if (woundHole != null)
        {
            woundHole.SetActive(false);
        }
    }

    public void GetHit()
    {
        if (childLimbs.Length > 0)
        {
            foreach (LimbDismemberment limb in childLimbs)
            {
                if (limb != null)
                {
                    limb.GetHit();
                }
            }
        }

        if (woundHole != null)
        {
            woundHole.SetActive(true);
        }


        transform.localScale = Vector3.zero;
        Destroy(this);
    }

}
