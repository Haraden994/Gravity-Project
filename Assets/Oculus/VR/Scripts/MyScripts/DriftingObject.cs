using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriftingObject : MonoBehaviour
{
    public enum ObjectType
    {
        drifting,
        rotating
    };
    public ObjectType objectType;
    
    public Vector3 direction;

    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        switch (objectType)
        {
            case ObjectType.drifting:
                gameObject.GetComponent<Rigidbody>().AddRelativeForce(direction * _rb.mass);
                break;
            case ObjectType.rotating:
                gameObject.GetComponent<Rigidbody>().AddRelativeTorque(direction * _rb.mass);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
