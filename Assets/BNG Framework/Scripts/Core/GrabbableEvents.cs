using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BNG {

    /// <summary>
    /// Override this class to respond to various events that happen to this Grabbable
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public abstract class GrabbableEvents : MonoBehaviour {

        protected Grabbable grab;
        protected Grabber thisGrabber;
        protected InputBridge input;

        private void Awake() {
            grab = GetComponent<Grabbable>();

            if(GameObject.FindGameObjectWithTag("Player")) {
                input = GameObject.FindGameObjectWithTag("Player").GetComponent<InputBridge>();
            }
        }

        /// <summary>
        /// Item has been grabbed by a Grabber
        /// </summary>
        /// <param name="grabber"></param>
        public virtual void OnGrab(Grabber grabber) {
            thisGrabber = grabber;
        }
        
        /// <summary>
        /// Has been dropped from the Grabber
        /// </summary>
        public virtual void OnRelease() {
           
        }

        /// <summary>
        /// Called if this is the closest grabbable but wasn't in the previous frame 
        /// </summary>
        /// <param name="touchingHand"></param>
        public virtual void OnBecomesClosestGrabbable(ControllerHand touchingHand) {
            
        }

        /// <summary>
        /// No longer closest grabbable. May need to disable highlight, ring, etc.
        /// </summary>
        /// <param name="touchingHand"></param>
        public virtual void OnNoLongerClosestGrabbable(ControllerHand touchingHand) {
            
        }

        /// <summary>
        /// Fires if this is the closest remote grabbable but wasn't in the previous frame
        /// </summary>
        /// <param name="touchingHand"></param>
        public virtual void OnBecomesClosestRemoteGrabbable(ControllerHand touchingHand) {
            
        }

        /// <summary>
        /// Fires if this was the closest remote grabbable last frame, but not this frame
        /// </summary>
        /// <param name="touchingHand"></param>
        public virtual void OnNoLongerClosestRemoteGrabbable(ControllerHand touchingHand) {
            
        }

        /// <summary>
        /// Amount of Grip (0-1). Only fired if object is being held.
        /// </summary>
        /// <param name="gripValue">0 - 1 Open / Closed</param>
        public virtual void OnGrip(float gripValue) {
            
        }

        /// <summary>
        /// Amount of Trigger being held down on the grabbed items controller. Only fired if object is being held.
        /// </summary>
        /// <param name="triggerValue">0 - 1 Open / Closed</param>
        public virtual void OnTrigger(float triggerValue) {
            
        }

        /// <summary>
        /// Fires if trigger was pressed down on this controller this frame. Only fired if object is being held.
        /// </summary>
        public virtual void OnTriggerDown() {
            
        }

        /// <summary>
        /// Fires if trigger was released on this controller this frame. Only fired if object is being held.
        /// </summary>
        public virtual void OnTriggerUp() {
           
        }

        /// <summary>
        /// Button 1 is being held down this frame but not last
        /// Oculus : Button 1 = "A" if held in Right controller."X" if held in Left Controller
        /// </summary>
        public virtual void OnButton1() {
            
        }

        /// <summary>
        /// Button 1 Pressed down this frame
        /// Oculus : Button 1 = "A" if held in Right controller."X" if held in Left Controller
        /// </summary>
        public virtual void OnButton1Down() {

        }

        /// <summary>
        /// Button 1 Released this frame
        /// Oculus : Button 1 = "A" if held in Right controller."X" if held in Left Controller
        /// </summary>
        public virtual void OnButton1Up() {
            
        }


        /// <summary>
        /// Button 2 is being held down this frame but not last
        /// Oculus : Button 2 = "B" if held in Right controller."Y" if held in Left Controller
        /// </summary>
        public virtual void OnButton2() {
            
        }

        /// <summary>
        /// Button 2 Pressed down this frame
        /// Oculus : Button 2 = "B" if held in Right controller."Y" if held in Left Controller
        /// </summary>
        public virtual void OnButton2Down() {
           
        }

        /// <summary>
        /// Button 2 Released this frame
        /// Oculus : Button 2 = "B" if held in Right controller."Y" if held in Left Controller
        /// </summary>
        public virtual void OnButton2Up() {

        }
    }
}