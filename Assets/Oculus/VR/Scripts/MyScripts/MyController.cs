using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class MyController : MonoBehaviour
{
    [SerializeField] 
    private GameObject OVRCamera;
    [SerializeField] 
    private GameObject inGameMenu;
        
    public float boostValue = 1.0f;
    public float brakePower = 1.0f;
    public float thumbstickDeadzoneX = 0.001f;
    public float thumbstickDeadzoneY = 0.001f;
    
    [SerializeField] 
    private GameObject body;
    [SerializeField]
    private Vector3 bodyOffset = new Vector3(0.0f, -0.5f, 0.0f);
    [SerializeField]
    private float neckThreshold = 30.0f;
    [SerializeField] 
    private float bodyRotationSpeed = 2.0f;
    [SerializeField]
    private float cameraDistanceThreshold = 0.55f;

    private Vector3 camRotation;
    private Quaternion bodyRotation;
    private Rigidbody _rb;
    private PauseManager pauseManager;
    private Transform cameraTransform;
    private bool playerInRange = true;
    private bool menuOpened;


    // Start is called before the first frame update
    void Start()
    {
        _rb = gameObject.GetComponent<Rigidbody>();
        pauseManager = FindObjectOfType<PauseManager>();
    }

    void Update()
    {
        OVRCamera.transform.position = transform.position;
    }
    
    void LateUpdate()
    {
        MoveWithOVRCamera();
    }
    
    void FixedUpdate()
    {
        ProcessInput();
    }

    void MoveWithOVRCamera()
    {
        cameraTransform = Camera.main.transform;
        camRotation = cameraTransform.rotation.eulerAngles;
        bodyRotation = transform.rotation;

        if (playerInRange)
        {
            body.transform.position = cameraTransform.position + bodyOffset;
            float deltaAngle = Mathf.DeltaAngle(camRotation.y, bodyRotation.eulerAngles.y);
            if (deltaAngle > neckThreshold || deltaAngle < -neckThreshold)
            {
                _rb.MoveRotation(Quaternion.Lerp(bodyRotation, Quaternion.Euler(0.0f, camRotation.y, 0.0f),
                    Time.deltaTime * bodyRotationSpeed));
            }
        }
        //Debug.Log("Distance: " + Vector3.Distance(cameraTransform.position, transform.position));
        if (Vector3.Distance(cameraTransform.position, transform.position) > cameraDistanceThreshold)
        {
            playerInRange = false;
        }
        else
        {
            playerInRange = true;
        }
    }
    
    void ProcessInput()
    {
        Vector2 primaryAxis = ApplyDeadZones(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick));
        Vector2 secondaryAxis = ApplyDeadZones(OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick));

        if (playerInRange && !menuOpened)
        {
            if (primaryAxis.x != 0.0f || primaryAxis.y != 0.0f)
                _rb.AddRelativeForce(new Vector3(primaryAxis.x, 0.0f, primaryAxis.y) * boostValue);
            else if (secondaryAxis.x != 0.0f || secondaryAxis.y != 0.0f)
                _rb.AddRelativeForce(new Vector3(secondaryAxis.x, 0.0f, secondaryAxis.y) * boostValue);

            if (OVRInput.Get(OVRInput.Button.Two) || OVRInput.Get(OVRInput.Button.Four))
            {
                _rb.AddForce(transform.up * boostValue);
            }

            if (OVRInput.Get(OVRInput.Button.One) || OVRInput.Get(OVRInput.Button.Three))
            {
                _rb.AddForce(-transform.up * boostValue);
            }

            if (OVRInput.Get(OVRInput.Button.PrimaryThumbstick) || OVRInput.Get(OVRInput.Button.SecondaryThumbstick))
            {
                _rb.velocity = Vector3.Lerp(_rb.velocity, Vector3.zero, Time.deltaTime * brakePower);
                _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, Vector3.zero, Time.deltaTime * brakePower);
            }
        }
        /*else
        {
            _rb.velocity = Vector3.Lerp(_rb.velocity, Vector3.zero, Time.deltaTime * brakePower);
            _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, Vector3.zero, Time.deltaTime * brakePower);
        }*/

        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            menuOpened = !menuOpened;

            if (menuOpened)
            {
                pauseManager.Pause();
            }
            else
            {
                pauseManager.Resume();
            }
            
            inGameMenu.SetActive(menuOpened);
        }
    }
    
    Vector2 ApplyDeadZones(Vector2 pos) {

        // X Axis
        if ((pos.x > 0.0f && pos.x < thumbstickDeadzoneX) || (pos.x < 0.0f && pos.x > -thumbstickDeadzoneX)) {
            pos = new Vector2(0.0f, pos.y);
        }

        // Y Positive
        if ((pos.y > 0 && pos.y < thumbstickDeadzoneY) || (pos.y < 0 && pos.y > -thumbstickDeadzoneY)) {
            pos = new Vector2(pos.x, 0.0f);
        }

        return pos;
    }
}
