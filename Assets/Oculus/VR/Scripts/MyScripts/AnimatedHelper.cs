using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedHelper : MonoBehaviour
{
    
    public enum AnimationType
    {
        scaleChanging,
        moving,
        none
    };
    public AnimationType animType;

    public Transform player;
    public float animationSpeed;

    [Header("Scale case")]
    public float maxScale;
    public float minScale;

    [Header("Position case")] 
    public Transform startingPosition; 
    public Transform endingPosition; 

    private float scale;
    private OVRGrabbable grabbable;


    // Start is called before the first frame update
    void Start()
    {
        grabbable = GetComponentInParent<OVRGrabbable>();
    }

    // Update is called once per frame
    void Update()
    {
        if(player)
            transform.LookAt(Camera.main.transform);

        if (grabbable)
        {
            if (grabbable.isGrabbed)
            {
                gameObject.SetActive(false);
            }
        }

        switch (animType)
        {
            case AnimationType.scaleChanging:
                scale = Mathf.Lerp(minScale, maxScale, Mathf.PingPong(Time.time * animationSpeed, 1));
                transform.localScale = new Vector3(scale, scale, scale);
                break;
            case AnimationType.moving:
                transform.position = Vector3.Lerp(startingPosition.position, endingPosition.position, Mathf.PingPong(Time.time * animationSpeed, 1));
                break;
            case AnimationType.none:
                break;
        }
        
        
    }
}
