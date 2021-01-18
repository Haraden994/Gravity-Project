using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CryoBed : MonoBehaviour
{
    public Collider otherCollider;
    
    private OVRGrabbable _grabbable;
    
    // Start is called before the first frame update
    void Start()
    {
        _grabbable = GetComponent<OVRGrabbable>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_grabbable.isGrabbed)
            otherCollider.enabled = false;
        else
            otherCollider.enabled = true;
    }
}
