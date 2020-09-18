/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus Utilities SDK License Version 1.31 (the "License"); you may not use
the Utilities SDK except in compliance with the License, which is provided at the time of installation
or download, or which otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at
https://developer.oculus.com/licenses/utilities-1.31

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows grabbing and throwing of objects with the OVRGrabbable component on them.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class OVRGrabber : MonoBehaviour
{
    // Grip trigger thresholds for picking up objects, with some hysteresis.
    public float grabBegin = 0.55f;
    public float grabEnd = 0.35f;

    bool alreadyUpdated = false;

    // Demonstrates parenting the held object to the hand's transform when grabbed.
    // When false, the grabbed object is moved every FixedUpdate using MovePosition.
    // Note that MovePosition is required for proper physics simulation. If you set this to true, you can
    // easily observe broken physics simulation by, for example, moving the bottom cube of a stacked
    // tower and noting a complete loss of friction.
    [SerializeField]
    protected bool m_parentHeldObject = false;

	// If true, this script will move the hand to the transform specified by m_parentTransform, using MovePosition in
	// FixedUpdate. This allows correct physics behavior, at the cost of some latency. In this usage scenario, you
	// should NOT parent the hand to the hand anchor.
	// (If m_moveHandPosition is false, this script will NOT update the game object's position.
	// The hand gameObject can simply be attached to the hand anchor, which updates position in LateUpdate,
    // gaining us a few ms of reduced latency.)
    [SerializeField]
    protected bool m_moveHandPosition = false;

    // Child/attached transforms of the grabber, indicating where to snap held objects to (if you snap them).
    // Also used for ranking grab targets in case of multiple candidates.
    [SerializeField]
    protected Transform m_gripTransform = null;
    // Child/attached Colliders to detect candidate grabbable objects.
    [SerializeField]
    protected Collider[] m_grabVolumes = null;

    // Should be OVRInput.Controller.LTouch or OVRInput.Controller.RTouch.
    [SerializeField]
    protected OVRInput.Controller m_controller;

	// You can set this explicitly in the inspector if you're using m_moveHandPosition.
	// Otherwise, you should typically leave this null and simply parent the hand to the hand anchor
	// in your scene, using Unity's inspector.
    [SerializeField]
    protected Transform m_parentTransform;

    [SerializeField]
    protected GameObject m_player;

    protected bool m_grabVolumeEnabled = true;
    protected Vector3 m_lastPos;
    protected Quaternion m_lastRot;
    protected Quaternion m_anchorOffsetRotation;
    protected Vector3 m_anchorOffsetPosition;
    protected float m_prevFlex;
	protected OVRGrabbable m_grabbedObj = null;
    protected Rigidbody m_grabbedRigidbody = null;
    protected Vector3 m_grabbedObjectPosOff;
    protected Quaternion m_grabbedObjectRotOff;
    protected Vector3 m_playerPosOff;
	protected Dictionary<OVRGrabbable, int> m_grabCandidates = new Dictionary<OVRGrabbable, int>();
	protected bool m_operatingWithoutOVRCameraRig = true;
    
    [HideInInspector]
    public Transform dummyTransform;

    private Rigidbody playerRB;
    private Vector3 playerMomentum;
    
    private bool oncePerGrab = true;
    [HideInInspector]
    public bool isGrabbing = false;
    [HideInInspector]
    public bool isClimbing = false;
    [HideInInspector]
    public bool collisionCheck;
    private Vector3 grabberPreviousPosition;
    private Vector3 climbingMovement = Vector3.zero;

    private AudioSource grabSFX;

    /// <summary>
    /// The currently grabbed object.
    /// </summary>
    public OVRGrabbable grabbedObject
    {
        get { return m_grabbedObj; }
    }

	public void ForceRelease(OVRGrabbable grabbable)
    {
        bool canRelease = (
            (m_grabbedObj != null) &&
            (m_grabbedObj == grabbable)
        );
        if (canRelease)
        {
            GrabEnd();
        }
    }

    protected virtual void Awake()
    {
        m_anchorOffsetPosition = transform.localPosition;
        m_anchorOffsetRotation = transform.localRotation;

        if(!m_moveHandPosition)
        {
		    // If we are being used with an OVRCameraRig, let it drive input updates, which may come from Update or FixedUpdate.
		    OVRCameraRig rig = transform.GetComponentInParent<OVRCameraRig>();
		    if (rig != null)
		    {
			    rig.UpdatedAnchors += (r) => {OnUpdatedAnchors();};
			    m_operatingWithoutOVRCameraRig = false;
		    }
        }

        playerRB = m_player.GetComponent<Rigidbody>();
    }

    protected virtual void Start()
    {
        m_lastPos = transform.position;
        m_lastRot = transform.rotation;
        if(m_parentTransform == null)
        {
			m_parentTransform = gameObject.transform;
        }

        grabSFX = m_parentTransform.GetComponentInChildren<AudioSource>();
        
		// We're going to setup the player collision to ignore the hand collision.
		SetPlayerIgnoreCollision(gameObject, true);
    }

    virtual public void Update()
    {
        alreadyUpdated = false;
    }

    virtual public void FixedUpdate()
    {
        playerMomentum = playerRB.mass * playerRB.velocity;
		if (m_operatingWithoutOVRCameraRig)
        {
		    OnUpdatedAnchors();
        }
	}

    // Hands follow the touch anchors by calling MovePosition each frame to reach the anchor.
    // This is done instead of parenting to achieve workable physics. If you don't require physics on
    // your hands or held objects, you may wish to switch to parenting.
    void OnUpdatedAnchors()
    {
        // Don't want to MovePosition multiple times in a frame, as it causes high judder in conjunction
        // with the hand position prediction in the runtime.
        if (alreadyUpdated) return;
        alreadyUpdated = true;

        Vector3 destPos = m_parentTransform.TransformPoint(m_anchorOffsetPosition);
        Quaternion destRot = m_parentTransform.rotation * m_anchorOffsetRotation;

        if (m_moveHandPosition)
        {
            GetComponent<Rigidbody>().MovePosition(destPos);
            GetComponent<Rigidbody>().MoveRotation(destRot);
        }

        if (!m_parentHeldObject && isGrabbing)
        {
            MoveGrabbedObject(destPos, destRot);
        }

        if (isClimbing)
        {
            grabberPreviousPosition = dummyTransform.position;
            transform.position = dummyTransform.position;
            Vector3 movementAmount = grabberPreviousPosition - destPos;
            climbingMovement += movementAmount;
            playerRB.MovePosition(climbingMovement);
        }

        m_lastPos = transform.position;
        m_lastRot = transform.rotation;

		float prevFlex = m_prevFlex;
		// Update values from inputs
		m_prevFlex = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, m_controller);

		CheckForGrabOrRelease(prevFlex);
    }

    void OnDestroy()
    {
        if (m_grabbedObj != null)
        {
            GrabEnd();
        }
    }

    void OnTriggerEnter(Collider otherCollider)
    {
        // Get the grab trigger
		OVRGrabbable grabbable = otherCollider.GetComponent<OVRGrabbable>() ?? otherCollider.GetComponentInParent<OVRGrabbable>();
        if (grabbable == null) return;

        // Add the grabbable
        int refCount = 0;
        m_grabCandidates.TryGetValue(grabbable, out refCount);
        m_grabCandidates[grabbable] = refCount + 1;
    }

    void OnTriggerExit(Collider otherCollider)
    {
		OVRGrabbable grabbable = otherCollider.GetComponent<OVRGrabbable>() ?? otherCollider.GetComponentInParent<OVRGrabbable>();
        if (grabbable == null) return;

        // Remove the grabbable
        int refCount = 0;
        bool found = m_grabCandidates.TryGetValue(grabbable, out refCount);
        if (!found)
        {
            return;
        }

        if (refCount > 1)
        {
            m_grabCandidates[grabbable] = refCount - 1;
        }
        else
        {
            m_grabCandidates.Remove(grabbable);
        }
    }

    protected void CheckForGrabOrRelease(float prevFlex)
    {
        if ((m_prevFlex >= grabBegin) && (prevFlex < grabBegin))
        {
            GrabBegin();
        }
        else if ((m_prevFlex <= grabEnd) && (prevFlex > grabEnd))
        {
            GrabEnd();
        }
    }

    protected virtual void GrabBegin()
    {
        float closestMagSq = float.MaxValue;
		OVRGrabbable closestGrabbable = null;
        Collider closestGrabbableCollider = null;

        // Iterate grab candidates and find the closest grabbable candidate
		foreach (OVRGrabbable grabbable in m_grabCandidates.Keys)
        {
            bool canGrab = !(grabbable.isGrabbed && !grabbable.allowOffhandGrab);
            if (!canGrab)
            {
                continue;
            }

            for (int j = 0; j < grabbable.grabPoints.Length; ++j)
            {
                Collider grabbableCollider = grabbable.grabPoints[j];
                // Store the closest grabbable
                Vector3 closestPointOnBounds = grabbableCollider.ClosestPointOnBounds(m_gripTransform.position);
                float grabbableMagSq = (m_gripTransform.position - closestPointOnBounds).sqrMagnitude;
                if (grabbableMagSq < closestMagSq)
                {
                    closestMagSq = grabbableMagSq;
                    closestGrabbable = grabbable;
                    closestGrabbableCollider = grabbableCollider;
                }
            }
        }

        // Disable grab volumes to prevent overlaps
        GrabVolumeEnable(false);
        
        if (closestGrabbable != null)
        {
            grabSFX.Play();
            
            if (closestGrabbable.isGrabbed)
            {
                closestGrabbable.grabbedBy.OffhandGrabbed(closestGrabbable);
            }

            m_grabbedObj = closestGrabbable;
            m_grabbedObj.GrabBegin(this, closestGrabbableCollider);
            m_grabbedRigidbody = m_grabbedObj.grabbedRigidbody;

            m_lastPos = transform.position;
            m_lastRot = transform.rotation;

            // Set up offsets for grabbed object desired position relative to hand.
            if(m_grabbedObj.snapPosition)
            {
                m_grabbedObjectPosOff = m_gripTransform.localPosition;
                if(m_grabbedObj.snapOffset)
                {
                    Vector3 snapOffset = m_grabbedObj.snapOffset.position;
                    if (m_controller == OVRInput.Controller.LTouch) snapOffset.x = -snapOffset.x;
                    m_grabbedObjectPosOff += snapOffset;
                }
            }
            else
            {
                Vector3 relPos = m_grabbedObj.transform.position - transform.position;
                relPos = Quaternion.Inverse(transform.rotation) * relPos;
                m_grabbedObjectPosOff = relPos;
            }

            if (m_grabbedObj.snapOrientation)
            {
                m_grabbedObjectRotOff = m_gripTransform.localRotation;
                if(m_grabbedObj.snapOffset)
                {
                    m_grabbedObjectRotOff = m_grabbedObj.snapOffset.rotation * m_grabbedObjectRotOff;
                }
            }
            else
            {
                Quaternion relOri = Quaternion.Inverse(transform.rotation) * m_grabbedObj.transform.rotation;
                m_grabbedObjectRotOff = relOri;
            }

            // Note: force teleport on grab, to avoid high-speed travel to dest which hits a lot of other objects at high
            // speed and sends them flying. The grabbed object may still teleport inside of other objects, but fixing that
            // is beyond the scope of this demo.
            if (isClimbing)
            {
                playerRB.velocity = Vector3.zero;
                m_grabbedRigidbody.velocity = (playerMomentum + grabbedObject.momentum) / (playerRB.mass + grabbedObject.grabbedRigidbody.mass);

                if (dummyTransform == null)
                {
                    GameObject go = new GameObject();
                    go.transform.name = "DummyTransform";
                    go.transform.parent = transform;
                    go.transform.position = transform.position;
                    go.transform.localEulerAngles = Vector3.zero;

                    dummyTransform = go.transform;
                }
                dummyTransform.parent = grabbedObject.transform;
                grabberPreviousPosition = dummyTransform.position;
                climbingMovement = playerRB.position;
            }
            if (isGrabbing)
            {
                MoveGrabbedObject(m_lastPos, m_lastRot, false);
                SetPlayerIgnoreCollision(m_grabbedObj.gameObject, true);
                playerRB.velocity = (playerMomentum + grabbedObject.momentum) / (playerRB.mass + grabbedObject.grabbedRigidbody.mass);
            }
            
            if (m_parentHeldObject)
            {
                m_grabbedObj.transform.parent = transform;
            }
        }
    }

    protected virtual void MoveGrabbedObject(Vector3 pos, Quaternion rot, bool forceTeleport = false)
    {
        if (m_grabbedObj == null)
        {
            return;
        }
        
        Vector3 grabbablePosition = pos + rot * m_grabbedObjectPosOff;
        Quaternion grabbableRotation = rot * m_grabbedObjectRotOff;

        if (forceTeleport)
        {
            m_grabbedRigidbody.transform.position = grabbablePosition;
            m_grabbedRigidbody.transform.rotation = grabbableRotation;
        }
        else
        {
            m_grabbedRigidbody.MovePosition(grabbablePosition);
            m_grabbedRigidbody.MoveRotation(grabbableRotation);
        }
    }

    protected void GrabEnd()
    {
        if (m_grabbedObj != null)
        {
			OVRPose localPose = new OVRPose { position = OVRInput.GetLocalControllerPosition(m_controller), orientation = OVRInput.GetLocalControllerRotation(m_controller) };
            OVRPose offsetPose = new OVRPose { position = m_anchorOffsetPosition, orientation = m_anchorOffsetRotation };
            localPose = localPose * offsetPose;

			OVRPose trackingSpace = transform.ToOVRPose() * localPose.Inverse();
			Vector3 linearVelocity = trackingSpace.orientation * OVRInput.GetLocalControllerVelocity(m_controller);
			Vector3 angularVelocity = trackingSpace.orientation * OVRInput.GetLocalControllerAngularVelocity(m_controller);
            Vector3 linearAcceleration = trackingSpace.orientation * OVRInput.GetLocalControllerAcceleration(m_controller);
            Vector3 angularAcceleration = trackingSpace.orientation * OVRInput.GetLocalControllerAngularAcceleration(m_controller);

            // Velocity = (Force of the player / Mass of the grabbed) * Time.fixedDeltaTime
            Vector3 forceApplied = -linearAcceleration * playerRB.mass;
            Vector3 rotationalForceApplied = angularAcceleration * (playerRB.mass / 1.5f);
            Vector3 grabbedVelocity = forceApplied / grabbedObject.grabbedRigidbody.mass * Time.fixedDeltaTime;
            Vector3 grabbedAngularVelocity = rotationalForceApplied / grabbedObject.grabbedRigidbody.mass * Time.fixedDeltaTime;

            //Third law of motion
            playerRB.velocity += (-forceApplied / playerRB.mass * Time.fixedDeltaTime);
            if (isClimbing)
                playerRB.velocity += m_grabbedRigidbody.velocity;
            GrabbableRelease(grabbedVelocity, grabbedAngularVelocity);
        }
        
        // Re-enable grab volumes to allow overlap events
        GrabVolumeEnable(true);
        isGrabbing = false;
        isClimbing = false;
        oncePerGrab = true;
        climbingMovement = Vector3.zero;
        transform.position = m_parentTransform.position;
    }

    protected void GrabbableReleaseWithForce(Vector3 forceApplied)
    {
        m_grabbedObj.GrabEndWithForce(forceApplied);
        if(m_parentHeldObject) m_grabbedObj.transform.parent = null;
        SetPlayerIgnoreCollision(m_grabbedObj.gameObject, false);
        m_grabbedObj = null;
    }
    
    protected void GrabbableRelease(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        if (dummyTransform != null)
        {
            dummyTransform.parent = transform;
            dummyTransform.localPosition = Vector3.zero;
        }
        m_grabbedObj.GrabEnd(linearVelocity, angularVelocity);
        if(m_parentHeldObject) m_grabbedObj.transform.parent = null;
        
        //TODO: Ignore collision if climbing with at least one hand.
        SetPlayerIgnoreCollision(m_grabbedObj.gameObject, false);
        m_grabbedObj = null;
        m_grabbedRigidbody = null;
    }

    protected virtual void GrabVolumeEnable(bool enabled)
    {
        if (m_grabVolumeEnabled == enabled)
        {
            return;
        }

        m_grabVolumeEnabled = enabled;
        for (int i = 0; i < m_grabVolumes.Length; ++i)
        {
            Collider grabVolume = m_grabVolumes[i];
            grabVolume.enabled = m_grabVolumeEnabled;
        }

        if (!m_grabVolumeEnabled)
        {
            m_grabCandidates.Clear();
        }
    }

	protected virtual void OffhandGrabbed(OVRGrabbable grabbable)
    {
        if (m_grabbedObj == grabbable)
        {
            GrabbableRelease(Vector3.zero, Vector3.zero);
        }
    }

	protected void SetPlayerIgnoreCollision(GameObject grabbable, bool ignore)
	{
		if (m_player != null)
		{
			Collider[] playerColliders = m_player.GetComponentsInChildren<Collider>();
			foreach (Collider pc in playerColliders)
			{
				Collider[] colliders = grabbable.GetComponentsInChildren<Collider>();
				foreach (Collider c in colliders)
				{
					Physics.IgnoreCollision(c, pc, ignore);
				}
			}
		}
	}
}

