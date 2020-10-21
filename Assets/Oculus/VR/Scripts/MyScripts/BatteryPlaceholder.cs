using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatteryPlaceholder : MonoBehaviour
{
    public WarpCounter warpCounter;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Battery"))
        {
            GameObject battery = other.gameObject;
            OVRGrabbable batteryGrabbable = battery.GetComponent<OVRGrabbable>();
            Rigidbody batteryRB = battery.GetComponent<Rigidbody>();
            
            batteryGrabbable.grabbedBy.ForceRelease(batteryGrabbable);
            batteryRB.velocity = Vector3.zero;
            batteryRB.angularVelocity = Vector3.zero;
            battery.GetComponent<MeshCollider>().enabled = false;
            battery.transform.position = transform.position;
            battery.transform.rotation = transform.rotation;
            warpCounter.activationCounter--;
            gameObject.SetActive(false);
        }
    }
}
