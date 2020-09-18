using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField]
    private GameObject inGameMenu;
    
    private Rigidbody[] rigidbodies;
    private Vector3[] velocities;
    private Vector3[] angularVelocities;
    
    public bool isTutorial;

    private bool paused;
    private bool once = true;

    void Start()
    {
        rigidbodies = FindObjectsOfType<Rigidbody>();
        velocities = new Vector3[rigidbodies.Length];
        angularVelocities = new Vector3[rigidbodies.Length];
    }

    private void Update()
    {
        if (isTutorial && once)
        {
            once = false;
            Pause();
        }

        if (!isTutorial)
        {
            if (OVRInput.GetDown(OVRInput.Button.Start))
            {
                paused = !paused;

                if (paused)
                {
                    Pause();
                }
                else
                {
                    Resume();
                }

                inGameMenu.SetActive(paused);
            }
        }
    }

    public bool IsPaused()
    {
        return paused;
    }
    
    public void CloseTutorial()
    {
        Resume();
        isTutorial = false;
    }

    public void Pause()
    {
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            velocities[i] = rigidbodies[i].velocity;
            angularVelocities[i] = rigidbodies[i].angularVelocity;
            rigidbodies[i].velocity = Vector3.zero;
            rigidbodies[i].angularVelocity = Vector3.zero;
        }
    }

    public void Resume()
    {
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].velocity = velocities[i];
            rigidbodies[i].angularVelocity = angularVelocities[i];
        }
    }
}
