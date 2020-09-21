using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialCrate : MonoBehaviour
{
    public GameObject tutorialWindow;
        
    private OVRGrabbable grabbable;
    
    // Start is called before the first frame update
    void Start()
    {
        grabbable = GetComponent<OVRGrabbable>();
    }

    // Update is called once per frame
    void Update()
    {
        if (grabbable.isGrabbed)
        {
            tutorialWindow.SetActive(false);
        }
    }
}
