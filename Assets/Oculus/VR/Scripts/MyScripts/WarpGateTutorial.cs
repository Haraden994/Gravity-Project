using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class WarpGateTutorial : MonoBehaviour
{
    public GameObject tutorial;

    public GameObject nextGate;

    public GameObject disabledGate;

    public bool goal;
    public bool needsObjective;

    private Rigidbody playerRB;
    private bool brakePlayer;
    private float speedThreshold = 0.01f;
    private MyController playerController;

    private bool once = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable()
    {
        if (disabledGate)
            disabledGate.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && once)
        {
            playerRB = other.gameObject.GetComponent<Rigidbody>();
            brakePlayer = true;

            if (nextGate)
            {
                nextGate.SetActive(true);
            }

            if (goal)
            {
                playerRB.gameObject.GetComponent<MyController>().GameOver(true);
            }

            once = false;
        }

        if (needsObjective)
        {
            if (other.gameObject.CompareTag("Objective"))
            {
                playerController = FindObjectOfType<MyController>();
                playerController.GameOver(true);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (brakePlayer && playerRB)
        {
            playerRB.velocity = Vector3.Lerp(playerRB.velocity, Vector3.zero, Time.deltaTime * 4);
            playerRB.angularVelocity = Vector3.Lerp(playerRB.angularVelocity, Vector3.zero, Time.deltaTime * 4);
            
            if (playerRB.velocity.magnitude <= speedThreshold)
            {
                brakePlayer = false;
                if(tutorial)
                    tutorial.SetActive(true);
                if (!goal)
                {
                    disabledGate.SetActive(true);
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
