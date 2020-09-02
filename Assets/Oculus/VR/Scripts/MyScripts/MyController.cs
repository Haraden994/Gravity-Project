using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MyController : MonoBehaviour
{
    [Header("Core Elements")]
    [SerializeField] 
    private GameObject OVRCamera;
    [SerializeField] 
    private OVRGrabber rightHand;
    [SerializeField] 
    private OVRGrabber leftHand;
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
    [SerializeField]
    private float thumbstickDeadzoneX = 0.001f;
    [SerializeField]
    private float thumbstickDeadzoneY = 0.001f;
    
    [Header("UI Components")]
    [SerializeField] 
    private GameObject inGameMenu;
    [SerializeField]
    private GameObject gameOverScreen;
    [SerializeField] 
    private GameObject outOfVrBodyUI;
    [SerializeField] 
    private Slider oxygenMeter;
    [SerializeField]
    private Slider boostMeter;
    [SerializeField] 
    private Image heavyBreathing;

    [Header("Gameplay Values")]
    public float boostPower = 1.0f;
    public float brakePower = 1.0f;
    public float oxygenAmount = 100.0f;
    public float oxygenDepletionAmount = 0.1f;
    public float boostAmount = 100.0f;
    public float boostDepletionAmount = 0.1f;
    public float timeToDie = 5.0f;

    private Vector3 camRotation;
    private Quaternion bodyRotation;
    private Rigidbody _rb;
    private PauseManager pauseManager;
    private Transform cameraTransform;
    
    //Booleans
    private bool playerInRange = true;
    private bool menuOpened;
    private bool pauseOnce = true;
    private bool resumeOnce;
    private bool gameOver;
    
    //Heavy Breathing animation and game over variables
    private float fade;
    private bool fadeCheck = true;
    private float maxFadeValue = 0.6f;
    private float fadeIncrease = 0.2f;
    private float fadeGameOverThreshold = 1.6f;
    private float fadeInterpolator = 0.0f;
    private Color breathingEffectColor;

    // Start is called before the first frame update
    void Start()
    {
        _rb = gameObject.GetComponent<Rigidbody>();
        pauseManager = FindObjectOfType<PauseManager>();
        
        oxygenMeter.maxValue = oxygenAmount;
        boostMeter.maxValue = boostAmount;
        breathingEffectColor = heavyBreathing.color;
    }

    void Update()
    {
        OVRCamera.transform.position = transform.position;
        cameraTransform = Camera.main.transform;
        camRotation = cameraTransform.rotation.eulerAngles;
        bodyRotation = transform.rotation;
        oxygenMeter.value = oxygenAmount;
        boostMeter.value = boostAmount;
        
        if (!gameOver)
        {
            CheckPlayerInRange();
            HandleOxygen();
        }
    }
    
    void LateUpdate()
    {
        MoveWithOVRCamera();
    }
    
    void FixedUpdate()
    {
        if(!gameOver)
            ProcessInput();
    }

    void CheckPlayerInRange()
    {
        //Debug.Log("Distance: " + Vector3.Distance(cameraTransform.position, transform.position));
        if (Vector3.Distance(cameraTransform.position, transform.position) > cameraDistanceThreshold)
        {
            playerInRange = false;
            if(pauseOnce){
                outOfVrBodyUI.SetActive(true);
                pauseManager.Pause();
                pauseOnce = false;
                resumeOnce = true;
            }
        }
        else
        {
            playerInRange = true;
            if (resumeOnce)
            {
                outOfVrBodyUI.SetActive(false);
                pauseManager.Resume();
                resumeOnce = false;
                pauseOnce = true;
            }
        }
    }

    void HandleOxygen()
    {
        //Oxygen Handling
        if (playerInRange && !menuOpened && oxygenAmount > 0.0f)
        {
            oxygenAmount -= Time.deltaTime * oxygenDepletionAmount;
        }
        else if (oxygenAmount <= 0.0f)
        {
            heavyBreathing.gameObject.SetActive(true);

            //Heavy Breathing effect in the HUD
            if (fadeCheck)
            {
                fade = Mathf.Lerp(0.0f, maxFadeValue, fadeInterpolator);
                fadeInterpolator += 0.25f * Time.deltaTime;
                if (fadeInterpolator > 1.0f)
                {
                    fadeInterpolator = 0.0f;
                    fadeCheck = false;
                }
            }
            else
            {
                fade = Mathf.Lerp(maxFadeValue, 0.0f, fadeInterpolator);
                fadeInterpolator += 0.5f * Time.deltaTime;
                if (fadeInterpolator > 1.0f)
                {
                    fadeInterpolator = 0.0f;
                    maxFadeValue += fadeIncrease;
                    fadeCheck = true;
                }
            }
            breathingEffectColor.a = fade;
            heavyBreathing.color = breathingEffectColor;
            if (maxFadeValue > fadeGameOverThreshold)
            {
                GameOver();
            }
        }
    }
    
    void MoveWithOVRCamera()
    {
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
    }
    
    void ProcessInput()
    {
        Vector2 primaryAxis = ApplyDeadZones(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick));
        Vector2 secondaryAxis = ApplyDeadZones(OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick));
        bool rightClimbing = rightHand.isClimbing;
        bool leftClimbing = leftHand.isClimbing;

        if (playerInRange && !menuOpened && boostAmount > 0.0f)
        {
            //Boost towards Thumbstick direction
            if (primaryAxis.x != 0.0f || primaryAxis.y != 0.0f)
            {
                if (rightClimbing)
                {
                    rightHand.grabbedObject.grabbedRigidbody.AddRelativeForce(new Vector3(primaryAxis.x, 0.0f, primaryAxis.y) * boostPower);
                }
                else if (leftClimbing)
                {
                    leftHand.grabbedObject.grabbedRigidbody.AddRelativeForce(new Vector3(primaryAxis.x, 0.0f, primaryAxis.y) * boostPower);
                }
                else
                {
                    _rb.AddRelativeForce(new Vector3(primaryAxis.x, 0.0f, primaryAxis.y) * boostPower);
                }
                
            }
            else if (secondaryAxis.x != 0.0f || secondaryAxis.y != 0.0f)
            {
                if (rightClimbing)
                {
                    rightHand.grabbedObject.grabbedRigidbody.AddRelativeForce(new Vector3(secondaryAxis.x, 0.0f, secondaryAxis.y) * boostPower);
                }
                else if (leftClimbing)
                {
                    leftHand.grabbedObject.grabbedRigidbody.AddRelativeForce(new Vector3(secondaryAxis.x, 0.0f, secondaryAxis.y) * boostPower);
                }
                else
                {
                    _rb.AddRelativeForce(new Vector3(secondaryAxis.x, 0.0f, secondaryAxis.y) * boostPower);
                }
            }
            
            //Boost UP
            if (OVRInput.Get(OVRInput.Button.Two) || OVRInput.Get(OVRInput.Button.Four))
            {
                if (rightClimbing)
                {
                    rightHand.grabbedObject.grabbedRigidbody.AddForce(rightHand.grabbedObject.transform.up * boostPower);
                }
                else if (leftClimbing)
                {
                    leftHand.grabbedObject.grabbedRigidbody.AddForce(leftHand.grabbedObject.transform.up * boostPower);
                }
                else
                {
                    _rb.AddForce(transform.up * boostPower);
                }
            }
            
            //Boost DOWN
            if (OVRInput.Get(OVRInput.Button.One) || OVRInput.Get(OVRInput.Button.Three))
            {
                if (rightClimbing)
                {
                    rightHand.grabbedObject.grabbedRigidbody.AddForce(-rightHand.grabbedObject.transform.up * boostPower);
                }
                else if (leftClimbing)
                {
                    leftHand.grabbedObject.grabbedRigidbody.AddForce(-leftHand.grabbedObject.transform.up * boostPower);
                }
                else
                {
                    _rb.AddForce(-transform.up * boostPower);
                }
            }
            
            //Check if any Boost command is being executed
            if (OVRInput.Get(OVRInput.Button.One) 
                || OVRInput.Get(OVRInput.Button.Two) 
                || OVRInput.Get(OVRInput.Button.Three) 
                || OVRInput.Get(OVRInput.Button.Four) 
                || primaryAxis.x != 0.0f || primaryAxis.y != 0.0f 
                || secondaryAxis.x != 0.0f || secondaryAxis.y != 0.0f)
            {
                //TODO: Boost SFX
                boostAmount -= Time.deltaTime * boostDepletionAmount;
            }

            //Brake
            if (OVRInput.Get(OVRInput.Button.PrimaryThumbstick) || OVRInput.Get(OVRInput.Button.SecondaryThumbstick))
            {
                if (!rightClimbing && !leftClimbing)
                {
                    _rb.velocity = Vector3.Lerp(_rb.velocity, Vector3.zero, Time.deltaTime * brakePower);
                    _rb.angularVelocity = Vector3.Lerp(_rb.angularVelocity, Vector3.zero, Time.deltaTime * brakePower);
                }
            }
        }

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

    void GameOver()
    {
        gameOver = true;
        gameOverScreen.SetActive(true);
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
