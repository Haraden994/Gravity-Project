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

using System;
using System.ComponentModel;
using UnityEngine;

/// <summary>
/// An object that can be grabbed and thrown by OVRGrabber.
/// </summary>
public class OVRGrabbable : MonoBehaviour
{
    [SerializeField]
    protected bool m_allowOffhandGrab = true;
    [SerializeField]
    protected bool m_snapPosition = false;
    [SerializeField]
    protected bool m_snapOrientation = false;
    [SerializeField]
    protected Transform m_snapOffset;
    [SerializeField]
    protected Collider[] m_grabPoints = null;

    public bool climbable;

    private Rigidbody _rb;
    [HideInInspector]
    public Vector3 momentum;

    [HideInInspector] 
    public bool noRigidbody;
    
    protected bool m_grabbedKinematic = false;
    protected Collider m_grabbedCollider = null;
    protected OVRGrabber m_grabbedBy = null;

	/// <summary>
	/// If true, the object can currently be grabbed.
	/// </summary>
    public bool allowOffhandGrab
    {
        get { return m_allowOffhandGrab; }
    }

	/// <summary>
	/// If true, the object is currently grabbed.
	/// </summary>
    public bool isGrabbed
    {
        get { return m_grabbedBy != null; }
    }

	/// <summary>
	/// If true, the object's position will snap to match snapOffset when grabbed.
	/// </summary>
    public bool snapPosition
    {
        get { return m_snapPosition; }
    }

	/// <summary>
	/// If true, the object's orientation will snap to match snapOffset when grabbed.
	/// </summary>
    public bool snapOrientation
    {
        get { return m_snapOrientation; }
    }

	/// <summary>
	/// An offset relative to the OVRGrabber where this object can snap when grabbed.
	/// </summary>
    public Transform snapOffset
    {
        get { return m_snapOffset; }
    }

	/// <summary>
	/// Returns the OVRGrabber currently grabbing this object.
	/// </summary>
    public OVRGrabber grabbedBy
    {
        get { return m_grabbedBy; }
    }

	/// <summary>
	/// The transform at which this object was grabbed.
	/// </summary>
    public Transform grabbedTransform
    {
        get { return m_grabbedCollider.transform; }
    }

	/// <summary>
	/// The Rigidbody of the collider that was used to grab this object.
	/// </summary>
    public Rigidbody grabbedRigidbody
    {
        get { return m_grabbedCollider.attachedRigidbody; }
    }

	/// <summary>
	/// The contact point(s) where the object was grabbed.
	/// </summary>
    public Collider[] grabPoints
    {
        get { return m_grabPoints; }
    }

	/// <summary>
	/// Notifies the object that it has been grabbed.
	/// </summary>
	virtual public void GrabBegin(OVRGrabber hand, Collider grabPoint)
    {
	    m_grabbedBy = hand;
        m_grabbedCollider = grabPoint;
        m_grabbedBy.collisionCheck = true;
        gameObject.layer = LayerMask.NameToLayer("PlayerGrabbed");
        if (climbable)
	        m_grabbedBy.isClimbing = true;
        else
        {
	        m_grabbedBy.isGrabbing = true;
	        _rb.velocity = Vector3.zero;
	        _rb.angularVelocity = Vector3.zero;
	        _rb.isKinematic = true;
        }
    }

	virtual public void ClimbNoRBBegin(OVRGrabber hand, Collider grabPoint)
	{
		m_grabbedBy = hand;
		m_grabbedCollider = grabPoint;
		m_grabbedBy.collisionCheck = true;
		m_grabbedBy.isClimbing = true;
	}

	virtual public void ClimbNoRBEnd()
	{
		m_grabbedBy.collisionCheck = false;
		m_grabbedBy = null;
		m_grabbedCollider = null;
	}
	
	/// <summary>
	/// Notifies the object that it has been released.
	/// </summary>
	virtual public void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
	{
		m_grabbedBy.collisionCheck = false;
	    _rb.isKinematic = m_grabbedKinematic;
	    if (climbable)
	    {
		    gameObject.layer = LayerMask.NameToLayer("Climbable");
		    _rb.velocity += linearVelocity;
		    _rb.angularVelocity += angularVelocity;
	    }
	    else
	    {
		    gameObject.layer = LayerMask.NameToLayer("Grabbable");
		    _rb.velocity = linearVelocity;
		    _rb.angularVelocity = angularVelocity;
	    }
	    m_grabbedBy = null;
        m_grabbedCollider = null;
    }

	public void GrabEndWithForce(Vector3 forceApplied)
	{
		_rb.isKinematic = m_grabbedKinematic;
		_rb.AddRelativeForce(forceApplied);
		m_grabbedBy = null;
		m_grabbedCollider = null;
	}

    void Awake()
    {
	    _rb = gameObject.GetComponent<Rigidbody>();
        if (m_grabPoints.Length == 0)
        {
            // Get the collider from the grabbable
            Collider collider = this.GetComponent<Collider>();
            if (collider == null)
            {
				throw new ArgumentException("Grabbables cannot have zero grab points and no collider -- please add a grab point or collider.");
            }

            // Create a default grab point
            m_grabPoints = new Collider[1] { collider };
        }
    }

    protected virtual void Start()
    {
	    if (_rb == null)
		    noRigidbody = true;
	    else
		    m_grabbedKinematic = _rb.isKinematic;
    }

    void OnDestroy()
    {
        if (m_grabbedBy != null)
        {
            // Notify the hand to release destroyed grabbables
            m_grabbedBy.ForceRelease(this);
        }
    }

    private void FixedUpdate()
    {
	    if(!noRigidbody)
			momentum = _rb.mass * _rb.velocity;
    }
}
