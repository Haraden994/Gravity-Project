using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WarpCounter : MonoBehaviour
{
    public GameObject warpGate;
    public int activationCounter;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (activationCounter == 0)
        {
            warpGate.SetActive(true);
        }
    }
}
