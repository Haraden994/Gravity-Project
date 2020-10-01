using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriftingObject : MonoBehaviour
{
    public enum ObjectType
    {
        drifting,
        rotating,
        driftingAndRotating
    };
    public ObjectType objectType;
    
    public Vector3 driftingDirection;
    public Vector3 rotationDirection;
    public float randomDriftPower;
    public float randomRotationPower;

    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (randomDriftPower > 0.0f)
            driftingDirection = new Vector3(Random.Range(-randomDriftPower, randomDriftPower), Random.Range(-randomDriftPower, randomDriftPower), Random.Range(-randomDriftPower, randomDriftPower));
        if(randomRotationPower > 0.0f)
            rotationDirection = new Vector3(Random.Range(-randomRotationPower, randomRotationPower), Random.Range(-randomRotationPower, randomRotationPower), Random.Range(-randomRotationPower, randomRotationPower));

        switch (objectType)
        {
            case ObjectType.drifting:
                gameObject.GetComponent<Rigidbody>().AddRelativeForce(driftingDirection * _rb.mass);
                break;
            case ObjectType.rotating:
                gameObject.GetComponent<Rigidbody>().AddRelativeTorque(rotationDirection * _rb.mass);
                break;
            case ObjectType.driftingAndRotating:
                gameObject.GetComponent<Rigidbody>().AddRelativeForce(driftingDirection * _rb.mass);
                gameObject.GetComponent<Rigidbody>().AddRelativeTorque(rotationDirection * _rb.mass);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
