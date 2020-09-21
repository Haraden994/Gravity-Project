using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class WarpGateTutorial : MonoBehaviour
{
    public GameObject tutorial;

    private Rigidbody playerRB;
    private bool brakePlayer;
    private float speedThreshold = 0.1f;

    private bool once = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Prop"))
        {
            Destroy(other.gameObject);

            if (tutorial)
                tutorial.SetActive(true);
        }

        if (other.gameObject.CompareTag("Player") && once)
        {
            playerRB = other.gameObject.GetComponent<Rigidbody>();
            brakePlayer = true;
            once = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (brakePlayer && playerRB)
        {
            playerRB.velocity = Vector3.Lerp(playerRB.velocity, Vector3.zero, Time.deltaTime * 2);
            playerRB.angularVelocity = Vector3.Lerp(playerRB.angularVelocity, Vector3.zero, Time.deltaTime * 2);
            

            if (playerRB.velocity.magnitude <= speedThreshold)
            {
                brakePlayer = false;
                tutorial.SetActive(true);
            }
        }
    }
}
