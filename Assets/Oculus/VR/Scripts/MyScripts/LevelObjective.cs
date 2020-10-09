using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelObjective : MonoBehaviour
{
    public WarpCounter warpCounter;
    
    private OVRGrabbable grabbable;

    private bool onceGrabbed = true;
    
    // Start is called before the first frame update
    void Start()
    {
        grabbable = GetComponent<OVRGrabbable>();
    }

    // Update is called once per frame
    void Update()
    {
        if (onceGrabbed && grabbable.isGrabbed)
        {
            warpCounter.activationCounter--;
            onceGrabbed = false;
        }
    }
}
