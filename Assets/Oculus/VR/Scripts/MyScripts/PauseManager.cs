using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField]
    private GameObject inGameMenu;
    [SerializeField] 
    private GameObject tutorial;

    [SerializeField] private OVRGrabber leftGrabber;
    [SerializeField] private OVRGrabber rightGrabber;

    private Rigidbody[] rigidbodies;
    private Vector3[] velocities;
    private Vector3[] angularVelocities;
    
    public bool startPaused;

    private bool paused;
    private bool menuOpened;
    private bool once = true;

    void Start()
    {
        rigidbodies = FindObjectsOfType<Rigidbody>();
        velocities = new Vector3[rigidbodies.Length];
        angularVelocities = new Vector3[rigidbodies.Length];
    }

    private void Update()
    {
        if (startPaused && once)
        {
            once = false;
            Pause();
        }

        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            menuOpened = !menuOpened;

            if (menuOpened)
            {
                Pause();
            }
            else
            {
                Resume();
            }

            if (tutorial)
            {
                tutorial.SetActive(!menuOpened);
            }
            inGameMenu.SetActive(menuOpened);
        }
    }

    public bool IsPaused()
    {
        return paused;
    }

    public void Pause()
    {
        leftGrabber.GamePaused();
        rightGrabber.GamePaused();
        
        paused = true;
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            if(rigidbodies[i].gameObject.CompareTag("NeverPaused"))
                continue;
            
            velocities[i] = rigidbodies[i].velocity;
            angularVelocities[i] = rigidbodies[i].angularVelocity;
            rigidbodies[i].velocity = Vector3.zero;
            rigidbodies[i].angularVelocity = Vector3.zero;
        }
    }

    public void Resume()
    {
        leftGrabber.GameResumed();
        rightGrabber.GameResumed();
        
        paused = false;
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            if(rigidbodies[i].gameObject.CompareTag("NeverPaused"))
                continue;
            
            rigidbodies[i].velocity = velocities[i];
            rigidbodies[i].angularVelocity = angularVelocities[i];
        }
    }

    public void DeleteTutorial()
    {
        tutorial = null;
    }
}
