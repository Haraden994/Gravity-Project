using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BNG {

    /// <summary>
    /// A trigger collider that handles grabbing grabbables.
    /// </summary>
    [RequireComponent(typeof(GrabbablesInTrigger))]
    public class Grabber : MonoBehaviour {

        /// <summary>
        /// Which controller side. None if not attached to a controller.
        /// </summary>
        [Tooltip("Which controller side. None if not attached to a controller.")]
        public ControllerHand HandSide = ControllerHand.Left;

        /// <summary>
        /// 0-1 determine how much to consider a grip.
        /// Example : 0.75 is holding the grip down 3/4 of the way
        /// </summary>
        [Tooltip("0-1 determine how much to consider a grip. Example : 0.75 is holding the grip down 3/4 of the way")]
        public float GripAmount = 0.9f;

        /// <summary>
        /// How much grip considered to release an ob ect (0-1)
        /// </summary>
        [Tooltip("How much grip considered to release an ob ect (0-1)")]
        public float ReleaseGripAmount = 0.1f;

        /// <summary>
        /// The Grabbable we are currently holding. Null if not holding anything.
        /// </summary>
        [Tooltip("The Grabbable we are currently holding. Null if not holding anything.")]
        public Grabbable HeldGrabbable;

        Grabbable previousClosest;
        Grabbable previousClosestRemote;

        /// <summary>
        /// Are we currently holding any valid items?
        /// </summary>
        public bool HoldingItem
        {
            get { return HeldGrabbable != null; }
        }

        // Time.time when we last dropped a Grabbable
        public float LastDropTime;

        /// <summary>
        /// Are we currently pulling a remote grabbable towards us?
        /// </summary>
        public bool RemoteGrabbingItem {
            get { return flyingGrabbable != null; }
        }

        /// <summary>
        /// Keep track of all grabbables in trigger
        /// </summary>
        GrabbablesInTrigger grabsInTrigger;

        // Object flying at our hand
        Grabbable flyingGrabbable;
        // How long the object has been flying at our hand
        float flyingTime = 0;

        /// <summary>
        /// Root transform that holds hands models. We may want to hide these while holding certain objects.
        /// </summary>
        public Transform HandsGraphics;
        Transform handsGraphicsParent;
        Vector3 handsGraphicsPosition;
        Quaternion handsGraphicsRotation;

        /// <summary>
        /// Assign a Grabbable here if you want to auto equip it on Start
        /// </summary>
        [Tooltip("Assign a Grabbable here if you want to auto equip it on Start")]
        public Grabbable EquipGrabbableOnStart;

        /// <summary>
        /// Same as holding down grip if set to true. Should not have same value as ForceRelease.
        /// </summary>
        [Tooltip("Same as holding down grip if set to true. Should not have same value as ForceRelease.")]
        public bool ForceGrab = false;

        /// <summary>
        /// Force the release of grip
        /// </summary>
        [Tooltip("Force the release of grip.")]
        public bool ForceRelease = false;

        public Vector3 PreviousPosition;

        /// <summary>
        /// Can be used to position hands independently from model
        /// /// </summary>
        public Transform DummyTransform; 

        // Number of FixedUpdates to average velocities
        float averageVelocityCount = 3;
        List<Vector3> previousVelocities;
        List<Vector3> previousAngularVelocities;

        Rigidbody rb;
        InputBridge input;
        ConfigurableJoint joint;

        // Is this a fresh grab / has the control been depressed
        [HideInInspector]
        public bool FreshGrip = true;
        /// <summary>
        /// How many seconds to check for grab input while Grip is held down. After grip is held down for this long, grip will need to be repressed in order to pick up an object.
        /// </summary>
        [Tooltip("How many seconds to check for grab input while Grip is held down. After grip is held down for this long, grip will need to be repressed in order to pick up an object.")]
        public float GrabCheckSeconds = 2f;
        float currentGrabTime;

        // Used for tracking playspace rotation
        GameObject playSpace;

        void Start() {
            rb = GetComponent<Rigidbody>();
            grabsInTrigger = GetComponent<GrabbablesInTrigger>();
            joint = GetComponent<ConfigurableJoint>();
            // Setup defaults
            if(joint == null) {
                joint = gameObject.AddComponent<ConfigurableJoint>();
                joint.rotationDriveMode = RotationDriveMode.Slerp;

                JointDrive slerpDrive = joint.slerpDrive;
                slerpDrive.positionSpring = 600;

                JointDrive xDrive = joint.xDrive;
                xDrive.positionSpring = 2500;
                JointDrive yDrive = joint.yDrive;
                yDrive.positionSpring = 2500;
                JointDrive zDrive = joint.zDrive;
                zDrive.positionSpring = 2500;
            }

            // Look for input from Player
            if (GameObject.FindGameObjectWithTag("Player")) {
                input = GameObject.FindGameObjectWithTag("Player").GetComponent<InputBridge>();
            }

            if(HandsGraphics) {
                handsGraphicsParent = HandsGraphics.transform.parent;
                handsGraphicsPosition = HandsGraphics.transform.localPosition;
                handsGraphicsRotation = HandsGraphics.transform.localRotation;
            }

            playSpace = GameObject.Find("TrackingSpace");
            previousVelocities = new List<Vector3>();
            previousAngularVelocities = new List<Vector3>();

            // Make Collision Dynamic so we don't miss any collisions
            if (rb && rb.isKinematic) {
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            // Should we auto equip an item
            if(EquipGrabbableOnStart != null) {
                GrabGrabbable(EquipGrabbableOnStart);
            }
        }

        void Update() {

            // Keep track of how long an object has been trying to fly to our hand
            if(flyingGrabbable != null) {
                flyingTime += Time.deltaTime;
                
                // Only allow an object to fly towards us
                float maxFlyingGrabbableTime = 5;
                if(flyingTime > maxFlyingGrabbableTime) {
                    resetFlyingGrabbable();
                }
            }

            // Make sure grab is valid
            updateFreshGrabStatus();

            // Fire off updates
            checkGrabbableEvents();

            // Check for input to grab or release item
            if ((inputCheckGrab() && !HoldingItem) || ForceGrab) {
                TryGrab();               
            }
            else if((HoldingItem && inputCheckRelease()) || ForceRelease) {                
                TryRelease();
            }
        }

        void updateFreshGrabStatus() {
            // Update Fresh Grab status
            if (getGrabInput(GrabButton.Grip) <= ReleaseGripAmount) {
                // We release grab, so this is considered fresh
                FreshGrip = true;
                currentGrabTime = 0;
            }

            // Increment fresh grab time
            if (getGrabInput(GrabButton.Grip) > GripAmount) {
                currentGrabTime += Time.deltaTime;
            }

            // Not considered a valid grab if holding down for too long
            if (currentGrabTime > GrabCheckSeconds) {
                FreshGrip = false;
            }
        }

        void checkGrabbableEvents() {

            // Bail if nothing in our trigger area
            if(grabsInTrigger == null) {
                return;
            }

            // If last closest was this one let event know and remove validator  
            if (previousClosest != grabsInTrigger.ClosestGrabbable) {
                if (previousClosest != null) {

                    // Fire Off Events
                    GrabbableEvents[] ge = previousClosest.GetComponents<GrabbableEvents>();
                    if (ge != null) {
                        for (int x = 0; x < ge.Length; x++) {
                            ge[x].OnNoLongerClosestGrabbable(HandSide);
                        }
                    }
                    previousClosest.RemoveValidGrabber(this);
                }

                // Update closest Grabbable
                if (grabsInTrigger.ClosestGrabbable != null && !HoldingItem) {

                    // Fire Off Events
                    GrabbableEvents[] ge = grabsInTrigger.ClosestGrabbable.GetComponents<GrabbableEvents>();
                    if (ge != null) {
                        for (int x = 0; x < ge.Length; x++) {
                            ge[x].OnBecomesClosestGrabbable(HandSide);
                        }
                    }
                    grabsInTrigger.ClosestGrabbable.AddValidGrabber(this);
                }
            }

            if (grabsInTrigger.ClosestGrabbable != null && !HoldingItem) {
                grabsInTrigger.ClosestGrabbable.AddValidGrabber(this);
            }

            // Remote Grabbable Events
            // If last closest was this one, unhighlight object            
            if (previousClosestRemote != grabsInTrigger.ClosestRemoteGrabbable) {
                if (previousClosestRemote != null) {
                    // Fire Off Events
                    GrabbableEvents[] ge = previousClosestRemote.GetComponents<GrabbableEvents>();
                    if (ge != null) {
                        for (int x = 0; x < ge.Length; x++) {
                            ge[x].OnNoLongerClosestRemoteGrabbable(HandSide);
                        }

                    }
                    previousClosestRemote.RemoveValidGrabber(this);
                }

                // Update closest remote Grabbable
                if (grabsInTrigger.ClosestRemoteGrabbable != null && !HoldingItem) {

                    // Fire Off Events 
                    GrabbableEvents[] ge = grabsInTrigger.ClosestRemoteGrabbable.GetComponents<GrabbableEvents>();
                    if (ge != null) {
                        for (int x = 0; x < ge.Length; x++) {
                            ge[x].OnBecomesClosestRemoteGrabbable(HandSide);
                        }
                    }

                    grabsInTrigger.ClosestRemoteGrabbable.AddValidGrabber(this);
                }
            }

            // Set this as previous closest
            previousClosest = grabsInTrigger.ClosestGrabbable;
            previousClosestRemote = grabsInTrigger.ClosestRemoteGrabbable;
        }        

        void FixedUpdate() {
            updateVelocities();
        }

        void updateVelocities() {
            previousVelocities.Add(GetGrabberVelocity());
            // Shrink list if necessary
            if(previousVelocities.Count >= averageVelocityCount) {
                previousVelocities.RemoveAt(0);
            }

            previousAngularVelocities.Add(GetGrabberAngularVelocity());
            if (previousAngularVelocities.Count >= averageVelocityCount) {
                previousAngularVelocities.RemoveAt(0);
            }
        }

        // See if we are inputting controls to grab an item
        bool inputCheckGrab() {

            // Can only hold one grabbable at a time
            if(HeldGrabbable != null) {
                return false;
            }

            // Nothing nearby to grab
            Grabbable closest = getClosestOrRemote();
            if (closest == null) {
                return false;
            }

            // Check Hold Controls
            if (closest.Grabtype == HoldType.HoldDown) {
                bool grabInput = getGrabInput(closest.GrabButton) >= GripAmount;

                if(closest.GrabButton == GrabButton.Grip && !FreshGrip) {
                    return false;
                }

                return grabInput;
            }
            // Check for toggle controls
            else if (closest.Grabtype == HoldType.Toggle) {
                return getToggleInput(closest.GrabButton);
            }

            return false;
        }

        GrabButton getGrabButton() {
            if(grabsInTrigger.ClosestGrabbable != null) {
                return grabsInTrigger.ClosestGrabbable.GrabButton;
            }
            else if (grabsInTrigger.ClosestRemoteGrabbable != null) {
                return grabsInTrigger.ClosestRemoteGrabbable.GrabButton;
            }

            return GrabButton.Grip;
        }

        Grabbable getClosestOrRemote() {
            if (grabsInTrigger.ClosestGrabbable != null) {
                return grabsInTrigger.ClosestGrabbable;
            }
            else if (grabsInTrigger.ClosestRemoteGrabbable != null) {
                return grabsInTrigger.ClosestRemoteGrabbable;
            }

            return null;
        }

        // Release conditions are a little different than grab
        bool inputCheckRelease() {            

            // Can't release anything we're not holding
            if (HeldGrabbable == null) {
                return false;
            }

            // Check Hold Controls
            if (HeldGrabbable.Grabtype == HoldType.HoldDown) {
                return getGrabInput(HeldGrabbable.GrabButton) <= ReleaseGripAmount;
            }
            // Check for toggle controls
            else if (HeldGrabbable.Grabtype == HoldType.Toggle) {
                return getToggleInput(HeldGrabbable.GrabButton);
            }

            return false;
        }

        float getGrabInput(GrabButton btn) {
            float gripValue = 0;

            if(input == null) {
                return 0;
            }

            // Left Hand
            if (HandSide == ControllerHand.Left) {
                if (btn == GrabButton.Grip) {
                    gripValue = input.LeftGrip;
                }
                else if (btn == GrabButton.Trigger) {
                    gripValue = input.LeftTrigger;
                }
            }
            // Right Hand
            else if (HandSide == ControllerHand.Right) {
                if (btn == GrabButton.Grip) {
                    gripValue = input.RightGrip;
                }
                else if (btn == GrabButton.Trigger) {
                    gripValue = input.RightTrigger;
                }
            }

            return gripValue;
        }

        bool getToggleInput(GrabButton btn) {

            if (input == null) {
                return false;
            }

            // Left Hand
            if (HandSide == ControllerHand.Left) {
                if (btn == GrabButton.Grip) {
                    return input.LeftGripDown;
                }
                else if (btn == GrabButton.Trigger) {
                    return input.LeftTriggerDown;
                }
            }
            // Right Hand
            else if (HandSide == ControllerHand.Right) {
                if (btn == GrabButton.Grip) {
                    return input.RightGripDown;
                }
                else if (btn == GrabButton.Trigger) {
                    return input.RightTriggerDown;
                }
            }

            return false;
        }               

        public bool TryGrab() {
            // Already holding something
            if (HeldGrabbable != null) {
                return false;
            }            

            // Activate Nearby Grabbable
            if (grabsInTrigger.ClosestGrabbable != null) {
                GrabGrabbable(grabsInTrigger.ClosestGrabbable);                
                
                return true;
            }
            // If no immediate grabbable, see if remote is available to pull
            else if(grabsInTrigger.ClosestRemoteGrabbable != null && flyingGrabbable == null) {
                flyingGrabbable = grabsInTrigger.ClosestRemoteGrabbable;
                flyingGrabbable.GrabRemoteItem(this);
            }

            return false;
        }

        // Assign new held Item, then grab the item into our hand / controller
        public void GrabGrabbable(Grabbable item) {

            // We are trying to grab something else
            if(flyingGrabbable != null && item != flyingGrabbable) {
                return;
            }

            // Make sure we aren't flying an object at us still
            resetFlyingGrabbable();

            // Drop whatever we were holding
            if (HeldGrabbable != null && HeldGrabbable) {
                TryRelease();
            }

            // Assign new grabbable
            HeldGrabbable = item;

            // Just grabbed something, no longer fresh.
            FreshGrip = false;

            // Let item know it's been grabbed
            item.GrabItem(this);
        }

        // Dropped whatever was in hand
        public void DidDrop() {
            HeldGrabbable = null;

            transform.localEulerAngles = Vector3.zero;

            LastDropTime = Time.time;

            resetFlyingGrabbable();

            ResetHandGraphics();
        }

        public void HideHandGraphics() {
            if (HandsGraphics != null) {
                HandsGraphics.gameObject.SetActive(false);
            }
        }

        public void ResetHandGraphics() {
            if(HandsGraphics != null) {
                // Make visible again
                HandsGraphics.gameObject.SetActive(true);

                // Move parent back to where it was originally
                HandsGraphics.transform.parent = handsGraphicsParent;
                HandsGraphics.transform.localPosition = handsGraphicsPosition;
                HandsGraphics.transform.localRotation = handsGraphicsRotation;
            }
        }

        void TryRelease() {
            if (HeldGrabbable != null && HeldGrabbable.CanBeDropped) {
                HeldGrabbable.DropItem(this);
            }

            // No longer try to bring flying grabbable to us
            resetFlyingGrabbable();
        }

        void resetFlyingGrabbable() {
            // No longer flying at us
            if (flyingGrabbable != null) {
                flyingGrabbable.ResetGrabbing();
                flyingGrabbable = null;
                flyingTime = 0;
            }
        }       

        public Vector3 GetGrabberVelocity() {
            // Left controller velocity
            if (HandSide == ControllerHand.Left) {
                return OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch);
            }
            // Right controller velocity
            else if (HandSide == ControllerHand.Right) {
                return OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);
            }

            return Vector3.zero;
        }

        public Vector3 GetGrabberAveragedVelocity() {
            if (playSpace) {
                return playSpace.transform.rotation * GetAveragedVector(previousVelocities);
            }
            else {
                return GetAveragedVector(previousVelocities);
            }
        }

        public Vector3 GetGrabberAveragedAngularVelocity() {

            if (playSpace) {
                return playSpace.transform.rotation * GetAveragedVector(previousAngularVelocities);
            }
            else {
                return GetAveragedVector(previousAngularVelocities);
            }
        }

        Vector3 GetAveragedVector(List<Vector3> vectors) {

            if (vectors != null) {

                int count = vectors.Count;
                float x = 0;
                float y = 0; 
                float z = 0;

                for (int i = 0; i < count; i++) {
                    Vector3 v = vectors[i];
                    x += v.x;
                    y += v.y;
                    z += v.z;
                }

                return new Vector3(x / count, y / count, z / count);
            }

            return Vector3.zero;
        }

        public Vector3 GetGrabberAngularVelocity() {
            
            // Left controller angular velocity
            if (HandSide == ControllerHand.Left) {
                return OVRInput.GetLocalControllerAngularVelocity(OVRInput.Controller.LTouch);
            }
            // Right controller angular velocity
            else if (HandSide == ControllerHand.Right) {
                return OVRInput.GetLocalControllerAngularVelocity(OVRInput.Controller.RTouch);
            }

            return Vector3.zero;
        }
    }
}