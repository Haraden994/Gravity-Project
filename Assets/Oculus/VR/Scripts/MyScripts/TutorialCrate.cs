using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialCrate : MonoBehaviour
{
    public Vector3 positionOffset;
    public bool spawnNearPlayer;
    public GameObject tutorialWindow;
    public Transform player;

    private OVRGrabbable grabbable;
    
    // Start is called before the first frame update
    void Start()
    {
        grabbable = GetComponent<OVRGrabbable>();
    }

    // Update is called once per frame
    void Update()
    {
        if (tutorialWindow)
        {
            if (grabbable.isGrabbed)
            {
                tutorialWindow.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        if(spawnNearPlayer && player)
            transform.position = player.position + positionOffset;
    }
}
