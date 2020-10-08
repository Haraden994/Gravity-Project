using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceTank : MonoBehaviour
{
    public enum ResourceType
    {
        oxygen,
        boost
    }
    public ResourceType resourceType;
    public float restorationAmount;
    public AudioSource sfx;
    
    private MyController playerController;
    private ParticleSystem particles;
    private OVRGrabbable grabbable;
    private float tempAmount;

    // Start is called before the first frame update
    void Start()
    {
        playerController = FindObjectOfType<MyController>();
        particles = GetComponentInChildren<ParticleSystem>();
        grabbable = GetComponent<OVRGrabbable>();
    }

    // Update is called once per frame
    void Update()
    {
        if (grabbable.isGrabbed)
        {
            if(!particles.isPaused)
                particles.Stop();

            if ((OVRInput.Get(OVRInput.Button.One) && grabbable.grabbedBy.CompareTag("Right"))
                || (OVRInput.Get(OVRInput.Button.Three) && grabbable.grabbedBy.CompareTag("Left")))
            {
                switch (resourceType)
                {
                    case ResourceType.boost:
                        tempAmount = playerController.boostAmount;
                        tempAmount += restorationAmount;
                        if (tempAmount > 100.0f)
                            playerController.SetBoost(100.0f);
                        else
                            playerController.SetBoost(playerController.boostAmount + restorationAmount);
                        break;
                    case ResourceType.oxygen:
                        tempAmount = playerController.oxygenAmount;
                        tempAmount += restorationAmount;
                        if (tempAmount > 100.0f)
                            playerController.SetOxygen(100.0f);
                        else
                            playerController.SetOxygen(playerController.oxygenAmount + restorationAmount);
                        break;
                }
                grabbable.grabbedBy.ForceRelease(grabbable);
                sfx.Play();
                gameObject.SetActive(false);
            }
        }
        else
        {
            if(particles.isPaused) 
                particles.Play();
        }
    }
}
