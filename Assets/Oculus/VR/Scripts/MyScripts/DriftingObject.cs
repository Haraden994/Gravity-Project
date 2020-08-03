using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriftingObject : MonoBehaviour
{
    public Vector3 driftingDirection;
    
    // Start is called before the first frame update
    void Start()
    {
        gameObject.GetComponent<Rigidbody>().AddForce(driftingDirection);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
